using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ==============================================================================
// CLASSE EVENT MANAGER (O Ouvinte Universal)
// Onde a Mágica Acontece: Capta os eventos C# da Unity e engatilha os scripts LUA.
// Tratativas Críticas & Dependências: 
// - CardEffectManager.cs: Transfere as chamadas para formar as correntes (ChainManager).
// - PhaseManager.cs / GameManager.cs: Avisa ao LUA quando turnos, danos e invocações ocorrem.
// ==============================================================================

public class EventData
{
    public object eg;
    public int ep;
    public int ev;
    public LuaEffect re;
    public int r;
    public int rp;

    public EventData(object eg, int ep, int ev, LuaEffect re, int r, int rp)
    {
        this.eg = eg;
        this.ep = ep;
        this.ev = ev;
        this.re = re;
        this.r = r;
        this.rp = rp;
    }
}

public class LuaEventManager
{
    private CardEffectManager core;
    public bool isProcessingTriggers = false;
    private int triggerTasks = 0;
    private HashSet<CardDisplay> cardsLeavingField = new HashSet<CardDisplay>();

    public LuaEventManager(CardEffectManager coreManager)
    {
        this.core = coreManager;
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;
        core.StartCoroutine(ProcessTriggersRoutine(eventCode, triggerArgs));
    }

    public IEnumerator ProcessTriggersRoutine(int eventCode, object triggerArgs)
    {
        triggerTasks++;
        isProcessingTriggers = true;

        // Identifica a carta que originou o evento para evitar re-trigger
        LuaCard sourceCard = null;
        object actualEg = triggerArgs;
        if (triggerArgs is EventData ed) actualEg = ed.eg;

        if (actualEg is LuaCard lc) sourceCard = lc;
        else if (actualEg is CardDisplay cd) sourceCard = core.EnsureCardScriptLoaded(cd);

        // 1. Processa efeitos globais e gatilhos passivos registrados na memória
        if (core.luaDuel != null && core.luaDuel.globalEffects != null)
        {
            var matchingGlobal = core.luaDuel.globalEffects.FindAll(e => e.code == eventCode);
            foreach (var effect in matchingGlobal)
            {
                if (core.CanActivateEffect(effect.owner, effect, 0, triggerArgs))
                {
                    if (effect.operationFunc != null)
                    {
                        bool opDone = false;
                        core.StartCoroutine(RunCoroutineAndSetDone(core.RunLuaCoroutine(effect.operationFunc, effect, 0, triggerArgs, -1), () => opDone = true));
                        yield return new WaitUntil(() => opDone);
                        yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.pendingVisualTasks > 0);
                    }
                }
            }
        }

        // 2. Processa efeitos das cartas físicas no campo
        List<CardDisplay> allField = new List<CardDisplay>();
        CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allField);

        // 3. Inclui a mão para processar efeitos reativos de cartas na mão (ex: Kuriboh, Deal with Dark Ruler)
        foreach(var go in GameManager.Instance.playerHand) { if (go != null) { var cd = go.GetComponent<CardDisplay>(); if (cd != null) allField.Add(cd); } }
        foreach(var go in GameManager.Instance.opponentHand) { if (go != null) { var cd = go.GetComponent<CardDisplay>(); if (cd != null) allField.Add(cd); } }

        foreach(var c in allField)
        {
            // NOVO: Pula a carta que originou o evento para não duplicar a corrente
            if (sourceCard != null && c == sourceCard.unityCard) continue;

            LuaCard lc_loop = core.EnsureCardScriptLoaded(c);
            if (lc_loop == null) continue;

            // Impede a Engine de auto-ativar cartas Manuais (Spells/Traps) ou Quick Effects. Eles devem ser ativados pelo jogador na Response Window!
            // EXCLUI EFFECT_TYPE_SINGLE (0x0001) para impedir que gatilhos pessoais (ex: EVENT_TO_GRAVE) disparem falsamente quando outra carta morre.
            var matchingEffects = lc_loop.registeredEffects.FindAll(e => e.code == eventCode && !e.isTypeSingle && (e.isTypeTriggerF || e.isTypeQuickF || e.isTypeContinuous));
            foreach(var effect in matchingEffects)
            {
                if (core.CanActivateEffect(lc_loop, effect, lc_loop.GetControler(), triggerArgs))
                {
                    if (effect.isTypeContinuous)
                    {
                        if (effect.operationFunc != null)
                        {
                            if (effect.hasCountLimit || effect.countLimitMax > 0)
                            {
                                if (effect.countLimitCode == 0) effect.currentUsages++;
                                else
                                {
                                    string key = $"{lc_loop.GetControler()}_{effect.countLimitCode}";
                                    if (!core.luaDuel.hardOncePerTurnUsages.ContainsKey(key)) core.luaDuel.hardOncePerTurnUsages[key] = 0;
                                    core.luaDuel.hardOncePerTurnUsages[key]++;
                                }
                            }
                            core.luaDuel.currentContinuousEffect = effect;
                            bool opDone = false;
                            core.StartCoroutine(RunCoroutineAndSetDone(core.RunLuaCoroutine(effect.operationFunc, effect, lc_loop.GetControler(), triggerArgs, -1), () => opDone = true));
                            yield return new WaitUntil(() => opDone);
                            core.luaDuel.currentContinuousEffect = null;
                            yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.pendingVisualTasks > 0);
                        }
                    }
                    else
                    {
                        bool chainDone = false;
                        core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc_loop, effect, triggerArgs, lc_loop.GetControler(), () => chainDone = true));
                        yield return new WaitUntil(() => chainDone);
                    }
                }
            }
        }
        triggerTasks--;
        if (triggerTasks <= 0) isProcessingTriggers = false;
    }

    private IEnumerator ProcessSingleEffectsRoutine(LuaCard lc, List<LuaEffect> effects, object triggerArgs)
    {
        triggerTasks++;
        isProcessingTriggers = true;
        
        foreach (var e in effects)
        {
            if (core.CanActivateEffect(lc, e, lc.GetControler(), triggerArgs))
            {
                    if (e.isTypeContinuous)
                    {
                        if (e.operationFunc != null)
                        {
                            if (e.hasCountLimit || e.countLimitMax > 0)
                            {
                                if (e.countLimitCode == 0) e.currentUsages++;
                                else
                                {
                                    string key = $"{lc.GetControler()}_{e.countLimitCode}";
                                    if (!core.luaDuel.hardOncePerTurnUsages.ContainsKey(key)) core.luaDuel.hardOncePerTurnUsages[key] = 0;
                                    core.luaDuel.hardOncePerTurnUsages[key]++;
                                }
                            }
                            core.luaDuel.currentContinuousEffect = e;
                            bool opDone = false;
                            core.StartCoroutine(RunCoroutineAndSetDone(core.RunLuaCoroutine(e.operationFunc, e, lc.GetControler(), triggerArgs, -1), () => opDone = true));
                            yield return new WaitUntil(() => opDone);
                            core.luaDuel.currentContinuousEffect = null;
                            yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.pendingVisualTasks > 0);
                        }
                    }
                    else if (e.isTypeTriggerF || e.isTypeQuickF)
                    {
                        bool chainDone = false;
                        core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, triggerArgs, lc.GetControler(), () => chainDone = true));
                        yield return new WaitUntil(() => chainDone);
                    }
                    else if (e.isTypeTriggerO || e.isTypeQuickO)
                    {
                        bool activate = false;
                        bool answered = false;

                        if (GameManager.Instance.isSimulating || lc.GetControler() != 0 || (OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeSelf && lc.GetControler() == 1))
                        {
                            // IA ou Simulador sempre aceita ativar efeitos benéficos de cemitério/campo
                            activate = true;
                            answered = true;
                        }
                        else if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ShowConfirmation($"Ativar o efeito de {lc.unityData.name}?", 
                                () => { activate = true; answered = true; }, 
                                () => { activate = false; answered = true; });
                        }
                        else
                        {
                            activate = true;
                            answered = true;
                        }

                        yield return new WaitUntil(() => answered);

                        if (activate)
                        {
                            bool chainDone = false;
                            core.StartCoroutine(core.chainManager.BuildAndResolveChainRoutine(lc, e, triggerArgs, lc.GetControler(), () => chainDone = true));
                            yield return new WaitUntil(() => chainDone);
                        }
                    }
            }
        }
        triggerTasks--;
        if (triggerTasks <= 0) isProcessingTriggers = false;
    }

    private IEnumerator RunCoroutineAndSetDone(IEnumerator routine, System.Action onDone)
    {
        yield return routine;
        onDone?.Invoke();
    }

    private void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (z != null && z.childCount > 0)
            {
                var cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    public void OnSummon(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);

        // Debug.Log($"<color=orange>[DEBUG LUA]</color> EVENT_SUMMON_SUCCESS (1100) acionado para: {card.CurrentCardData.name} | ATK: {card.currentAtk} | Controlador: {lc.GetControler()} | Faceup: {!card.isFlipped} | IsOnField: {card.isOnField}");

        // Adiciona efeitos contínuos ao entrar em campo
        var fieldEffects = lc.registeredEffects.FindAll(e => (e.type & 0x0002) != 0);
        core.continuousFieldEffects.AddRange(fieldEffects.Where(e => !core.continuousFieldEffects.Contains(e)));
        if (fieldEffects.Count > 0)
            core.ApplyAllContinuousEffects();
        
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1100 && e.isTypeSingle); // EFFECT_TYPE_SINGLE
        if (singleEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }
        TriggerLuaEvent(1100, ed); // EVENT_SUMMON_SUCCESS
        core.StartCoroutine(core.OpenFastEffectWindow($"Invocação de {card.CurrentCardData.name}", 1100, ed));
    }
    
    public void OnFlip(CardDisplay card)
    {
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);

        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1001 && e.isTypeFlip); // EVENT_FLIP (1001) & EFFECT_TYPE_FLIP
        if (singleEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }
        // Não abrimos janela de resposta para FLIP, pois é um efeito obrigatório que inicia sua própria corrente.
    }

    public void OnSet(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);
        
        int eventCode = card.CurrentCardData.type.Contains("Monster") ? 1107 : 1108; // 1107: EVENT_MSET, 1108: EVENT_SSET
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());
        TriggerLuaEvent(eventCode, ed);
        core.StartCoroutine(core.OpenFastEffectWindow($"Carta Baixada", eventCode, ed));
    }
    
    public void OnFlipSummon(CardDisplay card) {
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);

        // Debug.Log($"<color=orange>[DEBUG LUA]</color> EVENT_FLIP_SUMMON_SUCCESS (1101) acionado para: {card.CurrentCardData.name} | ATK: {card.currentAtk} | Controlador: {lc.GetControler()} | Faceup: {!card.isFlipped} | IsOnField: {card.isOnField}");
        
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1101 && e.isTypeSingle); // EFFECT_TYPE_SINGLE
        if (singleEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }

        TriggerLuaEvent(1101, ed); // EVENT_FLIP_SUMMON_SUCCESS
        core.StartCoroutine(core.OpenFastEffectWindow($"Invocação-Virar de {card.CurrentCardData.name}", 1101, ed));
    }

    public void OnBattlePositionChanged(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);
        
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());
        TriggerLuaEvent(1016, ed); // EVENT_CHANGE_POS
        core.StartCoroutine(core.OpenFastEffectWindow($"Mudança de Posição de {card.CurrentCardData.name}", 1016, ed));
    }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);
        core.ApplyAllContinuousEffects();

        LuaGroup eg = new LuaGroup(); eg.AddCard(lc);
        int ep = lc.GetControler();
        EventData ed = new EventData(eg, ep, 0, null, 0, ep);
        TriggerLuaEvent(1012, ed); // 1012 = EVENT_TO_HAND
        core.StartCoroutine(core.OpenFastEffectWindow($"Adicionada à Mão", 1012, ed, 0x200000)); // TIMING_TOHAND
    }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent)
    {
        // Recalcula stats caso uma carta na mão tenha sido descartada
        core.ApplyAllContinuousEffects();

        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);
        LuaGroup eg = new LuaGroup(); eg.AddCard(lc);
        int ep = card.ownerPlayer ? 0 : 1;
        int rp = causedByOpponent ? (1 - ep) : ep;

        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) {
            rp = core.chainManager.resolvingLink.player;
            re = core.chainManager.resolvingLink.effect;
        }

        EventData ed = new EventData(eg, ep, 1, re, 0x40 | 0x4000, rp); // REASON_EFFECT | REASON_DISCARD
        TriggerLuaEvent(1018, ed); // EVENT_DISCARD
        core.StartCoroutine(core.OpenFastEffectWindow($"Carta Descartada", 1018, ed));
    }
    public void OnCardDrawn(CardData card, bool isPlayer) 
    { 
        int ep = isPlayer ? 0 : 1;
        LuaCard lc = null;
        
        // RESGATE DE IDENTIDADE: Busca a instância persistente que pode conter efeitos "presos" (Ex: Parasite Paracide)
        if (core.luaDuel != null && core.luaDuel.persistentCards.ContainsKey(card))
            lc = core.luaDuel.persistentCards[card];
        else
            lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
            
        if (lc == null) lc = new LuaCard(card);
        
        LuaGroup eg = new LuaGroup(); eg.AddCard(lc);
        EventData ed = new EventData(eg, ep, 1, null, 0, ep);
        
        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1110 && e.isTypeSingle); // EVENT_DRAW
        if (singleEffects.Count > 0) {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }
        
        TriggerLuaEvent(1110, ed); // 1110 = EVENT_DRAW
        core.StartCoroutine(core.OpenFastEffectWindow($"Carta Comprada", 1110, ed));
    }
    public void OnSpecialSummon(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);

        var fieldEffects = lc.registeredEffects.FindAll(e => (e.type & 0x0002) != 0);
        core.continuousFieldEffects.AddRange(fieldEffects.Where(e => !core.continuousFieldEffects.Contains(e)));
        if (fieldEffects.Count > 0)
            core.ApplyAllContinuousEffects();
        
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1102 && e.isTypeSingle); // EFFECT_TYPE_SINGLE
        if (singleEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }

        TriggerLuaEvent(1102, ed); // EVENT_SPSUMMON_SUCCESS
        core.StartCoroutine(core.OpenFastEffectWindow($"Invocação Especial de {card.CurrentCardData.name}", 1102, ed));
    }
    public void OnControlSwitched(CardDisplay card) { 
        LuaCard lc = core.EnsureCardScriptLoaded(card);
        if (lc == null) lc = new LuaCard(card);
        
        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        var singleEffects = lc.registeredEffects.FindAll(e => e.code == 1120 && e.isTypeSingle); // 1120 = EVENT_CONTROL_CHANGED
        if (singleEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, singleEffects, ed));
        }

        TriggerLuaEvent(1120, ed); // EVENT_CONTROL_CHANGED
        core.StartCoroutine(core.OpenFastEffectWindow($"Controle Alterado", 1120, ed));
    }
    
    public void OnPhaseStart(GamePhase phase) { 
        int tp = core.luaDuel.GetTurnPlayer();
        EventData edPhase = new EventData(null, tp, 0, null, 0, tp);

        // Dispara o Gatilho Específico de Fase para o Lua (ex: Fim da Batalha)
        if (phase == GamePhase.Main2 || phase == GamePhase.End) {
            TriggerLuaEvent(4096 + 128, edPhase); // EVENT_PHASE + PHASE_BATTLE (4224)
            
            // LIMPEZA OFICIAL (OCG): O contexto de ataque cessa de existir ao sair da Battle Phase
            core.luaDuel.currentAttacker = null;
            core.luaDuel.currentAttackTarget = null;
        }
        if (phase == GamePhase.End) {
            TriggerLuaEvent(4096 + 256, edPhase); // EVENT_PHASE + PHASE_MAIN2 (4352)
        }

        TriggerLuaEvent(4096, edPhase); // EVENT_PHASE
        
        int timing = 0;
        if (phase == GamePhase.Standby) timing = 0x2; // TIMING_STANDBY_PHASE
        else if (phase == GamePhase.End) timing = 0x20; // TIMING_END_PHASE
        
        core.StartCoroutine(core.OpenFastEffectWindow($"Início da {phase}", 4096, edPhase, timing));

        // Executa a limpeza pesada da memória APENAS depois que todos os efeitos do fim do turno se resolverem
        if (phase == GamePhase.End) {
            core.StartCoroutine(DelayedEndTurnCleanup());
        }
    }

    private IEnumerator DelayedEndTurnCleanup()
    {
        // Espera pacientemente qualquer janela de resposta (UI) e corrente de cartas terminarem de agir
        yield return new WaitWhile(() => core.isChainResolving || core.isWaitingForLuaYield || core.isFastEffectWindowOpen);
        yield return new WaitForEndOfFrame();

        core.CleanAllExpiredModifiers(); 
        core.luaDuel.playerFlags.Clear(); // Limpa as flags temporárias de turno
        core.luaDuel.cardFlags.Clear();   // Limpa a memória das cartas no fim do turno
        core.luaDuel.hardOncePerTurnUsages.Clear(); // Limpa os usos "Hard Once per Turn"
        
        GameManager.Instance.normalSummonsThisTurnPlayer = 0;
        GameManager.Instance.normalSummonsThisTurnOpponent = 0;
        GameManager.Instance.specialSummonsThisTurnPlayer = 0;
        GameManager.Instance.specialSummonsThisTurnOpponent = 0;
        GameManager.Instance.flipSummonsThisTurnPlayer = 0;
        GameManager.Instance.flipSummonsThisTurnOpponent = 0;
        GameManager.Instance.attacksThisTurnPlayer = 0;
        GameManager.Instance.attacksThisTurnOpponent = 0;

        foreach (var lc in core.activeLuaCards.Values)
        {
            foreach (var eff in lc.registeredEffects)
            {
                if (!eff.noTurnReset) eff.currentUsages = 0; // EFFECT_FLAG_NO_TURN_RESET (Pula a limpeza!)
            }
        }
        
        // Limpa o Histórico de Batalha (Safety Net) no fim do turno
        core.luaDuel.historicalAttacker = null;
        core.luaDuel.historicalAttackTarget = null;

        // Remove Efeitos Globais/Invisíveis que duram até o fim do turno!
        if (core.luaDuel.globalEffects != null) {
            core.luaDuel.globalEffects.RemoveAll(e => (e.GetReset() & 0x1000) != 0 || (e.GetReset() & 0x0200) != 0);
        }

        if (core.luaDuel.endTurnCallbacks != null) {
            var callbacksToRun = new List<MoonSharp.Interpreter.Closure>(core.luaDuel.endTurnCallbacks);
            foreach (var cb in callbacksToRun) {
                try { core.luaEngine.Call(cb); } catch (System.Exception e) { Debug.LogWarning($"Erro no AddValuesReset: {e.Message}"); }
            }
        }
    }
    
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason) 
    { 
        if (card == null) return;

        LuaCard lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
        if (lc == null)
        {
            lc = new LuaCard(card);
        }

        lc.previousLocation = fromLocation; // Memoriza de onde veio para o IsPreviousLocation do LUA!
        lc.ownerPlayerIndex = isOwnerPlayer ? 0 : 1;
        int tp = lc.GetControler();

        // Debug.Log($"[LuaEventManager] {card.name} enviado ao GY. PreviousLocation: {fromLocation}, PreviousController: {tp}");

        int ocgReason = reason; // LUA NATIVO E PURO

        int rp = isOwnerPlayer ? 0 : 1; // Padrão: O próprio dono
        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) {
            rp = core.chainManager.resolvingLink.player; // Quem ativou o efeito resolvendo agora
            re = core.chainManager.resolvingLink.effect;
        } else if (core.luaDuel.currentActivatingEffect != null) {
            rp = core.luaDuel.currentActivatingEffect.owner.GetControler(); // Quem está ativando um custo
            re = core.luaDuel.currentActivatingEffect;
        }

        lc.currentReason = ocgReason;
        lc.reasonPlayer = rp;
        lc.reasonEffect = re;

        EventData edSingle = new EventData(lc, tp, 0, re, ocgReason, rp);
        var gyEffects = lc.registeredEffects.FindAll(e => e.code == 1014 && e.isTypeSingle); // EVENT_TO_GRAVE
        
        if (gyEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, gyEffects, edSingle));
        }

        LuaGroup eg = new LuaGroup();
        eg.AddCard(lc);
        EventData edGroup = new EventData(eg, tp, 0, re, ocgReason, rp);

        TriggerLuaEvent(1014, edGroup); 
        core.StartCoroutine(core.OpenFastEffectWindow($"Queda de {card.name}", 1014, edGroup));
    }

    public void OnDamageTaken(bool isPlayer, int amount) 
    { 
        int ep = isPlayer ? 0 : 1;
        int rp = 1 - ep; // Padrão
        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) {
            rp = core.chainManager.resolvingLink.player;
            re = core.chainManager.resolvingLink.effect;
        }
        EventData ed = new EventData(null, ep, amount, re, 0x40, rp); // REASON_EFFECT
        TriggerLuaEvent(1111, ed); // EVENT_DAMAGE
        core.StartCoroutine(core.OpenFastEffectWindow($"Dano Recebido ({amount})", 1111, ed));
    }

    public void OnLifePointsGained(bool isPlayer, int amount) 
    { 
        int ep = isPlayer ? 0 : 1;
        int rp = 1 - ep;
        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) {
            rp = core.chainManager.resolvingLink.player;
            re = core.chainManager.resolvingLink.effect;
        }
        EventData ed = new EventData(null, ep, amount, re, 0x40, rp); // REASON_EFFECT
        TriggerLuaEvent(1112, ed); // EVENT_RECOVER
        core.StartCoroutine(core.OpenFastEffectWindow($"Vida Restaurada ({amount})", 1112, ed));
    }

    public void OnCardBanished(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason)
    {
        if (card == null) return;
        LuaCard lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
        if (lc == null) lc = new LuaCard(card);
        lc.previousLocation = fromLocation;
        lc.ownerPlayerIndex = isOwnerPlayer ? 0 : 1;
        int tp = lc.GetControler();
        int rp = isOwnerPlayer ? 0 : 1;
        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) { rp = core.chainManager.resolvingLink.player; re = core.chainManager.resolvingLink.effect; }
        else if (core.luaDuel.currentActivatingEffect != null) { rp = core.luaDuel.currentActivatingEffect.owner.GetControler(); re = core.luaDuel.currentActivatingEffect; }
        lc.currentReason = reason; lc.reasonPlayer = rp; lc.reasonEffect = re;

        EventData edSingle = new EventData(lc, tp, 0, re, reason, rp);
        var effects = lc.registeredEffects.FindAll(e => e.code == 1011 && e.isTypeSingle); // EVENT_REMOVE (1011)
        if (effects.Count > 0) {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, effects, edSingle));
        }

        LuaGroup eg = new LuaGroup(); eg.AddCard(lc);
        EventData edGroup = new EventData(eg, tp, 0, re, reason, rp);
        TriggerLuaEvent(1011, edGroup); 
        core.StartCoroutine(core.OpenFastEffectWindow($"Banimento de {card.name}", 1011, edGroup, 0x100000));
    }

    public void OnCardReturnedToDeck(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason)
    {
        if (card == null) return;
        LuaCard lc = LuaScriptLoader.LoadScriptForData(card, core.luaEngine);
        if (lc == null) lc = new LuaCard(card);
        lc.previousLocation = fromLocation;
        lc.ownerPlayerIndex = isOwnerPlayer ? 0 : 1;
        int tp = lc.GetControler();
        int rp = isOwnerPlayer ? 0 : 1;
        LuaEffect re = null;
        if (core.chainManager != null && core.chainManager.resolvingLink != null) { rp = core.chainManager.resolvingLink.player; re = core.chainManager.resolvingLink.effect; }
        else if (core.luaDuel.currentActivatingEffect != null) { rp = core.luaDuel.currentActivatingEffect.owner.GetControler(); re = core.luaDuel.currentActivatingEffect; }
        lc.currentReason = reason; lc.reasonPlayer = rp; lc.reasonEffect = re;

        EventData edSingle = new EventData(lc, tp, 0, re, reason, rp);
        var effects = lc.registeredEffects.FindAll(e => e.code == 1013 && e.isTypeSingle); // EVENT_TO_DECK (1013)
        if (effects.Count > 0) {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, effects, edSingle));
        }

        LuaGroup eg = new LuaGroup(); eg.AddCard(lc);
        EventData edGroup = new EventData(eg, tp, 0, re, reason, rp);
        TriggerLuaEvent(1013, edGroup); 
        core.StartCoroutine(core.OpenFastEffectWindow($"Retorno ao Deck", 1013, edGroup, 0x400000));
    }
    
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayEquipEffect(equip, target);

        LuaCard eqLc = core.EnsureCardScriptLoaded(equip);
        LuaCard tgLc = core.EnsureCardScriptLoaded(target);
        if (eqLc != null && tgLc != null) {
            eqLc._lastEquipTarget = tgLc;
        }

        core.RecalculateStats(target);

        if (eqLc != null)
        {
            LuaGroup eg = new LuaGroup(); 
            eg.AddCard(eqLc);
            EventData ed = new EventData(eg, eqLc.GetControler(), 0, null, 0, eqLc.GetControler());
            TriggerLuaEvent(1121, ed); // EVENT_EQUIP
            core.StartCoroutine(core.OpenFastEffectWindow($"Equipamento de {equip.CurrentCardData.name}", 1121, ed, 0x2000000)); // TIMING_EQUIP
        }
    }
    
    public void OnSpellActivated(CardDisplay spell) { }

    public void OnCardLeavesField(CardDisplay card) {
        if (card == null || cardsLeavingField.Contains(card)) return;
        cardsLeavingField.Add(card);

        CardLink[] allLinks = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);

        // Remove efeitos contínuos
        LuaCard lc = core.activeLuaCards.ContainsKey(card) ? core.activeLuaCards[card] : null;
        if (lc == null) lc = new LuaCard(card);

        EventData ed = new EventData(lc, lc.GetControler(), 0, null, 0, lc.GetControler());

        // Dispara EVENT_LEAVE_FIELD_P (1019) ANTES da carta perder seus vínculos
        var leavePEffects = lc.registeredEffects.FindAll(e => e.code == 1019 && e.isTypeSingle); // EFFECT_TYPE_SINGLE
        if (leavePEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, leavePEffects, ed));
        }
        TriggerLuaEvent(1019, ed);

        // Dispara EVENT_LEAVE_FIELD (1015) ANTES da carta perder seus vínculos
        var leaveEffects = lc.registeredEffects.FindAll(e => e.code == 1015 && e.isTypeSingle); // EFFECT_TYPE_SINGLE
        if (leaveEffects.Count > 0)
        {
            core.StartCoroutine(ProcessSingleEffectsRoutine(lc, leaveEffects, ed));
        }
        TriggerLuaEvent(1015, ed);

        if (core.continuousFieldEffects.RemoveAll(e => e.owner == lc) > 0)
        {
            core.ApplyAllContinuousEffects();
        }

        foreach (var link in allLinks) if (link.source == card && link.type == CardLink.LinkType.Equipment && link.target != null) core.StartCoroutine(core.RecalculateStatsNextFrame(link.target));
        if (core.activeLuaCards.ContainsKey(card)) core.activeLuaCards.Remove(card);

        foreach (var link in allLinks) if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField) {
            core.StartCoroutine(DestroyEquipDelayed(link.source));
        }
        
        foreach (var link in allLinks) {
            if (link.source == card || link.target == card) {
                if (link != null && link.gameObject != null) GameObject.Destroy(link.gameObject);
            }
        }

        cardsLeavingField.Remove(card);
    }

    private IEnumerator DestroyEquipDelayed(CardDisplay equip)
    {
        yield return new WaitForSeconds(0.6f); // Atraso visível para separar a destruição da magia da destruição do monstro
        if (equip != null && equip.isOnField)
        {
            GameManager.Instance.MoveCard(equip, CardLocation.Graveyard, 0x400); // REASON_RULE
        }
    }
    
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { 
        if (target != null && target.CurrentCardData != null && (GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData))) {
            LuaCard deadCard = core.EnsureCardScriptLoaded(target);
            if (deadCard == null) deadCard = new LuaCard(target);

            EventData ed = new EventData(deadCard, deadCard.GetControler(), 0, null, 0x20, core.luaDuel.GetTurnPlayer()); // REASON_BATTLE = 0x20

            var singleEffects = deadCard.registeredEffects.FindAll(e => e.code == 1010 && (e.type & 0x0001) != 0); 
            if (singleEffects.Count > 0) {
                core.StartCoroutine(ProcessSingleEffectsRoutine(deadCard, singleEffects, ed));
            }
            TriggerLuaEvent(1010, ed);
        }
    }
}