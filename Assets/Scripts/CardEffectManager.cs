// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using System.Linq;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

    public LuaEngineCore engineCore;
    public LuaEventManager eventManager;
    
    public Script luaEngine => engineCore.luaEngine;
    public LuaDuel luaDuel => engineCore.luaDuel;

    // Variáveis de controle de Assincronicidade do Lua
    public DynValue activeLuaCoroutine = null;
    public bool isWaitingForLuaYield = false;
    public DynValue yieldReturnValue = null;
    public bool lastCoroutineSuccess = true;

    // --- SUBSISTEMAS LÓGICOS ---
    public ChainManager chainManager;
    
    // Propriedades Redirecionadas para retrocompatibilidade
    public List<ChainManager.ChainLink> currentChain => chainManager.currentChain;
    public bool isChainResolving => chainManager.isChainResolving;

    // Flags de estado global mantidas para compatibilidade com outros Managers
    public bool armoredGlassActive = false;
    public bool reverseStats = false;
    public bool invertDecks = false;
    public bool cannotSummonMonstersThisTurn = false;
    public bool trapsBlockedThisTurn = false;

    public Dictionary<CardDisplay, LuaCard> activeLuaCards = new Dictionary<CardDisplay, LuaCard>();

    // Closures seguras para preencher funções em branco e evitar "attempt to call a nil value"
    public Closure dummyClosureTrue => engineCore.dummyClosureTrue;

    // FASE 26: Properties for LuaAPI compatibility
    public UnityEngine.EventSystems.EventSystem eventSystem => UnityEngine.EventSystems.EventSystem.current;
    public CardDatabase cardDatabase => GameManager.Instance != null ? GameManager.Instance.cardDatabase : null;

    void Awake()
    {
        Instance = this;
        engineCore = new LuaEngineCore();
        engineCore.Initialize();
        chainManager = new ChainManager(this);
        eventManager = new LuaEventManager(this);
    }

    public LuaCard EnsureCardScriptLoaded(CardDisplay card)
    {
        return LuaScriptLoader.EnsureCardScriptLoaded(card, luaEngine, activeLuaCards);
    }

    public void ActivateCard(CardDisplay card, object triggerArgs, System.Action onComplete)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) 
        {
            onComplete?.Invoke();
            return;
        }

        // Procura por um efeito ativável instantâneo (Magia, Armadilha ou Ignition)
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            if (!CanActivateEffect(luaCard, activationEffect, tp, triggerArgs))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                onComplete?.Invoke();
                return; // Retorna sem chamar onComplete, abortando a ida para a Corrente
            }

            // Inicia a construção da corrente e a resolução LIFO
            StartCoroutine(chainManager.BuildAndResolveChainRoutine(luaCard, activationEffect, triggerArgs, tp, onComplete));
            
            // Toca um flash e um som se for monstro ativando efeito
            if (card.CurrentCardData.type.Contains("Monster") && DuelFXManager.Instance != null)
            {
                DuelFXManager.Instance.PlayMonsterEffect(card);
            }
        }
        else
        {
            onComplete?.Invoke(); // Não tem efeito manual, apenas deixa o jogo seguir
        }
    }

    public bool ExecuteCardEffect(CardDisplay card)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) return false;

        // Fallback Original: Procura por um efeito e o ativa instantaneamente (Pula a Corrente)
        // Útil para efeitos engatilhados pela IA ou sistemas que não passam pela interface manual.
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            
            // 1. Verificação Síncrona (Condition, Cost chk=0, Target chk=0)
            if (!CanActivateEffect(luaCard, activationEffect, tp, null))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                return false;
            }

            // 2. Injeta na Corrente Oficial
            StartCoroutine(chainManager.BuildAndResolveChainRoutine(luaCard, activationEffect, null, tp, null));
            
            // Toca um flash e um som se for monstro ativando efeito
            if (card.CurrentCardData.type.Contains("Monster") && DuelFXManager.Instance != null)
            {
                DuelFXManager.Instance.PlayMonsterEffect(card);
            }
            return true;
        }

        if (!GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} possui scripts carregados, mas não tem efeito ativável imediato.");
        return false;
    }

    private object WrapTriggerArgs(object triggerArgs)
    {
        if (triggerArgs == null) 
        {
            LuaGroup dummyGroup = new LuaGroup();
            dummyGroup.AddCard(new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }));
            return dummyGroup; 
        }
        if (triggerArgs is LuaCard card)
        {
            LuaGroup group = new LuaGroup();
            group.AddCard(card);
            return group;
        }
        else if (triggerArgs is List<CardDisplay> list)
        {
            LuaGroup group = new LuaGroup();
            foreach (var c in list) group.AddCard(new LuaCard(c));
            return group;
        }
        return triggerArgs;
    }

    public bool CanActivateEffect(LuaCard luaCard, LuaEffect effect, int tp, object triggerArgs)
    {
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)luaCard;

        // Cria um Dummy 're' (Reason Effect) para evitar crashes se a carta tentar ler propriedades da corrente
        LuaEffect dummyRe = new LuaEffect { owner = luaCard };

        try 
        {
            if (effect.conditionFunc != null) {
                DynValue res = luaEngine.Call(effect.conditionFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.costFunc != null) {
                DynValue res = luaEngine.Call(effect.costFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.targetFunc != null) {
                DynValue res = luaEngine.Call(effect.targetFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, null); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            return true;
        }
        catch (System.Exception ex)
        {
            if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                Debug.LogWarning($"[API LUA] Erro silencioso (seguro) ao verificar condições de {luaCard.unityData.name}: {ex.Message}");
            return false;
        }
    }

    public bool NegateChainLink(int chainIndex)
    {
        return chainManager.NegateChainLink(chainIndex);
    }

    public IEnumerator RunLuaCoroutine(Closure func, LuaEffect effect, int tp, object triggerArgs, int chk)
    {
        lastCoroutineSuccess = true;
        
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)(effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }));
        
        LuaEffect dummyRe = new LuaEffect { owner = effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }) };

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            // Assinatura YGOPro: (e, tp, eg, ep, ev, re, r, rp, chk)
            if (chk >= 0) result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(tp), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(tp), DynValue.NewNumber(chk));
            else result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(tp), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(tp));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] O efeito falhou graciosamente sem travar a engine: {e.Message}");
            lastCoroutineSuccess = false;
            yield break; // Aborta apenas este efeito, o jogo continua!
        }

        while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
        {
            while (isWaitingForLuaYield) yield return null;
            
            // Re-check state before resuming - coroutine may have completed during yield
            if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended)
                break;
            
            try
            {
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                lastCoroutineSuccess = false;
                yield break;
            }
        }
    }

    public IEnumerator RunGenericLuaCoroutine(Closure func, params object[] args)
    {
        // Captura o atacante e o alvo antes que o C# os limpe do cache global
        LuaCard storedAttacker = luaDuel.currentAttacker;
        LuaCard storedTarget = luaDuel.currentAttackTarget;

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            result = activeLuaCoroutine.Coroutine.Resume(args);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] Função Genérica falhou: {e.Message}");
            yield break; 
        }

        while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
        {
            if (result.Type == DataType.YieldRequest)
            {
                while (isWaitingForLuaYield) yield return null;
            }
            else if (result.Type == DataType.String)
            {
                string yieldCmd = result.String;
                if (yieldCmd == "WaitChain")
                {
                    yield return new WaitWhile(() => isChainResolving);
                }
                else if (yieldCmd == "PlayAttackAnimation")
                {
                    // Esconde a espada de "mira" exatamente na transição para a espada voadora
                    if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
                    
                    if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations)
                    {
                        bool animDone = false;
                        if (storedAttacker != null && storedAttacker.unityCard != null)
                        {
                            DuelFXManager.Instance.PlayAttack(storedAttacker.unityCard, storedTarget?.unityCard, () => { animDone = true; });
                            yield return new WaitUntil(() => animDone);
                        }
                    }
                }
                else if (yieldCmd == "RevealTarget")
                {
                    if (storedTarget != null && storedTarget.unityCard != null)
                    {
                        storedTarget.unityCard.RevealCard(true, true);
                        yield return new WaitForSeconds(0.8f); // Pausa pra ver o flip dramático
                    }
                }
            }
            
            if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended) break;
            
            try { result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue); }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                yield break;
            }
        }
        
        // Garante que a espada de mira suma se o ataque for abortado por uma Armadilha (ex: Mirror Force)
        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs) => eventManager.TriggerLuaEvent(eventCode, triggerArgs);

    public List<CardDisplay> GetValidResponses(int tp, ChainManager.ChainLink triggerLink)
    {
        List<CardDisplay> responses = new List<CardDisplay>();
        if (GameManager.Instance == null) return responses;
        
        Transform[] zones = tp == 0 ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
        foreach (var z in zones)
        {
            if (z.childCount > 0)
            {
                CardDisplay cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.isFlipped) // Apenas cartas Setadas 
                {
                    LuaCard lc = EnsureCardScriptLoaded(cd);
                    if (lc != null)
                    {
                        // Efeitos Rápidos (Quick-Play) ou Armadilhas (Activate / Quick_O)
                        LuaEffect eff = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0100 || e.type == 0x0080);
                        // A carta LUA recebe 'chk=0' para saber se pode se ativar em resposta àquele elo específico!
                        if (eff != null && CanActivateEffect(lc, eff, tp, triggerLink.card))
                        {
                            responses.Add(cd);
                        }
                    }
                }
            }
        }
        return responses;
    }

    private void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (z != null && z.childCount > 0)
            {
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) { 
        LuaCard lc = EnsureCardScriptLoaded(card);
        if (lc != null) {
            var singleEffects = lc.registeredEffects.FindAll(e => e.code == 11 && (e.type & 0x0001) != 0); // EFFECT_TYPE_SINGLE
            foreach(var e in singleEffects) 
            {
                if (CanActivateEffect(lc, e, lc.GetControler(), null))
                    StartCoroutine(chainManager.BuildAndResolveChainRoutine(lc, e, null, lc.GetControler(), null));
            }
        }
        TriggerLuaEvent(11, lc); // EVENT_SUMMON_SUCCESS
    }
    public void OnSet(CardDisplay card) { }
    public void OnBattlePositionChanged(CardDisplay card) { }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) { }
    public void OnCardDrawn(CardData card, bool isPlayer) { }
    public void OnSpecialSummon(CardDisplay card) { }
    public void OnControlSwitched(CardDisplay card) { }
    public void OnPhaseStart(GamePhase phase) { 
        if (phase == GamePhase.End) CleanAllExpiredModifiers(); 
        TriggerLuaEvent(4096, null); // EVENT_PHASE
    }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) { }
    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    public void OnCardEquipped(CardDisplay equip, CardDisplay target)
    {
        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayEquipEffect(equip, target);

        RecalculateStats(target);
    }
    public void OnSpellActivated(CardDisplay spell) { }

    // Métodos (Stubs) mantidos para não quebrar a lógica hardcoded do GameManager
    public void CheckMaintenanceCosts() { }
    public bool HasActiveSecondCoinToss(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeSecondCoinToss(bool isPlayer) { }
    public bool HasActiveDiceReRoll(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeDiceReRoll(bool isPlayer) { }

    public void OnCardLeavesField(CardDisplay card) => eventManager.OnCardLeavesField(card);

    public IEnumerator RecalculateStatsNextFrame(CardDisplay monster)
    {
        yield return null; // Aguarda 1 frame
        RecalculateStats(monster);
    }
    
    public void RecalculateStats(CardDisplay monster)
    {
        if (monster == null || monster.CurrentCardData == null || !monster.CurrentCardData.type.Contains("Monster")) return;

        int newAtk = monster.originalAtk;
        int newDef = monster.originalDef;

        // Encontra os equipamentos
        List<CardDisplay> equippedCards = GetEquippedCards(monster);

        foreach (var equipCard in equippedCards)
        {
            if (equipCard == null) continue;
            LuaCard luaEquip = EnsureCardScriptLoaded(equipCard);
            if (luaEquip == null) continue;

            var equipEffects = luaEquip.registeredEffects.FindAll(e => e.type == 0x4); // EFFECT_TYPE_EQUIP
            foreach (var effect in equipEffects)
            {
                try
                {
                    int value = 0;
                    object valObj = effect.GetValue();
                    if (valObj is double || valObj is long)
                    {
                        value = System.Convert.ToInt32(valObj);
                    }
                    else if (valObj is Closure valClosure)
                    {
                        DynValue result = luaEngine.Call(valClosure, effect, new LuaCard(monster));
                        if (result.Type == DataType.Number)
                        {
                            value = (int)result.Number;
                        }
                    }

                    if (effect.code == 100) // EFFECT_UPDATE_ATTACK
                    {
                        newAtk += value;
                    }
                    else if (effect.code == 104) // EFFECT_UPDATE_DEFENSE
                    {
                        newDef += value;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RecalculateStats] Erro ao aplicar o efeito de {equipCard.CurrentCardData.name}: {ex.Message}");
                }
            }
        }

        monster.currentAtk = newAtk;
        monster.currentDef = newDef;

        Debug.Log($"[RecalculateStats] Status de {monster.CurrentCardData.name} atualizado para ATK {newAtk} / DEF {newDef}");
    }

    // --- HOOKS DE BATALHA ---
    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) => eventManager.OnBattleEnd(attacker, target);
    public bool CanDeclareAttack(CardDisplay attacker) { return true; }
    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker) { return false; }
    public bool IsAttackRestricted(CardDisplay attacker) { return false; }

    // --- HELPERS E UTILITÁRIOS (A PONTE LUA VAI CHAMÁ-LOS) ---

    public void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    public void Effect_GainLP(CardDisplay source, int amount)
    {
        GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
    }

    public bool Effect_PayLP(CardDisplay source, int amount)
    {
        return GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
    }

    public List<CardDisplay> GetEquippedCards(CardDisplay target)
    {
        List<CardDisplay> equipped = new List<CardDisplay>();
        CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
                equipped.Add(link.source);
        return equipped;
    }

    public void CleanAllExpiredModifiers()
    {
        // O LUA Core já cuida da limpeza das Closures de Turno (RESET_PHASE + PHASE_END)
    }

    public void RollDice(int amount, System.Action<List<int>> onResult)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < amount; i++) results.Add(Random.Range(1, 7));
        onResult?.Invoke(results);
    }

    public void SetClockCounter(CardDisplay target, int turns)
    {
        if (target != null) target.turnCounter = turns; 
    }

    public void TickClock(CardDisplay card, System.Action onComplete)
    {
        if (card == null || card.turnCounter <= 0) { onComplete?.Invoke(); return; }
        int oldTurns = card.turnCounter;
        card.turnCounter--; 
        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null && GameManager.Instance.enableTurnClockVisuals)
            GameManager.Instance.turnClockUI.AnimateTick(card.CurrentCardData.name, oldTurns, card.turnCounter, card.maxTurnCounter, onComplete);
        else onComplete?.Invoke(); 
    }

    // =========================================================================
    // FERRAMENTA DE DEV: VALIDADOR DE SCRIPTS EM MASSA
    // =========================================================================
    [ContextMenu("DEV: Validar Todos os Scripts LUA")]
    public void DevValidateAllScripts()
    {
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) 
        {
            Debug.LogError("O jogo precisa estar rodando (Play) para executar a validação.");
            return;
        }
        
        int total = 0, success = 0, failed = 0;
        List<string> errorLogs = new List<string>();
        Debug.Log("<color=cyan>Iniciando Validação em Massa de LUA...</color>");
        
        foreach (var card in GameManager.Instance.cardDatabase.cardDatabase)
        {
            if (card.type.Contains("Normal") && !card.type.Contains("Effect")) continue;
            
            total++;
            GameObject dummyGO = new GameObject("DummyTester");
            dummyGO.AddComponent<UnityEngine.UI.RawImage>(); // Silencia o aviso visual
            CardDisplay dummy = dummyGO.AddComponent<CardDisplay>();
            dummy.gameObject.SetActive(false); // Mantém invisível
            dummy.SetCard(card, null, true);
            
            try
            {
                LuaCard lc = EnsureCardScriptLoaded(dummy);
                if (lc != null) 
                {
                    bool runtimeError = false;
                     // Evita o erro 'Collection was modified' fazendo uma cópia da lista
                    foreach (var eff in new List<LuaEffect>(lc.registeredEffects))
                     {
                        try {
                            object eg = WrapTriggerArgs(null);
                            LuaEffect dummyRe = new LuaEffect { owner = lc };
                            LuaCard dummyChkc = new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 });

                            // Efeitos de Ação/Gatilho (0x07F8 cobre do 0x0008 ao 0x0400: Activate, Flip, Ignition, Quick, Trigger_F, etc)
                            // Se for ação, envia o Jogador (tp). Se for contínuo/campo, envia a Carta (c).
                            bool isActionEffect = (eff.type & 0x07F8) != 0; 

                            System.Action<Closure, int> TestFunc = (func, chkArg) => {
                                if (func == null) return;
                                try {
                                    if (chkArg == -1) luaEngine.Call(func, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                    else if (chkArg == 0) luaEngine.Call(func, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                    else if (chkArg == 1) luaEngine.Call(func, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                                } catch {
                                    // Fallback: Tenta inverter o arg2 (tp vs lc) caso a assinatura da carta fuja do padrão esperado (ex: EFFECT_TYPE_FIELD exigindo c em vez de tp)
                                    if (chkArg == -1) luaEngine.Call(func, eff, !isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                    else if (chkArg == 0) luaEngine.Call(func, eff, !isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                    else if (chkArg == 1) luaEngine.Call(func, eff, !isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                                }
                            };

                            TestFunc(eff.conditionFunc, -1);
                            TestFunc(eff.costFunc, 0);
                            TestFunc(eff.targetFunc, 1);
                        } catch (System.Exception ex) {
                            errorLogs.Add($"<color=orange>[RUNTIME] Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
                            runtimeError = true;
                        }
                    }
                    if (runtimeError) failed++;
                    else success++;
                }
            }
            catch (System.Exception ex)
            {
                failed++;
                errorLogs.Add($"<color=red>Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
            }
            DestroyImmediate(dummy.gameObject);
        }
        Debug.Log($"<color=green>Validação Concluída! Total: {total} | Sucesso: {success} | Falhas: {failed}</color>");

        if (errorLogs.Count > 0)
        {
            Debug.LogWarning($"<color=yellow>--- RELATÓRIO DE ERROS ({errorLogs.Count}) ---</color>");
            foreach (var err in errorLogs)
            {
                Debug.LogError(err);
            }
        }
    }
}