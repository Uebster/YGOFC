using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

// ==============================================================================
// 4. CLASSE GROUP (Lista de Cartas selecionadas ou filtradas)
// Onde a Mágica Acontece: Gerencia os agrupamentos ('Group.') e filtragens do OCGCore.
// Tratativas Críticas & Dependências: 
// - LuaCard.cs: A lista é essencialmente um HashSet/List dinâmico de instâncias LuaCard.
// - GameManager.cs: Invoca as UIs de seleção múltipla (CardSelectionUI) quando 'Select' é chamado.
// - OpponentAI.cs: Intercepta a seleção de grupos se o jogador chamador for a IA.
// ==============================================================================
[MoonSharpUserData]
public partial class LuaGroup
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
    
    public bool IsContains(object card) { 
        if (card is LuaCard lc) 
        {
            return cards.Exists(c => c.unityData == lc.unityData);
        }
        return false; 
    }
    
    public bool Equal(object other)
    {
        if (other is LuaGroup g)
        {
            if (cards.Count != g.cards.Count) return false;
            foreach (var c in cards) if (!g.cards.Contains(c)) return false;
            return true;
        }
        return false;
    }

    public LuaGroup Clone() { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Sub(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }
    public LuaGroup Add(LuaGroup g) { return new LuaGroup { cards = new List<LuaCard>(this.cards) }; }

    public static LuaGroup FromCards(params object[] args)
    {
        LuaGroup g = new LuaGroup();
        foreach (var arg in args) if (arg is LuaCard lc) g.AddCard(lc);
        return g;
    }

    public LuaGroup AddMaximumCheck() { return this; } // Stub seguro para ignorar lógica de Maximum Rush Duel

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

    private int EvaluateFuncAsInt(object func, LuaCard c, params object[] extraArgs)
    {
        if (func is Closure closure)
        {
            List<object> callArgs = new List<object> { c };
            if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
            try {
                DynValue res = closure.Call(callArgs.ToArray());
                if (res.Type == DataType.Number) return (int)res.Number;
            } catch { }
        }
        else if (func is CallbackFunction cb)
        {
            List<DynValue> callArgs = new List<DynValue> { UserData.Create(c) };
            if (extraArgs != null && extraArgs.Length > 0) 
                foreach(var arg in extraArgs) callArgs.Add(DynValue.FromObject(null, arg));
            try {
                DynValue res = cb.Invoke(null, callArgs, false);
                if (res.Type == DataType.Number) return (int)res.Number;
            } catch { }
        }
        return 0;
    }

    public int GetSum(object func, params object[] extraArgs)
    {
        int sum = 0;
        foreach (var c in cards) sum += EvaluateFuncAsInt(func, c, extraArgs);
        return sum;
    }

    public bool CheckWithSumEqual(object func, object targetSum, object count, object maxCount, params object[] extraArgs)
    {
        int target = ConvertToInt(targetSum);
        int minC = ConvertToInt(count);
        int maxC = ConvertToInt(maxCount);

        List<int> values = new List<int>();
        foreach(var c in cards) values.Add(EvaluateFuncAsInt(func, c, extraArgs));

        return SubsetSumRecursive(values, target, minC, maxC, true, 0, 0, 0);
    }

    public bool CheckWithSumGreater(object func, object targetSum, params object[] extraArgs)
    {
        int target = ConvertToInt(targetSum);
        
        List<int> values = new List<int>();
        foreach(var c in cards) values.Add(EvaluateFuncAsInt(func, c, extraArgs));

        return SubsetSumGreaterRecursive(values, target, 0, new List<int>());
    }

    private bool SubsetSumRecursive(List<int> values, int target, int minCount, int maxCount, bool exact, int index, int currentSum, int currentCount)
    {
        if (exact && currentSum == target && currentCount >= minCount && currentCount <= maxCount) return true;
        if (index >= values.Count || currentCount >= maxCount) return false;

        if (SubsetSumRecursive(values, target, minCount, maxCount, exact, index + 1, currentSum + values[index], currentCount + 1)) return true;
        if (SubsetSumRecursive(values, target, minCount, maxCount, exact, index + 1, currentSum, currentCount)) return true;

        return false;
    }

    private bool SubsetSumGreaterRecursive(List<int> values, int target, int index, List<int> currentSelection)
    {
        int currentSum = currentSelection.Sum();
        if (currentSum >= target)
        {
            // Garante a regra rigorosa dos Rituais: Você não pode usar materiais extras redundantes se o Nível já foi atingido.
            foreach(int val in currentSelection)
            {
                if (currentSum - val >= target) return false; 
            }
            return true;
        }

        if (index >= values.Count) return false;

        currentSelection.Add(values[index]);
        if (SubsetSumGreaterRecursive(values, target, index + 1, currentSelection)) return true;
        currentSelection.RemoveAt(currentSelection.Count - 1);

        if (SubsetSumGreaterRecursive(values, target, index + 1, currentSelection)) return true;

        return false;
    }

    public DynValue SelectWithSumEqual(object player, object func, object targetSum, object count, object maxCount, params object[] extraArgs)
    {
        int tp = ConvertToInt(player);
        int target = ConvertToInt(targetSum);
        int min = ConvertToInt(count);
        int max = ConvertToInt(maxCount);

        LuaGroup selectedGroup = new LuaGroup();
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;
        
        CardEffectManager.Instance.StartCoroutine(OpenSumSelectionUI(tp, func, target, min, max, extraArgs, true, selectedGroup));

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.SelectWithSumEqual") });
    }

    public DynValue SelectWithSumGreater(object player, object func, object targetSum, params object[] extraArgs)
    {
        int tp = ConvertToInt(player);
        int target = ConvertToInt(targetSum);

        LuaGroup selectedGroup = new LuaGroup();
        CardEffectManager.Instance.isWaitingForLuaYield = true;
        CardEffectManager.Instance.yieldReturnValue = null;

        CardEffectManager.Instance.StartCoroutine(OpenSumSelectionUI(tp, func, target, 1, 99, extraArgs, false, selectedGroup));

        return DynValue.NewYieldReq(new DynValue[] { DynValue.NewString("Group.SelectWithSumGreater") });
    }

    private IEnumerator OpenSumSelectionUI(int tp, object funcObj, int targetSum, int minCount, int maxCount, object[] extraArgs, bool exactMath, LuaGroup outGroup)
    {
        List<CardData> validCards = new List<CardData>();
        foreach (var c in cards) if (c.unityData != null) validCards.Add(c.unityData);

        bool selectionDone = false;

        if (tp == 1 && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            int currentSum = 0;
            foreach (var c in cards) {
                if (currentSum >= targetSum) break;
                int val = EvaluateFuncAsInt(funcObj, c, extraArgs);
                if (val > 0) { currentSum += val; outGroup.AddCard(c); }
            }
            selectionDone = true;
        }
        else
        {
            if (GameManager.Instance != null)
            {
                System.Func<List<CardData>, bool> sumValidator = (selectedList) => {
                    List<int> values = new List<int>();
                    foreach (var d in selectedList) {
                        var match = cards.Find(c => c.unityData == d);
                        if (match != null) values.Add(EvaluateFuncAsInt(funcObj, match, extraArgs));
                    }
                    if (exactMath) return SubsetSumRecursive(values, targetSum, minCount, maxCount, true, 0, 0, 0);
                    else return SubsetSumGreaterRecursive(values, targetSum, 0, new List<int>());
                };

                HighlightCategory cat = HighlightCategory.Tribute;
                if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null) {
                    var eff = CardEffectManager.Instance.chainManager.resolvingLink.effect;
                    if (eff != null && eff.owner != null && eff.owner.unityData != null && eff.owner.unityData.type.Contains("Ritual")) cat = HighlightCategory.Ritual;
                }

                GameManager.Instance.StartDirectSelection(validCards, minCount, maxCount, sumValidator, $"Selecione Tributos (Soma {(exactMath ? "exata de" : "mínima de")} {targetSum})", (selected) => {
                    if (selected != null) {
                        foreach (var selData in selected) {
                            var match = cards.Find(c => c.unityData == selData);
                            if (match != null) outGroup.AddCard(match);
                        }
                    }
                    selectionDone = true;
                }, cat);
            }
            else selectionDone = true;
        }

        yield return new WaitUntil(() => selectionDone);

        CardEffectManager.Instance.yieldReturnValue = UserData.Create(outGroup);
        CardEffectManager.Instance.isWaitingForLuaYield = false;
    }
    
    public int FilterCount(object filterFunc, object excluded, params object[] extraArgs)
    {
        return Filter(filterFunc, excluded, extraArgs).GetCount();
    }
    
    public DynValue FilterSelect(object player, object filterFunc, object min, object max, object excluded, params object[] extraArgs)
    {
        LuaGroup filtered = Filter(filterFunc, excluded, extraArgs);
        return filtered.Select(player, min, max, null);
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

        bool canCancel = !(ConvertToInt(min) > 0 && CardEffectManager.Instance != null && CardEffectManager.Instance.isChainResolving);

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
            }, HighlightCategory.GenericTarget, forceModal, canCancel);
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
// STUBS FOR DIAGNOSTIC REPORT (GROUP)
// ==============================================================================
public partial class LuaGroup
{
    public void Match(object filterFunc, object excluded, params object[] extraArgs)
    {
        List<LuaCard> toRemove = new List<LuaCard>();
        foreach (var c in cards)
        {
            if (excluded != null)
            {
                if (excluded is LuaCard excCard && c == excCard) { toRemove.Add(c); continue; }
                if (excluded is LuaGroup excGroup && excGroup.cards.Contains(c)) { toRemove.Add(c); continue; }
            }

            if (filterFunc is Closure closure)
            {
                List<object> callArgs = new List<object> { c };
                if (extraArgs != null && extraArgs.Length > 0) callArgs.AddRange(extraArgs);
                try {
                    DynValue res = closure.Call(callArgs.ToArray());
                    if (res.Type == DataType.Boolean && !res.Boolean) toRemove.Add(c);
                    else if (res.IsNil()) toRemove.Add(c);
                } catch { toRemove.Add(c); }
            }
        }
        foreach (var c in toRemove) cards.Remove(c);
    }
}

// ==============================================================================
