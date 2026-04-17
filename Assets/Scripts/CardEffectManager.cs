// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using System.Linq;

[System.Flags]
public enum CardLocation { Hand = 1, Deck = 2, Field = 4, ExtraDeck = 8, Graveyard = 16, Banished = 32, Unknown = 64 }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public List<LuaEffect> continuousFieldEffects = new List<LuaEffect>();
    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

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
    
    public class FastEffectRequest { public string name; public int eventCode; public object eventArg; }
    private Queue<FastEffectRequest> fastEffectQueue = new Queue<FastEffectRequest>();

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
                    var cd = z.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && (cd.CurrentCardData.name == "7" || cd.CurrentCardData.id == "DM0004" || cd.CurrentCardData.id == "0004") && !cd.isFlipped) {
                        count7++; cards7.Add(cd);
                    }
                }
            }

            // A carta que está sendo ativada ainda não está no campo, então contamos ela também.
            // Se já existem 2 no campo, esta será a 3ª.
            if (count7 >= 2)
            {
                cards7.Add(card); // Adiciona a carta atual à lista para ser destruída
                StartCoroutine(Jackpot7Routine(cards7, card.isPlayerCard));                onComplete?.Invoke();
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
            // A ativação de uma Field Spell remove a anterior. OnCardLeavesField cuidará da remoção do efeito antigo.
            // Apenas precisamos garantir que a nova seja adicionada.
            continuousFieldEffects.AddRange(luaCard.registeredEffects.Where(e => (e.type & 0x0002) != 0 && !continuousFieldEffects.Contains(e)));
            ApplyAllContinuousEffects();
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
        bool expectsCard = (effect.type == 1 || effect.type == 4); // EFFECT_TYPE_SINGLE or EFFECT_TYPE_EQUIP
        object arg2 = expectsCard ? (object)luaCard : (object)tp;

        // Cria um Dummy 're' (Reason Effect) para evitar crashes se a carta tentar ler propriedades da corrente
        LuaEffect dummyRe = new LuaEffect { owner = luaCard };

        try 
        {
            if (effect.conditionFunc != null) {
                DynValue res = luaEngine.Call(effect.conditionFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    Debug.Log($"[Lua Validation] Condition falhou para {luaCard.unityData.name}");
                    return false;
                }
            }
            if (effect.costFunc != null) {
                DynValue res = luaEngine.Call(effect.costFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    Debug.Log($"[Lua Validation] Cost falhou para {luaCard.unityData.name}");
                    return false;
                }
            }
            if (effect.targetFunc != null) {
                DynValue res = luaEngine.Call(effect.targetFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, null); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) {
                    Debug.Log($"[Lua Validation] Target falhou para {luaCard.unityData.name}");
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

    public bool NegateChainLink(int chainIndex)
    {
        return chainManager.NegateChainLink(chainIndex);
    }

    public IEnumerator RunLuaCoroutine(Closure func, LuaEffect effect, int tp, object triggerArgs, int chk)
    {
        lastCoroutineSuccess = true;
        
        object eg = WrapTriggerArgs(triggerArgs);
        bool expectsCard = (effect.type == 1 || effect.type == 4); // EFFECT_TYPE_SINGLE or EFFECT_TYPE_EQUIP
        object arg2 = expectsCard ? (object)(effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" })) : (object)tp;
        
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
                    yield return new WaitWhile(() => isChainResolving);
                }
            else if (yieldCmd.StartsWith("FastEffectWindow"))
            {
                int eventCode = 0;
                if (yieldCmd.Contains("_")) int.TryParse(yieldCmd.Split('_')[1], out eventCode);
                yield return StartCoroutine(OpenFastEffectWindow("Evento (Batalha)", eventCode, storedAttacker));
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
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs) => eventManager.TriggerLuaEvent(eventCode, triggerArgs);

    public List<CardDisplay> GetValidResponses(int tp, ChainManager.ChainLink triggerLink, int currentEventCode = 0, object currentEventArg = null)
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
                if (eff.type == 0x0010 || eff.type == 0x0100 || eff.type == 0x0080) 
                {
                    // O Efeito deve reagir ao gatilho atual (ex: 1102) ou ser Corrente Livre (0 - EVENT_FREE_CHAIN)
                    if (eff.code == 0 || eff.code == currentEventCode)
                    {
                    // NOVO FIX: Se for um efeito de Corrente Livre (código 0), ele DEVE ser de uma
                    // carta com Velocidade de Magia 2 ou maior (Armadilhas, Magias Rápidas, Efeitos Rápidos de Monstros).
                    // Isso impede que Magias Normais (Velocidade 1) sejam sugeridas como resposta.
                    if (eff.code == 0)
                    {
                        string cardType = cd.CurrentCardData.type ?? "";
                        string cardProperty = cd.CurrentCardData.property ?? "";
                        bool isSpellSpeed1 = cardType.Contains("Spell") && !cardProperty.Contains("Quick-Play") && !cardType.Contains("Monster");
                        if (isSpellSpeed1)
                            continue; // Pula esta carta, pois é uma Magia Normal.
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
                CardDisplay cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.isFlipped) CheckAndAdd(cd, EnsureCardScriptLoaded(cd));
            }
        }

        // 2. Monstros no Campo (Efeitos Rápidos / Quick Effects)
        Transform[] mZones = tp == 0 ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        foreach (var z in mZones)
        {
            if (z.childCount > 0)
            {
                CardDisplay cd = z.GetChild(0).GetComponent<CardDisplay>();
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
    public IEnumerator OpenFastEffectWindow(string windowName, int eventCode = 0, object eventArg = null)
    {
        fastEffectQueue.Enqueue(new FastEffectRequest { name = windowName, eventCode = eventCode, eventArg = eventArg });
        if (fastEffectQueue.Count > 1) yield break; // A rotina já está lidando com a fila

        while (fastEffectQueue.Count > 0)
        {
            var req = fastEffectQueue.Peek();
            
            yield return new WaitWhile(() => isChainResolving || currentChain.Count > 0);
            isFastEffectWindowOpen = true;
            
            // Aguarda a Unity limpar os GameObjects destruídos do tabuleiro para liberar espaço
            yield return new WaitForEndOfFrame();

            List<CardDisplay> pResponses = GetValidResponses(0, null, req.eventCode, req.eventArg);
            List<CardDisplay> oResponses = GetValidResponses(1, null, req.eventCode, req.eventArg);

            if (pResponses.Count > 0 || oResponses.Count > 0)
            {
                LuaCard dummyCard = new LuaCard(new CardData { id = "0000", name = $"[Evento] {req.name}", type = "Spell", property = "Normal" });
                LuaEffect dummyEff = new LuaEffect { owner = dummyCard, type = 0x0010, conditionFunc = dummyClosureTrue, costFunc = dummyClosureTrue, targetFunc = dummyClosureTrue, operationFunc = dummyClosureTrue };
                yield return StartCoroutine(chainManager.BuildAndResolveChainRoutine(dummyCard, dummyEff, null, 1, null, true));
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
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
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
                GameManager.Instance.SendToGraveyard(c.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Effect);
                Destroy(c.gameObject);
                yield return new WaitForSeconds(0.8f); // Tempo da cura subir na tela
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
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) => eventManager.OnCardSentToGraveyard(card, isOwnerPlayer, fromLocation, reason);
    public void OnDamageTaken(bool isPlayer, int amount) => eventManager.OnDamageTaken(isPlayer, amount);
    public void OnLifePointsGained(bool isPlayer, int amount) => eventManager.OnLifePointsGained(isPlayer, amount);
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) => eventManager.OnCardEquipped(equip, target);
    public void OnSpellActivated(CardDisplay spell) => eventManager.OnSpellActivated(spell);

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

            foreach (var effect in continuousFieldEffects)
            {
                // Só aplica o Aura se a carta geradora estiver ativa no campo e virada para cima!
                if (effect.owner == null || effect.owner.unityCard == null || !effect.owner.unityCard.isOnField || effect.owner.unityCard.isFlipped) continue;

                string modType = "";
                if (effect.code == 1 || effect.code == 100) modType = "ATK";       // EFFECT_UPDATE_ATTACK
                else if (effect.code == 4 || effect.code == 104) modType = "DEF";  // EFFECT_UPDATE_DEFENSE
                else if (effect.code == 10) modType = "LEVEL";                     // EFFECT_UPDATE_LEVEL

                if (!string.IsNullOrEmpty(modType))
                {
                    int val = 0;
                    object valObj = effect.GetValue();
                    if (valObj is double || valObj is long) val = System.Convert.ToInt32(valObj);
                    
                    // Registra a aura passando a função de alvo (filter) original do script LUA
                    auraManager.RegisterAura(effect.owner, effect, effect.targetFunc, modType, val, CardLocation.Hand | CardLocation.Field);
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

        // Os stats de base agora são os current, que já foram modificados pelos efeitos de campo
        int newAtk = monster.currentAtk;
        int newDef = monster.currentDef;

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

                    // OCGCore usa 1 para UPDATE_ATTACK e 4 para UPDATE_DEFENSE
                    if (effect.code == 1 || effect.code == 100) 
                    {
                        newAtk += value;
                    }
                    else if (effect.code == 4 || effect.code == 104) 
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

    private int GetEffectValue(LuaEffect effect, CardDisplay target)
    {
        int value = 0;
        object valObj = effect.GetValue();
        if (valObj is double || valObj is long)
        {
            value = System.Convert.ToInt32(valObj);
        }
        else if (valObj is Closure valClosure)
        {
            DynValue result = luaEngine.Call(valClosure, effect, new LuaCard(target));
            if (result.Type == DataType.Number)
            {
                value = (int)result.Number;
            }
        }
        return value;
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

                            bool expectsCard = (eff.type == 1 || eff.type == 4); // EFFECT_TYPE_SINGLE or EFFECT_TYPE_EQUIP
                            object arg2 = expectsCard ? (object)lc : (object)lc.GetControler();

                            System.Action<Closure, int> TestFunc = (func, chkArg) => {
                                if (func == null) return;
                                try {
                                    if (chkArg == -1) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                    else if (chkArg == 0) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                    else if (chkArg == 1) luaEngine.Call(func, eff, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                                } catch {
                                    // Fallback: Tenta inverter o arg2 (tp vs lc) caso a assinatura da carta fuja do padrão esperado (ex: EFFECT_TYPE_FIELD exigindo c em vez de tp)
                                    object fallbackArg2 = expectsCard ? (object)lc.GetControler() : (object)lc;
                                    if (chkArg == -1) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                                    else if (chkArg == 0) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                                    else if (chkArg == 1) luaEngine.Call(func, eff, fallbackArg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
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