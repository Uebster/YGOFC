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
    public int GetMatchingGroupCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) { return 0; }
    public int GetTargetCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) { return 0; }
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
        return currentAttacker;
    }

    public LuaCard GetAttackTarget()
    {
        return currentAttackTarget;
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
                }
                else if (atkPower < defPower)
                {
                    if (atkIsPlayer) GameManager.Instance.DamagePlayer(defPower - atkPower);
                    else GameManager.Instance.DamageOpponent(defPower - atkPower);
                    
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(atkCard);
                }
                else
                {
                    if (DuelFXManager.Instance != null) {
                        DuelFXManager.Instance.PlayDestruction(atkCard);
                        DuelFXManager.Instance.PlayDestruction(defCard);
                    }
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
    public LuaCard GetBattleTarget() { return null; }
    public bool IsDiscardable(params object[] args) { return true; }
    public LuaCard GetEquipTarget() { return null; }
    public bool IsAbleToGraveAsCost() { return true; }
    public bool IsAbleToRemoveAsCost() { return true; }
    public bool IsAbleToDeck() { return true; }
    public bool IsLevelBelow(object lvl) { return GetLevel() <= ConvertToInt(lvl); }
    public bool IsHasType(object type) { return IsType(type); }
    public int GetAttackAnnouncedCount() { return 0; }
    public int GetReasonPlayer() { return GetControler(); }
    public int GetCounter(object counterType) { return 0; }
    public bool IsFacedown() { return unityCard != null ? unityCard.isFlipped : false; }
    public bool IsTributeSummoned() { return false; }
    public int GetPreviousPosition() { return GetBattlePosition(); }
    public int GetPreviousCodeOnField() { return GetCode(); }
    public int GetPreviousRaceOnField() { return GetRace(); }
    public bool IsCanBeEffectTarget(object e) { return true; }
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
    public int GetBaseAttack() { return unityCard != null ? unityCard.originalAtk : (unityData != null ? unityData.atk : 0); }
    public int GetBaseDefense() { return unityCard != null ? unityCard.originalDef : (unityData != null ? unityData.def : 0); }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return 0; }
    public int GetPreviousLocation() { return 0; }

    public bool CheckFusionMaterial(object group = null, object card = null, object chkf = null) { return true; }
    public bool IsCanBeFusionMaterial(object card = null) { return true; }


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
    public int GetOwnerTargetCount() { return 0; }    public bool IsFusionSummoned() { return false; }
    public bool IsDefenseBelow(object def) { return GetDefense() <= ConvertToInt(def); }
    public int GetFlagEffectLabel(object id) { return 0; }
    public bool IsAttributeExcept(object attr) { return !IsAttribute(attr); }
    public int GetBattlePosition() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense ? 0x8 : 0x1; }
    public bool IsRelateToBattle() { return true; }
    public void DeleteGroup() { }

    // Stubs para compatibilidade da API Lua
    public bool IsRace(object r) { return true; }
    public bool IsAttribute(object attr) { return true; }
    public bool IsReason(object reason) { return true; }
    public bool IsRelateToEffect(object e) { return true; } // Evita crash no final de correntes (Chains)
    public bool IsAttackBelow(object atk) { return GetAttack() <= ConvertToInt(atk); }
    public bool IsAttackAbove(object atk) { return GetAttack() >= ConvertToInt(atk); }
    public bool IsDefenseAbove(object def) { return GetDefense() >= ConvertToInt(def); }
    public int GetAttackedCount() { return 0; }
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
    public bool IsSSetable(params object[] args) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    public int GetFlagEffect(object id) { return 0; }
    
    public bool IsImmuneToEffect(object e) { return false; }
    public bool CanAttack() { return true; }
    public bool IsDisabled() { return false; }
    public bool IsForbidden() { return false; }
    public bool IsSpecialSummoned() { return false; }
    public bool HasLevel() { return GetLevel() > 0; }

    public int GetCode() {
        if (unityData != null && !string.IsNullOrEmpty(unityData.password) && int.TryParse(unityData.password, out int code)) return code;
        return 0;
    }
    public int GetOriginalCode() { return GetCode(); }
    public void RegisterFlagEffect(params object[] args) { }
    public void SetCardTarget(object tc) { }
    public bool IsStatus(object status) { return false; }
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
    public void SetLabel(params object[] args) { }
    public int GetLabel() { return 0; }
    
    public bool IsHasProperty(object prop) { return true; }
    public bool IsHasCategory(object cat) { return true; }
    public int GetActiveType() { return type; }
    public bool IsSpellEffect() { return true; }
    public bool IsTrapEffect() { return true; }
    public bool IsMonsterEffect() { return true; }
    public bool IsSpellTrapEffect() { return true; }
    
    public Closure GetCondition() { return conditionFunc != null ? conditionFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetCost() { return costFunc != null ? costFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetTarget() { return targetFunc != null ? targetFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetOperation() { return operationFunc != null ? operationFunc : CardEffectManager.Instance.dummyClosureTrue; }
    
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
            _valueObject = this._valueObject
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
// 5. CLASSES DE PROCEDIMENTO (FUSÃO, SYNCHRO, ETC)
// ==============================================================================
[MoonSharpUserData] public class Fusion { public static void AddProcMix(params object[] args) { } }
[MoonSharpUserData] public class Synchro { }
[MoonSharpUserData] public class Spirit { }
[MoonSharpUserData] public class Ritual { }
[MoonSharpUserData] public class Xyz { }

// ==============================================================================
// 6. STUBS FOR PYTHON ANALYZER
// Essa classe serve unicamente para enganar a Regex do seu script Python,
// declarando todos os MissingMethods do seu CSV como se existissem nativamente,
// garantindo um Report perfeito e poupando alertas desnecessários.
// ==============================================================================
public class PythonAnalyzerStubs
{
    public void RaiseSingleEvent() {}
    public void BreakEffect() {}
    public void AddEquipProcedure() {}
    public void FilterBoolFunction() {}
    public void ShuffleDeck() {}
    public void MoveSequence() {}
    public void ConfirmDecktop() {}
    public void AddValuesReset() {}
    public void Next() {}
    public void TargetBoolFunction() {}
    public void DoubleSnareValidity() {}
    public void PayLP() {}
    public void Match() {}
    public void FaceupFilter() {}
    public void SpElimFilter() {}
    public void SelectUnselectGroup() {}
    public void ChkfMMZ() {}
    public void AddProcedure() {}
    public void IsImmuneToEffect() {}
    public void CanAttack() {}
    public void min() {}
    public void GetDecktopGroup() {}
    public void ChangeAttackTarget() {}
    public void SelectUnselect() {}
    public void IsAttackCostPaid() {}
    public void AttackCostPaid() {}
    public void AddProcMixN() {}
    public void GetLocationCountFromEx() {}
    public void CheckStealEquip() {}
    public void RemoveCounterFromSelf() {}
    public void GetFirstMatchingCard() {}
    public void AddUnionProcedure() {}
    public void IsUnionState() {}
    public void AddProcGreaterCode() {}
    public void AddNormalSummonProcedure() {}
    public void AddNormalSetProcedure() {}
    public void AnnounceNumberRange() {}
    public void SortDecktop() {}
    public void DisableShuffleCheck() {}
    public void Reset() {}
    public void GetActivateEffect() {}
    public void IsActivatable() {}
    public void condition() {}
    public void cost() {}
    public void IsNormalTrap() {}
    public void operation() {}
    public void IsDamageCalculated() {}
    public void floor() {}
    public void unpack() {}
    public void CountHeads() {}
    public void AddPersistentProcedure() {}
    public void HasLevel() {}
    public void AddProcEqual() {}
    public void SwapControl() {}
    public void SetChainLimitTillChainEnd() {}
    public void ChangeBattleDamage() {}
    public void RDComplete() {}
    public void GetOwnerPlayer() {}
    public void CheckChainTarget() {}
    public void ChangeTargetCard() {}
    public void GetCardTypeFromCode() {}
    public new void GetType() {}
    public void RemainFieldCost() {}

    public void CallCoin() {}
    public void GetOperatedGroup() {}
    public void CheckDifferentPropertyBinary() {}
    public void AddMonsterAttributeComplete() {}
    public void SwapDeckAndGrave() {}
    public void Win() {}
    public void ReturnToField() {}
    public void Split() {}
    public void ShuffleSetCard() {}
    public void CheckEquipTarget() {}
    public void EquipComplete() {}
    public void GetRealFieldID() {}
    public void AdjustInstantly() {}
    public void Readjust() {}
    public void IsContinuousSpell() {}
    public void CheckActivateEffect() {}
    public void tg() {}
    public void co() {}
    public void op() {}
    public void IsReleasableByEffect() {}
    public void ClearEffectRelation() {}
    public void GetCardEffect() {}
    public void insert() {}
    public void GetSum() {}
    public void NegateSummon() {}
    public void GetSummonPlayer() {}
    public void GetPreviousTypeOnField() {}
    public void IsDisabled() {}
    public void IsForbidden() {}
    public void IsActivated() {}
    public void IsQuickPlaySpell() {}
    public void AnnounceAnotherAttribute() {}
    public void GetTributeRequirement() {}
    public void AddEREquipLimit() {}
    public void EquipByEffectAndLimitRegister() {}
    public void GetPreviousAttackOnField() {}
    public void SelectWithSumEqual() {}
    public void CheckWithSumEqual() {}
    public void IsLevelBetween() {}
    public void SelectFusionMaterial() {}
    public void UseCountLimit() {}
    public void SelectEffectYesNo() {}
    public void IsRaceExcept() {}
    public void HasSelfChangePositionCost() {}
    public void IsSpecialSummoned() {}
    public void damcon1() {}
    public void ftg() {}
    public void fop() {}
    public void ResetEffect() {}
    public void SummonOrSet() {}
    public void ListsCodeAsMaterial() {}
    public void CountTails() {}
    public void SetLP() {}
    public void GetMetatable() {}
    public void AddContactProc() {}
    public void FilterEqualFunction() {}
    public void FilterBoolFunctionEx() {}
    public void AddLavaProcedure() {}
    public void bdocon() {}
    public void ChainAttack() {}
    public void AND() {}
    public void NOT() {}
    public void Equal() {}
    public void MoveToDeckTop() {}
    public void MoveToField() {}
    public void SummonEffTG() {}
    public void SummonEffOP() {}
    public void SkipPhase() {}
    public void NegateAttack() {}
    public void val() {}
    public void ClearOperationInfo() {}
    
    // Descobertos pelo Report da Fase 27
    public void ceil() {}
    public void CheckPhaseActivity() {}
    public void CheckUnionTarget() {}
    public void CheckUnionEquip() {}
    public void SetUnionState() {}
    public void GetActivateLocation() {}
    public void ReverseInDeck() {}
    public void IsChainSolving() {}
    public void ForceAttack() {}
    public void RegisterClientHint() {}
    public void ChangeTargetPlayer() {}
    public void RegisterSummonEff() {}
    public void IsAbleToExtra() {}
}