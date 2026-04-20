using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

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
    private int _label = 0;
    
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
    
    // Funções muito usadas no OCGCore na inicialização ignoradas elegantemente
    public void SetCountLimit(params object[] args) { }
    public void SetHintTiming(params object[] args) { }
    public void SetTargetRange(params object[] args) { }
    public void SetReset(params object[] args) { }
    public void SetValue(params object[] args) { if (args != null && args.Length > 0) _valueObject = args[0]; }
    public object GetValue() { return _valueObject; }
    public void SetRange(params object[] args) { }
    public void SetLabel(params object[] args) { if (args != null && args.Length > 0) _label = ConvertToInt(args[0]); }
    public int GetLabel() { return _label; }
    
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
            _valueObject = this._valueObject,
            _label = this._label
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
