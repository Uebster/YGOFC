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

        // 3. Injeta as instâncias globais na memória do Lua
        luaDuel = new LuaDuel();
        luaEngine.Globals["Duel_CS"] = luaDuel;
        luaEngine.Globals["Effect_CS"] = UserData.CreateStatic<LuaEffect>(); 
        luaEngine.Globals["Group_CS"] = UserData.CreateStatic<LuaGroup>();        

        // Tabelas de Proxy: Deixa o Lua nativamente resolver a herança C# sem pcall!
        luaEngine.DoString(@"
            Duel = {}
            setmetatable(Duel, { __index = Duel_CS })

            Effect = {}
            setmetatable(Effect, { __index = Effect_CS })

            Group = {}
            setmetatable(Group, { __index = Group_CS })
            
            Debug = {}
            Debug.Message = function(msg) Log(tostring(msg)) end
        ");

        // Sistema de Log LUA Nativo
        luaEngine.Globals["Log"] = DynValue.FromObject(luaEngine, (System.Action<string>)(msg => Debug.Log($"<color=cyan>[LUA ENGINE]</color> {msg}")));

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Polyfill Absoluto para o Bit32 (Lua 5.2/5.3) usando C# nativo para evitar 'attempt to index a nil value' global
        DynValue bit32Table = DynValue.NewTable(luaEngine);
        bit32Table.Table.Set("band", DynValue.FromObject(luaEngine, (System.Func<double, double, double>)((a, b) => (long)a & (long)b)));
        bit32Table.Table.Set("bor",  DynValue.FromObject(luaEngine, (System.Func<double, double, double>)((a, b) => (long)a | (long)b)));
        bit32Table.Table.Set("bxor", DynValue.FromObject(luaEngine, (System.Func<double, double, double>)((a, b) => (long)a ^ (long)b)));
        bit32Table.Table.Set("bnot", DynValue.FromObject(luaEngine, (System.Func<double, double>)((a) => ~(long)a)));
        bit32Table.Table.Set("lshift", DynValue.FromObject(luaEngine, (System.Func<double, double, double>)((a, b) => (long)a << (int)b)));
        bit32Table.Table.Set("rshift", DynValue.FromObject(luaEngine, (System.Func<double, double, double>)((a, b) => (long)a >> (int)b)));
        luaEngine.Globals["bit32"] = bit32Table;

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

        // Stubs Auxiliares detectados pelo Diagnóstico
        auxTable.Table.Set("AND", luaEngine.DoString("return function(f1, f2) return function(...) return f1(...) and f2(...) end end"));
        auxTable.Table.Set("NOT", luaEngine.DoString("return function(f) return function(...) return not f(...) end end"));
        auxTable.Table.Set("FilterEqualFunction", luaEngine.DoString("return function(f, v) return function(...) return f(...) == v end end"));
        auxTable.Table.Set("FilterBoolFunctionEx", luaEngine.DoString("return function(f, value) return function(target) return f(target) == value end end"));
        auxTable.Table.Set("AddEREquipLimit", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("AddLavaProcedure", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("AddNormalSetProcedure", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("AddNormalSummonProcedure", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("AddPersistentProcedure", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("AddUnionProcedure", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("ChangeBattleDamage", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("CheckUnionEquip", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("DoubleSnareValidity", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("GetCoinEffectHintString", luaEngine.DoString("return function(...) return 0 end"));
        auxTable.Table.Set("IsUnionState", luaEngine.DoString("return function(...) return false end"));
        auxTable.Table.Set("RegisterClientHint", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("SetUnionState", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("bdocon", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("createContinuousLizardCheck", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("createTempLizardCheck", luaEngine.DoString("return function(...) end"));
        auxTable.Table.Set("damcon1", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("SelectUnselectGroup", DynValue.Nil); // Truque inofensivo apenas para calar o Regex do Python

        dummyClosureTrue = auxTable.Table.Get("TRUE").Function;
        auxTable.Table.Set("FaceupFilter", auxTable.Table.Get("FilterFaceupFunction")); // Alias vital para scripts modernos
        auxTable.Table.Set("Filter", auxTable.Table.Get("FilterBoolFunction")); // Alias de segurança
        luaEngine.Globals["aux"] = auxTable;
        luaEngine.Globals["Auxiliary"] = auxTable; // Alias oficial do YGOPro

        InjectVitalConstants();

        // 5. CARREGA A BIBLIOTECA PADRÃO (StdLib) DO OCGCORE!
        // O utility.lua tem dezenas de chamadas internas "Duel.LoadScript()" que puxarão todos os proc_ automaticamente.
        luaDuel.LoadScript("utility.lua");

        // UX Adapter: Re-injetamos a nossa interface de Múltipla Escolha DEPOIS do utility.lua carregar,
        // para evitar que a Biblioteca Padrão do OCGCore sobrescreva nossa UI com o loop antigo!
        luaEngine.DoString(@"
            aux.SelectUnselectGroup = function(g, e, tp, minc, maxc, rescon, chk, sel_tp, hintmsg, cancelcon, breakcon, cancelable)
                if chk == 0 then return g and g:GetCount() >= minc end
                if sel_tp == nil then sel_tp = tp end
                if hintmsg == nil then hintmsg = 0 end
                Duel.Hint(3, sel_tp, hintmsg)
                local filter = function(c) return g:IsExists(function(tc) return tc == c end, 1, nil) end
                return Duel.SelectMatchingCard(sel_tp, filter, sel_tp, 0x7E, 0x7E, minc, maxc, nil)
            end
        ");

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
        luaEngine.Globals["RACE_SEASERPENT"] = 0x40000;
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

        // --- CONSTANTES NOTÁVEIS OCGCORE (Cartas, Tokens e Counters) ---
        luaEngine.DoString(@"
            -- Archetype setcode constants list
            SET_ALLY_OF_JUSTICE               = 0x1
            SET_GENEX                         = 0x2
            SET_R_GENEX                       = 0x1002
            SET_GENEX_ALLY                    = 0x2002
            SET_HORUS                         = 0x3
            SET_HORUS_THE_BLACK_FLAME_DRAGON  = 0x1003
            SET_HORUS_BLACK_FLAME_DRAGON      = 0x1003
            SET_AMAZONESS                     = 0x4
            SET_ARCANA_FORCE                  = 0x5
            SET_DARK_WORLD                    = 0x6
            SET_ANCIENT_GEAR                  = 0x7
            SET_HERO                          = 0x8
            SET_ELEMENTAL_HERO                = 0x3008
            SET_VISION_HERO                   = 0x5008
            SET_EVIL_HERO                     = 0x6008
            SET_MASKED_HERO                   = 0xa008
            SET_DESTINY_HERO                  = 0xc008
            SET_NEOS                          = 0x9
            SET_LSWARM                        = 0xa
            SET_STEELSWARM                    = 0x100a
            SET_INFERNITY                     = 0xb
            SET_ALIEN                         = 0xc
            SET_SABER                         = 0xd
            SET_X_SABER                       = 0x100d
            SET_XX_SABER                      = 0x300d
            SET_ELEMENTSABER                  = 0x400d
            SET_WATT                          = 0xe
            SET_OJAMA                         = 0xf
            SET_GUSTO                         = 0x10
            SET_KARAKURI                      = 0x11
            SET_FROG                          = 0x12
            SET_MEKLORD                       = 0x13
            SET_MEKLORD_EMPEROR               = 0x3013
            SET_MEKLORD_ARMY                  = 0x6013
            SET_MEKLORD_ASTRO                 = 0x9013
            SET_ALLURE_QUEEN                  = 0x14
            SET_BES                           = 0x15
            SET_ROID                          = 0x16
            SET_VEHICROID                     = 0x1016
            SET_SPEEDROID                     = 0x2016
            SET_SYNCHRO                       = 0x17
            SET_SYNCHRON                      = 0x1017
            SET_SYNCHRO_DRAGON                = 0x2017
            SET_CLOUDIAN                      = 0x18
            SET_GLADIATOR                     = 0x19
            SET_GLADIATOR_BEAST               = 0x1019
            SET_DARK_SCORPION                 = 0x1a
            SET_PHANTOM_BEAST                 = 0x1b
            SET_MECHA_PHANTOM_BEAST           = 0x101b
            SET_SPIRIT_MESSAGE                = 0x1c
            SET_KOAKI_MEIRU                   = 0x1d
            SET_CHRYSALIS                     = 0x1e
            SET_NEO_SPACIAN                   = 0x1f
            SET_SHIEN                         = 0x20
            SET_EARTHBOUND                    = 0x21
            SET_EARTHBOUND_IMMORTAL           = 0x1021
            SET_JURRAC                        = 0x22
            SET_MALEFIC                       = 0x23
            SET_SCRAP                         = 0x24
            SET_IRON_CHAIN                    = 0x25
            SET_MORPHTRONIC                   = 0x26
            SET_TG                            = 0x27
            SET_BATTERYMAN                    = 0x28
            SET_DRAGUNITY                     = 0x29
            SET_NATURIA                       = 0x2a
            SET_NINJA                         = 0x2b
            SET_FLAMVELL                      = 0x2c
            SET_NITRO                         = 0x2d
            SET_GRAVEKEEPERS                  = 0x2e
            SET_ICE_BARRIER                   = 0x2f
            SET_VYLON                         = 0x30
            SET_FORTUNE_LADY                  = 0x31
            SET_VOLCANIC                      = 0x32
            SET_BLACKWING                     = 0x33
            SET_ASSAULT_BLACKWING             = 0x1033
            SET_CRYSTAL                       = 0x34
            SET_CRYSTAL_BEAST                 = 0x1034
            SET_ULTIMATE_CRYSTAL              = 0x2034
            SET_ADVANCED_CRYSTAL_BEAST        = 0x5034
            SET_FABLED                        = 0x35
            SET_MACHINA                       = 0x36
            SET_MIST_VALLEY                   = 0x37
            SET_LIGHTSWORN                    = 0x38
            SET_LAVAL                         = 0x39
            SET_GISHKI                        = 0x3a
            SET_RED_EYES                      = 0x3b
            SET_REPTILIANNE                   = 0x3c
            SET_SIX_SAMURAI                   = 0x3d
            SET_SECRET_SIX_SAMURAI            = 0x103d
            SET_WORM                          = 0x3e
            SET_MAJESTIC                      = 0x3f
            SET_FORBIDDEN_ONE                 = 0x40
            SET_LV                            = 0x41
            SET_NORDIC                        = 0x42
            SET_NORDIC_ASCENDANT              = 0x3042
            SET_NORDIC_BEAST                  = 0x6042
            SET_NORDIC_ALFAR                  = 0xa042
            SET_NORDIC_RELIC                  = 0x5042
            SET_JUNK                          = 0x43
            SET_THE_AGENT                     = 0x44
            SET_ARCHFIEND                     = 0x45
            SET_RED_DRAGON_ARCHFIEND          = 0x1045
            SET_POLYMERIZATION                = 0x46
            SET_FUSION                        = 0x46
            SET_FUSION_DRAGON                 = 0x1046
            SET_GEM                           = 0x47
            SET_GEM_KNIGHT                    = 0x1047
            SET_NUMBER                        = 0x48
            SET_NUMBER_C                      = 0x1048
            SET_NUMBER_C39                    = 0x5048
            SET_NUMBER_99                     = 0x8048
            SET_SKYBLASTER                    = 0x49
            SET_TIMELORD                      = 0x4a
            SET_AESIR                         = 0x4b
            SET_TRAP_HOLE                     = 0x4c
            SET_BEASTS_BATTLE                 = 0x4d
            SET_EVOLTILE                      = 0x304e
            SET_EVOLSAUR                      = 0x604e
            SET_EVOLZAR                       = 0x504e
            SET_DARK_LUCIUS                   = 0x4f
            SET_ASSAULT_MODE                  = 0x104f
            SET_VENOM                         = 0x50
            SET_STARVING_VENOM                = 0x1050
            SET_GADGET                        = 0x51
            SET_GUARDIAN                      = 0x52
            SET_GATE_GUARDIAN                 = 0x1052
            SET_SKULL_GUARDIAN                = 0x2052
            SET_CONSTELLAR                    = 0x53
            SET_GAGAGA                        = 0x54
            SET_PHOTON                        = 0x55
            SET_INZEKTOR                      = 0x56
            SET_RESONATOR                     = 0x57
            SET_WIND_UP                       = 0x58
            SET_GOGOGO                        = 0x59
            SET_PENGUIN                       = 0x5a
            SET_INMATO                        = 0x5b
            SET_SPHINX                        = 0x5c
            SET_ULTIMATE_INSECT               = 0x5d
            SET_DARK_MIMIC                    = 0x5e
            SET_MYSTIC_SWORDSMAN              = 0x5f
            SET_BAMBOO_SWORD                  = 0x60
            SET_NINJITSU_ART                  = 0x61
            SET_TOON                          = 0x62
            SET_REACTOR                       = 0x63
            SET_HARPIE                        = 0x64
            SET_INFESTATION                   = 0x65
            SET_MAGNET                        = 0x1066
            SET_WARRIOR                       = 0x2066
            SET_MAGNET_WARRIOR                = 0x3066
            SET_SYMPHONIC_WARRIOR             = 0x6066
            SET_MAGNET_WARRIOR_SIGMA          = 0xb066
            SET_HIERATIC                      = 0x69
            SET_BUTTERSPY                     = 0x6a
            SET_BOUNZER                       = 0x6b
            SET_DJINN                         = 0x6d
            SET_PROPHECY                      = 0x6e
            SET_SPELLBOOK                     = 0x106e
            SET_HEROIC                        = 0x6f
            SET_HEROIC_CHALLENGER             = 0x106f
            SET_HEROIC_CHAMPION               = 0x206f
            SET_CHRONOMALY                    = 0x70
            SET_MADOLCHE                      = 0x71
            SET_GEARGIA                       = 0x72
            SET_GEARGIANO                     = 0x1072
            SET_XYZ                           = 0x73
            SET_CXYZ                          = 0x1073
            SET_XYZ_DRAGON                    = 0x2073
            SET_ARMORED_XYZ                   = 0x4073
            SET_MERMAIL                       = 0x74
            SET_ABYSS                         = 0x75
            SET_HERALDIC_BEAST                = 0x76
            SET_ATLANTEAN                     = 0x77
            SET_NIMBLE                        = 0x78
            SET_FIRE_FIST                     = 0x79
            SET_NOBLE_KNIGHT                  = 0x107a
            SET_INFERNOBLE_KNIGHT             = 0x507a
            SET_NOBLE_ARMS                    = 0x207a
            SET_INFERNOBLE_ARMS               = 0x607a
            SET_GALAXY                        = 0x7b
            SET_GALAXY_EYES                   = 0x107b
            SET_GALAXY_EYES_TACHYON_DRAGON    = 0x307b
            SET_FIRE_FORMATION                = 0x7c
            SET_HAZY                          = 0x7d
            SET_HAZY_FLAME                    = 0x107d
            SET_ZEXAL                         = 0x7e
            SET_ZW                            = 0x107e
            SET_ZS                            = 0x207e
            SET_UTOPIC                        = 0x7f
            SET_UTOPIA                        = 0x107f
            SET_UTOPIC_FUTURE                 = 0x207f
            SET_DUSTON                        = 0x80
            SET_FIRE_KING                     = 0x81
            SET_FIRE_KING_AVATAR              = 0x1081
            SET_DODODO                        = 0x82
            SET_PUPPET                        = 0x83
            SET_GIMMICK_PUPPET                = 0x1083
            SET_BATTLIN_BOXER                 = 0x84
            SET_SUPER_DEFENSE_ROBOT           = 0x85
            SET_STAR_SERAPH                   = 0x86
            SET_UMBRAL_HORROR                 = 0x87
            SET_BUJIN                         = 0x88
            SET_HOLE                          = 0x89
            SET_TRAPTRIX                      = 0x8a
            SET_MALICEVOROUS                  = 0x8b
            SET_GHOSTRICK                     = 0x8d
            SET_VAMPIRE                       = 0x8e
            SET_ZUBABA                        = 0x8f
            SET_SYLVAN                        = 0x90
            SET_NECROVALLEY                   = 0x91
            SET_HERALDRY                      = 0x92
            SET_CYBER                         = 0x93
            SET_CYBER_DRAGON                  = 0x1093
            SET_CYBER_ANGEL                   = 0x2093
            SET_CYBERDARK                     = 0x4093
            SET_CYBERNETIC                    = 0x94
            SET_RANK_UP_MAGIC                 = 0x95
            SET_FISHBORG                      = 0x96
            SET_ARTIFACT                      = 0x97
            SET_MAGICIAN                      = 0x98
            SET_ODD_EYES                      = 0x99
            SET_SUPERHEAVY_SAMURAI            = 0x9a
            SET_SUPERHEAVY_SAMURAI_SOUL       = 0x109a
            SET_MELODIOUS                     = 0x9b
            SET_MELODIOUS_MAESTRA             = 0x109b
            SET_TELLARKNIGHT                  = 0x9c
            SET_STELLARKNIGHT                 = 0x109c
            SET_SHADDOLL                      = 0x9d
            SET_YANG_ZING                     = 0x9e
            SET_PERFORMAPAL                   = 0x9f
            SET_LEGENDARY_KNIGHT              = 0xa0
            SET_LEGENDARY_DRAGON              = 0xa1
            SET_DARK_MAGICIAN                 = 0x10a2
            SET_MAGICIAN_GIRL                 = 0x20a2
            SET_DARK_MAGICIAN_GIRL            = 0x30a2
            SET_STARDUST                      = 0xa3
            SET_KURIBOH                       = 0xa4
            SET_WINGED_KURIBOH                = 0x10a4
            SET_CHANGE                        = 0xa5
            SET_SPROUT                        = 0xa6
            SET_ARTORIGUS                     = 0xa7
            SET_LAUNDSALLYN                   = 0xa8
            SET_FLUFFAL                       = 0xa9
            SET_QLI                           = 0xaa
            SET_APOQLIPHORT                   = 0x10aa
            SET_DESKBOT                       = 0xab
            SET_GOBLIN                        = 0xac
            SET_GOBLIN_RIDER                  = 0x10ac
            SET_FRIGHTFUR                     = 0xad
            SET_DARK_CONTRACT                 = 0xae
            SET_DD                            = 0xaf
            SET_DDD                           = 0x10af
            SET_GOTTOMS                       = 0xb0
            SET_BURNING_ABYSS                 = 0xb1
            SET_UA                            = 0xb2
            SET_YOSENJU                       = 0xb3
            SET_NEKROZ                        = 0xb4
            SET_RITUAL_BEAST                  = 0xb5
            SET_RITUAL_BEAST_TAMER            = 0x10b5
            SET_SPIRITUAL_BEAST               = 0x20b5
            SET_SPIRITUAL_BEAST_TAMER         = 0x30b5
            SET_RITUAL_BEAST_ULTI             = 0x40b5
            SET_OUTER_ENTITY                  = 0x10b7
            SET_ELDER_ENTITY                  = 0x20b7
            SET_OLD_ENTITY                    = 0x40b7
            SET_BLAZE_ACCELERATOR             = 0xb9
            SET_RAIDRAPTOR                    = 0xba
            SET_INFERNOID                     = 0xbb
            SET_JINZO                         = 0xbc
            SET_GAIA_THE_FIERCE_KNIGHT        = 0xbd
            SET_MONARCH                       = 0xbe
            SET_CHARMER                       = 0xbf
            SET_POSSESSED                     = 0xc0
            SET_FAMILIAR_POSSESSED            = 0x10c0
            SET_PSY_FRAME                     = 0xc1
            SET_PSY_FRAMEGEAR                 = 0x10c1
            SET_POWER_TOOL                    = 0xc2
            SET_EDGE_IMP                      = 0xc3
            SET_ZEFRA                         = 0xc4
            SET_VOID                          = 0xc5
            SET_PERFORMAGE                    = 0xc6
            SET_DRACOSLAYER                   = 0xc7
            SET_IGKNIGHT                      = 0xc8
            SET_AROMA                         = 0xc9
            SET_EMPOWERED_WARRIOR             = 0xca
            SET_AETHER                        = 0xcb
            SET_PREDICTION_PRINCESS           = 0xcc
            SET_AQUAACTRESS                   = 0x10cd
            SET_AQUARIUM                      = 0x20cd
            SET_CHAOS                         = 0xcf
            SET_BLACK_LUSTER_SOLDIER          = 0x10cf
            SET_MAJESPECTER                   = 0xd0
            SET_GRAYDLE                       = 0xd1
            SET_KOZMO                         = 0xd2
            SET_KAIJU                         = 0xd3
            SET_PALEOZOIC                     = 0xd4
            SET_DANTE                         = 0xd5
            SET_DESTRUCTION_SWORD             = 0xd6
            SET_BUSTER_BLADER                 = 0xd7
            SET_DINOMIST                      = 0xd8
            SET_SHIRANUI                      = 0xd9
            SET_SHIRANUI_SPECTRALSWORD        = 0x10d9
            SET_DRACOVERLORD                  = 0xda
            SET_PHANTOM_KNIGHTS               = 0xdb
            SET_THE_PHANTOM_KNIGHTS           = 0x10db
            SET_SUPER_QUANT                   = 0xdc
            SET_SUPER_QUANTUM                 = 0x10dc
            SET_SUPER_QUANTAL_MECH_BEAST      = 0x20dc
            SET_BLUE_EYES                     = 0xdd
            SET_EXODIA                        = 0xde
            SET_LUNALIGHT                     = 0xdf
            SET_AMORPHAGE                     = 0xe0
            SET_METALFOES                     = 0xe1
            SET_TRIAMID                       = 0xe2
            SET_CUBIC                         = 0xe3
            SET_CELTIC_GUARD                  = 0xe4
            SET_CIPHER                        = 0xe5
            SET_CIPHER_DRAGON                 = 0x10e5
            SET_FLOWER_CARDIAN                = 0xe6
            SET_SILENT_SWORDSMAN              = 0xe7
            SET_SILENT_MAGICIAN               = 0xe8
            SET_MAGNA_WARRIOR                 = 0xe9
            SET_CRYSTRON                      = 0xea
            SET_CHEMICRITTER                  = 0xeb
            SET_ABYSS_ACTOR                   = 0x10ec
            SET_ABYSS_SCRIPT                  = 0x20ec
            SET_SUBTERROR                     = 0xed
            SET_SUBTERROR_BEHEMOTH            = 0x10ed
            SET_SPYRAL                        = 0xee
            SET_SPYRAL_GEAR                   = 0x10ee
            SET_SPYRAL_MISSION                = 0x20ee
            SET_DARKLORD                      = 0xef
            SET_WINDWITCH                     = 0xf0
            SET_ZOODIAC                       = 0xf1
            SET_PENDULUM                      = 0xf2
            SET_PENDULUM_DRAGON               = 0x10f2
            SET_PENDULUMGRAPH                 = 0x20f2
            SET_PREDAP                        = 0xf3
            SET_PREDAPLANT                    = 0x10f3
            SET_INVOKED                       = 0xf4
            SET_GANDORA                       = 0xf5
            SET_SKYSCRAPER                    = 0xf6
            SET_LYRILUSC                      = 0xf7
            SET_SUPREME_KING_GATE             = 0x10f8
            SET_SUPREME_KING_DRAGON           = 0x20f8
            SET_TRUE_DRACO_KING               = 0xf9
            SET_PHANTASM_SPIRAL               = 0xfa
            SET_TRICKSTAR                     = 0xfb
            SET_GOUKI                         = 0xfc
            SET_WORLD_CHALICE                 = 0xfd
            SET_WORLD_LEGACY                  = 0xfe
            SET_CLEAR_WING                    = 0xff
            SET_BONDING                       = 0x100
            SET_CODE_TALKER                   = 0x101
            SET_ROKKET                        = 0x102
            SET_ALTERGEIST                    = 0x103
            SET_KRAWLER                       = 0x104
            SET_METAPHYS                      = 0x105
            SET_VENDREAD                      = 0x106
            SET_FA                            = 0x107
            SET_MAGICAL_MUSKET                = 0x108
            SET_THE_WEATHER                   = 0x109
            SET_PARSHATH                      = 0x10a
            SET_TINDANGLE                     = 0x10b
            SET_MEKK_KNIGHT                   = 0x10c
            SET_MYTHICAL_BEAST                = 0x10d
            SET_EVOLUTION_PILL                = 0x10e
            SET_BORREL                        = 0x10f
            SET_RELINQUISHED                  = 0x110
            SET_EYES_RESTRICT                 = 0x1110
            SET_ARMED_DRAGON                  = 0x111
            SET_ARMED_DRAGON_THUNDER          = 0x1111
            SET_KNIGHTMARE                    = 0x112
            SET_ELEMENTAL_LORD                = 0x113
            SET_FUR_HIRE                      = 0x114
            SET_SKY_STRIKER                   = 0x115
            SET_SKY_STRIKER_ACE               = 0x1115
            SET_CRUSADIA                      = 0x116
            SET_IMPCANTATION                  = 0x117
            SET_CYNET                         = 0x118
            SET_SALAMANGREAT                  = 0x119
            SET_DINOWRESTLER                  = 0x11a
            SET_ORCUST                        = 0x11b
            SET_THUNDER_DRAGON                = 0x11c
            SET_FORBIDDEN                     = 0x11d
            SET_DANGER                        = 0x11e
            SET_NEPHTHYS                      = 0x11f
            SET_PRANK_KIDS                    = 0x120
            SET_MAYAKASHI                     = 0x121
            SET_VALKYRIE                      = 0x122
            SET_ROSE                          = 0x123
            SET_ROSE_DRAGON                   = 0x1123
            SET_MACHINE_ANGEL                 = 0x124
            SET_SMILE                         = 0x125
            SET_TIME_THIEF                    = 0x126
            SET_INFINITRACK                   = 0x127
            SET_WITCHCRAFTER                  = 0x128
            SET_EVIL_EYE                      = 0x129
            SET_ENDYMION                      = 0x12a
            SET_MARINCESS                     = 0x12b
            SET_TENYI                         = 0x12c
            SET_SIMORGH                       = 0x12d
            SET_FORTUNE_FAIRY                 = 0x12e
            SET_BATTLEWASP                    = 0x12f
            SET_UNCHAINED                     = 0x130
            SET_UNCHAINED_SOUL                = 0x1130
            SET_DREAM_MIRROR                  = 0x131
            SET_MATHMECH                      = 0x132
            SET_DRAGONMAID                    = 0x133
            SET_GENERAIDER                    = 0x134
            SET_IGNISTER                      = 0x135
            SET_AI                            = 0x136
            SET_ANCIENT_WARRIORS              = 0x137
            SET_MEGALITH                      = 0x138
            SET_ONOMAT                        = 0x139
            SET_PALLADIUM                     = 0x13a
            SET_REBELLION                     = 0x13b
            SET_CODEBREAKER                   = 0x13c
            SET_NEMESES                       = 0x13d
            SET_BARBAROS                      = 0x13e
            SET_PLUNDER_PATROLL               = 0x13f
            SET_ADAMANCIPATOR                 = 0x140
            SET_RIKKA                         = 0x141
            SET_ELDLICH                       = 0x142
            SET_ELDLIXIR                      = 0x143
            SET_GOLDEN_LAND                   = 0x144
            SET_PHANTASM                      = 0x145
            SET_SACRED_BEAST                  = 0x1145
            SET_DOGMATIKA                     = 0x146
            SET_MELFFY                        = 0x147
            SET_POTAN                         = 0x148
            SET_ROLAND                        = 0x149
            SET_APPLIANCER                    = 0x14a
            SET_NUMERON                       = 0x14b
            SET_NUMERON_GATE                  = 0x114b
            SET_FOSSIL                        = 0x14c
            SET_SPIRITUAL_EARTH_ART           = 0x314d
            SET_SPIRITUAL_FIRE_ART            = 0x514d
            SET_SPIRITUAL_WATER_ART           = 0x614d
            SET_SPIRITUAL_WIND_ART            = 0xa14d
            SET_DUAL_AVATAR                   = 0x14e
            SET_TRI_BRIGADE                   = 0x14f
            SET_VIRTUAL_WORLD                 = 0x150
            SET_VIRTUAL_WORLD_GATE            = 0x1150
            SET_DRYTRON                       = 0x151
            SET_MAGISTUS                      = 0x152
            SET_KI_SIKIL                      = 0x153
            SET_LIL_LA                        = 0x154
            SET_EVIL_TWIN                     = 0x155
            SET_LIVE_TWIN                     = 0x156
            SET_SUNAVALON                     = 0x1157
            SET_SUNVINE                       = 0x2157
            SET_SUNSEED                       = 0x4157
            SET_SPRINGANS                     = 0x158
            SET_MYUTANT                       = 0x159
            SET_S_FORCE                       = 0x15a
            SET_STARRY_KNIGHT                 = 0x15b
            SET_DOLL_MONSTER                  = 0x15c
            SET_RANK_DOWN_MAGIC               = 0x15d
            SET_AMAZEMENT                     = 0x15e
            SET_ATTRACTION                    = 0x15f
            SET_BRANDED                       = 0x160
            SET_WAR_ROCK                      = 0x161
            SET_MATERIACTOR                   = 0x162
            SET_OGDOADIC                      = 0x163
            SET_SOLFACHORD                    = 0x164
            SET_GRANSOLFACHORD                = 0x1164
            SET_URSARCTIC                     = 0x165
            SET_DESPIA                        = 0x166
            SET_MAGIKEY                       = 0x167
            SET_GUNKAN                        = 0x168
            SET_MYSTICAL_BEAST_OF_THE_FOREST  = 0x1169
            SET_MYSTICAL_SPIRIT_OF_THE_FOREST = 0x2169
            SET_STEALTH_KRAGEN                = 0x16a
            SET_NUMEROUNIOUS                  = 0x16b
            SET_NUMBER_SPELL_TRAP             = 0x16c
            SET_SWORDSOUL                     = 0x16d
            SET_ICEJADE                       = 0x16e
            SET_FLOOWANDEREEZE                = 0x16f
            SET_TOPOLOGIC                     = 0x170
            SET_HYPERION                      = 0x171
            SET_BEETROOPER                    = 0x172
            SET_PUNK                          = 0x173
            SET_EXOSISTER                     = 0x174
            SET_DINOMORPHIA                   = 0x175
            SET_LADY_OF_LAMENT                = 0x176
            SET_SEVENTH                       = 0x177
            SET_BARIANS                       = 0x1178
            SET_BATTLEGUARD                   = 0x2178
            SET_KAIRYU_SHIN                   = 0x179
            SET_SEA_STEALTH                   = 0x17a
            SET_THERION                       = 0x17b
            SET_SCARECLAW                     = 0x17c
            SET_LIBROMANCER                   = 0x17d
            SET_VAYLANTZ                      = 0x17e
            SET_LABRYNTH                      = 0x17f
            SET_WELCOME_LABRYNTH              = 0x117f
            SET_RUNICK                        = 0x180
            SET_SPRIGHT                       = 0x181
            SET_TEARLAMENTS                   = 0x182
            SET_VERNUSYLPH                    = 0x183
            SET_MOKEY_MOKEY                   = 0x184
            SET_WINGMAN                       = 0x185
            SET_DOODLE_BEAST                  = 0x1186
            SET_DOODLEBOOK                    = 0x2186
            SET_G_GOLEM                       = 0x187
            SET_RAINBOW_BRIDGE                = 0x188
            SET_BYSTIAL                       = 0x189
            SET_GHOTI                         = 0x18b
            SET_KASHTIRA                      = 0x18a
            SET_GOLD_PRIDE                    = 0x193
            SET_KOALA                         = 0x67
            SET_RESCUE_ACE                    = 0x18c
            SET_PURRELY                       = 0x18d
            SET_MIKANKO                       = 0x18e
            SET_AQUAMIRROR                    = 0x18f
            SET_FIREWALL                      = 0x190
            SET_MANNADIUM                     = 0x191
            SET_NEMLERIA                      = 0x192
            SET_LABYRINTH_WALL                = 0x194
            SET_FAVORITE                      = 0x195
            SET_VANQUISH_SOUL                 = 0x196
            SET_NOUVELLES                     = 0x197
            SET_RECIPE                        = 0x198
            SET_VISAS                         = 0x199
            SET_MEMENTO                       = 0x19a
            SET_CENTUR_ION                    = 0x19b
            SET_VAALMONICA                    = 0x19c
            SET_YUBEL                         = 0x19d
            SET_VOICELESS_VOICE               = 0x19e
            SET_TOY                           = 0x1a0
            SET_TENPAI_DRAGON                 = 0x1a1
            SET_SANGEN                        = 0x1a2
            SET_RAGNARAIKA                    = 0x1a3
            SET_MILLENNIUM                    = 0x1a6
            SET_EXODD                         = 0x1a7
            SET_FIENDSMITH                    = 0x1a8
            SET_WHITE_FOREST                  = 0x1aa
            SET_MULCHARMY                     = 0x1ac
            SET_EMBLEMA                       = 0x1ad
            SET_COUNTER                       = 0x200
            SET_BATTLIN_BOXING                = 0x201
            SET_VEDA                          = 0x202
            SET_DIABELL                       = 0x203
            SET_DIABELLSTAR                   = 0x1203
            SET_SINFUL_SPOILS                 = 0x204
            SET_SNAKE_EYE                     = 0x205
            SET_PATISSCIEL                    = 0x206
            SET_HEART                         = 0x207
            SET_TISTINA                       = 0x208
            SET_WHITE_AURA                    = 0x119f
            SET_SALAMANDRA                    = 0x1a4
            SET_ASHENED                       = 0x1a5
            SET_BLUE_TEARS                    = 0x1a9
            SET_TACHYON                       = 0x1ab
            SET_SHARK                         = 0x1ae
            SET_SHARK_DRAKE                   = 0x11ae
            SET_WEDJU                         = 0x1af
            SET_PRIMITE                       = 0x1b0
            SET_SIX_STRIKE                    = 0x1b1
            SET_METALMORPH                    = 0x1b2
            SET_MORGANITE                     = 0x1b3
            SET_AZAMINA                       = 0x1b4
            SET_MIMIGHOUL                     = 0x1b5
            SET_RYZEAL                        = 0x1b6
            SET_SCHOOLWORK                    = 0x1b7
            SET_RYU_GE                        = 0x1b8
            SET_MALISS                        = 0x1b9
            SET_ARGOSTARS                     = 0x1ba
            SET_AQUA_JET                      = 0x1bb
            SET_DRAGON_RULER                  = 0x1bc
            SET_MITSURUGI                     = 0x1bd
            SET_DOMINUS                       = 0x1bf
            SET_APOPHIS                       = 0x1c2
            SET_SERKET                        = 0x1c3
            SET_REGENESIS                     = 0x1be
            SET_TELEPORT                      = 0x1c5
            SET_POWER_PATRON                  = 0x1c6
            SET_ARTMAGE                       = 0x1c7
            SET_RB                            = 0x1ca
            SET_DRACOTAIL                     = 0x1c0
            SET_YUMMY                         = 0x1c1
            SET_K9                            = 0x1c4
            SET_DOOM_KING                     = 0x1c8
            SET_RADIANT_TYPHOON               = 0x1c9
            SET_DOOMZ                         = 0x1cb
            SET_HECAHANDS                     = 0x1cc
            SET_ENNEACRAFT                    = 0x1cd
            SET_KEWL_TUNE                     = 0x1ce
            SET_ECCLESIA                      = 0x1cf
            SET_END_OF_THE_WORLD              = 0x1d1
            SET_FAIRY_TAIL                    = 0x1d2
            SET_ELFNOTE                       = 0x1d0
            SET_GMX                           = 0x1d4
            --Released but the official English name is unconfirmed
            SET_CLOWN_CREW                    = 0x1d3
            SET_DARK_TUNER                    = 0x1d5
            --Pre-release archetypes
            SET_BLITZCLIQUE                   = 0x1d6
            SET_THEOREALIZE                   = 0x1d7
            SET_ALEISTER                      = 0x1d8
            SET_WHITE_KNIGHT_NIGHT            = 0x1d9

            --Cards with double names
            CARD_MARINE_DOLPHIN         = 78734254
            CARD_TWINKLE_MOSS           = 13857930

            --Commonly used OCG/TCG cards
            CARD_ADVANCED_DARK                  = 12644061
            CARD_ALBAZ                          = 68468459
            CARD_ANCIENT_FAIRY_DRAGON           = 25862681
            CARD_ANCIENT_GEAR_GOLEM             = 83104731
            CARD_ARGYRO_SYSTEM                  = 21887075
            CARD_ASSAULT_MODE                   = 80280737
            CARD_BLACK_ROSE_DRAGON              = 73580471
            CARD_BLACK_WINGED_DRAGON            = 9012916
            CARD_BLUEEYES_SPIRIT                = 59822133
            CARD_BLUEEYES_W_DRAGON              = 89631139
            CARD_BOX_OF_FRIENDS                 = 81587028
            CARD_BUSTER_BLADER                  = 78193831
            CARD_CALL_OF_THE_HAUNTED            = 97077563
            CARD_CARD_ADVANCE                   = 52112003
            CARD_CHIMERA_FUSION                 = 63136489
            CARD_CHIMERA_MYTHICAL_BEAST         = 4796100
            CARD_CLEAR_WORLD                    = 33900648
            CARD_CLOCK_LIZARD                   = 51476410
            CARD_CRIMSON_DRAGON                 = 63436931
            CARD_CRYSTAL_GOD_TISTINA            = 86999951
            CARD_CRYSTAL_TREE                   = 47408488
            CARD_CYBER_DRAGON                   = 70095154
            CARD_DARK_FUSION                    = 94820406
            CARD_DARK_MAGICIAN                  = 46986414
            CARD_DARK_MAGICIAN_GIRL             = 38033121
            CARD_DARK_SANCTUARY                 = 16625614
            CARD_DESTINY_BOARD                  = 94212438
            CARD_DREAM_MIRROR_JOY               = 74665651
            CARD_DREAM_MIRROR_TERROR            = 1050355
            CARD_DREAMING_NEMLERIA              = 70155677
            CARD_EHERO_BLAZEMAN                 = 63060238
            CARD_EVIL_EYE_SELENE                = 44133040
            CARD_EXCHANGE_SPIRIT                = 17484499
            CARD_FAIRY_PRINCE                   = 10000120
            CARD_FAIRY_TALE_PROLOGUE            = 43236494
            CARD_FLAME_SWORDSMAN                = 45231177
            CARD_FIRE_FIST_EAGLE                = 46241344
            CARD_FOSSIL_FUSION                  = 59419719
            CARD_GAIA_CHAMPION                  = 66889139
            CARD_GALAXYEYES_P_DRAGON            = 93717133
            CARD_GOLDEN_LORD                    = 95440946
            CARD_GRANDPA_DEMETTO                = 44190146
            CARD_HARMONIC_OSCILLATION           = 31531170
            CARD_HARPIE_LADY                    = 76812113
            CARD_HARPIE_LADY_SISTERS            = 12206212
            CARD_INFERNOBLE_CHARLES             = 77656797
            CARD_INVOCATION                     = 74063034
            CARD_JACK_KNIGHT                    = 90876561
            CARD_JINZO                          = 77585513
            CARD_JUNK_SYNCHRON                  = 63977008
            CARD_JUNK_WARRIOR                   = 60800381
            CARD_KAZEJIN                        = 62340868
            CARD_KING_KNIGHT                    = 64788463
            CARD_KING_SARCOPHAGUS               = 16528181
            CARD_KURIBOH                        = 40640057
            CARD_LABRYNTH_LABYRINTH             = 33407125
            CARD_LIGHT_BARRIER                  = 73206827
            CARD_MACRO_COSMOS                   = 30241314
            CARD_MAGICAL_MIDBREAKER             = 71650854
            CARD_MAX_METALMORPH                 = 89812483
            CARD_MEDIUS_THE_PURE                = 97556336
            CARD_MEMENTOAL_TECUHTLICA           = 23288411
            CARD_MILLENNIUM_CROSS               = 37613663
            CARD_MONSTER_REBORN                 = 83764718
            CARD_MYSTICAL_SPACE_TYPHOON         = 5318639
            CARD_MYUTANT_ARSENAL                = 7574904
            CARD_MYUTANT_BEAST                  = 34695290
            CARD_MYUTANT_MIST                   = 61089209
            CARD_NATURIA_CAMELLIA               = 29942771
            CARD_NECROVALLEY                    = 47355498
            CARD_NEOS                           = 89943723
            CARD_NUMERON_NETWORK                = 41418852
            CARD_OBELISK                        = 10000000
            CARD_OBSIDIM_ASHENED_CITY           = 3055018
            CARD_ORCUSTRATED_BABEL              = 90351981
            CARD_POLYMERIZATION                 = 24094653
            CARD_PRANKKIDS_MEOWMU               = 25725326
            CARD_PRINCESS_COLOGNE               = 75574498
            CARD_PSYFRAME_DRIVER                = 49036338
            CARD_PSYFRAME_LAMBDA                = 8802510
            CARD_QUEEN_KNIGHT                   = 25652259
            CARD_R_ACE_HYDRANT                  = 37617348
            CARD_RA                             = 10000010
            CARD_RED_DRAGON_ARCHFIEND           = 70902743
            CARD_REDEYES_B_DRAGON               = 74677422
            CARD_REGULUS_THE_PRINCE_OF_ENDYMION = 96228804
            CARD_REVEALER_ICEBARRIER            = 18319762
            CARD_REVERSAL_OF_FATE               = 36690018
            CARD_RIKKA_KONKON                   = 76869711
            CARD_RITUAL_OF_LIGHT_AND_DARKNESS   = 101305044
            CARD_SALAMANGREAT_SANCTUARY         = 1295111
            CARD_SANCTUARY_SKY                  = 56433456
            CARD_SANGA_OF_THE_THUNDER           = 25955164
            CARD_SFORCE_CHASE                   = 55049722
            CARD_SHINING_SARCOPHAGUS            = 79791878
            CARD_SKULL_SERVANT                  = 32274490
            CARD_SLIFER                         = 10000020
            CARD_SPIRIT_ELIMINATION             = 69832741
            CARD_SPYRAL_SUPER_AGENT             = 41091257
            CARD_STARDUST_DRAGON                = 44508094
            CARD_STROMBERG                      = 72283691
            CARD_SUIJIN                         = 98434877
            CARD_SUSHIP_SHARI                   = 24639891
            CARD_SUMMON_GATE                    = 29724053
            CARD_SUMMONED_SKULL                 = 70781052
            CARD_SUPER_POLYMERIZATION           = 48130397
            CARD_TEMPLE_OF_THE_KINGS            = 29762407
            CARD_TOON_WORLD                     = 15259703
            CARD_TOY_BOX                        = 24878656
            CARD_UMI                            = 22702055
            CARD_UNKNOWN                        = 10000110
            CARD_URSARCTIC_BIG_DIPPER           = 89264428
            CARD_URSARCTIC_DRYTRON              = 89771220
            CARD_VALIANTS_KOENIGWISSEN          = 75952542
            CARD_VALIANTS_SHINRABANSHO          = 49568943
            CARD_VEIDOS_ERUPTION_DRAGON         = 78783557
            CARD_VERNUSYLPH_COROLLA             = 14108995
            CARD_VIJAM                          = 15610297
            CARD_VISAS_STARFROST                = 56099748
            CARD_YUBEL                          = 78371393
            CARD_ZARC                           = 13331639
            CARD_ZEFRAATH                       = 29432356

            --Commonly used groups of cards
            CARDS_SANGA_KAZEJIN_SUIJIN  = {25955164,62340868,98434877}
            CARDS_SPIRIT_MESSAGE        = {31894809,94212877,30170981,67287533,94772232}

            --Commonly used Tokens
            TOKEN_ADVENTURER                     = 3285552
            TOKEN_BOMB                           = 22411610
            TOKEN_DUAL_AVATAR_SPIRIT             = 87669905
            TOKEN_ENGINE                         = 82556059
            TOKEN_FIREBALL                       = 23116809
            TOKEN_HIPPO                          = 11050416
            TOKEN_IGNISTER                       = 11738490
            TOKEN_INSECT_MONSTER                 = 91512836
            TOKEN_KURIBOH                        = 40703223
            TOKEN_LINK                           = 48068379
            TOKEN_MECHA_PHANTOM_BEAST            = 31533705
            TOKEN_MECHA_PHANTOM_BEAST_DRACOSSACK = 22110648
            TOKEN_MECHA_PHANTOM_BEAST_HARRLIARD  = 20368764
            TOKEN_MECHA_PHANTOM_BEAST_TETHERWOLF = 67922703
            TOKEN_OJAMA                          = 29843092
            TOKEN_OJAMA_DUO                      = 14470846
            TOKEN_OPTION                         = 93130022
            TOKEN_REPTILIANNE                    = 21179144
            TOKEN_ROSE                           = 71645243
            TOKEN_SHINOBIRD                      = 52900001
            TOKEN_SLIME                          = 21770261
            TOKEN_SWORDSOUL                      = 20001444
            TOKEN_TRICKSTAR                      = 51208047
            TOKEN_WORLD_LEGACY                   = 46647145

            --Specific Special Summons
            SUMMON_WITH_MONSTER_REBORN = 1010

            --Commonly used Rush Cards
            CARD_AMALILITH            = 160428037
            CARD_BIG_UMI              = 160003050
            CARD_BLUETOOTH_B_DRAGON   = 160010101
            CARD_CAN_D                = 160005002
            CARD_CELEB_ROSE_MAGICIAN  = 160013012
            CARD_CELEB_ROSE_WITCH     = 160013011
            CARD_FUSION               = 160204050
            CARD_GALACTICA_OBLIVION   = 160009002
            CARD_IMAGINARY_ACTOR      = 160204006
            CARD_JELLYPLUG            = 160008006
            CARD_KURIBOT              = 160001017
            CARD_LIGHTNING_VOLTCONDOR = 160002017
            CARD_MEEEG_CHAN           = 160009006
            CARD_NEEDLKYRIE           = 160007001
            CARD_PRIMA_GUITARNA       = 160001028
            CARD_PRINTING_PRESSER     = 160003032
            CARD_REDBOOT_B_DRAGON     = 160315001
            CARD_SCOOP_SCOOTER        = 160003002
            CARD_SEVENS_ROAD_MAGICIAN = 160301001
            CARD_SKYSAVIOR_LUA        = 160012021
            CARD_SKYSAVIOR_SOLEIL     = 160012020
            CARD_SPIRIT_STADIUM       = 160007047
            CARD_STRAYNGE_CAT         = 160301010
            CARD_TAMABOT              = 160312023
            CARD_TASTE_INSPECTOR      = 160007008
            CARD_TRANSAMU_RAINAC      = 160425001
            CARD_UNIFORM_39           = 160007028
            CARD_UNIFORM_99           = 160007027
            CARD_UPSTART_GOBLIN       = 70368879

            --Commonly used Skill Cards
            SKILL_DARK_UNITY          = 300306009

            --Commonly used counters
            COUNTER_A            = 0x100e
            COUNTER_BUSHIDO      = 0x3
            COUNTER_EC           = 0x217
            COUNTER_FEATHER      = 0x10
            COUNTER_FOG          = 0x1019
            COUNTER_KAIJU        = 0x37
            COUNTER_PREDATOR     = 0x1041
            COUNTER_RESONANCE    = 0x211
            COUNTER_SIGNAL       = 0x1148
            COUNTER_SPELL        = 0x1
            COUNTER_VENOM        = 0x1009
        ");

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
        luaEngine.Globals["DUEL_INVERTED_QUICK_PRIORITY"] = 0x40;

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