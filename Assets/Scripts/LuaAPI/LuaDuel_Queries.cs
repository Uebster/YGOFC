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
        if (GameManager.Instance == null) return 0;
        bool isPlayer = IsPlayer(player);
        int loc = ConvertToInt(location);
        int result = 5;
        
        if (loc == 0x04) // LOCATION_MZONE
            result = GameManager.Instance.GetFreeMonsterZones(isPlayer);
        if (loc == 0x08) // LOCATION_SZONE
            result = GameManager.Instance.GetFreeSpellTrapZones(isPlayer);
            
        // Debug.Log($"[LuaDuel] GetLocationCount consultado: Player {player}, Loc {loc} -> Result: {result}");
        return result;
    }

    public void AddCustomActivityCounter(object counter_id, object activity_type, object filter) { }
    public void EnableGlobalFlag(object flag) { }

    private Dictionary<int, LuaGroup> _opInfoCache = new Dictionary<int, LuaGroup>();

    public void SetOperationInfo(object chainc, object category, object target, object count, object player, object param) 
    { 
        int cat = ConvertToInt(category);
        if (target is LuaGroup g) _opInfoCache[cat] = g;
        else if (target is LuaCard c) 
        {
            LuaGroup newG = new LuaGroup();
            newG.AddCard(c);
            _opInfoCache[cat] = newG;
        }

        int idx = ConvertToInt(chainc);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.currentChain.Count >= idx && idx > 0)
        {
            // Guarda a informação na memória do elo (Usado por Stardust Dragon, My Body as a Shield, etc)
            // CardEffectManager.Instance.currentChain[idx - 1].operationInfo = ...
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
    public bool IsAbleToEnterBP() { return true; }
    public bool IsBattlePhase() { return PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle; }
    public bool IsMainPhase() { return PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2); }
    public bool IsPlayerAffectedByEffect(object player, object effect_code) { return false; }
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
    public int GetMZoneCount(object player) { return 5; }
    public bool IsCanAddCounter(object player, object counterType, object count, object card) { return true; }
    public LuaGroup GetReleaseGroup(object player, object hand = null) { return new LuaGroup(); }
    public LuaGroup GetTributeGroup(object card) { return new LuaGroup(); }

    public LuaGroup GetRitualMaterial(object player, object nocheck = null)
    {
        // Stub blindado: Retorna a Mão (0x02) e o Campo (0x04) do jogador para o proc_ritual.lua aprovar o chk=0 da ativação.
        return GetMatchingGroup(null, player, 0x02 | 0x04, 0, null);
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
        if (_opInfoCache.ContainsKey(cat))
        {
            return DynValue.NewTuple(DynValue.NewBoolean(true), UserData.Create(_opInfoCache[cat]));
        }
        return DynValue.NewTuple(DynValue.NewBoolean(false), UserData.Create(new LuaGroup())); 
    }

    public void ClearOperationInfo() 
    {
        _opInfoCache.Clear();
    }

    public bool IsChainNegatable(object chaincount) { return true; }
    public int GetOperationCount(object chainc) { return 0; }
    public bool IsChainDisablable(object chainc) { return true; }
    public void DiscardDeck(object player, object count, object reason) { }
    public bool CheckTribute(object card, object min, object max, object group = null, object zone = null) { return true; }
    public void SetTargetCard(object target) { 
        if (currentTargetGroup == null) currentTargetGroup = new LuaGroup();
        if (target is LuaGroup g) currentTargetGroup.cards.AddRange(g.cards);
        else if (target is LuaCard c) currentTargetGroup.AddCard(c);
    }
    public void ClearTargetCard() { 
        currentTargetGroup = new LuaGroup(); 
    }
    public bool CheckEvent(object event_code, object chainc = null) { return false; }
    public int GetDrawCount(params object[] args) { return 1; }

    public bool IsPlayerCanDraw(object player, object amount = null)
    {
        int amt = amount == null ? 1 : ConvertToInt(amount);
        int deckCount = IsPlayer(player) ? GameManager.Instance.GetPlayerMainDeck().Count : GameManager.Instance.GetOpponentMainDeck().Count;
        return deckCount >= (amt > 0 ? amt : 1);
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
            if (arg == 1) // CHAININFO_TARGET_PLAYER
            {
                returns.Add(DynValue.NewNumber(targetPl));
            }
            else if (arg == 2) // CHAININFO_TARGET_PARAM
            {
                // Debug.Log($"<color=magenta>[LuaDuel LOG]</color> GetChainInfo: Resgatando TARGET_PARAM da memória. Valor: {targetPa}");
                returns.Add(DynValue.NewNumber(targetPa));
            }
            else if (arg == 3 || arg == 16 || arg == 8388608) // CHAININFO_TARGET_CARDS (3 injetado pelo nosso Core)
            {
                LuaGroup g = targGr != null ? targGr : new LuaGroup();
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
            else if (arg == 1024) // CHAININFO_TRIGGERING_LOCATION
            {
                int loc = targetLink != null && targetLink.card != null ? targetLink.card.GetLocation() : 0;
                returns.Add(DynValue.NewNumber(loc));
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
            return CardEffectManager.Instance.chainManager.resolvingLink.targetGroup ?? new LuaGroup();
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
        if ((loc & 0x04) != 0) // MZONE
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
        if ((loc & 0x08) != 0) // SZONE
        {
            Transform[] zones = isPlayer ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
            foreach (var z in zones) if (z.childCount > 0) { 
                CardDisplay cd = z.GetComponentInChildren<CardDisplay>();
                if (cd != null) {
                    LuaCard lc = GetCachedCard(cd); lc.ownerPlayerIndex = pIdx; 
                    if(excluded == null || lc.unityCard != excluded.unityCard) candidates.Add(lc); 
                }
            }
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