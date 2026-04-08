// Assets/Scripts/CardEffectManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System.Text.RegularExpressions;
using System.Linq;

public enum CardLocation { Hand, Deck, Field, ExtraDeck, Graveyard, Banished, Unknown }
public enum SendReason { Battle, Effect, Cost, Tribute, Destroyed, Discarded, Mill, Return, Rule, Unknown }

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    public enum TargetType { Monster, Spell, Trap, Any }

    // Dicionário de zonas físicas bloqueadas (ex: Ojama King)
    public Dictionary<CardDisplay, List<Transform>> blockedZonesByCard = new Dictionary<CardDisplay, List<Transform>>();

    public Script luaEngine;
    public LuaDuel luaDuel;

    // Variáveis de controle de Assincronicidade do Lua
    public DynValue activeLuaCoroutine = null;
    public bool isWaitingForLuaYield = false;
    public DynValue yieldReturnValue = null;
    public bool lastCoroutineSuccess = true;

    // --- SISTEMA DE CORRENTE (CHAIN SYSTEM LIFO) ---
    public class ChainLink
    {
        public int chainIndex;
        public LuaEffect effect;
        public LuaCard card;
        public int player;
        public object triggerArgs;
        public bool isNegated = false;
        public bool isActivationNegated = false;
    }

    public List<ChainLink> currentChain = new List<ChainLink>();
    public bool isChainResolving = false;

    // Flags de estado global mantidas para compatibilidade com outros Managers
    public bool armoredGlassActive = false;
    public bool reverseStats = false;
    public bool invertDecks = false;
    public bool cannotSummonMonstersThisTurn = false;
    public bool trapsBlockedThisTurn = false;

    public Dictionary<CardDisplay, LuaCard> activeLuaCards = new Dictionary<CardDisplay, LuaCard>();

    // Closures seguras para preencher funções em branco e evitar "attempt to call a nil value"
    public Closure dummyClosureTrue;

    // FASE 26: Properties for LuaAPI compatibility
    public UnityEngine.EventSystems.EventSystem eventSystem => UnityEngine.EventSystems.EventSystem.current;
    public CardDatabase cardDatabase => GameManager.Instance != null ? GameManager.Instance.cardDatabase : null;

    void Awake()
    {
        Instance = this;
        InitializeLuaEngine();
    }

    private void InitializeLuaEngine()
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
        luaEngine.Globals["Card"] = typeof(LuaCard);
        luaEngine.Globals["Group"] = typeof(LuaGroup);
        luaEngine.Globals["Fusion"] = typeof(Fusion);        // FASE 27: Fusion summons
        luaEngine.Globals["Synchro"] = typeof(Synchro);      // FASE 27: Synchro summons
        luaEngine.Globals["Spirit"] = typeof(Spirit);        // FASE 27: Spirit summons (cDM0113)
        luaEngine.Globals["Ritual"] = typeof(Ritual);        // FASE 27: Ritual summons
        luaEngine.Globals["Xyz"] = typeof(Xyz);              // FASE 27: XYZ summons

        // 4. Funções Base OCGCore que os scripts chamam o tempo todo
        luaEngine.Globals["GetID"] = (System.Func<DynValue>)(() => DynValue.NewTuple(luaEngine.Globals.Get("self_table"), luaEngine.Globals.Get("self_code")));
        
        // Tabela Auxiliar Básica
        DynValue auxTable = DynValue.NewTable(luaEngine);
        // Aceitar object para prevenir falhas se um userdata vazar como ID
        auxTable.Table.Set("Stringid", DynValue.FromObject(luaEngine, (System.Func<object, object, string>)((code, id) => $"{code}_{id}")));
        
        // O GlobalCheck garante que efeitos passivos no fundo sejam executados!
        auxTable.Table.Set("GlobalCheck", DynValue.FromObject(luaEngine, (System.Action<object, Closure>)((s, func) => { func?.Call(); })));
        
        // Constantes lógicas (Criadas como closures nativas para evitar erros de casting inter-linguagem)
        auxTable.Table.Set("TRUE", luaEngine.DoString("return function(...) return true end"));
        auxTable.Table.Set("FALSE", luaEngine.DoString("return function(...) return false end"));

        // FASE 27: Adicionar funções auxiliares que retornam nil
        // FilterBoolFunction: Cria um filtro baseado em uma função booleana
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
        
        // AddEquipProcedure: Registra procedimento de equipar
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
        
        // AddRitualProcedure: Registra procedimento de ritual  
        auxTable.Table.Set("AddRitualProcedure", DynValue.FromObject(luaEngine,
            (System.Action<object, object, object, object, object>)
            ((card, filter, ritual_level, ritual_attr, operation) => {
                Debug.Log("[Lua] aux.AddRitualProcedure called - ritual procedure registered");
            })));
        
        // Next: Iterator para grupos de cartas (aux.Next(group))
        auxTable.Table.Set("Next", DynValue.FromObject(luaEngine,
            (System.Func<object, object>)(group => {
                // Retorna um iterador que percorre as cartas do grupo
                string luaCode = @"
                    return function()
                        return nil  -- Stub: group iteration handled by Lua scripts
                    end
                ";
                return luaEngine.DoString(luaCode);
            })));
        
        // NecroValleyFilter: Filtro comum usado por cartas de cemitério (ex: Gravekeeper's)
        auxTable.Table.Set("NecroValleyFilter", DynValue.FromObject(luaEngine,
            (System.Func<object, object>)(filterFunc => {
                string luaCode = @"
                    return function(c)
                        return true  -- Stub: Passa no filtro de Necrovalley por padrão
                    end
                ";
                return luaEngine.DoString(luaCode);
            })));

        // RemainFieldCost: Função de custo comum para manter a carta (Mágica/Armadilha) no campo
        auxTable.Table.Set("RemainFieldCost", luaEngine.DoString(@"
            return function(e, tp, eg, ep, ev, re, r, rp, chk)
                if chk == 0 then return true end
                -- Stub: Lógica de manter a carta no campo
                return true
            end
        "));

        // Cacheia a closure verdadeira para uso nos Dummies
        dummyClosureTrue = auxTable.Table.Get("TRUE").Function;

        luaEngine.Globals["aux"] = auxTable;

        // Tabelas de Procedimentos Oficiais (EDOPro Lua Updates)
        string[] procTables = { "Spirit", "Toon", "Union", "Gemini", "Pendulum", "Link", "Xyz", "Synchro", "Fusion", "Ritual" };
        foreach (var pName in procTables)
        {
            luaEngine.Globals[pName] = DynValue.NewTable(luaEngine);
        }

        // 5. Injeção de Constantes Vitais (Evita os erros de Bitwise do Lua 5.2)
        InjectVitalConstants();

        Debug.Log("[MoonSharp] Motor LUA Inicializado e pronto para interpretar OCGCore!");
    }

    private void InjectVitalConstants()
    {
        // Para começar a testar feitiços e armadilhas (Dano, Cura, Destruição),
        // injetamos as constantes diretamente no ambiente Lua.
        luaEngine.Globals["LOCATION_DECK"] = 0x1;
        luaEngine.Globals["LOCATION_HAND"] = 0x2;
        luaEngine.Globals["LOCATION_MZONE"] = 0x4;
        luaEngine.Globals["LOCATION_SZONE"] = 0x8;
        luaEngine.Globals["LOCATION_GRAVE"] = 0x10;
        luaEngine.Globals["LOCATION_REMOVED"] = 0x20;
        luaEngine.Globals["LOCATION_EXTRA"] = 0x40;
        luaEngine.Globals["LOCATION_OVERLAY"] = 0x80;
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8; // MZONE | SZONE
        luaEngine.Globals["LOCATION_PUBLIC"] = 0x4 | 0x8 | 0x10 | 0x20;  // ONFIELD | GRAVE | REMOVED
        luaEngine.Globals["LOCATION_ALL"] = 0x3ff;
        
        // Symbolic locations for advanced zone targeting (FASE 23)
        luaEngine.Globals["LOCATION_FZONE"] = 0x100;     // Field zone
        luaEngine.Globals["LOCATION_PZONE"] = 0x200;     // Pendulum zones
        luaEngine.Globals["LOCATION_STZONE"] = 0x400;    // Skill trap zone
        luaEngine.Globals["LOCATION_MMZONE"] = 0x800;    // Main monster zones
        luaEngine.Globals["LOCATION_EMZONE"] = 0x1000;   // Extra monster zones
        
        // Special deck locations for redirects
        luaEngine.Globals["LOCATION_DECKBOT"] = 0x10001;     // Bottom of deck
        luaEngine.Globals["LOCATION_DECKSHF"] = 0x20001;     // Shuffled into deck
        
        // ===== COMPOSITE LOCATION CONSTANTS (FASE 23) =====
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8;  // MZONE | SZONE
        luaEngine.Globals["LOCATION_PUBLIC"] = (0x4 | 0x8 | 0x10 | 0x20);  // MZONE | SZONE | GRAVE | REMOVED
        luaEngine.Globals["LOCATION_ALL"] = 0x3ff;  // All locations

        // ===== LOCATION REASON CONSTANTS (FASE 23) =====
        luaEngine.Globals["LOCATION_REASON_TOFIELD"] = 0x1;  // Duel.GetLocationCount()
        luaEngine.Globals["LOCATION_REASON_CONTROL"] = 0x2;  // Card.IsControlerCanBeChanged()
        luaEngine.Globals["LOCATION_REASON_COUNT"] = 0x4;    // Duel.GetLocationCount() for disablefield check
        luaEngine.Globals["LOCATION_REASON_RETURN"] = 0x8;
        
        // Tipos de Cartas (TYPE_)
        luaEngine.Globals["TYPE_MONSTER"] = 0x1;
        luaEngine.Globals["TYPE_SPELL"] = 0x2;
        luaEngine.Globals["TYPE_TRAP"] = 0x4;
        luaEngine.Globals["TYPE_NORMAL"] = 0x10;
        luaEngine.Globals["TYPE_EFFECT"] = 0x20;
        luaEngine.Globals["TYPE_FUSION"] = 0x40;
        luaEngine.Globals["TYPE_RITUAL"] = 0x80;
        luaEngine.Globals["TYPE_TRAPMONSTER"] = 0x100;
        luaEngine.Globals["TYPE_SPIRIT"] = 0x200;
        luaEngine.Globals["TYPE_UNION"] = 0x400;
        luaEngine.Globals["TYPE_GEMINI"] = 0x800;
        luaEngine.Globals["TYPE_TUNER"] = 0x1000;
        luaEngine.Globals["TYPE_SYNCHRO"] = 0x2000;
        luaEngine.Globals["TYPE_TOKEN"] = 0x4000;
        luaEngine.Globals["TYPE_QUICKPLAY"] = 0x10000;
        luaEngine.Globals["TYPE_CONTINUOUS"] = 0x20000;
        luaEngine.Globals["TYPE_EQUIP"] = 0x40000;
        luaEngine.Globals["TYPE_FIELD"] = 0x80000;
        luaEngine.Globals["TYPE_COUNTER"] = 0x100000;
        luaEngine.Globals["TYPE_FLIP"] = 0x200000;
        luaEngine.Globals["TYPE_TOON"] = 0x400000;
        luaEngine.Globals["TYPE_XYZ"] = 0x800000;
        luaEngine.Globals["TYPE_PENDULUM"] = 0x1000000;
        
        // ===== ADDITIONAL TYPE CONSTANTS (FASE 23) =====
        luaEngine.Globals["TYPE_SPSUMMON"] = 0x2000000;
        luaEngine.Globals["TYPE_LINK"] = 0x4000000;
        luaEngine.Globals["TYPE_SKILL"] = 0x8000000;
        luaEngine.Globals["TYPE_ACTION"] = 0x10000000;
        luaEngine.Globals["TYPE_PLUS"] = 0x20000000;
        luaEngine.Globals["TYPE_MINUS"] = 0x40000000;
        luaEngine.Globals["TYPE_ARMOR"] = 0x80000000;
        
        // ===== COMPOSITE TYPE CONSTANTS (FASE 23) =====
        luaEngine.Globals["TYPE_MAXIMUM"] = 0x8000;
        luaEngine.Globals["TYPE_EXTRA"] = 0x2000 | 0x2000000 | 0x800000 | 0x4000000;  // SYNCHRO | XYZ | LINK
        luaEngine.Globals["TYPE_PLUSMINUS"] = 0x20000000 | 0x40000000;  // PLUS | MINUS
        luaEngine.Globals["TYPES_TOKEN"] = 0x1 | 0x10 | 0x4000;  // MONSTER | NORMAL | TOKEN

        // ===== ZONE CONSTANTS (FASE 23) =====
        luaEngine.Globals["ZONE_CENTER_MMZ"] = 0x4;
        luaEngine.Globals["ZONES_MMZ"] = 0x1f;
        luaEngine.Globals["ZONES_EMZ"] = 0x60;

        // ===== SEQUENCE CONSTANTS (FASE 23) =====
        luaEngine.Globals["SEQ_DECKTOP"] = 0;
        luaEngine.Globals["SEQ_DECKBOTTOM"] = 1;
        luaEngine.Globals["SEQ_DECKSHUFFLE"] = 2;

        // ===== COIN/DICE CONSTANTS (FASE 23) =====
        luaEngine.Globals["COIN_HEADS"] = 1;
        luaEngine.Globals["COIN_TAILS"] = 0;

        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x01;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x02;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x04;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x08;
        luaEngine.Globals["ATTRIBUTE_DIVINE"] = 0x40;
        luaEngine.Globals["ATTRIBUTE_ALL"] = 0x7f;  // All 7 attributes combined

        // ===== STATUS CONSTANTS (FASE 23) =====
        luaEngine.Globals["STATUS_DISABLED"] = 0x1;
        luaEngine.Globals["STATUS_FORBIDDEN"] = 0x2;
        luaEngine.Globals["STATUS_LIMITAVAILABLE"] = 0x4;
        luaEngine.Globals["STATUS_LEGEND"] = 0x8;
        luaEngine.Globals["STATUS_OCG_RELEASED"] = 0x10;
        luaEngine.Globals["STATUS_TCG_RELEASED"] = 0x20;

        // ===== EXTENDED STATUS CONSTANTS (FASE 23) =====
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
        luaEngine.Globals["STATUS_SUMMON_TURN"] = 0x800;
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
        luaEngine.Globals["STATUS_ACTIVATED"] = 0x800000;  // deprecated
        luaEngine.Globals["STATUS_JUST_POS"] = 0x1000000;
        luaEngine.Globals["STATUS_CONTINUOUS_POS"] = 0x2000000;
        luaEngine.Globals["STATUS_FORBIDDEN"] = 0x4000000;
        luaEngine.Globals["STATUS_ACT_FROM_HAND"] = 0x8000000;
        luaEngine.Globals["STATUS_OPPO_BATTLE"] = 0x10000000;
        luaEngine.Globals["STATUS_FLIP_SUMMON_TURN"] = 0x20000000;
        luaEngine.Globals["STATUS_SPSUMMON_TURN"] = 0x40000000;

        // ===== RACE CONSTANTS (FASE 20) =====
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
        luaEngine.Globals["RACE_SYNCHRO"] = 0x100000;
        luaEngine.Globals["RACE_TUNER"] = 0x200000;
        luaEngine.Globals["RACE_XYZ"] = 0x400000;
        
        // ===== MISSING RACE CONSTANTS (FASE 23) - From constant.lua =====
        luaEngine.Globals["RACE_PSYCHIC"] = 0x100000;  // Same as SYNCHRO in official
        luaEngine.Globals["RACE_DIVINE"] = 0x200000;  // Same as TUNER in official
        luaEngine.Globals["RACE_CREATORGOD"] = 0x400000;  // Same as XYZ in official
        luaEngine.Globals["RACE_WYRM"] = 0x800000;
        luaEngine.Globals["RACE_CYBERSE"] = 0x1000000;
        luaEngine.Globals["RACE_ILLUSION"] = 0x2000000;
        
        // ===== ADDITIONAL RACE CONSTANTS (FASE 23) =====
        luaEngine.Globals["RACE_CYBORG"] = 0x4000000;
        luaEngine.Globals["RACE_MAGICALKNIGHT"] = 0x8000000;
        luaEngine.Globals["RACE_HIGHDRAGON"] = 0x10000000;
        luaEngine.Globals["RACE_OMEGAPSYCHIC"] = 0x20000000;
        luaEngine.Globals["RACE_CELESTIALWARRIOR"] = 0x40000000;
        luaEngine.Globals["RACE_GALAXY"] = 0x80000000;
        luaEngine.Globals["RACE_YOKAI"] = 0x4000000000000000;
        
        // ===== COMPOSITE RACE CONSTANT (FASE 23) =====
        luaEngine.Globals["RACE_ALL"] = 0x3ffffff;  // All races from WARRIOR to ILLUSION
        luaEngine.Globals["RACES_BEAST_BWARRIOR_WINGB"] = 0x4000 | 0x8000 | 0x200;  // BEAST | BEASTWARRIOR | WINGEDBEAST
        
        luaEngine.Globals["POS_FACEUP"] = 0x1 | 0x4;
        luaEngine.Globals["POS_FACEUP_ATTACK"] = 0x1;
        luaEngine.Globals["POS_FACEDOWN_DEFENSE"] = 0x8;
        luaEngine.Globals["POS_FACEUP_DEFENSE"] = 0x4;
        luaEngine.Globals["POS_DEFENSE"] = 0x4 | 0x8;
        
        // ===== ADDITIONAL POSITION CONSTANTS (FASE 23) =====
        luaEngine.Globals["POS_FACEDOWN_ATTACK"] = 0x2;
        luaEngine.Globals["POS_FACEDOWN"] = 0xa;
        luaEngine.Globals["POS_ATTACK"] = 0x3;
        luaEngine.Globals["NO_FLIP_EFFECT"] = 0x10000;

        // ===== OPERATION CONSTANTS (FASE 23) =====
        luaEngine.Globals["OPERATION_OVERLAY"] = 0x1;
        luaEngine.Globals["OPERATION_PEND"] = 0x2;
        luaEngine.Globals["OPERATION_LINK"] = 0x4;

        // ===== SUMMON TYPE CONSTANTS (FASE 23) =====
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

        // ===== BATTLE RESULT CONSTANTS (FASE 23) =====
        luaEngine.Globals["BATTLE_RESULT_WIN"] = 0x1;
        luaEngine.Globals["BATTLE_RESULT_LOSE"] = 0x2;
        luaEngine.Globals["BATTLE_RESULT_DRAW"] = 0x3;

        // ===== DECLARE CONSTANTS (FASE 23) =====
        luaEngine.Globals["DECLARE_ATTRIB"] = 0x1;
        luaEngine.Globals["DECLARE_RACE"] = 0x2;
        luaEngine.Globals["DECLARE_LEVEL"] = 0x3;
        luaEngine.Globals["DECLARE_DAMAGE"] = 0x4;
        luaEngine.Globals["DECLARE_COIN"] = 0x5;
        luaEngine.Globals["DECLARE_EFFECT"] = 0x6;

        // ===== TARGET CONSTANTS (FASE 23) =====
        luaEngine.Globals["TARGET_SEL_SINGLE"] = 0x1;
        luaEngine.Globals["TARGET_SEL_RANGE"] = 0x2;
        luaEngine.Globals["TARGET_SEL_MONSTER"] = 0x3;
        luaEngine.Globals["TARGET_SEL_SPELL_TRAP"] = 0x4;
        luaEngine.Globals["TARGET_SEL_ANY"] = 0x5;

        // ===== VALUE CONSTANTS (FASE 23) =====
        luaEngine.Globals["VALUE_TYPE_ACTIVATION"] = 0x1;
        luaEngine.Globals["VALUE_TYPE_TRIGGER"] = 0x2;
        luaEngine.Globals["VALUE_TYPE_QUICK"] = 0x3;
        luaEngine.Globals["VALUE_TYPE_QUICK_O"] = 0x4;
        
        luaEngine.Globals["REASON_EFFECT"] = 0x40;
        luaEngine.Globals["REASON_COST"] = 0x80;  // FASE 23 FIX: Was 0x2, corrected to 0x80
        luaEngine.Globals["REASON_BATTLE"] = 0x20;  // FASE 23 FIX: Was 0x10, corrected to 0x20
        luaEngine.Globals["REASON_DISCARD"] = 0x4000;
        
        // ===== ALL MISSING REASON CONSTANTS (FASE 23) =====
        luaEngine.Globals["REASON_DESTROY"] = 0x1;
        luaEngine.Globals["REASON_RELEASE"] = 0x2;
        luaEngine.Globals["REASON_TEMPORARY"] = 0x4;
        luaEngine.Globals["REASON_MATERIAL"] = 0x8;
        luaEngine.Globals["REASON_SUMMON"] = 0x10;
        luaEngine.Globals["REASON_ADJUST"] = 0x100;
        luaEngine.Globals["REASON_LOST_TARGET"] = 0x200;
        luaEngine.Globals["REASON_RULE"] = 0x400;
        luaEngine.Globals["REASON_SPSUMMON"] = 0x800;
        luaEngine.Globals["REASON_DISSUMMON"] = 0x1000;
        luaEngine.Globals["REASON_FLIP"] = 0x2000;
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

        luaEngine.Globals["EVENT_FREE_CHAIN"] = 1002;

        // ===== CATEGORY CONSTANTS (FASE 20) - Extended & FASE 23 Complete =====
        luaEngine.Globals["CATEGORY_DRAW"] = 0x1;
        luaEngine.Globals["CATEGORY_RELEASE"] = 0x2;
        luaEngine.Globals["CATEGORY_REMOVE"] = 0x4;
        luaEngine.Globals["CATEGORY_TOHAND"] = 0x4;
        luaEngine.Globals["CATEGORY_TODECK"] = 0x8;
        luaEngine.Globals["CATEGORY_TOGRAVE"] = 0x20;
        luaEngine.Globals["CATEGORY_DECKDES"] = 0x40;
        luaEngine.Globals["CATEGORY_SPECIAL_SUMMON"] = 0x10;
        luaEngine.Globals["CATEGORY_NEGATE"] = 0x80;
        luaEngine.Globals["CATEGORY_SEARCH"] = 0x100;
        luaEngine.Globals["CATEGORY_SUMMON"] = 0x100;
        luaEngine.Globals["CATEGORY_POSITION"] = 0x200;
        luaEngine.Globals["CATEGORY_SET"] = 0x400;
        luaEngine.Globals["CATEGORY_TOKEN"] = 0x400;
        luaEngine.Globals["CATEGORY_ATKCHANGE"] = 0x800;
        luaEngine.Globals["CATEGORY_FLIP"] = 0x800;
        luaEngine.Globals["CATEGORY_DAMAGE"] = 0x80000;
        luaEngine.Globals["CATEGORY_RECOVER"] = 0x100000;
        luaEngine.Globals["CATEGORY_DESTROY"] = 0x20000;
        luaEngine.Globals["CATEGORY_EQUIP"] = 0x40000;
        luaEngine.Globals["CATEGORY_CONTROL"] = 0x2000;
        luaEngine.Globals["CATEGORY_DISABLE"] = 0x4000;
        luaEngine.Globals["CATEGORY_DISABLE_SUMMON"] = 0x8000;
        luaEngine.Globals["CATEGORY_DEFCHANGE"] = 0x400000;
        luaEngine.Globals["CATEGORY_COUNTER"] = 0x800000;
        luaEngine.Globals["CATEGORY_COIN"] = 0x1000000;
        luaEngine.Globals["CATEGORY_DICE"] = 0x2000000;
        luaEngine.Globals["CATEGORY_LEAVE_GRAVE"] = 0x4000000;
        luaEngine.Globals["CATEGORY_LVCHANGE"] = 0x8000000;
        luaEngine.Globals["CATEGORY_ANNOUNCE"] = 0x20000000;
        luaEngine.Globals["CATEGORY_FUSION_SUMMON"] = 0x40000000;
        luaEngine.Globals["CATEGORY_TOEXTRA"] = 0x80000000;
        
        // Gatilhos e Eventos (Events OCGCore)
        luaEngine.Globals["EVENT_SUMMON_SUCCESS"] = 1100;  // FASE 23 FIX: Was 11, corrected
        luaEngine.Globals["EVENT_FLIP_SUMMON_SUCCESS"] = 1101;  // FASE 23 FIX: Was 13, corrected
        luaEngine.Globals["EVENT_SPSUMMON_SUCCESS"] = 1102;  // FASE 23 FIX: Was 14, corrected
        luaEngine.Globals["EVENT_DESTROYED"] = 1029;
        luaEngine.Globals["EVENT_DESTROY"] = 1029;
        luaEngine.Globals["EVENT_PHASE"] = 0x1000;
        luaEngine.Globals["EVENT_PHASE_START"] = 8192;  // FASE 23 FIX: Was 4097, should be 0x2000 = 8192
        luaEngine.Globals["EVENT_CHANGE_POS"] = 1016;
        luaEngine.Globals["EVENT_LEAVE_FIELD"] = 1015;
        luaEngine.Globals["EVENT_CHAINING"] = 1027;  // FASE 23 FIX: Was 8000000
        luaEngine.Globals["EVENT_TO_GRAVE"] = 1014;  // FASE 23 FIX: Was 10000000
        luaEngine.Globals["EVENT_BATTLE_DESTROYING"] = 1139;  // FASE 23 FIX: Was 40000000
        
        // ===== COMPLETE EVENT CONSTANTS (FASE 24) =====
        luaEngine.Globals["EVENT_STARTUP"] = 1000;
        luaEngine.Globals["EVENT_REMOVE"] = 1011;
        luaEngine.Globals["EVENT_TO_HAND"] = 1012;
        luaEngine.Globals["EVENT_TO_DECK"] = 1013;
        luaEngine.Globals["EVENT_LEAVE_FIELD_P"] = 1019;
        luaEngine.Globals["EVENT_CHAIN_SOLVING"] = 1020;
        luaEngine.Globals["EVENT_CHAIN_ACTIVATING"] = 1021;
        luaEngine.Globals["EVENT_CHAIN_SOLVED"] = 1022;
        luaEngine.Globals["EVENT_CHAIN_NEGATED"] = 1024;

        // Tipos de Efeitos (EFFECT_TYPE)
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
        
        // ===== COMPLETE EFFECT_TYPE CONSTANTS (FASE 24) =====
        luaEngine.Globals["EFFECT_TYPE_XMATERIAL"] = 0x1000;
        luaEngine.Globals["EFFECT_TYPE_GRANT"] = 0x2000;
        luaEngine.Globals["EFFECT_TYPE_TARGET"] = 0x4000;

        // ===== ALL EFFECT FLAG CONSTANTS (FASE 24) =====
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
        luaEngine.Globals["EFFECT_FLAG_IMMEDIATELY_APPLY"] = 0x80000000;
        luaEngine.Globals["EFFECT_FLAG2_CONTINUOUS_EQUIP"] = 0x0001;
        luaEngine.Globals["EFFECT_FLAG2_COF"] = 0x0002;
        luaEngine.Globals["EFFECT_FLAG2_CHECK_SIMULTANEOUS"] = 0x0004;
        luaEngine.Globals["EFFECT_FLAG2_FORCE_ACTIVATE_LOCATION"] = 0x40000000;
        luaEngine.Globals["EFFECT_FLAG2_MAJESTIC_MUST_COPY"] = 0x80000000;

        // ===== CRITICAL EFFECT CODES (FASE 24 - Part 1: 60 most common) =====
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
        
        // ===== EFFECT UPGRADE & STAT CONSTANTS (FASE 24 - Part 2) =====
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
        
        // ===== MORE CRITICAL EFFECT CODES (FASE 24 - Part 3: Tribute & Material) =====
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
        
        // ===== BATTLE & TURN SKIP EFFECTS (FASE 24 - Part 4) =====
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
        
        // ===== COIN & DICE & MATERIAL EFFECTS (FASE 24 - Part 5) =====
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
        
        // ===== FIELD & ZONE EFFECTS (FASE 24 - Part 6) =====
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
        
        // ===== ADVANCED SYNCHRO & LINK EFFECTS (FASE 24 - Part 7) =====
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
        
        // ===== SPECIAL CARD EFFECTS (FASE 24 - Part 8) =====
        luaEngine.Globals["EFFECT_FUSION_MAT_RESTRICTION"] = 73941492 + 0x40;  // TYPE_FUSION
        luaEngine.Globals["EFFECT_SYNCHRO_MAT_RESTRICTION"] = 73941492 + 0x2000;  // TYPE_SYNCHRO
        luaEngine.Globals["EFFECT_XYZ_MAT_RESTRICTION"] = 73941492 + 0x800000;  // TYPE_XYZ
        luaEngine.Globals["EFFECT_SYNCHRO_MAT_FROM_HAND"] = 97682931;
        luaEngine.Globals["EFFECT_XYZ_MAT_FROM_GRAVE"] = 511002793;
        luaEngine.Globals["EFFECT_SPELL_XYZ_MAT"] = 511000189;
        luaEngine.Globals["EFFECT_EQUIP_SPELL_XYZ_MAT"] = 511001175;
        luaEngine.Globals["EFFECT_ORICHALCUM_CHAIN"] = 511002116;
        luaEngine.Globals["EFFECT_DOUBLE_XYZ_MATERIAL"] = 511001225;
        luaEngine.Globals["EFFECT_SATELLARKNIGHT_CAPELLA"] = 86466163;
        luaEngine.Globals["EFFECT_STAR_SERAPH_SOVEREIGNTY"] = 91110378;
        
        // ===== EVENT CONSTANTS - COMPLETE LIST (FASE 24) =====
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
        luaEngine.Globals["EVENT_CHAIN_END"] = 1023;
        luaEngine.Globals["EVENT_CHAIN_NEGATED"] = 1024;
        luaEngine.Globals["EVENT_DAMAGE"] = 1025;
        luaEngine.Globals["EVENT_RECOVER"] = 1026;
        luaEngine.Globals["EVENT_DRAW"] = 1027;
        luaEngine.Globals["EVENT_SUMMON"] = 1028;
        luaEngine.Globals["EVENT_SPSUMMON"] = 1029;
        luaEngine.Globals["EVENT_FLIPSUMMON"] = 1030;
        luaEngine.Globals["EVENT_MSET"] = 1031;
        luaEngine.Globals["EVENT_SSET"] = 1032;
        luaEngine.Globals["EVENT_EQUIP"] = 1033;
        luaEngine.Globals["EVENT_UNEQUIP"] = 1034;
        luaEngine.Globals["EVENT_CUSTOM"] = 1100;
        
        // Battle Event constants added (FASE 24)
        luaEngine.Globals["EVENT_BE_MATERIAL"] = 1100;
        luaEngine.Globals["EVENT_CONFIRM_DECKTOP"] = 1101;
        luaEngine.Globals["EVENT_ATTACK_ANNOUNCE"] = 1102;
        luaEngine.Globals["EVENT_ATTACK_DISABLED"] = 1103;
        luaEngine.Globals["EVENT_SUMMON_SUCCESS"] = 1104;
        luaEngine.Globals["EVENT_SPSUMMON_SUCCESS"] = 1105;
        luaEngine.Globals["EVENT_FLIPSUMMON_SUCCESS"] = 1106;
        luaEngine.Globals["EVENT_MSET_SUCCESS"] = 1107;
        luaEngine.Globals["EVENT_SSET_SUCCESS"] = 1108;
        
        // ===== HINTMSG CONSTANTS - UI MESSAGE IDs (FASE 24) =====
        luaEngine.Globals["HINTMSG_SELECT"] = 500;
        luaEngine.Globals["HINTMSG_DISCARD"] = 501;
        luaEngine.Globals["HINTMSG_CONFIRM"] = 502;
        luaEngine.Globals["HINTMSG_ACTIVATE"] = 503;
        luaEngine.Globals["HINTMSG_RELEASE"] = 504;
        luaEngine.Globals["HINTMSG_EXCAVATE"] = 505;
        luaEngine.Globals["HINTMSG_DESTROY"] = 506;
        luaEngine.Globals["HINTMSG_TOGRAVE"] = 507;
        luaEngine.Globals["HINTMSG_REMOVE"] = 508;
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;
        luaEngine.Globals["HINTMSG_RTOHAND"] = 510;
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 513;
        luaEngine.Globals["HINTMSG_TARGET"] = 514;
        luaEngine.Globals["HINTMSG_EQUIP"] = 515;
        luaEngine.Globals["HINTMSG_SHUFFLE"] = 520;
        luaEngine.Globals["HINTMSG_BANISH"] = 521;
        luaEngine.Globals["CHAININFO_TARGET_PLAYER"] = 1;
        luaEngine.Globals["CHAININFO_TARGET_PARAM"] = 2;

        // ===== EXTENDED CHAININFO CONSTANTS (FASE 23) =====
        luaEngine.Globals["CHAININFO_TRIGGERING_EFFECT"] = 1;
        luaEngine.Globals["CHAININFO_TRIGGERING_PLAYER"] = 2;
        luaEngine.Globals["CHAININFO_TRIGGERING_CONTROLER"] = 3;
        luaEngine.Globals["CHAININFO_TRIGGERING_LOCATION"] = 4;
        luaEngine.Globals["CHAININFO_TRIGGERING_LOCATION_SYMBOLIC"] = 5;
        luaEngine.Globals["CHAININFO_TRIGGERING_SEQUENCE"] = 6;
        luaEngine.Globals["CHAININFO_TRIGGERING_SEQUENCE_SYMBOLIC"] = 7;
        luaEngine.Globals["CHAININFO_TARGET_CARDS"] = 8;
        luaEngine.Globals["CHAININFO_TARGET_PLAYER_RENAMED"] = 9;  // Old number for deprecated API
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

        // ===== PHASE CONSTANTS (FASE 20) =====
        // ===== PHASE CONSTANTS (FASE 23 - Complete List) =====
        luaEngine.Globals["PHASE_DRAW"] = 0x1;
        luaEngine.Globals["PHASE_STANDBY"] = 0x2;
        luaEngine.Globals["PHASE_MAIN1"] = 0x4;
        luaEngine.Globals["PHASE_BATTLE_START"] = 0x8;
        luaEngine.Globals["PHASE_BATTLE_STEP"] = 0x10;
        luaEngine.Globals["PHASE_DAMAGE"] = 0x20;
        luaEngine.Globals["PHASE_DAMAGE_CAL"] = 0x40;
        luaEngine.Globals["PHASE_BATTLE"] = 0x80;
        luaEngine.Globals["PHASE_MAIN2"] = 0x100;
        luaEngine.Globals["PHASE_END"] = 0x200;

        // ===== RESET CONSTANTS (FASE 20) =====
        luaEngine.Globals["RESET_EVENT"] = 0x1000;
        luaEngine.Globals["RESET_PHASE"] = 0x40000000;
        luaEngine.Globals["RESET_STANDARD"] = 0x60000000;
        luaEngine.Globals["RESET_END_PHASE"] = 0x40000000;
        luaEngine.Globals["RESETS_STANDARD_EXC_GRAVE"] = 0x6FF00FF;
        
        // ===== ALL MISSING RESET CONSTANTS (FASE 23) =====
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
        luaEngine.Globals["RESET_CHAIN"] = 0x80000000;
        
        // Composite resets
        luaEngine.Globals["RESETS_STANDARD"] = 0x60000000;
        luaEngine.Globals["RESETS_STANDARD_DISABLE"] = 0x60000000 | 0x10000;
        luaEngine.Globals["RESETS_STANDARD_PHASE_END"] = 0x1000 | 0x60000000 | 0x40000000;
        luaEngine.Globals["RESETS_STANDARD_DISABLE_PHASE_END"] = 0x1000 | 0x60000000 | 0x40000000 | 0x10000;
        
        // ===== ADDITIONAL COMPOSITE RESETS (FASE 23) =====
        luaEngine.Globals["RESETS_STANDARD"] = 0x40000 | 0x80000 | 0x100000 | 0x200000 | 0x400000 | 0x800000 | 0x20000;
        luaEngine.Globals["RESETS_STANDARD_DISABLE"] = 0x40000 | 0x80000 | 0x100000 | 0x200000 | 0x400000 | 0x800000 | 0x20000 | 0x10000;
        luaEngine.Globals["RESETS_REDIRECT"] = ((0x40000 | 0x80000 | 0x100000 | 0x200000 | 0x400000 | 0x800000 | 0x20000 | 0x4000000 | 0x8000000) & ~(0x1000000 | 0x800000));  // Complex formula from constant.lua
        luaEngine.Globals["RESETS_CANNOT_ACT"] = ((0x40000 | 0x80000 | 0x100000 | 0x200000 | 0x400000 | 0x800000 | 0x20000) & ~0x800000);  // Complex formula from constant.lua

        // ===== ASSUME CONSTANTS (FASE 23) =====
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

        // ===== LINK MARKER CONSTANTS (FASE 23) =====
        luaEngine.Globals["LINK_MARKER_BOTTOM_LEFT"] = 0x1;
        luaEngine.Globals["LINK_MARKER_BOTTOM"] = 0x2;
        luaEngine.Globals["LINK_MARKER_BOTTOM_RIGHT"] = 0x4;
        luaEngine.Globals["LINK_MARKER_LEFT"] = 0x8;
        luaEngine.Globals["LINK_MARKER_RIGHT"] = 0x20;
        luaEngine.Globals["LINK_MARKER_TOP_LEFT"] = 0x40;
        luaEngine.Globals["LINK_MARKER_TOP"] = 0x80;
        luaEngine.Globals["LINK_MARKER_TOP_RIGHT"] = 0x100;

        // ===== PLAYER CONSTANTS (FASE 23) =====
        luaEngine.Globals["PLAYER_NONE"] = 2;
        luaEngine.Globals["PLAYER_ALL"] = 3;
        luaEngine.Globals["PLAYER_EITHER"] = 4;  // Not defined by the core
        luaEngine.Globals["PLAYER_SELFDES"] = 5;

        luaEngine.Globals["HINT_SELECTMSG"] = 1;
        
        // ===== HINT MESSAGE CONSTANTS (FASE 24) =====
        luaEngine.Globals["HINTMSG_DESTROY"] = 506;
        luaEngine.Globals["HINTMSG_TOGRAVE"] = 507;
        luaEngine.Globals["HINTMSG_REMOVE"] = 508;
        luaEngine.Globals["HINTMSG_SPSUMMON"] = 509;
        luaEngine.Globals["HINTMSG_RTOHAND"] = 510;
        luaEngine.Globals["HINTMSG_POSCHANGE"] = 513;
        luaEngine.Globals["HINTMSG_TARGET"] = 514;
        luaEngine.Globals["HINTMSG_EQUIP"] = 515;
        luaEngine.Globals["HINTMSG_SHUFFLE"] = 520;
        luaEngine.Globals["HINTMSG_BANISH"] = 521;

        // ===== SUMMON TYPE CONSTANTS (FASE 24) =====
        luaEngine.Globals["SUMMON_TYPE_NORMAL"] = 0x1;
        luaEngine.Globals["SUMMON_TYPE_FLIP"] = 0x2;
        luaEngine.Globals["SUMMON_TYPE_SPECIAL"] = 0x4;
        luaEngine.Globals["SUMMON_TYPE_FUSION"] = 0x8;
        luaEngine.Globals["SUMMON_TYPE_SYNCHRO"] = 0x10;
        luaEngine.Globals["SUMMON_TYPE_XYZ"] = 0x20;
        luaEngine.Globals["SUMMON_TYPE_PENDULUM"] = 0x40;
        luaEngine.Globals["SUMMON_TYPE_LINK"] = 0x80;

        // ===== PLAYERINFO CONSTANTS (FASE 24) =====
        luaEngine.Globals["PLAYERINFO_LPCALC"] = 1;
        luaEngine.Globals["PLAYERINFO_FIELD"] = 2;
        luaEngine.Globals["PLAYERINFO_MONSTER"] = 3;
        luaEngine.Globals["PLAYERINFO_SPELL"] = 4;
        luaEngine.Globals["PLAYERINFO_HAND"] = 5;
        luaEngine.Globals["PLAYERINFO_DECK"] = 6;
        luaEngine.Globals["PLAYERINFO_GRAVE"] = 7;
        luaEngine.Globals["PLAYERINFO_REMOVE"] = 8;
        luaEngine.Globals["PLAYERINFO_EXTRA"] = 9;
        luaEngine.Globals["PLAYERINFO_SZONE"] = 10;
        luaEngine.Globals["PLAYERINFO_MZONE"] = 11;

        // ===== LOCATION CONSTANTS (FASE 24) =====
        luaEngine.Globals["LOCATION_DECK"] = 0x1;
        luaEngine.Globals["LOCATION_HAND"] = 0x2;
        luaEngine.Globals["LOCATION_MZONE"] = 0x4;
        luaEngine.Globals["LOCATION_SZONE"] = 0x8;
        luaEngine.Globals["LOCATION_GRAVE"] = 0x10;
        luaEngine.Globals["LOCATION_REMOVED"] = 0x20;
        luaEngine.Globals["LOCATION_EXTRA"] = 0x40;
        luaEngine.Globals["LOCATION_ONFIELD"] = 0x4 | 0x8;
        luaEngine.Globals["LOCATION_PZONE"] = 0x80;

        // ===== SET CONSTANTS - Archetype Identifiers (FASE 21) =====
        luaEngine.Globals["SET_AMAZONESS"] = 0x4F;         // Amazoness tribe
        luaEngine.Globals["SET_ARCHFIEND"] = 0x50;         // Archfiend monsters
        luaEngine.Globals["SET_GUARDIAN"] = 0x51;          // Guardian support
        luaEngine.Globals["SET_BATTERYMAN"] = 0x52;        // Batteryman archetype
        luaEngine.Globals["SET_HERO"] = 0x53;              // Elemental HERO
        luaEngine.Globals["SET_LIGHTSWORN"] = 0x54;        // Lightsworn
        luaEngine.Globals["SET_BLACKWING"] = 0x55;         // Blackwing
        luaEngine.Globals["SET_DRAGUNITY"] = 0x56;         // Dragunity

        // ===== TYPE CONSTANTS (FASE 24) =====
        luaEngine.Globals["TYPE_MONSTER"] = 0x1;
        luaEngine.Globals["TYPE_SPELL"] = 0x2;
        luaEngine.Globals["TYPE_TRAP"] = 0x4;
        luaEngine.Globals["TYPE_NORMAL"] = 0x10;
        luaEngine.Globals["TYPE_EFFECT"] = 0x20;
        luaEngine.Globals["TYPE_FUSION"] = 0x40;
        luaEngine.Globals["TYPE_RITUAL"] = 0x80;
        luaEngine.Globals["TYPE_TRAPMONSTER"] = 0x100;
        luaEngine.Globals["TYPE_SYNCHRO"] = 0x2000;
        luaEngine.Globals["TYPE_TUNER"] = 0x4000;
        luaEngine.Globals["TYPE_XYZ"] = 0x800000;
        luaEngine.Globals["TYPE_PENDULUM"] = 0x1000000;
        luaEngine.Globals["TYPE_LINK"] = 0x2000000;
        luaEngine.Globals["TYPE_QUICKPLAY"] = 0x40000;
        luaEngine.Globals["TYPE_CONTINUOUS"] = 0x80000;
        luaEngine.Globals["TYPE_EQUIP"] = 0x100000;
        luaEngine.Globals["TYPE_FIELD"] = 0x200000;
        luaEngine.Globals["TYPE_COUNTER"] = 0x400000;

        // ===== ATTRIBUTE CONSTANTS (FASE 24) =====
        luaEngine.Globals["ATTRIBUTE_EARTH"] = 0x1;
        luaEngine.Globals["ATTRIBUTE_WATER"] = 0x2;
        luaEngine.Globals["ATTRIBUTE_FIRE"] = 0x4;
        luaEngine.Globals["ATTRIBUTE_WIND"] = 0x8;
        luaEngine.Globals["ATTRIBUTE_LIGHT"] = 0x10;
        luaEngine.Globals["ATTRIBUTE_DARK"] = 0x20;
        luaEngine.Globals["ATTRIBUTE_DIVINE"] = 0x40;

        // ===== RACE CONSTANTS (FASE 24) =====
        luaEngine.Globals["RACE_WARRIOR"] = 0x1;
        luaEngine.Globals["RACE_SPELLCASTER"] = 0x2;
        luaEngine.Globals["RACE_FAIRY"] = 0x4;
        luaEngine.Globals["RACE_FIEND"] = 0x8;
        luaEngine.Globals["RACE_ZOMBIE"] = 0x10;
        luaEngine.Globals["RACE_MACHINE"] = 0x20;
        luaEngine.Globals["RACE_AQUA"] = 0x40;
        luaEngine.Globals["RACE_PYRO"] = 0x80;
        luaEngine.Globals["RACE_ROCK"] = 0x100;
        luaEngine.Globals["RACE_WINDBEAST"] = 0x200;
        luaEngine.Globals["RACE_PLANT"] = 0x400;
        luaEngine.Globals["RACE_INSECT"] = 0x800;
        luaEngine.Globals["RACE_THUNDER"] = 0x1000;
        luaEngine.Globals["RACE_DRAGON"] = 0x2000;
        luaEngine.Globals["RACE_BEAST"] = 0x4000;
        luaEngine.Globals["RACE_BEASTWARRIOR"] = 0x8000;
        luaEngine.Globals["RACE_DINOSAUR"] = 0x10000;
        luaEngine.Globals["RACE_FISH"] = 0x20000;
        luaEngine.Globals["RACE_SEASERPENT"] = 0x40000;
        luaEngine.Globals["RACE_REPTILE"] = 0x80000;
        luaEngine.Globals["RACE_PSYCHIC"] = 0x100000;
        luaEngine.Globals["RACE_DIVINE"] = 0x200000;
        luaEngine.Globals["RACE_WYRM"] = 0x400000;
        luaEngine.Globals["RACE_CYBERSE"] = 0x800000;
        luaEngine.Globals["RACE_ILLUSION"] = 0x1000000;

        // ===== COUNTER CONSTANTS (FASE 21 & FASE 23 Complete) =====
        luaEngine.Globals["COUNTER_SPELL"] = 1;            // Spell counter for counter cards
        luaEngine.Globals["COUNTER_WITHOUT_PERMIT"] = 0x1000;  // Counter that needs no permission
        luaEngine.Globals["COUNTER_NEED_ENABLE"] = 0x2000;     // Counter that requires enabling

        // ===== RACE CONSTANTS - MISSING (FASE 25) =====
        luaEngine.Globals["RACE_WINGEDBEAST"] = 0x200;

        // ===== EVENT CONSTANTS - EXTENDED (FASE 25) =====
        luaEngine.Globals["EVENT_CHAIN_DISABLED"] = 1110;
        luaEngine.Globals["EVENT_BECOME_TARGET"] = 1111;
        luaEngine.Globals["EVENT_MOVE"] = 1112;
        luaEngine.Globals["EVENT_LEAVE_GRAVE"] = 1113;
        luaEngine.Globals["EVENT_ADJUST"] = 1114;
        luaEngine.Globals["EVENT_BREAK_EFFECT"] = 1115;
        luaEngine.Globals["EVENT_FLIP_SUMMON"] = 1116;
        luaEngine.Globals["EVENT_BE_PRE_MATERIAL"] = 1117;
        luaEngine.Globals["EVENT_PREDRAW"] = 1118;
        luaEngine.Globals["EVENT_SUMMON_NEGATED"] = 1119;
        luaEngine.Globals["EVENT_FLIP_SUMMON_NEGATED"] = 1120;
        luaEngine.Globals["EVENT_SPSUMMON_NEGATED"] = 1121;
        luaEngine.Globals["EVENT_CONTROL_CHANGED"] = 1122;
        luaEngine.Globals["EVENT_BE_BATTLE_TARGET"] = 1123;
        luaEngine.Globals["EVENT_BATTLE_START"] = 1124;
        luaEngine.Globals["EVENT_BATTLE_CONFIRM"] = 1125;
        luaEngine.Globals["EVENT_PRE_DAMAGE_CALCULATE"] = 1126;
        luaEngine.Globals["EVENT_DAMAGE_CALCULATING"] = 1127;
        luaEngine.Globals["EVENT_PRE_BATTLE_DAMAGE"] = 1128;
        luaEngine.Globals["EVENT_BATTLE_END"] = 1129;
        luaEngine.Globals["EVENT_BATTLED"] = 1130;
        luaEngine.Globals["EVENT_BATTLE_DESTROYED"] = 1131;
        luaEngine.Globals["EVENT_DAMAGE_STEP_END"] = 1132;
        luaEngine.Globals["EVENT_BATTLE_DAMAGE"] = 1133;
        luaEngine.Globals["EVENT_TOSS_DICE"] = 1134;
        luaEngine.Globals["EVENT_TOSS_COIN"] = 1135;
        luaEngine.Globals["EVENT_TOSS_COIN_NEGATE"] = 1136;
        luaEngine.Globals["EVENT_TOSS_DICE_NEGATE"] = 1137;
        luaEngine.Globals["EVENT_LEVEL_UP"] = 1138;
        luaEngine.Globals["EVENT_PAY_LPCOST"] = 1139;
        luaEngine.Globals["EVENT_DETACH_MATERIAL"] = 1140;
        luaEngine.Globals["EVENT_TURN_END"] = 1141;
        luaEngine.Globals["EVENT_CONFIRM"] = 1142;
        luaEngine.Globals["EVENT_TOHAND_CONFIRM"] = 1143;
        luaEngine.Globals["EVENT_ADD_COUNTER"] = 1144;
        luaEngine.Globals["EVENT_REMOVE_COUNTER"] = 1145;

        // ===== CATEGORY CONSTANTS (FASE 25) =====
        luaEngine.Globals["CATEGORY_HANDES"] = 0x1000;

        // ===== HINT CONSTANTS (FASE 25) =====
        luaEngine.Globals["HINT_EVENT"] = 1;
        luaEngine.Globals["HINT_MESSAGE"] = 2;
        luaEngine.Globals["HINT_OPSELECTED"] = 3;
        luaEngine.Globals["HINT_EFFECT"] = 4;
        luaEngine.Globals["HINT_RACE"] = 5;
        luaEngine.Globals["HINT_ATTRIB"] = 6;
        luaEngine.Globals["HINT_CODE"] = 7;
        luaEngine.Globals["HINT_NUMBER"] = 8;
        luaEngine.Globals["HINT_CARD"] = 9;
        luaEngine.Globals["HINT_ZONE"] = 10;
        luaEngine.Globals["HINT_SKILL"] = 11;
        luaEngine.Globals["HINT_SKILL_COVER"] = 12;
        luaEngine.Globals["HINT_SKILL_FLIP"] = 13;
        luaEngine.Globals["HINT_SKILL_REMOVE"] = 14;

        // ===== CHAIN HINT CONSTANTS (FASE 25) =====
        luaEngine.Globals["CHINT_TURN"] = 1;
        luaEngine.Globals["CHINT_CARD"] = 2;
        luaEngine.Globals["CHINT_RACE"] = 3;
        luaEngine.Globals["CHINT_ATTRIBUTE"] = 4;
        luaEngine.Globals["CHINT_NUMBER"] = 5;
        luaEngine.Globals["CHINT_DESC_ADD"] = 6;
        luaEngine.Globals["CHINT_DESC_REMOVE"] = 7;

        // ===== PROC HINT CONSTANTS (FASE 25) =====
        luaEngine.Globals["PHINT_DESC_ADD"] = 1;
        luaEngine.Globals["PHINT_DESC_REMOVE"] = 2;

        // ===== OPCODE CONSTANTS (FASE 25) =====
        luaEngine.Globals["OPCODE_ADD"] = 1;
        luaEngine.Globals["OPCODE_SUB"] = 2;
        luaEngine.Globals["OPCODE_MUL"] = 3;
        luaEngine.Globals["OPCODE_DIV"] = 4;
        luaEngine.Globals["OPCODE_AND"] = 5;
        luaEngine.Globals["OPCODE_OR"] = 6;
        luaEngine.Globals["OPCODE_NEG"] = 7;
        luaEngine.Globals["OPCODE_NOT"] = 8;
        luaEngine.Globals["OPCODE_BAND"] = 9;
        luaEngine.Globals["OPCODE_BOR"] = 10;
        luaEngine.Globals["OPCODE_BNOT"] = 11;
        luaEngine.Globals["OPCODE_BXOR"] = 12;
        luaEngine.Globals["OPCODE_LSHIFT"] = 13;
        luaEngine.Globals["OPCODE_RSHIFT"] = 14;
        luaEngine.Globals["OPCODE_ALLOW_ALIASES"] = 0x1;
        luaEngine.Globals["OPCODE_ALLOW_TOKENS"] = 0x2;
        luaEngine.Globals["OPCODE_ISCODE"] = 1;
        luaEngine.Globals["OPCODE_ISSETCARD"] = 2;
        luaEngine.Globals["OPCODE_ISTYPE"] = 3;
        luaEngine.Globals["OPCODE_ISRACE"] = 4;
        luaEngine.Globals["OPCODE_ISATTRIBUTE"] = 5;
        luaEngine.Globals["OPCODE_GETCODE"] = 6;
        luaEngine.Globals["OPCODE_GETSETCARD"] = 7;
        luaEngine.Globals["OPCODE_GETTYPE"] = 8;
        luaEngine.Globals["OPCODE_GETRACE"] = 9;
        luaEngine.Globals["OPCODE_GETATTRIBUTE"] = 10;

        // ===== DAMAGE CONSTANTS (FASE 25) =====
        luaEngine.Globals["DOUBLE_DAMAGE"] = 2;
        luaEngine.Globals["HALF_DAMAGE"] = 0.5;

        // ===== EXTENDED HINTMSG CONSTANTS (FASE 25) =====
        luaEngine.Globals["HINTMSG_ATOHAND"] = 522;
        luaEngine.Globals["HINTMSG_TODECK"] = 523;
        luaEngine.Globals["HINTMSG_SUMMON"] = 524;
        luaEngine.Globals["HINTMSG_SET"] = 525;
        luaEngine.Globals["HINTMSG_FMATERIAL"] = 526;
        luaEngine.Globals["HINTMSG_SMATERIAL"] = 527;
        luaEngine.Globals["HINTMSG_XMATERIAL"] = 528;
        luaEngine.Globals["HINTMSG_FACEUP"] = 529;
        luaEngine.Globals["HINTMSG_FACEDOWN"] = 530;
        luaEngine.Globals["HINTMSG_ATTACK"] = 531;
        luaEngine.Globals["HINTMSG_DEFENSE"] = 532;
        luaEngine.Globals["HINTMSG_REMOVEXYZ"] = 533;
        luaEngine.Globals["HINTMSG_CONTROL"] = 534;
        luaEngine.Globals["HINTMSG_DESREPLACE"] = 535;
        luaEngine.Globals["HINTMSG_FACEUPATTACK"] = 536;
        luaEngine.Globals["HINTMSG_FACEUPDEFENSE"] = 537;
        luaEngine.Globals["HINTMSG_FACEDOWNATTACK"] = 538;
        luaEngine.Globals["HINTMSG_FACEDOWNDEFENSE"] = 539;
        luaEngine.Globals["HINTMSG_TOFIELD"] = 540;
        luaEngine.Globals["HINTMSG_SELF"] = 541;
        luaEngine.Globals["HINTMSG_OPPO"] = 542;
        luaEngine.Globals["HINTMSG_TRIBUTE"] = 543;
        luaEngine.Globals["HINTMSG_DEATTACHFROM"] = 544;
        luaEngine.Globals["HINTMSG_LMATERIAL"] = 545;
        luaEngine.Globals["HINTMSG_ATTACKTARGET"] = 546;
        luaEngine.Globals["HINTMSG_EFFECT"] = 547;
        luaEngine.Globals["HINTMSG_COIN"] = 548;
        luaEngine.Globals["HINTMSG_DICE"] = 549;
        luaEngine.Globals["HINTMSG_CARDTYPE"] = 550;
        luaEngine.Globals["HINTMSG_OPTION"] = 551;
        luaEngine.Globals["HINTMSG_RESOLVEEFFECT"] = 552;
        luaEngine.Globals["HINTMSG_POSITION"] = 553;
        luaEngine.Globals["HINTMSG_ATTRIBUTE"] = 554;
        luaEngine.Globals["HINTMSG_RACE"] = 555;
        luaEngine.Globals["HINTMSG_CODE"] = 556;
        luaEngine.Globals["HINTMSG_NUMBER"] = 557;
        luaEngine.Globals["HINTMSG_EFFACTIVATE"] = 558;
        luaEngine.Globals["HINTMSG_LVRANK"] = 559;
        luaEngine.Globals["HINTMSG_RESOLVECARD"] = 560;
        luaEngine.Globals["HINTMSG_ZONE"] = 561;
        luaEngine.Globals["HINTMSG_DISABLEZONE"] = 562;
        luaEngine.Globals["HINTMSG_TOZONE"] = 563;
        luaEngine.Globals["HINTMSG_COUNTER"] = 564;
        luaEngine.Globals["HINTMSG_NEGATE"] = 565;
        luaEngine.Globals["HINTMSG_ATKDEF"] = 566;
        luaEngine.Globals["HINTMSG_APPLYTO"] = 567;
        luaEngine.Globals["HINTMSG_ATTACH"] = 568;
        luaEngine.Globals["HINTMSG_RTOGRAVE"] = 569;

        // ===== COIN/COIN SELECT CONSTANTS (FASE 25) =====
        luaEngine.Globals["SELECT_HEADS"] = 1;
        luaEngine.Globals["SELECT_TAILS"] = 0;

        // ===== DECLARATION TYPE CONSTANTS (FASE 25) =====
        luaEngine.Globals["DECLTYPE_MONSTER"] = 1;
        luaEngine.Globals["DECLTYPE_SPELL"] = 2;
        luaEngine.Globals["DECLTYPE_TRAP"] = 3;

        // ===== TIMING CONSTANTS (FASE 25) =====
        luaEngine.Globals["TIMING_DRAW_PHASE"] = 1;
        luaEngine.Globals["TIMING_STANDBY_PHASE"] = 2;
        luaEngine.Globals["TIMING_MAIN_END"] = 4;
        luaEngine.Globals["TIMING_BATTLE_START"] = 8;
        luaEngine.Globals["TIMING_BATTLE_END"] = 0x10;
        luaEngine.Globals["TIMING_END_PHASE"] = 0x20;
        luaEngine.Globals["TIMING_SUMMON"] = 0x40;
        luaEngine.Globals["TIMING_SPSUMMON"] = 0x80;
        luaEngine.Globals["TIMING_FLIPSUMMON"] = 0x100;
        luaEngine.Globals["TIMING_MSET"] = 0x200;
        luaEngine.Globals["TIMING_SSET"] = 0x400;
        luaEngine.Globals["TIMING_POS_CHANGE"] = 0x800;
        luaEngine.Globals["TIMING_ATTACK"] = 0x1000;
        luaEngine.Globals["TIMING_DAMAGE_STEP"] = 0x2000;
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
        luaEngine.Globals["TIMINGS_CHECK_MONSTER"] = 0x37fffff;
        luaEngine.Globals["TIMINGS_CHECK_MONSTER_E"] = 0x3ffffff;

        // ===== GLOBAL FLAG CONSTANTS (FASE 25) =====
        luaEngine.Globals["GLOBALFLAG_DECK_REVERSE_CHECK"] = 0x1;
        luaEngine.Globals["GLOBALFLAG_BRAINWASHING_CHECK"] = 0x2;
        luaEngine.Globals["GLOBALFLAG_DELAYED_QUICKEFFECT"] = 0x4;
        luaEngine.Globals["GLOBALFLAG_DETACH_EVENT"] = 0x8;
        luaEngine.Globals["GLOBALFLAG_SPSUMMON_COUNT"] = 0x10;
        luaEngine.Globals["GLOBALFLAG_SELF_TOGRAVE"] = 0x20;
        luaEngine.Globals["GLOBALFLAG_SPSUMMON_ONCE"] = 0x40;

        // ===== EFFECT COUNT CODE CONSTANTS (FASE 25) =====
        luaEngine.Globals["EFFECT_COUNT_CODE_OATH"] = 1;
        luaEngine.Globals["EFFECT_COUNT_CODE_DUEL"] = 2;
        luaEngine.Globals["EFFECT_COUNT_CODE_SINGLE"] = 3;
        luaEngine.Globals["EFFECT_COUNT_CODE_CHAIN"] = 4;

        // ===== DUEL MODE FLAGS (FASE 25) =====
        luaEngine.Globals["DUEL_TEST_MODE"] = 0x1;
        luaEngine.Globals["DUEL_ATTACK_FIRST_TURN"] = 0x2;
        luaEngine.Globals["DUEL_USE_TRAPS_IN_NEW_CHAIN"] = 0x4;
        luaEngine.Globals["DUEL_PSEUDO_SHUFFLE"] = 0x8;
        luaEngine.Globals["DUEL_TRIGGER_WHEN_PRIVATE_KNOWLEDGE"] = 0x10;
        luaEngine.Globals["DUEL_SIMPLE_AI"] = 0x20;
        luaEngine.Globals["DUEL_RELAY"] = 0x40;
        luaEngine.Globals["DUEL_OCG_OBSOLETE_IGNITION"] = 0x100;
        luaEngine.Globals["DUEL_OBSOLETE_IGNITION"] = 0x200;
        luaEngine.Globals["DUEL_PZONE"] = 0x400;
        luaEngine.Globals["DUEL_SEPARATE_PZONE"] = 0x800;
        luaEngine.Globals["DUEL_EMZONE"] = 0x1000;
        luaEngine.Globals["DUEL_FSX_MMZONE"] = 0x2000;
        luaEngine.Globals["DUEL_TRAP_MONSTERS_NOT_USE_ZONE"] = 0x4000;
        luaEngine.Globals["DUEL_RETURN_TO_DECK_TRIGGERS"] = 0x8000;
        luaEngine.Globals["DUEL_TRIGGER_ONLY_IN_LOCATION"] = 0x10000;
        luaEngine.Globals["DUEL_SPSUMMON_ONCE_OLD_NEGATE"] = 0x20000;
        luaEngine.Globals["DUEL_CANNOT_SUMMON_OATH_OLD"] = 0x40000;
        luaEngine.Globals["DUEL_NO_STANDBY_PHASE"] = 0x80000;
        luaEngine.Globals["DUEL_NO_HAND_LIMIT"] = 0x100000;
        luaEngine.Globals["DUEL_UNLIMITED_SUMMONS"] = 0x200000;
        luaEngine.Globals["DUEL_INVERTED_QUICK_PRIORITY"] = 0x400000;
        luaEngine.Globals["DUEL_EQUIP_NOT_SENT_IF_MISSING_TARGET"] = 0x800000;
        luaEngine.Globals["DUEL_STORE_ATTACK_REPLAYS"] = 0x1000000;
        luaEngine.Globals["DUEL_SINGLE_CHAIN_IN_DAMAGE_SUBSTEP"] = 0x2000000;
        luaEngine.Globals["DUEL_CAN_REPOS_IF_NON_SUMPLAYER"] = 0x4000000;
        luaEngine.Globals["DUEL_TCG_SEGOC_NONPUBLIC"] = 0x8000000;
        luaEngine.Globals["DUEL_TCG_SEGOC_FIRSTTRIGGER"] = 0x10000000;
        luaEngine.Globals["DUEL_TCG_FAST_EFFECT_IGNITION"] = 0x20000000;
        luaEngine.Globals["DUEL_EXTRA_DECK_RITUAL"] = 0x40000000;
        luaEngine.Globals["DUEL_NORMAL_SUMMON_FACEUP_DEF"] = 0x80000000;
        luaEngine.Globals["DUEL_MODE_SPEED"] = 0x1000000000;
        luaEngine.Globals["DUEL_MODE_RUSH"] = 0x2000000000;
        luaEngine.Globals["DUEL_MODE_GOAT"] = 0x4000000000;
        luaEngine.Globals["DUEL_OBSOLETE_RULING"] = 0x8000000000;

        // ===== ACTIVITY CONSTANTS (FASE 25) =====
        luaEngine.Globals["ACTIVITY_SUMMON"] = 1;
        luaEngine.Globals["ACTIVITY_NORMALSUMMON"] = 2;
        luaEngine.Globals["ACTIVITY_SPSUMMON"] = 3;
        luaEngine.Globals["ACTIVITY_FLIPSUMMON"] = 4;
        luaEngine.Globals["ACTIVITY_ATTACK"] = 5;
        luaEngine.Globals["ACTIVITY_BATTLE_PHASE"] = 6;
        luaEngine.Globals["ACTIVITY_CHAIN"] = 7;

        // ===== ANNOUNCE TYPE CONSTANTS (FASE 25) =====
        luaEngine.Globals["ANNOUNCE_CARD"] = 1;
        luaEngine.Globals["ANNOUNCE_CARD_FILTER"] = 2;

        // ===== SPECIAL EFFECT CONSTANTS (FASE 25) =====
        luaEngine.Globals["EFFECT_CAN_BE_TUNER"] = 362;
        luaEngine.Globals["EFFECT_CLEAR_WALL"] = 363;
        luaEngine.Globals["EFFECT_CLEAR_WORLD_IMMUNE"] = 364;
        luaEngine.Globals["EFFECT_CYBERDARK_WORLD"] = 365;
        luaEngine.Globals["EFFECT_FORMUD_SKIPPER"] = 366;
        luaEngine.Globals["EFFECT_FUR_HIRE_REPLACE"] = 367;
        luaEngine.Globals["EFFECT_GOLDEN_ALLURE_QUEEN"] = 368;
        luaEngine.Globals["EFFECT_ICEBARRIER_REPLACE"] = 369;
        luaEngine.Globals["EFFECT_MULTIPLE_TUNERS"] = 370;
        luaEngine.Globals["EFFECT_SFORCE_REPLACE"] = 371;
        luaEngine.Globals["EFFECT_SUPREME_CASTLE"] = 372;
        luaEngine.Globals["EFFECT_SYNSUB_NORDIC"] = 373;
        luaEngine.Globals["EFFECT_WITCHCRAFTER_REPLACE"] = 374;

        // ===== MARKER CONSTANTS (FASE 25) =====
        luaEngine.Globals["EFFECT_MARKER_DETACH_XMAT"] = 1201;
        luaEngine.Globals["EFFECT_MARKER_CARDIAN"] = 1202;
        luaEngine.Globals["EFFECT_MARKER_THUNDRA"] = 1203;
        luaEngine.Globals["EFFECT_MARKER_ALLURE_LVUP"] = 1204;
        luaEngine.Globals["EFFECT_MARKER_TELLAR"] = 1205;
        luaEngine.Globals["EFFECT_MARKER_DRAGON_RULER"] = 1206;

        // ===== REGISTER FLAG CONSTANTS (FASE 25) =====
        luaEngine.Globals["REGISTER_FLAG_DETACH_XMAT"] = 1;
        luaEngine.Globals["REGISTER_FLAG_CARDIAN"] = 2;
        luaEngine.Globals["REGISTER_FLAG_THUNDRA"] = 3;
        luaEngine.Globals["REGISTER_FLAG_ALLURE_LVUP"] = 4;
        luaEngine.Globals["REGISTER_FLAG_TELLAR"] = 5;
        luaEngine.Globals["REGISTER_FLAG_DRAGON_RULER"] = 6;

        // ===== FUSION/RITUAL PROCEDURE CONSTANTS (FASE 25) =====
        luaEngine.Globals["FUSPROC_NOTFUSION"] = 0;
        luaEngine.Globals["FUSPROC_CONTACTFUS"] = 1;
        luaEngine.Globals["FUSPROC_LISTEDMATS"] = 2;
        luaEngine.Globals["FUSPROC_NOLIMIT"] = 4;
        luaEngine.Globals["FUSPROC_CANCELABLE"] = 8;
        luaEngine.Globals["RITPROC_EQUAL"] = 1;
        luaEngine.Globals["RITPROC_GREATER"] = 2;

        // ===== MATERIAL CATEGORY CONSTANTS (FASE 25) =====
        luaEngine.Globals["MATERIAL_FUSION"] = 1;
        luaEngine.Globals["MATERIAL_SYNCHRO"] = 2;
        luaEngine.Globals["MATERIAL_XYZ"] = 3;
        luaEngine.Globals["MATERIAL_LINK"] = 4;

        // ===== WIN REASON CONSTANTS (FASE 25 - Victory Conditions) =====
        luaEngine.Globals["WIN_REASON_EXODIA"] = 0x1;
        luaEngine.Globals["WIN_REASON_FINAL_COUNTDOWN"] = 0x2;
        luaEngine.Globals["WIN_REASON_VENNOMINAGA"] = 0x4;
        luaEngine.Globals["WIN_REASON_CREATORGOD"] = 0x8;
        luaEngine.Globals["WIN_REASON_EXODIUS"] = 0x10;
        luaEngine.Globals["WIN_REASON_DESTINY_BOARD"] = 0x20;
        luaEngine.Globals["WIN_REASON_LAST_TURN"] = 0x40;
        luaEngine.Globals["WIN_REASON_PUPPET_LEO"] = 0x80;
        luaEngine.Globals["WIN_REASON_DISASTER_LEO"] = 0x100;
        luaEngine.Globals["WIN_REASON_RELAY_SOUL"] = 0x200;
        luaEngine.Globals["WIN_REASON_GHOSTRICK_MISCHIEF"] = 0x400;
        luaEngine.Globals["WIN_REASON_PHANTASM_SPIRAL"] = 0x800;
        luaEngine.Globals["WIN_REASON_FA_WINNERS"] = 0x1000;
        luaEngine.Globals["WIN_REASON_FLYING_ELEPHANT"] = 0x2000;
        luaEngine.Globals["WIN_REASON_EXODIA_DEFENDER"] = 0x4000;
        luaEngine.Globals["WIN_REASON_TRUE_EXODIA"] = 0x8000;
        luaEngine.Globals["WIN_REASON_FINAL_DRAW"] = 0x10000;
        luaEngine.Globals["WIN_REASON_CREATOR_MIRACLE"] = 0x20000;
        luaEngine.Globals["WIN_REASON_ZERO_GATE"] = 0x40000;
        luaEngine.Globals["WIN_REASON_DEUCE"] = 0x80000;
        luaEngine.Globals["WIN_REASON_DECK_MASTER"] = 0x100000;
        luaEngine.Globals["WIN_REASON_DRAW_OF_FATE"] = 0x200000;
        luaEngine.Globals["WIN_REASON_MUSICAL_SUMO"] = 0x400000;
        luaEngine.Globals["WIN_REASON_SUMMER_SCHOOLWORK"] = 0x800000;

        // 6. O Segredo da Sobrevivência (Metatable Global)
        // Qualquer constante OCGCore (ALL_CAPS) não definida acima retornará 0 em vez de nil.
        // Isso previne que a VM crashe com "attempt to perform arithmetic on a nil value" (Ex: FLAG_A + FLAG_B).
        // Também blinda as tabelas procedurais (Fusion, Ritual, etc) para retornarem efeitos vazios inofensivos caso a função não exista.
        luaEngine.DoString(@"
            local dummyFunc = function() return Effect.CreateEffect(nil) end
            local procMt = { __index = function() return dummyFunc end, __call = function() return dummyFunc end }
            local dummyTable = {}
            setmetatable(dummyTable, procMt)

            local procTables = { aux, Spirit, Toon, Union, Gemini, Pendulum, Link, Xyz, Synchro, Fusion, Ritual }
            for _, pTable in ipairs(procTables) do
                setmetatable(pTable, procMt)
            end

            setmetatable(_G, {
                __index = function(t, k)
                    if type(k) == 'string' and string.match(k, '^[A-Z0-9_]+$') then
                        return 0
                    end
                    -- Se começar com Letra Maiúscula (Ex: GladiatorBeast, Card, Effect) e for desconhecido, devolve Tabela Fantasma
                    if type(k) == 'string' and string.match(k, '^[A-Z][a-zA-Z0-9_]*$') then
                        return dummyTable
                    end
                    return nil
                end
            })

            -- Suporte nativo para o iterador avançado: 'for tc in group do'
            local dummyGroup = Group.CreateGroup()
            local gm = getmetatable(dummyGroup)
            if gm then
                gm.__call = function(grp, state, var)
                    if var == nil then return grp:GetFirst() end
                    return grp:GetNext()
                end
                gm.__iterator = function(grp)
                    return grp:Iter()
                end
            end
            
            Core = {}
            function Core.Attack(attacker, target)
                -- 1. Battle Step: Declara ataque e abre janela de corrente para armadilhas (Mirror Force)
                Duel.RaiseEvent(attacker, 1102, nil, 0, attacker:GetControler(), attacker:GetControler(), 0)
                coroutine.yield('WaitChain')
                
                -- Se o monstro foi destruído na corrente, aborta o ataque pacificamente
                if not attacker:IsOnField() then return false end
                if target ~= nil and not target:IsOnField() then return false end
                
                -- 2. Animação de Ataque (Sinal para a Unity tocar as espadas/tiros)
                coroutine.yield('PlayAttackAnimation')
                
                -- 3. Damage Step: Revela alvo se estiver setado para trás
                if target ~= nil and target:IsFacedown() then
                    coroutine.yield('RevealTarget')
                end
                
                -- 4. Damage Calculation & Resolution: Matemática e envio ao Cemitério
                Duel.CalculateDamage(attacker, target)
                
                return true
            end

            function Core.NormalSummon(player, card, isSet)
                local level = card:GetLevel()
                local tributes = 0
                if level >= 5 and level <= 6 then tributes = 1 end
                if level >= 7 then tributes = 2 end
                
                if Duel.GetActivityCount(player, 2) > 0 then
                    -- 2 = ACTIVITY_NORMALSUMMON
                    return false
                end
                
                if tributes > 0 then
                    local g = Duel.GetMatchingGroup(Card.IsReleasable, player, LOCATION_MZONE, 0, nil)
                    if g:GetCount() < tributes then return false end
                    local sg = g:Select(player, tributes, tributes, nil)
                    Duel.Release(sg, REASON_SUMMON)
                end
                
                if isSet then
                    Duel.MSet(player, card, true, nil)
                else
                    Duel.Summon(player, card, true, nil)
                end
                return true
            end
        ");
    }

    public LuaCard EnsureCardScriptLoaded(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return null;
        if (activeLuaCards.ContainsKey(card)) return activeLuaCards[card];
        
        string cardId = card.CurrentCardData.id;
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        
        if (!System.IO.File.Exists(scriptPath)) return null;

        try
        {
            DynValue selfTable = DynValue.NewTable(luaEngine);
            int numericId = 0;
            
            // Usa a senha oficial da Konami (password) como ID interno do LUA
            // Isso garante que funções oficiais como "GetID()" recebam o número real
            if (!string.IsNullOrEmpty(card.CurrentCardData.password))
                int.TryParse(card.CurrentCardData.password, out numericId);
            
            luaEngine.Globals["self_table"] = selfTable;
            luaEngine.Globals["self_code"] = numericId;

            string scriptCode = System.IO.File.ReadAllText(scriptPath);
            scriptCode = SanitizeOCGScript(scriptCode);
            luaEngine.DoString(scriptCode);

            DynValue initialEffect = selfTable.Table.Get("initial_effect");
            if (!initialEffect.IsNil())
            {
                LuaCard luaCard = new LuaCard(card);
                luaEngine.Call(initialEffect, luaCard);
                activeLuaCards[card] = luaCard;
                return luaCard;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA CRASH] Ocorreu uma falha no carregamento do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    private string SanitizeOCGScript(string script)
    {
        // A engine do EDOPro atualizou para Lua 5.3, que possui operadores bit a bit nativos (&, |, <<, >>, ~).
        // O MoonSharp roda em Lua 5.2, que não os entende nativamente e usa a biblioteca bit32.
        // Esta função sanitiza o script em tempo de execução para evitar crash da Máquina Virtual.

        // 0. Remove comentários para evitar falsos positivos na tradução de operadores (Ex: -- // comment)
        script = Regex.Replace(script, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);
        script = Regex.Replace(script, @"--.*", "");

        // Padrão Avançado: Captura parênteses balanceados infinitos sem gerar Catastrophic Backtracking
        string parens = @"\((?>[^()]+|\((?<DEPTH>)|\)(?<-DEPTH>))*(?(DEPTH)(?!))\)";
        string brackets = @"\[(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\]";
        string curlies = @"\{(?>[^{}]+|\{(?<DEPTH>)|}(?<-DEPTH>))*(?(DEPTH)(?!))\}";
        
        // O padrão captura variáveis, constantes, chamadas de métodos, arrays e agrupamentos
        string termPattern = $@"(?:[\w_]+|{parens}|{brackets}|{curlies})(?:\s*[\.\:]\s*[\w_]+|\s*{parens}|\s*{brackets}|\s*{curlies})*";

        // 1. Substitui o Bitwise NOT unário (~) por bit32.bnot()
        // O (?!=) previne que o Regex altere o operador de desigualdade (~=) nativo do Lua!
        script = Regex.Replace(script, $@"~(?!=)\s*({termPattern})", "bit32.bnot($1)");

        // 2. Divisão Inteira (//) do Lua 5.3 para math.floor()
        string prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*//\s*({termPattern})", "math.floor($1 / $2)");
        }

        // 3. Bitwise Shifts (<< e >>) para garantir compatibilidade
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*<<\s*({termPattern})", "bit32.lshift($1, $2)");
        }
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*>>\s*({termPattern})", "bit32.rshift($1, $2)");
        }

        // 4. Substitui o Bitwise AND (&) por bit32.band()
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*&\s*({termPattern})", "bit32.band($1, $2)");
        }

        // 5. Substitui o Bitwise OR (|) por bit32.bor() (Substitui a adição perigosa anterior)
        prev = "";
        while (script != prev)
        {
            prev = script;
            script = Regex.Replace(script, $@"({termPattern})\s*\|\s*({termPattern})", "bit32.bor($1, $2)");
        }

        // 6. Operador de Comprimento (#) do Lua 5.3 para userdata (MoonSharp crash fix)
        // Substitui a chamada de operador por uma verificação segura da função GetCount()
        script = Regex.Replace(script, @"#\s*([a-zA-Z_][a-zA-Z0-9_]*)", "(type($1)=='userdata' and $1:GetCount() or #$1)");

        // 7. Loop Direto sobre Userdata (Iterador para MoonSharp)
        // Converte "for tc in eg do" para "for tc in eg:Iter() do"
        script = Regex.Replace(script, @"for\s+([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_]+)\s+do", "for $1 in $2:Iter() do");

        return script;
    }

    public void ActivateCard(CardDisplay card, object triggerArgs, System.Action onComplete)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) 
        {
            onComplete?.Invoke();
            return;
        }

        // Procura por um efeito ativável instantâneo (Magia, Armadilha ou Ignition)
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            if (!CanActivateEffect(luaCard, activationEffect, tp, triggerArgs))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                onComplete?.Invoke();
                return; // Retorna sem chamar onComplete, abortando a ida para a Corrente
            }

            // Inicia a construção da corrente e a resolução LIFO
            StartCoroutine(BuildAndResolveChainRoutine(luaCard, activationEffect, triggerArgs, tp, onComplete));
            
            // Toca um flash e um som se for monstro ativando efeito
            if (card.CurrentCardData.type.Contains("Monster") && DuelFXManager.Instance != null)
            {
                DuelFXManager.Instance.PlayMonsterEffect(card);
            }
        }
        else
        {
            onComplete?.Invoke(); // Não tem efeito manual, apenas deixa o jogo seguir
        }
    }

    public bool ExecuteCardEffect(CardDisplay card)
    {
        LuaCard luaCard = EnsureCardScriptLoaded(card);
        if (luaCard == null) return false;

        // Fallback Original: Procura por um efeito e o ativa instantaneamente (Pula a Corrente)
        // Útil para efeitos engatilhados pela IA ou sistemas que não passam pela interface manual.
        LuaEffect activationEffect = luaCard.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);

        if (activationEffect != null)
        {
            int tp = luaCard.GetControler();
            
            // 1. Verificação Síncrona (Condition, Cost chk=0, Target chk=0)
            if (!CanActivateEffect(luaCard, activationEffect, tp, null))
            {
                if (GameManager.Instance == null || !GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} não cumpre os requisitos/custos para ativação no momento.");
                return false;
            }

            // 2. Injeta na Corrente Oficial
            StartCoroutine(BuildAndResolveChainRoutine(luaCard, activationEffect, null, tp, null));
            
            // Toca um flash e um som se for monstro ativando efeito
            if (card.CurrentCardData.type.Contains("Monster") && DuelFXManager.Instance != null)
            {
                DuelFXManager.Instance.PlayMonsterEffect(card);
            }
            return true;
        }

        if (!GameManager.Instance.isSimulating) Debug.LogWarning($"[API LUA] {card.CurrentCardData.name} possui scripts carregados, mas não tem efeito ativável imediato.");
        return false;
    }

    private object WrapTriggerArgs(object triggerArgs)
    {
        if (triggerArgs == null) 
        {
            LuaGroup dummyGroup = new LuaGroup();
            dummyGroup.AddCard(new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }));
            return dummyGroup; 
        }
        if (triggerArgs is LuaCard card)
        {
            LuaGroup group = new LuaGroup();
            group.AddCard(card);
            return group;
        }
        else if (triggerArgs is List<CardDisplay> list)
        {
            LuaGroup group = new LuaGroup();
            foreach (var c in list) group.AddCard(new LuaCard(c));
            return group;
        }
        return triggerArgs;
    }

    public bool CanActivateEffect(LuaCard luaCard, LuaEffect effect, int tp, object triggerArgs)
    {
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)luaCard;

        // Cria um Dummy 're' (Reason Effect) para evitar crashes se a carta tentar ler propriedades da corrente
        LuaEffect dummyRe = new LuaEffect { owner = luaCard };

        try 
        {
            if (effect.conditionFunc != null) {
                DynValue res = luaEngine.Call(effect.conditionFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.costFunc != null) {
                DynValue res = luaEngine.Call(effect.costFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), DynValue.NewNumber(0)); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            if (effect.targetFunc != null) {
                DynValue res = luaEngine.Call(effect.targetFunc, effect, arg2, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, null); // chk = 0
                if (res.Type == DataType.Boolean && !res.Boolean) return false;
            }
            return true;
        }
        catch (System.Exception ex)
        {
            if (GameManager.Instance == null || !GameManager.Instance.isSimulating)
                Debug.LogWarning($"[API LUA] Erro silencioso (seguro) ao verificar condições de {luaCard.unityData.name}: {ex.Message}");
            return false;
        }
    }

    private IEnumerator BuildAndResolveChainRoutine(LuaCard luaCard, LuaEffect effect, object triggerArgs, int tp, System.Action onComplete)
    {
        // FASE DE ATIVAÇÃO (Custo e Alvo) [chk=1]
        luaDuel.currentActivatingEffect = effect;
        if (effect.costFunc != null) {
            yield return StartCoroutine(RunLuaCoroutine(effect.costFunc, effect, tp, triggerArgs, 1));
            if (!lastCoroutineSuccess) { onComplete?.Invoke(); yield break; }
        }
        if (effect.targetFunc != null) {
            yield return StartCoroutine(RunLuaCoroutine(effect.targetFunc, effect, tp, triggerArgs, 1));
            if (!lastCoroutineSuccess) { onComplete?.Invoke(); yield break; }
        }
        luaDuel.currentActivatingEffect = null;

        // ADICIONA À CORRENTE LIFO
        ChainLink newLink = new ChainLink { 
            chainIndex = currentChain.Count + 1, 
            effect = effect, 
            card = luaCard, 
            player = tp, 
            triggerArgs = triggerArgs 
        };
        currentChain.Add(newLink);
        Debug.Log($"[Chain] Link {newLink.chainIndex}: {luaCard.unityData.name} adicionado à pilha.");

        // Feedback Visual de Corrente
        if (DuelFXManager.Instance != null && luaCard.unityCard != null)
            DuelFXManager.Instance.PlayChainLinkEffect(luaCard.unityCard, newLink.chainIndex);

        // JANELA DE RESPOSTA (Speed 2/3 - Pergunta ao Oponente)
        yield return StartCoroutine(ResponseWindowRoutine(1 - tp, newLink));

        // RESOLUÇÃO LIFO (De trás pra frente) - Apenas o Link 1 comanda o desempilhamento!
        if (newLink.chainIndex == 1)
        {
            isChainResolving = true;
            Debug.Log($"[Chain] Iniciando Resolução da Corrente com {currentChain.Count} elos...");

            for (int i = currentChain.Count - 1; i >= 0; i--)
            {
                ChainLink link = currentChain[i];
                if (link.isActivationNegated)
                {
                    Debug.Log($"[Chain] Link {link.chainIndex} ({link.card.unityData.name}): Ativação Negada.");
                    continue;
                }
                if (link.isNegated)
                {
                    Debug.Log($"[Chain] Link {link.chainIndex} ({link.card.unityData.name}): Efeito Negado.");
                    continue;
                }

                Debug.Log($"[Chain] Resolvendo Link {link.chainIndex}: {link.card.unityData.name}...");
                if (link.effect.operationFunc != null) 
                    yield return StartCoroutine(RunLuaCoroutine(link.effect.operationFunc, link.effect, link.player, link.triggerArgs, -1));

                CleanupSpellTrapAfterResolution(link.card);
            }

            currentChain.Clear();
            isChainResolving = false;
            Debug.Log($"[Chain] Corrente resolvida e limpa com sucesso!");
        }

        onComplete?.Invoke();
    }

    private IEnumerator ResponseWindowRoutine(int priorityPlayer, ChainLink triggerLink)
    {
        bool isHuman = (priorityPlayer == 0); // 0 = Player, 1 = Opponent(IA)
        List<CardDisplay> validResponses = GetValidResponses(priorityPlayer, triggerLink);

        if (validResponses.Count == 0) yield break; // Ninguém pode responder, a corrente avança

        bool decisionMade = false;
        CardDisplay chosenCard = null;

        if (isHuman && UIManager.Instance != null && (!GameManager.Instance.isSimulating))
        {
            UIManager.Instance.ShowResponseWindow(validResponses, (selectedCard) => {
                chosenCard = selectedCard;
                decisionMade = true;
            }, () => {
                decisionMade = true; // Passou
            });
            while (!decisionMade) yield return null;
        }
        else if (!isHuman && OpponentAI.Instance != null && (!GameManager.Instance.isSimulating))
        {
            // TODO IA: Escolher resposta (Por enquanto passa a vez)
            decisionMade = true;
        }
        else
        {
            decisionMade = true;
        }

        // Se o jogador escolheu uma resposta (ex: Magic Jammer), ativamos ela. 
        // Ela entrará na corrente como Link N+1 e recursivamente abrirá uma nova janela de resposta.
        if (chosenCard != null)
        {
            if (chosenCard.isFlipped == false && (chosenCard.CurrentCardData.type.Contains("Spell") || chosenCard.CurrentCardData.type.Contains("Trap")))
                chosenCard.ShowFront(); // Revela a face-down
                
            bool childChainEnded = false;
            ActivateCard(chosenCard, triggerLink.card, () => { childChainEnded = true; }); // triggerArgs vira a carta que causou a resposta
            while (!childChainEnded) yield return null;
        }
    }

    public bool NegateChainLink(int chainIndex)
    {
        var link = currentChain.Find(l => l.chainIndex == chainIndex);
        if (link != null) { link.isActivationNegated = true; return true; }
        return false;
    }

    private void CleanupSpellTrapAfterResolution(LuaCard luaCard)
    {
        if (luaCard == null || luaCard.unityCard == null || !luaCard.unityCard.isOnField) return;

        // Só aplicamos a limpeza a Mágicas e Armadilhas
        if (!luaCard.unityData.type.Contains("Spell") && !luaCard.unityData.type.Contains("Trap")) return;

        string prop = (luaCard.unityData.property ?? "").ToLower();
        string type = (luaCard.unityData.type ?? "").ToLower();

        // Se for de permanência, fica
        bool isContinuous = type.Contains("continuous") || prop.Contains("continuous");
        bool isEquip = type.Contains("equip") || prop.Contains("equip");
        bool isField = type.Contains("field") || prop.Contains("field");
        bool hasRemainField = luaCard.registeredEffects.Exists(e => e.code == 17); // 17 = EFFECT_REMAIN_FIELD

        if (!isContinuous && !isEquip && !isField && !hasRemainField)
        {
            Debug.Log($"[CardEffectManager] Limpando Mágica/Armadilha normal após uso: {luaCard.unityData.name}");
            GameManager.Instance.SendToGraveyard(luaCard.unityData, luaCard.unityCard.isPlayerCard, CardLocation.Field, SendReason.Rule);
            Destroy(luaCard.unityCard.gameObject);
        }
    }

    private IEnumerator RunLuaCoroutine(Closure func, LuaEffect effect, int tp, object triggerArgs, int chk)
    {
        lastCoroutineSuccess = true;
        
        object eg = WrapTriggerArgs(triggerArgs);
        bool isActionEffect = (effect.type & 0x07F8) != 0;
        object arg2 = isActionEffect ? (object)tp : (object)(effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }));
        
        LuaEffect dummyRe = new LuaEffect { owner = effect.owner ?? new LuaCard(new CardData { id = "0000", name = "Dummy" }) };

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            // Assinatura YGOPro: (e, tp, eg, ep, ev, re, r, rp, chk)
            if (chk >= 0) result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(tp), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(tp), DynValue.NewNumber(chk));
            else result = activeLuaCoroutine.Coroutine.Resume(effect, arg2, eg, DynValue.NewNumber(tp), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(tp));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] O efeito falhou graciosamente sem travar a engine: {e.Message}");
            lastCoroutineSuccess = false;
            yield break; // Aborta apenas este efeito, o jogo continua!
        }

        while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
        {
            while (isWaitingForLuaYield) yield return null;
            
            // Re-check state before resuming - coroutine may have completed during yield
            if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended)
                break;
            
            try
            {
                result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                lastCoroutineSuccess = false;
                yield break;
            }
        }
    }

    public IEnumerator RunGenericLuaCoroutine(Closure func, params object[] args)
    {
        // Captura o atacante e o alvo antes que o C# os limpe do cache global
        LuaCard storedAttacker = luaDuel.currentAttacker;
        LuaCard storedTarget = luaDuel.currentAttackTarget;

        activeLuaCoroutine = luaEngine.CreateCoroutine(func);
        DynValue result;
        
        try
        {
            result = activeLuaCoroutine.Coroutine.Resume(args);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API LUA CRASH] Função Genérica falhou: {e.Message}");
            yield break; 
        }

        while (activeLuaCoroutine.Coroutine.State == CoroutineState.Suspended)
        {
            if (result.Type == DataType.YieldRequest)
            {
                while (isWaitingForLuaYield) yield return null;
            }
            else if (result.Type == DataType.String)
            {
                string yieldCmd = result.String;
                if (yieldCmd == "WaitChain")
                {
                    yield return new WaitWhile(() => isChainResolving);
                }
                else if (yieldCmd == "PlayAttackAnimation")
                {
                    // Esconde a espada de "mira" exatamente na transição para a espada voadora
                    if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
                    
                    if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations)
                    {
                        bool animDone = false;
                        if (storedAttacker != null && storedAttacker.unityCard != null)
                        {
                            DuelFXManager.Instance.PlayAttack(storedAttacker.unityCard, storedTarget?.unityCard, () => { animDone = true; });
                            yield return new WaitUntil(() => animDone);
                        }
                    }
                }
                else if (yieldCmd == "RevealTarget")
                {
                    if (storedTarget != null && storedTarget.unityCard != null)
                    {
                        storedTarget.unityCard.RevealCard(true, true);
                        yield return new WaitForSeconds(0.8f); // Pausa pra ver o flip dramático
                    }
                }
            }
            
            if (activeLuaCoroutine.Coroutine.State != CoroutineState.Suspended) break;
            
            try { result = activeLuaCoroutine.Coroutine.Resume(yieldReturnValue); }
            catch (System.Exception e)
            {
                Debug.LogError($"[API LUA CRASH] Falha ao continuar o yield: {e.Message}");
                yield break;
            }
        }
        
        // Garante que a espada de mira suma se o ataque for abortado por uma Armadilha (ex: Mirror Force)
        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
    }

    public void TriggerLuaEvent(int eventCode, object triggerArgs)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return;

        List<CardDisplay> allField = new List<CardDisplay>();
        CollectCards(GameManager.Instance.duelFieldUI.playerMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentMonsterZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.playerSpellZones, allField);
        CollectCards(GameManager.Instance.duelFieldUI.opponentSpellZones, allField);

        foreach(var c in allField)
        {
            LuaCard lc = EnsureCardScriptLoaded(c);
            if (lc == null) continue;

            var matchingEffects = lc.registeredEffects.FindAll(e => e.code == eventCode && (e.type & 0x0001) == 0);
            foreach(var effect in matchingEffects)
            {
                if (CanActivateEffect(lc, effect, lc.GetControler(), triggerArgs))
                {
                    StartCoroutine(BuildAndResolveChainRoutine(lc, effect, triggerArgs, lc.GetControler(), null));
                }
            }
        }
    }

    private List<CardDisplay> GetValidResponses(int tp, ChainLink triggerLink)
    {
        List<CardDisplay> responses = new List<CardDisplay>();
        if (GameManager.Instance == null) return responses;
        
        Transform[] zones = tp == 0 ? GameManager.Instance.duelFieldUI.playerSpellZones : GameManager.Instance.duelFieldUI.opponentSpellZones;
        foreach (var z in zones)
        {
            if (z.childCount > 0)
            {
                CardDisplay cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.isFlipped) // Apenas cartas Setadas 
                {
                    LuaCard lc = EnsureCardScriptLoaded(cd);
                    if (lc != null)
                    {
                        // Efeitos Rápidos (Quick-Play) ou Armadilhas (Activate / Quick_O)
                        LuaEffect eff = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0100 || e.type == 0x0080);
                        // A carta LUA recebe 'chk=0' para saber se pode se ativar em resposta àquele elo específico!
                        if (eff != null && CanActivateEffect(lc, eff, tp, triggerLink.card))
                        {
                            responses.Add(cd);
                        }
                    }
                }
            }
        }
        return responses;
    }

    private void CollectCards(Transform[] zones, List<CardDisplay> list)
    {
        if (zones == null) return;
        foreach (var z in zones)
        {
            if (z != null && z.childCount > 0)
            {
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    // --- HOOKS DA ENGINE (O LUA VAI SE INSCREVER NELES DEPOIS) ---
    public void OnSummon(CardDisplay card) { 
        LuaCard lc = EnsureCardScriptLoaded(card);
        if (lc != null) {
            var singleEffects = lc.registeredEffects.FindAll(e => e.code == 11 && (e.type & 0x0001) != 0); // EFFECT_TYPE_SINGLE
            foreach(var e in singleEffects) 
            {
                if (CanActivateEffect(lc, e, lc.GetControler(), null))
                    StartCoroutine(BuildAndResolveChainRoutine(lc, e, null, lc.GetControler(), null));
            }
        }
        TriggerLuaEvent(11, lc); // EVENT_SUMMON_SUCCESS
    }
    public void OnSet(CardDisplay card) { }
    public void OnBattlePositionChanged(CardDisplay card) { }
    public void OnDamageDealt(CardDisplay attacker, CardDisplay target, int amount) { }
    public void OnCounterTrapResolved(CardDisplay trap) { }
    public void OnCardAddedToHand(CardDisplay card) { }
    public void OnTribute(CardDisplay card) { }
    public void OnCardDiscarded(CardDisplay card, bool causedByOpponent) { }
    public void OnCardDrawn(CardData card, bool isPlayer) { }
    public void OnSpecialSummon(CardDisplay card) { }
    public void OnControlSwitched(CardDisplay card) { }
    public void OnPhaseStart(GamePhase phase) { 
        if (phase == GamePhase.End) CleanAllExpiredModifiers(); 
        TriggerLuaEvent(4096, null); // EVENT_PHASE
    }
    public void OnPreDrawPhase(bool isPlayerTurn, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnCardSentToGraveyard(CardData card, bool isOwnerPlayer, CardLocation fromLocation, SendReason reason) { }
    public void OnDamageTaken(bool isPlayer, int amount) { }
    public void OnLifePointsGained(bool isPlayer, int amount) { }
    public void OnCardEquipped(CardDisplay equip, CardDisplay target)
    {
        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayEquipEffect(equip, target);

        RecalculateStats(target);
    }
    public void OnSpellActivated(CardDisplay spell) { }

    // Métodos (Stubs) mantidos para não quebrar a lógica hardcoded do GameManager
    public void CheckMaintenanceCosts() { }
    public bool HasActiveSecondCoinToss(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeSecondCoinToss(bool isPlayer) { }
    public bool HasActiveDiceReRoll(out bool isPlayerCard) { isPlayerCard = false; return false; }
    public void ConsumeDiceReRoll(bool isPlayer) { }

    public void OnCardLeavesField(CardDisplay card)
    {
        // NOVA LÓGICA DE EQUIPAMENTO: Se a carta que sai é um equipamento, recalcula o status do alvo
        // Este bloco deve vir ANTES da destruição dos links.
        CardLink[] allLinks = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in allLinks)
        {
            // Se a carta que está saindo é a FONTE de um link de equipamento
            if (link.source == card && link.type == CardLink.LinkType.Equipment && link.target != null)
            {
                // Recalcula o status do alvo, que agora não tem mais este equipamento
                // Atraso de um frame para garantir que o link seja destruído primeiro se necessário
                StartCoroutine(RecalculateStatsNextFrame(link.target));
            }
        }

        // Limpeza de cache de Lua para não poluir a RAM
        if (activeLuaCards.ContainsKey(card)) activeLuaCards.Remove(card);

        // Destrói equipamentos físicos que dependem DESTA carta (se esta carta for o ALVO)
        foreach (var link in allLinks)
        {
            if (link.target == card && link.type == CardLink.LinkType.Equipment && link.source != null && link.source.isOnField)
            {
                GameManager.Instance.SendToGraveyard(link.source.CurrentCardData, link.source.isPlayerCard);
                Destroy(link.source.gameObject);
            }
        }

        // Libera zonas bloqueadas por cartas contínuas
        if (blockedZonesByCard.ContainsKey(card))
        {
            if (GameManager.Instance.duelFieldUI != null)
            {
                foreach (var z in blockedZonesByCard[card]) GameManager.Instance.duelFieldUI.UnblockZone(z);
            }
            blockedZonesByCard.Remove(card);
        }
    }

    private IEnumerator RecalculateStatsNextFrame(CardDisplay monster)
    {
        yield return null; // Aguarda 1 frame
        RecalculateStats(monster);
    }
    
    public void RecalculateStats(CardDisplay monster)
    {
        if (monster == null || monster.CurrentCardData == null || !monster.CurrentCardData.type.Contains("Monster")) return;

        int newAtk = monster.originalAtk;
        int newDef = monster.originalDef;

        // Encontra os equipamentos
        List<CardDisplay> equippedCards = GetEquippedCards(monster);

        foreach (var equipCard in equippedCards)
        {
            if (equipCard == null) continue;
            LuaCard luaEquip = EnsureCardScriptLoaded(equipCard);
            if (luaEquip == null) continue;

            var equipEffects = luaEquip.registeredEffects.FindAll(e => e.type == 0x4); // EFFECT_TYPE_EQUIP
            foreach (var effect in equipEffects)
            {
                try
                {
                    int value = 0;
                    object valObj = effect.GetValue();
                    if (valObj is double || valObj is long)
                    {
                        value = System.Convert.ToInt32(valObj);
                    }
                    else if (valObj is Closure valClosure)
                    {
                        DynValue result = luaEngine.Call(valClosure, effect, new LuaCard(monster));
                        if (result.Type == DataType.Number)
                        {
                            value = (int)result.Number;
                        }
                    }

                    if (effect.code == 100) // EFFECT_UPDATE_ATTACK
                    {
                        newAtk += value;
                    }
                    else if (effect.code == 104) // EFFECT_UPDATE_DEFENSE
                    {
                        newDef += value;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RecalculateStats] Erro ao aplicar o efeito de {equipCard.CurrentCardData.name}: {ex.Message}");
                }
            }
        }

        monster.currentAtk = newAtk;
        monster.currentDef = newDef;

        Debug.Log($"[RecalculateStats] Status de {monster.CurrentCardData.name} atualizado para ATK {newAtk} / DEF {newDef}");
    }

    // --- HOOKS DE BATALHA ---
    public void OnAttackDeclared(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnDamageCalculation(CardDisplay attacker, CardDisplay target, System.Action onContinue) { onContinue?.Invoke(); }
    public void OnBattleEnd(CardDisplay attacker, CardDisplay target) { 
        if (target != null && target.CurrentCardData != null && (GameManager.Instance.GetPlayerGraveyard().Contains(target.CurrentCardData) || GameManager.Instance.GetOpponentGraveyard().Contains(target.CurrentCardData)))
        {
            LuaCard deadCard = EnsureCardScriptLoaded(target);
            if (deadCard != null) {
                var singleEffects = deadCard.registeredEffects.FindAll(e => e.code == 1010 && (e.type & 0x0001) != 0); // EVENT_BATTLE_DESTROYED
                foreach(var e in singleEffects) 
                {
                    if (CanActivateEffect(deadCard, e, deadCard.GetControler(), deadCard))
                        StartCoroutine(BuildAndResolveChainRoutine(deadCard, e, deadCard, deadCard.GetControler(), null));
                }
            }
            TriggerLuaEvent(1010, deadCard);
        }
    }
    public bool CanDeclareAttack(CardDisplay attacker) { return true; }
    public bool IsAttackPreventedByContinuousEffect(CardDisplay attacker) { return false; }
    public bool IsAttackRestricted(CardDisplay attacker) { return false; }

    // --- HELPERS E UTILITÁRIOS (A PONTE LUA VAI CHAMÁ-LOS) ---

    public void Effect_DirectDamage(CardDisplay source, int amount)
    {
        if (source.isPlayerCard) GameManager.Instance.DamageOpponent(amount);
        else GameManager.Instance.DamagePlayer(amount);
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    public void Effect_GainLP(CardDisplay source, int amount)
    {
        GameManager.Instance.GainLifePoints(source.isPlayerCard, amount);
    }

    public bool Effect_PayLP(CardDisplay source, int amount)
    {
        return GameManager.Instance.PayLifePoints(source.isPlayerCard, amount);
    }

    public List<CardDisplay> GetEquippedCards(CardDisplay target)
    {
        List<CardDisplay> equipped = new List<CardDisplay>();
        CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
            if (link.target == target && link.type == CardLink.LinkType.Equipment && link.source != null)
                equipped.Add(link.source);
        return equipped;
    }

    public void CleanAllExpiredModifiers()
    {
        // O LUA Core já cuida da limpeza das Closures de Turno (RESET_PHASE + PHASE_END)
    }

    public void RollDice(int amount, System.Action<List<int>> onResult)
    {
        List<int> results = new List<int>();
        for (int i = 0; i < amount; i++) results.Add(Random.Range(1, 7));
        onResult?.Invoke(results);
    }

    public void SetClockCounter(CardDisplay target, int turns)
    {
        if (target != null) target.turnCounter = turns; 
    }

    public void TickClock(CardDisplay card, System.Action onComplete)
    {
        if (card == null || card.turnCounter <= 0) { onComplete?.Invoke(); return; }
        int oldTurns = card.turnCounter;
        card.turnCounter--; 
        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null && GameManager.Instance.enableTurnClockVisuals)
            GameManager.Instance.turnClockUI.AnimateTick(card.CurrentCardData.name, oldTurns, card.turnCounter, card.maxTurnCounter, onComplete);
        else onComplete?.Invoke(); 
    }

    // =========================================================================
    // FERRAMENTA DE DEV: VALIDADOR DE SCRIPTS EM MASSA
    // =========================================================================
    [ContextMenu("DEV: Validar Todos os Scripts LUA")]
    public void DevValidateAllScripts()
    {
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) 
        {
            Debug.LogError("O jogo precisa estar rodando (Play) para executar a validação.");
            return;
        }
        
        int total = 0, success = 0, failed = 0;
        List<string> errorLogs = new List<string>();
        Debug.Log("<color=cyan>Iniciando Validação em Massa de LUA...</color>");
        
        foreach (var card in GameManager.Instance.cardDatabase.cardDatabase)
        {
            if (card.type.Contains("Normal") && !card.type.Contains("Effect")) continue;
            
            total++;
            GameObject dummyGO = new GameObject("DummyTester");
            dummyGO.AddComponent<UnityEngine.UI.RawImage>(); // Silencia o aviso visual
            CardDisplay dummy = dummyGO.AddComponent<CardDisplay>();
            dummy.gameObject.SetActive(false); // Mantém invisível
            dummy.SetCard(card, null, true);
            
            try
            {
                LuaCard lc = EnsureCardScriptLoaded(dummy);
                if (lc != null) 
                {
                    bool runtimeError = false;
                     // Evita o erro 'Collection was modified' fazendo uma cópia da lista
                    foreach (var eff in new List<LuaEffect>(lc.registeredEffects))
                     {
                        try {
                            object eg = WrapTriggerArgs(null);
                            LuaEffect dummyRe = new LuaEffect { owner = lc };
                            LuaCard dummyChkc = new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 });

                            // Efeitos de Ação/Gatilho (0x07F8 cobre do 0x0008 ao 0x0400: Activate, Flip, Ignition, Quick, Trigger_F, etc)
                            // Se for ação, envia o Jogador (tp). Se for contínuo/campo, envia a Carta (c).
                            bool isActionEffect = (eff.type & 0x07F8) != 0; 

                            if (eff.conditionFunc != null) luaEngine.Call(eff.conditionFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0));
                            if (eff.costFunc != null) luaEngine.Call(eff.costFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0);
                            // O '0' a mais aqui corrige o chk=0, impedindo ativações falsas explosivas!
                            if (eff.targetFunc != null) luaEngine.Call(eff.targetFunc, eff, isActionEffect ? (object)lc.GetControler() : (object)lc, eg, DynValue.NewNumber(0), DynValue.NewNumber(0), dummyRe, DynValue.NewNumber(0), DynValue.NewNumber(0), 0, dummyChkc);
                        } catch (System.Exception ex) {
                            errorLogs.Add($"<color=orange>[RUNTIME] Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
                            runtimeError = true;
                        }
                    }
                    if (runtimeError) failed++;
                    else success++;
                }
            }
            catch (System.Exception ex)
            {
                failed++;
                errorLogs.Add($"<color=red>Falha na carta {card.name} ({card.id}): {ex.Message}</color>");
            }
            DestroyImmediate(dummy.gameObject);
        }
        Debug.Log($"<color=green>Validação Concluída! Total: {total} | Sucesso: {success} | Falhas: {failed}</color>");

        if (errorLogs.Count > 0)
        {
            Debug.LogWarning($"<color=yellow>--- RELATÓRIO DE ERROS ({errorLogs.Count}) ---</color>");
            foreach (var err in errorLogs)
            {
                Debug.LogError(err);
            }
        }
    }
}