using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// 3. CLASSE EFFECT (Estrutura de Habilidades)
// Onde a Mágica Acontece: Intercepta a criação e configuração de efeitos ('Effect.').
// Tratativas Críticas & Dependências: 
// - LuaCard.cs: Todo efeito pertence a um 'owner' (LuaCard).
// - MoonSharp.Closure: Armazena os ponteiros vitais para condition, cost, target e operation
//   que serão disparados posteriormente pelo CardEffectManager e ChainManager.
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
    public int range;
    public int targetRangeSelf;
    public int targetRangeOpponent;
    private object _valueObject = 0;
    private int _label = 0;
    private object _labelObject = null;
    private int _resetValue = 0;
    public int countLimitMax = 0;
    public int countLimitCode = 0;
    public int currentUsages = 0;
    public int hintTimingSelf = 0;
    public int hintTimingOpponent = 0;
    
    public Closure conditionFunc;
    public Closure costFunc;
    public Closure targetFunc;
    public Closure operationFunc;

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
    
    public void SetCountLimit(params object[] args) 
    { 
        if (args != null && args.Length > 0) countLimitMax = ConvertToInt(args[0]); 
        if (args != null && args.Length > 1) countLimitCode = ConvertToInt(args[1]); 
        Debug.Log($"<color=orange>[LuaEffect]</color> SetCountLimit registrado! Máx: {countLimitMax} | Código: {countLimitCode}");
    }
    
    public void SetHintTiming(params object[] args) 
    { 
        if (args != null && args.Length > 0) hintTimingSelf = ConvertToInt(args[0]); 
        if (args != null && args.Length > 1) hintTimingOpponent = ConvertToInt(args[1]); 
        else hintTimingOpponent = hintTimingSelf; // Padrão OCGCore: Assume o mesmo para ambos se só enviar 1
    }
    public void SetTargetRange(params object[] args) { 
        if (args != null && args.Length > 0) targetRangeSelf = ConvertToInt(args[0]); 
        if (args != null && args.Length > 1) targetRangeOpponent = ConvertToInt(args[1]); 
    }
    public void SetReset(params object[] args) { if (args != null && args.Length > 0) _resetValue = ConvertToInt(args[0]); }
    public int GetReset() { return _resetValue; }
    public void SetValue(params object[] args) { if (args != null && args.Length > 0) _valueObject = args[0]; }
    public object GetValue() { return _valueObject; }
    public void SetRange(params object[] args) { if (args != null && args.Length > 0) range = ConvertToInt(args[0]); }
    
    public void SetLabel(params object[] args) { 
        if (args != null && args.Length > 0) _label = ConvertToInt(args[0]); 
        Debug.Log($"<color=magenta>[LuaEffect LOG]</color> SetLabel chamado! Memória armazenada: {_label} (Efeito ID: {code})");
    }
    
    public int GetLabel() { 
        return _label; 
    }
    
    public bool IsHasProperty(object prop) { return true; }
    public bool IsHasCategory(object cat) { return true; }
    public int GetActiveType() { return type; }
    public bool IsSpellEffect() { return owner != null && owner.IsSpell(); }
    public bool IsTrapEffect() { return owner != null && owner.IsTrap(); }
    public bool IsMonsterEffect() { return owner != null && owner.IsMonster(); }
    public bool IsSpellTrapEffect() { return owner != null && (owner.IsSpell() || owner.IsTrap()); }
    
    public Closure GetCondition() { return conditionFunc != null ? conditionFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetCost() { return costFunc != null ? costFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetTarget() { return targetFunc != null ? targetFunc : CardEffectManager.Instance.dummyClosureTrue; }
    public Closure GetOperation() { return operationFunc != null ? operationFunc : CardEffectManager.Instance.dummyClosureTrue; }
    
    public int GetCategory() { return category; }
    public int GetProperty() { return property; }
    public int GetCode() { return code; }
    
    public void SetLabelObject(object o) { 
        _labelObject = o; 
        Debug.Log($"<color=magenta>[LuaEffect LOG]</color> SetLabelObject chamado! Ponte criada com o objeto LUA.");
    }
    
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
            range = this.range,
            targetRangeSelf = this.targetRangeSelf,
            targetRangeOpponent = this.targetRangeOpponent,
            conditionFunc = this.conditionFunc,
            costFunc = this.costFunc,
            targetFunc = this.targetFunc,
            operationFunc = this.operationFunc,
            _valueObject = this._valueObject,
            _label = this._label,
            _labelObject = this._labelObject, // FIX: Copiando a Ponte de Memória!
            _resetValue = this._resetValue,
            countLimitMax = this.countLimitMax,
            countLimitCode = this.countLimitCode,
            currentUsages = 0,
            hintTimingSelf = this.hintTimingSelf,
            hintTimingOpponent = this.hintTimingOpponent
        };
    }

    // Callbacks do Lua (Condição, Alvo, Resolução)
    public void SetCondition(object condition) { conditionFunc = condition as Closure; }
    public void SetCost(object cost) { costFunc = cost as Closure; }
    public void SetTarget(object target) { targetFunc = target as Closure; }
    public void SetOperation(object operation) { operationFunc = operation as Closure; }

    public int GetHandlerPlayer() { return owner != null ? owner.GetControler() : 0; }
    public int GetOwnerPlayer() { return GetHandlerPlayer(); }
    // Retorna a quem o efeito pertence blindado contra Nulos!
    public LuaCard GetHandler() { return owner ?? new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }
    public LuaCard GetOwner() { return owner ?? new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }
}

// ==============================================================================
