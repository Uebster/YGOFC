// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

    public Script luaEngine;

    // Variáveis de controle de Assincronicidade do Lua
    public DynValue activeLuaCoroutine = null;
    public bool isWaitingForLuaYield = false;
    public DynValue yieldReturnValue = null;

    // Flags de estado global mantidas para compatibilidade com outros Managers
    public bool armoredGlassActive = false;
    public bool reverseStats = false;
    public bool invertDecks = false;
    public bool cannotSummonMonstersThisTurn = false;
    public bool trapsBlockedThisTurn = false;

    void Awake()
    {
        Instance = this;
        InitializeLuaEngine();
    }

    private void InitializeLuaEngine()
    {
        // 1. Instancia o interpretador MoonSharp
        luaEngine = new Script();

        // 2. Registra nossas Classes C# para serem expostas ao Lua
        UserData.RegisterType<LuaDuel>();
        UserData.RegisterType<LuaCard>();
        UserData.RegisterType<LuaEffect>();
        UserData.RegisterType<LuaGroup>();

        // 3. Injeta as instâncias globais na memória do Lua
        luaEngine.Globals["Duel"] = new LuaDuel();
        luaEngine.Globals["Effect"] = typeof(LuaEffect); // Permite chamar Effect.CreateEffect(c)
        luaEngine.Globals["Card"] = typeof(LuaCard);
        luaEngine.Globals["Group"] = typeof(LuaGroup);

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Tabela Auxiliar Básica
        DynValue auxTable = DynValue.NewTable(luaEngine);
        auxTable.Table.Set("Stringid", DynValue.FromObject(luaEngine, (System.Func<int, int, string>)((code, id) => $"{code}_{id}")));
        auxTable.Table.Set("GlobalCheck", DynValue.FromObject(luaEngine, (System.Action<object, Closure>)((s, func) => { })));
        auxTable.Table.Set("EnableSpiritReturn", DynValue.FromObject(luaEngine, (System.Action<object, int, int>)((c, e1, e2) => { })));

        luaEngine.Globals["aux"] = auxTable;

        // 5. Injeção de Constantes Vitais (Evita os erros de Bitwise do Lua 5.2)
        InjectVitalConstants();

        Debug.Log("[MoonSharp] Motor LUA Inicializado e pronto para interpretar OCGCore!");
    }

    private void InjectVitalConstants()
    {
        // Para começar a testar feitiços e armadilhas (Dano, Cura, Destruição),
        // injetamos as constantes diretamente no ambiente Lua.
        luaEngine.Globals["LOCATION_MZONE"] = 0x4;
        luaEngine.Globals["LOCATION_SZONE"] = 0x8;
        luaEngine.Globals["LOCATION_GRAVE"] = 0x10;
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8; // Resolvido no C#!
        luaEngine.Globals["LOCATION_HAND"] = 0x2;
        luaEngine.Globals["LOCATION_DECK"] = 0x1;
        
        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x01;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x02;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x04;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x08;
        
        luaEngine.Globals["POS_FACEUP"] = 0x1 | 0x4;
        luaEngine.Globals["POS_FACEUP_ATTACK"] = 0x1;
        luaEngine.Globals["POS_FACEDOWN_DEFENSE"] = 0x8;
        luaEngine.Globals["POS_FACEUP_DEFENSE"] = 0x4;
        luaEngine.Globals["POS_DEFENSE"] = 0x4 | 0x8;
        
        luaEngine.Globals["REASON_EFFECT"] = 0x40;
        luaEngine.Globals["EVENT_FREE_CHAIN"] = 1002;
        luaEngine.Globals["CATEGORY_DAMAGE"] = 0x80000;
        luaEngine.Globals["CATEGORY_RECOVER"] = 0x100000;
        luaEngine.Globals["CATEGORY_DESTROY"] = 0x20000;
        luaEngine.Globals["REASON_COST"] = 0x2;
        luaEngine.Globals["REASON_BATTLE"] = 0x10;

        // Tipos de Efeitos (EFFECT_TYPE)
        luaEngine.Globals["EFFECT_TYPE_SINGLE"] = 0x0001;
        luaEngine.Globals["EFFECT_TYPE_FIELD"] = 0x0002;
        luaEngine.Globals["EFFECT_TYPE_EQUIP"] = 0x0004;
        luaEngine.Globals["EFFECT_TYPE_ACTIVATE"] = 0x0010;
        luaEngine.Globals["EFFECT_TYPE_IGNITION"] = 0x0020;
        luaEngine.Globals["EFFECT_TYPE_TRIGGER_O"] = 0x0080;
        
        // Cadeia e Alvos
        luaEngine.Globals["CHAININFO_TARGET_PLAYER"] = 1;
        luaEngine.Globals["CHAININFO_TARGET_PARAM"] = 2;
        
        luaEngine.Globals["HINT_SELECTMSG"] = 1;
        luaEngine.Globals["HINTMSG_DESTROY"] = 506;
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 513;
        luaEngine.Globals["HINTMSG_RTOHAND"] = 510;
        luaEngine.Globals["HINTMSG_TARGET"] = 514;
        luaEngine.Globals["HINTMSG_EQUIP"] = 515;
    }

    public bool ExecuteCardEffect(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return false;
        
        string cardId = card.CurrentCardData.id;
        Debug.Log($"[API] Preparando para executar script LUA para: {card.CurrentCardData.name} ({cardId})");
        
        // Caminho físico do arquivo LUA gerado pelo seu Downloader
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        
        if (!System.IO.File.Exists(scriptPath))
        {
            Debug.LogWarning($"[API LUA] Arquivo não encontrado em: {scriptPath}. A carta não possui efeito registrado.");
            return false;
        }

        try
        {
            // 1. Inicializa o estado local da carta no LUA (GetID)
            DynValue selfTable = DynValue.NewTable(luaEngine);
            int numericId = 0;
            int.TryParse(cardId, out numericId);
            
            luaEngine.Globals["self_table"] = selfTable;
            luaEngine.Globals["self_code"] = numericId;

            // 2. Lê e roda o arquivo .lua na memória
            string scriptCode = System.IO.File.ReadAllText(scriptPath);
            luaEngine.DoString(scriptCode);

            // 3. Puxa a função de fábrica 'initial_effect' e registra os efeitos
            DynValue initialEffect = selfTable.Table.Get("initial_effect");
            if (initialEffect.IsNil())
            {
                Debug.LogError($"[API LUA] Função 'initial_effect' não encontrada no arquivo c{cardId}.lua");
                return false;
            }

            LuaCard luaCard = new LuaCard(card);
            luaEngine.Call(initialEffect, luaCard);

            // 4. Execução do Efeito (Emulando a Chain)
            // Procura por um efeito ativável instantâneo (Magias/Armadilhas/Ignition)
            LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0020);

            if (activationEffect != null)
            {
                StartCoroutine(RunLuaEffectRoutine(luaCard, activationEffect));
                return true;
            }
            else
            {
                Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} possui scripts carregados, mas não tem efeito ativável imediato (Apenas efeitos passivos).");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA CRASH] c{cardId}.lua falhou de forma catastrófica:\n{ex.Message}\n{ex.StackTrace}");
        }
        
        return false;
    }

    private IEnumerator RunLuaEffectRoutine(LuaCard luaCard, LuaEffect activationEffect)
    {
        int tp = luaCard.GetControler();

        if (activationEffect.targetFunc != null)
        {
            activeLuaCoroutine = luaEngine.CreateCoroutine(activationEffect.targetFunc);
            DynValue result = activeLuaCoroutine.Coroutine.Resume(activationEffect, tp, null, 0, 0, null, 0, 0, 1);
            
            while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
            {
                while (isWaitingForLuaYield) yield return null;
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue);
            }
        }

        if (activationEffect.operationFunc != null)
        {
            activeLuaCoroutine = luaEngine.CreateCoroutine(activationEffect.operationFunc);
            DynValue result = activeLuaCoroutine.Coroutine.Resume(activationEffect, tp, null, 0, 0, null, 0, 0);
            
            while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
            {
                while (isWaitingForLuaYield) yield return null;
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue);
            }
        }
        
        Debug.Log($"[API LUA] Rotina de Efeito concluída com sucesso: {luaCard.unityData.name}");
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) { }
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
    public void OnPhaseStart(GamePhase phase) { if (phase == GamePhase.End) CleanAllExpiredModifiers(); }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) { }
    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    public void OnCardEquipped(CardDisplay equip, CardDisplay target) { }
    public void OnSpellActivated(CardDisplay spell) { }
    
    // Métodos (Stubs) mantidos para não quebrar a lógica hardcoded do GameManager
    public void CheckMaintenanceCosts() { }
    public bool HasActiveSecondCoinToss(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeSecondCoinToss(bool isPlayer) { }
    public bool HasActiveDiceReRoll(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeDiceReRoll(bool isPlayer) { }

    public void OnCardLeavesField(CardDisplay card)
    {
        // Limpeza Genérica Obrigatória (Isso continua sendo C# nativo para não bugar a engine)
        if (GameManager.Instance.duelFieldUI != null)
        {
            List<Transform> allZones = new List<Transform>();
            allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
            allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);
            foreach (var zone in allZones)
            {
                if (zone.childCount > 0)
                {
                    CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (target != null) target.RemoveModifiersFromSource(card);
                }
            }
        }

        // Destrói equipamentos físicos que dependem desta carta
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField)
            {
                GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
                Destroy(link.source.gameObject);
            }
        }

        // Libera zonas bloqueadas por cartas contínuas
        if (blockedZonesByCard.ContainsKey(card))
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach (var z in blockedZonesByCard[card]) GameManager.Instance.duelFieldUI.UnblockZone(z);
            }
            blockedZonesByCard.Remove(card);
        }
    }

    // --- HOOKS DE BATALHA ---
    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { }
    public bool CanDeclareAttack(CardDisplay attacker) { return true; }
    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker) { return false; }
    public bool IsAttackRestricted(CardDisplay attacker) { return false; }

    // --- REGRAS CONTÍNUAS GLOBAIS ---
    public bool IsSummonRestricted(bool isSpecialSummon) { return false; }
    public bool ShouldBanishInsteadOfGraveyard() { return false; }
    public string GetEffectiveRace(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryRace) ? card.temporaryRace : (card.isTrapMonster ? card.trapMonsterRace : card.CurrentCardData.race); }
    public string GetEffectiveAttribute(CardDisplay card) { return !string.IsNullOrEmpty(card.temporaryAttribute) ? card.temporaryAttribute : (card.isTrapMonster ? card.trapMonsterAttribute : card.CurrentCardData.attribute); }
    public bool HasAttribute(CardDisplay card, string attribute) { return GetEffectiveAttribute(card).Equals(attribute, System.StringComparison.OrdinalIgnoreCase); }
    public bool CheckChainEnergy(bool isPlayer) { return true; }
    public bool CheckSpatialCollapse(bool isPlayer) { return true; }
    public bool CheckRivalryOfWarlords(bool isPlayer, string newRace) { return true; }
    public bool IsFusionSubstitute(string cardIdOrName) { return false; }

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
        CardLink[] links = Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
                equipped.Add(link.source);
        return equipped;
    }

    public void CleanAllExpiredModifiers()
    {
        if (GameManager.Instance.duelFieldUI == null) return;
        List<Transform> allZones = new List<Transform>();
        allZones.AddRange(GameManager.Instance.duelFieldUI.playerMonsterZones);
        allZones.AddRange(GameManager.Instance.duelFieldUI.opponentMonsterZones);

        foreach (var zone in allZones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) cd.CleanExpiredModifiers();
            }
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
}