using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// 1. CLASSE DUEL (Ações Globais e Tabuleiro)
// Chamado no Lua como: Duel.Damage(tp, 500, REASON_EFFECT)
// ==============================================================================
[MoonSharpUserData]
public class LuaDuel
{
    public int targetPlayer;
    public int targetParam;
    public LuaGroup currentTargetGroup;
    
    // FASE 15: Chain info variables
    public int chainId = 0;
    public int chainCount = 0;
    public int triggeringPlayer = 0;
    public LuaEffect currentEffect;
    public LuaCard currentTriggeringCard;
    public int chainOperation = 0;
    public bool chainSolving = false;

    private int ConvertToInt(object obj)
    {
        if (obj == null) return 0;
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        return 0;
    }

    // Utilitário de Conversão: 0 = Human Player, 1 = AI Opponent
    public bool IsPlayer(object playerIndex)
    {
        return ConvertToInt(playerIndex) == 0;
    }

    public void Damage(object player, object amount, object reason)
    {
        if (IsPlayer(player)) GameManager.Instance.DamagePlayer(ConvertToInt(amount));
        else GameManager.Instance.DamageOpponent(ConvertToInt(amount));
    }

    public void Recover(object player, object amount, object reason)
    {
        if (IsPlayer(player)) GameManager.Instance.GainLifePoints(true, ConvertToInt(amount));
        else GameManager.Instance.GainLifePoints(false, ConvertToInt(amount));
    }

    public void Destroy(object target, object reason)
    {
        if (target is LuaGroup group)
        {
            List<CardDisplay> toDestroy = new List<CardDisplay>();
            foreach (var c in group.cards) if (c.unityCard != null) toDestroy.Add(c.unityCard);
            
            GameManager.Instance.StartCoroutine(DestroyCardsRoutine(toDestroy));
            Debug.Log($"[Lua] Duel.Destroy(Grupo com {group.cards.Count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.StartCoroutine(DestroyCardsRoutine(new List<CardDisplay> { card.unityCard }));
            Debug.Log($"[Lua] Duel.Destroy({card.unityCard.CurrentCardData.name})");
        }
    }

    // --- MOVIMENTAÇÃO DE CARTAS ---

    public int Draw(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);
        for (int i = 0; i < amt; i++)
        {
            // Passamos true para 'ignoreLimit' para que efeitos comprem livremente fora da Draw Phase
            if (isPlayer) GameManager.Instance.DrawCard(true);
            else GameManager.Instance.DrawOpponentCard();
        }
        return amt;
    }

    public bool IsPlayerCanDraw(object player, object amount = null)
    {
        int amt = amount == null ? 1 : ConvertToInt(amount);
        int deckCount = IsPlayer(player) ? GameManager.Instance.GetPlayerMainDeck().Count : GameManager.Instance.GetOpponentMainDeck().Count;
        return deckCount >= (amt > 0 ? amt : 1);
    }

    public int SendtoGrave(object target, object reason)
    {
        int count = 0;
        SendReason sendReason = SendReason.Effect; // Default
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    GameManager.Instance.MoveCard(c.unityCard, CardLocation.Graveyard, sendReason);
                    count++;
                }
            }
            Debug.Log($"[Lua] Duel.SendtoGrave(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.MoveCard(card.unityCard, CardLocation.Graveyard, sendReason);
            count = 1;
            Debug.Log($"[Lua] Duel.SendtoGrave({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    public int Remove(object target, object pos, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null)
                {
                    GameManager.Instance.MoveCard(c.unityCard, CardLocation.Banished, SendReason.Effect);
                    count++;
                }
            Debug.Log($"[Lua] Duel.Remove(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.MoveCard(card.unityCard, CardLocation.Banished, SendReason.Effect);
            count = 1;
            Debug.Log($"[Lua] Duel.Remove({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    public int SendtoDeck(object target, object player, object seq, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null) 
                { 
                    GameManager.Instance.MoveCard(c.unityCard, CardLocation.Deck, SendReason.Effect);
                    count++; 
                }
            Debug.Log($"[Lua] Duel.SendtoDeck(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.MoveCard(card.unityCard, CardLocation.Deck, SendReason.Effect);
            count = 1;
            Debug.Log($"[Lua] Duel.SendtoDeck({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    public int DiscardHand(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);
        GameManager.Instance.DiscardRandomHand(isPlayer, amt, false);
        return amt;
    }

    public void ShuffleHand(object player)
    {
        bool isPlayer = IsPlayer(player);
        
        if (isPlayer)
        {
            // Shuffle player's hand using Fisher-Yates
            if (GameManager.Instance.playerHand != null && GameManager.Instance.playerHand.Count > 0)
            {
                for (int i = GameManager.Instance.playerHand.Count - 1; i > 0; i--)
                {
                    int randomIndex = UnityEngine.Random.Range(0, i + 1);
                    // Swap
                    var temp = GameManager.Instance.playerHand[i];
                    GameManager.Instance.playerHand[i] = GameManager.Instance.playerHand[randomIndex];
                    GameManager.Instance.playerHand[randomIndex] = temp;
                }
            }
        }
        else
        {
            // Shuffle opponent's hand
            if (GameManager.Instance.opponentHand != null && GameManager.Instance.opponentHand.Count > 0)
            {
                for (int i = GameManager.Instance.opponentHand.Count - 1; i > 0; i--)
                {
                    int randomIndex = UnityEngine.Random.Range(0, i + 1);
                    // Swap
                    var temp = GameManager.Instance.opponentHand[i];
                    GameManager.Instance.opponentHand[i] = GameManager.Instance.opponentHand[randomIndex];
                    GameManager.Instance.opponentHand[randomIndex] = temp;
                }
            }
        }
        
        Debug.Log($"[Lua] Duel.ShuffleHand({(isPlayer ? "Player" : "Opponent")})");
    }

    // FASE 15.01: Get real free zone count for location
    public int GetLocationCount(object player, object location, params object[] extraArgs)
    {
        if (GameManager.Instance == null) return 0;
        
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        
        // Count free zones based on location
        if (loc == LuaConstants.LOCATION_MZONE)
        {
            return GameManager.Instance.GetFreeMonsterZones(isPlayer);
        }
        else if (loc == LuaConstants.LOCATION_SZONE || loc == LuaConstants.LOCATION_FIELD)
        {
            return GameManager.Instance.GetFreeSpellTrapZones(isPlayer);
        }
        
        return 0;
    }

    // ===== FASE 22: Counter and Extra Deck Location Methods =====
    public int GetLocationCountFromEx(object player, object summonPlayer, object ignoreCard = null, object targetCard = null)
    {
        if (GameManager.Instance == null) return 0;
        
        bool isPlayer = IsPlayer(player);
        
        // Count free extra deck zones (typically for Synchro/XYZ summons)
        // For now, return a fixed value - in full impl would check actual extra deck state
        return isPlayer ? 
            Mathf.Max(0, GameManager.Instance.GetPlayerExtraDeck().Count) :
            Mathf.Max(0, GameManager.Instance.GetOpponentExtraDeck().Count);
    }

    public bool RemoveCounterFromCard(object player, object card, object counterType, object count, object reason)
    {
        LuaCard luaCard = card as LuaCard;
        if (luaCard == null) return false;
        return luaCard.RemoveCounter(player, counterType, count, reason);
    }

    public bool IsCanRemoveCounterFromCard(object player, object card, object counterType, object count, object reason)
    {
        LuaCard luaCard = card as LuaCard;
        if (luaCard == null) return false;
        int current = luaCard.GetCounter(counterType);
        return current >= ConvertToInt(count);
    }

    public int GetTurnPlayer()
    {
        // Retorna 0 se for o turno do Jogador Humano, 1 se for da IA
        return GameManager.Instance.isPlayerTurn ? 0 : 1;
    }

    public int GetCurrentPhase()
    {
        if (PhaseManager.Instance == null) return 0;
        
        // Tradução direta para a tabela hexadecimal do constant.lua
        switch (PhaseManager.Instance.currentPhase)
        {
            case GamePhase.Draw: return 0x01;       // PHASE_DRAW
            case GamePhase.Standby: return 0x02;    // PHASE_STANDBY
            case GamePhase.Main1: return 0x04;      // PHASE_MAIN1
            case GamePhase.Battle: return 0x80;     // PHASE_BATTLE
            case GamePhase.Main2: return 0x100;     // PHASE_MAIN2
            case GamePhase.End: return 0x200;       // PHASE_END
            default: return 0;
        }
    }

    public bool IsPhase(object phaseObj)
    {
        int phase = 0;
        if (phaseObj is double d) phase = (int)d;
        else if (phaseObj is int i) phase = i;
        else if (phaseObj is long l) phase = (int)l;
        return GetCurrentPhase() == phase;
    }
    
    // FASE 18: Chain/Phase Query Methods
    /// <summary>
    /// Check if a chain can be disabled/negated by field effects
    /// </summary>
    public bool IsChainDisablable(object chainc)
    {
        // Chain can be disabled if there are active negation effects
        // For now, return true as a base implementation
        // Could expand with checking CardEffectManager for active negations
        return true;
    }
    
    /// <summary>
    /// Check if a chain can be negated
    /// </summary>
    public bool IsChainNegatable(object chainc)
    {
        int chainNum = ConvertToInt(chainc);
        if (chainNum <= 0) return false;
        
        // Chain is negatable if player has not acted yet in response window
        // Most chains can be negated unless effect explicitly prohibits
        return true;
    }
    
    /// <summary>
    /// Check if a player is affected by a specific effect type
    /// </summary>
    public bool IsPlayerAffectedByEffect(object player, object effect_type)
    {
        bool isPlayer = IsPlayer(player);
        int effectType = ConvertToInt(effect_type);
        
        // Check if player has immunity effects active
        // For now, return false (no immunity by default)
        if (CardEffectManager.Instance != null)
        {
            // Could check for Jinzo-like effects that grant immunity
            // return CardEffectManager.Instance.IsPlayerImmuneToEffect(isPlayer, effectType);
        }
        
        return false;
    }
    
    /// <summary>
    /// Shortcut: Check if current phase is battle phase
    /// </summary>
    public bool IsBattlePhase()
    {
        return IsPhase(LuaConstants.PHASE_BATTLE);
    }
    
    /// <summary>
    /// Check if current phase is main phase (MAIN1 or MAIN2)
    /// </summary>
    public bool IsMainPhase(object main_num = null)
    {
        int currentPhase = GetCurrentPhase();
        
        if (main_num == null)
        {
            // Check for either MAIN1 or MAIN2
            return currentPhase == LuaConstants.PHASE_MAIN1 || currentPhase == LuaConstants.PHASE_MAIN2;
        }
        
        int mainNum = ConvertToInt(main_num);
        if (mainNum == 1)
            return currentPhase == LuaConstants.PHASE_MAIN1;
        else if (mainNum == 2)
            return currentPhase == LuaConstants.PHASE_MAIN2;
        
        return false;
    }

    public void Hint(object msgType, object player, object desc) { }
    public void AddCustomActivityCounter(object counter_id, object activity_type, object filter) { }
    public void EnableGlobalFlag(object flag) { }
    
    // FASE 19: Missing Duel Methods
    /// <summary>
    /// Check if a card is involved in current chain
    /// </summary>
    public bool IsRelateToChain(object card)
    {
        if (card is not LuaCard c || c.unityCard == null) return false;
        
        // Check if card is in the current chain
        if (ChainManager.Instance != null && ChainManager.Instance.chainLinks != null)
        {
            foreach (var link in ChainManager.Instance.chainLinks)
            {
                if (link.cardSource == c.unityCard)
                    return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get current chain count/depth
    /// </summary>
    public int GetChainCount()
    {
        if (ChainManager.Instance != null && ChainManager.Instance.chainLinks != null)
            return ChainManager.Instance.chainLinks.Count;
        return 0;
    }
    
    /// <summary>
    /// Get the player currently making decisions
    /// </summary>
    public int GetActingPlayer()
    {
        return GetTurnPlayer();  // Currently active player is always acting
    }
    
    /// <summary>
    /// Check if chain resolution is complete
    /// </summary>
    public bool IsChainSolved()
    {
        // Chain is solved when no more chain links exist
        return GetChainCount() == 0;
    }
    
    /// <summary>
    /// Select a spell card from field
    /// </summary>
    public LuaGroup SelectSpellCards()
    {
        LuaGroup group = new LuaGroup();
        
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return group;
        
        // Get all spell/trap zones for both players
        Transform[] playerSpellZones = GameManager.Instance.duelFieldUI.playerSpellZones;
        Transform[] opponentSpellZones = GameManager.Instance.duelFieldUI.opponentSpellZones;
        
        // Collect all spell cards
        if (playerSpellZones != null)
        {
            foreach (Transform zone in playerSpellZones)
            {
                if (zone != null && zone.childCount > 0)
                {
                    CardDisplay card = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (card != null)
                        group.AddCard(new LuaCard(card));
                }
            }
        }
        
        if (opponentSpellZones != null)
        {
            foreach (Transform zone in opponentSpellZones)
            {
                if (zone != null && zone.childCount > 0)
                {
                    CardDisplay card = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (card != null)
                        group.AddCard(new LuaCard(card));
                }
            }
        }
        
        return group;
    }
    
    /// <summary>
    /// Get a specific field zone
    /// </summary>
    public LuaCard GetFieldZone(object player, object zone_index)
    {
        bool isPlayer = IsPlayer(player);
        int zoneIdx = ConvertToInt(zone_index);
        
        if (GameManager.Instance?.duelFieldUI == null) return null;
        
        Transform[] zones = isPlayer ? 
            GameManager.Instance.duelFieldUI.playerMonsterZones : 
            GameManager.Instance.duelFieldUI.opponentMonsterZones;
        
        if (zones != null && zoneIdx >= 0 && zoneIdx < zones.Length && zones[zoneIdx].childCount > 0)
        {
            CardDisplay card = zones[zoneIdx].GetChild(0).GetComponent<CardDisplay>();
            if (card != null)
                return new LuaCard(card);
        }
        
        return null;
    }

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) { }

    
    // Extensões Descobertas pelo Mass Validator
    // FASE 15.03: CheckReleaseGroupCost - validate tributes exist
    public bool CheckReleaseGroupCost(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs)
    {
        bool isPlayer = IsPlayer(player);
        int requiredCount = ConvertToInt(count);
        
        if (requiredCount <= 0) return true;
        if (GameManager.Instance == null) return false;
        
        int availableCount = 0;
        
        if (ConvertToInt(use_hand) != 0)
        {
            if (isPlayer && GameManager.Instance.playerHand != null)
                availableCount += GameManager.Instance.playerHand.Count;
            else if (!isPlayer && GameManager.Instance.opponentHand != null)
                availableCount += GameManager.Instance.opponentHand.Count;
        }
        
        int freeMonsters = GameManager.Instance.GetFreeMonsterZones(isPlayer);
        int totalMonsters = 5;
        availableCount += (totalMonsters - freeMonsters);
        
        return availableCount >= requiredCount;
    }
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetFieldGroupCount(object player, object location1, object location2) { return 0; }
    public bool IsEnvironment(object cardcode) 
    { 
        // FASE 27: Environment detection - check if card is a Field Spell
        int code = ConvertToInt(cardcode);
        if (code == 0) return false;
        
        try
        {
            // Search through the card database for this card
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.cardDatabase != null)
            {
                foreach (CardData card in CardEffectManager.Instance.cardDatabase.cardDatabase)
                {
                    string cardCodeStr = card.id?.Replace("DM", "") ?? card.password ?? "";
                    if (int.TryParse(cardCodeStr, out int cardCode) && cardCode == code)
                    {
                        bool isField = card.type != null && 
                            (card.type.Contains("Field") || 
                             card.type.Contains("Environment") ||
                             card.property?.Contains("Field") == true);
                        
                        Debug.Log($"[Lua] IsEnvironment({code}): {card.name} = {(isField ? "true" : "false")}");
                        return isField;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Lua] IsEnvironment error: {ex.Message}");
        }
        
        Debug.Log($"[Lua] IsEnvironment({code}): Card not found in database");
        return false;
    }
    public bool IsPlayerCanDiscardDeck(object player, object count) { return true; }
    public bool IsPlayerCanRemove(object player) { return true; }
    public int GetCustomActivityCount(object counter_id, object player, object activity_type) { return 0; }
    public bool IsPlayerCanSpecialSummonMonster(object player, object code, params object[] args) { return true; }
    public int GetActivityCount(object player, object activity_type, params object[] args) { return 0; }
    public int GetCurrentChain() { return ChainManager.Instance != null ? ChainManager.Instance.currentChain.Count : 0; }
    public int GetTurnCount() { return GameManager.Instance != null ? GameManager.Instance.turnCount : 0; }
    public bool IsAbleToEnterBP() { return true; }
    public bool IsMainPhase() { return PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2); }
    public int GetLP(object player) { return IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP; }
    
    public void SetLP(object player, object amount)
    {
        if (GameManager.Instance == null) return;
        
        int newLP = ConvertToInt(amount);
        if (newLP < 0) newLP = 0;
        
        if (IsPlayer(player))
        {
            GameManager.Instance.playerLP = newLP;
            Debug.Log($"[LuaAPI] Player LP set to {newLP}");
            // Atualiza UI
            if (GameManager.Instance.duelFieldUI != null && GameManager.Instance.duelFieldUI.playerLPText != null)
                GameManager.Instance.duelFieldUI.playerLPText.text = newLP.ToString();
        }
        else
        {
            GameManager.Instance.opponentLP = newLP;
            Debug.Log($"[LuaAPI] Opponent LP set to {newLP}");
            // Atualiza UI
            if (GameManager.Instance.duelFieldUI != null && GameManager.Instance.duelFieldUI.opponentLPText != null)
                GameManager.Instance.duelFieldUI.opponentLPText.text = newLP.ToString();
        }
    }
    
    public LuaGroup GetFieldGroup(object player, object loc1, object loc2) { return new LuaGroup(); }
    public bool IsCanRemoveCounter(object player, object s, object o, object counterType, object count, object reason) { return true; }
    public bool HasFlagEffect(object player, object flag) { return false; }
    public bool CheckEvent(object event_code) { return false; }
    public int GetFlagEffect(object player, object flag) { return 0; }
    public int GetBattleDamage(object player) { return 0; }
    public bool IsDuelType(object type) { return true; }
    public bool IsPlayerCanSpecialSummon(object player) { return true; }
    public int GetMZoneCount(object player) { return 5; }
    public int GetSZoneCount(object player) { return 5; }
    
    public int GetFreeMonsterZones(object player)
    {
        if (GameManager.Instance == null) return 0;
        bool isPlayer = IsPlayer(player);
        int free = GameManager.Instance.GetFreeMonsterZones(isPlayer);
        Debug.Log($"[LuaAPI] GetFreeMonsterZones({(isPlayer ? "Player" : "Opponent")}): {free}");
        return free;
    }
    
    public int GetFreeSpellTrapZones(object player)
    {
        if (GameManager.Instance == null) return 0;
        bool isPlayer = IsPlayer(player);
        int free = GameManager.Instance.GetFreeSpellTrapZones(isPlayer);
        Debug.Log($"[LuaAPI] GetFreeSpellTrapZones({(isPlayer ? "Player" : "Opponent")}): {free}");
        return free;
    }
    
    public bool IsCanAddCounter(object player, object counterType, object count, object card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(object card) { return new LuaGroup(); }
    public int GetMatchingGroupCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) { return 0; }
    public int GetTargetCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) { return 0; }
    public LuaGroup CheckReleaseGroup(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) { return new LuaGroup(); }
    public bool IsPlayerCanDiscardDeckAsCost(object player, object count) { return true; }
    public DynValue GetOperationInfo(object chainc, object category) { return DynValue.NewTuple(DynValue.NewBoolean(false), UserData.Create(new LuaGroup()), DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); }
    public bool SelectYesNo(object player, object desc) { return true; }
    public int SelectOption(object player, params object[] options) { return 0; }
    public int SelectPosition(object player, object card, object pos) { return 1; }
    public int AnnounceNumber(object player, params object[] args) { return 1000; }
    public int AnnounceLevel(object player, params object[] args) { return 4; }
    public int AnnounceAttribute(object player, object count, object avail) { return 1; }
    public int AnnounceRace(object player, object count, object avail) { return 1; }
    public int AnnounceCard(object player, params object[] args) { return 0; }
    public int GetOperationCount(object chainc) { return 0; }
    public void DiscardDeck(object player, object count, object reason) { }
    public bool CheckTribute(object card, object min, object max, object group = null, object zone = null) { return true; }
    public void SetTargetCard(object target) { }
    public void ClearTargetCard() { }
    public bool CheckEvent(object event_code, object chainc = null) { return false; }
    
    public void BreakEffect() { } // Usado para separar efeitos simultâneos em Mágicas/Traps

    public DynValue SelectReleaseGroupCost(params object[] args) { return UserData.Create(new LuaGroup()); }
    public int SelectDisableField(params object[] args) { return 0; }
    public LuaEffect SelectEffect(params object[] args) { return new LuaEffect { owner = SafeDummyCard() }; }
    public void ConfirmCards(params object[] args) { }
    public void SetPossibleOperationInfo(params object[] args) { }
    public int GetDrawCount(params object[] args) { return 1; }
    public void SetTargetPlayer(object p) { targetPlayer = ConvertToInt(p); }

    public void RegisterEffect(LuaEffect e, object player = null)
    {
        if (e == null) return;
        Debug.Log($"[Lua] Duel.RegisterEffect (Global) - Evento: {e.code}");
    }

    public void SetTargetParam(object p) { targetParam = ConvertToInt(p); }
    
// FASE 15.04: GetChainInfo - support 10 standard parameters
public DynValue GetChainInfo(object chainc, params object[] args)
{
    List<DynValue> returns = new List<DynValue>();
    foreach (object o in args)
    {
        int arg = ConvertToInt(o);
        switch (arg)
        {
            case 1: returns.Add(DynValue.NewNumber(chainId)); break;
            case 2: returns.Add(DynValue.NewNumber(chainCount)); break;
            case 4: returns.Add(DynValue.NewNumber(targetPlayer)); break;
            case 8: returns.Add(DynValue.NewNumber(targetParam)); break;
            case 16:
                {
                    LuaGroup g = currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
                    returns.Add(UserData.Create(g));
                }
                break;
            case 32: returns.Add(DynValue.NewNumber(triggeringPlayer)); break;
            case 64: returns.Add(UserData.Create(currentEffect ?? new LuaEffect())); break;
            case 128: returns.Add(UserData.Create(currentTriggeringCard ?? SafeDummyCard())); break;
            case 256: returns.Add(DynValue.NewNumber(chainOperation)); break;
            case 512: returns.Add(DynValue.NewNumber(chainSolving ? 1 : 0)); break;
            default: returns.Add(DynValue.Nil); break;
        }
    }
    if (returns.Count == 0) return DynValue.NewTuple(DynValue.Nil, DynValue.Nil);
    if (returns.Count == 1) return returns[0];
    return DynValue.NewTuple(returns.ToArray());
}

    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public LuaEffect GetPlayerEffect(object player, object effect_code) { return new LuaEffect { owner = SafeDummyCard() }; }

    public LuaCard GetFirstTarget()
    {
        return (currentTargetGroup != null && currentTargetGroup.GetFirst() != null) ? currentTargetGroup.GetFirst() : null;
    }

    public LuaGroup GetTargetCards(object e)
    {
        return currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
    }

    public int SendtoHand(object target, object player, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    GameManager.Instance.MoveCard(c.unityCard, CardLocation.Hand, SendReason.Effect);
                    count++;
                }
            }
            Debug.Log($"[Lua] Duel.SendtoHand(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.MoveCard(card.unityCard, CardLocation.Hand, SendReason.Effect);
            count = 1;
            Debug.Log($"[Lua] Duel.SendtoHand({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    // ===== FASE 20: Duel.MoveCard() - Generic card transportation =====
    public bool MoveCard(object target, object location, object reason = null, object player = null)
    {
        LuaCard card = target as LuaCard;
        if (card == null || card.unityCard == null) return false;

        int locInt = ConvertToInt(location);
        CardLocation dest = CardLocation.Unknown;

        // Map Lua LOCATION_* constants to CardLocation
        switch (locInt & 0x7F)  // Mask off flags to get base location
        {
            case 0x1: dest = CardLocation.Deck; break;
            case 0x2: dest = CardLocation.Hand; break;
            case 0x4: dest = CardLocation.Field; break;  // LOCATION_MZONE
            case 0x8: dest = CardLocation.Field; break;  // LOCATION_SZONE
            case 0x10: dest = CardLocation.Graveyard; break;
            case 0x20: dest = CardLocation.Banished; break;
            case 0x40: dest = CardLocation.ExtraDeck; break;
            default: return false;
        }

        // Track previous location before moving (for history)
        if (!card.unityCard.displayData.ContainsKey("previousLocation"))
        {
            card.unityCard.displayData["previousLocation"] = card.unityCard.CurrentLocation;
        }

        // Execute the move
        SendReason sendReason = SendReason.Effect;
        if (reason != null)
        {
            int reasonInt = ConvertToInt(reason);
            if (reasonInt == LuaConstants.REASON_COST) sendReason = SendReason.Cost;
            else if (reasonInt == LuaConstants.REASON_BATTLE) sendReason = SendReason.Battle;
            else if (reasonInt == LuaConstants.REASON_DISCARD) sendReason = SendReason.Discarded;
        }

        GameManager.Instance.MoveCard(card.unityCard, dest, sendReason);
        Debug.Log($"[Lua] Duel.MoveCard({card.unityCard.CurrentCardData.name} -> {dest}, Reason: {sendReason})");
        return true;
    }

    // ===== FASE 22: IsCanBeSpecialSummoned - checks if card can be SS (used in 40+ filters) =====
    public bool IsCanBeSpecialSummoned(object effect, object sumtype, object player, object noMatCheck = null, object noSummonCheck = null, object pos = null)
    {
        // This is a critical filter function used in many card scripts
        // FASE 27: Enhanced to handle real summon restrictions
        
        int playerInt = ConvertToInt(player);
        int sumtypeInt = ConvertToInt(sumtype);
        
        // Check if player has summon restrictions
        bool isPlayer = playerInt == 0;
        DeckManager playerDeck = isPlayer ? GameManager.Instance.playerDeckManager : GameManager.Instance.opponentDeckManager;
        
        // Forbidden status checks
        if (playerDeck != null)
        {
            if (GameManager.Instance.checkPlayerForbiddenStatus(playerDeck, "FORBID_SPECIAL_SUMMON"))
                return false;
            
            // Summon type specific checks
            if ((sumtypeInt & 0x1) != 0 && GameManager.Instance.checkPlayerForbiddenStatus(playerDeck, "FORBID_FUSION_SUMMON"))
                return false;
            if ((sumtypeInt & 0x2) != 0 && GameManager.Instance.checkPlayerForbiddenStatus(playerDeck, "FORBID_SYNCHRO_SUMMON"))
                return false;
            if ((sumtypeInt & 0x4) != 0 && GameManager.Instance.checkPlayerForbiddenStatus(playerDeck, "FORBID_XYZ_SUMMON"))
                return false;
        }
        
        // Check available monster zones
        bool hasMonsterZone = isPlayer ? 
            GameManager.Instance.playerMonsterZones.Any(z => z == null || !z.isOccupied) :
            GameManager.Instance.opponentMonsterZones.Any(z => z == null || !z.isOccupied);
        
        if (!hasMonsterZone) return false;
        
        // Card is eligible for special summon
        return true;
    }

    public bool SpecialSummon(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        int posInt = ConvertToInt(pos);
        bool inDefense = (posInt & 0x8) != 0 || (posInt & 0xA) != 0; // Verifica se tem flag de defesa

        if (target is LuaGroup group && group.cards.Count > 0)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                    GameManager.Instance.SpecialSummonFromData(c.unityCard.CurrentCardData, isPlayerSummoning, 0, true, inDefense);
                else if (c.unityData != null)
                    GameManager.Instance.SpecialSummonFromData(c.unityData, isPlayerSummoning, 0, true, inDefense);
            }
            Debug.Log($"[Lua] Duel.SpecialSummon(Grupo)");
            return true;
        }
        else if (target is LuaCard card)
        {
            if (card.unityCard != null)
            {
                GameManager.Instance.SpecialSummonFromData(card.unityCard.CurrentCardData, isPlayerSummoning, 0, true, inDefense);
                Debug.Log($"[Lua] Duel.SpecialSummon({card.unityCard.CurrentCardData.name})");
                return true;
            }
            else if (card.unityData != null)
            {
                GameManager.Instance.SpecialSummonFromData(card.unityData, isPlayerSummoning, 0, true, inDefense);
                Debug.Log($"[Lua] Duel.SpecialSummon({card.unityData.name} - Token)");
                return true;
            }
        }
        
        return false;
    }
    
    public bool SpecialSummonStep(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        return SpecialSummon(target, sumtype, sumplayer, player, nocheck, nolimit, pos);
    }

    public void SpecialSummonComplete()
    {
        // Finaliza a invocação iniciada pelo SpecialSummonStep (Já resolvemos instantaneamente no passo)
    }

    public LuaCard CreateToken(object player, object code)
    {
        int cardCode = ConvertToInt(code);
        CardData tokenData = null;

        // Tenta achar os dados do token (status, imagem) no banco usando o ID (Password)
        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            string strCode = cardCode.ToString();
            tokenData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == strCode);
        }

        // Fallback robusto se a API do site não nos enviou o token
        if (tokenData == null)
        {
            tokenData = new CardData 
            { 
                id = "TOKEN", 
                password = cardCode.ToString(),
                name = "Token", 
                type = "Monster (Token)", 
                atk = 0, def = 0, level = 1, 
                race = "Beast", attribute = "Earth" 
            };
        }

        return new LuaCard(tokenData);
    }

    public void Release(object target, object reason)
    {
        SendtoGrave(target, reason); // Tribute usa a mesma lógica base de enviar ao GY por enquanto
    }

    public bool Equip(object player, object equip_card, object target)
    {
        LuaCard ec = equip_card as LuaCard;
        LuaCard t = target as LuaCard;
        if (ec != null && t != null && ec.unityCard != null && t.unityCard != null)
        {
            if (ec.unityData.type.Contains("Monster"))
                GameManager.Instance.EquipMonsterToMonster(ec.unityCard, t.unityCard);
            else
                GameManager.Instance.CreateCardLink(ec.unityCard, t.unityCard, CardLink.LinkType.Equipment);
            
            Debug.Log($"[Lua] Duel.Equip({ec.unityData.name} em {t.unityData.name})");
            return true;
        }
        return false;
    }

    // FASE 15.02: ChangePosition with position parameter support
    public void ChangePosition(object card, object au, object ad, object du, object dd)
    {
        if (card is not LuaCard c || c.unityCard == null) return;
        
        int auVal = ConvertToInt(au);
        int duVal = ConvertToInt(du);
        
        if (auVal != 0 && c.unityCard.position != CardDisplay.BattlePosition.Attack)
        {
            c.unityCard.position = CardDisplay.BattlePosition.Attack;
            c.unityCard.transform.localRotation = Quaternion.identity;
        }
        else if (duVal != 0 && c.unityCard.position != CardDisplay.BattlePosition.Defense)
        {
            c.unityCard.position = CardDisplay.BattlePosition.Defense;
            c.unityCard.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        
        Debug.Log($"[LuaAPI] ChangePosition updated to {c.unityCard.position}");
        if (GameManager.Instance != null)
            GameManager.Instance.OnBattlePositionChanged(c.unityCard);
    }

    public void Summon(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) GameManager.Instance.TrySummonMonster(c.unityCard.gameObject, c.unityData, false, ConvertToInt(ignoreLimit) != 0);
    }

    public void MSet(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) GameManager.Instance.TrySummonMonster(c.unityCard.gameObject, c.unityData, true, ConvertToInt(ignoreLimit) != 0);
    }

    public bool CheckLPCost(object player, object cost)
    {
        int lp = IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        return lp >= ConvertToInt(cost);
    }

    public void PayLPCost(object player, object cost)
    {
        GameManager.Instance.PayLifePoints(IsPlayer(player), ConvertToInt(cost));
    }

    public LuaCard GetAttacker()
    {
        // Exige reflexão da classe estática/singleton de Batalha do jogo
        if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
            return new LuaCard(BattleManager.Instance.currentAttacker);
        return null;
    }

    public LuaCard GetAttackTarget()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentTarget != null)
            return new LuaCard(BattleManager.Instance.currentTarget);
        return null;
    }

    public bool GetControl(object target, object player, object reset_phase = null, object reset_count = null)
    {
        bool targetPlayerIsHuman = IsPlayer(player);
        
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null && c.unityCard.isPlayerCard != targetPlayerIsHuman)
                    GameManager.Instance.SwitchControl(c.unityCard);
            }
            return true;
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            if (card.unityCard.isPlayerCard != targetPlayerIsHuman)
                GameManager.Instance.SwitchControl(card.unityCard);
            return true;
        }
        
        return false;
    }

    public LuaCard GetFieldCard(object player, object location, object sequence)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return null;
        
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        int seq = ConvertToInt(sequence);

        Transform[] zones = null;
        if ((loc & 0x04) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        else if ((loc & 0x08) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;

        if (zones != null && seq >= 0 && seq < zones.Length && zones[seq].childCount > 0)
        {
            var cd = zones[seq].GetChild(0).GetComponent<CardDisplay>();
            if (cd != null) return new LuaCard(cd);
        }
        if ((loc & 0x08) != 0 && seq == 5) // Field Zone Convention
        {
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { var cd = fz.GetChild(0).GetComponent<CardDisplay>(); if (cd != null) return new LuaCard(cd); }
        }
        return null;
    }

    // --- SISTEMA DE BUSCA E FILTROS DO LUA ---

    private void CollectCandidates(int loc, bool isPlayer, List<LuaCard> candidates, LuaCard excluded = null)
    {
        if ((loc & 0x01) != 0) // DECK
        {
            var deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
            foreach(var c in deck) { LuaCard lc = new LuaCard(c); if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x02) != 0) // HAND
        {
            var handGOs = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
            foreach(var go in handGOs) { LuaCard lc = new LuaCard(go.GetComponent<CardDisplay>()); if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); }
        }
        if ((loc & 0x04) != 0) // MZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var z in zones) if (z.childCount > 0) { LuaCard lc = new LuaCard(z.GetChild(0).GetComponent<CardDisplay>()); if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); }
        }
        if ((loc & 0x08) != 0) // SZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            foreach (var z in zones) if (z.childCount > 0) { LuaCard lc = new LuaCard(z.GetChild(0).GetComponent<CardDisplay>()); if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); }
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { LuaCard lc = new LuaCard(fz.GetChild(0).GetComponent<CardDisplay>()); if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); }
        }
        if ((loc & 0x10) != 0) // GRAVE
        {
            var gy = isPlayer ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
            foreach(var c in gy) { LuaCard lc = new LuaCard(c); if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x20) != 0) // REMOVED
        {
            var rm = isPlayer ? GameManager.Instance.GetPlayerRemoved() : GameManager.Instance.GetOpponentRemoved();
            foreach(var c in rm) { LuaCard lc = new LuaCard(c); if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x40) != 0) // EXTRA
        {
            var ex = isPlayer ? GameManager.Instance.GetPlayerExtraDeck() : GameManager.Instance.GetOpponentExtraDeck();
            foreach(var c in ex) { LuaCard lc = new LuaCard(c); if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
    }

    public LuaGroup GetMatchingGroup(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs)
    {
        LuaGroup group = new LuaGroup();
        List<LuaCard> candidates = new List<LuaCard>();

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            bool isPlayer = IsPlayer(ConvertToInt(player));
            
            LuaCard excludedCard = excluded as LuaCard; // Simplificado para compatibilidade
            int lSelf = ConvertToInt(locSelf);
            int lOpp = ConvertToInt(locOpp);
            if (lSelf != 0) CollectCandidates(lSelf, isPlayer, candidates, excludedCard);
            if (lOpp != 0) CollectCandidates(lOpp, !isPlayer, candidates, excludedCard);
        }

        // Aplica o filtro Lua em cada carta encontrada na Unity
        foreach (var c in candidates)
        {            
            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                DynValue result = closure.Call(callArgs.ToArray());                if (result.Type == DataType.Boolean && result.Boolean)
                {
                    group.AddCard(c);
                }
            }
            else
            {
                group.AddCard(c);
            }
        }
        return group;
    }

    public bool IsExistingMatchingCard(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs);
        return group.GetCount() >= ConvertToInt(count);
    }

    public bool IsExistingTarget(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        return IsExistingMatchingCard(filterFunc, player, locSelf, locOpp, count, excluded, extraArgs);
    }

    // --- OPERAÇÕES ASSÍNCRONAS DE LUA (YIELD REQ) ---

    public DynValue SelectTarget(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        LuaGroup candidates = GetMatchingGroup(filterFunc, ConvertToInt(player), ConvertToInt(locSelf), ConvertToInt(locOpp), excluded, extraArgs);

        if (candidates.cards.Count == 0)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return UserData.Create(new LuaGroup());
        }

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(candidates, ConvertToInt(min), ConvertToInt(max));
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            this.currentTargetGroup = aiChoice;
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
        }

        System.Predicate<CardDisplay> unityFilter = (cd) => {
            return candidates.cards.Exists(lc => lc.unityCard == cd);
        };

        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(unityFilter, (selectedCard) => {
                LuaGroup selectedGroup = new LuaGroup();
                selectedGroup.AddCard(new LuaCard(selectedCard));
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                this.currentTargetGroup = selectedGroup; // Salva para o GetFirstTarget()
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
    }

    public DynValue SelectMatchingCard(object player, object filterFunc, object player2, object locSelf, object locOpp, object min, object max, object excluded, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        LuaGroup candidates = GetMatchingGroup(filterFunc, ConvertToInt(player), ConvertToInt(locSelf), ConvertToInt(locOpp), excluded, extraArgs);
        if (candidates.cards.Count == 0) {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return UserData.Create(new LuaGroup());
        }

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(candidates, ConvertToInt(min), ConvertToInt(max));
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            this.currentTargetGroup = aiChoice;
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectMatchingCard") });
        }

        List<CardData> selectableData = new List<CardData>();
        foreach (var c in candidates.cards) {
            if (c.unityData != null && !selectableData.Contains(c.unityData)) selectableData.Add(c.unityData);
        }

        if (GameManager.Instance != null && selectableData.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(selectableData, "Escolha um alvo", ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                LuaGroup selectedGroup = new LuaGroup();
                foreach (var data in selectedList)
                {
                    LuaCard match = candidates.cards.Find(lc => lc.unityData == data);
                    if (match != null) selectedGroup.AddCard(match);
                    else selectedGroup.AddCard(new LuaCard(data));
                }
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                this.currentTargetGroup = selectedGroup; // Salva para o GetFirstTarget()
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectMatchingCard") });
    }

    public DynValue SelectReleaseGroup(object player, object filterFunc, object min, object max, object excluded, params object[] extraArgs)
    {
        return SelectMatchingCard(player, filterFunc, player, 0x04, 0, ConvertToInt(min), ConvertToInt(max), excluded, extraArgs);
    }

    public int GetTargetPlayer() { return targetPlayer; }
    public int GetTargetParam() { return targetParam; }

    public DynValue TossCoin(object player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        GameManager.Instance.TossCoin(ConvertToInt(count), (heads) => {
            object[] results = new object[ConvertToInt(count)];
            for (int i = 0; i < ConvertToInt(count); i++) results[i] = (i < heads) ? 1 : 0;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(Array.ConvertAll(results, x => DynValue.NewNumber((int)x)));
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        });

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossCoin") });
    }

    public DynValue TossDice(object player, object count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            int c = ConvertToInt(count);
            DynValue[] dynResults = new DynValue[c];
            for (int i = 0; i < c; i++) dynResults[i] = DynValue.NewNumber(UnityEngine.Random.Range(1, 7));
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(dynResults);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossDice") });
        }

        GameManager.Instance.RollDice(ConvertToInt(count), false, (results) => {
            int c = ConvertToInt(count);
            DynValue[] dynResults = new DynValue[c];
            for (int i = 0; i < c; i++) dynResults[i] = DynValue.NewNumber(results[i]);
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(dynResults);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        });

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossDice") });
    }

    // Helper em Corrotina para a Destruição Simultânea com VFX
    private IEnumerator DestroyCardsRoutine(List<CardDisplay> cards)
    {
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.SendToGraveyard(c.CurrentCardData, c.isPlayerCard);
                GameObject.Destroy(c.gameObject);
            }
        }
        yield return null;
    }

    // FASE 12: Event System Methods
    public void RaiseSingleEvent(int eventCode, params object[] args)
    {
        Debug.Log($"[LuaDuel] RaiseSingleEvent called: eventCode={eventCode}, args={args?.Length ?? 0}");
        EventSystem.RaiseEvent(eventCode, args);
    }

    public void OnEvent(int eventCode, object luaFunction)
    {
        if (luaFunction == null) return;
        
        // Wrapper callback que executa a Lua function
        EventSystem.EventCallback callback = (evt) =>
        {
            try
            {
                // Se fosse Lua real, aqui chamaria a função Lua
                // Por enquanto, apenas log
                Debug.Log($"[LuaDuel] OnEvent callback for {eventCode}: {evt}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LuaDuel] Error in OnEvent callback: {ex.Message}");
            }
        };

        EventSystem.Subscribe(eventCode, callback);
        Debug.Log($"[LuaDuel] Subscribed to event {eventCode}");
    }
}

// ==============================================================================
// 2. CLASSE CARD (Representação de uma Carta Física)
// Chamado no Lua como: c:GetAttack()
// ==============================================================================
[MoonSharpUserData]
public class LuaCard
{
    public CardDisplay unityCard;
    public CardData unityData;
    public List<LuaEffect> registeredEffects = new List<LuaEffect>();

    public LuaCard(CardDisplay card) { unityCard = card; unityData = card?.CurrentCardData; }
    public LuaCard(CardData data) { unityData = data; unityCard = null; }

    private int ConvertToInt(object obj)
    {
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        return 0;
    }
    
    // Helper para gerar um dummy seguro e evitar crashes de Null Reference no LUA
    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public int GetAttack() { return unityCard != null ? unityCard.currentAtk : (unityData != null ? unityData.atk : 0); }
    public int GetDefense() { return unityCard != null ? unityCard.currentDef : (unityData != null ? unityData.def : 0); }
    public int GetLevel() { return unityCard != null ? unityCard.CurrentCardData.level : (unityData != null ? unityData.level : 0); }
    public bool IsFaceup() { return unityCard != null ? !unityCard.isFlipped : false; }
    
    public int GetControler()
    {
        if (unityCard == null) return 0;
        return unityCard.isPlayerCard ? 0 : 1;
    }

    public bool IsControler(object playerIndex)
    {
        // Retorna true se o 'tp' repassado no script for o dono atual desta carta física no tabuleiro
        return GetControler() == ConvertToInt(playerIndex);
    }

    public bool IsOnField()
    {
        return unityCard != null && unityCard.isOnField;
    }

    public bool IsLocation(object locationVal) 
    { 
        int loc = ConvertToInt(locationVal);
        if (unityCard != null && unityCard.isOnField)
        {
            if (unityData.type.Contains("Spell") || unityData.type.Contains("Trap")) return (loc & 0x08) != 0;
            return (loc & 0x04) != 0;
        }
        if (unityCard != null && !unityCard.isOnField) return (loc & 0x02) != 0; // Hand
        
        if (unityData != null)
        {
            if ((loc & 0x10) != 0 && (GameManager.Instance.GetPlayerGraveyard().Contains(unityData) || GameManager.Instance.GetOpponentGraveyard().Contains(unityData))) return true;
            if ((loc & 0x01) != 0 && (GameManager.Instance.GetPlayerMainDeck().Contains(unityData) || GameManager.Instance.GetOpponentMainDeck().Contains(unityData))) return true;
            if ((loc & 0x20) != 0 && (GameManager.Instance.GetPlayerRemoved().Contains(unityData) || GameManager.Instance.GetOpponentRemoved().Contains(unityData))) return true;
            if ((loc & 0x40) != 0 && (GameManager.Instance.GetPlayerExtraDeck().Contains(unityData) || GameManager.Instance.GetOpponentExtraDeck().Contains(unityData))) return true;
        }
        return false;
    }

    // --- PERGUNTAS DE STATUS QUE O SCRIPT FAZ À CARTA ---

    public bool IsImmuneToEffect() { return false; }

    public bool IsDestructable() 
    { 
        // Regra geral de protótipo: Tudo é destrutível a menos que algum escudo proíba
        return true; 
    }

    public bool IsSpellTrap()
    {
        if (unityData == null) return false;
        return unityData.type.Contains("Spell") || unityData.type.Contains("Trap");
    }

    public new int GetType()
    {
        if (unityData == null) return 0;
        int t = 0;
        if (unityData.type.Contains("Monster")) t |= 0x1;
        if (unityData.type.Contains("Spell")) t |= 0x2;
        if (unityData.type.Contains("Trap")) t |= 0x4;
        if (unityData.type.Contains("Normal")) t |= 0x10;
        if (unityData.type.Contains("Effect")) t |= 0x20;
        if (unityData.type.Contains("Fusion")) t |= 0x40;
        if (unityData.type.Contains("Ritual")) t |= 0x80;
        if (unityData.type.Contains("Spirit")) t |= 0x200;
        if (unityData.type.Contains("Union")) t |= 0x400;
        if (unityData.type.Contains("Gemini")) t |= 0x800;
        if (unityData.type.Contains("Token")) t |= 0x4000;
        if (unityData.property == "Quick-Play") t |= 0x10000;
        if (unityData.property == "Continuous") t |= 0x20000;
        if (unityData.property == "Equip") t |= 0x40000;
        if (unityData.property == "Field") t |= 0x80000;
        if (unityData.property == "Counter") t |= 0x100000;
        if (unityData.type.Contains("Toon")) t |= 0x400000;
        return t;
    }

    public int GetOriginalRace() { return GetRace(); }
    public int GetOriginalAttribute() { return GetAttribute(); }
    public int GetAttribute() { 
        if (unityData == null) return 0;
        string a = unityData.attribute;
        if (a == "Earth") return 0x01;
        if (a == "Water") return 0x02;
        if (a == "Fire") return 0x04;
        if (a == "Wind") return 0x08;
        if (a == "Light") return 0x20;
        if (a == "Dark") return 0x10;
        if (a == "Divine") return 0x40;
        return 0;
    }
    public int GetTextAttack() { return GetAttack(); }
    public int GetTextDefense() { return GetDefense(); }

    public int GetOriginalType() { return GetType(); }

    public bool IsType(object t) { return (GetType() & ConvertToInt(t)) != 0; }
    
    public bool IsTrap() { return unityData != null && unityData.type.Contains("Trap"); }
    public bool IsSpell() { return unityData != null && unityData.type.Contains("Spell"); }
    public bool IsMonster() { return unityData != null && unityData.type.Contains("Monster"); }

    // Novos Stubs Descobertos pelo Mass Validator
    public bool IsPreviousLocation(object loc) { return true; }
    // FASE 15.05: Get actual opponent card in battle
    public LuaCard GetBattleTarget()
    {
        if (unityCard == null || GameManager.Instance == null) return null;
        
        bool isPlayer = unityCard.ownerPlayer;
        Transform[] opponentZones = isPlayer ? GameManager.Instance.duelFieldUI?.opponentMonsterZones : GameManager.Instance.duelFieldUI?.playerMonsterZones;
        
        if (opponentZones == null) return null;
        
        foreach (Transform zone in opponentZones)
        {
            if (zone != null && zone.childCount > 0)
            {
                CardDisplay target = zone.GetChild(0).GetComponent<CardDisplay>();
                if (target != null) return new LuaCard(target);
            }
        }
        
        return null;
    }
    // FASE 20: IsDiscardable with real logic - checks if card can be sent to GY as discard cost
    public bool IsDiscardable(params object[] args) 
    { 
        if (unityCard == null) return true;  // Dummy cards always discardable
        
        // Card can't be discarded if:
        // 1. It's blocked/negated
        if (unityCard.displayData.ContainsKey("isBlocked") && (bool)unityCard.displayData["isBlocked"]) 
            return false;
        
        // 2. It has protective effect running
        if (unityCard.displayData.ContainsKey("protective_shield")) 
            return false;
            
        // 3. It's in a location that can't send to GY (Extra deck face-down, removed zone, etc)
        if (unityCard.CurrentLocation == CardLocation.ExtraDeck || 
            unityCard.CurrentLocation == CardLocation.Banished)
            return false;
        
        return true;  // Otherwise always discardable
    }
    public LuaCard GetEquipTarget() { return null; }
    public bool IsAbleToGraveAsCost() { return true; }
    public bool IsAbleToRemoveAsCost() { return true; }
    public bool IsAbleToDeck() { return true; }
    public bool IsLevelBelow(object lvl) { return GetLevel() <= ConvertToInt(lvl); }
    public bool IsHasType(object type) { return IsType(type); }
    public int GetAttackAnnouncedCount() { return GetAttackedCount(); }
    public int GetReasonPlayer() { return GetControler(); }
    
    // ===== FASE 22: Counter Card Methods =====
    public int GetCounter(object counterType) 
    { 
        if (unityCard == null) return 0;
        int cType = ConvertToInt(counterType);
        
        // For COUNTER_SPELL (1), check spellCounters field
        if (cType == LuaConstants.COUNTER_SPELL)
            return unityCard.displayData.ContainsKey("counterCount") ? 
                ConvertToInt(unityCard.displayData["counterCount"]) : 
                unityCard.spellCounters;
        
        // For other counter types, store in displayData
        string counterKey = $"counter_{cType}";
        return unityCard.displayData.ContainsKey(counterKey) ? 
            ConvertToInt(unityCard.displayData[counterKey]) : 0;
    }

    public void AddCounter(object counterType, object count)
    {
        if (unityCard == null) return;
        int cType = ConvertToInt(counterType);
        int cnt = ConvertToInt(count);
        if (cnt <= 0) return;

        if (cType == LuaConstants.COUNTER_SPELL)
        {
            int current = unityCard.spellCounters;
            unityCard.spellCounters = current + cnt;
            unityCard.displayData["counterCount"] = unityCard.spellCounters;
            Debug.Log($"[Lua] Card.AddCounter({unityCard.CurrentCardData.name}, SPELL, +{cnt}) = {unityCard.spellCounters}");
        }
        else
        {
            string counterKey = $"counter_{cType}";
            int current = unityCard.displayData.ContainsKey(counterKey) ? ConvertToInt(unityCard.displayData[counterKey]) : 0;
            unityCard.displayData[counterKey] = current + cnt;
            Debug.Log($"[Lua] Card.AddCounter({unityCard.CurrentCardData.name}, Type:{cType}, +{cnt}) = {current + cnt}");
        }
    }

    public bool RemoveCounter(object playerObj, object counterType, object count, object reason)
    {
        if (unityCard == null) return false;
        int cType = ConvertToInt(counterType);
        int cnt = ConvertToInt(count);
        if (cnt <= 0) return true;

        int current = GetCounter(counterType);
        if (current < cnt) return false;  // Can't remove more than exists

        if (cType == LuaConstants.COUNTER_SPELL)
        {
            unityCard.spellCounters = current - cnt;
            unityCard.displayData["counterCount"] = unityCard.spellCounters;
            Debug.Log($"[Lua] Card.RemoveCounter({unityCard.CurrentCardData.name}, SPELL, -{cnt}) = {unityCard.spellCounters}");
        }
        else
        {
            string counterKey = $"counter_{cType}";
            unityCard.displayData[counterKey] = current - cnt;
            Debug.Log($"[Lua] Card.RemoveCounter({unityCard.CurrentCardData.name}, Type:{cType}, -{cnt}) = {current - cnt}");
        }
        
        // Fire event if event system available
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.eventSystem != null)
            EventSystem.RaiseEvent(LuaConstants.EVENT_CUSTOM, new { card = this, counter = cType, removed = cnt });
        
        return true;
    }

    public bool IsCanAddCounter(object counterType, object count)
    {
        if (unityCard == null) return false;
        // Most cards can add counters unless blocked
        // Could check for "cannot add counters" status effects here if implemented
        return true;
    }

    public bool IsFacedown() { return unityCard != null ? unityCard.isFlipped : false; }
    public bool IsTributeSummoned() { return unityCard != null && unityCard.isTributeSummoned; }
    public int GetPreviousPosition() { return GetBattlePosition(); }
    public int GetPreviousCodeOnField() { return GetCode(); }
    public int GetPreviousRaceOnField() { return GetRace(); }
    public bool IsCanBeEffectTarget(object e) { return true; }
    public int GetBaseDefense() { return GetDefense(); }
    public bool IsSummonType(object sumtype) { return true; }
    public bool IsCanRemoveCounter(object player, object counterType, object count, object reason) { return true; }
    public bool IsPreviousRaceOnField(object race) { return true; }
    public bool IsRelateToCard(object card) { return true; }
    public bool CanSummonOrSet(object ignoreLimit, object param) { return true; }
    public LuaCard GetOwner() { return this; }
    public int GetPreviousControler() { return GetControler(); }
    public int GetReason() { return 0; }
    public LuaGroup GetTarget() { return new LuaGroup(); }
    public void SetMaterial(object g) { }
    public LuaGroup GetAdminGroup() { return new LuaGroup(); }
    public bool IsSummonLocation(object loc) { return true; }
    public void SetStatus(object status, object enable) { }
    
    public int GetRace() 
    { 
        if (unityData == null) return 0;
        string r = unityData.race;
        if (r == "Warrior") return 0x1;
        if (r == "Spellcaster") return 0x2;
        if (r == "Fairy") return 0x4;
        if (r == "Fiend") return 0x8;
        if (r == "Zombie") return 0x10;
        if (r == "Machine") return 0x20;
        if (r == "Aqua") return 0x40;
        if (r == "Pyro") return 0x80;
        if (r == "Rock") return 0x100;
        if (r == "Winged Beast") return 0x200;
        if (r == "Plant") return 0x400;
        if (r == "Insect") return 0x800;
        if (r == "Thunder") return 0x1000;
        if (r == "Dragon") return 0x2000;
        if (r == "Beast") return 0x4000;
        if (r == "Beast-Warrior") return 0x8000;
        if (r == "Dinosaur") return 0x10000;
        if (r == "Fish") return 0x20000;
        if (r == "Sea Serpent") return 0x40000;
        if (r == "Reptile") return 0x80000;
        return 0;
    }
    public int GetMaterialCount() { return 0; }
    public bool IsAbleToDeckAsCost() { return true; }
    public bool IsSummonPlayer(object player) { return true; }
    public int GetOriginalLevel() { return GetLevel(); }
    public LuaGroup GetAttackableTarget() { return new LuaGroup(); }
    public bool IsAbleToChangeControler() { return true; }
    public bool IsSummonableCard() { return true; }
    public LuaGroup GetMaterial() { return new LuaGroup(); }
    public int GetEffectCount(object code) { return 0; }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return 0; }
    public int GetPreviousLocation() { return 0; }
    public LuaCard GetHandler() { return this; }
    public int GetCardTargetCount() { return 0; }
    public bool IsOriginalCodeRule(params object[] codes) { return IsCode(codes); }
    public bool IsFieldSpell() { return IsType(0x80000); }
    public int GetLocation() { return unityCard != null && unityCard.isOnField ? (unityData.type.Contains("Spell") || unityData.type.Contains("Trap") ? 0x08 : 0x04) : 0x02; }
    public int GetDestination() { return 0; }
    public int GetLeaveFieldDest() { return 0; }
    public LuaGroup GetColumnGroup() { return new LuaGroup(); }
    public LuaGroup GetOverlayGroup() { return new LuaGroup(); }
    public int GetOverlayCount() { return 0; }
    public LuaGroup GetCardTarget() { return new LuaGroup(); }
    public bool HasNonZeroAttack() { return GetAttack() > 0; }
    public bool HasNonZeroDefense() { return GetDefense() > 0; }
    public bool IsCanChangePosition() { return true; }
    public int GetTurnID() { return 0; }
    public bool CanChainAttack() { return true; }
    public LuaCard GetPreviousEquipTarget() { return null; }
    public bool IsAbleToGrave() { return true; }
    public bool HasFlagEffect(object id) { return false; }
    public int GetTurnCounter() { return unityCard != null ? unityCard.turnCounter : 0; }
    public bool IsHasCardTarget(object c) { return false; }
    public LuaCard GetReasonCard() { return SafeDummyCard(); }
    public void CompleteProcedure() { } // Confirmação de invocações especiais/rituais
    public void CancelToGrave(params object[] args) { } // Mantém a carta no campo (Ex: Swords of Revealing Light)
    public bool IsLevelAbove(object lvl) { return GetLevel() >= ConvertToInt(lvl); }
    public int GetBattledGroupCount() { return 0; }
    public bool IsRitualMonster() { return unityData != null && unityData.type.Contains("Ritual"); }
    public bool IsRitualSpell() { return unityData != null && unityData.type.Contains("Spell") && unityData.property == "Ritual"; }
    public bool IsPublic() { return true; }
    public int GetOwnerTargetCount() { return 0; }
    public bool IsFusionSummoned() { return unityCard != null && unityCard.summonedThisTurn; }
    public bool IsDefenseBelow(object def) { return GetDefense() <= ConvertToInt(def); }
    public int GetFlagEffectLabel(object id) { return 0; }
    public bool IsAttributeExcept(object attr) { return !IsAttribute(attr); }
    public int GetBattlePosition() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense ? 0x8 : 0x1; }
    public bool IsRelateToBattle() { return true; }
    public void DeleteGroup() { }

    // Stubs para compatibilidade da API Lua
    public bool IsRace(object r) 
    { 
        if (unityData == null) return false;
        int raceVal = ConvertToInt(r);
        int cardRace = GetRace();
        return (cardRace & raceVal) != 0;  // Bitwise AND to check if race matches
    }
    
    public bool IsAttribute(object attr) 
    { 
        if (unityData == null) return false;
        int attrVal = ConvertToInt(attr);
        int cardAttr = GetAttribute();
        return (cardAttr & attrVal) != 0;  // Bitwise AND to check if attribute matches
    }
    
    public bool IsReason(object reason) { return true; }
    public bool IsRelateToEffect(object e) { return true; } // Evita crash no final de correntes (Chains)
    public bool IsCodeExact(object code) { return GetCode() == ConvertToInt(code); }
    public bool IsAttackBelow(object atk) { return GetAttack() <= ConvertToInt(atk); }
    public bool IsAttackAbove(object atk) { return GetAttack() >= ConvertToInt(atk); }
    public bool IsDefenseAbove(object def) { return GetDefense() >= ConvertToInt(def); }
    public int GetAttackedCount() { return unityCard != null ? unityCard.attacksDeclaredThisTurn : 0; }
    public bool IsCanTurnSet() { return true; }
    public bool IsCanBeSpecialSummoned(object e, object sumtype, object sumplayer, object nocheck, object nolimit, object pos = null) { return true; }
    public bool IsReleasable() { return true; }
    public bool IsReleasableByEffect() { return true; }
    public bool IsDestructibleByEffect() { return IsDestructable(); }
    public bool IsRemovableByEffect() { return true; }
    public bool IsBanishableByEffect() { return true; }
    public bool IsDiscardableByEffect() { return IsDiscardable(); }
    public bool IsPreviousControler(object p) { return true; }
    public bool IsAbleToHand() { return true; }
    public bool IsPreviousPosition(object pos) { return true; }
    public bool IsDefensePos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense; }
    public bool IsAttackPos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Attack; }
    
    // FASE 17: Card Property Accessors
    /// <summary>
    /// Get original/base attack value without modifications
    /// </summary>
    public int GetBaseAttack()
    {
        if (unityData != null) return unityData.atk;
        return (unityCard != null) ? unityCard.currentAtk : 0;
    }
    
    /// <summary>
    /// Get field zone index (0-4) if on field, -1 if not
    /// </summary>
    public int GetFieldID()
    {
        if (unityCard == null || unityCard.transform.parent == null) return -1;
        
        Transform[] monsterZones = unityCard.ownerPlayer ? 
            GameManager.Instance?.duelFieldUI?.playerMonsterZones : 
            GameManager.Instance?.duelFieldUI?.opponentMonsterZones;
        
        if (monsterZones == null) return -1;
        
        for (int i = 0; i < monsterZones.Length; i++)
        {
            if (monsterZones[i] == unityCard.transform.parent)
                return i;
        }
        
        return -1;
    }
    
    /// <summary>
    /// Check if this card is related to another (link, equip, material, etc)
    /// </summary>
    public bool IsRelateToCard(LuaCard other)
    {
        if (other == null || other.unityCard == null) return false;
        
        // Check if equipped to other card
        if (CardEffectManager.Instance != null && unityCard != null)
        {
            var equipped = CardEffectManager.Instance.GetEquippedCards(other.unityCard);
            if (equipped != null && equipped.Contains(unityCard)) return true;
        }
        
        // Could expand with more relation types (link, material, etc)
        return false;
    }
    
    /// <summary>
    /// Get all cards related to this one (equipped cards, materials, linked cards)
    /// </summary>
    public LuaGroup GetRelatedGroup()
    {
        LuaGroup group = new LuaGroup();
        
        if (CardEffectManager.Instance == null || unityCard == null) return group;
        
        // Add equipped cards
        var equipped = CardEffectManager.Instance.GetEquippedCards(unityCard);
        if (equipped != null)
        {
            foreach (var card in equipped)
            {
                group.AddCard(new LuaCard(card));
            }
        }
        
        return group;
    }
    public bool IsPosition(object pos) { return true; } // Stub para checagens múltiplas
    public bool IsSummonable(object ignoreLimit, object param) { return true; }
    public bool IsMSetable(object ignoreLimit, object param) { return true; }
    public bool IsSSetable(params object[] args) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    public int GetFlagEffect(object id) { return 0; }

    public int GetCode() {
        if (unityData != null && !string.IsNullOrEmpty(unityData.password) && int.TryParse(unityData.password, out int code)) return code;
        return 0;
    }
    public int GetOriginalCode() { return GetCode(); }
    public void RegisterFlagEffect(int effectId, int statusCode, params object[] args) 
    { 
        if (unityCard != null) 
        {
            if (unityCard.flagEffects == null) unityCard.flagEffects = new System.Collections.Generic.Dictionary<int, CardDisplay.FlagEffect>();
            unityCard.flagEffects[effectId] = new CardDisplay.FlagEffect 
            { 
                effectId = effectId, 
                statusCode = statusCode, 
                parameters = args 
            };
            Debug.Log($"[LuaAPI] Card {unityCard.CurrentCardData.name} registered flag effect {effectId} with status {statusCode}");
        }
    }
    public void RemoveFlagEffect(int effectId)
    {
        if (unityCard != null && unityCard.flagEffects != null && unityCard.flagEffects.ContainsKey(effectId))
        {
            unityCard.flagEffects.Remove(effectId);
            Debug.Log($"[LuaAPI] Removed flag effect {effectId} from {unityCard.CurrentCardData.name}");
        }
    }
    
    // FASE 14: Status System - Set/clear card status flags
    public void SetStatus(int statusCode, bool activate)
    {
        if (unityCard != null)
        {
            unityCard.SetStatus(statusCode, activate);
            Debug.Log($"[LuaAPI] Card {unityCard.CurrentCardData.name} status {statusCode} set to {activate}");
        }
    }
    
    public void SetCardTarget(object tc) { }
    // FASE 14: Status System - Check card status flags (bitwise)
    public bool IsStatus(int statusCode) 
    { 
        if (unityCard == null) return false;
        return unityCard.IsStatusActive(statusCode);
    }
    
    // FASE 13: Card History Tracking
    public bool IsPreviousLocation(int luaLocation)
    {
        if (unityCard == null) return false;
        
        CardLocation luaLoc = LuaConstants.LuaLocationToCardLocation(luaLocation);
        bool result = (unityCard.previousLocation == luaLoc);
        
        Debug.Log($"[LuaAPI] IsPreviousLocation check: {luaLoc} vs {unityCard.previousLocation} = {result}");
        return result;
    }

    public int GetPreviousOwner()
    {
        if (unityCard == null) return 0;
        int owner = unityCard.previousOwner; // 0 = player, 1 = opponent
        Debug.Log($"[LuaAPI] GetPreviousOwner: {owner}");
        return owner;
    }

    public bool RememberPreviousLocation()
    {
        // This is called when a card's effect activates to remember its location
        // Typically called at chain activation to snapshot the location at that moment
        if (unityCard == null) return false;
        
        Debug.Log($"[LuaAPI] RememberPreviousLocation: Current location = {unityCard.CurrentLocation}");
        // The location is already tracked in MoveCard, so just return true
        return true;
    }
    
    public LuaCard GetFirstCardTarget() { return null; }
    public void SetTurnCounter(object ct) { }
    public int GetLabel() { return 0; }
    public void SetLabel(object ct) { }
    
    public void EnableReviveLimit() { }
    public void SetUniqueOnField(params object[] args) { }
    public void EnableCounterPermit(params object[] args) { }
    public void SetCounterLimit(params object[] args) { }
    public bool IsAbleToRemove() { return true; }
    public void AddMustBeSpecialSummoned(params object[] args) { }
    public void EnableUnsummonable() { }
    public void SetSPSummonOnce(params object[] args) { }
    public DynValue IsHasEffect(object effectCode) { return DynValue.Nil; }
    public void ResetFlagEffect(object id) { }
    public void SetFlagEffectLabel(object id, object label) { }
    public void CreateEffectRelation(object e) { }
    public void ReleaseEffectRelation(object e) { }
    public void SetHint(params object[] args) { }
    
    public bool IsCode(params object[] codes)
    {
        int myId = 0;
        if (unityData != null && !string.IsNullOrEmpty(unityData.password)) int.TryParse(unityData.password, out myId);
        foreach(var c in codes) if (myId == ConvertToInt(c)) return true;
        return false;
    }

    public bool IsContinuousTrap() { return IsTrap() && unityData != null && unityData.property == "Continuous"; }
    public int GetEquipCount() { return CardEffectManager.Instance != null && unityCard != null ? CardEffectManager.Instance.GetEquippedCards(unityCard).Count : 0; }
    public bool IsOriginalType(object t) { return IsType(t); }
    public bool IsOriginalAttribute(object attr) { return IsAttribute(attr); }
    public bool IsOriginalRace(object race) { return IsRace(race); }
    public bool IsOriginalCode(params object[] codes) { return IsCode(codes); }
    public LuaGroup GetEquippedGroup() { 
        LuaGroup g = new LuaGroup(); 
        if (CardEffectManager.Instance != null && unityCard != null) 
            foreach(var c in CardEffectManager.Instance.GetEquippedCards(unityCard)) g.AddCard(new LuaCard(c));
        return g; 
    }
    
    public void AddMonsterAttribute(params object[] args) { }
    public void AddSetcodes(params object[] args) { }

    public void RegisterEffect(LuaEffect e, bool forced = false)
    {
        if (e == null) return;
        registeredEffects.Add(e);
        Debug.Log($"[Lua] Efeito tipo {e.type} registrado em {unityData?.name}.");
    }

    public bool IsSetCard(params object[] setCodes) { return true; }
    
    public LuaEffect CheckActivateResult(object b) 
    { 
        return new LuaEffect { 
            owner = this, 
            conditionFunc = CardEffectManager.Instance.dummyClosureTrue, 
            costFunc = CardEffectManager.Instance.dummyClosureTrue, 
            targetFunc = CardEffectManager.Instance.dummyClosureTrue, 
            operationFunc = CardEffectManager.Instance.dummyClosureTrue 
        }; 
    }

    public static LuaGroup operator +(LuaCard a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.AddCard(a);
        if (b != null && a != b) g.AddCard(b);
        return g;
    }
    public static LuaGroup operator -(LuaCard a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null && a != b) g.AddCard(a);
        return g;
    }
}

// ==============================================================================
// 3. CLASSE EFFECT (Estrutura de Habilidades)
// Chamado no Lua como: Effect.CreateEffect(c)
// ==============================================================================
[MoonSharpUserData]
public class LuaEffect
{
    public LuaCard owner;
    public int code;
    public int type;
    public int property;
    public string description;
    public int category;
    private object _valueObject = 0;
    private int _label = 0;  // FASE 20: Track effect-specific state
    
    public Closure conditionFunc;
    public Closure costFunc;
    public Closure targetFunc;
    public Closure operationFunc;

    private int ConvertToInt(object obj)
    {
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        return 0;
    }

    // O YGOPro permite que os efeitos sejam criados chamando a classe Effect direto
    public static LuaEffect CreateEffect(object c)
    {
        return new LuaEffect { owner = c as LuaCard };
    }

    public void SetType(object t) { type = ConvertToInt(t); }
    public void SetCode(object c) { code = ConvertToInt(c); }
    public void SetProperty(object p1, object p2 = null) { property = ConvertToInt(p1); }
    public void SetDescription(object d) { description = d?.ToString() ?? ""; }
    public void SetCategory(object c) { category = ConvertToInt(c); }
    public bool IsHasType(object t) { return (type & ConvertToInt(t)) != 0; }
    public bool IsActiveType(object t) { return true; }
    public int GetCount() { return 0; }
    public void SetOwnerPlayer(object player) { }
    public int GetDescription() { return 0; }
    public void SetHint(params object[] args) { }
    
    // Funções muito usadas no OCGCore na inicialização ignoradas elegantemente
    public void SetCountLimit(params object[] args) { }
    public void SetHintTiming(params object[] args) { }
    public void SetTargetRange(params object[] args) { }
    public void SetReset(params object[] args) { }
    public void SetValue(params object[] args) { if (args != null && args.Length > 0) _valueObject = args[0]; }
    public object GetValue() { return _valueObject; }
    public void SetRange(params object[] args) { }
    public void SetLabel(params object[] args) { if (args != null && args.Length > 0) _label = ConvertToInt(args[0]); }  // FASE 20: Now stores state
    public int GetLabel() { return _label; }  // FASE 20: Now returns real state
    
    public bool IsHasProperty(object prop) { return true; }
    public bool IsHasCategory(object cat) { return true; }
    public int GetActiveType() { return type; }
    public bool IsSpellEffect() { return true; }
    public bool IsTrapEffect() { return true; }
    public bool IsMonsterEffect() { return true; }
    public bool IsSpellTrapEffect() { return true; }
    
public Closure GetCondition() { return conditionFunc != null ? conditionFunc : (CardEffectManager.Instance != null ? CardEffectManager.Instance.dummyClosureTrue : null); }
public Closure GetCost() { return costFunc != null ? costFunc : (CardEffectManager.Instance != null ? CardEffectManager.Instance.dummyClosureTrue : null); }
public Closure GetTarget() { return targetFunc != null ? targetFunc : (CardEffectManager.Instance != null ? CardEffectManager.Instance.dummyClosureTrue : null); }
public Closure GetOperation() { return operationFunc != null ? operationFunc : (CardEffectManager.Instance != null ? CardEffectManager.Instance.dummyClosureTrue : null); }    
    public int GetCategory() { return category; }
    public int GetProperty() { return property; }
    public int GetCode() { return code; }
    
    private object _labelObject = null;
    public void SetLabelObject(object o) { _labelObject = o; }
    public object GetLabelObject() { 
        if (_labelObject != null) return _labelObject;
        return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); 
    }

    public LuaEffect Clone()
    {
        return new LuaEffect {
            owner = this.owner,
            code = this.code,
            type = this.type,
            property = this.property,
            description = this.description,
            category = this.category,
            conditionFunc = this.conditionFunc,
            costFunc = this.costFunc,
            targetFunc = this.targetFunc,
            operationFunc = this.operationFunc,
            _valueObject = this._valueObject,
            _label = this._label  // FASE 20: Copy label state on clone
        };
    }

    // Callbacks do Lua (Condição, Alvo, Resolução)
    public void SetCondition(object condition) { conditionFunc = condition as Closure; }
    public void SetCost(object cost) { costFunc = cost as Closure; }
    public void SetTarget(object target) { targetFunc = target as Closure; }
    public void SetOperation(object operation) { operationFunc = operation as Closure; }

    public int GetHandlerPlayer() { return owner != null ? owner.GetControler() : 0; }
    // Retorna a quem o efeito pertence blindado contra Nulos!
    public LuaCard GetHandler() { return owner ?? new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }
    public LuaCard GetOwner() { return owner ?? new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }
}

// ==============================================================================
// 4. CLASSE GROUP (Lista de Cartas selecionadas ou filtradas)
// Chamado no Lua como: g:GetFirst()
// ==============================================================================
[MoonSharpUserData]
public class LuaGroup
{
    public List<LuaCard> cards = new List<LuaCard>();
    private int _iterIndex = 0;

    public void AddCard(object c) { if (c is LuaCard lc && lc != null && !cards.Contains(lc)) cards.Add(lc); }
    public void RemoveCard(object c) { if (c is LuaCard lc && lc != null) cards.Remove(lc); }
    public LuaCard GetFirst() { 
        _iterIndex = 0; 
        if (cards.Count > 0) { _iterIndex = 1; return cards[0]; }
        return null; 
    }
    public LuaCard GetNext() {
        if (_iterIndex < cards.Count) return cards[_iterIndex++];
        return null;
    }
    public void Clear() { cards.Clear(); }
    public int GetCount() { return cards.Count; }
    
    public bool IsContains(object card) { return true; }
    public LuaGroup Clone() { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Sub(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Add(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }

    public static LuaGroup FromCards(params object[] args)
    {
        LuaGroup g = new LuaGroup();
        foreach (var arg in args) if (arg is LuaCard lc) g.AddCard(lc);
        return g;
    }

    public static LuaGroup CreateGroup() { return new LuaGroup(); }

    public int GetClassCount(object func)
    {
        // Retorna a quantidade de cartas ÚnICAS pelo ID dentro deste grupo
        HashSet<int> uniqueCodes = new HashSet<int>();
        foreach (var c in cards)
        {
            if (c.unityData != null)
            {
                if (!string.IsNullOrEmpty(c.unityData.password) && int.TryParse(c.unityData.password, out int code))
                    uniqueCodes.Add(code);
            }
        }
        return uniqueCodes.Count;
    }

    public bool IsExists(object filterFunc, object count, object excluded, params object[] extraArgs)
    {
        int matchCount = 0;
        List<LuaCard> safeCards = new List<LuaCard>(cards);
        foreach (var c in safeCards)
        {
            if (excluded != null)
            {
                if (excluded is LuaCard excCard && c == excCard) continue;
                if (excluded is LuaGroup excGroup && excGroup.cards.Contains(c)) continue;
            }

            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                DynValue result = closure.Call(callArgs.ToArray());
                if (result.Type == DataType.Boolean && result.Boolean) matchCount++;
            }
            else matchCount++;
        }
        return matchCount >= ConvertToInt(count);
    }

    public LuaGroup Filter(object filterFunc, object excluded, params object[] extraArgs)
    {
        LuaGroup newGroup = new LuaGroup();
        List<LuaCard> safeCards = new List<LuaCard>(cards);
        foreach (var c in safeCards)
        {
            if (excluded != null)
            {
                if (excluded is LuaCard excCard && c == excCard) continue;
                if (excluded is LuaGroup excGroup && excGroup.cards.Contains(c)) continue;
            }

            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                DynValue result = closure.Call(callArgs.ToArray());
                if (result.Type == DataType.Boolean && result.Boolean) newGroup.AddCard(c);
            }
            else newGroup.AddCard(c);
        }
        return newGroup;
    }
    
    public int FilterCount(params object[] args)
    {
        return 0; // Stub rápido para Dry-Run não quebrar
    }
    
    public DynValue FilterSelect(params object[] args)
    {
        return UserData.Create(new LuaGroup()); // Stub blindado
    }

    private int ConvertToInt(object obj)
    {
        if (obj == null) return 0;
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        return 0;
    }

    public DynValue Select(object player, object min, object max, object excluded)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        List<CardData> selectableData = new List<CardData>();
        foreach (var c in cards) {
            if (excluded != null)
            {
                if (excluded is LuaCard excCard && c == excCard) continue;
                if (excluded is LuaGroup excGroup && excGroup.cards.Contains(c)) continue;
            }

            if (c.unityData != null && !selectableData.Contains(c.unityData)) selectableData.Add(c.unityData);
        }
        
        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (ConvertToInt(player) != 0 && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(this, ConvertToInt(min), ConvertToInt(max));
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            CardEffectManager.Instance.luaDuel.currentTargetGroup = aiChoice; 
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.Select") });
        }

        if (GameManager.Instance != null && selectableData.Count > 0) {
            GameManager.Instance.OpenCardMultiSelection(selectableData, "Escolha um alvo do grupo", ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                LuaGroup selectedGroup = new LuaGroup();
                foreach (var data in selectedList) {
                    LuaCard match = cards.Find(lc => lc.unityData == data);
                    if (match != null) selectedGroup.AddCard(match);
                }
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                CardEffectManager.Instance.luaDuel.currentTargetGroup = selectedGroup; 
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        } else {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return UserData.Create(new LuaGroup());
        }
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.Select") });
    }

    public void KeepAlive() { }
    public void Delete() { }
    public void Merge(LuaGroup group) { if (group != null) this.cards.AddRange(group.cards); }

    public static LuaGroup operator +(LuaGroup a, LuaGroup b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(new List<LuaCard>(a.cards));
        if (b != null) g.cards.AddRange(new List<LuaCard>(b.cards));
        g.cards = new List<LuaCard>(new HashSet<LuaCard>(g.cards));
        return g;
    }
    public static LuaGroup operator -(LuaGroup a, LuaGroup b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(new List<LuaCard>(a.cards));
        if (b != null) g.cards.RemoveAll(x => b.cards.Contains(x));
        return g;
    }
    public static LuaGroup operator +(LuaGroup a, object b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(new List<LuaCard>(a.cards));
        if (b is LuaCard lc && !g.cards.Contains(lc)) g.cards.Add(lc);
        return g;
    }
    public static LuaGroup operator +(object a, LuaGroup b) { return b + a; }
    public static LuaGroup operator -(LuaGroup a, object b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(new List<LuaCard>(a.cards));
        if (b is LuaCard lc) g.cards.Remove(lc);
        return g;
    }

    public LuaGroup GetMaxGroup(object evalFunc)
    {
        if (cards.Count == 0) return new LuaGroup();
        LuaGroup g = new LuaGroup();
        g.AddCard(cards[0]);
        return g;
    }
    public LuaGroup GetMinGroup(object evalFunc)
    {
        if (cards.Count == 0) return new LuaGroup();
        LuaGroup g = new LuaGroup();
        g.AddCard(cards[0]);
        return g;
    }

    public LuaGroup RandomSelect(object player, object count)
    {
        int c = ConvertToInt(count);
        LuaGroup result = new LuaGroup();
        if (cards.Count == 0 || c <= 0) return result;

        List<LuaCard> pool = new List<LuaCard>(cards);
        
        for (int i = 0; i < c && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.AddCard(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }
    
    [MoonSharpUserDataMetamethod("__call")]
    public DynValue __call(ScriptExecutionContext executionContext, CallbackArguments args)
    {
        DynValue varArg = args.Count > 2 ? args[2] : DynValue.Nil;
        if (varArg.IsNil()) {
            LuaCard first = GetFirst();
            return first != null ? UserData.Create(first) : DynValue.Nil;
        } else {
            LuaCard next = GetNext();
            return next != null ? UserData.Create(next) : DynValue.Nil;
        }
    }

    [MoonSharpUserDataMetamethod("__iterator")]
    public DynValue __iterator(ScriptExecutionContext executionContext, CallbackArguments args)
    {
        return Iter();
    }

    public DynValue Iter()
    {
        int i = 0;
        return DynValue.NewCallback((context, args) => {
            if (i < cards.Count) return UserData.Create(cards[i++]);
            return DynValue.Nil;
        });
    }
}

// ==============================================================================
// FUSION SUMMON HELPER - Called by Lua scripts for Fusion summons
// Used by 4+ cards: cDM0036, cDM0051, cDM0079, cDM0083
// ==============================================================================
[MoonSharpUserData]
public class Fusion
{
    public static void RegisterSummonEff(LuaCard card)
    {
        Debug.Log($"[Lua] Fusion.RegisterSummonEff({card.CurrentCardData?.name ?? "Unknown"})");
        // Register fusion summon effect on the card
        if (card?.unityCard != null && FusionManager.Instance != null)
        {
            FusionManager.Instance.RegisterFusionCard(card.unityCard);
        }
    }

    public static bool AddProcMix(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Fusion.AddProcMix({card.CurrentCardData?.name ?? "Unknown"})");
        // Add Fusion procedure with mixed materials
        if (card?.unityCard != null && FusionManager.Instance != null)
        {
            return FusionManager.Instance.AddFusionProcedure(card.unityCard, args);
        }
        return false;
    }

    public static bool AddProc(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Fusion.AddProc({card.CurrentCardData?.name ?? "Unknown"})");
        // Standard Fusion procedure
        return AddProcMix(card, args);
    }

    public static bool AddProcUnfusionEffect(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Fusion.AddProcUnfusionEffect({card.CurrentCardData?.name ?? "Unknown"})");
        // Unfusion effect support
        return true;
    }
}

// ==============================================================================
// SYNCHRO SUMMON HELPER - Called by Lua scripts for Synchro summons
// ==============================================================================
[MoonSharpUserData]
public class Synchro
{
    public static void RegisterSummonEff(LuaCard card)
    {
        Debug.Log($"[Lua] Synchro.RegisterSummonEff({card.CurrentCardData?.name ?? "Unknown"})");
        if (card?.unityCard != null && SummonManager.Instance != null)
        {
            SummonManager.Instance.RegisterSynchroCard(card.unityCard);
        }
    }

    public static bool AddProc(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Synchro.AddProc({card.CurrentCardData?.name ?? "Unknown"})");
        return true;
    }
}

// ==============================================================================
// SPIRIT SUMMON HELPER - Called by Lua scripts for Spirit summons
// Used by 1+ cards: cDM0113
// ==============================================================================
[MoonSharpUserData]
public class Spirit
{
    public static void RegisterSummonEff(LuaCard card)
    {
        Debug.Log($"[Lua] Spirit.RegisterSummonEff({card.CurrentCardData?.name ?? "Unknown"})");
        if (card?.unityCard != null)
        {
            // Spirit monsters have special summon restrictions
            card.unityCard.canSummonThisTurn = true;
        }
    }

    public static bool AddProcedure(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Spirit.AddProcedure({card.CurrentCardData?.name ?? "Unknown"})");
        return true;
    }

    public static bool AddProc(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Spirit.AddProc({card.CurrentCardData?.name ?? "Unknown"})");
        return AddProcedure(card, args);
    }
}

// ==============================================================================
// RITUAL SUMMON HELPER - Called by Lua scripts for Ritual summons
// ==============================================================================
[MoonSharpUserData]
public class Ritual
{
    public static void RegisterSummonEff(LuaCard card)
    {
        Debug.Log($"[Lua] Ritual.RegisterSummonEff({card.CurrentCardData?.name ?? "Unknown"})");
        if (card?.unityCard != null && RitualManager.Instance != null)
        {
            RitualManager.Instance.RegisterRitualCard(card.unityCard);
        }
    }

    public static bool AddProc(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Ritual.AddProc({card.CurrentCardData?.name ?? "Unknown"})");
        return true;
    }
}

// ==============================================================================
// XYZ SUMMON HELPER - Called by Lua scripts for XYZ summons
// ==============================================================================
[MoonSharpUserData]
public class Xyz
{
    public static void RegisterSummonEff(LuaCard card)
    {
        Debug.Log($"[Lua] Xyz.RegisterSummonEff({card.CurrentCardData?.name ?? "Unknown"})");
        if (card?.unityCard != null && XYZManager.Instance != null)
        {
            XYZManager.Instance.RegisterXyzCard(card.unityCard);
        }
    }

    public static bool AddProc(LuaCard card, params object[] args)
    {
        Debug.Log($"[Lua] Xyz.AddProc({card.CurrentCardData?.name ?? "Unknown"})");
        return true;
    }
}