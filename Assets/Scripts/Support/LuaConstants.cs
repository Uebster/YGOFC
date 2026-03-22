using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FASE 7: Constants Bridge - Mapeia valores de Lua constants para CardLocation/Position enums
/// 
/// Lua Constants:
/// LOCATION_HAND = 0x2
/// LOCATION_DECK = 0x1
/// LOCATION_GRAVE = 0x10
/// LOCATION_EXTRA = 0x40
/// LOCATION_REMOVED = 0x20
/// 
/// POS_FACEUP_ATTACK = 0x1
/// POS_FACEDOWN_ATTACK = 0x2
/// POS_FACEUP_DEFENSE = 0x4
/// POS_FACEDOWN_DEFENSE = 0x8
/// 
/// TYPE (bitflags):
/// TYPE_MONSTER = 0x1
/// TYPE_SPELL = 0x2
/// TYPE_TRAP = 0x4
/// TYPE_FUSION = 0x40
/// TYPE_RITUAL = 0x80
/// TYPE_SYNCHRO = 0x2000
/// TYPE_XYZ = 0x800000
/// </summary>
public static class LuaConstants
{
    // ===== LOCATION CONSTANTS =====
    public const int LOCATION_HAND = 0x2;
    public const int LOCATION_DECK = 0x1;
    public const int LOCATION_GRAVE = 0x10;
    public const int LOCATION_EXTRA = 0x40;
    public const int LOCATION_REMOVED = 0x20;
    public const int LOCATION_FIELD = 0x8; // On field (Spell-Trap zones)
    public const int LOCATION_MZONE = 0x4;  // Monster zone
    public const int LOCATION_SZONE = 0x8;  // Spell/Trap zone
    public const int LOCATION_UNKNOWN = 0x0;

    // ===== POSITION CONSTANTS =====
    public const int POS_FACEUP_ATTACK = 0x1;
    public const int POS_FACEDOWN_ATTACK = 0x2;
    public const int POS_FACEUP_DEFENSE = 0x4;
    public const int POS_FACEDOWN_DEFENSE = 0x8;

    // ===== TYPE CONSTANTS (Bitflags) =====
    public const int TYPE_MONSTER = 0x1;
    public const int TYPE_SPELL = 0x2;
    public const int TYPE_TRAP = 0x4;
    public const int TYPE_FUSION = 0x40;
    public const int TYPE_RITUAL = 0x80;
    public const int TYPE_SYNCHRO = 0x2000;
    public const int TYPE_XYZ = 0x800000;
    public const int TYPE_LINK = 0x1000000;

    // ===== EVENT CONSTANTS (FASE 12) =====
    public const int EVENT_DRAW = 1;
    public const int EVENT_DESTROY = 2;
    public const int EVENT_SUMMON = 3;
    public const int EVENT_DAMAGE = 4;
    public const int EVENT_RECOVER = 5;
    public const int EVENT_MOVE = 6;
    public const int EVENT_ACTIVATE = 7;
    public const int EVENT_CUSTOM = 99;

    // ===== STATUS CONSTANTS (FASE 14) =====
    public const int STATUS_BATTLE_DESTROYED = 1;      // Card destroyed in battle
    public const int STATUS_BLOCKED = 2;                // Card effect blocked/negated
    public const int STATUS_CANT_ATTACK = 4;            // Card cannot attack this turn
    public const int STATUS_CANT_SUMMON = 8;            // No summons allowed
    public const int STATUS_TOON = 16;                  // Card is Toon
    public const int STATUS_MONSTER = 32;               // Card is monster
    public const int STATUS_MZONE_EFFECT_RESET = 64;    // Monster zone effect reset
    public const int STATUS_ACTIVATED = 128;            // Effect was activated
    public const int STATUS_DESTROYED = 256;            // Card was destroyed this turn
    public const int STATUS_SPSUMMON = 512;             // Recently special summoned

    // ===== CARD IDENTIFIER CONSTANTS (FASE 16) - Environment & Important Cards =====
    // These are password codes used to identify specific cards by code
    public const int CARD_UMI = 22702055;                // Umi (Field Spell)
    public const int CARD_NECROVALLEY = 47355498;       // Necrovalley (Field Spell)
    public const int CARD_SANCTUARY_SKY = 56433456;     // The Sanctuary in the Sky (Field Spell)
    public const int CARD_PANDEMONIUM = 94585852;       // Pandemonium (Field Spell)
    public const int CARD_BLUEEYES_W_DRAGON = 89631139; // Blue-Eyes White Dragon
    public const int CARD_MONSTER_REBORN = 83746708;    // Monster Reborn (Spell)
    public const int CARD_DARK_HOLE = 53129469;         // Dark Hole (Spell)
    public const int CARD_JINZO = 76375976;             // Jinzo (Monster)
    public const int CARD_MAGIC_CYLINDER = 3898165;     // Magic Cylinder (Trap)
    public const int CARD_NEGATE_ATTACK = 14087893;     // Negate Attack (Trap)
    public const int CARD_MIRROR_FORCE = 44095762;      // Mirror Force (Trap)
    public const int CARD_TORRENTIAL_TRIBUTE = 53582587; // Torrential Tribute (Trap)
    public const int CARD_QUEEN_KNIGHT = 25109950;      // Queen Knight
    public const int CARD_JACK_KNIGHT = 91444579;       // Jack Knight
    public const int CARD_KING_KNIGHT = 64274560;       // King Knight

    // ===== EFFECT TYPE CONSTANTS (FASE 20) =====
    public const int EFFECT_TYPE_IGNITION = 0x1;        // Player can activate anytime
    public const int EFFECT_TYPE_ACTIVATE = 0x2;        // Card activation effect
    public const int EFFECT_TYPE_SINGLE = 0x4;          // Single use effect
    public const int EFFECT_TYPE_FLIP = 0x8;            // Flip effect
    public const int EFFECT_TYPE_TRIGGER_F = 0x10;      // Mandatory trigger effect
    public const int EFFECT_TYPE_QUICK_O = 0x20;        // Quick-play effect
    public const int EFFECT_TYPE_FIELD = 0x40;          // Field spell effect
    public const int EFFECT_TYPE_CONTINUOUS = 0x80;     // Continuous effect
    public const int EFFECT_TYPE_EQUIP = 0x100;         // Equip card effect

    // ===== EFFECT FLAG CONSTANTS (FASE 20) =====
    public const int EFFECT_FLAG_PLAYER_TARGET = 0x1;
    public const int EFFECT_FLAG_DAMAGE_STEP = 0x2;
    public const int EFFECT_FLAG_CARD_TARGET = 0x4;
    public const int EFFECT_FLAG_SET_AVAILABLE = 0x8;
    public const int EFFECT_FLAG_SINGLE_RANGE = 0x10;

    // ===== EFFECT UPDATE CONSTANTS (FASE 20) =====
    public const int EFFECT_UPDATE_ATTACK = 0x1;
    public const int EFFECT_UPDATE_DEFENSE = 0x2;
    public const int EFFECT_DIRECT_ATTACK = 0x1000;
    public const int EFFECT_SET_ATTACK_FINAL = 0x2000;

    // ===== CATEGORY CONSTANTS (Effect categories - FASE 20) =====
    public const int CATEGORY_DRAW = 0x1;
    public const int CATEGORY_DESTROY = 0x2;
    public const int CATEGORY_TOHAND = 0x4;
    public const int CATEGORY_TODECK = 0x8;
    public const int CATEGORY_SPECIAL_SUMMON = 0x10;
    public const int CATEGORY_DAMAGE = 0x20;
    public const int CATEGORY_RECOVER = 0x40;
    public const int CATEGORY_NEGATE = 0x80;
    public const int CATEGORY_SEARCH = 0x100;
    public const int CATEGORY_POSITION = 0x200;
    public const int CATEGORY_SET = 0x400;
    public const int CATEGORY_ATKCHANGE = 0x800;

    // ===== ADDITIONAL EVENT CONSTANTS (FASE 20) =====
    public const int EVENT_FREE_CHAIN = 0x4000000;
    public const int EVENT_CHAINING = 0x8000000;
    public const int EVENT_TO_GRAVE = 0x10000000;
    public const int EVENT_BATTLE_DESTROYING = 0x40000000;

    // ===== REASON CONSTANTS (FASE 20) =====
    public const int REASON_EFFECT = 0x40;
    public const int REASON_COST = 0x80;
    public const int REASON_DISCARD = 0x4000;
    public const int REASON_BATTLE = 0x1;     // Card destroyed/sent by battle

    // ===== PHASE CONSTANTS (FASE 20) =====
    public const int PHASE_END = 0x200;
    public const int PHASE_DAMAGE = 0x20;
    public const int PHASE_DAMAGE_CAL = 0x40;
    public const int PHASE_BATTLE = 0x4;       // Battle phase
    public const int PHASE_MAIN1 = 0x1;        // Main phase 1
    public const int PHASE_MAIN2 = 0x2;        // Main phase 2

    // ===== ATTRIBUTE CONSTANTS (FASE 20) =====
    public const int ATTRIBUTE_EARTH = 0x1;
    public const int ATTRIBUTE_WATER = 0x2;
    public const int ATTRIBUTE_FIRE = 0x4;
    public const int ATTRIBUTE_WIND = 0x8;
    public const int ATTRIBUTE_LIGHT = 0x10;
    public const int ATTRIBUTE_DARK = 0x20;
    public const int ATTRIBUTE_DIVINE = 0x40;

    // ===== RACE CONSTANTS (FASE 20) =====
    public const int RACE_WARRIOR = 0x1;
    public const int RACE_SPELLCASTER = 0x2;
    public const int RACE_FAIRY = 0x4;
    public const int RACE_FIEND = 0x8;
    public const int RACE_ZOMBIE = 0x10;
    public const int RACE_MACHINE = 0x20;
    public const int RACE_AQUA = 0x40;
    public const int RACE_PYRO = 0x80;
    public const int RACE_ROCK = 0x100;
    public const int RACE_WINGED_BEAST = 0x200;
    public const int RACE_PLANT = 0x400;
    public const int RACE_INSECT = 0x800;
    public const int RACE_THUNDER = 0x1000;
    public const int RACE_DRAGON = 0x2000;
    public const int RACE_BEAST = 0x4000;
    public const int RACE_BEAST_WARRIOR = 0x8000;
    public const int RACE_DINOSAUR = 0x10000;
    public const int RACE_FISH = 0x20000;
    public const int RACE_SEA_SERPENT = 0x40000;
    public const int RACE_REPTILE = 0x80000;
    public const int RACE_SYNCHRO = 0x100000;
    public const int RACE_TUNER = 0x200000;
    public const int RACE_XYZ = 0x400000;

    // ===== RESET CONSTANTS (FASE 20) =====
    public const int RESET_EVENT = 0x1000;
    public const int RESET_PHASE = 0x40000000;
    public const int RESET_STANDARD = 0x60000000;  // RESET_PHASE + RESET_END
    public const int RESET_STANDARD_PHASE_END = 0x60000000;
    public const int RESET_END_PHASE = 0x40000000;
    public const int RESETS_STANDARD_EXC_GRAVE = 0x6FF00FF;  // FASE 21

    // ===== SET CONSTANTS - Archetype Identifiers (FASE 21) =====
    public const int SET_AMAZONESS = 0x4F;
    public const int SET_ARCHFIEND = 0x50;
    public const int SET_GUARDIAN = 0x51;
    public const int SET_BATTERYMAN = 0x52;
    public const int SET_HERO = 0x53;
    public const int SET_LIGHTSWORN = 0x54;
    public const int SET_BLACKWING = 0x55;
    public const int SET_DRAGUNITY = 0x56;

    // ===== COUNTER CONSTANTS (FASE 21) =====
    public const int COUNTER_SPELL = 1;  // Spell counter

    // ===== CONVERSION HELPERS =====

    /// <summary>
    /// Converte Lua location constant para CardLocation enum
    /// </summary>
    public static CardLocation LuaLocationToCardLocation(int luaLocation)
    {
        switch (luaLocation)
        {
            case LOCATION_HAND: return CardLocation.Hand;
            case LOCATION_DECK: return CardLocation.Deck;
            case LOCATION_GRAVE: return CardLocation.Graveyard;
            case LOCATION_EXTRA: return CardLocation.ExtraDeck;
            case LOCATION_REMOVED: return CardLocation.Banished;
            case LOCATION_FIELD: return CardLocation.Field;
            default: return CardLocation.Unknown;
        }
    }

    /// <summary>
    /// Converte CardLocation enum para Lua location constant
    /// </summary>
    public static int CardLocationToLuaLocation(CardLocation location)
    {
        switch (location)
        {
            case CardLocation.Hand: return LOCATION_HAND;
            case CardLocation.Deck: return LOCATION_DECK;
            case CardLocation.Graveyard: return LOCATION_GRAVE;
            case CardLocation.ExtraDeck: return LOCATION_EXTRA;
            case CardLocation.Banished: return LOCATION_REMOVED;
            case CardLocation.Field: return LOCATION_FIELD;
            default: return LOCATION_UNKNOWN;
        }
    }

    /// <summary>
    /// Converte Lua position constant para CardDisplay.BattlePosition enum
    /// </summary>
    public static CardDisplay.BattlePosition LuaPositionToBattlePosition(int luaPos, out bool isFaceDown)
    {
        isFaceDown = false;

        switch (luaPos)
        {
            case POS_FACEUP_ATTACK:
                isFaceDown = false;
                return CardDisplay.BattlePosition.Attack;

            case POS_FACEDOWN_ATTACK:
                isFaceDown = true;
                return CardDisplay.BattlePosition.Attack;

            case POS_FACEUP_DEFENSE:
                isFaceDown = false;
                return CardDisplay.BattlePosition.Defense;

            case POS_FACEDOWN_DEFENSE:
                isFaceDown = true;
                return CardDisplay.BattlePosition.Defense;

            default:
                isFaceDown = false;
                return CardDisplay.BattlePosition.Attack;
        }
    }

    /// <summary>
    /// Converte CardDisplay.BattlePosition + isFaceDown para Lua position constant
    /// </summary>
    public static int BattlePositionToLuaPosition(CardDisplay.BattlePosition position, bool isFaceDown)
    {
        if (position == CardDisplay.BattlePosition.Attack)
        {
            return isFaceDown ? POS_FACEDOWN_ATTACK : POS_FACEUP_ATTACK;
        }
        else // Defense
        {
            return isFaceDown ? POS_FACEDOWN_DEFENSE : POS_FACEUP_DEFENSE;
        }
    }

    /// <summary>
    /// Verifica se um tipo de carta possui a flag de tipo especificada
    /// </summary>
    public static bool HasTypeFlag(int typeFlags, int flagToCheck)
    {
        return (typeFlags & flagToCheck) != 0;
    }

    /// <summary>
    /// Mapeia CardData.type (string) para bitflags Lua
    /// </summary>
    public static int CardTypeStringToLuaTypeFlag(string cardTypeString)
    {
        if (string.IsNullOrEmpty(cardTypeString))
            return 0;

        int flags = 0;

        // Base types
        if (cardTypeString.Contains("Spell"))
            flags |= TYPE_SPELL;
        else if (cardTypeString.Contains("Trap"))
            flags |= TYPE_TRAP;
        else if (cardTypeString.Contains("Monster"))
            flags |= TYPE_MONSTER;

        // Synchro/XYZ
        if (cardTypeString.Contains("Synchro"))
            flags |= TYPE_SYNCHRO;
        if (cardTypeString.Contains("XYZ") || cardTypeString.Contains("Xyz"))
            flags |= TYPE_XYZ;
        if (cardTypeString.Contains("Fusion"))
            flags |= TYPE_FUSION;
        if (cardTypeString.Contains("Ritual"))
            flags |= TYPE_RITUAL;
        if (cardTypeString.Contains("Link"))
            flags |= TYPE_LINK;

        return flags;
    }

    /// <summary>
    /// Mapeia bitflags Lua para string descritivo
    /// </summary>
    public static string LuaTypeFlagToString(int typeFlags)
    {
        List<string> types = new List<string>();

        if (HasTypeFlag(typeFlags, TYPE_SYNCHRO))
            types.Add("Synchro");
        else if (HasTypeFlag(typeFlags, TYPE_XYZ))
            types.Add("XYZ");
        else if (HasTypeFlag(typeFlags, TYPE_FUSION))
            types.Add("Fusion");
        else if (HasTypeFlag(typeFlags, TYPE_RITUAL))
            types.Add("Ritual");
        else if (HasTypeFlag(typeFlags, TYPE_MONSTER))
            types.Add("Monster");

        if (HasTypeFlag(typeFlags, TYPE_SPELL))
            types.Add("Spell");
        if (HasTypeFlag(typeFlags, TYPE_TRAP))
            types.Add("Trap");

        return string.Join(" ", types);
    }

    /// <summary>
    /// Debug: printa todas as constantes configuradas
    /// </summary>
    public static void PrintConstants()
    {
        Debug.Log("[LuaConstants] === LOCATION CONSTANTS ===");
        Debug.Log($"[LuaConstants] LOCATION_HAND: 0x{LOCATION_HAND:X}");
        Debug.Log($"[LuaConstants] LOCATION_DECK: 0x{LOCATION_DECK:X}");
        Debug.Log($"[LuaConstants] LOCATION_GRAVE: 0x{LOCATION_GRAVE:X}");
        Debug.Log($"[LuaConstants] LOCATION_EXTRA: 0x{LOCATION_EXTRA:X}");
        Debug.Log($"[LuaConstants] LOCATION_REMOVED: 0x{LOCATION_REMOVED:X}");

        Debug.Log("[LuaConstants] === POSITION CONSTANTS ===");
        Debug.Log($"[LuaConstants] POS_FACEUP_ATTACK: 0x{POS_FACEUP_ATTACK:X}");
        Debug.Log($"[LuaConstants] POS_FACEDOWN_ATTACK: 0x{POS_FACEDOWN_ATTACK:X}");
        Debug.Log($"[LuaConstants] POS_FACEUP_DEFENSE: 0x{POS_FACEUP_DEFENSE:X}");
        Debug.Log($"[LuaConstants] POS_FACEDOWN_DEFENSE: 0x{POS_FACEDOWN_DEFENSE:X}");

        Debug.Log("[LuaConstants] === TYPE CONSTANTS ===");
        Debug.Log($"[LuaConstants] TYPE_MONSTER: 0x{TYPE_MONSTER:X}");
        Debug.Log($"[LuaConstants] TYPE_SPELL: 0x{TYPE_SPELL:X}");
        Debug.Log($"[LuaConstants] TYPE_TRAP: 0x{TYPE_TRAP:X}");
        Debug.Log($"[LuaConstants] TYPE_SYNCHRO: 0x{TYPE_SYNCHRO:X}");
        Debug.Log($"[LuaConstants] TYPE_XYZ: 0x{TYPE_XYZ:X}");
        Debug.Log($"[LuaConstants] TYPE_FUSION: 0x{TYPE_FUSION:X}");
        Debug.Log($"[LuaConstants] TYPE_RITUAL: 0x{TYPE_RITUAL:X}");

        Debug.Log("[LuaConstants] === EVENT CONSTANTS (FASE 12) ===");
        Debug.Log($"[LuaConstants] EVENT_DRAW: {EVENT_DRAW}");
        Debug.Log($"[LuaConstants] EVENT_DESTROY: {EVENT_DESTROY}");
        Debug.Log($"[LuaConstants] EVENT_SUMMON: {EVENT_SUMMON}");
        Debug.Log($"[LuaConstants] EVENT_DAMAGE: {EVENT_DAMAGE}");
        Debug.Log($"[LuaConstants] EVENT_RECOVER: {EVENT_RECOVER}");
        Debug.Log($"[LuaConstants] EVENT_MOVE: {EVENT_MOVE}");
        Debug.Log($"[LuaConstants] EVENT_ACTIVATE: {EVENT_ACTIVATE}");
        Debug.Log($"[LuaConstants] EVENT_CUSTOM: {EVENT_CUSTOM}");

        Debug.Log("[LuaConstants] === EFFECT TYPES (FASE 20) ===");
        Debug.Log($"[LuaConstants] EFFECT_TYPE_IGNITION: 0x{EFFECT_TYPE_IGNITION:X}");
        Debug.Log($"[LuaConstants] EFFECT_TYPE_ACTIVATE: 0x{EFFECT_TYPE_ACTIVATE:X}");
        Debug.Log($"[LuaConstants] EFFECT_TYPE_FLIP: 0x{EFFECT_TYPE_FLIP:X}");
        Debug.Log($"[LuaConstants] EFFECT_TYPE_TRIGGER_F: 0x{EFFECT_TYPE_TRIGGER_F:X}");

        Debug.Log("[LuaConstants] === CATEGORIES (FASE 20) ===");
        Debug.Log($"[LuaConstants] CATEGORY_DRAW: 0x{CATEGORY_DRAW:X}");
        Debug.Log($"[LuaConstants] CATEGORY_DESTROY: 0x{CATEGORY_DESTROY:X}");
        Debug.Log($"[LuaConstants] CATEGORY_SPECIAL_SUMMON: 0x{CATEGORY_SPECIAL_SUMMON:X}");

        Debug.Log("[LuaConstants] === ATTRIBUTES (FASE 20) ===");
        Debug.Log($"[LuaConstants] ATTRIBUTE_EARTH: 0x{ATTRIBUTE_EARTH:X}");
        Debug.Log($"[LuaConstants] ATTRIBUTE_WATER: 0x{ATTRIBUTE_WATER:X}");
        Debug.Log($"[LuaConstants] ATTRIBUTE_FIRE: 0x{ATTRIBUTE_FIRE:X}");
        Debug.Log($"[LuaConstants] ATTRIBUTE_DARK: 0x{ATTRIBUTE_DARK:X}");

        Debug.Log("[LuaConstants] === RACES (FASE 20) ===");
        Debug.Log($"[LuaConstants] RACE_WARRIOR: 0x{RACE_WARRIOR:X}");
        Debug.Log($"[LuaConstants] RACE_SPELLCASTER: 0x{RACE_SPELLCASTER:X}");
        Debug.Log($"[LuaConstants] RACE_DRAGON: 0x{RACE_DRAGON:X}");
    }
}
