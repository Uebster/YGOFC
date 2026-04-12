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
    public LuaCard currentAttacker;
    public LuaCard currentAttackTarget;
    public LuaEffect currentActivatingEffect;

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
        Debug.Log($"[Surgical Log] Duel.Recover! Jogador_Raw: {player} | Amount_Raw: {amount}");
        int pInt = ConvertToInt(player);
        int aInt = ConvertToInt(amount);
        Debug.Log($"[Surgical Log] Convertido para C# -> Jogador: {pInt} | Cura: {aInt}");
        if (pInt == 0) GameManager.Instance.GainLifePoints(true, aInt);
        else GameManager.Instance.GainLifePoints(false, aInt);
    }

    public DynValue Destroy(object target, object reason)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        List<CardDisplay> toDestroy = new List<CardDisplay>();

        if (target is LuaGroup group)
        {
            foreach (var c in group.cards) if (c.unityCard != null) toDestroy.Add(c.unityCard);
            Debug.Log($"[Lua] Duel.Destroy(Grupo com {group.cards.Count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            toDestroy.Add(card.unityCard);
            Debug.Log($"[Lua] Duel.Destroy({card.unityCard.CurrentCardData.name})");
        }

        CardEffectManager.Instance.StartCoroutine(DestroyCardsRoutine(toDestroy));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Destroy") });
    }

    // --- MOVIMENTAÇÃO DE CARTAS ---

    public DynValue Draw(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);

        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        CardEffectManager.Instance.StartCoroutine(DrawRoutine(isPlayer, amt));
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Draw") });
    }

    private IEnumerator DrawRoutine(bool isPlayer, int amount)
    {
        if (isPlayer && GameManager.Instance != null && GameManager.Instance.canPlayerDrawFromDeck && !GameManager.Instance.isSimulating)
        {
            for (int i = 0; i < amount; i++)
            {
                int drawsLeft = amount - i;
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Efeito ativado: Compre {drawsLeft} carta(s) do seu Deck.");
                
                GameManager.Instance.pendingEffectDraws = 1;
                yield return new WaitWhile(() => GameManager.Instance.pendingEffectDraws > 0);
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                if (isPlayer) GameManager.Instance.DrawCard(true);
                else GameManager.Instance.DrawOpponentCard();
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.4f);
            }
        }
        
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(amount);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
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
                GameManager.Instance.SendToGraveyard(c.unityCard.CurrentCardData, c.unityCard.isPlayerCard, c.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand, SendReason.Effect);
                    if (c.unityCard.gameObject != null) GameObject.Destroy(c.unityCard.gameObject);
                    count++;
                }
            }
            Debug.Log($"[Lua] Duel.SendtoGrave(Grupo com {count} cartas)");
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
        GameManager.Instance.SendToGraveyard(card.unityCard.CurrentCardData, card.unityCard.isPlayerCard, card.unityCard.isOnField ? CardLocation.Field : CardLocation.Hand, SendReason.Effect);
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

    public void ShuffleHand(object player)
    {
        // A Unity não precisa embaralhar visualmente a mão porque a ordem das cartas é controlada nativamente pela lista.
        // Mas o LUA precisa desta função para não quebrar cartas como D.D. Designator ou Exchange.
        Debug.Log($"[Lua] Duel.ShuffleHand({player})");
    }

    public int DiscardHand(object player, object amount, object reason)
    {
        int amt = ConvertToInt(amount);
        bool isPlayer = IsPlayer(player);
        GameManager.Instance.DiscardRandomHand(isPlayer, amt, false);
        return amt;
    }

    public int GetLocationCount(object player, object location, params object[] extraArgs)
    {
        if (GameManager.Instance == null) return 0;
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        
        if (loc == 0x04) // LOCATION_MZONE
            return GameManager.Instance.GetFreeMonsterZones(isPlayer);
        if (loc == 0x08) // LOCATION_SZONE
            return GameManager.Instance.GetFreeSpellTrapZones(isPlayer);
            
        return 5; 
    }

    public int GetTurnPlayer()
    {
        // Retorna 0 se for o turno do Jogador Humano, 1 se for da IA
        return GameManager.Instance.isPlayerTurn ? 0 : 1;
    }

    public int GetPhase() { return GetCurrentPhase(); }

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

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) 
    { 
        int idx = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.currentChain.Count >= idx && idx > 0)
        {
            // Guarda a informação na memória do elo (Usado por Stardust Dragon, My Body as a Shield, etc)
            // CardEffectManager.Instance.currentChain[idx - 1].operationInfo = ...
        }
    }

    
    // Extensões Descobertas pelo Mass Validator
    public bool CheckReleaseGroupCost(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) { return true; }
    public bool IsTurnPlayer(object player) { return GetTurnPlayer() == ConvertToInt(player); }
    public int GetFieldGroupCount(object player, object location1, object location2) { return 0; }
    public bool IsEnvironment(object cardcode) { return false; }
    public bool IsPlayerCanDiscardDeck(object player, object count) { return true; }
    public bool IsPlayerCanRemove(object player) { return true; }
    public int GetCustomActivityCount(object counter_id, object player, object activity_type) { return 0; }
    public bool IsPlayerCanSpecialSummonMonster(object player, object code, params object[] args) { return true; }
    public int GetActivityCount(object player, object activity_type, params object[] args) 
    { 
        int type = ConvertToInt(activity_type);
        if (type == 2 && GameManager.Instance != null) // ACTIVITY_NORMALSUMMON
        {
            return IsPlayer(player) ? GameManager.Instance.normalSummonsThisTurnPlayer : GameManager.Instance.normalSummonsThisTurnOpponent;
        }
        return 0; 
    }
    public int GetCurrentChain() { return CardEffectManager.Instance != null ? CardEffectManager.Instance.currentChain.Count : 0; }
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
    public bool IsCanAddCounter(object player, object counterType, object count, object card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(object card) { return new LuaGroup(); }
    public int GetMatchingGroupCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }
    
    public int GetTargetCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }    
    
    public LuaGroup CheckReleaseGroup(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) { return new LuaGroup(); }
    public bool IsPlayerCanDiscardDeckAsCost(object player, object count) { return true; }
    public DynValue GetOperationInfo(object chainc, object category) { return DynValue.NewTuple(DynValue.NewBoolean(false), UserData.Create(new LuaGroup()), DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); }
    
    public DynValue SelectYesNo(object player, object desc)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true); // AI default Yes
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectYesNo") });
        }

        if (UIManager.Instance != null)
        {
            string msg = "Você deseja ativar este efeito?";
            if (desc is string s) msg = s;
            else if (desc is double d) msg = GetHintMessageString((int)d);
            else if (desc != null) msg = desc.ToString();
            
            UIManager.Instance.ShowConfirmation(msg, () => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }, () => {
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(false);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewBoolean(true);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectYesNo") });
    }

    public DynValue SelectOption(object player, params object[] options)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(0);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
        }

        // Placeholder genérico pois não temos UI de SelectOption pura, usamos o MultiSelection de cartas criando Dummies
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(0);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectOption") });
    }

    public DynValue SelectPosition(object player, object card, object pos)
    {
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        if (!IsPlayer(player) && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1); // POS_FACEUP_ATTACK
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectPosition") });
        }

        if (UIManager.Instance != null && card is LuaCard lc)
        {
            CardData data = lc.unityData ?? new CardData { name = "Card" };
            UIManager.Instance.ShowPositionSelection(data, (selectedPos) => {
                int res = selectedPos == CardDisplay.BattlePosition.Attack ? 1 : 4; 
                CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(res);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(1);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
        }

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectPosition") });
    }

    private string GetHintMessageString(int hintCode)
    {
        if (hintCode == 500) return "Selecione";
        if (hintCode == 501) return "Descarte";
        if (hintCode == 502) return "Confirme";
        if (hintCode == 503) return "Ativar";
        if (hintCode == 504) return "Tributo";
        if (hintCode == 506) return "Destruir";
        if (hintCode == 509) return "Special Summon";
        if (hintCode == 510) return "Retornar para a Mão";
        if (hintCode == 525) return "Setar";
        return $"Opção {hintCode}";
    }

    public int AnnounceNumber(object player, params object[] args) { return 1000; }
    public int AnnounceLevel(object player, params object[] args) { return 4; }
    public int AnnounceAttribute(object player, object count, object avail) { return 1; }
    public int AnnounceRace(object player, object count, object avail) { return 1; }
    public int AnnounceCard(object player, params object[] args) { return 0; }
    public bool IsChainNegatable(object chaincount) { return true; }
    public int GetOperationCount(object chainc) { return 0; }
    public bool IsChainDisablable(object chainc) { return true; }
    public void DiscardDeck(object player, object count, object reason) { }
    public bool CheckTribute(object card, object min, object max, object group = null, object zone = null) { return true; }
    public void SetTargetCard(object target) { }
    public void ClearTargetCard() { }
    public bool CheckEvent(object event_code, object chainc = null) { return false; }

    public void RaiseEvent(object triggerCard, object eventCode, object eg, object ep, object ev, object re, object r)
    {
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.TriggerLuaEvent(ConvertToInt(eventCode), triggerCard);
    }

    public void RaiseSingleEvent(object triggerCard, object eventCode, object e, object ep, object ev, object re, object r)
    {
        if (triggerCard is LuaCard lc && CardEffectManager.Instance != null)
        {
            int code = ConvertToInt(eventCode);
            var matchingEffects = lc.registeredEffects.FindAll(eff => eff.code == code);
            
            foreach(var effect in matchingEffects)
            {
                if (CardEffectManager.Instance.CanActivateEffect(lc, effect, lc.GetControler(), null))
                {
                    CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.chainManager.BuildAndResolveChainRoutine(lc, effect, null, lc.GetControler(), null));
                }
            }
        }
    }

    public void BreakEffect()
    {
        // Ponto de quebra de resolução de corrente (usado para Miss Timing em jogos reais).
    }

    // Stubs vitais capturados pelo relatório (Counter Traps e UX)
    public void HintSelection(object group) { }
    public bool NegateActivation(object chainc) 
    { 
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public bool NegateEffect(object chainc) 
    { 
        if (CardEffectManager.Instance != null) return CardEffectManager.Instance.NegateChainLink(ConvertToInt(chainc));
        return true; 
    }
    public void ChangeChainOperation(object chainc, object op) { }

    
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
    
    public DynValue GetChainInfo(object chainc, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
    {
         Debug.Log($"[Surgical Log] GetChainInfo invocado! arg1: {arg1}, arg2: {arg2}");
         List<DynValue> returns = new List<DynValue>();
         List<object> argsList = new List<object>();
         if (arg1 != null) argsList.Add(arg1);
         if (arg2 != null) argsList.Add(arg2);
         if (arg3 != null) argsList.Add(arg3);
         if (arg4 != null) argsList.Add(arg4);

        foreach (object o in argsList)
        {
            int arg = ConvertToInt(o);
            if (arg == 1) // CHAININFO_TARGET_PLAYER
            {
                Debug.Log($"[Surgical Log] Retornando targetPlayer: {targetPlayer}");
                returns.Add(DynValue.NewNumber(targetPlayer));
            }
            else if (arg == 2) // CHAININFO_TARGET_PARAM
            {
                Debug.Log($"[Surgical Log] Retornando targetParam: {targetParam}");
                returns.Add(DynValue.NewNumber(targetParam));
            }
            else if (arg == 16 || arg == 8388608) // CHAININFO_TARGET_CARDS (0x10)
            {
                LuaGroup g = currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
                returns.Add(UserData.Create(g));
            }
            else if (arg == 64 || arg == 128 || arg == 32) // TRIGGERING_EFFECT (0x40)
            {
                LuaEffect dummyEff = new LuaEffect { 
                    owner = SafeDummyCard(),
                    conditionFunc = CardEffectManager.Instance.dummyClosureTrue,
                    costFunc = CardEffectManager.Instance.dummyClosureTrue,
                    targetFunc = CardEffectManager.Instance.dummyClosureTrue,
                    operationFunc = CardEffectManager.Instance.dummyClosureTrue
                };
                returns.Add(UserData.Create(dummyEff));
            }
            else
            {
                returns.Add(DynValue.Nil);
            }
        }
        if (returns.Count == 0) return DynValue.NewTuple(DynValue.Nil, DynValue.Nil);
        if (returns.Count == 1) return returns[0];
        Debug.Log($"[Surgical Log] GetChainInfo enviando um Tuple de volta ao LUA com {returns.Count} valores.");
        return DynValue.NewTuple(returns.ToArray());
    }

    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public LuaEffect GetPlayerEffect(object player, object effect_code) { return new LuaEffect { owner = SafeDummyCard() }; }

    public LuaCard GetFirstTarget()
    {
        return (currentTargetGroup != null && currentTargetGroup.GetFirst() != null) ? currentTargetGroup.GetFirst() : SafeDummyCard();
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
        if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.TributeCard(c.unityCard);
        }
        else if (target is LuaCard card && card.unityCard != null)
        {
            GameManager.Instance.TributeCard(card.unityCard);
        }
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
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, false, IsPlayer(player), false, c.unityData.level >= 5, null);
        }
    }

    public void MSet(object player, object card, object ignoreLimit, object param)
    {
        if (card is LuaCard c && c.unityCard != null) 
        {
            if (IsPlayer(player)) GameManager.Instance.normalSummonsThisTurnPlayer++;
            else GameManager.Instance.normalSummonsThisTurnOpponent++;
            
            GameManager.Instance.FinalizeSummon(c.unityCard.gameObject, c.unityData, true, IsPlayer(player), true, c.unityData.level >= 5, null);
        }
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
        return currentAttacker ?? SafeDummyCard();
    }

    public LuaCard GetAttackTarget()
    {
        return currentAttackTarget ?? SafeDummyCard();
    }

    public void CalculateDamage(object attackerObj, object defenderObj)
    {
        LuaCard attacker = attackerObj as LuaCard;
        LuaCard defender = defenderObj as LuaCard;
        
        if (attacker == null || attacker.unityCard == null) return;

        CardDisplay atkCard = attacker.unityCard;
        CardDisplay defCard = defender != null ? defender.unityCard : null;

        int atkPower = atkCard.currentAtk;
        bool atkIsPlayer = atkCard.isPlayerCard;
        
        if (defCard == null)
        {
            // Ataque Direto
            if (atkIsPlayer) GameManager.Instance.DamageOpponent(atkPower);
            else GameManager.Instance.DamagePlayer(atkPower);
        }
        else
        {
            int defPower = defCard.position == CardDisplay.BattlePosition.Attack ? defCard.currentAtk : defCard.currentDef;
            bool defIsPlayer = defCard.isPlayerCard;

            if (defCard.position == CardDisplay.BattlePosition.Attack)
            {
                if (atkPower > defPower)
                {
                    if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                    else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayDestruction(atkCard);
                        DuelFXManager.Instance.PlayDestruction(defCard);
                    }
                    GameManager.Instance.MoveCard(atkCard, CardLocation.Graveyard, SendReason.Battle);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
            }
            else // Defesa
            {
                if (atkPower > defPower)
                {
                    if (atkCard.hasPiercing)
                    {
                        if (defIsPlayer) GameManager.Instance.DamagePlayer(atkPower - defPower);
                        else GameManager.Instance.DamageOpponent(atkPower - defPower);
                    }
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(defCard);
                    GameManager.Instance.MoveCard(defCard, CardLocation.Graveyard, SendReason.Battle);
                }
                else if (atkPower < defPower)
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                }
                else
                {
                    // Equal ATK and DEF: reflect VFX but no damage
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayAttackFail(atkCard);
                        DuelFXManager.Instance.PlayDefenseSuccessEffect(defCard);
                    }
                }
            }
        }
    }

    public void SSet(object player, object target, params object[] extraArgs)
    {
        if (target is LuaCard card && card.unityCard != null)
            GameManager.Instance.PlaySpellTrap(card.unityCard.gameObject, card.unityData, true);
        else if (target is LuaGroup group)
        {
            foreach (var c in group.cards)
                if (c.unityCard != null) GameManager.Instance.PlaySpellTrap(c.unityCard.gameObject, c.unityData, true);
        }
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

        // Otimização: Se há apenas um alvo possível, seleciona automaticamente (a menos que min > 1) OU se alwaysConfirmSingleTarget for true
        if (!GameManager.Instance.alwaysConfirmSingleTarget && candidates.cards.Count == 1 && ConvertToInt(min) <= 1)
        {
            LuaGroup selectedGroup = new LuaGroup();
            selectedGroup.AddCard(candidates.cards[0]);
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
            this.currentTargetGroup = selectedGroup;
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("SelectTarget") });
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

        // Extrai CardData dos candidatos para a UI de seleção
        List<CardData> selectableData = new List<CardData>();
        foreach (var c in candidates.cards)
        {
            if (c.unityData != null && !selectableData.Contains(c.unityData)) selectableData.Add(c.unityData);
        }

        if (GameManager.Instance != null && selectableData.Count > 0)
        {
            GameManager.Instance.OpenCardMultiSelection(selectableData, GetHintMessageString(500), ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                LuaGroup selectedGroup = new LuaGroup();
                foreach (var data in selectedList)
                {
                    LuaCard match = candidates.cards.Find(lc => lc.unityData == data);
                    if (match != null) selectedGroup.AddCard(match);
                    else selectedGroup.AddCard(new LuaCard(data)); // Fallback se não encontrar a instância original
                }
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                this.currentTargetGroup = selectedGroup; // Salva para o GetFirstTarget()
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            });
        }
        else
        {
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(new LuaGroup());
            CardEffectManager.Instance.isWaitingForLuaYield = false;
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
        int count = 0;
        foreach (var c in cards)
        {
            if (c != null && c.isOnField)
            {
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(c);
                GameManager.Instance.SendToGraveyard(c.CurrentCardData, c.isPlayerCard, CardLocation.Field, SendReason.Effect);
                GameObject.Destroy(c.gameObject);
                count++;
                
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                    yield return new WaitForSeconds(0.25f);
            }
        }
        CardEffectManager.Instance.yieldReturnValue = DynValue.NewNumber(count);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }
}

// ==============================================================================
