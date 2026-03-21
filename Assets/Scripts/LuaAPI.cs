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

    public void Destroy(object target, int reason)
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
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
            {
                if (c.unityCard != null)
                {
                    GameManager.Instance.SendToGraveyard(c.unityCard.CurrentCardData, c.unityCard.isPlayerCard);
                    if (c.unityCard.gameObject != null) GameObject.Destroy(c.unityCard.gameObject);
                    count++;
                }
            }
            Debug.Log($"[Lua] Duel.SendtoGrave(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.SendToGraveyard(card.unityCard.CurrentCardData, card.unityCard.isPlayerCard);
            if (card.unityCard.gameObject != null) GameObject.Destroy(card.unityCard.gameObject);
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
            foreach (var c in group.cards) { if (c.unityCard != null) { GameManager.Instance.BanishCard(c.unityCard); count++; } }
            Debug.Log($"[Lua] Duel.Remove(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.BanishCard(card.unityCard);
            count = 1;
            Debug.Log($"[Lua] Duel.Remove({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    public int SendtoDeck(object target, object player, object seq, object reason)
    {
        int count = 0;
        bool toTop = (ConvertToInt(seq) == 0);
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) { if (c.unityCard != null) { GameManager.Instance.ReturnToDeck(c.unityCard, toTop); count++; } }
            Debug.Log($"[Lua] Duel.SendtoDeck(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.ReturnToDeck(card.unityCard, toTop);
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

    public int GetLocationCount(object player, object location)
    {
        // Exemplo: Retorna quantas zonas de monstro estão livres
        return 5; 
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

    public void Hint(object msgType, object player, object desc) { }
    public void AddCustomActivityCounter(object counter_id, object activity_type, object filter) { }
    public void EnableGlobalFlag(object flag) { }

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) { }

    
    // Extensões Descobertas pelo Mass Validator
    public bool CheckReleaseGroupCost(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) { return true; }
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetFieldGroupCount(object player, object location1, object location2) { return 0; }
    public bool IsEnvironment(object cardcode) { return false; }
    public bool IsPlayerCanDiscardDeck(object player, object count) { return true; }
    public bool IsPlayerCanRemove(object player) { return true; }
    public int GetCustomActivityCount(object counter_id, object player, object activity_type) { return 0; }
    public bool IsPlayerCanSpecialSummonMonster(object player, object code, params object[] args) { return true; }
    public int GetActivityCount(object player, object activity_type, params object[] args) { return 0; }
    public int GetCurrentChain() { return ChainManager.Instance != null ? ChainManager.Instance.currentChain.Count : 0; }
    public int GetTurnCount() { return GameManager.Instance != null ? GameManager.Instance.turnCount : 0; }
    public bool IsAbleToEnterBP() { return true; }
    public bool IsBattlePhase() { return PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle; }
    public bool IsMainPhase() { return PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2); }
    public int GetLP(object player) { return IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP; }
    public bool IsPlayerAffectedByEffect(object player, object effect_code) { return false; }
    public LuaGroup GetFieldGroup(object player, object loc1, object loc2) { return new LuaGroup(); }
    public bool IsCanRemoveCounter(object player, object s, object o, object counterType, object count, object reason) { return true; }
    public bool HasFlagEffect(object player, object flag) { return false; }
    public bool CheckEvent(object event_code) { return false; }
    public int GetFlagEffect(object player, object flag) { return 0; }
    public int GetBattleDamage(object player) { return 0; }
    public bool IsDuelType(object type) { return true; }
    public bool IsPlayerCanSpecialSummon(object player) { return true; }
    public int GetMZoneCount(object player) { return 5; }
    public bool IsCanAddCounter(object player, object counterType, object count, LuaCard card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(LuaCard card) { return new LuaGroup(); }
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
    public bool IsChainNegatable(object chaincount) { return true; }

    public void RegisterEffect(LuaEffect e, object player = null)
    {
        if (e == null) return;
        Debug.Log($"[Lua] Duel.RegisterEffect (Global) - Evento: {e.code}");
    }

    public void SetTargetPlayer(object p) { targetPlayer = ConvertToInt(p); }
    public void SetTargetParam(object p) { targetParam = ConvertToInt(p); }
    
    public DynValue GetChainInfo(object chainc, params object[] args)
    {
         List<DynValue> returns = new List<DynValue>();
        foreach (object o in args)
        {
            int arg = ConvertToInt(o);
            if (arg == 16 || arg == 8388608) // CHAININFO_TARGET_CARDS (0x10)
            {
                LuaGroup g = currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
                returns.Add(UserData.Create(g));
            }
            else if (arg == 64 || arg == 128 || arg == 32) // TRIGGERING_EFFECT (0x40)
            {
                LuaEffect dummyEff = new LuaEffect { owner = new LuaCard(new CardData { id = "0000", name = "Dummy" }) };
                returns.Add(UserData.Create(dummyEff));
            }
            else
            {
                returns.Add(DynValue.NewNumber(0));
            }
        }
        if (returns.Count == 0) return DynValue.NewTuple(DynValue.NewNumber(0), DynValue.NewNumber(0));
        if (returns.Count == 1) return returns[0];
        return DynValue.NewTuple(returns.ToArray());
    }

    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public LuaEffect GetPlayerEffect(object player, object effect_code) { return new LuaEffect { owner = SafeDummyCard() }; }

    public LuaCard GetFirstTarget()
    {
        return (currentTargetGroup != null && currentTargetGroup.GetFirst() != null) ? currentTargetGroup.GetFirst() : SafeDummyCard();
    }

    public int SendtoHand(object target, object player, object reason)
    {
        int count = 0;
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) { if (c.unityCard != null) { GameManager.Instance.ReturnToHand(c.unityCard); count++; } }
            Debug.Log($"[Lua] Duel.SendtoHand(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.ReturnToHand(card.unityCard);
            count = 1;
            Debug.Log($"[Lua] Duel.SendtoHand({card.unityCard.CurrentCardData.name})");
        }
        return count;
    }

    public bool SpecialSummon(object target, object sumtype, object sumplayer, object player, object nocheck, object nolimit, object pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        int posInt = ConvertToInt(pos);
        bool inDefense = (posInt & 0x8) != 0 || (posInt & 0xA) != 0; // Verifica se tem flag de defesa

        if (target is LuaGroup group && group.cards.Count > 0)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null)
                    GameManager.Instance.SpecialSummonFromData(c.unityCard.CurrentCardData, isPlayerSummoning, true, inDefense);
            Debug.Log($"[Lua] Duel.SpecialSummon(Grupo)");
            return true;
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.SpecialSummonFromData(card.unityCard.CurrentCardData, isPlayerSummoning, true, inDefense);
            Debug.Log($"[Lua] Duel.SpecialSummon({card.unityCard.CurrentCardData.name})");
            return true;
        }
        
        return false;
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

    public void ChangePosition(object card, object au, object ad, object du, object dd)
    {
        if (card is LuaCard c && c.unityCard != null) c.unityCard.ChangePosition();
    }

    public void Summon(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) GameManager.Instance.TrySummonMonster(c.unityCard.gameObject, c.unityData, false, ignoreLimit);
    }

    public void MSet(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) GameManager.Instance.TrySummonMonster(c.unityCard.gameObject, c.unityData, true, ignoreLimit);
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
        return SafeDummyCard();
    }

    public LuaCard GetAttackTarget()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.currentTarget != null)
            return new LuaCard(BattleManager.Instance.currentTarget);
        return SafeDummyCard();
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

    public bool IsControler(int playerIndex)
    {
        // Retorna true se o 'tp' repassado no script for o dono atual desta carta física no tabuleiro
        return GetControler() == playerIndex;
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
    public LuaCard GetBattleTarget() { return SafeDummyCard(); }
    public bool IsDiscardable(params object[] args) { return true; }
    public LuaCard GetEquipTarget() { return SafeDummyCard(); }
    public bool IsAbleToGraveAsCost() { return true; }
    public bool IsAbleToRemoveAsCost() { return true; }
    public bool IsAbleToDeck() { return true; }
    public bool IsLevelBelow(object lvl) { return GetLevel() <= ConvertToInt(lvl); }
    public bool IsHasType(object type) { return IsType(type); }
    public int GetAttackAnnouncedCount() { return GetAttackedCount(); }
    public int GetReasonPlayer() { return GetControler(); }
    public int GetCounter(object counterType) { return unityCard != null ? unityCard.spellCounters : 0; }
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
    public void AddCounter(object counterType, object count) { }
    public void RemoveCounter(object player, object counterType, object count, object reason) { }
    public bool IsCanAddCounter(object counterType, object count) { return true; }
    public int GetFieldID() { return 0; }
    public int GetReason() { return 0; }
    public LuaGroup GetTarget() { return new LuaGroup(); }
    
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
    public int GetBaseAttack() { return GetAttack(); }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return 0; }
    public int GetPreviousLocation() { return 0; }
    public LuaGroup GetCardTarget() { return new LuaGroup(); }
    public bool HasNonZeroAttack() { return GetAttack() > 0; }
    public bool HasNonZeroDefense() { return GetDefense() > 0; }
    public bool IsCanChangePosition() { return true; }
    public int GetTurnID() { return 0; }
    public bool CanChainAttack() { return true; }
    public LuaCard GetPreviousEquipTarget() { return SafeDummyCard(); }
    public bool IsAbleToGrave() { return true; }
    public bool HasFlagEffect(object id) { return false; }
    public int GetTurnCounter() { return unityCard != null ? unityCard.turnCounter : 0; }
    public bool IsHasCardTarget(LuaCard c) { return false; }
    public LuaCard GetReasonCard() { return SafeDummyCard(); }
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

    // Stubs para compatibilidade da API Lua
    public bool IsRace(object r) { return true; }
    public bool IsAttribute(object attr) { return true; }
    public bool IsReason(object reason) { return true; }
    public bool IsRelateToEffect(object e) { return true; } // Evita crash no final de correntes (Chains)
    public bool IsAttackBelow(object atk) { return GetAttack() <= ConvertToInt(atk); }
    public bool IsAttackAbove(object atk) { return GetAttack() >= ConvertToInt(atk); }
    public bool IsDefenseAbove(object def) { return GetDefense() >= ConvertToInt(def); }
    public int GetAttackedCount() { return unityCard != null ? unityCard.attacksDeclaredThisTurn : 0; }
    public bool IsCanTurnSet() { return true; }
    public bool IsCanBeSpecialSummoned(object e, object sumtype, object sumplayer, object nocheck, object nolimit, object pos = null) { return true; }
    public bool IsReleasable() { return true; }
    public bool IsPreviousControler(object p) { return true; }
    public bool IsAbleToHand() { return true; }
    public bool IsPreviousPosition(object pos) { return true; }
    public bool IsDefensePos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense; }
    public bool IsAttackPos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Attack; }
    public bool IsPosition(object pos) { return true; } // Stub para checagens múltiplas
    public bool IsSummonable(object ignoreLimit, object param) { return true; }
    public bool IsMSetable(object ignoreLimit, object param) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    public int GetFlagEffect(object id) { return 0; }

    public int GetCode() {
        if (unityData != null && int.TryParse(unityData.id, out int code)) return code;
        return 0;
    }
    public int GetOriginalCode() { return GetCode(); }
    public void RegisterFlagEffect(params object[] args) { }
    public void SetCardTarget(object tc) { }
    public bool IsStatus(object status) { return false; }
    public LuaCard GetFirstCardTarget() { return SafeDummyCard(); }
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
    public bool IsHasEffect(object effectCode) { return false; }
    
    public bool IsHasCardTarget(object c) { return false; }
    
    public bool IsCode(params object[] codes)
    {
        int myId = 0;
        if (unityData != null) int.TryParse(unityData.id, out myId);
        foreach(var c in codes) if (myId == ConvertToInt(c)) return true;
        return false;
    }

    public void RegisterEffect(LuaEffect e, bool forced = false)
    {
        if (e == null) return;
        registeredEffects.Add(e);
        Debug.Log($"[Lua] Efeito tipo {e.type} registrado em {unityData?.name}.");
    }

    public bool IsSetCard(params object[] setCodes) { return true; }
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
    public static LuaEffect CreateEffect(LuaCard c)
    {
        return new LuaEffect { owner = c };
    }

    public void SetType(object t) { type = ConvertToInt(t); }
    public void SetCode(object c) { code = ConvertToInt(c); }
    public void SetProperty(object p1, object p2 = null) { property = ConvertToInt(p1); }
    public void SetDescription(object d) { description = d?.ToString() ?? ""; }
    public void SetCategory(object c) { category = ConvertToInt(c); }
    public bool IsHasType(object t) { return (type & ConvertToInt(t)) != 0; }
    public bool IsActiveType(object t) { return true; }
    public int GetCount() { return 0; }
    
    // Funções muito usadas no OCGCore na inicialização ignoradas elegantemente
    public void SetCountLimit(params object[] args) { }
    public void SetHintTiming(params object[] args) { }
    public void SetTargetRange(params object[] args) { }
    public void SetReset(params object[] args) { }
    public void SetValue(params object[] args) { }
    public void SetRange(params object[] args) { }
    public void SetLabel(params object[] args) { }
    public int GetLabel() { return 0; }
    
    public bool IsHasProperty(object prop) { return true; }
    public bool IsHasCategory(object cat) { return true; }
    public int GetActiveType() { return type; }
    public bool IsSpellEffect() { return true; }
    public bool IsTrapEffect() { return true; }
    public bool IsMonsterEffect() { return true; }
    public bool IsSpellTrapEffect() { return true; }
    
    public Closure GetCondition() { return conditionFunc; }
    public Closure GetCost() { return costFunc; }
    public Closure GetTarget() { return targetFunc; }
    public Closure GetOperation() { return operationFunc; }
    
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
            operationFunc = this.operationFunc
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

    public void AddCard(LuaCard c) { cards.Add(c); }
    public void RemoveCard(LuaCard c) { cards.Remove(c); }
    public LuaCard GetFirst() { 
        _iterIndex = 0; 
        if (cards.Count > 0) { _iterIndex = 1; return cards[0]; }
        return null; 
    }
    public LuaCard GetNext() {
        if (_iterIndex < cards.Count) return cards[_iterIndex++];
        return null;
    }
    public int GetCount() { return cards.Count; }
    
    public bool IsContains(LuaCard card) { return true; }
    public LuaGroup Clone() { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Sub(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Add(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }

    public static LuaGroup CreateGroup() { return new LuaGroup(); }

    public int GetClassCount(object func)
    {
        // Retorna a quantidade de cartas ÚnICAS pelo ID dentro deste grupo
        HashSet<int> uniqueCodes = new HashSet<int>();
        foreach (var c in cards)
        {
            if (c.unityData != null)
            {
                if (int.TryParse(c.unityData.id, out int code))
                    uniqueCodes.Add(code);
            }
        }
        return uniqueCodes.Count;
    }

    public bool IsExists(object filterFunc, object count, object excluded, params object[] extraArgs)
    {
        int matchCount = 0;
        foreach (var c in cards)
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
        foreach (var c in cards)
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
        if (a != null) g.cards.AddRange(a.cards);
        if (b != null) g.cards.AddRange(b.cards);
        g.cards = new List<LuaCard>(new HashSet<LuaCard>(g.cards));
        return g;
    }
    public static LuaGroup operator -(LuaGroup a, LuaGroup b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(a.cards);
        if (b != null) g.cards.RemoveAll(x => b.cards.Contains(x));
        return g;
    }
    public static LuaGroup operator +(LuaGroup a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(a.cards);
        if (b != null && !g.cards.Contains(b)) g.cards.Add(b);
        return g;
    }
    public static LuaGroup operator +(LuaCard a, LuaGroup b) { return b + a; }
    public static LuaGroup operator -(LuaGroup a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.cards.AddRange(a.cards);
        if (b != null) g.cards.Remove(b);
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
    
    // Converte nativamente um enumerador C# para uma função geradora (Closure) no Lua
    public object Iter()
    {
        int index = 0;
        List<LuaCard> list = this.cards;
        return (System.Func<LuaCard>)(() => {
            if (index < list.Count) return list[index++];
            return null;
        });
    }
}