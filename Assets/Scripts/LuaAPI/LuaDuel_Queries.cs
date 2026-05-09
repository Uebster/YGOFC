using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public partial class LuaDuel
{
    // ==============================================================================
    // QUERIES, RADARES E BUSCAS
    // Funções passivas que escaneiam o tabuleiro e retornam dados sem abrir janelas.
    // ==============================================================================

    public int GetLocationCount(object player, object location, params object[] extraArgs)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return 0;
        int loc = ConvertToInt(location);
        
        int use_player = ConvertToInt(player);
        int reason = 0x1; // LOCATION_REASON_TOFIELD
        int uzone = 0xff; // Máscara padrão permitindo todas as zonas (11111)

        if (extraArgs != null)
        {
            if (extraArgs.Length > 0 && extraArgs[0] != null && !(extraArgs[0] is MoonSharp.Interpreter.DynValue dv1 && dv1.IsNil())) use_player = ConvertToInt(extraArgs[0]);
            if (extraArgs.Length > 1 && extraArgs[1] != null && !(extraArgs[1] is MoonSharp.Interpreter.DynValue dv2 && dv2.IsNil())) reason = ConvertToInt(extraArgs[1]);
            if (extraArgs.Length > 2 && extraArgs[2] != null && !(extraArgs[2] is MoonSharp.Interpreter.DynValue dv3 && dv3.IsNil())) uzone = ConvertToInt(extraArgs[2]);
        }
        
        bool checkPlayer = (use_player == 0);
        int count = 0;

        if ((loc & 0x04) != 0) // LOCATION_MZONE
        {
            Transform[] zones = checkPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            for (int i = 0; i < zones.Length; i++) { if ((uzone & (1 << i)) != 0 && zones[i].childCount == 0) count++; }
        }
        if ((loc & 0x08) != 0) // LOCATION_SZONE
        {
            Transform[] zones = checkPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            for (int i = 0; i < zones.Length; i++) { if ((uzone & (1 << i)) != 0 && zones[i].childCount == 0) count++; }
        }
            
        return count;
    }

    public int GetLocationCountFromEx(object player, object target_player, object group, object scard, object zone = null)
    {
        // Retorna a contagem de zonas livres considerando a Extra Monster Zone (Regra Master Rule 4+)
        int p = ConvertToInt(target_player);
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return 0;
        // Fallback simplificado: Conta as zonas MZONE normais para garantir compatibilidade com as invocações
        return GetLocationCount(p, 0x04);
    }

    public int GetUsableMZoneCount(object player, object uzone = null)
    {
        int p = ConvertToInt(player);
        return GetLocationCount(p, 0x04);
    }

    public int GetTributeCount(object card, object group = null, object zone = null)
    {
        LuaCard lc = card as LuaCard;
        if (lc != null && lc.IsHasEffect(150).Type != DataType.Nil) return 2; // EFFECT_DOUBLE_TRIBUTE
        return 1;
    }

    public void AddCustomActivityCounter(object counter_id, object activity_type, object filter) { Debug.LogWarning("[LUA STUB] AddCustomActivityCounter chamado"); }
    public void EnableGlobalFlag(object flag) { globalFlags |= ConvertToInt(flag); }

    public Dictionary<int, ChainManager.OperationInfo> _opInfoCache = new Dictionary<int, ChainManager.OperationInfo>();

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) 
    { 
        int cat = ConvertToInt(category);
        int idx = ConvertToInt(chainc);

        ChainManager.OperationInfo info = new ChainManager.OperationInfo {
            category = cat,
            count = ConvertToInt(count),
            player = ConvertToInt(player),
            param = ConvertToInt(param)
        };

        if (target is LuaGroup g) info.targetGroup = g;
        else if (target is LuaCard c) 
        {
            LuaGroup newG = new LuaGroup();
            newG.AddCard(c);
            info.targetGroup = newG;
        }

        if (idx == 0)
        {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.isChainResolving && CardEffectManager.Instance.chainManager.resolvingLink != null)
            {
                CardEffectManager.Instance.chainManager.resolvingLink.opInfo[cat] = info;
            }
            else
            {
                _opInfoCache[cat] = info;
            }
        }
        else if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            var link = CardEffectManager.Instance.chainManager.currentChain.Find(l => l.chainIndex == idx);
            if (link != null) link.opInfo[cat] = info;
        }
    }

    // Extensões Descobertas pelo Mass Validator
    public bool CheckReleaseGroupCost(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) 
    { 
        int locSelf = 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0);
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, 0, excluded, extraArgs);
        return group.GetCount() >= ConvertToInt(count); 
    }    
    public int GetFieldGroupCount(object player, object location1, object location2) 
    { 
        return GetMatchingGroup(null, player, location1, location2, null).GetCount(); 
    }
    public int GetPlayersCount(object p) { return 2; }

    public bool IsEnvironment(object cardcode) { return false; }
    public bool IsPlayerCanDiscardDeck(object player, object count) { return true; }
    public bool IsPlayerCanRemove(object player) { return true; }
    public int GetCustomActivityCount(object counter_id, object player, object activity_type) { return 0; }
    public bool IsPlayerCanSpecialSummonMonster(object player, object code, params object[] args) { return true; }
    public int GetActivityCount(object player, object activity_type, params object[] args) 
    { 
        int type = ConvertToInt(activity_type);
        bool isPlayer = IsPlayer(player);
        if (GameManager.Instance == null) return 0;

        if (type == 1) // ACTIVITY_SUMMON (All summons)
            return isPlayer ? GameManager.Instance.normalSummonsThisTurnPlayer + GameManager.Instance.specialSummonsThisTurnPlayer : GameManager.Instance.normalSummonsThisTurnOpponent + GameManager.Instance.specialSummonsThisTurnOpponent;
        if (type == 2) // ACTIVITY_NORMALSUMMON
            return isPlayer ? GameManager.Instance.normalSummonsThisTurnPlayer : GameManager.Instance.normalSummonsThisTurnOpponent;
        if (type == 3) // ACTIVITY_SPSUMMON
            return isPlayer ? GameManager.Instance.specialSummonsThisTurnPlayer : GameManager.Instance.specialSummonsThisTurnOpponent;
        if (type == 4) // ACTIVITY_FLIPSUMMON
            return isPlayer ? GameManager.Instance.flipSummonsThisTurnPlayer : GameManager.Instance.flipSummonsThisTurnOpponent;
        if (type == 5) // ACTIVITY_ATTACK
            return isPlayer ? GameManager.Instance.attacksThisTurnPlayer : GameManager.Instance.attacksThisTurnOpponent;
        if (type == 6) // ACTIVITY_BATTLE_PHASE
            return (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle) ? 1 : 0;

        return 0; 
    }
    public int GetCurrentChain() { return CardEffectManager.Instance != null ? CardEffectManager.Instance.currentChain.Count : 0; }
    public bool IsAbleToEnterBP() { return true; }
    public bool IsBattlePhase() { return PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle; }
    public bool IsMainPhase() { return PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2); }
    public bool IsPlayerAffectedByEffect(object player, object effect_code) 
    { 
        int p = ConvertToInt(player);
        int code = ConvertToInt(effect_code);
        
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            // Verifica auras globais que afetam o jogador
            foreach(var eff in CardEffectManager.Instance.luaDuel.globalEffects)
            {
                if (eff.code == code) return true;
            }
        }
        
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
        {
            return CardEffectManager.Instance.auraManager.IsPlayerAffectedBy(p, code);
        }
        return false; 
    }
    public LuaGroup GetFieldGroup(object player, object loc1, object loc2) 
    { 
        return GetMatchingGroup(null, player, loc1, loc2, null); 
    }
    public bool IsCanRemoveCounter(object player, object s, object o, object counterType, object count, object reason) { return true; }
    public bool CheckEvent(object event_code) { return false; }

    public void RegisterFlagEffect(object player, object flag, params object[] extraArgs)
    {
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        if (playerFlags.ContainsKey(key)) playerFlags[key]++;
        else playerFlags[key] = 1;
        // Debug.Log($"[LuaDuel] Flag Effect Registrado: Jogador {player}, Flag {flag}");
    }

    public int GetFlagEffect(object player, object flag)
    {
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        int result = playerFlags.ContainsKey(key) ? playerFlags[key] : 0;
        // Debug.Log($"[LuaDuel] GetFlagEffect consultado: Jogador {player}, Flag {flag} -> Resultado: {result}");
        return result;
    }

    public bool HasFlagEffect(object player, object flag) 
    { 
        string key = $"{ConvertToInt(player)}_{ConvertToInt(flag)}";
        bool result = playerFlags.ContainsKey(key) && playerFlags[key] > 0;
        // Debug.Log($"[LuaDuel] HasFlagEffect consultado: Jogador {player}, Flag {flag} -> Resultado: {result}");
        return result;
    }

    public bool IsPlayerCanSpecialSummon(object player) { return true; }
    
    public int GetMZoneCount(object player, object releasedGroup = null, object reasonPlayer = null) 
    { 
        int p = ConvertToInt(player);
        int count = GetLocationCount(p, 0x04);
        
        if (releasedGroup is LuaGroup rg)
        {
            foreach (var c in rg.cards)
            {
                if (c.unityCard != null && c.unityCard.isOnField && c.GetControler() == p && c.IsLocation(0x04))
                    count++; // A engine adivinha que a zona vai abrir espaço!
            }
        }
        return count; 
    }
    public bool IsCanAddCounter(object player, object counterType, object count, object card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(object card) { return new LuaGroup(); }

    public LuaGroup GetRitualMaterial(object player, object nocheck = null)
    {
        if (RitualManager.Instance != null) return RitualManager.Instance.GetRitualMaterial(ConvertToInt(player), nocheck);
        return new LuaGroup();
    }

    public int GetMatchingGroupCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }
    
    public int GetTargetCount(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs) 
    { 
        return GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs).GetCount(); 
    }    

    public bool CheckReleaseGroup(object player, object filterFunc, object count, object use_hand, object excluded, params object[] extraArgs) 
    { 
        int locSelf = 0x04 | (ConvertToInt(use_hand) != 0 ? 0x02 : 0);
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, 0, excluded, extraArgs);
        return group.GetCount() >= ConvertToInt(count); 
    }
    public bool IsPlayerCanDiscardDeckAsCost(object player, object count) { return true; }

    public DynValue GetOperationInfo(object chainc, object category) 
    { 
        int cat = ConvertToInt(category);
        int idx = ConvertToInt(chainc);
        ChainManager.OperationInfo info = null;

        if (idx == 0)
        {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            {
                if (CardEffectManager.Instance.chainManager.resolvingLink.opInfo.ContainsKey(cat))
                    info = CardEffectManager.Instance.chainManager.resolvingLink.opInfo[cat];
            }
            else if (_opInfoCache.ContainsKey(cat))
            {
                info = _opInfoCache[cat];
            }
        }
        else if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            var link = CardEffectManager.Instance.chainManager.currentChain.Find(l => l.chainIndex == idx);
            if (link != null && link.opInfo.ContainsKey(cat))
                info = link.opInfo[cat];
        }

        if (info != null)
        {
            return DynValue.NewTuple(DynValue.NewBoolean(true), UserData.Create(info.targetGroup ?? new LuaGroup()), DynValue.NewNumber(info.count), DynValue.NewNumber(info.player), DynValue.NewNumber(info.param));
        }
        
        return DynValue.NewTuple(DynValue.NewBoolean(false), UserData.Create(new LuaGroup()), DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); 
    }

    public void ClearOperationInfo() 
    {
        _opInfoCache.Clear();
    }

    public bool IsChainNegatable(object chaincount) { return true; }
    public int GetOperationCount(object chainc) { return 0; }
    public bool IsChainDisablable(object chainc) { return true; }
    public void DiscardDeck(object player, object count, object reason) 
    { 
        int pInt = ConvertToInt(player);
        int cInt = ConvertToInt(count);
        int rInt = ConvertToInt(reason);
        if (pInt == 0 || pInt == 3) GameManager.Instance.MillCards(true, cInt); // Avise-me se atualizar o GameManager para aceitar rInt
        if (pInt == 1 || pInt == 3) GameManager.Instance.MillCards(false, cInt);
    }
    public bool CheckTribute(object card, object min, object max, object group = null, object zone = null) { return true; }
    public void SetTargetCard(object target) { 
        if (currentTargetGroup == null) currentTargetGroup = new LuaGroup();
        if (target is LuaGroup g) currentTargetGroup.cards.AddRange(g.cards);
        else if (target is LuaCard c) currentTargetGroup.AddCard(c);
    }
    
    public LuaGroup _selectedCardGroup;
    public void SetSelectedCard(object target) {
        _selectedCardGroup = new LuaGroup();
        if (target is LuaGroup g) _selectedCardGroup.cards.AddRange(g.cards);
        else if (target is LuaCard c) _selectedCardGroup.AddCard(c);
    }
    
    public void ClearTargetCard() { 
        currentTargetGroup = new LuaGroup(); 
    }
    public bool CheckEvent(object event_code, object chainc = null) { return false; }
    public int GetDrawCount(params object[] args) { return 1; }

    public bool IsPlayerCanDraw(object player, object amount = null)
    {
        int pInt = ConvertToInt(player);
        int amt = amount == null ? 1 : ConvertToInt(amount);
        int req = amt > 0 ? amt : 1;
        if (pInt == 3) {
            return GameManager.Instance.GetPlayerMainDeck().Count >= req && GameManager.Instance.GetOpponentMainDeck().Count >= req;
        }
        int deckCount = pInt == 0 ? GameManager.Instance.GetPlayerMainDeck().Count : GameManager.Instance.GetOpponentMainDeck().Count;
        return deckCount >= req;
    }
    public DynValue GetChainInfo(object chainc, object arg1 = null, object arg2 = null, object arg3 = null, object arg4 = null)
    {
         // Debug.Log($"[Surgical Log] GetChainInfo invocado! arg1: {arg1}, arg2: {arg2}");
         List<DynValue> returns = new List<DynValue>();
         List<object> argsList = new List<object>();
         if (arg1 != null) argsList.Add(arg1);
         if (arg2 != null) argsList.Add(arg2);
         if (arg3 != null) argsList.Add(arg3);
         if (arg4 != null) argsList.Add(arg4);

        int targetPl = targetPlayer;
        int targetPa = targetParam;
        LuaGroup targGr = currentTargetGroup;
        ChainManager.ChainLink targetLink = null;
        
        int chainIndex = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null)
        {
            if (chainIndex == 0 && CardEffectManager.Instance.chainManager.resolvingLink != null)
            {
                targetPl = CardEffectManager.Instance.chainManager.resolvingLink.targetPlayer;
                targetPa = CardEffectManager.Instance.chainManager.resolvingLink.targetParam;
                targGr = CardEffectManager.Instance.chainManager.resolvingLink.targetGroup;
                targetLink = CardEffectManager.Instance.chainManager.resolvingLink;
            }
            else if (chainIndex > 0)
            {
                var specificLink = CardEffectManager.Instance.chainManager.currentChain.Find(l => l.chainIndex == chainIndex);
                if (specificLink != null)
                {
                    targetPl = specificLink.targetPlayer;
                    targetPa = specificLink.targetParam;
                    targGr = specificLink.targetGroup;
                    targetLink = specificLink;
                }
            }
        }

        foreach (object o in argsList)
        {
            int arg = ConvertToInt(o);
            if (arg == 9) // CHAININFO_TARGET_PLAYER
            {
                returns.Add(DynValue.NewNumber(targetPl));
            }
            else if (arg == 10) // CHAININFO_TARGET_PARAM
            {
                returns.Add(DynValue.NewNumber(targetPa));
            }
            else if (arg == 8) // CHAININFO_TARGET_CARDS
            {
                LuaGroup g = targGr != null ? targGr : new LuaGroup();
                returns.Add(UserData.Create(g));
            }
            else if (arg == 1) // CHAININFO_TRIGGERING_EFFECT
            {
                LuaEffect eff = targetLink != null ? targetLink.effect : new LuaEffect { owner = SafeDummyCard() };
                returns.Add(UserData.Create(eff));
            }
            else if (arg == 2 || arg == 3) // CHAININFO_TRIGGERING_PLAYER ou CONTROLER
            {
                int p = targetLink != null ? targetLink.player : 0;
                returns.Add(DynValue.NewNumber(p));
            }
            else if (arg == 4) // CHAININFO_TRIGGERING_LOCATION
            {
                int loc = targetLink != null && targetLink.card != null ? targetLink.card.GetLocation() : 0;
                returns.Add(DynValue.NewNumber(loc));
            }
            else if (arg == 5) // CHAININFO_TRIGGERING_LOCATION_SYMBOLIC
            {
                int loc = targetLink != null && targetLink.card != null ? targetLink.card.GetLocation() : 0;
                returns.Add(DynValue.NewNumber(loc));
            }
            else if (arg == 6 || arg == 7) // CHAININFO_TRIGGERING_SEQUENCE & SYMBOLIC
            {
                int seq = targetLink != null && targetLink.card != null ? targetLink.card.GetSequence() : 0;
                returns.Add(DynValue.NewNumber(seq));
            }
            else if (arg == 13) // CHAININFO_CHAIN_ID
            {
                returns.Add(DynValue.NewNumber(chainIndex));
            }
            else if (arg == 14) // CHAININFO_TYPE
            {
                int type = targetLink != null && targetLink.card != null ? targetLink.card.GetType() : 0;
                returns.Add(DynValue.NewNumber(type));
            }
            else if (arg == 16) // CHAININFO_TRIGGERING_POSITION
            {
                int pos = targetLink != null && targetLink.card != null ? targetLink.card.GetBattlePosition() : 0;
                returns.Add(DynValue.NewNumber(pos));
            }
            else if (arg == 17) // CHAININFO_TRIGGERING_CODE
            {
                int code = targetLink != null && targetLink.card != null ? targetLink.card.GetCode() : 0;
                returns.Add(DynValue.NewNumber(code));
            }
            else if (arg == 18) // CHAININFO_TRIGGERING_CODE2
            {
                int code2 = targetLink != null && targetLink.card != null ? targetLink.card.GetOriginalCode() : 0;
                returns.Add(DynValue.NewNumber(code2));
            }
            else if (arg == 19) // CHAININFO_TRIGGERING_TYPE
            {
                int type = targetLink != null && targetLink.card != null ? targetLink.card.GetType() : 0;
                returns.Add(DynValue.NewNumber(type));
            }
            else if (arg == 20) // CHAININFO_TRIGGERING_LEVEL
            {
                int lvl = targetLink != null && targetLink.card != null ? targetLink.card.GetLevel() : 0;
                returns.Add(DynValue.NewNumber(lvl));
            }
            else if (arg == 21) // CHAININFO_TRIGGERING_RANK
            {
                int rank = targetLink != null && targetLink.card != null && targetLink.card.unityData.type.Contains("Xyz") ? targetLink.card.GetLevel() : 0;
                returns.Add(DynValue.NewNumber(rank));
            }
            else if (arg == 22) // CHAININFO_TRIGGERING_ATTRIBUTE
            {
                int attr = targetLink != null && targetLink.card != null ? targetLink.card.GetAttribute() : 0;
                returns.Add(DynValue.NewNumber(attr));
            }
            else if (arg == 23) // CHAININFO_TRIGGERING_RACE
            {
                int race = targetLink != null && targetLink.card != null ? targetLink.card.GetRace() : 0;
                returns.Add(DynValue.NewNumber(race));
            }
            else if (arg == 24) // CHAININFO_TRIGGERING_ATTACK
            {
                int atk = targetLink != null && targetLink.card != null ? targetLink.card.GetAttack() : 0;
                returns.Add(DynValue.NewNumber(atk));
            }
            else if (arg == 25) // CHAININFO_TRIGGERING_DEFENSE
            {
                int def = targetLink != null && targetLink.card != null ? targetLink.card.GetDefense() : 0;
                returns.Add(DynValue.NewNumber(def));
            }
            else if (arg >= 26 && arg <= 29) // STATUS, SUMMON_LOCATION, SUMMON_TYPE, PROC_COMPLETE
            {
                returns.Add(DynValue.NewNumber(0));
            }
            else if (arg == 30) // CHAININFO_TRIGGERING_SETCODES
            {
                returns.Add(DynValue.NewTable(CardEffectManager.Instance.luaEngine));
            }
            else if (arg == 11) // CHAININFO_DISABLE_REASON
            {
                LuaEffect de = targetLink != null ? targetLink.disableReason : null;
                returns.Add(de != null ? UserData.Create(de) : DynValue.Nil);
            }
            else if (arg == 12) // CHAININFO_DISABLE_PLAYER
            {
                int dp = targetLink != null ? targetLink.disablePlayer : 0;
                returns.Add(DynValue.NewNumber(dp));
            }
            else
            {
                returns.Add(DynValue.Nil);
            }
        }
        if (returns.Count == 0) return DynValue.NewTuple(DynValue.Nil, DynValue.Nil);
        if (returns.Count == 1) return returns[0];
        // Debug.Log($"[Surgical Log] GetChainInfo enviando um Tuple de volta ao LUA com {returns.Count} valores.");
        return DynValue.NewTuple(returns.ToArray());
    }

    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public LuaEffect GetPlayerEffect(object player, object effect_code) { return new LuaEffect { owner = SafeDummyCard() }; }

    public LuaCard GetFirstTarget()
    {
        LuaGroup group = GetTargetCards(null);
        return (group != null && group.GetFirst() != null) ? group.GetFirst() : SafeDummyCard();
    }

    public LuaGroup GetTargetCards(object e)
    {
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
        {
            var grp = CardEffectManager.Instance.chainManager.resolvingLink.targetGroup;
            if (grp != null && grp.cards.Count > 0) return grp;
        }
            
        return currentTargetGroup != null ? currentTargetGroup : new LuaGroup();
    }

    public LuaCard GetFieldCard(object player, object location, object sequence)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return null;
        
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        int seq = ConvertToInt(sequence);

        System.Func<CardDisplay, LuaCard> GetCachedCard = (cd) => {
            if (CardEffectManager.Instance != null) {
                LuaCard cached = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                if (cached != null) return cached;
            }
            return new LuaCard(cd);
        };

        Transform[] zones = null;
        if ((loc & 0x04) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        else if ((loc & 0x08) != 0) zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;

        if (zones != null && seq >= 0 && seq < zones.Length && zones[seq].childCount > 0)
        {
            var cd = zones[seq].GetComponentInChildren<CardDisplay>();
            if (cd != null) return GetCachedCard(cd);
        }
        if ((loc & 0x08) != 0 && seq == 5) // Field Zone Convention
        {
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { var cd = fz.GetComponentInChildren<CardDisplay>(); if (cd != null) return GetCachedCard(cd); }
        }
        return null;
    }

    // --- SISTEMA DE BUSCA E FILTROS DO LUA ---

    private void CollectCandidates(int loc, bool isPlayer, List<LuaCard> candidates, LuaCard excluded = null)
    {
        int pIdx = isPlayer ? 0 : 1;

        System.Func<CardDisplay, LuaCard> GetCachedCard = (cd) => {
            if (CardEffectManager.Instance != null) {
                LuaCard cached = CardEffectManager.Instance.EnsureCardScriptLoaded(cd);
                if (cached != null) return cached;
            }
            return new LuaCard(cd);
        };

        if ((loc & 0x01) != 0) // DECK
        {
            var deck = isPlayer ? GameManager.Instance.GetPlayerMainDeck() : GameManager.Instance.GetOpponentMainDeck();
            foreach(var c in deck) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x02) != 0) // HAND
        {
            var handGOs = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
            foreach(var go in handGOs) { 
                CardDisplay cd = go.GetComponent<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x04) != 0 || (loc & 0x800) != 0) // MZONE ou MMZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
            foreach (var z in zones) if (z.childCount > 0) { 
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x08) != 0 || (loc & 0x400) != 0) // SZONE ou STZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            foreach (var z in zones) if (z.childCount > 0) { 
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x08) != 0 || (loc & 0x100) != 0 || (loc & 0x400) != 0) // FZONE
        {
            Transform fz = isPlayer ? GameManager.Instance.duelFieldUI.playerFieldSpell : GameManager.Instance.duelFieldUI.opponentFieldSpell;
            if (fz.childCount > 0) { 
                CardDisplay cd = fz.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
        }
        if ((loc & 0x10) != 0) // GRAVE
        {
            var gy = isPlayer ? GameManager.Instance.GetPlayerGraveyard() : GameManager.Instance.GetOpponentGraveyard();
            foreach(var c in gy) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x20) != 0) // REMOVED
        {
            var rm = isPlayer ? GameManager.Instance.GetPlayerRemoved() : GameManager.Instance.GetOpponentRemoved();
            foreach(var c in rm) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
        if ((loc & 0x40) != 0) // EXTRA
        {
            var ex = isPlayer ? GameManager.Instance.GetPlayerExtraDeck() : GameManager.Instance.GetOpponentExtraDeck();
            foreach(var c in ex) { LuaCard lc = new LuaCard(c); lc.ownerPlayerIndex = pIdx; if(excluded == null || lc.unityData != excluded.unityData) candidates.Add(lc); }
        }
    }

    public LuaGroup GetMatchingGroup(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs)
    {
        LuaGroup group = new LuaGroup();
        List<LuaCard> candidates = new List<LuaCard>();

        int lSelf = ConvertToInt(locSelf);
        int lOpp = ConvertToInt(locOpp);
        int pInt = ConvertToInt(player);

        if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
        {
            bool isPlayer = IsPlayer(pInt);
            
            LuaCard excludedCard = excluded as LuaCard; // Simplificado para compatibilidade
            if (lSelf != 0) CollectCandidates(lSelf, isPlayer, candidates, excludedCard);
            if (lOpp != 0) CollectCandidates(lOpp, !isPlayer, candidates, excludedCard);
        }

        // Aplica o filtro Lua em cada carta encontrada na Unity
        foreach (var c in candidates)
        {   
            // Garantia Absoluta C#: A carta DEVE pertencer à localização exigida matematicamente para evitar vazamentos!
            int cLoc = c.GetLocation();
            bool locMatch = (c.GetControler() == pInt) ? ((lSelf & cLoc) != 0) : ((lOpp & cLoc) != 0);
            if (!locMatch) continue;
                     
            // Previne que cartas que acabaram de ser usadas como custo (ex: A Feather of the Phoenix) sejam alvo de si mesmas
            if (excluded is LuaGroup exg && exg.cards.Exists(exc => exc.unityData == c.unityData)) continue;
            else if (excluded is LuaCard excCard && excCard.unityData == c.unityData) continue;

            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                
                try {
                    DynValue result = closure.Call(callArgs.ToArray());
                    
                    bool passed = result.Type == DataType.Boolean && result.Boolean;
                    
                    Debug.Log($"<color=cyan>[Filtro LUA]</color> Avaliando alvo: '{c.unityData?.name}' | Aprovado no filtro? {passed}");
                    
                    if (result.Type == DataType.Boolean && result.Boolean) {
                        group.AddCard(c);
                    }
                } catch (System.Exception ex) {
                    Debug.LogWarning($"<color=red>[Filtro LUA]</color> Erro fatal ao avaliar '{c.unityData?.name}': {ex.Message}");
                }
            }
            else
            {
                group.AddCard(c);
            }
        }
        return group;
    }

    public LuaCard GetFirstMatchingCard(object filterFunc, object player, object locSelf, object locOpp, object excluded, params object[] extraArgs)
    {
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs);
        return (group != null && group.GetCount() > 0) ? group.GetFirst() : null;
    }

    public LuaGroup GetFusionMaterial(object player)
    {
        Debug.LogWarning("[LUA STUB] Duel.GetFusionMaterial chamado");
        int tp = ConvertToInt(player);
        return GetMatchingGroup(null, tp, 0x02 | 0x04, 0, null); // Mão + Campo
    }

    public DynValue GetEnvironment()
    {
        Debug.LogWarning("[LUA STUB] Duel.GetEnvironment chamado");
        return DynValue.Nil;
    }

    public bool IsExistingMatchingCard(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        LuaGroup group = GetMatchingGroup(filterFunc, player, locSelf, locOpp, excluded, extraArgs);
        bool res = group.GetCount() >= ConvertToInt(count);
        
        Debug.Log($"<color=orange>[LuaDuel]</color> Validando alvo no campo | Requisito: >= {ConvertToInt(count)} alvos | Encontrou: {group.GetCount()} | Permitido? {res}");
        return res;
    }

    public bool IsExistingTarget(object filterFunc, object player, object locSelf, object locOpp, object count, object excluded, params object[] extraArgs)
    {
        return IsExistingMatchingCard(filterFunc, player, locSelf, locOpp, count, excluded, extraArgs);
    }

    // ==============================================================================
    // STUBS FOR DIAGNOSTIC REPORT (DUEL)
    // ==============================================================================
    public void ActivateFieldSpell(params object[] args) { Debug.LogWarning("[LUA STUB] ActivateFieldSpell"); }
    public bool AddNoTributeCheck(params object[] args) { Debug.LogWarning("[LUA STUB] AddNoTributeCheck"); return false; }
    public void AdjustInstantly(params object[] args) { Debug.LogWarning("[LUA STUB] AdjustInstantly"); }
    public void AnnounceAnotherAttribute(params object[] args) { Debug.LogWarning("[LUA STUB] AnnounceAnotherAttribute"); }
    public void AnnounceAnotherRace(params object[] args) { Debug.LogWarning("[LUA STUB] AnnounceAnotherRace"); }
    public void AnnounceCoin(params object[] args) { Debug.LogWarning("[LUA STUB] AnnounceCoin"); }
    public DynValue AnnounceNumberRange(params object[] args) { Debug.LogWarning("[LUA STUB] AnnounceNumberRange"); return DynValue.NewNumber(0); }
    public void AskAny(params object[] args) { Debug.LogWarning("[LUA STUB] AskAny"); }
    public void AskEveryone(params object[] args) { Debug.LogWarning("[LUA STUB] AskEveryone"); }
    public bool AttackCostPaid(params object[] args) { Debug.LogWarning("[LUA STUB] AttackCostPaid"); return true; }
    public DynValue CallCoin(params object[] args) { Debug.LogWarning("[LUA STUB] CallCoin"); return DynValue.NewNumber(0); }
    public void ChainAttack(params object[] args) { Debug.LogWarning("[LUA STUB] ChainAttack"); }
    public void ChangeAttackTarget(params object[] args) { Debug.LogWarning("[LUA STUB] ChangeAttackTarget"); }
    public void ChangeTargetCard(params object[] args) { Debug.LogWarning("[LUA STUB] ChangeTargetCard"); }
    public void ChangeTargetPlayer(params object[] args) { Debug.LogWarning("[LUA STUB] ChangeTargetPlayer"); }
    public void ChangeToFaceupAttackOrFacedownDefense(params object[] args) { Debug.LogWarning("[LUA STUB] ChangeToFaceupAttackOrFacedownDefense"); }
    public bool CheckChainTarget(params object[] args) { Debug.LogWarning("[LUA STUB] CheckChainTarget"); return false; }
    public bool CheckLocation(params object[] args) { Debug.LogWarning("[LUA STUB] CheckLocation"); return false; }
    public bool CheckPendulumZones(params object[] args) { Debug.LogWarning("[LUA STUB] CheckPendulumZones"); return false; }
    public bool CheckPhaseActivity(params object[] args) { Debug.LogWarning("[LUA STUB] CheckPhaseActivity"); return false; }
    public bool CheckReleaseGroupSummon(params object[] args) { Debug.LogWarning("[LUA STUB] CheckReleaseGroupSummon"); return false; }
    public DynValue CountHeads(params object[] args) { Debug.LogWarning("[LUA STUB] CountHeads"); return DynValue.NewNumber(0); }
    public DynValue CountTails(params object[] args) { Debug.LogWarning("[LUA STUB] CountTails"); return DynValue.NewNumber(0); }
    public void EnableUnofficialAttribute(params object[] args) { Debug.LogWarning("[LUA STUB] EnableUnofficialAttribute"); }
    public void EnableUnofficialRace(params object[] args) { Debug.LogWarning("[LUA STUB] EnableUnofficialRace"); }
    public void EquipComplete(params object[] args) { Debug.LogWarning("[LUA STUB] EquipComplete"); }
    public int GetCardSetcodeFromCode(params object[] args) { Debug.LogWarning("[LUA STUB] GetCardSetcodeFromCode"); return 0; }
    public int GetCardTypeFromCode(params object[] args) { Debug.LogWarning("[LUA STUB] GetCardTypeFromCode"); return 0; }
    public int GetFieldGroupCountRush(params object[] args) { Debug.LogWarning("[LUA STUB] GetFieldGroupCountRush"); return 0; }
    public int GetLinkedZone(params object[] args) { Debug.LogWarning("[LUA STUB] GetLinkedZone"); return 0; }
    public int GetMasterRule(params object[] args) { Debug.LogWarning("[LUA STUB] GetMasterRule"); return 4; }
    public int GetMatchingGroupCountRush(params object[] args) { Debug.LogWarning("[LUA STUB] GetMatchingGroupCountRush"); return 0; }
    public LuaGroup GetMatchingGroupRush(params object[] args) { Debug.LogWarning("[LUA STUB] GetMatchingGroupRush"); return new LuaGroup(); }
    public LuaGroup GetOperatedGroup(params object[] args) { Debug.LogWarning("[LUA STUB] GetOperatedGroup"); return new LuaGroup(); }
    public DynValue GetPossibleOperationInfo(params object[] args) { Debug.LogWarning("[LUA STUB] GetPossibleOperationInfo"); return DynValue.Nil; }
    public LuaGroup GetStartingHand(params object[] args) { Debug.LogWarning("[LUA STUB] GetStartingHand"); return new LuaGroup(); }
    public LuaGroup GetTargetGroup(params object[] args) { Debug.LogWarning("[LUA STUB] GetTargetGroup"); return new LuaGroup(); }
    public int GetZoneWithLinkedCount(params object[] args) { Debug.LogWarning("[LUA STUB] GetZoneWithLinkedCount"); return 0; }
    public void GoatConfirm(params object[] args) { Debug.LogWarning("[LUA STUB] GoatConfirm"); }
    public void HintSelection(params object[] args) { Debug.LogWarning("[LUA STUB] HintSelection"); }
    public bool IsAttackCostPaid(params object[] args) { Debug.LogWarning("[LUA STUB] IsAttackCostPaid"); return true; }
    public bool IsChainSolving(params object[] args) { Debug.LogWarning("[LUA STUB] IsChainSolving"); return false; }
    public bool IsDamageCalculated(params object[] args) { Debug.LogWarning("[LUA STUB] IsDamageCalculated"); return false; }
    public bool IsDamageStep(params object[] args) { Debug.LogWarning("[LUA STUB] IsDamageStep"); return false; }
    public bool IsPlayerCanAdditionalTributeSummon(params object[] args) { Debug.LogWarning("[LUA STUB] IsPlayerCanAdditionalTributeSummon"); return false; }
    public bool IsSummonCancelable(params object[] args) { Debug.LogWarning("[LUA STUB] IsSummonCancelable"); return false; }
    public void LoadCardScript(params object[] args) { Debug.LogWarning("[LUA STUB] LoadCardScript"); }
    public void LoadCardScriptAlias(params object[] args) { Debug.LogWarning("[LUA STUB] LoadCardScriptAlias"); }
    public void MoveSequence(params object[] args) { Debug.LogWarning("[LUA STUB] MoveSequence"); }
    public void MoveToDeckBottom(params object[] args) { Debug.LogWarning("[LUA STUB] MoveToDeckBottom"); }
    public void MoveToDeckTop(params object[] args) { Debug.LogWarning("[LUA STUB] MoveToDeckTop"); }
    public void NegateAttack(params object[] args) { Debug.LogWarning("[LUA STUB] NegateAttack"); }
    public void NegateRelatedChain(params object[] args) { Debug.LogWarning("[LUA STUB] NegateRelatedChain"); }
    public void NegateSummon(params object[] args) { Debug.LogWarning("[LUA STUB] NegateSummon"); }
    public void RDComplete(params object[] args) { Debug.LogWarning("[LUA STUB] RDComplete"); }
    public void Readjust(params object[] args) { Debug.LogWarning("[LUA STUB] Readjust"); }
    public void RemoveCards(params object[] args) { Debug.LogWarning("[LUA STUB] RemoveCards"); }
    public DynValue SelectEffectYesNo(params object[] args) { Debug.LogWarning("[LUA STUB] SelectEffectYesNo"); return DynValue.NewBoolean(false); }
    public DynValue SelectFieldZone(params object[] args) { Debug.LogWarning("[LUA STUB] SelectFieldZone"); return DynValue.NewNumber(0); }
    public LuaGroup SelectReleaseGroupSummon(params object[] args) { Debug.LogWarning("[LUA STUB] SelectReleaseGroupSummon"); return new LuaGroup(); }
    public LuaGroup SelectTribute(params object[] args) { Debug.LogWarning("[LUA STUB] SelectTribute"); return new LuaGroup(); }
    public void Sendto(params object[] args) { Debug.LogWarning("[LUA STUB] Sendto"); }
    public void SetChainLimit(params object[] args) { Debug.LogWarning("[LUA STUB] SetChainLimit"); }
    public void SetChainLimitTillChainEnd(params object[] args) { Debug.LogWarning("[LUA STUB] SetChainLimitTillChainEnd"); }
    public void SetFusionMaterial(params object[] args) { Debug.LogWarning("[LUA STUB] SetFusionMaterial"); }
    public void SetLP(params object[] args) { Debug.LogWarning("[LUA STUB] SetLP"); }
    public void ShuffleExtra(params object[] args) { Debug.LogWarning("[LUA STUB] ShuffleExtra"); }
    public void SkipPhase(params object[] args) { Debug.LogWarning("[LUA STUB] SkipPhase"); }
    public void SummonOrSet(params object[] args) { Debug.LogWarning("[LUA STUB] SummonOrSet"); }
    public void SwapControl(params object[] args) { Debug.LogWarning("[LUA STUB] SwapControl"); }
    public void TagSwap(params object[] args) { Debug.LogWarning("[LUA STUB] TagSwap"); 
    }

    public int GetTargetPlayer() { 
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.targetPlayer;
        return targetPlayer; 
    }
    public int GetTargetParam() { 
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
            return CardEffectManager.Instance.chainManager.resolvingLink.targetParam;
        return targetParam; 
    }
}