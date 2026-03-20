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

    // Utilitário de Conversão: 0 = Human Player, 1 = AI Opponent
    public bool IsPlayer(int playerIndex)
    {
        return playerIndex == 0;
    }

    public void Damage(int player, int amount, int reason)
    {
        if (IsPlayer(player)) GameManager.Instance.DamagePlayer(amount);
        else GameManager.Instance.DamageOpponent(amount);
        Debug.Log($"[Lua] Duel.Damage({player}, {amount})");
    }

    public void Recover(int player, int amount, int reason)
    {
        if (IsPlayer(player)) GameManager.Instance.GainLifePoints(true, amount);
        else GameManager.Instance.GainLifePoints(false, amount);
        Debug.Log($"[Lua] Duel.Recover({player}, {amount})");
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

    public int Draw(int player, int amount, int reason)
    {
        bool isPlayer = IsPlayer(player);
        for (int i = 0; i < amount; i++)
        {
            // Passamos true para 'ignoreLimit' para que efeitos comprem livremente fora da Draw Phase
            if (isPlayer) GameManager.Instance.DrawCard(true);
            else GameManager.Instance.DrawOpponentCard();
        }
        Debug.Log($"[Lua] Duel.Draw({player}, {amount})");
        return amount; // OCGCore geralmente retorna quantas cartas foram compradas
    }

    public int SendtoGrave(object target, int reason)
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

    public int Remove(object target, int pos, int reason)
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

    public int SendtoDeck(object target, int player, int seq, int reason)
    {
        int count = 0;
        bool toTop = (seq == 0); // 0 = Top, 1 = Bottom (simplificado)
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

    public int DiscardHand(int player, int amount, int reason)
    {
        bool isPlayer = IsPlayer(player);
        GameManager.Instance.DiscardRandomHand(isPlayer, amount, false);
        return amount;
    }

    public int GetLocationCount(int player, int location)
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

    public void Hint(int msgType, int player, int desc)
    {
        // Usado apenas para logs na UI. Podemos ignorar ou printar no console.
    }

    // Usado por scripts para informar ações futuras (como preparar para destruir cartas)
    public void SetOperationInfo(int chainc, int category, object target, int count, int player, int param)
    {
        // No YGOPro, isso serve para dar dicas à IA e à UI sobre o que a carta vai fazer.
        // Para a nossa Unity, não precisamos fazer nada estrito aqui por enquanto.
    }

    public void SetTargetPlayer(int p) { targetPlayer = p; }
    public void SetTargetParam(int p) { targetParam = p; }
    
    public DynValue GetChainInfo(int chainc, params int[] args)
    {
        // Emula o retorno múltiplo do YGOPro: local p,d = Duel.GetChainInfo(...)
        // No MoonSharp, múltiplos retornos são feitos através de um Tuple!
        return DynValue.NewTuple(DynValue.NewNumber(targetPlayer), DynValue.NewNumber(targetParam));
    }

    public LuaCard GetFirstTarget()
    {
        return currentTargetGroup != null ? currentTargetGroup.GetFirst() : null;
    }

    public int SendtoHand(object target, int player, int reason)
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

    public bool SpecialSummon(object target, int sumtype, int sumplayer, int player, bool nocheck, bool nolimit, int pos)
    {
        bool isPlayerSummoning = IsPlayer(player);
        bool inDefense = (pos & 0x8) != 0 || (pos & 0xA) != 0; // Verifica se tem flag de defesa

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
    
    public void Release(object target, int reason)
    {
        SendtoGrave(target, reason); // Tribute usa a mesma lógica base de enviar ao GY por enquanto
    }

    public bool Equip(int player, LuaCard equip_card, LuaCard target)
    {
        if (equip_card != null && target != null && equip_card.unityCard != null && target.unityCard != null)
        {
            if (equip_card.unityData.type.Contains("Monster"))
                GameManager.Instance.EquipMonsterToMonster(equip_card.unityCard, target.unityCard);
            else
                GameManager.Instance.CreateCardLink(equip_card.unityCard, target.unityCard, CardLink.LinkType.Equipment);
            
            Debug.Log($"[Lua] Duel.Equip({equip_card.unityData.name} em {target.unityData.name})");
            return true;
        }
        return false;
    }

    public void ChangePosition(LuaCard card, int au, int ad, int du, int dd)
    {
        if (card.unityCard != null) card.unityCard.ChangePosition();
    }

    public void Summon(int player, LuaCard card, bool ignoreLimit, object param)
    {
        if (card.unityCard != null) GameManager.Instance.TrySummonMonster(card.unityCard.gameObject, card.unityData, false, ignoreLimit);
    }

    public void MSet(int player, LuaCard card, bool ignoreLimit, object param)
    {
        if (card.unityCard != null) GameManager.Instance.TrySummonMonster(card.unityCard.gameObject, card.unityData, true, ignoreLimit);
    }

    public bool CheckLPCost(int player, int cost)
    {
        int lp = IsPlayer(player) ? GameManager.Instance.playerLP : GameManager.Instance.opponentLP;
        return lp >= cost;
    }

    public void PayLPCost(int player, int cost)
    {
        GameManager.Instance.PayLifePoints(IsPlayer(player), cost);
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

    public LuaGroup GetMatchingGroup(object filterFunc, int player, int locSelf, int locOpp, LuaCard excludedCard, params object[] extraArgs)
    {
        LuaGroup group = new LuaGroup();
        List<LuaCard> candidates = new List<LuaCard>();

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            bool isPlayer = IsPlayer(player);
            
            if (locSelf != 0) CollectCandidates(locSelf, isPlayer, candidates, excludedCard);
            if (locOpp != 0) CollectCandidates(locOpp, !isPlayer, candidates, excludedCard);
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

    public bool IsExistingMatchingCard(object filterFunc, int player, int locSelf, int locOpp, int count, LuaCard excludedCard, params object[] extraArgs)
    {
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excludedCard, extraArgs);
        return group.GetCount() >= count;
    }

    public bool IsExistingTarget(object filterFunc, int player, int locSelf, int locOpp, int count, LuaCard excludedCard, params object[] extraArgs)
    {
        return IsExistingMatchingCard(filterFunc, player, locSelf, locOpp, count, excludedCard, extraArgs);
    }

    // --- OPERAÇÕES ASSÍNCRONAS DE LUA (YIELD REQ) ---

    public DynValue SelectTarget(int player, object filterFunc, int player2, int locSelf, int locOpp, int min, int max, LuaCard excludedCard, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        LuaGroup candidates = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excludedCard, extraArgs);

        if (candidates.cards.Count == 0)
        {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.FromObject(CardEffectManager.Instance.luaEngine, new LuaGroup());
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

    public DynValue SelectMatchingCard(int player, object filterFunc, int player2, int locSelf, int locOpp, int min, int max, LuaCard excludedCard, params object[] extraArgs)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        LuaGroup candidates = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excludedCard, extraArgs);
        if (candidates.cards.Count == 0) {
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.FromObject(CardEffectManager.Instance.luaEngine, new LuaGroup());
        }

        List<CardData> selectableData = new List<CardData>();
        foreach (var c in candidates.cards) {
            if (c.unityData != null && !selectableData.Contains(c.unityData)) selectableData.Add(c.unityData);
        }

        if (GameManager.Instance != null && selectableData.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(selectableData, "Escolha um alvo", min, max, (selectedList) => {
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

    public DynValue SelectReleaseGroup(int player, object filterFunc, int min, int max, LuaCard excluded, params object[] extraArgs)
    {
        return SelectMatchingCard(player, filterFunc, player, 0x04, 0, min, max, excluded, extraArgs);
    }

    public bool CheckReleaseGroup(int player, object filterFunc, int count, LuaCard excluded, params object[] extraArgs)
    {
        return IsExistingMatchingCard(filterFunc, player, 0x04, 0, count, excluded, extraArgs);
    }

    public DynValue TossCoin(int player, int count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        GameManager.Instance.TossCoin(count, (heads) => {
            object[] results = new object[count];
            for (int i = 0; i < count; i++) results[i] = (i < heads) ? 1 : 0;
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewTuple(Array.ConvertAll(results, x => DynValue.NewNumber((int)x)));
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        });

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("TossCoin") });
    }

    public DynValue TossDice(int player, int count)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        GameManager.Instance.RollDice(count, false, (results) => {
            DynValue[] dynResults = new DynValue[count];
            for (int i = 0; i < count; i++) dynResults[i] = DynValue.NewNumber(results[i]);
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

    public bool IsLocation(int loc) 
    { 
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

    public bool IsType(int typeVal)
    {
        if (unityData == null) return false;
        if ((typeVal & 0x1) != 0 && unityData.type.Contains("Monster")) return true;
        if ((typeVal & 0x2) != 0 && unityData.type.Contains("Spell")) return true;
        if ((typeVal & 0x4) != 0 && unityData.type.Contains("Trap")) return true;
        return false;
    }

    // Stubs para compatibilidade da API Lua
    public bool IsAttribute(int attr) { return true; }
    public bool IsAttackBelow(int atk) { return GetAttack() <= atk; }
    public bool IsCanBeSpecialSummoned(object e, int sumtype, int sumplayer, bool nocheck, bool nolimit, int pos = 0) { return true; }
    public bool IsReleasable() { return true; }
    public bool IsPreviousControler(int p) { return true; }
    public bool IsAbleToHand() { return true; }
    public bool IsPreviousPosition(int pos) { return true; }
    public bool IsDefensePos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense; }
    public bool IsSummonable(bool ignoreLimit, object param) { return true; }
    public bool IsMSetable(bool ignoreLimit, object param) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    public int GetFlagEffect(int id) { return 0; }
    public void RegisterFlagEffect(int id, int reset, int prop, int count) { }
    public void SetCardTarget(LuaCard tc) { }
    public bool IsStatus(int status) { return false; }
    public LuaCard GetFirstCardTarget() { return null; }
    public void SetTurnCounter(int ct) { }
    public int GetLabel() { return 0; }
    public void SetLabel(int ct) { }
    public bool IsCode(params int[] codes) 
    {
        int myId = 0;
        if (unityData != null) int.TryParse(unityData.id, out myId);
        foreach(var c in codes) if (myId == c) return true;
        return false;
    }

    public void RegisterEffect(LuaEffect e, bool forced = false)
    {
        registeredEffects.Add(e);
        Debug.Log($"[Lua] Efeito tipo {e.type} registrado em {unityData?.name}.");
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
    
    public Closure conditionFunc;
    public Closure costFunc;
    public Closure targetFunc;
    public Closure operationFunc;

    // O YGOPro permite que os efeitos sejam criados chamando a classe Effect direto
    public static LuaEffect CreateEffect(LuaCard c)
    {
        return new LuaEffect { owner = c };
    }

    public void SetType(int t) { type = t; }
    public void SetCode(int c) { code = c; }
    public void SetProperty(int p) { property = p; }
    public void SetDescription(string d) { description = d; }
    
    // Callbacks do Lua (Condição, Alvo, Resolução)
    public void SetCondition(Closure condition) { conditionFunc = condition; }
    public void SetCost(Closure cost) { costFunc = cost; }
    public void SetTarget(Closure target) { targetFunc = target; }
    public void SetOperation(Closure operation) { operationFunc = operation; }

    // Retorna a quem o efeito pertence (extremamente usado em Lua)
    public LuaCard GetHandler() { return owner; }
    public LuaCard GetOwner() { return owner; }
}

// ==============================================================================
// 4. CLASSE GROUP (Lista de Cartas selecionadas ou filtradas)
// Chamado no Lua como: g:GetFirst()
// ==============================================================================
[MoonSharpUserData]
public class LuaGroup
{
    public List<LuaCard> cards = new List<LuaCard>();

    public void AddCard(LuaCard c) { cards.Add(c); }
    public void RemoveCard(LuaCard c) { cards.Remove(c); }
    public LuaCard GetFirst() { return cards.Count > 0 ? cards[0] : null; }
    public int GetCount() { return cards.Count; }

    public bool IsExists(object filterFunc, int count, LuaCard excludedCard, params object[] extraArgs)
    {
        int matchCount = 0;
        foreach (var c in cards)
        {
            if (excludedCard != null && c == excludedCard) continue;
            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                DynValue result = closure.Call(callArgs.ToArray());
                if (result.Type == DataType.Boolean && result.Boolean) matchCount++;
            }
            else matchCount++;
        }
        return matchCount >= count;
    }

    public LuaGroup Filter(object filterFunc, LuaCard excludedCard, params object[] extraArgs)
    {
        LuaGroup newGroup = new LuaGroup();
        foreach (var c in cards)
        {
            if (excludedCard != null && c == excludedCard) continue;
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

    public DynValue Select(int player, int min, int max, LuaCard excludedCard)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        List<CardData> selectableData = new List<CardData>();
        foreach (var c in cards) {
            if (excludedCard != null && c == excludedCard) continue;
            if (c.unityData != null && !selectableData.Contains(c.unityData)) selectableData.Add(c.unityData);
        }
        if (GameManager.Instance != null && selectableData.Count > 0) {
            GameManager.Instance.OpenCardMultiSelection(selectableData, "Escolha um alvo do grupo", min, max, (selectedList) => {
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
            return DynValue.FromObject(CardEffectManager.Instance.luaEngine, new LuaGroup());
        }
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.Select") });
    }

    public void KeepAlive() { }
    public void Delete() { }
}