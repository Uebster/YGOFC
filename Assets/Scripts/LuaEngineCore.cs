using UnityEngine;
using MoonSharp.Interpreter;

// ==============================================================================
// CLASSE ENGINE CORE (O Coração do Interpretador MoonSharp)
// Onde a Mágica Acontece: Instancia a Máquina Virtual Lua e mapeia a API do OCGCore.
// Tratativas Críticas & Dependências: 
// - LuaAPI: Registra e disponibiliza as classes (LuaDuel, LuaCard, etc.) pro LUA.
// - Constantes: Todas as variáveis globais (LOCATION_DECK, TYPE_SPELL) nascem aqui.
// - Closures: Fornece a tabela 'aux' com funções utilitárias do Yu-Gi-Oh nativo.
// ==============================================================================
public class LuaEngineCore
{
    public Script luaEngine;
    public LuaDuel luaDuel;
    public Closure dummyClosureTrue;

    public void Initialize()
    {
        // 1. Instancia o interpretador MoonSharp
        luaEngine = new Script();

        // 2. Registra nossas Classes C# para serem expostas ao Lua
        UserData.RegisterType<LuaDuel>();
        UserData.RegisterType<LuaCard>();
        UserData.RegisterType<LuaEffect>();
        UserData.RegisterType<LuaGroup>();
        UserData.RegisterType<Fusion>();
        UserData.RegisterType<Synchro>();
        UserData.RegisterType<Spirit>();
        UserData.RegisterType<Ritual>();
        UserData.RegisterType<Xyz>();

        // 3. Injeta as instâncias globais na memória do Lua
        luaDuel = new LuaDuel();
        luaEngine.Globals["Duel"] = luaDuel;
        luaEngine.Globals["Effect"] = typeof(LuaEffect); // Permite chamar Effect.CreateEffect(c)
        luaEngine.Globals["Group"] = typeof(LuaGroup);
        luaEngine.Globals["Fusion"] = typeof(Fusion);        
        luaEngine.Globals["Synchro"] = typeof(Synchro);      
        luaEngine.Globals["Spirit"] = typeof(Spirit);        
        luaEngine.Globals["Ritual"] = typeof(Ritual);        
        luaEngine.Globals["Xyz"] = typeof(Xyz);              

        // Sistema de Log LUA Nativo
        luaEngine.Globals["Log"] = DynValue.FromObject(luaEngine, (System.Action<string>)(msg => Debug.Log($"<color=cyan>[LUA ENGINE]</color> {msg}")));

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Tabela Auxiliar Básica
        DynValue auxTable = DynValue.NewTable(luaEngine);
        auxTable.Table.Set("Stringid", luaEngine.DoString("return function(code, id) return (code * 16) + id end"));
        auxTable.Table.Set("GlobalCheck", DynValue.FromObject(luaEngine, (System.Action<object, Closure>)((s, func) => { 
            Table luaTable = null;
            if (s is Table t) luaTable = t;
            else if (s is DynValue dv && dv.Type == DataType.Table) luaTable = dv.Table;
            
            if (luaTable != null) {
                var check = luaTable.Get("global_check");
                if (check.IsNil() || (check.Type == DataType.Boolean && !check.Boolean)) {
                    luaTable.Set("global_check", DynValue.NewBoolean(true));
                    func?.Call();
                }
            } else {
                func?.Call();
            }
        })));
        
        // Constantes lógicas (Closures nativas)
        auxTable.Table.Set("TRUE", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("FALSE", luaEngine.DoString("return function(...) return false end"));

        auxTable.Table.Set("TargetBoolFunction", luaEngine.DoString(@"
            return function(f, val1, val2, val3)
                return function(e, target)
                    local c = target or e
                    if c == nil then return false end
                    if type(f) == 'function' then
                        if val3 ~= nil then return f(c, val1, val2, val3) end
                        if val2 ~= nil then return f(c, val1, val2) end
                        if val1 ~= nil then return f(c, val1) end
                        return f(c)
                    end
                    return true
                end
            end
        "));

        auxTable.Table.Set("FilterBoolFunction", luaEngine.DoString(@"
            return function(f, val1, val2, val3)
                return function(target)
                    if target == nil then return false end
                    if type(f) == 'function' then
                        if val3 ~= nil then return f(target, val1, val2, val3) end
                        if val2 ~= nil then return f(target, val1, val2) end
                        if val1 ~= nil then return f(target, val1) end
                        return f(target)
                    end
                    return true
                end
            end
        "));

        auxTable.Table.Set("FilterFaceupFunction", luaEngine.DoString(@"
            return function(f, val1, val2, val3)
                return function(target)
                    if target == nil or not target:IsFaceup() then return false end
                    if type(f) == 'function' then
                        if val3 ~= nil then return f(target, val1, val2, val3) end
                        if val2 ~= nil then return f(target, val1, val2) end
                        if val1 ~= nil then return f(target, val1) end
                        return f(target)
                    end
                    return true
                end
            end
        "));

        auxTable.Table.Set("SpElimFilter", luaEngine.DoString(@"
            return function(c, mustbe_monster, excludefield)
                if c == nil then return false end
                if c:IsMonster() then
                    if excludefield then return c:IsLocation(LOCATION_GRAVE) end
                    -- OCGCore: Permite Token no campo, mas tranca monstros reais no GY!
                    return c:IsLocation(LOCATION_GRAVE) or (c:IsLocation(LOCATION_MZONE) and c:IsFaceup() and c:IsType(TYPE_TOKEN))
                else
                    if mustbe_monster then return false end
                    return c:IsLocation(LOCATION_GRAVE)
                end
            end
        "));

        auxTable.Table.Set("CheckStealEquip", luaEngine.DoString(@"
            return function(c, e, tp) return c:IsControlerCanBeChanged() and c:IsFaceup() end
        "));

        auxTable.Table.Set("ChkfMMZ", luaEngine.DoString(@"
            return function(count) return function(sg, e, tp, mg) return Duel.GetLocationCount(tp, 4) >= count end end
        "));

        // UX Adapter: O utility.lua nativo força um loop de selecionar uma carta por vez (YGOPro UX).
        // Nós sobrescrevemos isso aqui para injetar a UI moderna da Unity de "Múltipla Escolha" de uma vez só.
        auxTable.Table.Set("SelectUnselectGroup", luaEngine.DoString(@"
            return function(g, e, tp, minc, maxc, rescon, chk, sel_tp, hintmsg, cancelcon, breakcon, cancelable)
                if chk == 0 then return g and g:GetCount() >= minc end
                if sel_tp == nil then sel_tp = tp end
                if hintmsg == nil then hintmsg = 0 end
                Duel.Hint(3, sel_tp, hintmsg)
                local filter = function(c) return g:IsExists(function(tc) return tc == c end, 1, nil) end
                return Duel.SelectMatchingCard(sel_tp, filter, sel_tp, 0x7E, 0x7E, minc, maxc, nil)
            end
        "));
        
        auxTable.Table.Set("AddEquipProcedure", luaEngine.DoString(@"
            return function(c, player, filter, eqlimit, prop, tg, op, con)
                local e1=Effect.CreateEffect(c)
                e1:SetCategory(CATEGORY_EQUIP)
                e1:SetType(EFFECT_TYPE_ACTIVATE)
                e1:SetCode(EVENT_FREE_CHAIN)
                e1:SetProperty(prop or EFFECT_FLAG_CARD_TARGET)
                if con then e1:SetCondition(con) end
                e1:SetTarget(function(e,tp,eg,ep,ev,re,r,rp,chk,chkc)
                    if chkc then return chkc:IsLocation(LOCATION_MZONE) and chkc:IsFaceup() and (not filter or filter(chkc, e, tp)) end
                    if chk==0 then return Duel.IsExistingTarget(function(mc) return mc:IsFaceup() and (not filter or filter(mc, e, tp)) end, tp, LOCATION_MZONE, LOCATION_MZONE, 1, nil) end
                    Duel.Hint(HINT_SELECTMSG, tp, HINTMSG_EQUIP)
                    Duel.SelectTarget(tp, function(mc) return mc:IsFaceup() and (not filter or filter(mc, e, tp)) end, tp, LOCATION_MZONE, LOCATION_MZONE, 1, 1, nil)
                    Duel.SetOperationInfo(0, CATEGORY_EQUIP, e:GetHandler(), 1, 0, 0)
                end)
                e1:SetOperation(function(e,tp,eg,ep,ev,re,r,rp)
                    local tc=Duel.GetFirstTarget()
                    if e:GetHandler():IsRelateToEffect(e) and tc and tc:IsRelateToEffect(e) and tc:IsFaceup() then
                        Duel.Equip(tp, e:GetHandler(), tc)
                    end
                    if op then op(e,tp,eg,ep,ev,re,r,rp) end
                end)
                c:RegisterEffect(e1)
                local e2=Effect.CreateEffect(c)
                e2:SetType(EFFECT_TYPE_SINGLE)
                e2:SetCode(EFFECT_EQUIP_LIMIT)
                e2:SetProperty(EFFECT_FLAG_CANNOT_DISABLE)
                e2:SetValue(function(e,tc)
                    return tc and tc:IsFaceup() and (not filter or filter(tc, e, tp))
                end)
                c:RegisterEffect(e2)
            end
        "));
        
        auxTable.Table.Set("AddRitualProcedure", DynValue.FromObject(luaEngine,
            (System.Action<object, object, object, object, object>)
            ((card, filter, ritual_level, ritual_attr, operation) => {
            })));
        
        auxTable.Table.Set("Next", DynValue.FromObject(luaEngine,
            (System.Func<object, DynValue>)(group => {
                if (group is LuaGroup g) return g.Iter();
                return DynValue.NewCallback((context, args) => DynValue.Nil);
            })));

        auxTable.Table.Set("AddValuesReset", DynValue.FromObject(luaEngine,
            (System.Action<Closure>)(cb => {
                if (cb != null && CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null) {
                    CardEffectManager.Instance.luaDuel.endTurnCallbacks.Add(cb);
                }
            })));
        
        auxTable.Table.Set("NecroValleyFilter", DynValue.FromObject(luaEngine,
            (System.Func<object, object>)(filterFunc => {
                // Retorna a função de filtro original intacta. 
                // Mantém a restrição da carta original (Ex: Agido limitando a Fadas).
                return filterFunc;
            })));

        auxTable.Table.Set("RemainFieldCost", luaEngine.DoString(@"
            return function(e, tp, eg, ep, ev, re, r, rp, chk)
                if chk == 0 then return true end
                return true
            end
        "));

        dummyClosureTrue = auxTable.Table.Get("TRUE").Function;
        auxTable.Table.Set("FaceupFilter", auxTable.Table.Get("FilterFaceupFunction")); // Alias vital para scripts modernos
        auxTable.Table.Set("Filter", auxTable.Table.Get("FilterBoolFunction")); // Alias de segurança
        luaEngine.Globals["aux"] = auxTable;

        // Previne o crash "attempt to call a nil value" criando uma metatable de fallback na tabela 'aux'
        // Caso um script Lua invoque uma função não implementada (ex: aux.AddRitualProcGreater), não haverá crash.
        luaEngine.DoString(@"
            local auxMt = getmetatable(aux) or {}
            auxMt.__index = function(t, k)
                return function(...) return true end
            end
            setmetatable(aux, auxMt)
        ");

        string[] procTables = { "Toon", "Union", "Gemini", "Pendulum", "Link" };
        foreach (var pName in procTables) luaEngine.Globals[pName] = DynValue.NewTable(luaEngine);

        InjectVitalConstants();
        Debug.Log("[MoonSharp] Motor LUA Inicializado e pronto para interpretar OCGCore!");
    }

    private void InjectVitalConstants()
    {
        luaEngine.Globals["LOCATION_DECK"] = 0x1;
        luaEngine.Globals["LOCATION_HAND"] = 0x2;
        luaEngine.Globals["LOCATION_MZONE"] = 0x4;
        luaEngine.Globals["LOCATION_SZONE"] = 0x8;
        luaEngine.Globals["LOCATION_GRAVE"] = 0x10;
        luaEngine.Globals["LOCATION_REMOVED"] = 0x20;
        luaEngine.Globals["LOCATION_EXTRA"] = 0x40;
        luaEngine.Globals["LOCATION_OVERLAY"] = 0x80;
        luaEngine.Globals["LOCATION_FZONE"] = 0x100;
        luaEngine.Globals["LOCATION_PZONE"] = 0x200;
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8; 
        luaEngine.Globals["LOCATION_PUBLIC"] = 0x4 | 0x8 | 0x10 | 0x20;  
        luaEngine.Globals["LOCATION_ALL"] = 0x3ff;
        luaEngine.Globals["LOCATION_STZONE"] = 0x400;
        luaEngine.Globals["LOCATION_MMZONE"] = 0x800;
        luaEngine.Globals["LOCATION_EMZONE"] = 0x1000;
        luaEngine.Globals["LOCATION_DECKBOT"] = 0x10001;
        luaEngine.Globals["LOCATION_DECKSHF"] = 0x20001;
        luaEngine.Globals["ZONE_CENTER_MMZ"] = 0x4;
        luaEngine.Globals["ZONES_MMZ"] = 0x1f;
        luaEngine.Globals["ZONES_EMZ"] = 0x60;
        luaEngine.Globals["SEQ_DECKTOP"] = 0;
        luaEngine.Globals["SEQ_DECKBOTTOM"] = 1;
        luaEngine.Globals["SEQ_DECKSHUFFLE"] = 2;
        luaEngine.Globals["COIN_HEADS"] = 1;
        luaEngine.Globals["COIN_TAILS"] = 0;
        luaEngine.Globals["NO_FLIP_EFFECT"] = 0x10000;
        
        luaEngine.Globals["TYPE_MONSTER"] = 0x1;
        luaEngine.Globals["TYPE_SPELL"] = 0x2;
        luaEngine.Globals["TYPE_TRAP"] = 0x4;
        luaEngine.Globals["TYPE_NORMAL"] = 0x10;
        luaEngine.Globals["TYPE_EFFECT"] = 0x20;
        luaEngine.Globals["TYPE_FUSION"] = 0x40;
        luaEngine.Globals["TYPE_RITUAL"] = 0x80;
        luaEngine.Globals["TYPE_TRAPMONSTER"] = 0x100;
        luaEngine.Globals["TYPE_QUICKPLAY"] = 0x10000;
        luaEngine.Globals["TYPE_CONTINUOUS"] = 0x20000;
        luaEngine.Globals["TYPE_EQUIP"] = 0x40000;
        luaEngine.Globals["TYPE_FIELD"] = 0x80000;
        luaEngine.Globals["TYPE_COUNTER"] = 0x100000;
        luaEngine.Globals["TYPE_FLIP"] = 0x200000;
        luaEngine.Globals["TYPE_SPIRIT"] = 0x200;
        luaEngine.Globals["TYPE_UNION"] = 0x400;
        luaEngine.Globals["TYPE_GEMINI"] = 0x800;
        luaEngine.Globals["TYPE_TUNER"] = 0x1000;
        luaEngine.Globals["TYPE_SYNCHRO"] = 0x2000;
        luaEngine.Globals["TYPE_TOKEN"] = 0x4000;
        luaEngine.Globals["TYPE_MAXIMUM"] = 0x8000;
        luaEngine.Globals["TYPE_TOON"] = 0x400000;
        luaEngine.Globals["TYPE_XYZ"] = 0x800000;
        luaEngine.Globals["TYPE_PENDULUM"] = 0x1000000;
        luaEngine.Globals["TYPE_SPSUMMON"] = 0x2000000;
        luaEngine.Globals["TYPE_LINK"] = 0x4000000;
        luaEngine.Globals["TYPE_SKILL"] = 0x8000000;
        luaEngine.Globals["TYPE_ACTION"] = 0x10000000;
        luaEngine.Globals["TYPE_PLUS"] = 0x20000000;
        luaEngine.Globals["TYPE_MINUS"] = 0x40000000;
        luaEngine.Globals["TYPE_ARMOR"] = 0x80000000L;
        luaEngine.Globals["TYPES_TOKEN"] = 0x1 | 0x10 | 0x4000;
        luaEngine.Globals["TYPE_EXTRA"] = 0x40 | 0x2000 | 0x800000 | 0x4000000;
        luaEngine.Globals["TYPE_PLUSMINUS"] = 0x20000000 | 0x40000000;

        luaEngine.Globals["EVENT_STARTUP"] = 1000;
        luaEngine.Globals["EVENT_FLIP"] = 1001;
        luaEngine.Globals["EVENT_FREE_CHAIN"] = 1002;
        luaEngine.Globals["EVENT_DESTROY"] = 1010;
        luaEngine.Globals["EVENT_REMOVE"] = 1011;
        luaEngine.Globals["EVENT_TO_HAND"] = 1012;
        luaEngine.Globals["EVENT_TO_DECK"] = 1013;
        luaEngine.Globals["EVENT_TO_GRAVE"] = 1014;
        luaEngine.Globals["EVENT_LEAVE_FIELD"] = 1015;
        luaEngine.Globals["EVENT_CHANGE_POS"] = 1016;
        luaEngine.Globals["EVENT_RELEASE"] = 1017;
        luaEngine.Globals["EVENT_DISCARD"] = 1018;
        luaEngine.Globals["EVENT_LEAVE_FIELD_P"] = 1019;
        luaEngine.Globals["EVENT_CHAIN_SOLVING"] = 1020;
        luaEngine.Globals["EVENT_CHAIN_ACTIVATING"] = 1021;
        luaEngine.Globals["EVENT_CHAIN_SOLVED"] = 1022;
        luaEngine.Globals["EVENT_CHAIN_NEGATED"] = 1024;
        luaEngine.Globals["EVENT_CHAIN_DISABLED"] = 1025;
        luaEngine.Globals["EVENT_CHAIN_END"] = 1026;
        luaEngine.Globals["EVENT_CHAINING"] = 1027;
        luaEngine.Globals["EVENT_BECOME_TARGET"] = 1028;
        luaEngine.Globals["EVENT_DESTROYED"] = 1029;
        luaEngine.Globals["EVENT_MOVE"] = 1030;
        luaEngine.Globals["EVENT_LEAVE_GRAVE"] = 1031;
        luaEngine.Globals["EVENT_ADJUST"] = 1040;
        luaEngine.Globals["EVENT_BREAK_EFFECT"] = 1050;
        luaEngine.Globals["EVENT_SUMMON_SUCCESS"] = 1100;
        luaEngine.Globals["EVENT_FLIP_SUMMON_SUCCESS"] = 1101;
        luaEngine.Globals["EVENT_SPSUMMON_SUCCESS"] = 1102;
        luaEngine.Globals["EVENT_SUMMON"] = 1103;
        luaEngine.Globals["EVENT_FLIP_SUMMON"] = 1104;
        luaEngine.Globals["EVENT_SPSUMMON"] = 1105;
        luaEngine.Globals["EVENT_MSET"] = 1106;
        luaEngine.Globals["EVENT_SSET"] = 1107;
        luaEngine.Globals["EVENT_BE_MATERIAL"] = 1108;
        luaEngine.Globals["EVENT_BE_PRE_MATERIAL"] = 1109;
        luaEngine.Globals["EVENT_DRAW"] = 1110;
        luaEngine.Globals["EVENT_DAMAGE"] = 1111;
        luaEngine.Globals["EVENT_RECOVER"] = 1112;
        luaEngine.Globals["EVENT_PREDRAW"] = 1113;
        luaEngine.Globals["EVENT_SUMMON_NEGATED"] = 1114;
        luaEngine.Globals["EVENT_FLIP_SUMMON_NEGATED"] = 1115;
        luaEngine.Globals["EVENT_SPSUMMON_NEGATED"] = 1116;
        luaEngine.Globals["EVENT_CONTROL_CHANGED"] = 1120;
        luaEngine.Globals["EVENT_EQUIP"] = 1121;
        luaEngine.Globals["EVENT_ATTACK_ANNOUNCE"] = 1130;
        luaEngine.Globals["EVENT_BE_BATTLE_TARGET"] = 1131;
        luaEngine.Globals["EVENT_BATTLE_START"] = 1132;
        luaEngine.Globals["EVENT_BATTLE_CONFIRM"] = 1133;
        luaEngine.Globals["EVENT_PRE_DAMAGE_CALCULATE"] = 1134;
        luaEngine.Globals["EVENT_DAMAGE_CALCULATING"] = 1135;
        luaEngine.Globals["EVENT_PRE_BATTLE_DAMAGE"] = 1136;
        luaEngine.Globals["EVENT_BATTLE_END"] = 1137;
        luaEngine.Globals["EVENT_BATTLED"] = 1138;
        luaEngine.Globals["EVENT_BATTLE_DESTROYING"] = 1139;
        luaEngine.Globals["EVENT_BATTLE_DESTROYED"] = 1140;
        luaEngine.Globals["EVENT_DAMAGE_STEP_END"] = 1141;
        luaEngine.Globals["EVENT_ATTACK_DISABLED"] = 1142;
        luaEngine.Globals["EVENT_BATTLE_DAMAGE"] = 1143;
        luaEngine.Globals["EVENT_TOSS_DICE"] = 1150;
        luaEngine.Globals["EVENT_TOSS_COIN"] = 1151;
        luaEngine.Globals["EVENT_TOSS_COIN_NEGATE"] = 1152;
        luaEngine.Globals["EVENT_TOSS_DICE_NEGATE"] = 1153;
        luaEngine.Globals["EVENT_LEVEL_UP"] = 1200;
        luaEngine.Globals["EVENT_PAY_LPCOST"] = 1201;
        luaEngine.Globals["EVENT_DETACH_MATERIAL"] = 1202;
        luaEngine.Globals["EVENT_TURN_END"] = 1210;
        luaEngine.Globals["EVENT_CONFIRM"] = 1211;
        luaEngine.Globals["EVENT_TOHAND_CONFIRM"] = 1212;
        luaEngine.Globals["EVENT_PHASE"] = 0x1000;
        luaEngine.Globals["EVENT_PHASE_START"] = 0x2000;
        luaEngine.Globals["EVENT_ADD_COUNTER"] = 0x10000;
        luaEngine.Globals["EVENT_REMOVE_COUNTER"] = 0x20000;
        luaEngine.Globals["EVENT_CUSTOM"] = 0x10000000;
        
        luaEngine.Globals["STATUS_SUMMON_TURN"] = 0x800;
        luaEngine.Globals["STATUS_FLIP_SUMMON_TURN"] = 0x20000000;
        luaEngine.Globals["STATUS_SPSUMMON_TURN"] = 0x40000000;
        
        luaEngine.Globals["STATUS_DISABLED"] = 0x1;
        luaEngine.Globals["STATUS_TO_ENABLE"] = 0x2;
        luaEngine.Globals["STATUS_TO_DISABLE"] = 0x4;
        luaEngine.Globals["STATUS_PROC_COMPLETE"] = 0x8;
        luaEngine.Globals["STATUS_SET_TURN"] = 0x10;
        luaEngine.Globals["STATUS_NO_LEVEL"] = 0x20;
        luaEngine.Globals["STATUS_BATTLE_RESULT"] = 0x40;
        luaEngine.Globals["STATUS_SPSUMMON_STEP"] = 0x80;
        luaEngine.Globals["STATUS_FORM_CHANGED"] = 0x100;
        luaEngine.Globals["STATUS_SUMMONING"] = 0x200;
        luaEngine.Globals["STATUS_EFFECT_ENABLED"] = 0x400;
        luaEngine.Globals["STATUS_DESTROY_CONFIRMED"] = 0x1000;
        luaEngine.Globals["STATUS_LEAVE_CONFIRMED"] = 0x2000;
        luaEngine.Globals["STATUS_BATTLE_DESTROYED"] = 0x4000;
        luaEngine.Globals["STATUS_COPYING_EFFECT"] = 0x8000;
        luaEngine.Globals["STATUS_CHAINING"] = 0x10000;
        luaEngine.Globals["STATUS_SUMMON_DISABLED"] = 0x20000;
        luaEngine.Globals["STATUS_ACTIVATE_DISABLED"] = 0x40000;
        luaEngine.Globals["STATUS_EFFECT_REPLACED"] = 0x80000;
        luaEngine.Globals["STATUS_FUTURE_FUSION"] = 0x100000;
        luaEngine.Globals["STATUS_ATTACK_CANCELED"] = 0x200000;
        luaEngine.Globals["STATUS_INITIALIZING"] = 0x400000;
        luaEngine.Globals["STATUS_ACTIVATED"] = 0x800000;
        luaEngine.Globals["STATUS_JUST_POS"] = 0x1000000;
        luaEngine.Globals["STATUS_CONTINUOUS_POS"] = 0x2000000;
        luaEngine.Globals["STATUS_FORBIDDEN"] = 0x4000000;
        luaEngine.Globals["STATUS_ACT_FROM_HAND"] = 0x8000000;
        luaEngine.Globals["STATUS_OPPO_BATTLE"] = 0x10000000;

        luaEngine.Globals["GLOBALFLAG_DECK_REVERSE_CHECK"] = 0x1;

        luaEngine.Globals["EFFECT_TYPE_SINGLE"] = 0x0001;
        luaEngine.Globals["EFFECT_TYPE_FIELD"] = 0x0002;
        luaEngine.Globals["EFFECT_TYPE_EQUIP"] = 0x0004;
        luaEngine.Globals["EFFECT_TYPE_ACTIONS"] = 0x0008;
        luaEngine.Globals["EFFECT_TYPE_ACTIVATE"] = 0x0010;
        luaEngine.Globals["EFFECT_TYPE_FLIP"] = 0x0020;
        luaEngine.Globals["EFFECT_TYPE_IGNITION"] = 0x0040;
        luaEngine.Globals["EFFECT_TYPE_TRIGGER_O"] = 0x0080;
        luaEngine.Globals["EFFECT_TYPE_QUICK_O"] = 0x0100;
        luaEngine.Globals["EFFECT_TYPE_TRIGGER_F"] = 0x0200;
        luaEngine.Globals["EFFECT_TYPE_QUICK_F"] = 0x0400;
        luaEngine.Globals["EFFECT_TYPE_CONTINUOUS"] = 0x0800;

        // Constantes Vitais de Categoria e Corrente (Para cura, dano e destruição funcionarem)
        luaEngine.Globals["CATEGORY_DESTROY"] = 0x1;
        luaEngine.Globals["CATEGORY_RELEASE"] = 0x2;
        luaEngine.Globals["CATEGORY_REMOVE"] = 0x4;
        luaEngine.Globals["CATEGORY_TOHAND"] = 0x8;
        luaEngine.Globals["CATEGORY_TODECK"] = 0x10;
        luaEngine.Globals["CATEGORY_TOGRAVE"] = 0x20;
        luaEngine.Globals["CATEGORY_DECKDES"] = 0x40;
        luaEngine.Globals["CATEGORY_HANDES"] = 0x80;
        luaEngine.Globals["CATEGORY_SUMMON"] = 0x100;
        luaEngine.Globals["CATEGORY_SPECIAL_SUMMON"] = 0x200;
        luaEngine.Globals["CATEGORY_TOKEN"] = 0x400;
        luaEngine.Globals["CATEGORY_POSITION"] = 0x800;
        luaEngine.Globals["CATEGORY_CONTROL"] = 0x1000;
        luaEngine.Globals["CATEGORY_SET"] = 0x100000000L;
        luaEngine.Globals["CATEGORY_DISABLE"] = 0x2000;
        luaEngine.Globals["CATEGORY_DISABLE_SUMMON"] = 0x4000;
        luaEngine.Globals["CATEGORY_DRAW"] = 0x10000;
        luaEngine.Globals["CATEGORY_SEARCH"] = 0x20000;
        luaEngine.Globals["CATEGORY_EQUIP"] = 0x40000;
        luaEngine.Globals["CATEGORY_DAMAGE"] = 0x80000;
        luaEngine.Globals["CATEGORY_RECOVER"] = 0x100000;
        luaEngine.Globals["CATEGORY_ATKCHANGE"] = 0x200000;
        luaEngine.Globals["CATEGORY_DEFCHANGE"] = 0x400000;
        luaEngine.Globals["CATEGORY_DICE"] = 0x2000000;
        luaEngine.Globals["CATEGORY_COUNTER"] = 0x800000;
        luaEngine.Globals["CATEGORY_FLIP"] = 0x800;
        luaEngine.Globals["CATEGORY_LEAVE_GRAVE"] = 0x4000000;
        luaEngine.Globals["CATEGORY_LVCHANGE"] = 0x8000000;
        luaEngine.Globals["CATEGORY_NEGATE"] = 0x10000000;
        luaEngine.Globals["CATEGORY_ANNOUNCE"] = 0x20000000;
        luaEngine.Globals["CATEGORY_FUSION_SUMMON"] = 0x40000000;
        luaEngine.Globals["CATEGORY_TOEXTRA"] = 0x80000000L;
        luaEngine.Globals["CATEGORY_COIN"] = 0x1000000;
        
        luaEngine.Globals["REASON_EFFECT"] = 0x40;
        luaEngine.Globals["REASON_SUMMON"] = 0x20;
        luaEngine.Globals["REASON_MATERIAL"] = 0x80;
        luaEngine.Globals["REASON_DESTROY"] = 0x1;
        luaEngine.Globals["REASON_RELEASE"] = 0x2;
        luaEngine.Globals["REASON_TEMPORARY"] = 0x4;
        luaEngine.Globals["REASON_BATTLE"] = 0x20;
        luaEngine.Globals["REASON_COST"] = 0x80;
        luaEngine.Globals["REASON_ADJUST"] = 0x100;
        luaEngine.Globals["REASON_LOST_TARGET"] = 0x200;
        luaEngine.Globals["REASON_RULE"] = 0x400;
        luaEngine.Globals["REASON_SPSUMMON"] = 0x800;
        luaEngine.Globals["REASON_DISSUMMON"] = 0x1000;
        luaEngine.Globals["REASON_FLIP"] = 0x2000;
        luaEngine.Globals["REASON_DISCARD"] = 0x4000;
        luaEngine.Globals["REASON_RDAMAGE"] = 0x8000;
        luaEngine.Globals["REASON_RRECOVER"] = 0x10000;
        luaEngine.Globals["REASON_RETURN"] = 0x20000;
        luaEngine.Globals["REASON_FUSION"] = 0x40000;
        luaEngine.Globals["REASON_SYNCHRO"] = 0x80000;
        luaEngine.Globals["REASON_RITUAL"] = 0x100000;
        luaEngine.Globals["REASON_XYZ"] = 0x200000;
        luaEngine.Globals["REASON_REPLACE"] = 0x1000000;
        luaEngine.Globals["REASON_DRAW"] = 0x2000000;
        luaEngine.Globals["REASON_REDIRECT"] = 0x4000000;
        luaEngine.Globals["REASON_EXCAVATE"] = 0x8000000;
        luaEngine.Globals["REASON_LINK"] = 0x10000000;
        luaEngine.Globals["REASON_REVEAL"] = 0x8000000;

        luaEngine.Globals["LOCATION_REASON_TOFIELD"] = 0x1;
        luaEngine.Globals["LOCATION_REASON_CONTROL"] = 0x2;
        luaEngine.Globals["LOCATION_REASON_COUNT"] = 0x4;
        luaEngine.Globals["LOCATION_REASON_RETURN"] = 0x8;

        luaEngine.Globals["CHAININFO_TRIGGERING_EFFECT"] = 1;
        luaEngine.Globals["CHAININFO_TRIGGERING_PLAYER"] = 2;
        luaEngine.Globals["CHAININFO_TRIGGERING_CONTROLER"] = 3;
        luaEngine.Globals["CHAININFO_TRIGGERING_LOCATION"] = 4;
        luaEngine.Globals["CHAININFO_TRIGGERING_LOCATION_SYMBOLIC"] = 5;
        luaEngine.Globals["CHAININFO_TRIGGERING_SEQUENCE"] = 6;
        luaEngine.Globals["CHAININFO_TRIGGERING_SEQUENCE_SYMBOLIC"] = 7;
        luaEngine.Globals["CHAININFO_TARGET_CARDS"] = 8;
        luaEngine.Globals["CHAININFO_TARGET_PLAYER"] = 9;
        luaEngine.Globals["CHAININFO_TARGET_PARAM"] = 10;
        luaEngine.Globals["CHAININFO_DISABLE_REASON"] = 11;
        luaEngine.Globals["CHAININFO_DISABLE_PLAYER"] = 12;
        luaEngine.Globals["CHAININFO_CHAIN_ID"] = 13;
        luaEngine.Globals["CHAININFO_TYPE"] = 14;
        luaEngine.Globals["CHAININFO_EXTTYPE"] = 15;
        luaEngine.Globals["CHAININFO_TRIGGERING_POSITION"] = 16;
        luaEngine.Globals["CHAININFO_TRIGGERING_CODE"] = 17;
        luaEngine.Globals["CHAININFO_TRIGGERING_CODE2"] = 18;
        luaEngine.Globals["CHAININFO_TRIGGERING_TYPE"] = 19;
        luaEngine.Globals["CHAININFO_TRIGGERING_LEVEL"] = 20;
        luaEngine.Globals["CHAININFO_TRIGGERING_RANK"] = 21;
        luaEngine.Globals["CHAININFO_TRIGGERING_ATTRIBUTE"] = 22;
        luaEngine.Globals["CHAININFO_TRIGGERING_RACE"] = 23;
        luaEngine.Globals["CHAININFO_TRIGGERING_ATTACK"] = 24;
        luaEngine.Globals["CHAININFO_TRIGGERING_DEFENSE"] = 25;
        luaEngine.Globals["CHAININFO_TRIGGERING_STATUS"] = 26;
        luaEngine.Globals["CHAININFO_TRIGGERING_SUMMON_LOCATION"] = 27;
        luaEngine.Globals["CHAININFO_TRIGGERING_SUMMON_TYPE"] = 28;
        luaEngine.Globals["CHAININFO_TRIGGERING_SUMMON_PROC_COMPLETE"] = 29;
        luaEngine.Globals["CHAININFO_TRIGGERING_SETCODES"] = 30;
        
        luaEngine.Globals["RACE_ALL"] = 0x3ffffff;
        luaEngine.Globals["CHINT_RACE"] = 3;
        luaEngine.Globals["CHINT_ATTRIBUTE"] = 4;
        luaEngine.Globals["CHINT_NUMBER"] = 5;
        luaEngine.Globals["HINT_SELECTMSG"] = 3;
        luaEngine.Globals["HINTMSG_RACE"] = 563;
        luaEngine.Globals["HINT_EVENT"] = 1;
        luaEngine.Globals["HINT_MESSAGE"] = 2;
        luaEngine.Globals["HINT_OPSELECTED"] = 4;
        luaEngine.Globals["HINT_EFFECT"] = 5;
        luaEngine.Globals["HINT_RACE"] = 6;
        luaEngine.Globals["HINT_ATTRIB"] = 7;
        luaEngine.Globals["HINT_CODE"] = 8;
        luaEngine.Globals["HINT_NUMBER"] = 9;
        luaEngine.Globals["HINT_CARD"] = 10;
        luaEngine.Globals["HINT_ZONE"] = 11;
        luaEngine.Globals["HINT_SKILL"] = 200;
        luaEngine.Globals["HINT_SKILL_COVER"] = 201;
        luaEngine.Globals["HINT_SKILL_FLIP"] = 202;
        luaEngine.Globals["HINT_SKILL_REMOVE"] = 203;
        luaEngine.Globals["CHINT_TURN"] = 1;
        luaEngine.Globals["CHINT_CARD"] = 2;
        luaEngine.Globals["CHINT_DESC_ADD"] = 6;
        luaEngine.Globals["CHINT_DESC_REMOVE"] = 7;
        luaEngine.Globals["PHINT_DESC_ADD"] = 6;
        luaEngine.Globals["PHINT_DESC_REMOVE"] = 7;

        // Constantes de Raça e Atributo (Essenciais para Filtros funcionarem)
        luaEngine.Globals["RACE_WARRIOR"] = 0x1;
        luaEngine.Globals["RACE_SPELLCASTER"] = 0x2;
        luaEngine.Globals["RACE_FAIRY"] = 0x4;
        luaEngine.Globals["RACE_FIEND"] = 0x8;
        luaEngine.Globals["RACE_ZOMBIE"] = 0x10;
        luaEngine.Globals["RACE_MACHINE"] = 0x20;
        luaEngine.Globals["RACE_AQUA"] = 0x40;
        luaEngine.Globals["RACE_PYRO"] = 0x80;
        luaEngine.Globals["RACE_ROCK"] = 0x100;
        luaEngine.Globals["RACE_WINGEDBEAST"] = 0x200;
        luaEngine.Globals["RACE_PLANT"] = 0x400;
        luaEngine.Globals["RACE_INSECT"] = 0x800;
        luaEngine.Globals["RACE_THUNDER"] = 0x1000;
        luaEngine.Globals["RACE_DRAGON"] = 0x2000;
        luaEngine.Globals["RACE_BEAST"] = 0x4000;
        luaEngine.Globals["RACE_BEAST_WARRIOR"] = 0x8000;
        luaEngine.Globals["RACE_BEASTWARRIOR"] = 0x8000;
        luaEngine.Globals["RACE_DINOSAUR"] = 0x10000;
        luaEngine.Globals["RACE_FISH"] = 0x20000;
        luaEngine.Globals["RACE_SEA_SERPENT"] = 0x40000;
        luaEngine.Globals["RACE_REPTILE"] = 0x80000;
        luaEngine.Globals["RACE_PSYCHIC"] = 0x100000;
        luaEngine.Globals["RACE_DIVINE"] = 0x200000;
        luaEngine.Globals["RACE_CREATORGOD"] = 0x400000;
        luaEngine.Globals["RACE_WYRM"] = 0x800000;
        luaEngine.Globals["RACE_CYBERSE"] = 0x1000000;
        luaEngine.Globals["RACE_ILLUSION"] = 0x2000000;
        luaEngine.Globals["RACE_CYBORG"] = 0x4000000;
        luaEngine.Globals["RACE_MAGICALKNIGHT"] = 0x8000000;
        luaEngine.Globals["RACE_HIGHDRAGON"] = 0x10000000;
        luaEngine.Globals["RACE_OMEGAPSYCHIC"] = 0x20000000;
        luaEngine.Globals["RACE_CELESTIALWARRIOR"] = 0x40000000;
        luaEngine.Globals["RACE_GALAXY"] = 0x80000000L;
        luaEngine.Globals["RACE_YOKAI"] = 0x4000000000000000L;
        luaEngine.Globals["RACES_BEAST_BWARRIOR_WINGB"] = 0x4000 | 0x8000 | 0x200;

        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x01;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x02;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x04;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x08;
        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_DIVINE"] = 0x40;
        luaEngine.Globals["ATTRIBUTE_ALL"] = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 | 0x40;

        // Constantes de Arquétipo (Setcodes)
        luaEngine.Globals["SET_AMAZONESS"] = 0x04;
        luaEngine.Globals["SET_ARCHFIEND"] = 0x45;
        luaEngine.Globals["SET_BATTERYMAN"] = 0x28;
        luaEngine.Globals["SET_GAIA_THE_FIERCE_KNIGHT"] = 0x1048;
        
        luaEngine.Globals["CARD_JINZO"] = 77585513;
        luaEngine.Globals["CARD_HARPIE_LADY_SISTERS"] = 12206212;
        luaEngine.Globals["CARD_NECROVALLEY"] = 47355498;
        luaEngine.Globals["CARD_BLUEEYES_SPIRIT"] = 89399912;

        // Constantes de Status Oficiais do OCGCore
        luaEngine.Globals["EFFECT_IMMUNE_EFFECT"] = 1;
        luaEngine.Globals["EFFECT_DISABLE"] = 2;
        luaEngine.Globals["EFFECT_CANNOT_DISABLE"] = 3;
        luaEngine.Globals["EFFECT_SET_CONTROL"] = 4;
        luaEngine.Globals["EFFECT_CANNOT_CHANGE_CONTROL"] = 5;
        luaEngine.Globals["EFFECT_CANNOT_ACTIVATE"] = 6;
        luaEngine.Globals["EFFECT_CANNOT_TRIGGER"] = 7;
        luaEngine.Globals["EFFECT_DISABLE_EFFECT"] = 8;
        luaEngine.Globals["EFFECT_DISABLE_CHAIN"] = 9;
        luaEngine.Globals["EFFECT_DISABLE_TRAPMONSTER"] = 10;
        luaEngine.Globals["EFFECT_CANNOT_INACTIVATE"] = 12;
        luaEngine.Globals["EFFECT_CANNOT_DISEFFECT"] = 13;
        luaEngine.Globals["EFFECT_CANNOT_CHANGE_POSITION"] = 14;
        luaEngine.Globals["EFFECT_TRAP_ACT_IN_HAND"] = 15;
        luaEngine.Globals["EFFECT_TRAP_ACT_IN_SET_TURN"] = 16;
        luaEngine.Globals["EFFECT_REMAIN_FIELD"] = 17;
        luaEngine.Globals["EFFECT_MONSTER_SSET"] = 18;
        luaEngine.Globals["EFFECT_QP_ACT_IN_SET_TURN"] = 19;
        luaEngine.Globals["EFFECT_CANNOT_SUMMON"] = 20;
        luaEngine.Globals["EFFECT_CANNOT_FLIP_SUMMON"] = 21;
        luaEngine.Globals["EFFECT_CANNOT_SPECIAL_SUMMON"] = 22;
        luaEngine.Globals["EFFECT_CANNOT_MSET"] = 23;
        luaEngine.Globals["EFFECT_CANNOT_SSET"] = 24;
        luaEngine.Globals["EFFECT_CANNOT_DRAW"] = 25;
        luaEngine.Globals["EFFECT_CANNOT_DISABLE_SUMMON"] = 26;
        luaEngine.Globals["EFFECT_CANNOT_DISABLE_SPSUMMON"] = 27;
        luaEngine.Globals["EFFECT_SET_SUMMON_COUNT_LIMIT"] = 28;
        luaEngine.Globals["EFFECT_EXTRA_SUMMON_COUNT"] = 29;
        luaEngine.Globals["EFFECT_SPSUMMON_CONDITION"] = 30;
        luaEngine.Globals["EFFECT_REVIVE_LIMIT"] = 31;
        luaEngine.Globals["EFFECT_SUMMON_PROC"] = 32;
        luaEngine.Globals["EFFECT_LIMIT_SUMMON_PROC"] = 33;
        luaEngine.Globals["EFFECT_SPSUMMON_PROC"] = 34;
        luaEngine.Globals["EFFECT_EXTRA_SET_COUNT"] = 35;
        luaEngine.Globals["EFFECT_SET_PROC"] = 36;
        luaEngine.Globals["EFFECT_LIMIT_SET_PROC"] = 37;
        luaEngine.Globals["EFFECT_LIGHT_OF_INTERVENTION"] = 38;
        luaEngine.Globals["EFFECT_CANNOT_DISABLE_FLIP_SUMMON"] = 39;
        luaEngine.Globals["EFFECT_INDESTRUCTABLE"] = 40;
        luaEngine.Globals["EFFECT_INDESTRUCTABLE_EFFECT"] = 41;
        luaEngine.Globals["EFFECT_INDESTRUCTABLE_BATTLE"] = 42;
        luaEngine.Globals["EFFECT_UNRELEASABLE_SUM"] = 43;
        luaEngine.Globals["EFFECT_UNRELEASABLE_NONSUM"] = 44;
        luaEngine.Globals["EFFECT_DESTROY_SUBSTITUTE"] = 45;
        luaEngine.Globals["EFFECT_CANNOT_RELEASE"] = 46;
        luaEngine.Globals["EFFECT_INDESTRUCTABLE_COUNT"] = 47;
        luaEngine.Globals["EFFECT_UNRELEASABLE_EFFECT"] = 48;
        luaEngine.Globals["EFFECT_DESTROY_REPLACE"] = 50;
        luaEngine.Globals["EFFECT_RELEASE_REPLACE"] = 51;
        luaEngine.Globals["EFFECT_SEND_REPLACE"] = 52;
        luaEngine.Globals["EFFECT_CANNOT_DISCARD_HAND"] = 55;
        luaEngine.Globals["EFFECT_CANNOT_DISCARD_DECK"] = 56;
        luaEngine.Globals["EFFECT_CANNOT_USE_AS_COST"] = 57;
        luaEngine.Globals["EFFECT_CANNOT_PLACE_COUNTER"] = 58;
        luaEngine.Globals["EFFECT_CANNOT_TO_GRAVE_AS_COST"] = 59;
        luaEngine.Globals["EFFECT_LEAVE_FIELD_REDIRECT"] = 60;
        luaEngine.Globals["EFFECT_TO_HAND_REDIRECT"] = 61;
        luaEngine.Globals["EFFECT_TO_DECK_REDIRECT"] = 62;
        luaEngine.Globals["EFFECT_TO_GRAVE_REDIRECT"] = 63;
        luaEngine.Globals["EFFECT_REMOVE_REDIRECT"] = 64;
        luaEngine.Globals["EFFECT_CANNOT_TO_HAND"] = 65;
        luaEngine.Globals["EFFECT_CANNOT_TO_DECK"] = 66;
        luaEngine.Globals["EFFECT_CANNOT_REMOVE"] = 67;
        luaEngine.Globals["EFFECT_CANNOT_TO_GRAVE"] = 68;
        luaEngine.Globals["EFFECT_CANNOT_TURN_SET"] = 69;
        luaEngine.Globals["EFFECT_CANNOT_BE_BATTLE_TARGET"] = 70;
        luaEngine.Globals["EFFECT_CANNOT_BE_EFFECT_TARGET"] = 71;
        luaEngine.Globals["EFFECT_IGNORE_BATTLE_TARGET"] = 72;
        luaEngine.Globals["EFFECT_CANNOT_DIRECT_ATTACK"] = 73;
        luaEngine.Globals["EFFECT_DIRECT_ATTACK"] = 74;
        luaEngine.Globals["EFFECT_GEMINI_STATUS"] = 75;
        luaEngine.Globals["EFFECT_EQUIP_LIMIT"] = 76;
        luaEngine.Globals["EFFECT_GEMINI_SUMMONABLE"] = 77;
        luaEngine.Globals["EFFECT_UNION_LIMIT"] = 78;
        luaEngine.Globals["EFFECT_REVERSE_DAMAGE"] = 80;
        luaEngine.Globals["EFFECT_REVERSE_RECOVER"] = 81;
        luaEngine.Globals["EFFECT_CHANGE_DAMAGE"] = 82;
        luaEngine.Globals["EFFECT_REFLECT_DAMAGE"] = 83;
        luaEngine.Globals["EFFECT_CANNOT_ATTACK"] = 85;
        luaEngine.Globals["EFFECT_CANNOT_ATTACK_ANNOUNCE"] = 86;
        luaEngine.Globals["EFFECT_CANNOT_CHANGE_POS_E"] = 87;
        luaEngine.Globals["EFFECT_ACTIVATE_COST"] = 90;
        luaEngine.Globals["EFFECT_SUMMON_COST"] = 91;
        luaEngine.Globals["EFFECT_SPSUMMON_COST"] = 92;
        luaEngine.Globals["EFFECT_FLIPSUMMON_COST"] = 93;
        luaEngine.Globals["EFFECT_MSET_COST"] = 94;
        luaEngine.Globals["EFFECT_SSET_COST"] = 95;
        luaEngine.Globals["EFFECT_ATTACK_COST"] = 96;
        luaEngine.Globals["EFFECT_UPDATE_ATTACK"] = 100;
        luaEngine.Globals["EFFECT_SET_ATTACK"] = 101;
        luaEngine.Globals["EFFECT_SET_ATTACK_FINAL"] = 102;
        luaEngine.Globals["EFFECT_SET_BASE_ATTACK"] = 103;
        luaEngine.Globals["EFFECT_UPDATE_DEFENSE"] = 104;
        luaEngine.Globals["EFFECT_SET_DEFENSE"] = 105;
        luaEngine.Globals["EFFECT_SET_DEFENSE_FINAL"] = 106;
        luaEngine.Globals["EFFECT_SET_BASE_DEFENSE"] = 107;
        luaEngine.Globals["EFFECT_REVERSE_UPDATE"] = 108;
        luaEngine.Globals["EFFECT_SWAP_AD"] = 109;
        luaEngine.Globals["EFFECT_SWAP_BASE_AD"] = 110;
        luaEngine.Globals["EFFECT_SWAP_ATTACK_FINAL"] = 111;
        luaEngine.Globals["EFFECT_SWAP_DEFENSE_FINAL"] = 112;
        luaEngine.Globals["EFFECT_ADD_CODE"] = 113;
        luaEngine.Globals["EFFECT_CHANGE_CODE"] = 114;
        luaEngine.Globals["EFFECT_ADD_TYPE"] = 115;
        luaEngine.Globals["EFFECT_REMOVE_TYPE"] = 116;
        luaEngine.Globals["EFFECT_CHANGE_TYPE"] = 117;
        luaEngine.Globals["EFFECT_REMOVE_CODE"] = 118;
        luaEngine.Globals["EFFECT_ADD_RACE"] = 120;
        luaEngine.Globals["EFFECT_REMOVE_RACE"] = 121;
        luaEngine.Globals["EFFECT_CHANGE_RACE"] = 122;
        luaEngine.Globals["EFFECT_ADD_ATTRIBUTE"] = 125;
        luaEngine.Globals["EFFECT_REMOVE_ATTRIBUTE"] = 126;
        luaEngine.Globals["EFFECT_CHANGE_ATTRIBUTE"] = 127;
        luaEngine.Globals["EFFECT_UPDATE_LEVEL"] = 130;
        luaEngine.Globals["EFFECT_CHANGE_LEVEL"] = 131;
        luaEngine.Globals["EFFECT_UPDATE_RANK"] = 132;
        luaEngine.Globals["EFFECT_CHANGE_RANK"] = 133;
        luaEngine.Globals["EFFECT_UPDATE_LSCALE"] = 134;
        luaEngine.Globals["EFFECT_CHANGE_LSCALE"] = 135;
        luaEngine.Globals["EFFECT_UPDATE_RSCALE"] = 136;
        luaEngine.Globals["EFFECT_CHANGE_RSCALE"] = 137;
        luaEngine.Globals["EFFECT_SET_POSITION"] = 140;
        luaEngine.Globals["EFFECT_SELF_DESTROY"] = 141;
        luaEngine.Globals["EFFECT_SELF_TOGRAVE"] = 142;
        luaEngine.Globals["EFFECT_DOUBLE_TRIBUTE"] = 150;
        luaEngine.Globals["EFFECT_DECREASE_TRIBUTE"] = 151;
        luaEngine.Globals["EFFECT_DECREASE_TRIBUTE_SET"] = 152;
        luaEngine.Globals["EFFECT_EXTRA_RELEASE"] = 153;
        luaEngine.Globals["EFFECT_TRIBUTE_LIMIT"] = 154;
        luaEngine.Globals["EFFECT_EXTRA_RELEASE_SUM"] = 155;
        luaEngine.Globals["EFFECT_TRIPLE_TRIBUTE"] = 156;
        luaEngine.Globals["EFFECT_ADD_EXTRA_TRIBUTE"] = 157;
        luaEngine.Globals["EFFECT_EXTRA_RELEASE_NONSUM"] = 158;
        luaEngine.Globals["EFFECT_PUBLIC"] = 160;
        luaEngine.Globals["EFFECT_COUNTER_PERMIT"] = 0x10000;
        luaEngine.Globals["EFFECT_COUNTER_LIMIT"] = 0x20000;
        luaEngine.Globals["EFFECT_RCOUNTER_REPLACE"] = 0x30000;
        luaEngine.Globals["EFFECT_LPCOST_CHANGE"] = 170;
        luaEngine.Globals["EFFECT_LPCOST_REPLACE"] = 171;
        luaEngine.Globals["EFFECT_SKIP_DP"] = 180;
        luaEngine.Globals["EFFECT_SKIP_SP"] = 181;
        luaEngine.Globals["EFFECT_SKIP_M1"] = 182;
        luaEngine.Globals["EFFECT_SKIP_BP"] = 183;
        luaEngine.Globals["EFFECT_SKIP_M2"] = 184;
        luaEngine.Globals["EFFECT_CANNOT_BP"] = 185;
        luaEngine.Globals["EFFECT_CANNOT_M2"] = 186;
        luaEngine.Globals["EFFECT_CANNOT_EP"] = 187;
        luaEngine.Globals["EFFECT_SKIP_TURN"] = 188;
        luaEngine.Globals["EFFECT_SKIP_EP"] = 189;
        luaEngine.Globals["EFFECT_DEFENSE_ATTACK"] = 190;
        luaEngine.Globals["EFFECT_MUST_ATTACK"] = 191;
        luaEngine.Globals["EFFECT_FIRST_ATTACK"] = 192;
        luaEngine.Globals["EFFECT_ATTACK_ALL"] = 193;
        luaEngine.Globals["EFFECT_EXTRA_ATTACK"] = 194;
        luaEngine.Globals["EFFECT_ONLY_BE_ATTACKED"] = 196;
        luaEngine.Globals["EFFECT_ATTACK_DISABLED"] = 197;
        luaEngine.Globals["EFFECT_CHANGE_BATTLE_STAT"] = 198;
        luaEngine.Globals["EFFECT_NO_BATTLE_DAMAGE"] = 200;
        luaEngine.Globals["EFFECT_AVOID_BATTLE_DAMAGE"] = 201;
        luaEngine.Globals["EFFECT_REFLECT_BATTLE_DAMAGE"] = 202;
        luaEngine.Globals["EFFECT_PIERCE"] = 203;
        luaEngine.Globals["EFFECT_BATTLE_DESTROY_REDIRECT"] = 204;
        luaEngine.Globals["EFFECT_BATTLE_DAMAGE_TO_EFFECT"] = 205;
        luaEngine.Globals["EFFECT_BOTH_BATTLE_DAMAGE"] = 206;
        luaEngine.Globals["EFFECT_ALSO_BATTLE_DAMAGE"] = 207;
        luaEngine.Globals["EFFECT_CHANGE_BATTLE_DAMAGE"] = 208;
        luaEngine.Globals["EFFECT_TOSS_COIN_REPLACE"] = 220;
        luaEngine.Globals["EFFECT_TOSS_DICE_REPLACE"] = 221;
        luaEngine.Globals["EFFECT_TOSS_COIN_CHOOSE"] = 222;
        luaEngine.Globals["EFFECT_TOSS_DICE_CHOOSE"] = 223;
        luaEngine.Globals["EFFECT_FUSION_MATERIAL"] = 230;
        luaEngine.Globals["EFFECT_CHAIN_MATERIAL"] = 231;
        luaEngine.Globals["EFFECT_SYNCHRO_MATERIAL"] = 232;
        luaEngine.Globals["EFFECT_XYZ_MATERIAL"] = 233;
        luaEngine.Globals["EFFECT_FUSION_SUBSTITUTE"] = 234;
        luaEngine.Globals["EFFECT_CANNOT_BE_FUSION_MATERIAL"] = 235;
        luaEngine.Globals["EFFECT_CANNOT_BE_SYNCHRO_MATERIAL"] = 236;
        luaEngine.Globals["EFFECT_SYNCHRO_MATERIAL_CUSTOM"] = 237;
        luaEngine.Globals["EFFECT_CANNOT_BE_XYZ_MATERIAL"] = 238;
        luaEngine.Globals["EFFECT_CANNOT_BE_LINK_MATERIAL"] = 239;
        luaEngine.Globals["EFFECT_SYNCHRO_LEVEL"] = 240;
        luaEngine.Globals["EFFECT_RITUAL_LEVEL"] = 241;
        luaEngine.Globals["EFFECT_XYZ_LEVEL"] = 242;
        luaEngine.Globals["EFFECT_EXTRA_RITUAL_MATERIAL"] = 243;
        luaEngine.Globals["EFFECT_NONTUNER"] = 244;
        luaEngine.Globals["EFFECT_OVERLAY_REMOVE_REPLACE"] = 245;
        luaEngine.Globals["EFFECT_CANNOT_BE_MATERIAL"] = 248;
        luaEngine.Globals["EFFECT_PRE_MONSTER"] = 250;
        luaEngine.Globals["EFFECT_MATERIAL_CHECK"] = 251;
        luaEngine.Globals["EFFECT_DISABLE_FIELD"] = 260;
        luaEngine.Globals["EFFECT_USE_EXTRA_MZONE"] = 261;
        luaEngine.Globals["EFFECT_USE_EXTRA_SZONE"] = 262;
        luaEngine.Globals["EFFECT_MAX_MZONE"] = 263;
        luaEngine.Globals["EFFECT_MAX_SZONE"] = 264;
        luaEngine.Globals["EFFECT_FORCE_MZONE"] = 265;
        luaEngine.Globals["EFFECT_BECOME_LINKED_ZONE"] = 266;
        luaEngine.Globals["EFFECT_HAND_LIMIT"] = 270;
        luaEngine.Globals["EFFECT_DRAW_COUNT"] = 271;
        luaEngine.Globals["EFFECT_SPIRIT_DONOT_RETURN"] = 280;
        luaEngine.Globals["EFFECT_SPIRIT_MAYNOT_RETURN"] = 281;
        luaEngine.Globals["EFFECT_CHANGE_ENVIRONMENT"] = 290;
        luaEngine.Globals["EFFECT_NECRO_VALLEY"] = 291;
        luaEngine.Globals["EFFECT_FORBIDDEN"] = 292;
        luaEngine.Globals["EFFECT_NECRO_VALLEY_IM"] = 293;
        luaEngine.Globals["EFFECT_REVERSE_DECK"] = 294;
        luaEngine.Globals["EFFECT_REMOVE_BRAINWASHING"] = 295;
        luaEngine.Globals["EFFECT_BP_TWICE"] = 296;
        luaEngine.Globals["EFFECT_UNIQUE_CHECK"] = 297;
        luaEngine.Globals["EFFECT_MATCH_KILL"] = 300;
        luaEngine.Globals["EFFECT_SYNCHRO_CHECK"] = 310;
        luaEngine.Globals["EFFECT_QP_ACT_IN_NTPHAND"] = 311;
        luaEngine.Globals["EFFECT_MUST_BE_MATERIAL"] = 312;
        luaEngine.Globals["EFFECT_TO_GRAVE_REDIRECT_CB"] = 313;
        luaEngine.Globals["EFFECT_CHANGE_LEVEL_FINAL"] = 314;
        luaEngine.Globals["EFFECT_CHANGE_RANK_FINAL"] = 315;
        luaEngine.Globals["EFFECT_MUST_BE_FMATERIAL"] = 316;
        luaEngine.Globals["EFFECT_MUST_BE_XMATERIAL"] = 317;
        luaEngine.Globals["EFFECT_MUST_BE_LMATERIAL"] = 318;
        luaEngine.Globals["EFFECT_SPSUMMON_PROC_G"] = 320;
        luaEngine.Globals["EFFECT_SPSUMMON_COUNT_LIMIT"] = 330;
        luaEngine.Globals["EFFECT_LEFT_SPSUMMON_COUNT"] = 331;
        luaEngine.Globals["EFFECT_CANNOT_SELECT_BATTLE_TARGET"] = 332;
        luaEngine.Globals["EFFECT_CANNOT_SELECT_EFFECT_TARGET"] = 333;
        luaEngine.Globals["EFFECT_ADD_SETCODE"] = 334;
        luaEngine.Globals["EFFECT_NO_EFFECT_DAMAGE"] = 335;
        luaEngine.Globals["EFFECT_UNSUMMONABLE_CARD"] = 336;
        luaEngine.Globals["EFFECT_DISCARD_COST_CHANGE"] = 338;
        luaEngine.Globals["EFFECT_HAND_SYNCHRO"] = 339;
        luaEngine.Globals["EFFECT_ONLY_ATTACK_MONSTER"] = 343;
        luaEngine.Globals["EFFECT_MUST_ATTACK_MONSTER"] = 344;
        luaEngine.Globals["EFFECT_PATRICIAN_OF_DARKNESS"] = 345;
        luaEngine.Globals["EFFECT_EXTRA_ATTACK_MONSTER"] = 346;
        luaEngine.Globals["EFFECT_UNION_STATUS"] = 347;
        luaEngine.Globals["EFFECT_OLDUNION_STATUS"] = 348;
        luaEngine.Globals["EFFECT_REMOVE_SETCODE"] = 349;
        luaEngine.Globals["EFFECT_CHANGE_SETCODE"] = 350;
        luaEngine.Globals["EFFECT_EXTRA_FUSION_MATERIAL"] = 352;
        luaEngine.Globals["EFFECT_TUNER_MATERIAL_LIMIT"] = 353;
        luaEngine.Globals["EFFECT_EXTRA_MATERIAL"] = 358;
        luaEngine.Globals["EFFECT_EXTRA_PENDULUM_SUMMON"] = 360;
        luaEngine.Globals["EFFECT_IRON_WALL"] = 361;
        luaEngine.Globals["EFFECT_CANNOT_LOSE_DECK"] = 400;
        luaEngine.Globals["EFFECT_CANNOT_LOSE_LP"] = 401;
        luaEngine.Globals["EFFECT_CANNOT_LOSE_EFFECT"] = 402;
        luaEngine.Globals["EFFECT_BP_FIRST_TURN"] = 403;
        luaEngine.Globals["EFFECT_UNSTOPPABLE_ATTACK"] = 404;
        luaEngine.Globals["EFFECT_ALLOW_NEGATIVE"] = 405;
        luaEngine.Globals["EFFECT_SELF_ATTACK"] = 406;
        luaEngine.Globals["EFFECT_BECOME_QUICK"] = 407;
        luaEngine.Globals["EFFECT_LEVEL_RANK"] = 408;
        luaEngine.Globals["EFFECT_RANK_LEVEL"] = 409;
        luaEngine.Globals["EFFECT_LEVEL_RANK_S"] = 410;
        luaEngine.Globals["EFFECT_RANK_LEVEL_S"] = 411;
        luaEngine.Globals["EFFECT_UPDATE_LINK"] = 420;
        luaEngine.Globals["EFFECT_CHANGE_LINK"] = 421;
        luaEngine.Globals["EFFECT_CHANGE_LINK_FINAL"] = 422;
        luaEngine.Globals["EFFECT_ADD_LINKMARKER"] = 423;
        luaEngine.Globals["EFFECT_REMOVE_LINKMARKER"] = 424;
        luaEngine.Globals["EFFECT_CHANGE_LINKMARKER"] = 425;
        luaEngine.Globals["EFFECT_FORCE_NORMAL_SUMMON_POSITION"] = 426;
        luaEngine.Globals["EFFECT_FORCE_SPSUMMON_POSITION"] = 427;
        luaEngine.Globals["EFFECT_DARKNESS_HIDE"] = 428;
        luaEngine.Globals["EFFECT_FUSION_MAT_RESTRICTION"] = 73941492 + 0x40; // TYPE_FUSION
        luaEngine.Globals["EFFECT_SYNCHRO_MAT_RESTRICTION"] = 73941492 + 0x2000; // TYPE_SYNCHRO
        luaEngine.Globals["EFFECT_XYZ_MAT_RESTRICTION"] = 73941492 + 0x800000; // TYPE_XYZ
        luaEngine.Globals["EFFECT_SYNCHRO_MAT_FROM_HAND"] = 97682931;
        luaEngine.Globals["EFFECT_XYZ_MAT_FROM_GRAVE"] = 511002793;
        luaEngine.Globals["EFFECT_SPELL_XYZ_MAT"] = 511000189;
        luaEngine.Globals["EFFECT_EQUIP_SPELL_XYZ_MAT"] = 511001175;
        luaEngine.Globals["EFFECT_ORICHALCUM_CHAIN"] = 511002116;
        luaEngine.Globals["EFFECT_DOUBLE_XYZ_MATERIAL"] = 511001225;
        luaEngine.Globals["EFFECT_SATELLARKNIGHT_CAPELLA"] = 86466163;
        luaEngine.Globals["EFFECT_STAR_SERAPH_SOVEREIGNTY"] = 91110378;

        luaEngine.Globals["SUMMON_TYPE_NORMAL"] = 0x10000000;
        luaEngine.Globals["SUMMON_TYPE_TRIBUTE"] = 0x11000000;
        luaEngine.Globals["SUMMON_TYPE_GEMINI"] = 0x12000000;
        luaEngine.Globals["SUMMON_TYPE_FLIP"] = 0x20000000;
        luaEngine.Globals["SUMMON_TYPE_SPECIAL"] = 0x40000000;
        luaEngine.Globals["SUMMON_TYPE_FUSION"] = 0x43000000;
        luaEngine.Globals["SUMMON_TYPE_RITUAL"] = 0x45000000;
        luaEngine.Globals["SUMMON_TYPE_SYNCHRO"] = 0x46000000;
        luaEngine.Globals["SUMMON_TYPE_XYZ"] = 0x49000000;
        luaEngine.Globals["SUMMON_TYPE_PENDULUM"] = 0x4a000000;
        luaEngine.Globals["SUMMON_TYPE_LINK"] = 0x4c000000;
        luaEngine.Globals["SUMMON_TYPE_MAXIMUM"] = 0x4e000000;

        luaEngine.Globals["ASSUME_CODE"] = 1;
        luaEngine.Globals["ASSUME_TYPE"] = 2;
        luaEngine.Globals["ASSUME_LEVEL"] = 3;
        luaEngine.Globals["ASSUME_RANK"] = 4;
        luaEngine.Globals["ASSUME_ATTRIBUTE"] = 5;
        luaEngine.Globals["ASSUME_RACE"] = 6;
        luaEngine.Globals["ASSUME_ATTACK"] = 7;
        luaEngine.Globals["ASSUME_DEFENSE"] = 8;
        luaEngine.Globals["ASSUME_LINK"] = 9;
        luaEngine.Globals["ASSUME_LINKMARKER"] = 10;

        luaEngine.Globals["LINK_MARKER_BOTTOM_LEFT"] = 0x1;
        luaEngine.Globals["LINK_MARKER_BOTTOM"] = 0x2;
        luaEngine.Globals["LINK_MARKER_BOTTOM_RIGHT"] = 0x4;
        luaEngine.Globals["LINK_MARKER_LEFT"] = 0x8;
        luaEngine.Globals["LINK_MARKER_RIGHT"] = 0x20;
        luaEngine.Globals["LINK_MARKER_TOP_LEFT"] = 0x40;
        luaEngine.Globals["LINK_MARKER_TOP"] = 0x80;
        luaEngine.Globals["LINK_MARKER_TOP_RIGHT"] = 0x100;

        luaEngine.Globals["COUNTER_WITHOUT_PERMIT"] = 0x1000;
        luaEngine.Globals["COUNTER_NEED_ENABLE"] = 0x2000;

        luaEngine.Globals["PLAYER_NONE"] = 2;
        luaEngine.Globals["PLAYER_ALL"] = 3;
        luaEngine.Globals["PLAYER_EITHER"] = 4;
        luaEngine.Globals["PLAYER_SELFDES"] = 5;

        luaEngine.Globals["EFFECT_FLAG_INITIAL"] = 0x1;
        luaEngine.Globals["EFFECT_FLAG_FUNC_VALUE"] = 0x2;
        luaEngine.Globals["EFFECT_FLAG_COUNT_LIMIT"] = 0x4;
        luaEngine.Globals["EFFECT_FLAG_FIELD_ONLY"] = 0x8;
        luaEngine.Globals["EFFECT_FLAG_CARD_TARGET"] = 0x10;
        luaEngine.Globals["EFFECT_FLAG_IGNORE_RANGE"] = 0x20;
        luaEngine.Globals["EFFECT_FLAG_ABSOLUTE_TARGET"] = 0x40;
        luaEngine.Globals["EFFECT_FLAG_IGNORE_IMMUNE"] = 0x80;
        luaEngine.Globals["EFFECT_FLAG_SET_AVAILABLE"] = 0x100;
        luaEngine.Globals["EFFECT_FLAG_CANNOT_NEGATE"] = 0x200;
        luaEngine.Globals["EFFECT_FLAG_CANNOT_DISABLE"] = 0x400;
        luaEngine.Globals["EFFECT_FLAG_PLAYER_TARGET"] = 0x800;
        luaEngine.Globals["EFFECT_FLAG_BOTH_SIDE"] = 0x1000;
        luaEngine.Globals["EFFECT_FLAG_COPY_INHERIT"] = 0x2000;
        luaEngine.Globals["EFFECT_FLAG_DAMAGE_STEP"] = 0x4000;
        luaEngine.Globals["EFFECT_FLAG_DAMAGE_CAL"] = 0x8000;
        luaEngine.Globals["EFFECT_FLAG_DELAY"] = 0x10000;
        luaEngine.Globals["EFFECT_FLAG_SINGLE_RANGE"] = 0x20000;
        luaEngine.Globals["EFFECT_FLAG_UNCOPYABLE"] = 0x40000;
        luaEngine.Globals["EFFECT_FLAG_OATH"] = 0x80000;
        luaEngine.Globals["EFFECT_FLAG_SPSUM_PARAM"] = 0x100000;
        luaEngine.Globals["EFFECT_FLAG_REPEAT"] = 0x200000;
        luaEngine.Globals["EFFECT_FLAG_NO_TURN_RESET"] = 0x400000;
        luaEngine.Globals["EFFECT_FLAG_EVENT_PLAYER"] = 0x800000;
        luaEngine.Globals["EFFECT_FLAG_OWNER_RELATE"] = 0x1000000;
        luaEngine.Globals["EFFECT_FLAG_CANNOT_INACTIVATE"] = 0x2000000;
        luaEngine.Globals["EFFECT_FLAG_CLIENT_HINT"] = 0x4000000;
        luaEngine.Globals["EFFECT_FLAG_CONTINUOUS_TARGET"] = 0x8000000;
        luaEngine.Globals["EFFECT_FLAG_LIMIT_ZONE"] = 0x10000000;
        luaEngine.Globals["EFFECT_FLAG_IMMEDIATELY_APPLY"] = 0x80000000L;
        luaEngine.Globals["EFFECT_FLAG2_CONTINUOUS_EQUIP"] = 0x0001;
        luaEngine.Globals["EFFECT_FLAG2_COF"] = 0x0002;
        luaEngine.Globals["EFFECT_FLAG2_CHECK_SIMULTANEOUS"] = 0x0004;
        luaEngine.Globals["EFFECT_FLAG2_FORCE_ACTIVATE_LOCATION"] = 0x40000000;
        luaEngine.Globals["EFFECT_FLAG2_MAJESTIC_MUST_COPY"] = 0x80000000L;

        // Posições de Batalha (Battle Positions)
        luaEngine.Globals["POS_FACEUP_ATTACK"] = 0x1;
        luaEngine.Globals["POS_FACEDOWN_ATTACK"] = 0x2;
        luaEngine.Globals["POS_FACEUP_DEFENSE"] = 0x4;
        luaEngine.Globals["POS_FACEDOWN_DEFENSE"] = 0x8;
        luaEngine.Globals["POS_FACEUP"] = 0x5;
        luaEngine.Globals["POS_FACEDOWN"] = 0xA;
        luaEngine.Globals["POS_ATTACK"] = 0x3;
        luaEngine.Globals["POS_DEFENSE"] = 0xC;

        // Constantes de Fases (Phases)
        luaEngine.Globals["PHASE_DRAW"] = 0x01;
        luaEngine.Globals["PHASE_STANDBY"] = 0x02;
        luaEngine.Globals["PHASE_MAIN1"] = 0x04;
        luaEngine.Globals["PHASE_BATTLE_START"] = 0x08;
        luaEngine.Globals["PHASE_BATTLE_STEP"] = 0x10;
        luaEngine.Globals["PHASE_DAMAGE"] = 0x20;
        luaEngine.Globals["PHASE_DAMAGE_CAL"] = 0x40;
        luaEngine.Globals["PHASE_BATTLE"] = 0x80;
        luaEngine.Globals["PHASE_MAIN2"] = 0x100;
        luaEngine.Globals["PHASE_END"] = 0x200;
        
        luaEngine.Globals["TIMING_DRAW_PHASE"] = 0x1;
        luaEngine.Globals["TIMING_STANDBY_PHASE"] = 0x2;
        luaEngine.Globals["TIMING_MAIN_END"] = 0x4;
        luaEngine.Globals["TIMING_BATTLE_START"] = 0x8;
        luaEngine.Globals["TIMING_DAMAGE_STEP"] = 0x10;
        luaEngine.Globals["TIMING_BATTLE_END"] = 0x400000;
        luaEngine.Globals["TIMING_END_PHASE"] = 0x20;
        luaEngine.Globals["TIMING_SUMMON"] = 0x40;
        luaEngine.Globals["TIMING_SPSUMMON"] = 0x80;
        luaEngine.Globals["TIMING_FLIPSUMMON"] = 0x100;
        luaEngine.Globals["TIMING_MSET"] = 0x200;
        luaEngine.Globals["TIMING_SSET"] = 0x400;
        luaEngine.Globals["TIMING_POS_CHANGE"] = 0x800;
        luaEngine.Globals["TIMING_ATTACK"] = 0x1000;
        luaEngine.Globals["TIMING_DAMAGE_CAL"] = 0x4000;
        luaEngine.Globals["TIMING_CHAIN_END"] = 0x8000;
        luaEngine.Globals["TIMING_DRAW"] = 0x10000;
        luaEngine.Globals["TIMING_DAMAGE"] = 0x20000;
        luaEngine.Globals["TIMING_RECOVER"] = 0x40000;
        luaEngine.Globals["TIMING_DESTROY"] = 0x80000;
        luaEngine.Globals["TIMING_REMOVE"] = 0x100000;
        luaEngine.Globals["TIMING_TOHAND"] = 0x200000;
        luaEngine.Globals["TIMING_TODECK"] = 0x400000;
        luaEngine.Globals["TIMING_TOGRAVE"] = 0x800000;
        luaEngine.Globals["TIMING_BATTLE_PHASE"] = 0x1000000;
        luaEngine.Globals["TIMING_EQUIP"] = 0x2000000;
        luaEngine.Globals["TIMING_BATTLE_STEP_END"] = 0x4000000;
        luaEngine.Globals["TIMING_BATTLED"] = 0x8000000;
        luaEngine.Globals["TIMINGS_CHECK_MONSTER"] = 0x1c0;
        luaEngine.Globals["TIMINGS_CHECK_MONSTER_E"] = 0x1e0;

        luaEngine.Globals["RESET_EVENT"] = 0x1000;
        luaEngine.Globals["RESET_CARD"] = 0x2000;
        luaEngine.Globals["RESET_CODE"] = 0x4000;
        luaEngine.Globals["RESET_COPY"] = 0x8000;
        luaEngine.Globals["RESET_DISABLE"] = 0x10000;
        luaEngine.Globals["RESET_TURN_SET"] = 0x20000;
        luaEngine.Globals["RESET_TOGRAVE"] = 0x40000;
        luaEngine.Globals["RESET_REMOVE"] = 0x80000;
        luaEngine.Globals["RESET_TEMP_REMOVE"] = 0x100000;
        luaEngine.Globals["RESET_TOHAND"] = 0x200000;
        luaEngine.Globals["RESET_TODECK"] = 0x400000;
        luaEngine.Globals["RESET_LEAVE"] = 0x800000;
        luaEngine.Globals["RESET_TOFIELD"] = 0x1000000;
        luaEngine.Globals["RESET_CONTROL"] = 0x2000000;
        luaEngine.Globals["RESET_OVERLAY"] = 0x4000000;
        luaEngine.Globals["RESET_MSCHANGE"] = 0x8000000;
        luaEngine.Globals["RESET_SELF_TURN"] = 0x10000000;
        luaEngine.Globals["RESET_OPPO_TURN"] = 0x20000000;
        luaEngine.Globals["RESET_PHASE"] = 0x40000000;
        luaEngine.Globals["RESET_CHAIN"] = 0x80000000L;

        luaEngine.Globals["RESETS_STANDARD"] = 0x1000000 | 0x800000 | 0x400000 | 0x200000 | 0x100000 | 0x80000 | 0x40000 | 0x20000;
        luaEngine.Globals["RESETS_STANDARD_DISABLE"] = (0x1000000 | 0x800000 | 0x400000 | 0x200000 | 0x100000 | 0x80000 | 0x40000 | 0x20000) | 0x10000;
        luaEngine.Globals["RESETS_STANDARD_PHASE_END"] = 0x1000 | (0x1000000 | 0x800000 | 0x400000 | 0x200000 | 0x100000 | 0x80000 | 0x40000 | 0x20000) | 0x40000000 | 0x200;
        luaEngine.Globals["RESETS_STANDARD_DISABLE_PHASE_END"] = (0x1000 | (0x1000000 | 0x800000 | 0x400000 | 0x200000 | 0x100000 | 0x80000 | 0x40000 | 0x20000) | 0x40000000 | 0x200) | 0x10000;

        luaEngine.Globals["HINTMSG_RELEASE"] = 500;
        luaEngine.Globals["HINTMSG_DISCARD"] = 501;
        luaEngine.Globals["HINTMSG_DESTROY"] = 502;
        luaEngine.Globals["HINTMSG_REMOVE"] = 503;
        luaEngine.Globals["HINTMSG_TOGRAVE"] = 504;
        luaEngine.Globals["HINTMSG_RTOHAND"] = 505;
        luaEngine.Globals["HINTMSG_ATOHAND"] = 506;
        luaEngine.Globals["HINTMSG_TODECK"] = 507;
        luaEngine.Globals["HINTMSG_SUMMON"] = 508;
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;
        luaEngine.Globals["HINTMSG_SET"] = 510;
        luaEngine.Globals["HINTMSG_FMATERIAL"] = 511;
        luaEngine.Globals["HINTMSG_SMATERIAL"] = 512;
        luaEngine.Globals["HINTMSG_XMATERIAL"] = 513;
        luaEngine.Globals["HINTMSG_FACEUP"] = 514;
        luaEngine.Globals["HINTMSG_FACEDOWN"] = 515;
        luaEngine.Globals["HINTMSG_ATTACK"] = 516;
        luaEngine.Globals["HINTMSG_DEFENSE"] = 517;
        luaEngine.Globals["HINTMSG_EQUIP"] = 518;
        luaEngine.Globals["HINTMSG_REMOVEXYZ"] = 519;
        luaEngine.Globals["HINTMSG_CONTROL"] = 520;
        luaEngine.Globals["HINTMSG_DESREPLACE"] = 521;
        luaEngine.Globals["HINTMSG_FACEUPATTACK"] = 522;
        luaEngine.Globals["HINTMSG_FACEUPDEFENSE"] = 523;
        luaEngine.Globals["HINTMSG_FACEDOWNATTACK"] = 524;
        luaEngine.Globals["HINTMSG_FACEDOWNDEFENSE"] = 525;
        luaEngine.Globals["HINTMSG_CONFIRM"] = 526;
        luaEngine.Globals["HINTMSG_TOFIELD"] = 527;
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 528;
        luaEngine.Globals["HINTMSG_SELF"] = 529;
        luaEngine.Globals["HINTMSG_OPPO"] = 530;
        luaEngine.Globals["HINTMSG_TRIBUTE"] = 531;
        luaEngine.Globals["HINTMSG_DEATTACHFROM"] = 532;
        luaEngine.Globals["HINTMSG_LMATERIAL"] = 533;
        luaEngine.Globals["HINTMSG_ATTACKTARGET"] = 549;
        luaEngine.Globals["HINTMSG_EFFECT"] = 550;
        luaEngine.Globals["HINTMSG_TARGET"] = 551;
        luaEngine.Globals["HINTMSG_COIN"] = 552;
        luaEngine.Globals["HINTMSG_DICE"] = 553;
        luaEngine.Globals["HINTMSG_CARDTYPE"] = 554;
        luaEngine.Globals["HINTMSG_OPTION"] = 555;
        luaEngine.Globals["HINTMSG_RESOLVEEFFECT"] = 556;
        luaEngine.Globals["HINTMSG_SELECT"] = 560;
        luaEngine.Globals["HINTMSG_POSITION"] = 561;
        luaEngine.Globals["HINTMSG_ATTRIBUTE"] = 562;
        luaEngine.Globals["HINTMSG_CODE"] = 564;
        luaEngine.Globals["HINTMSG_NUMBER"] = 565;
        luaEngine.Globals["HINTMSG_EFFACTIVATE"] = 566;
        luaEngine.Globals["HINTMSG_LVRANK"] = 567;
        luaEngine.Globals["HINTMSG_RESOLVECARD"] = 568;
        luaEngine.Globals["HINTMSG_ZONE"] = 569;
        luaEngine.Globals["HINTMSG_DISABLEZONE"] = 570;
        luaEngine.Globals["HINTMSG_TOZONE"] = 571;
        luaEngine.Globals["HINTMSG_COUNTER"] = 572;
        luaEngine.Globals["HINTMSG_NEGATE"] = 575;
        luaEngine.Globals["HINTMSG_ATKDEF"] = 576;
        luaEngine.Globals["HINTMSG_APPLYTO"] = 577;
        luaEngine.Globals["HINTMSG_ATTACH"] = 578;
        luaEngine.Globals["HINTMSG_RTOGRAVE"] = 579;

        luaEngine.Globals["SELECT_HEADS"] = 60;
        luaEngine.Globals["SELECT_TAILS"] = 61;

        luaEngine.Globals["DECLTYPE_MONSTER"] = 70;
        luaEngine.Globals["DECLTYPE_SPELL"] = 71;
        luaEngine.Globals["DECLTYPE_TRAP"] = 72;

        luaEngine.Globals["GLOBALFLAG_BRAINWASHING_CHECK"] = 0x2;
        luaEngine.Globals["GLOBALFLAG_DELAYED_QUICKEFFECT"] = 0x8;
        luaEngine.Globals["GLOBALFLAG_DETACH_EVENT"] = 0x10;
        luaEngine.Globals["GLOBALFLAG_SPSUMMON_COUNT"] = 0x40;
        luaEngine.Globals["GLOBALFLAG_SELF_TOGRAVE"] = 0x100;
        luaEngine.Globals["GLOBALFLAG_SPSUMMON_ONCE"] = 0x200;

        luaEngine.Globals["EFFECT_COUNT_CODE_OATH"] = 0x1;
        luaEngine.Globals["EFFECT_COUNT_CODE_DUEL"] = 0x2;
        luaEngine.Globals["EFFECT_COUNT_CODE_SINGLE"] = 0x4;
        luaEngine.Globals["EFFECT_COUNT_CODE_CHAIN"] = 0x8;

        luaEngine.Globals["ACTIVITY_SUMMON"] = 1;
        luaEngine.Globals["ACTIVITY_NORMALSUMMON"] = 2;
        luaEngine.Globals["ACTIVITY_SPSUMMON"] = 3;
        luaEngine.Globals["ACTIVITY_FLIPSUMMON"] = 4;
        luaEngine.Globals["ACTIVITY_ATTACK"] = 5;
        luaEngine.Globals["ACTIVITY_BATTLE_PHASE"] = 6;
        luaEngine.Globals["ACTIVITY_CHAIN"] = 7;

        luaEngine.Globals["ANNOUNCE_CARD"] = 0x7;
        luaEngine.Globals["ANNOUNCE_CARD_FILTER"] = 0x8;

        luaEngine.Globals["DOUBLE_DAMAGE"] = 0x80000000L;
        luaEngine.Globals["HALF_DAMAGE"] = 0x80000001L;

        luaEngine.Globals["EFFECT_MARKER_DETACH_XMAT"] = 511002571;
        luaEngine.Globals["EFFECT_MARKER_CARDIAN"] = 511001692;
        luaEngine.Globals["EFFECT_MARKER_THUNDRA"] = 12081875;
        luaEngine.Globals["EFFECT_MARKER_ALLURE_LVUP"] = 511310036;
        luaEngine.Globals["EFFECT_MARKER_TELLAR"] = 58858807;
        luaEngine.Globals["EFFECT_MARKER_DRAGON_RULER"] = 4965193;
        luaEngine.Globals["EFFECT_CAN_BE_TUNER"] = 30765615;
        luaEngine.Globals["EFFECT_CLEAR_WALL"] = 6089145;
        luaEngine.Globals["EFFECT_CLEAR_WORLD_IMMUNE"] = 97811903;
        luaEngine.Globals["EFFECT_CYBERDARK_WORLD"] = 64753988;
        luaEngine.Globals["EFFECT_FORMUD_SKIPPER"] = 50366775;
        luaEngine.Globals["EFFECT_FUR_HIRE_REPLACE"] = 101303062;
        luaEngine.Globals["EFFECT_GOLDEN_ALLURE_QUEEN"] = 95937545;
        luaEngine.Globals["EFFECT_ICEBARRIER_REPLACE"] = 18319762;
        luaEngine.Globals["EFFECT_MULTIPLE_TUNERS"] = 21142671;
        luaEngine.Globals["EFFECT_SFORCE_REPLACE"] = 55049722;
        luaEngine.Globals["EFFECT_SUPREME_CASTLE"] = 72043279;
        luaEngine.Globals["EFFECT_SYNSUB_NORDIC"] = 61777313;
        luaEngine.Globals["EFFECT_WITCHCRAFTER_REPLACE"] = 83289866;

        luaEngine.Globals["FUSPROC_NOTFUSION"] = 0x100;
        luaEngine.Globals["FUSPROC_CONTACTFUS"] = 0x200;
        luaEngine.Globals["FUSPROC_LISTEDMATS"] = 0x400;
        luaEngine.Globals["FUSPROC_NOLIMIT"] = 0x800;
        luaEngine.Globals["FUSPROC_CANCELABLE"] = 0x1000;
        luaEngine.Globals["RITPROC_EQUAL"] = 0x1;
        luaEngine.Globals["RITPROC_GREATER"] = 0x2;
        luaEngine.Globals["MATERIAL_FUSION"] = 0x1L << 32;
        luaEngine.Globals["MATERIAL_SYNCHRO"] = 0x2L << 32;
        luaEngine.Globals["MATERIAL_XYZ"] = 0x4L << 32;
        luaEngine.Globals["MATERIAL_LINK"] = 0x8L << 32;

        luaEngine.Globals["WIN_REASON_EXODIA"] = 0x10;
        luaEngine.Globals["WIN_REASON_FINAL_COUNTDOWN"] = 0x11;
        luaEngine.Globals["WIN_REASON_VENNOMINAGA"] = 0x12;
        luaEngine.Globals["WIN_REASON_CREATORGOD"] = 0x13;
        luaEngine.Globals["WIN_REASON_EXODIUS"] = 0x14;
        luaEngine.Globals["WIN_REASON_DESTINY_BOARD"] = 0x15;
        luaEngine.Globals["WIN_REASON_LAST_TURN"] = 0x16;
        luaEngine.Globals["WIN_REASON_PUPPET_LEO"] = 0x17;
        luaEngine.Globals["WIN_REASON_DISASTER_LEO"] = 0x18;
        luaEngine.Globals["WIN_REASON_JACKPOT7"] = 0x19;
        luaEngine.Globals["WIN_REASON_RELAY_SOUL"] = 0x1a;
        luaEngine.Globals["WIN_REASON_GHOSTRICK_MISCHIEF"] = 0x1b;
        luaEngine.Globals["WIN_REASON_PHANTASM_SPIRAL"] = 0x1c;
        luaEngine.Globals["WIN_REASON_FA_WINNERS"] = 0x1d;
        luaEngine.Globals["WIN_REASON_FLYING_ELEPHANT"] = 0x1e;
        luaEngine.Globals["WIN_REASON_EXODIA_DEFENDER"] = 0x1f;
        luaEngine.Globals["WIN_REASON_TRUE_EXODIA"] = 0x21;
        luaEngine.Globals["WIN_REASON_FINAL_DRAW"] = 0x22;
        luaEngine.Globals["WIN_REASON_CREATOR_MIRACLE"] = 0x30;
        luaEngine.Globals["WIN_REASON_NUMBER_iC1000"] = 0x52;
        luaEngine.Globals["WIN_REASON_ZERO_GATE"] = 0x53;
        luaEngine.Globals["WIN_REASON_DEUCE"] = 0x54;
        luaEngine.Globals["WIN_REASON_DECK_MASTER"] = 0x56;
        luaEngine.Globals["WIN_REASON_DRAW_OF_FATE"] = 0x57;
        luaEngine.Globals["WIN_REASON_MUSICAL_SUMO"] = 0x58;
        luaEngine.Globals["WIN_REASON_SUMMER_SCHOOLWORK"] = 0x59;

        luaEngine.Globals["DUEL_MODE_SPEED"] = 0x400000 | 0x200000 | 0x20000;
        luaEngine.Globals["DUEL_MODE_RUSH"] = 0x400000 | 0x200000 | 0x100000 | 0x200 | 0x4000000 | 0x800000 | 0x1000000 | 0x2000000 | 0x20000 | 0x800000000L;
        luaEngine.Globals["DUEL_MODE_MR1"] = 0x100 | 0x200 | 0x400 | 0x40000 | 0x10000 | 0x80000;
        luaEngine.Globals["DUEL_MODE_GOAT"] = (0x100 | 0x200 | 0x400 | 0x40000 | 0x10000 | 0x80000) | 0x400000000L | 0x4 | 0x8 | 0x20 | 0x8000000 | 0x10000000 | 0x20000000 | 0x40000000 | 0x80000000 | 0x100000000L | 0x200000000L;
        luaEngine.Globals["DUEL_MODE_MR2"] = 0x200 | 0x400 | 0x40000 | 0x10000 | 0x80000;
        luaEngine.Globals["DUEL_MODE_MR3"] = 0x800 | 0x1000 | 0x40000 | 0x10000 | 0x80000;
        luaEngine.Globals["DUEL_MODE_MR4"] = 0x800 | 0x2000 | 0x40000 | 0x10000 | 0x80000;
        luaEngine.Globals["DUEL_MODE_MR5"] = 0x800 | 0x2000 | 0x4000 | 0x8000 | 0x20000;
        luaEngine.Globals["DUEL_OBSOLETE_RULING"] = luaEngine.Globals["DUEL_MODE_MR1"];

        luaEngine.Globals["OPCODE_ADD"] = 0x4000000000000000L;
        luaEngine.Globals["OPCODE_SUB"] = 0x4000000100000000L;
        luaEngine.Globals["OPCODE_MUL"] = 0x4000000200000000L;
        luaEngine.Globals["OPCODE_DIV"] = 0x4000000300000000L;
        luaEngine.Globals["OPCODE_AND"] = 0x4000000400000000L;
        luaEngine.Globals["OPCODE_OR"] = 0x4000000500000000L;
        luaEngine.Globals["OPCODE_NEG"] = 0x4000000600000000L;
        luaEngine.Globals["OPCODE_NOT"] = 0x4000000700000000L;
        luaEngine.Globals["OPCODE_BAND"] = 0x4000000800000000L;
        luaEngine.Globals["OPCODE_BOR"] = 0x4000000900000000L;
        luaEngine.Globals["OPCODE_BNOT"] = 0x4000001000000000L;
        luaEngine.Globals["OPCODE_BXOR"] = 0x4000001100000000L;
        luaEngine.Globals["OPCODE_LSHIFT"] = 0x4000001200000000L;
        luaEngine.Globals["OPCODE_RSHIFT"] = 0x4000001300000000L;
        luaEngine.Globals["OPCODE_ALLOW_ALIASES"] = 0x4000001400000000L;
        luaEngine.Globals["OPCODE_ALLOW_TOKENS"] = 0x4000001500000000L;
        luaEngine.Globals["OPCODE_ISCODE"] = 0x4000010000000000L;
        luaEngine.Globals["OPCODE_ISSETCARD"] = 0x4000010100000000L;
        luaEngine.Globals["OPCODE_ISTYPE"] = 0x4000010200000000L;
        luaEngine.Globals["OPCODE_ISRACE"] = 0x4000010300000000L;
        luaEngine.Globals["OPCODE_ISATTRIBUTE"] = 0x4000010400000000L;
        luaEngine.Globals["OPCODE_GETCODE"] = 0x4000010500000000L;
        luaEngine.Globals["OPCODE_GETSETCARD"] = 0x4000010600000000L;
        luaEngine.Globals["OPCODE_GETTYPE"] = 0x4000010700000000L;
        luaEngine.Globals["OPCODE_GETRACE"] = 0x4000010800000000L;
        luaEngine.Globals["OPCODE_GETATTRIBUTE"] = 0x4000010900000000L;

        // Metatable Global Segura
        luaEngine.DoString(@"
            local dummyFunc = function() return Effect.CreateEffect(nil) end
            local procMt = { __index = function() return dummyFunc end, __call = function() return dummyFunc end }
            local dummyTable = {}
            setmetatable(dummyTable, procMt)

            setmetatable(_G, {
                __index = function(t, k)
                    if type(k) == 'string' and string.match(k, '^[A-Z0-9_]+$') then 
                        Log('<color=red>[LUA MISSING CONSTANT]</color> A constante ' .. k .. ' não está definida na Engine C#! Retornando 0 (NIL) como Fallback.')
                        return 0 
                    end
                    if type(k) == 'string' and string.match(k, '^[A-Z][a-zA-Z0-9_]*$') then 
                        Log('<color=red>[LUA MISSING STUB]</color> A Classe ' .. k .. ' não está definida na Engine C#! Retornando Dummy.')
                        return dummyTable 
                    end
                    return nil
                end
            })

            local dummyGroup = Group.CreateGroup()
            local gm = getmetatable(dummyGroup)
            if gm then
                gm.__call = function(grp, state, var)
                    if var == nil then return grp:GetFirst() end
                    return grp:GetNext()
                end
                gm.__iterator = function(grp) return grp:Iter() end
            end
            
            Card = {}
            
            -- Implementação Limpa OCGCore: Resolve o problema de Face-Downs sem hackear o carregador!
            -- No YGO, qualquer carta na zona S/T (8) ou Campo (256) é uma Magia/Armadilha.
            Card.IsSpellTrap = function(c)
                if c == nil then return false end
                return c:IsLocation(8) or c:IsLocation(256) or c:IsType(TYPE_SPELL) or c:IsType(TYPE_TRAP)
            end
            
            Card.IsMonster = function(c)
                if c == nil then return false end
                return c:IsLocation(4) or c:IsType(TYPE_MONSTER)
            end

            setmetatable(Card, {
                __index = function(t, k)
                    return function(c, ...)
                        if c and type(c) == 'userdata' then
                            local func = c[k]
                            if type(func) == 'function' then
                                return func(...)
                            end
                            
                            -- Fallbacks Vitais: Ensina a Engine C# a responder aos filtros clássicos do OCGCore
                            if k == 'IsDestructable' or k == 'IsAbleToHand' or k == 'IsAbleToGrave' or k == 'IsAbleToRemove' or k == 'IsAbleToHandAsCost' then return true end
                            if k == 'IsRelateToEffect' then return true end
                            if k == 'IsCanBeEffectTarget' then return true end
                            if k == 'IsFacedown' then return not c:IsFaceup() end
                            if k == 'IsOnField' then return c:IsLocation(LOCATION_ONFIELD) end
                            
                            -- Stat & State Checks
                            if k == 'IsDefenseBelow' then return c:GetDefense() <= select(1, ...) end
                            if k == 'IsDefenseAbove' then return c:GetDefense() >= select(1, ...) end
                            if k == 'IsAttackBelow' then return c:GetAttack() <= select(1, ...) end
                            if k == 'IsAttackAbove' then return c:GetAttack() >= select(1, ...) end
                            if k == 'IsLevelBelow' then return c:GetLevel() <= select(1, ...) end
                            if k == 'IsLevelAbove' then return c:GetLevel() >= select(1, ...) end
                            if k == 'IsSummonPlayer' then return c:GetControler() == select(1, ...) end
                        end
                        return false
                    end
                end
            })

            Cost = {}
            setmetatable(Cost, {
                __index = function(t, k)
                    return function(...) return function(e, tp, eg, ep, ev, re, r, rp, chk) if chk == 0 then return true end end end
                end
            })
            function Cost.PayLP(amount)
                return function(e, tp, eg, ep, ev, re, r, rp, chk)
                    if chk == 0 then return Duel.CheckLPCost(tp, amount) end
                    Duel.PayLPCost(tp, amount)
                end
            end
            
            Core = {}
            function Core.Attack(attacker, target)
                Duel.RaiseEvent(attacker, 1130, nil, 0, attacker:GetControler(), attacker:GetControler(), 0)
                local unstoppable = attacker:IsHasEffect(404)
                if not unstoppable then
                    coroutine.yield('WaitChain')
                    coroutine.yield('FastEffectWindow_1130')
                end
                if not attacker:IsOnField() then return false end
                if target ~= nil and not target:IsOnField() then return false end
                coroutine.yield('PlayAttackAnimation')
                if target ~= nil and target:IsFacedown() then coroutine.yield('RevealTarget') end
                
                if not unstoppable then
                    coroutine.yield('FastEffectWindow_DamageStep')
                    coroutine.yield('FastEffectWindow_DamageCal')
                end
                
                Duel.CalculateDamage(attacker, target)
                
                -- Dispara o gatilho de Pós-Dano para efeitos contínuos e memórias de campo (EVENT_BATTLED = 1138)
                Duel.RaiseEvent(attacker, 1138, nil, 0, attacker:GetControler(), attacker:GetControler(), 0)
                coroutine.yield('WaitChain')
                coroutine.yield('FastEffectWindow_1138')
                
                coroutine.yield('FastEffectWindow_BattleStepEnd')
                
                return true
            end

            function Core.NormalSummon(player, card, isSet, dynLevel, ignoreTributes)
                local level = dynLevel or card:GetLevel()
                local tributes = 0
                if level >= 5 and level <= 6 then tributes = 1 end
                if level >= 7 then tributes = 2 end
                if ignoreTributes then tributes = 0 end
                if tributes > 0 then
                    local sg = Duel.SelectReleaseGroup(player, aux.TRUE, tributes, tributes, nil)
                    if not sg or sg:GetCount() < tributes then return false end
                    Duel.Release(sg, REASON_SUMMON)
                end
                if isSet then Duel.MSet(player, card, true, nil) else Duel.Summon(player, card, true, nil) end
                return true
            end
        ");
    }
}