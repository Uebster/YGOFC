// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using System.Linq;

[System.Flags]
public enum CardLocation { Hand = 1, Deck = 2, Field = 4, ExtraDeck = 8, Graveyard = 16, Banished = 32, Unknown = 64 }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public List<LuaEffect> continuousFieldEffects = new List<LuaEffect>();
    public enum TargetType { Monster, Spell, Trap, Any }

    public LuaEngineCore engineCore;
    public LuaEventManager eventManager;
    public GlobalAuraManager auraManager;
    
    public Script luaEngine => engineCore.luaEngine;
    public LuaDuel luaDuel => engineCore.luaDuel;

    // Variáveis de controle de Assincronicidade do Lua
    public DynValue activeLuaCoroutine = null;
    public bool isWaitingForLuaYield = false;
    public DynValue yieldReturnValue = null;
    public bool lastCoroutineSuccess = true;
    public bool isFastEffectWindowOpen = false;
    public bool isBusy => isChainResolving || isWaitingForLuaYield || isFastEffectWindowOpen || (eventManager != null && eventManager.isProcessingTriggers) || fastEffectQueueCount > 0 || (chainManager != null && chainManager.activeChainTasks > 0) || (GameManager.Instance != null && GameManager.Instance.pendingVisualTasks > 0);

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
    
    public class FastEffectRequest { public string name; public int eventCode; public object eventArg; public int timing; public bool missedTiming; }
    private Queue<FastEffectRequest> fastEffectQueue = new Queue<FastEffectRequest>();
    public int fastEffectQueueCount => fastEffectQueue.Count;

    void Awake()
    {
        Instance = this;
        engineCore = new LuaEngineCore();
        engineCore.Initialize();
        chainManager = new ChainManager(this);
        eventManager = new LuaEventManager(this);
        auraManager = gameObject.AddComponent<GlobalAuraManager>();
    }

    public LuaCard EnsureCardScriptLoaded(CardDisplay card)
    {
        return LuaScriptLoader.EnsureCardScriptLoaded(card, luaEngine, activeLuaCards);
    }

    public void ActivateCard(CardDisplay card, object triggerArgs, System.Action onComplete)
    {
        // HARDCODE EXCEÇÃO CARTA 7 (DM0004) - Sequência Cinemática de Jackpot
        if (card.CurrentCardData.name == "7" || card.CurrentCardData.id == "DM0004" || card.CurrentCardData.id == "0004")
        {
            int count7 = 0;
            List<CardDisplay> cards7 = new List<CardDisplay>();
            
            Transform[] zones = card.isPlayerCard ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            foreach (var z in zones) {
                if (z.childCount > 0) {
                    var cd = z.GetComponentInChildren<CardDisplay>();
                    if (cd != null && (cd.CurrentCardData.name == "7" || cd.CurrentCardData.id == "DM0004" || cd.CurrentCardData.id == "0004") && !cd.isFlipped) {
                        count7++; cards7.Add(cd);
                    }
                }
            }

            // O GameManager primeiro move a carta para a zona e a vira para cima, DEPOIS chama este método.
            // Isso significa que a carta ATUAL já foi contada no loop acima! O alvo é 3 cartas em campo.
            if (count7 >= 3)
            {
                if (!cards7.Contains(card)) cards7.Add(card); // Garantia extra caso a mecânica mude
                StartCoroutine(Jackpot7Routine(cards7, card.isPlayerCard));
                onComplete?.Invoke();
                return; // Ignora o LUA e executa a nossa corrotina visual!
            }
        }

        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) 
        {
            onComplete?.Invoke();
            return;
        }

        // Se a carta tem um efeito de campo, registra/desregistra e atualiza tudo
        if (luaCard.registeredEffects.Any(e => (e.type & 0x0002) != 0))
        {
            continuousFieldEffects.AddRange(luaCard.registeredEffects.Where(e => (e.type & 0x0002) != 0 && !continuousFieldEffects.Contains(e)));
            ApplyAllContinuousEffects();
        }

        // Tratamento Exclusivo para Boss Monsters com Procedimentos Especiais (Ex: Dark Necrofear)
        LuaEffect spSummonProc = luaCard.registeredEffects.Find(e => e.code == 34); // EFFECT_SPSUMMON_PROC
        if (spSummonProc != null && !card.isOnField)
        {
            int tp = luaCard.GetControler();
            if (spSummonProc.conditionFunc != null) {
                var res = luaEngine.Call(spSummonProc.conditionFunc, spSummonProc, luaCard);
                if (res.Type == DataType.Boolean && !res.Boolean) return; // Requisitos não atingidos
            }
            StartCoroutine(ResolveSpSummonProcRoutine(luaCard, spSummonProc, tp));
            return; // Termina aqui, a rotina faz o Special Summon!
        }

        // Procura por um efeito ativável instantâneo (Magia, Armadilha ou Ignition)
            LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.isTypeActivate || e.isTypeIgnition || e.isTypeTriggerO);

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

        // 0. Verifica SpSummonProc (One-Click)
        LuaEffect spSummonProc = luaCard.registeredEffects.Find(e => e.code == 34); // EFFECT_SPSUMMON_PROC
        if (spSummonProc != null && !card.isOnField)
        {
            int tp = luaCard.GetControler();
            if (spSummonProc.conditionFunc != null) {
                var res = luaEngine.Call(spSummonProc.conditionFunc, spSummonProc, luaCard);
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            StartCoroutine(ResolveSpSummonProcRoutine(luaCard, spSummonProc, tp));
            return true;
        }

        // Para cartas já ativas no campo (Face-up), procuramos APENAS por Ignition ou Trigger
        // Ignoramos o 0x0010 (ACTIVATE) para não reativar o efeito de "jogar a carta" de Magias Contínuas!
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.isTypeIgnition || e.isTypeTriggerO);

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
            return null; // OCGCore nativo exige que eventos sem alvo passem 'nil' em vez de um grupo falso com carta dummy.
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
        object finalEg = triggerArgs;
        int ep = tp;
        int ev = 0;
        LuaEffect re = null;
        int r = 0;
        int rp = tp;

        if (triggerArgs is EventData ed)
        {
            finalEg = ed.eg;
            ep = ed.ep;
            ev = ed.ev;
            re = ed.re;
            r = ed.r;
            rp = ed.rp;
        }
        else if (triggerArgs is LuaCard tCard)
        {
            ep = tCard.GetControler();
            rp = ep;
        }

        object eg = WrapTriggerArgs(finalEg);
        bool expectsCard = (effect.type == 1 || effect.type == 4); // EFFECT_TYPE_SINGLE or EFFECT_TYPE_EQUIP
        object arg2 = expectsCard ? (object)luaCard : (object)tp;

        if (re == null) re = new LuaEffect { owner = luaCard };

        if (!(triggerArgs is EventData) && eg is LuaGroup g && g.cards.Count > 0) 
        { 
            ep = g.cards[0].GetControler(); 
            rp = ep; 
        }

        // 0. Verifica Auras Globais de Bloqueio (Ex: Jinzo, Imperial Order)
        bool isManualActivation = effect.isTypeActivate || effect.isTypeIgnition || effect.isTypeTriggerO || effect.isTypeQuickO;
        if (isManualActivation && auraManager != null)
        {
            if (auraManager.IsUnderRestriction(luaCard, effect, "CANNOT_ACTIVATE", CardLocation.Hand | CardLocation.Field | CardLocation.Graveyard) ||
                auraManager.IsUnderRestriction(luaCard, effect, "CANNOT_TRIGGER", CardLocation.Hand | CardLocation.Field | CardLocation.Graveyard) ||
                (luaCard.IsOnField() && auraManager.IsUnderRestriction(luaCard, effect, "DISABLE", CardLocation.Field)))
            {
                // Debug.LogWarning($"<color=yellow>[Aura Lock]</color> A ativação de {luaCard.unityData.name} foi bloqueada por um Efeito Contínuo Global!");
                return false;
            }
        }

        // Validação de "Once per Turn" (CountLimit)
        if (effect.countLimitMax > 0)
        {
            if (effect.countLimitCode == 0)
            {
                if (effect.currentUsages >= effect.countLimitMax) 
                {
                    // Debug.LogWarning($"<color=orange>[OncePerTurn]</color> O efeito de {luaCard.unityData.name} atingiu o limite de usos ({effect.currentUsages}/{effect.countLimitMax}). Bloqueado!");
                    return false;
                }
            }
            else
            {
                string key = $"{tp}_{effect.countLimitCode}";
                if (luaDuel.hardOncePerTurnUsages.ContainsKey(key) && luaDuel.hardOncePerTurnUsages[key] >= effect.countLimitMax) 
                {
                    // Debug.LogWarning($"<color=orange>[OncePerTurn]</color> O efeito HARD de {luaCard.unityData.name} atingiu o limite. Bloqueado!");
                    return false;
                }
            }
        }

        // Validação de EFFECT_FLAG_OATH (Once per Duel)
        if (effect.isOath)
        {
            string oathKey = $"{tp}_{effect.code}_OATH";
            if (luaDuel.oathUsages.ContainsKey(oathKey) && luaDuel.oathUsages[oathKey] > 0)
            {
                // Debug.LogWarning($"<color=orange>[Oath]</color> O efeito de {luaCard.unityData.name} é OATH e já foi usado neste duelo!");
                return false;
            }
        }

        try 
        {
            if (effect.conditionFunc != null) {
                DynValue res = luaEngine.Call(effect.conditionFunc, effect, arg2, eg, DynValue.NewNumber(ep), DynValue.NewNumber(ev), re, DynValue.NewNumber(r), DynValue.NewNumber(rp));
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    Debug.LogWarning($"<color=yellow>[Lua Validation]</color> Condition falhou para a carta {luaCard.unityData.name}");
                    return false;
                }
            }
            if (effect.costFunc != null) {
                DynValue res = luaEngine.Call(effect.costFunc, effect, arg2, eg, DynValue.NewNumber(ep), DynValue.NewNumber(ev), re, DynValue.NewNumber(r), DynValue.NewNumber(rp), DynValue.NewNumber(0)); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    Debug.LogWarning($"<color=yellow>[Lua Validation]</color> Cost (chk=0) falhou para a carta {luaCard.unityData.name}");
                    return false;
                }
            }
            if (effect.targetFunc != null) {
                DynValue res = luaEngine.Call(effect.targetFunc, effect, arg2, eg, DynValue.NewNumber(ep), DynValue.NewNumber(ev), re, DynValue.NewNumber(r), DynValue.NewNumber(rp), 0, null); // chk = 0
                
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    // Debug.LogWarning($"<color=yellow>[Lua Validation]</color> Target (chk=0) falhou para a carta {luaCard.unityData.name}!");
                    return false;
                }
            }
            return true;
        }
        catch (System.Exception ex)
        {
            if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                Debug.LogWarning($"[API LUA] Erro silencioso (seguro) ao verificar condições de {luaCard.unityData.name}: {ex.Message}\nStack: {ex.StackTrace}");
            return false;
        }
    }

    public bool NegateChainLink(int chainIndex, bool isActivation = true)
    {
        return chainManager.NegateChainLink(chainIndex, isActivation);
    }

    public IEnumerator RunLuaCoroutine(Closure func, LuaEffect effect, int tp, object triggerArgs, int chk)
    {
        lastCoroutineSuccess = true;
        
        object finalEg = triggerArgs;
        int ep = tp;
        int ev = 0;
        LuaEffect re = null;
        int r = 0;
        int rp = tp;

        if (triggerArgs is EventData ed)
        {
            finalEg = ed.eg;
            ep = ed.ep;
            ev = ed.ev;
            re = ed.re;
            r = ed.r;
            rp = ed.rp;
        }
        else if (triggerArgs is LuaCard tCard)
        {
            ep = tCard.GetControler();
            rp = ep;
        }

        object eg = WrapTriggerArgs(finalEg);
        bool expectsCard = effect.isTypeSingle || effect.isTypeEquip;
        object arg2 = expectsCard ? (object)(effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" })) : (object)tp;
        
        if (re == null) re = new LuaEffect { owner = effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }) };

        if (!(triggerArgs is EventData) && eg is LuaGroup g && g.cards.Count > 0) 
        { 
            ep = g.cards[0].GetControler(); 
            rp = ep; 
        }

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            // Assinatura YGOPro: (e, tp, eg, ep, ev, re, r, rp, chk)
            if (chk >= 0) result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(ep), DynValue.NewNumber(ev), re, DynValue.NewNumber(r), DynValue.NewNumber(rp), DynValue.NewNumber(chk));
            else result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(ep), DynValue.NewNumber(ev), re, DynValue.NewNumber(r), DynValue.NewNumber(rp));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] O efeito falhou graciosamente sem travar a engine: {e.Message}");
            lastCoroutineSuccess = false;
            yield break; // Aborta apenas este efeito, o jogo continua!
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
                    yield return new WaitWhile(() => chainManager.activeChainTasks > 0 || eventManager.isProcessingTriggers);
                }
                else if (yieldCmd.StartsWith("FastEffectWindow"))
                {
                    int eventCode = 0;
                    int explicitTiming = 0;
                    
                    if (yieldCmd == "FastEffectWindow_DamageStep") explicitTiming = 0x2000;
                    else if (yieldCmd == "FastEffectWindow_DamageCal") explicitTiming = 0x4000;
                    else if (yieldCmd == "FastEffectWindow_BattleStepEnd") explicitTiming = 0x4000000;
                    else if (yieldCmd.Contains("_")) int.TryParse(yieldCmd.Split('_')[1], out eventCode);
                    
                    yield return StartCoroutine(OpenFastEffectWindow("Evento LUA", eventCode, null, explicitTiming));
                }
                else if (yieldCmd == "UI_Wait")
                {
                    while (isWaitingForLuaYield) yield return null;
                }
            }
            
            // Re-check state before resuming - coroutine may have completed during yield
            if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended)
                break;
            
            try
            {
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue ?? DynValue.Nil);
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
        LuaCard storedAttacker = luaDuel?.currentAttacker;
        LuaCard storedTarget = luaDuel?.currentAttackTarget;

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
                    yield return new WaitWhile(() => chainManager.activeChainTasks > 0 || eventManager.isProcessingTriggers);
                }
            else if (yieldCmd.StartsWith("FastEffectWindow"))
            {
                int eventCode = 0;
                int explicitTiming = 0;
                
                if (yieldCmd == "FastEffectWindow_DamageStep") explicitTiming = 0x2000; // TIMING_DAMAGE_STEP
                else if (yieldCmd == "FastEffectWindow_DamageCal") explicitTiming = 0x4000; // TIMING_DAMAGE_CAL
                else if (yieldCmd == "FastEffectWindow_BattleStepEnd") explicitTiming = 0x4000000; // TIMING_BATTLE_STEP_END
                else if (yieldCmd.Contains("_")) int.TryParse(yieldCmd.Split('_')[1], out eventCode);
                
                yield return StartCoroutine(OpenFastEffectWindow("Evento (Batalha)", eventCode, storedAttacker, explicitTiming));
            }
                else if (yieldCmd == "UI_Wait")
                {
                    while (isWaitingForLuaYield) yield return null;
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
            
            // Passa um valor seguro caso o yieldReturnValue tenha sido consumido
            try { result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue ?? DynValue.Nil); }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                yield break;
            }
        }
        
        // Garante que a espada de mira suma se o ataque for abortado por uma Armadilha (ex: Mirror Force)
        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();

        // Limpa a memória de ataque para evitar ataques infinitos e fantasmas
        if (luaDuel != null)
        {
            if (luaDuel.currentAttacker != null && luaDuel.currentAttacker.unityCard != null)
                luaDuel.currentAttacker.unityCard.SetAttackSelectionVisual(false);
            luaDuel.currentAttacker = null;
            luaDuel.currentAttackTarget = null;
        }
        if (GameManager.Instance != null) GameManager.Instance.RefreshAttackIndicators();
    }

    public IEnumerator ResolveSpSummonProcRoutine(LuaCard lc, LuaEffect eff, int tp)
    {
        isWaitingForLuaYield = false;
        
        // Passo 1: Executa a Seleção de Alvos/Custo (O Target do SpSummonProc)
        if (eff.targetFunc != null && eff.targetFunc != dummyClosureTrue)
        {
            activeLuaCoroutine = luaEngine.CreateCoroutine(eff.targetFunc);
            DynValue res;
            try { res = activeLuaCoroutine.Coroutine.Resume(eff, DynValue.NewNumber(tp), DynValue.Nil, DynValue.NewNumber(tp), DynValue.NewNumber(0), DynValue.Nil, DynValue.NewNumber(0), DynValue.NewNumber(tp), lc); }
            catch (System.Exception e) { Debug.LogError($"[SpSummonProc] Target Crash: {e.Message}"); yield break; }

            while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended) {
                while (isWaitingForLuaYield) yield return null;
                if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended) break;
                try { res = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue ?? DynValue.Nil); } catch { yield break; }
            }
        }

        // Passo 2: Paga o Custo Real (Ex: Banir as Cartas Selecionadas)
        if (eff.operationFunc != null && eff.operationFunc != dummyClosureTrue)
        {
            activeLuaCoroutine = luaEngine.CreateCoroutine(eff.operationFunc);
            DynValue res;
            try { res = activeLuaCoroutine.Coroutine.Resume(eff, DynValue.NewNumber(tp), DynValue.Nil, DynValue.NewNumber(tp), DynValue.NewNumber(0), DynValue.Nil, DynValue.NewNumber(0), DynValue.NewNumber(tp), lc); }
            catch (System.Exception e) { Debug.LogError($"[SpSummonProc] Op Crash: {e.Message}"); yield break; }

            while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended) {
                while (isWaitingForLuaYield) yield return null;
                if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended) break;
                try { res = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue ?? DynValue.Nil); } catch { yield break; }
            }
        }

        // Passo 3: Executa a Invocação Visual no Tabuleiro
        if (GameManager.Instance != null && lc.unityCard != null)
            GameManager.Instance.PerformSpecialSummon(lc.unityCard.gameObject, lc.unityData);
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs) => eventManager.TriggerLuaEvent(eventCode, triggerArgs);

    public List<CardDisplay> GetValidResponses(int tp, ChainManager.ChainLink triggerLink, int currentEventCode = 0, object currentEventArg = null, int currentTiming = 0, bool missedTiming = false)
    {
        List<CardDisplay> responses = new List<CardDisplay>();
        if (GameManager.Instance == null) return responses;
        
        object argsToPass = currentEventArg ?? triggerLink?.triggerArgs ?? triggerLink?.card;

        System.Action<CardDisplay, LuaCard> CheckAndAdd = (cd, lc) => {
            if (lc == null) return;

            // Regra de Traps e Quick-Plays: Não podem ser ativados no turno em que foram setados
            if (cd.isOnField && cd.isFlipped)
            {
                if (cd.CurrentCardData.type.Contains("Trap") && cd.summonedTurnCount == GameManager.Instance.turnCount) return;
                if (cd.CurrentCardData.property == "Quick-Play" && cd.summonedTurnCount == GameManager.Instance.turnCount) return;
            }

            foreach (var eff in lc.registeredEffects)
            {
                // Filtra para Efeitos Manuais (Ativação de S/T, Quick Effects e Trigger Opcionais)
                if (eff.isTypeActivate || eff.isTypeQuickO || eff.isTypeTriggerO) 
                {
                    // O Efeito deve reagir ao gatilho atual (ex: 1102) ou ser Corrente Livre (0 - EVENT_FREE_CHAIN)
                    if (eff.code == 0 || eff.code == currentEventCode)
                    {
                        if (eff.isTypeTriggerO)
                        {
                            if (missedTiming && !eff.delay)
                            {
                                continue; 
                            }
                        }

                        // --- ESCUDO DA DAMAGE STEP ---
                        bool inDamageStep = (currentTiming & 0x2000) != 0 || (currentTiming & 0x4000) != 0;
                        if (inDamageStep)
                        {
                            bool allowed = false;
                            if (eff.damageStep || eff.damageCal) allowed = true;
                            if ((currentTiming & 0x4000) != 0 && !eff.damageCal) allowed = false; // Em Damage Cal, exige a flag específica (0x8000)
                            if (cd.CurrentCardData.property == "Counter" && eff.isTypeActivate) allowed = true; // Counter Traps ignoram a restrição
                            if (eff.code == currentEventCode && currentEventCode != 0) allowed = true; // Gatilhos obrigatórios de Batalha passam
                            
                            if (!allowed) continue; // Bloqueado pela restrição da Damage Step!
                        }

                    if (eff.code == 0)
                    {
                        string cardType = cd.CurrentCardData.type ?? "";
                        string cardProperty = cd.CurrentCardData.property ?? "";
                        bool isSpellSpeed1 = cardType.Contains("Spell") && !cardProperty.Contains("Quick-Play") && !cardType.Contains("Monster");
                        if (isSpellSpeed1)
                            continue; // Pula esta carta, pois é uma Magia Normal.
                            
                        // FILTRO DEFINITIVO DE TIMING: (O Respeito Absoluto ao OCGCore)
                        int effTiming = (tp == luaDuel.GetTurnPlayer()) ? eff.hintTimingSelf : eff.hintTimingOpponent;
                        
                        // OCGCore UX: Restaura a restrição de HintTiming para cartas Free Chain (code == 0).
                        // Exceção de Ouro: Se a carta tiver uma Condição LUA Específica (Ex: A Deal with Dark Ruler), ela fura a fila!
                        bool hasCustomCondition = (eff.conditionFunc != null && eff.conditionFunc != dummyClosureTrue);

                        if (!hasCustomCondition && currentTiming > 0 && (effTiming & currentTiming) == 0) { continue; }
                    }

                        if (CanActivateEffect(lc, eff, tp, argsToPass))
                        {
                            if (!responses.Contains(cd)) responses.Add(cd);
                        }
                    }
                }
            }
        };

        // 1. Cartas Setadas (Spell/Trap Zones)
        Transform[] sZones = tp == 0 ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
        foreach (var z in sZones)
        {
            if (z.childCount > 0)
            {
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null && cd.isFlipped) CheckAndAdd(cd, EnsureCardScriptLoaded(cd));
            }
        }

        // 2. Monstros no Campo (Efeitos Rápidos / Quick Effects)
        Transform[] mZones = tp == 0 ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        foreach (var z in mZones)
        {
            if (z.childCount > 0)
            {
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null && !cd.isFlipped) CheckAndAdd(cd, EnsureCardScriptLoaded(cd));
            }
        }

        // 3. Cartas na Mão (Hand Traps, Kuriboh, ou Quick-Plays no próprio turno)
        List<GameObject> hand = tp == 0 ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
        bool isMyTurn = (tp == 0 && GameManager.Instance.isPlayerTurn) || (tp == 1 && !GameManager.Instance.isPlayerTurn);

        foreach (var go in hand)
        {
            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd != null)
            {
                LuaCard lc = EnsureCardScriptLoaded(cd);
                if (lc != null)
                {
                    // Quick Effects de Monstro na mão sempre funcionam
                    if (cd.CurrentCardData.type.Contains("Monster")) CheckAndAdd(cd, lc);
                    // Quick-Play Spells na mão SÓ podem ser ativados no próprio turno
                    else if (isMyTurn && cd.CurrentCardData.property == "Quick-Play") CheckAndAdd(cd, lc);
                }
            }
        }

        return responses;
    }

    // Abre uma janela de interrupção manualmente para cartas "Free Chain" (Armadilhas, Magias Rápidas)
    public IEnumerator OpenFastEffectWindow(string windowName, int eventCode = 0, object eventArg = null, int explicitTiming = 0)
    {
        int timing = explicitTiming;
        
        bool missed = false;
        if (chainManager != null && chainManager.isChainResolving)
        {
            var rLink = chainManager.resolvingLink;
            if (rLink != null && rLink.chainIndex > 1) missed = true;
        }
        
        // TRADUÇÃO AUTOMÁTICA DE EVENTOS DO TABULEIRO PARA OS SEUS RESPECTIVOS TIMINGS (OCGCore Translation)
        if (timing == 0)
        {
            switch (eventCode)
            {
                case 1100: timing = 0x40; break; // TIMING_SUMMON
                case 1101: timing = 0x100; break; // TIMING_FLIPSUMMON
                case 1102: timing = 0x80; break; // TIMING_SPSUMMON
                case 1107: timing = 0x200; break; // TIMING_MSET
                case 1108: timing = 0x400; break; // TIMING_SSET
                case 1016: timing = 0x800; break; // TIMING_POS_CHANGE
                case 1130: timing = 0x1000; break; // TIMING_ATTACK
                case 1138: timing = 0x8000000; break; // TIMING_BATTLED
                case 1121: timing = 0x2000000; break; // TIMING_EQUIP
                case 1026: timing = 0x8000; break; // TIMING_CHAIN_END
                case 1110: timing = 0x10000; break; // TIMING_DRAW
                case 1111: timing = 0x20000; break; // TIMING_DAMAGE
                case 1112: timing = 0x40000; break; // TIMING_RECOVER
                case 1010: timing = 0x80000; break; // TIMING_DESTROY
                case 1011: timing = 0x100000; break; // TIMING_REMOVE
                case 1012: timing = 0x200000; break; // TIMING_TOHAND
                case 1013: timing = 0x400000; break; // TIMING_TODECK
                case 1014: timing = 0x800000; break; // TIMING_TOGRAVE
            }
        }

        // Injeta TIMING_BATTLE_PHASE globalmente se estivermos na Fase de Batalha
        if (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle)
            timing |= 0x1000000;

        fastEffectQueue.Enqueue(new FastEffectRequest { name = windowName, eventCode = eventCode, eventArg = eventArg, timing = timing, missedTiming = missed });
        if (fastEffectQueue.Count > 1) yield break; // A rotina já está lidando com a fila

        while (fastEffectQueue.Count > 0)
        {
            var req = fastEffectQueue.Peek();
            
            yield return new WaitWhile(() => chainManager.activeChainTasks > 0 || eventManager.isProcessingTriggers);
            isFastEffectWindowOpen = true;
            
            // Aguarda a Unity limpar os GameObjects destruídos do tabuleiro para liberar espaço
            yield return new WaitForEndOfFrame();

            List<CardDisplay> pResponses = GetValidResponses(0, null, req.eventCode, req.eventArg, req.timing, req.missedTiming);
            List<CardDisplay> oResponses = GetValidResponses(1, null, req.eventCode, req.eventArg, req.timing, req.missedTiming);

            if (pResponses.Count > 0 || oResponses.Count > 0)
            {
                LuaCard dummyCard = new LuaCard(new CardData { id = "0000", name = $"[Evento] {req.name}", type = "Spell", property = "Normal" });
                LuaEffect dummyEff = new LuaEffect { owner = dummyCard, type = 0x0010, code = req.eventCode, conditionFunc = dummyClosureTrue, costFunc = dummyClosureTrue, targetFunc = dummyClosureTrue, operationFunc = dummyClosureTrue };
                yield return StartCoroutine(chainManager.BuildAndResolveChainRoutine(dummyCard, dummyEff, req.eventArg, 1, null, true));
            }
            
            isFastEffectWindowOpen = false;
            fastEffectQueue.Dequeue();
        }
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

    private IEnumerator Jackpot7Routine(List<CardDisplay> cards, bool isPlayer)
    {
        Debug.Log($"[Jackpot] Iniciando sequência para {(isPlayer ? "JOGADOR" : "OPONENTE")}.");
        Debug.Log("[Jackpot] Iniciando sequência especial da carta 7!");
        
        bool cinematicDone = false;

        // Auto-atribuição caso o painel não tenha sido arrastado para o Inspector do UIManager
        if (UIManager.Instance != null && UIManager.Instance.jackpot7UI == null)
        {
            UIManager.Instance.jackpot7UI = Resources.FindObjectsOfTypeAll<Jackpot7UI>().FirstOrDefault(x => x.gameObject.scene.IsValid());
        }

        if (UIManager.Instance != null && UIManager.Instance.jackpot7UI != null)
        {
            UIManager.Instance.jackpot7UI.ShowSequence(() => cinematicDone = true);
            yield return new WaitUntil(() => cinematicDone);
        }
        else
        {
            if (DuelFXManager.Instance != null) foreach(var c in cards) if (c != null) DuelFXManager.Instance.PlayCardActivation(c, false);
            yield return new WaitForSeconds(1.2f);
        }
        
        // 1. Destruir as cartas uma a uma (O Lua vai interceptar o GY e curar 700 automaticamente!)
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.SendToGraveyard(c.CurrentCardData, isPlayer, CardLocation.Field, 0x40); // REASON_EFFECT
                Destroy(c.gameObject);
                
                // A Cadência Perfeita: Espera o cemitério LUA processar e a vida terminar de rolar fisicamente no placar!
                yield return new WaitWhile(() => GameManager.Instance.pendingVisualTasks > 0 || eventManager.isProcessingTriggers);
                
                yield return new WaitForSeconds(0.15f); // Pequeno respiro de Game Feel antes da próxima explosão
            }
        }

        // 2. Após todas explodirem e curarem, iniciamos os saques manuais com bloqueio
        yield return new WaitForSeconds(0.5f);
        
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.canPlayerDrawFromDeck && !GameManager.Instance.isSimulating)
        {
            for (int i = 0; i < 3; i++)
            {
                int drawsLeft = 3 - i;
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"JACKPOT! Compre {drawsLeft} carta(s) do seu Deck.");
                
                GameManager.Instance.pendingEffectDraws = 1;
                yield return new WaitWhile(() => GameManager.Instance.pendingEffectDraws > 0);
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                if (isPlayer) GameManager.Instance.DrawCard(true);
                else GameManager.Instance.DrawOpponentCard();
                yield return new WaitForSeconds(0.4f);
            }
        }
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) => eventManager.OnSummon(card);
    public void OnSet(CardDisplay card) => eventManager.OnSet(card);
    public void OnFlipSummon(CardDisplay card) => eventManager.OnFlipSummon(card);
    public void OnBattlePositionChanged(CardDisplay card) => eventManager.OnBattlePositionChanged(card);
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) => eventManager.OnDamageDealt(attacker, target, amount);
    public void OnCounterTrapResolved(CardDisplay trap) => eventManager.OnCounterTrapResolved(trap);
    public void OnCardAddedToHand(CardDisplay card) => eventManager.OnCardAddedToHand(card);
    public void OnTribute(CardDisplay card) => eventManager.OnTribute(card);
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) => eventManager.OnCardDiscarded(card, causedByOpponent);
    public void OnCardDrawn(CardData card, bool isPlayer) => eventManager.OnCardDrawn(card, isPlayer);
    public void OnSpecialSummon(CardDisplay card) => eventManager.OnSpecialSummon(card);
    public void OnControlSwitched(CardDisplay card) => eventManager.OnControlSwitched(card);
    public void OnPhaseStart(GamePhase phase) => eventManager.OnPhaseStart(phase);
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) => eventManager.OnPreDrawPhase(isPlayerTurn, onContinue);
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason) => eventManager.OnCardSentToGraveyard(card, isOwnerPlayer, fromLocation, reason);
    public void OnDamageTaken(bool isPlayer, int amount) => eventManager.OnDamageTaken(isPlayer, amount);
    public void OnLifePointsGained(bool isPlayer, int amount) => eventManager.OnLifePointsGained(isPlayer, amount);
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) => eventManager.OnCardEquipped(equip, target);
    public void OnSpellActivated(CardDisplay spell) => eventManager.OnSpellActivated(spell);
    public void OnCardBanished(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason) => eventManager.OnCardBanished(card, isOwnerPlayer, fromLocation, reason);
    public void OnCardReturnedToDeck(CardData card, bool isOwnerPlayer, CardLocation fromLocation, int reason) => eventManager.OnCardReturnedToDeck(card, isOwnerPlayer, fromLocation, reason);

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

    public void ApplyAllContinuousEffects()
    {
        if (auraManager != null)
        {
            auraManager.ClearAllAuras(); // Limpa as auras velhas

            // Junta as auras Físicas e as auras Flutuantes (Fantasmas/Limbo)
            List<LuaEffect> activeContinuous = new List<LuaEffect>();
            foreach (var effect in continuousFieldEffects)
            {
                if (effect.owner == null || effect.owner.unityCard == null || !effect.owner.unityCard.isOnField || effect.owner.unityCard.isFlipped) continue;
                activeContinuous.Add(effect);
            }
            if (engineCore.luaDuel != null && engineCore.luaDuel.globalEffects != null)
            {
                activeContinuous.AddRange(engineCore.luaDuel.globalEffects);
            }

            // PASSO 1: Registra as Auras de Bloqueio (Floodgates) como Jinzo e Skill Drain primeiro!
            foreach (var effect in activeContinuous)
            {
                string modType = "";
                if (effect.code == 1) modType = "IMMUNE";
                else if (effect.code == 2) modType = "DISABLE";
                else if (effect.code == 3) modType = "CANNOT_DISABLE";
                else if (effect.code == 4) modType = "SET_CONTROL";
                else if (effect.code == 6) modType = "CANNOT_ACTIVATE";
                else if (effect.code == 7) modType = "CANNOT_TRIGGER";
                else if (effect.code == 8) modType = "DISABLE_EFFECT";
                else if (effect.code == 9) modType = "DISABLE_CHAIN";
                else if (effect.code == 10) modType = "DISABLE_TRAPMONSTER";
                else if (effect.code == 12) modType = "CANNOT_INACTIVATE";
                else if (effect.code == 13) modType = "CANNOT_DISEFFECT";
                else if (effect.code == 14 || effect.code == 87) modType = "CANNOT_CHANGE_POSITION";
                else if (effect.code == 18) modType = "MONSTER_SSET";
                else if (effect.code == 20) modType = "CANNOT_SUMMON";
                else if (effect.code == 21) modType = "CANNOT_FLIP_SUMMON";
                else if (effect.code == 22) modType = "CANNOT_SPECIAL_SUMMON";
                else if (effect.code == 23) modType = "CANNOT_MSET";
                else if (effect.code == 24) modType = "CANNOT_SSET";
                else if (effect.code == 25) modType = "CANNOT_DRAW";
                else if (effect.code == 26) modType = "CANNOT_DISABLE_SUMMON";
                else if (effect.code == 27) modType = "CANNOT_DISABLE_SPSUMMON";
                else if (effect.code == 28) modType = "SET_SUMMON_COUNT_LIMIT";
                else if (effect.code == 29) modType = "EXTRA_SUMMON_COUNT";
                else if (effect.code == 38) modType = "LIGHT_OF_INTERVENTION";
                else if (effect.code == 40) modType = "INDESTRUCTABLE";
                else if (effect.code == 41) modType = "INDESTRUCTABLE_EFFECT";
                else if (effect.code == 42) modType = "INDESTRUCTABLE_BATTLE";
                else if (effect.code == 43 || effect.code == 44 || effect.code == 46 || effect.code == 48) modType = "CANNOT_RELEASE";
                else if (effect.code == 65) modType = "CANNOT_TO_HAND";
                else if (effect.code == 66) modType = "CANNOT_TO_DECK";
                else if (effect.code == 67) modType = "CANNOT_REMOVE";
                else if (effect.code == 68) modType = "CANNOT_TO_GRAVE";
                else if (effect.code == 69) modType = "CANNOT_TURN_SET";
                else if (effect.code == 70) modType = "CANNOT_BE_BATTLE_TARGET";                else if (effect.code == 71) modType = "CANNOT_BE_EFFECT_TARGET";
                else if (effect.code == 72) modType = "IGNORE_BATTLE_TARGET";
                else if (effect.code == 73) modType = "CANNOT_DIRECT_ATTACK";
                else if (effect.code == 74) modType = "DIRECT_ATTACK";
                else if (effect.code == 85 || effect.code == 86) modType = "CANNOT_ATTACK";
                else if (effect.code == 183 || effect.code == 185) modType = "CANNOT_BP";
                else if (effect.code == 184 || effect.code == 186) modType = "CANNOT_M2";
                else if (effect.code == 190) modType = "DEFENSE_ATTACK";
                else if (effect.code == 191) modType = "MUST_ATTACK";
                else if (effect.code == 196) modType = "ONLY_BE_ATTACKED";
                else if (effect.code == 200) modType = "NO_BATTLE_DAMAGE";
                else if (effect.code == 201) modType = "AVOID_BATTLE_DAMAGE";
                else if (effect.code == 202) modType = "REFLECT_BATTLE_DAMAGE";
                else if (effect.code == 193) modType = "ATTACK_ALL";
                else if (effect.code == 194 || effect.code == 346) modType = "EXTRA_ATTACK";
                else if (effect.code == 260) modType = "DISABLE_FIELD";
                else if (effect.code == 270) modType = "HAND_LIMIT";
                // [FUTURO] Adicionar auras para EFFECT_UPDATE_RANK (132), CHANGE_RANK (133)
                // [FUTURO] Adicionar auras para PENDULUM (LSCALE 134, RSCALE 136)
                // [FUTURO] Adicionar auras para LINK (UPDATE_LINK 420, ADD_LINKMARKER 423)
                else if (effect.code == 334) modType = "ADD_SETCODE";
                else if (effect.code == 349) modType = "REMOVE_SETCODE";
                else if (effect.code == 350) modType = "CHANGE_SETCODE";
                else if (effect.code == 291) modType = "NECRO_VALLEY";
                else if (effect.code == 293) modType = "NECRO_VALLEY_IM";
                else if (effect.code == 400) modType = "CANNOT_LOSE_DECK";
                else if (effect.code == 401) modType = "CANNOT_LOSE_LP";                

                if (!string.IsNullOrEmpty(modType))
                {
                    var filterToUse = effect.targetFunc;
                    if (modType == "CANNOT_ACTIVATE")
                    {
                        filterToUse = null; // Será avaliado dinamicamente via GetValue() em IsUnderRestriction
                    }

                    // Debug.Log($"<color=green>[AuraManager LOG]</color> Barreira Invisível Registrada: {modType} | Fonte: {effect.owner.unityData.name} | Efeito LUA: {effect.code}");
                    auraManager.RegisterAura(effect.owner, effect, filterToUse, modType, 0, CardLocation.Hand | CardLocation.Field | CardLocation.Graveyard);
                }
            }

            // PASSO 2: Registra os modificadores de Status (ATK, DEF, LEVEL) respeitando as restrições do Passo 1!
            foreach (var effect in activeContinuous)
            {
                // OCGCore: Ignora a aplicação se a própria carta geradora estiver sob um efeito "DISABLE" (Ex: Jinzo silenciando uma Armadilha Contínua)
                if (effect.owner != null && effect.owner.IsOnField() && auraManager.IsUnderRestriction(effect.owner, effect, "DISABLE", CardLocation.Field)) continue;

                string modType = "";
                if (effect.code == 100 || effect.code == 101 || effect.code == 102) modType = "ATK";       
                else if (effect.code == 104 || effect.code == 105 || effect.code == 106) modType = "DEF";  
                else if (effect.code == 130 || effect.code == 131) modType = "LEVEL";
                else if (effect.code == 108) modType = "REVERSE_UPDATE";
                else if (effect.code == 109) modType = "SWAP_AD";
                else if (effect.code == 110) modType = "SWAP_BASE_AD";
                else if (effect.code == 113) modType = "ADD_CODE";
                else if (effect.code == 114) modType = "CHANGE_CODE";
                else if (effect.code == 115) modType = "ADD_TYPE";
                else if (effect.code == 116) modType = "REMOVE_TYPE";
                else if (effect.code == 117) modType = "CHANGE_TYPE";
                else if (effect.code == 118) modType = "REMOVE_CODE";
                else if (effect.code == 120) modType = "ADD_RACE";
                else if (effect.code == 121) modType = "REMOVE_RACE";
                else if (effect.code == 122) modType = "CHANGE_RACE";
                else if (effect.code == 125) modType = "ADD_ATTRIBUTE";
                else if (effect.code == 126) modType = "REMOVE_ATTRIBUTE";
                else if (effect.code == 127) modType = "CHANGE_ATTRIBUTE";
                else if (effect.code == 171) modType = "LPCOST_REPLACE";

                if (!string.IsNullOrEmpty(modType))
                {
                    int val = EvaluateEffectValue(effect, effect.owner, effect.owner);
                    
                    // Registra a aura passando a função de alvo (filter) original do script LUA
                    auraManager.RegisterAura(effect.owner, effect, effect.targetFunc, modType, val, CardLocation.Hand | CardLocation.Field);
                }
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null && auraManager != null)
        {
            GameManager.Instance.duelFieldUI.ClearAllBlocks();
            foreach (var aura in auraManager.GetActiveAuras())
            {
                if (aura.modifierType == "DISABLE_FIELD")
                {
                    int pIdx = aura.sourceCard != null ? aura.sourceCard.GetControler() : 0;
                    GameManager.Instance.duelFieldUI.ApplyDisableFieldMask(aura.value, pIdx);
                }
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshAllCardsVisuals();
        }
    }
    
    public void RecalculateStats(CardDisplay monster)
    {
        if (monster == null || monster.CurrentCardData == null || !monster.CurrentCardData.type.Contains("Monster")) return;

        // Delega para o sistema unificado do GameManager para recalcular TODOS os status a partir da base original.
        // Isso previne o bug gravíssimo de duplo acúmulo (double counting) de Equipamentos.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshAllCardsVisuals();
        }
    }

    public int EvaluateEffectValue(LuaEffect eff, LuaCard sourceCard, LuaCard targetCard)
    {
        int val = 0;
        object valObj = eff.GetValue();
        if (valObj is double || valObj is long) val = System.Convert.ToInt32(valObj);
        else if (valObj is MoonSharp.Interpreter.Closure valClosure)
        {
            try {
                var res = luaEngine.Call(valClosure, eff, targetCard);
                if (res.Type == MoonSharp.Interpreter.DataType.Number) val = (int)res.Number;
                else if (res.Type == MoonSharp.Interpreter.DataType.Boolean && res.Boolean) val = 1;
            } catch { }
        }
        return val;
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

    // --- NOVAS FUNÇÕES PREPARADAS PARA RECEBER O JOGADOR ALVO EXATO DO LUA ---
    public void Effect_DirectDamage(int targetPlayerIndex, int amount)
    {
        if (targetPlayerIndex == 0) GameManager.Instance.DamagePlayer(amount);
        else GameManager.Instance.DamageOpponent(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    public void Effect_GainLP(int targetPlayerIndex, int amount)
    {
        GameManager.Instance.GainLifePoints(targetPlayerIndex == 0, amount);
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

    public CardDisplay GetEquipTarget(CardDisplay equipSpell)
    {
        CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.source == equipSpell && link.type == CardLink.LinkType.Equipment && link.target != null)
                return link.target;
        return null;
    }

    public void CleanAllExpiredModifiers()
    {
        int removedCount = 0;
        // Limpeza de Efeitos Temporários registrados diretamente nas cartas (ex: Amazoness Spellcaster)
        foreach(var kvp in activeLuaCards)
        {
            if (kvp.Value != null && kvp.Value.registeredEffects != null)
            {
                // 0x40000000 = RESET_PHASE, 0x0200 = PHASE_END
                int removed = kvp.Value.registeredEffects.RemoveAll(e => {
                    bool shouldRemove = ((uint)e.GetReset() & 0x40000000) != 0; // Se o efeito é agendado para expirar em uma Fase!
                    // if (shouldRemove) Debug.Log($"[CleanAllExpiredModifiers] Removendo efeito {e.code} de {kvp.Value.unityData.name} (Reset Value: {e.GetReset()})");
                    return shouldRemove;
                });
                if (removed > 0) removedCount += removed;
            }
        }
        
        Debug.Log($"[CleanAllExpiredModifiers] Total de efeitos temporários expirados removidos: {removedCount}");

        // Força a UI a recalcular tudo agora que os bônus sumiram
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshAllCardsVisuals();
        }
    }

    public void CleanChainExpiredModifiers()
    {
        int removedCount = 0;
        // Limpeza de Efeitos Temporários registrados diretamente nas cartas (ex: "até o fim desta Corrente")
        foreach(var kvp in activeLuaCards)
        {
            if (kvp.Value != null && kvp.Value.registeredEffects != null)
            {
                // 0x80000000 = RESET_CHAIN
                int removed = kvp.Value.registeredEffects.RemoveAll(e => {
                    bool shouldRemove = ((uint)e.GetReset() & 0x80000000) != 0;
                    if (shouldRemove) Debug.Log($"[CleanChainExpiredModifiers] Removendo efeito {e.code} de {kvp.Value.unityData.name} (Reset Value: {e.GetReset()})");
                    return shouldRemove;
                });
                if (removed > 0) removedCount += removed;
            }
        }
        
        // Remove Efeitos Globais/Invisíveis que duram até o fim da corrente!
        if (luaDuel != null && luaDuel.globalEffects != null) {
            removedCount += luaDuel.globalEffects.RemoveAll(e => ((uint)e.GetReset() & 0x80000000) != 0);
        }
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
    
    public void PreloadScriptsForDecks(IEnumerable<CardData> pMain, IEnumerable<CardData> pExtra, IEnumerable<CardData> oMain, IEnumerable<CardData> oExtra)
    {
        HashSet<string> uniqueIds = new HashSet<string>();
        foreach (var c in pMain.Concat(pExtra).Concat(oMain).Concat(oExtra))
        {
            if (c != null && !uniqueIds.Contains(c.id))
            {
                uniqueIds.Add(c.id);
                LuaScriptLoader.LoadScriptForData(c, luaEngine);
            }
        }
        Debug.Log($"[API LUA] Preloaded scripts for {uniqueIds.Count} unique cards to register global effects.");
    }
}