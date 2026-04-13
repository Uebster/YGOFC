using UnityEngine;
using MoonSharp.Interpreter;

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

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Tabela Auxiliar Básica
        DynValue auxTable = DynValue.NewTable(luaEngine);
        auxTable.Table.Set("Stringid", luaEngine.DoString("return function(code, id) return (code * 16) + id end"));
        auxTable.Table.Set("GlobalCheck", DynValue.FromObject(luaEngine, (System.Action<object, Closure>)((s, func) => { func?.Call(); })));
        
        // Constantes lógicas (Closures nativas)
        auxTable.Table.Set("TRUE", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("FALSE", luaEngine.DoString("return function(...) return false end"));

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
            (System.Func<object, object>)(group => {
                string luaCode = "return function() return nil end";
                return luaEngine.DoString(luaCode);
            })));
        
        auxTable.Table.Set("NecroValleyFilter", DynValue.FromObject(luaEngine,
            (System.Func<object, object>)(filterFunc => {
                string luaCode = "return function(c) return true end";
                return luaEngine.DoString(luaCode);
            })));

        auxTable.Table.Set("RemainFieldCost", luaEngine.DoString(@"
            return function(e, tp, eg, ep, ev, re, r, rp, chk)
                if chk == 0 then return true end
                return true
            end
        "));

        dummyClosureTrue = auxTable.Table.Get("TRUE").Function;
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
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8; 
        luaEngine.Globals["LOCATION_PUBLIC"] = 0x4 | 0x8 | 0x10 | 0x20;  
        luaEngine.Globals["LOCATION_ALL"] = 0x3ff;
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
        luaEngine.Globals["EVENT_SUMMON_SUCCESS"] = 1100;
        luaEngine.Globals["EVENT_FLIP_SUMMON_SUCCESS"] = 1101;
        luaEngine.Globals["EVENT_SPSUMMON_SUCCESS"] = 1102;
        luaEngine.Globals["EVENT_DESTROYED"] = 1029;
        luaEngine.Globals["EVENT_DESTROY"] = 1029;
        luaEngine.Globals["EVENT_PHASE"] = 0x1000;
        luaEngine.Globals["EVENT_PHASE_START"] = 8192;
        luaEngine.Globals["EVENT_CHANGE_POS"] = 1016;
        luaEngine.Globals["EVENT_LEAVE_FIELD"] = 1015;
        luaEngine.Globals["EVENT_TO_GRAVE"] = 1014;
        luaEngine.Globals["EVENT_BATTLE_DESTROYING"] = 1139;
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
        luaEngine.Globals["CATEGORY_DRAW"] = 0x1;
        luaEngine.Globals["CATEGORY_RECOVER"] = 0x100000;
        luaEngine.Globals["CATEGORY_DESTROY"] = 0x20000;
        luaEngine.Globals["REASON_EFFECT"] = 0x40;
        luaEngine.Globals["CHAININFO_TARGET_PLAYER"] = 1;
        luaEngine.Globals["CHAININFO_TARGET_PARAM"] = 2;

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
        luaEngine.Globals["RACE_WINGED_BEAST"] = 0x200;
        luaEngine.Globals["RACE_PLANT"] = 0x400;
        luaEngine.Globals["RACE_INSECT"] = 0x800;
        luaEngine.Globals["RACE_THUNDER"] = 0x1000;
        luaEngine.Globals["RACE_DRAGON"] = 0x2000;
        luaEngine.Globals["RACE_BEAST"] = 0x4000;
        luaEngine.Globals["RACE_BEAST_WARRIOR"] = 0x8000;
        luaEngine.Globals["RACE_DINOSAUR"] = 0x10000;
        luaEngine.Globals["RACE_FISH"] = 0x20000;
        luaEngine.Globals["RACE_SEA_SERPENT"] = 0x40000;
        luaEngine.Globals["RACE_REPTILE"] = 0x80000;
        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x01;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x02;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x04;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x08;
        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_DIVINE"] = 0x40;

        // Constantes de Status Oficiais do OCGCore
        luaEngine.Globals["EFFECT_UPDATE_ATTACK"] = 1;
        luaEngine.Globals["EFFECT_SET_ATTACK"] = 2;
        luaEngine.Globals["EFFECT_SET_ATTACK_FINAL"] = 3;
        luaEngine.Globals["EFFECT_UPDATE_DEFENSE"] = 4;
        luaEngine.Globals["EFFECT_SET_DEFENSE"] = 5;
        luaEngine.Globals["EFFECT_SET_DEFENSE_FINAL"] = 6;
        luaEngine.Globals["EFFECT_EQUIP_LIMIT"] = 147;

        // Metatable Global Segura
        luaEngine.DoString(@"
            local dummyFunc = function() return Effect.CreateEffect(nil) end
            local procMt = { __index = function() return dummyFunc end, __call = function() return dummyFunc end }
            local dummyTable = {}
            setmetatable(dummyTable, procMt)

            setmetatable(_G, {
                __index = function(t, k)
                    if type(k) == 'string' and string.match(k, '^[A-Z0-9_]+$') then return 0 end
                    if type(k) == 'string' and string.match(k, '^[A-Z][a-zA-Z0-9_]*$') then return dummyTable end
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
            setmetatable(Card, {
                __index = function(t, k)
                    return function(c, ...)
                        if c and type(c) == 'userdata' then
                            local func = c[k]
                            if type(func) == 'function' then
                                return func(...)
                            end
                        end
                        return false
                    end
                end
            })

            Core = {}
            function Core.Attack(attacker, target)
                Duel.RaiseEvent(attacker, 1102, nil, 0, attacker:GetControler(), attacker:GetControler(), 0)
                coroutine.yield('WaitChain')
                if not attacker:IsOnField() then return false end
                if target ~= nil and not target:IsOnField() then return false end
                coroutine.yield('PlayAttackAnimation')
                if target ~= nil and target:IsFacedown() then coroutine.yield('RevealTarget') end
                Duel.CalculateDamage(attacker, target)
                return true
            end

            function Core.NormalSummon(player, card, isSet)
                local level = card:GetLevel()
                local tributes = 0
                if level >= 5 and level <= 6 then tributes = 1 end
                if level >= 7 then tributes = 2 end
                if Duel.GetActivityCount(player, 2) > 0 then return false end
                if tributes > 0 then
                    local sg = Duel.SelectReleaseGroup(player, Card.IsReleasable, tributes, tributes, nil)
                    if not sg or sg:GetCount() < tributes then return false end
                    Duel.Release(sg, REASON_SUMMON)
                end
                if isSet then Duel.MSet(player, card, true, nil) else Duel.Summon(player, card, true, nil) end
                return true
            end
        ");
    }
}