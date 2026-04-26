using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// 4. CLASSE GROUP (Lista de Cartas selecionadas ou filtradas)
// Onde a Mágica Acontece: Gerencia os agrupamentos ('Group.') e filtragens do OCGCore.
// Tratativas Críticas & Dependências: 
// - LuaCard.cs: A lista é essencialmente um HashSet/List dinâmico de instâncias LuaCard.
// - GameManager.cs: Invoca as UIs de seleção múltipla (CardSelectionUI) quando 'Select' é chamado.
// - OpponentAI.cs: Intercepta a seleção de grupos se o jogador chamador for a IA.
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
        if (obj is MoonSharp.Interpreter.DynValue dv)
        {
            if (dv.Type == MoonSharp.Interpreter.DataType.Number) return (int)dv.Number;
            if (dv.Type == MoonSharp.Interpreter.DataType.Boolean) return dv.Boolean ? 1 : 0;
            if (dv.Type == MoonSharp.Interpreter.DataType.String && int.TryParse(dv.String, out int res)) return res;
            return 0;
        }
        if (obj is double d) return (int)d;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is bool b) return b ? 1 : 0;
        if (obj is string s && int.TryParse(s, out int parsed)) return parsed;
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

            if (c.unityData != null) selectableData.Add(c.unityData);
        }
        
        // --- BYPASS DE INTELIGÊNCIA ARTIFICIAL ---
        if (ConvertToInt(player) != 0 && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            LuaGroup aiChoice = OpponentAI.Instance.SelectLuaTargets(this, ConvertToInt(min), ConvertToInt(max));
            CardEffectManager.Instance.yieldReturnValue = UserData.Create(aiChoice);
            CardEffectManager.Instance.isWaitingForLuaYield = false;
            return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.Select") });
        }

        if (GameManager.Instance != null && selectableData.Count > 0) {
            bool forceModal = cards.Exists(c => !c.IsLocation(0x02 | 0x04 | 0x08));
            GameManager.Instance.OpenCardMultiSelection(selectableData, "Select a target from the group", ConvertToInt(min), ConvertToInt(max), (selectedList) => {
                LuaGroup selectedGroup = new LuaGroup();
                foreach (var data in selectedList) {
                    LuaCard match = cards.Find(lc => lc.unityData == data && !selectedGroup.cards.Contains(lc));
                    if (match != null) selectedGroup.AddCard(match);
                }
                CardEffectManager.Instance.yieldReturnValue = UserData.Create(selectedGroup);
                CardEffectManager.Instance.isWaitingForLuaYield = false;
            }, HighlightCategory.GenericTarget, forceModal);
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
