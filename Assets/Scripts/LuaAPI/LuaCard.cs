using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

// ==============================================================================
// 2. CLASSE CARD (Representação de uma Carta Física)
// Onde a Mágica Acontece: Intercepta as chamadas de propriedades 'c:' do OCGCore.
// Tratativas Críticas & Dependências: 
// - CardData.cs / CardDisplay.cs: Acessa os status reais do Unity (ATK, DEF, Level).
// - GameManager.cs: Verifica pilhas, zonas e propriedades da partida para filtros.
// ==============================================================================
[MoonSharpUserData]
public class LuaCard
{
    public CardDisplay unityCard;
    public CardData unityData;
    public List<LuaEffect> registeredEffects = new List<LuaEffect>();
    public Dictionary<int, int> flagEffectLabels = new Dictionary<int, int>();
    public Dictionary<int, int> counters = new Dictionary<int, int>();
    public bool isProcComplete = false;

    public LuaCard(CardDisplay card) { 
        unityCard = card; 
        unityData = card?.CurrentCardData; 
        if (unityData != null && CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
            CardEffectManager.Instance.luaDuel.persistentCards[unityData] = this;
    }
    public LuaCard(CardData data) { 
        unityData = data; 
        unityCard = null; 
        if (unityData != null && CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
            CardEffectManager.Instance.luaDuel.persistentCards[unityData] = this;
    }

    private int ConvertToInt(object obj)
    {
        if (obj == null) return 0;
        if (obj is MoonSharp.Interpreter.DynValue dv)
        {
            if (dv.Type == MoonSharp.Interpreter.DataType.Number) return unchecked((int)(long)dv.Number);
            if (dv.Type == MoonSharp.Interpreter.DataType.Boolean) return dv.Boolean ? 1 : 0;
            if (dv.Type == MoonSharp.Interpreter.DataType.String && int.TryParse(dv.String, out int res)) return res;
            return 0;
        }
        if (obj is double d) return unchecked((int)(long)d);
        if (obj is int i) return i;
        if (obj is long l) return unchecked((int)l);
        if (obj is bool b) return b ? 1 : 0;
        if (obj is string s && int.TryParse(s, out int parsed)) return parsed;
        return 0;
    }
    
    private int ProcessSingleBitwiseModifiers(int baseVal, int changeCode, int addCode, int removeCode)
    {
        int finalVal = baseVal;
        if (registeredEffects != null) {
            foreach (var eff in registeredEffects.FindAll(e => e.isTypeSingle && (e.code == changeCode || e.code == addCode || e.code == removeCode))) {
                if (eff.singleRange && (eff.range & GetLocation()) == 0) continue;
                int val = ConvertToInt(eff.GetValue());
                if (eff.code == changeCode) finalVal = val;
                else if (eff.code == addCode) finalVal |= val;
                else if (eff.code == removeCode) finalVal &= ~val;
            }
        }
        return finalVal;
    }

    // Helper para gerar um dummy seguro e evitar crashes de Null Reference no LUA
    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    private Dictionary<int, int> assumedProperties = new Dictionary<int, int>();
    public void AssumeProperty(object assumeCode, object value) { assumedProperties[ConvertToInt(assumeCode)] = ConvertToInt(value); }

    public int GetAttack() { 
        if (assumedProperties.ContainsKey(7)) return assumedProperties[7]; // ASSUME_ATTACK
        return unityCard != null ? unityCard.currentAtk : (unityData != null ? unityData.atk : 0); 
    }
    public int GetDefense() { 
        if (assumedProperties.ContainsKey(8)) return assumedProperties[8]; // ASSUME_DEFENSE
        return unityCard != null ? unityCard.currentDef : (unityData != null ? unityData.def : 0); 
    }
    public int GetLevel() { 
        if (assumedProperties.ContainsKey(3)) return assumedProperties[3]; // ASSUME_LEVEL
        int lvl = unityCard != null ? unityCard.currentLevel : (unityData != null ? unityData.level : 0); 
        if (unityCard == null && registeredEffects != null) {
            foreach (var eff in registeredEffects.FindAll(e => e.isTypeSingle && (e.code == 130 || e.code == 131))) {
                if (eff.singleRange && (eff.range & GetLocation()) == 0) continue;
                int val = ConvertToInt(eff.GetValue());
                if (eff.code == 130) lvl += val;
                else if (eff.code == 131) lvl = val;
            }
        }
        return lvl;
    }
    public int GetOriginalLevel() { return unityCard != null ? unityCard.originalLevel : (unityData != null ? unityData.level : 0); }
    public bool IsFaceup() { return unityCard != null ? !unityCard.isFlipped : false; }
    
    public int ownerPlayerIndex = -1;

    public int GetControler()
    {
        // Se tiver EFFECT_SET_CONTROL ativo, retorna o novo dono!
        var setControlEffs = registeredEffects.FindAll(e => e.code == 4); // EFFECT_SET_CONTROL
        if (setControlEffs.Count > 0)
        {
            var lastEff = setControlEffs.Last();
            object valObj = lastEff.GetValue();
            if (valObj is double || valObj is long || valObj is int) return System.Convert.ToInt32(valObj);
            else if (valObj is MoonSharp.Interpreter.Closure valClosure && CardEffectManager.Instance != null) {
                try {
                    var res = CardEffectManager.Instance.luaEngine.Call(valClosure, lastEff, this);
                    if (res.Type == MoonSharp.Interpreter.DataType.Number) return (int)res.Number;
                } catch {}
            }
        }
        
        if (unityCard != null) return unityCard.isPlayerCard ? 0 : 1;
        if (ownerPlayerIndex != -1) return ownerPlayerIndex;
        return 0;
    }

    public bool IsControler(object playerIndex)
    {
        // Retorna true se o 'tp' repassado no script for o dono atual desta carta física no tabuleiro
        return GetControler() == ConvertToInt(playerIndex);
    }

    public bool IsOnField()
    {
        return unityCard != null && unityCard.isOnField;
    }

    public bool IsLocation(object locationVal) 
    { 
        int loc = ConvertToInt(locationVal);
        if (unityCard != null && unityCard.isOnField)
        {
            if (unityData.type.Contains("Spell") || unityData.type.Contains("Trap")) 
            {
                if ((loc & 0x08) != 0) return true; // SZONE
                if ((loc & 0x100) != 0 && unityCard.transform.parent != null && unityCard.transform.parent.name.Contains("Field")) return true; // FZONE
                if ((loc & 0x400) != 0) return true; // STZONE
            }
            else 
            {
                if ((loc & 0x04) != 0) return true; // MZONE
                if ((loc & 0x800) != 0) return true; // MMZONE
                if ((loc & 0x1000) != 0) return true; // EMZONE
            }
        }
        if (unityCard != null && !unityCard.isOnField) return (loc & 0x02) != 0; // Hand
        
        if (unityData != null)
        {
            if ((loc & 0x10) != 0 && (GameManager.Instance.GetPlayerGraveyard().Contains(unityData) || GameManager.Instance.GetOpponentGraveyard().Contains(unityData))) return true;
            if ((loc & 0x01) != 0 && (GameManager.Instance.GetPlayerMainDeck().Contains(unityData) || GameManager.Instance.GetOpponentMainDeck().Contains(unityData))) return true; // Cobre 0x01, DECKBOT (0x10001) e DECKSHF (0x20001)
            if ((loc & 0x20) != 0 && (GameManager.Instance.GetPlayerRemoved().Contains(unityData) || GameManager.Instance.GetOpponentRemoved().Contains(unityData))) return true;
            if ((loc & 0x40) != 0 && (GameManager.Instance.GetPlayerExtraDeck().Contains(unityData) || GameManager.Instance.GetOpponentExtraDeck().Contains(unityData))) return true;
            if ((loc & 0x80) != 0) return false; // OVERLAY (Evita crashes na validação LUA de Xyz)
        }
        return false;
    }

    // --- PERGUNTAS DE STATUS QUE O SCRIPT FAZ À CARTA ---

    public bool IsDestructable(object e = null) 
    { 
        if (registeredEffects.Exists(eff => eff.code == 40)) return false; // EFFECT_INDESTRUCTABLE
        if (e != null && registeredEffects.Exists(eff => eff.code == 41)) return false; // EFFECT_INDESTRUCTABLE_EFFECT
        
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, e as LuaEffect, "INDESTRUCTABLE", CardLocation.Field)) return false;
            if (e != null && CardEffectManager.Instance.auraManager.IsUnderRestriction(this, e as LuaEffect, "INDESTRUCTABLE_EFFECT", CardLocation.Field)) return false;
        }
        return true; 
    }

    public bool IsIndestructableByBattle()
    {
        if (registeredEffects.Exists(e => e.code == 40 || e.code == 42)) return true; // 40 = INDESTRUCTABLE, 42 = INDESTRUCTABLE_BATTLE
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "INDESTRUCTABLE", CardLocation.Field)) return true;
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "INDESTRUCTABLE_BATTLE", CardLocation.Field)) return true;
        }
        return false;
    }

    public bool IsSpellTrap()
    {
        if (unityData == null) return false;
        return unityData.type.Contains("Spell") || unityData.type.Contains("Trap");
    }

    public new int GetType()
    {
        if (assumedProperties.ContainsKey(2)) return assumedProperties[2]; // ASSUME_TYPE
        if (unityData == null || string.IsNullOrEmpty(unityData.type)) return 0;
        int t = 0;
        
        // Usa o typeline completo da API se existir (Para suportar Tuner, Synchro, etc), senão usa o type básico da UI
        string typeStr = (!string.IsNullOrEmpty(unityData.typeline) ? unityData.typeline : unityData.type).Trim().ToUpperInvariant();
        string propStr = unityData.property != null ? unityData.property.Trim().ToUpperInvariant() : "";

        if (typeStr.Contains("MONSTER")) t |= 0x1;
        if (typeStr.Contains("SPELL")) t |= 0x2;
        if (typeStr.Contains("TRAP")) t |= 0x4;
        if (typeStr.Contains("NORMAL")) t |= 0x10;
        if (typeStr.Contains("EFFECT")) t |= 0x20;
        if (typeStr.Contains("FUSION")) t |= 0x40;
        if (typeStr.Contains("RITUAL")) t |= 0x80;
        if (typeStr.Contains("SPIRIT")) t |= 0x200;
        if (typeStr.Contains("UNION")) t |= 0x400;
        if (typeStr.Contains("GEMINI")) t |= 0x800;
        if (typeStr.Contains("TOKEN")) t |= 0x4000;
        if (propStr == "QUICK-PLAY") t |= 0x10000;
        if (propStr == "CONTINUOUS") t |= 0x20000;
        if (propStr == "EQUIP") t |= 0x40000;
        if (propStr == "FIELD") t |= 0x80000;
        if (propStr == "COUNTER") t |= 0x100000;
        if (typeStr.Contains("TOON")) t |= 0x400000;
        
        t = ProcessSingleBitwiseModifiers(t, 117, 115, 116);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            t = CardEffectManager.Instance.auraManager.ProcessBitwiseModifiers(this, t, "CHANGE_TYPE", "ADD_TYPE", "REMOVE_TYPE", (CardLocation)GetLocation());
        }
        return t;
    }

    public int GetOriginalRace() { return GetRace(); }
    public int GetOriginalAttribute() { return GetAttribute(); }
    public int GetAttribute() { 
        if (assumedProperties.ContainsKey(5)) return assumedProperties[5]; // ASSUME_ATTRIBUTE
        if (unityData == null || string.IsNullOrEmpty(unityData.attribute)) return 0;
        int attr = 0;
        string a = unityData.attribute.Trim().ToUpperInvariant();
        if (a.Contains("EARTH")) attr |= 0x01;
        if (a.Contains("WATER")) attr |= 0x02;
        if (a.Contains("FIRE")) attr |= 0x04;
        if (a.Contains("WIND")) attr |= 0x08;
        if (a.Contains("DARK")) attr |= 0x10;
        if (a.Contains("LIGHT")) attr |= 0x20;
        if (a.Contains("DIVINE")) attr |= 0x40;
        
        attr = ProcessSingleBitwiseModifiers(attr, 127, 125, 126);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            attr = CardEffectManager.Instance.auraManager.ProcessBitwiseModifiers(this, attr, "CHANGE_ATTRIBUTE", "ADD_ATTRIBUTE", "REMOVE_ATTRIBUTE", (CardLocation)GetLocation());
        }
        return attr;
    }
    public int GetTextAttack() { return GetAttack(); }
    public int GetTextDefense() { return GetDefense(); }

    public bool IsType(object t) { 
        // Debug.Log($"<color=magenta>[IsType]</color> O LUA perguntou se '{unityData?.name}' possui o Tipo (código {ConvertToInt(t)})");
        return (GetType() & ConvertToInt(t)) != 0; 
    }
    
    public bool IsTrap() { return unityData != null && unityData.type.Contains("Trap"); }
    public bool IsSpell() { return unityData != null && unityData.type.Contains("Spell"); }
    public bool IsMonster() { return unityData != null && unityData.type.Contains("Monster"); }

    // Novos Stubs Descobertos pelo Mass Validator
    public CardLocation previousLocation = CardLocation.Unknown;
    public int currentReason = 0;
    public int reasonPlayer = -1;
    public LuaEffect reasonEffect = null;
    
    public bool IsPreviousLocation(object loc) 
    { 
        int l = ConvertToInt(loc);
        if (previousLocation != CardLocation.Unknown)
        {
            if ((l & 0x08) != 0 && previousLocation == CardLocation.Field) return true; // SZONE
            if ((l & 0x04) != 0 && previousLocation == CardLocation.Field) return true; // MZONE
            if ((l & 0x0C) != 0 && previousLocation == CardLocation.Field) return true; // ONFIELD
            if ((l & 0x02) != 0 && previousLocation == CardLocation.Hand) return true;
            if ((l & 0x01) != 0 && previousLocation == CardLocation.Deck) return true;
            if ((l & 0x10) != 0 && previousLocation == CardLocation.Graveyard) return true;
            if ((l & 0x20) != 0 && previousLocation == CardLocation.Banished) return true;
            return false;
        }
        return true; 
    }
    public LuaCard GetBattleTarget() { return SafeDummyCard(); }
    public bool IsDiscardable(params object[] args) { return true; }
    
    public LuaCard _lastEquipTarget = null;

    public LuaCard GetEquipTarget() 
    { 
        if (CardEffectManager.Instance != null && unityCard != null)
        {
            CardDisplay target = CardEffectManager.Instance.GetEquipTarget(unityCard);
            if (target != null) {
                _lastEquipTarget = CardEffectManager.Instance.EnsureCardScriptLoaded(target);
                return _lastEquipTarget;
            }
        }
        else if (CardEffectManager.Instance != null && unityData != null)
        {
            // Fallback Extremo: Se a carta visual já morreu, caçamos o Elo Invisível (CardLink) ainda pendente na RAM!
            CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(UnityEngine.FindObjectsSortMode.None);
            foreach(var l in links) {
                if (l.source != null && l.source.CurrentCardData == unityData && l.target != null) {
                    _lastEquipTarget = CardEffectManager.Instance.EnsureCardScriptLoaded(l.target);
                    return _lastEquipTarget;
                }
            }
        }
        return _lastEquipTarget; 
    }

    public bool IsAbleToGraveAsCost() { return true; }
    public bool IsAbleToRemoveAsCost() { return true; }
    public bool IsAbleToDeck() 
    { 
        if (registeredEffects.Exists(e => e.code == 66)) return false; // EFFECT_CANNOT_TO_DECK
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_TO_DECK", CardLocation.Field | CardLocation.Graveyard | CardLocation.Hand)) return false;
        }
        return true; 
    }
    public bool IsLevelBelow(object lvl) { return GetLevel() <= ConvertToInt(lvl); }
    public bool IsHasType(object type) { return IsType(type); }
    public int GetAttackAnnouncedCount() { return 0; }
    public int GetReasonPlayer() { return reasonPlayer != -1 ? reasonPlayer : GetControler(); }
    public LuaEffect GetReasonEffect() { return reasonEffect; }
    public int GetCounter(object counterType) { return 0; }
    public bool IsFacedown() { return unityCard != null ? unityCard.isFlipped : false; }
    public bool IsTributeSummoned() { return false; }
    public int GetPreviousPosition() { return GetBattlePosition(); }
    public int GetPreviousCodeOnField() { return GetCode(); }
    public int GetPreviousRaceOnField() { return GetRace(); }
    public bool IsCanBeEffectTarget(object e) 
    { 
        LuaEffect eff = e as LuaEffect;
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            // Se estiver sob restrição de "CANNOT_BE_EFFECT_TARGET", a Engine Unity a torna invisível para cliques do LUA!
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, eff, "CANNOT_BE_EFFECT_TARGET", CardLocation.Field)) return false;
        }
        return true; 
    }
    public bool IsSummonType(object sumtype) { return (GetSummonType() & ConvertToInt(sumtype)) == ConvertToInt(sumtype); }
    public bool IsCanRemoveCounter(object player, object counterType, object count, object reason) { return true; }
    public bool IsPreviousRaceOnField(object race) { return true; }
    public bool IsRelateToCard(object card) { return true; }
    public bool CanSummonOrSet(object ignoreLimit, object param) { return true; }
    public LuaCard GetOwner() { return this; }
    public int GetPreviousControler() { return GetControler(); }
    public void AddCounter(object counterType, object count) 
    { 
        int type = ConvertToInt(counterType);
        int amt = ConvertToInt(count);
        if (counters.ContainsKey(type)) counters[type] += amt;
        else counters[type] = amt;
        Debug.Log($"<color=cyan>[Contadores]</color> Adicionado {amt} contador(es) do tipo {type} em {unityData?.name}. Total: {counters[type]}");
        if (GameManager.Instance != null) GameManager.Instance.RefreshAllCardsVisuals();
    }
    public void RemoveCounter(object player, object counterType, object count, object reason) 
    { 
        int type = ConvertToInt(counterType);
        int amt = ConvertToInt(count);
        if (counters.ContainsKey(type)) {
            counters[type] -= amt;
            if (counters[type] < 0) counters[type] = 0;
            Debug.Log($"<color=cyan>[Contadores]</color> Removido {amt} contador(es) do tipo {type} em {unityData?.name}. Restam: {counters[type]}");
            if (GameManager.Instance != null) GameManager.Instance.RefreshAllCardsVisuals();
        }
    }
    public bool IsCanAddCounter(object counterType, object count) { return true; }
    public int GetFieldID() { return 0; }
    public LuaGroup GetTarget() { return new LuaGroup(); }
    public void SetMaterial(object g) { Debug.LogWarning($"[LUA STUB] SetMaterial chamado em {unityData?.name}"); }
    public LuaGroup GetAdminGroup() { return new LuaGroup(); }
    public bool IsSummonLocation(object loc) { return true; }
    public void SetStatus(object status, object enable) 
    { 
        if (unityCard == null) return;
        int s = ConvertToInt(status);
        bool en = ConvertToInt(enable) != 0;
        
        if (en) unityCard.AddStatus(s);
        else unityCard.RemoveStatus(s);
    }
    public int GetCounter(object counterType) 
    { 
        int type = ConvertToInt(counterType);
        return counters.ContainsKey(type) ? counters[type] : 0;
    }
    
    public int GetRace() 
    { 
        if (assumedProperties.ContainsKey(6)) return assumedProperties[6]; // ASSUME_RACE
        if (unityData == null || string.IsNullOrEmpty(unityData.race)) return 0;
        int rCode = 0;
        string r = unityData.race.Trim().ToUpperInvariant();
        if (r.Contains("WARRIOR")) rCode |= 0x1;
        if (r.Contains("SPELLCASTER")) rCode |= 0x2;
        if (r.Contains("FAIRY")) rCode |= 0x4;
        if (r.Contains("FIEND")) rCode |= 0x8;
        if (r.Contains("ZOMBIE")) rCode |= 0x10;
        if (r.Contains("MACHINE")) rCode |= 0x20;
        if (r.Contains("AQUA")) rCode |= 0x40;
        if (r.Contains("PYRO")) rCode |= 0x80;
        if (r.Contains("ROCK")) rCode |= 0x100;
        if (r.Contains("WINGED BEAST")) rCode |= 0x200;
        if (r.Contains("PLANT")) rCode |= 0x400;
        if (r.Contains("INSECT")) rCode |= 0x800;
        if (r.Contains("THUNDER")) rCode |= 0x1000;
        if (r.Contains("DRAGON")) rCode |= 0x2000;
        if (r.Contains("BEAST-WARRIOR")) rCode |= 0x8000;
        else if (r.Contains("BEAST")) rCode |= 0x4000;
        if (r.Contains("DINOSAUR")) rCode |= 0x10000;
        if (r.Contains("FISH")) rCode |= 0x20000;
        if (r.Contains("SEA SERPENT")) rCode |= 0x40000;
        if (r.Contains("REPTILE")) rCode |= 0x80000;
        
        rCode = ProcessSingleBitwiseModifiers(rCode, 122, 120, 121);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            rCode = CardEffectManager.Instance.auraManager.ProcessBitwiseModifiers(this, rCode, "CHANGE_RACE", "ADD_RACE", "REMOVE_RACE", (CardLocation)GetLocation());
        }
        return rCode;
    }
    public int GetMaterialCount() { return 0; }
    public bool IsAbleToDeckAsCost() { return IsAbleToDeck(); }
    public bool IsSummonPlayer(object player) { return true; }
    public LuaGroup GetAttackableTarget() { return new LuaGroup(); }
    public bool IsAbleToChangeControler() { return true; }
    public bool IsSummonableCard() { return true; }
    public LuaGroup GetMaterial() { return new LuaGroup(); }
    public int GetEffectCount(object code) 
    { 
        int targetCode = ConvertToInt(code);
        return registeredEffects.Count(e => e.code == targetCode); 
    }
    
    public int GetBaseAttack() 
    { 
        int baseAtk = unityCard != null ? unityCard.originalAtk : (unityData != null ? unityData.atk : 0); 
        if (registeredEffects != null) {
            var effs = registeredEffects.FindAll(e => e.isTypeSingle && e.code == 103); // 103 = EFFECT_SET_BASE_ATTACK
            foreach (var eff in effs) {
                if (eff.singleRange && (eff.range & GetLocation()) == 0) continue;
                object valObj = eff.GetValue();
                if (valObj is double || valObj is long) baseAtk = System.Convert.ToInt32(valObj);
            }
        }
        return baseAtk;
    }
    public int GetBaseDefense() 
    { 
        int baseDef = unityCard != null ? unityCard.originalDef : (unityData != null ? unityData.def : 0); 
        if (registeredEffects != null) {
            var effs = registeredEffects.FindAll(e => e.isTypeSingle && e.code == 107); // 107 = EFFECT_SET_BASE_DEFENSE
            foreach (var eff in effs) {
                if (eff.singleRange && (eff.range & GetLocation()) == 0) continue;
                object valObj = eff.GetValue();
                if (valObj is double || valObj is long) baseDef = System.Convert.ToInt32(valObj);
            }
        }
        return baseDef;
    }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return unityCard != null ? unityCard.summonType : 0; }
    public int GetPreviousLocation() { return 0; }

    public bool CheckFusionMaterial(object group = null, object card = null, object chkf = null) { return true; }
    public bool IsCanBeFusionMaterial(object card = null) 
    { 
        if (registeredEffects.Exists(e => e.code == 235)) return false; // EFFECT_CANNOT_BE_FUSION_MATERIAL
        return true; 
    }
    public bool IsCanBeSynchroMaterial(object card = null) 
    { 
        if (registeredEffects.Exists(e => e.code == 236)) return false; // EFFECT_CANNOT_BE_SYNCHRO_MATERIAL
        // [FUTURO] Implementar lógica avançada de Synchro Summon (Tuner, Níveis)
        return true; 
    }
    public bool IsCanBeXyzMaterial(object card = null) 
    { 
        if (registeredEffects.Exists(e => e.code == 238)) return false; // EFFECT_CANNOT_BE_XYZ_MATERIAL
        // [FUTURO] Implementar lógica avançada de Xyz Summon (Ranks, Overlays)
        return true; 
    }
    public bool IsCanBeLinkMaterial(object card = null) 
    { 
        if (registeredEffects.Exists(e => e.code == 239)) return false; // EFFECT_CANNOT_BE_LINK_MATERIAL
        // [FUTURO] Implementar lógica avançada de Link Summon (Link Markers, Ratings)
        return true; 
    }

    public bool IsCanBeRitualMaterial(object card = null) { 
        if (registeredEffects.Exists(e => e.code == 248)) return false; // EFFECT_CANNOT_BE_MATERIAL
        return true; 
    }
    public int GetRitualLevel(object rc = null) { 
        var eff = registeredEffects.Find(e => e.code == 241); // EFFECT_RITUAL_LEVEL
        if (eff != null) return ConvertToInt(eff.GetValue());
        return GetLevel(); 
    }
    public int GetSynchroLevel(object rc = null) { 
        var eff = registeredEffects.Find(e => e.code == 240); // EFFECT_SYNCHRO_LEVEL
        if (eff != null) return ConvertToInt(eff.GetValue());
        // [FUTURO] Retornar Múltiplos Níveis se o monstro tiver a propriedade (ex: Nível 3 e 4)
        return GetLevel(); 
    }
    public int GetXyzLevel(object rc = null) { 
        var eff = registeredEffects.Find(e => e.code == 242); // EFFECT_XYZ_LEVEL
        if (eff != null) return ConvertToInt(eff.GetValue());
        // [FUTURO] Retornar Múltiplos Níveis
        return GetLevel(); 
    }
    
    public LuaCard GetHandler() { return this; }
    public int GetCardTargetCount() { return 0; }
    public bool IsOriginalCodeRule(params object[] codes) { return IsCode(codes); }
    public bool IsFieldSpell() { return IsType(0x80000); }
    public int GetLocation() { return unityCard != null && unityCard.isOnField ? (unityData.type.Contains("Spell") || unityData.type.Contains("Trap") ? 0x08 : 0x04) : 0x02; }
    public int GetDestination() { return 0; }
    public int GetLeaveFieldDest() { return 0; }
    public LuaGroup GetColumnGroup() { return new LuaGroup(); } // [FUTURO] Importante para Link Monsters
    public LuaGroup GetOverlayGroup() { return new LuaGroup(); } // [FUTURO] Importante para Xyz Monsters (Materiais acoplados)
    public int GetOverlayCount() { return 0; } // [FUTURO] Importante para Xyz Monsters
    public LuaGroup GetCardTarget() { return new LuaGroup(); }
    public bool HasNonZeroAttack() { return GetAttack() > 0; }
    public bool HasNonZeroDefense() { return GetDefense() > 0; }
    public bool IsCanChangePosition() { return true; }
    public int GetTurnID() { return 0; }
    public bool CanChainAttack() { return true; }
    public LuaCard GetPreviousEquipTarget() { return _lastEquipTarget; }
    public bool IsAbleToGrave() 
    { 
        if (registeredEffects.Exists(e => e.code == 68)) return false; // EFFECT_CANNOT_TO_GRAVE
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_TO_GRAVE", CardLocation.Field | CardLocation.Hand | CardLocation.Deck | CardLocation.Banished)) return false;
        }
        return true; 
    }    
    public bool HasFlagEffect(object id) { return false; }
    public int GetTurnCounter() { return unityCard != null ? unityCard.turnCounter : 0; }
    public bool IsHasCardTarget(object c) { return false; }
    public LuaCard GetReasonCard() { return SafeDummyCard(); }
    public void CompleteProcedure() 
    { 
        isProcComplete = true;
        if (unityCard != null) unityCard.AddStatus(0x8); // STATUS_PROC_COMPLETE
        Debug.Log($"<color=green>[Proc Complete]</color> {unityData?.name} concluiu seu procedimento de invocação oficial!");
    }
    public void CancelToGrave(params object[] args) { Debug.LogWarning($"[LUA STUB] CancelToGrave chamado em {unityData?.name}"); }
    public bool IsLevelAbove(object lvl) { return GetLevel() >= ConvertToInt(lvl); }
    public int GetBattledGroupCount() { return 0; }
    public bool IsRitualMonster() { return unityData != null && unityData.type.Contains("Ritual"); }
    public bool IsRitualSpell() { return unityData != null && unityData.type.Contains("Spell") && unityData.property == "Ritual"; }
    public bool IsPublic() { return true; }
    public int GetOwnerTargetCount() { return 0; }    public bool IsFusionSummoned() { return false; }
    public bool IsDefenseBelow(object def) { return GetDefense() <= ConvertToInt(def); }
    public bool IsAttributeExcept(object attr) { return !IsAttribute(attr); }
    public int GetBattlePosition() { 
        if (unityCard == null) return 0;
        if (unityCard.position == CardDisplay.BattlePosition.Attack) return unityCard.isFlipped ? 0x2 : 0x1; // 0x2 = Face-Down Attack (Raro), 0x1 = Face-Up Attack
        return unityCard.isFlipped ? 0x8 : 0x4; // 0x8 = Face-Down Defense, 0x4 = Face-Up Defense
    }
    public bool IsRelateToBattle() { return true; }
    public void DeleteGroup() { Debug.LogWarning($"[LUA STUB] DeleteGroup chamado em {unityData?.name}"); }

    // Stubs para compatibilidade da API Lua
    public bool IsRace(object r) { 
        int targetRace = ConvertToInt(r);
        int myRace = GetRace();
        bool passed = (myRace & targetRace) != 0;
        
        if (unityCard != null && unityCard.isOnField && unityData != null && unityData.type.Contains("Monster")) {
            Debug.Log($"<color=cyan>[IsRace LOG]</color> Avaliando {unityData.name} | Raça do Monstro: {myRace} | Raça Alvo (Da Magia): {targetRace} -> Combina? {passed}");
        }
        return passed; 
    }
    public bool IsAttribute(object attr) { 
        // Debug.Log($"<color=magenta>[IsAttribute]</color> O LUA perguntou se '{unityData?.name}' possui o Atributo (código {ConvertToInt(attr)})");
        return (GetAttribute() & ConvertToInt(attr)) != 0; 
    }
    public bool IsReason(object reason) { 
        return (currentReason & ConvertToInt(reason)) != 0; 
    }
    public int GetReason() { return currentReason; }
    public bool IsRelateToEffect(object e) { return true; } // Evita crash no final de correntes (Chains)
    public bool IsAttackBelow(object atk) { return GetAttack() <= ConvertToInt(atk); }
    public bool IsAttackAbove(object atk) { return GetAttack() >= ConvertToInt(atk); }
    public bool IsDefenseAbove(object def) { return GetDefense() >= ConvertToInt(def); }
    public int GetAttackedCount() { return 0; }
    public bool IsCanTurnSet() 
    { 
        if (!IsFaceup()) return false; // Já está virado para baixo
        if (IsType(0x4000)) return false; // TYPE_TOKEN (Tokens não podem ficar face-down)
        if (IsType(0x4000000)) return false; // TYPE_LINK (Links não existem no DM, mas previne bugs futuros)
        if (registeredEffects.Exists(e => e.code == 69)) return false; // EFFECT_CANNOT_TURN_SET
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_TURN_SET", CardLocation.Field)) return false;
        }
        return true; 
    }
    public bool IsCanBeSpecialSummoned(object e, object sumtype, object sumplayer, object nocheck, object nolimit, params object[] extraArgs) 
    { 
        bool ignoreLimit = ConvertToInt(nolimit) != 0;
        
        if (!ignoreLimit)
        {
            bool hasReviveLimit = registeredEffects.Exists(eff => eff.code == 31); // EFFECT_REVIVE_LIMIT
            if (hasReviveLimit)
            {
                int loc = GetLocation();
                if ((loc & 0x30) != 0) // LOCATION_GRAVE | LOCATION_REMOVED
                {
                    if (!isProcComplete) return false; // Bloqueado, precisa ter o Certificado de Nascimento!
                }
                else if ((loc & 0x43) != 0) // LOCATION_DECK | LOCATION_HAND | LOCATION_EXTRA
                {
                    // Monstros no Deck/Mão/Extra com Revive Limit NUNCA podem ser invocados genericamente
                    return false;
                }
            }
        }
        return true; 
    }
    public bool IsReleasable() 
    { 
        if (registeredEffects.Exists(e => e.code == 46 || e.code == 43 || e.code == 44 || e.code == 48)) return false; // EFFECT_CANNOT_RELEASE
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_RELEASE", CardLocation.Field)) return false;
        }
        return true; 
    }
    public bool IsPreviousControler(object p) { return true; }
    public bool IsAbleToHand() 
    { 
        if (registeredEffects.Exists(e => e.code == 65)) return false; // EFFECT_CANNOT_TO_HAND
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_TO_HAND", CardLocation.Field | CardLocation.Graveyard | CardLocation.Deck)) return false;
        }
        return true; 
    }
    public bool IsPreviousPosition(object pos) { return true; }
    public bool IsDefensePos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense; }
    public bool IsAttackPos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Attack; }
    public bool IsPosition(object pos) { 
        int p = ConvertToInt(pos);
        return (GetBattlePosition() & p) != 0;
    }
    public bool IsSummonable(object ignoreLimit, object param) { return true; }
    public bool IsMSetable(object ignoreLimit, object param) { return true; }
    public bool IsSSetable(params object[] args) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    
    private string GetCardUniqueKey()
    {
        if (unityCard != null) return "GO_" + unityCard.gameObject.GetInstanceID().ToString();
        if (unityData != null) return "DATA_" + unityData.GetHashCode().ToString();
        return "UNKNOWN";
    }

    public int GetFlagEffect(object id) 
    { 
        int flagId = ConvertToInt(id);
        int result = 0;
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            string key = GetCardUniqueKey();
            if (CardEffectManager.Instance.luaDuel.cardFlags.ContainsKey(key))
                result = CardEffectManager.Instance.luaDuel.cardFlags[key].ContainsKey(flagId) ? CardEffectManager.Instance.luaDuel.cardFlags[key][flagId] : 0;
        }
        return result;
    }
    
    public bool IsImmuneToEffect(object e) 
    { 
        LuaEffect eff = e as LuaEffect;
        if (eff != null && eff.ignoreImmune) return false; // EFFECT_FLAG_IGNORE_IMMUNE: Fura as defesas

        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            return CardEffectManager.Instance.auraManager.IsImmuneTo(this, eff);
        }
        return false; 
    }
    public bool CanAttack() { return true; }
    public bool IsDisabled() { 
        if (IsHasEffect(3).Type != DataType.Nil) return false; // EFFECT_CANNOT_DISABLE
        if (IsStatus(0x1)) return true; // STATUS_DISABLED
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            return CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "DISABLE", CardLocation.Field);
        }
        return false; 
    }
    public bool IsForbidden() { return false; }
    public bool IsSpecialSummoned() { return false; }
    public bool HasLevel() { return GetLevel() > 0; }

    public int GetCode() {
        if (assumedProperties.ContainsKey(1)) return assumedProperties[1]; // ASSUME_CODE
        if (unityData == null) return 0;
        int cCode = 0;
        if (!string.IsNullOrEmpty(unityData.password) && int.TryParse(unityData.password, out int code)) cCode = code;
        else {
            string digits = System.Text.RegularExpressions.Regex.Replace(unityData.id, @"\D", "");
            if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out int fallbackCode)) cCode = fallbackCode;
        }
        
        cCode = ProcessSingleBitwiseModifiers(cCode, 114, 113, 118);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            cCode = CardEffectManager.Instance.auraManager.ProcessBitwiseModifiers(this, cCode, "CHANGE_CODE", "ADD_CODE", "REMOVE_CODE", (CardLocation)GetLocation());
        }
        return cCode;
    }
    public int GetOriginalCode() { return GetCode(); }
    
    public void RegisterFlagEffect(params object[] args) 
    { 
        if (args == null || args.Length == 0) return;
        int flagId = ConvertToInt(args[0]);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            string key = GetCardUniqueKey();
            if (!CardEffectManager.Instance.luaDuel.cardFlags.ContainsKey(key)) CardEffectManager.Instance.luaDuel.cardFlags[key] = new Dictionary<int, int>();
            if (CardEffectManager.Instance.luaDuel.cardFlags[key].ContainsKey(flagId)) CardEffectManager.Instance.luaDuel.cardFlags[key][flagId]++;
            else CardEffectManager.Instance.luaDuel.cardFlags[key][flagId] = 1;
        }
    }

    public void SetCardTarget(object tc) { Debug.LogWarning($"[LUA STUB] SetCardTarget chamado em {unityData?.name}"); }
    
    public bool IsStatus(object status) 
    { 
        int s = ConvertToInt(status);
        if (s == 0x8 && isProcComplete) return true;
        if (unityCard != null) return unityCard.HasStatus(s);
        return false; 
    }
    
    public LuaCard GetFirstCardTarget() 
    { 
        // Emulamos GetFirstCardTarget para Equip Spells retornando o alvo do equipamento
        if (unityData != null && (unityData.type.Contains("Equip") || unityData.property == "Equip"))
            return GetEquipTarget();
            
        // Fallback genérico de alvo para o Elo atual da Corrente
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.chainManager != null && CardEffectManager.Instance.chainManager.resolvingLink != null)
        {
            var grp = CardEffectManager.Instance.chainManager.resolvingLink.targetGroup;
            if (grp != null && grp.cards.Count > 0) return grp.cards[0];
        }

        return SafeDummyCard(); 
    }

    public void SetTurnCounter(object ct) { Debug.LogWarning($"[LUA STUB] SetTurnCounter chamado em {unityData?.name}"); }
    public int GetLabel() { return 0; }
    public void SetLabel(object ct) { Debug.LogWarning($"[LUA STUB] SetLabel chamado em {unityData?.name}"); }
    
    public void EnableReviveLimit() 
    { 
        // Adiciona a restrição oficial (EFFECT_REVIVE_LIMIT = 31)
        LuaEffect e = new LuaEffect { owner = this, type = 0x0001, code = 31, property = 0x40000 }; // 0x40000 = EFFECT_FLAG_UNCOPYABLE
        registeredEffects.Add(e);
    }
    public void SetUniqueOnField(params object[] args) { Debug.LogWarning($"[LUA STUB] SetUniqueOnField chamado em {unityData?.name}"); }
    public void EnableCounterPermit(params object[] args) { Debug.LogWarning($"[LUA STUB] EnableCounterPermit chamado em {unityData?.name}"); }
    public void SetCounterLimit(params object[] args) { Debug.LogWarning($"[LUA STUB] SetCounterLimit chamado em {unityData?.name}"); }
    public bool IsAbleToRemove() 
    { 
        if (registeredEffects.Exists(e => e.code == 67)) return false; // EFFECT_CANNOT_REMOVE
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(this, null, "CANNOT_REMOVE", CardLocation.Field | CardLocation.Graveyard | CardLocation.Hand | CardLocation.Deck)) return false;
        }
        return true; 
    }
    public void AddMustBeSpecialSummoned(params object[] args) { Debug.LogWarning($"[LUA STUB] AddMustBeSpecialSummoned chamado em {unityData?.name}"); }
    public void EnableUnsummonable() { Debug.LogWarning($"[LUA STUB] EnableUnsummonable chamado em {unityData?.name}"); }
    public void SetSPSummonOnce(params object[] args) { Debug.LogWarning($"[LUA STUB] SetSPSummonOnce chamado em {unityData?.name}"); }
    public DynValue IsHasEffect(object effectCode) 
    { 
        int code = ConvertToInt(effectCode);
        var effects = registeredEffects.FindAll(e => e.code == code);
        
        // Integração OCGCore: Busca por Auras Globais concedidas por Mágicas de Campo/Contínuas
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
        {
            var globalAuras = CardEffectManager.Instance.auraManager.GetEffectsTargetingCard(this, code, (CardLocation)GetLocation());
            effects.AddRange(globalAuras);
        }

        if (effects.Count > 0)
        {
            return DynValue.NewTuple(effects.Select(e => UserData.Create(e)).ToArray());
        }
        return DynValue.Nil; 
    }
    
    public void ResetFlagEffect(object id) 
    { 
        int flagId = ConvertToInt(id);
        if (flagEffectLabels.ContainsKey(flagId)) flagEffectLabels.Remove(flagId);
        
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            string key = GetCardUniqueKey();
            if (CardEffectManager.Instance.luaDuel.cardFlags.ContainsKey(key)) CardEffectManager.Instance.luaDuel.cardFlags[key].Remove(flagId);
        }
    }

    public void SetFlagEffectLabel(object id, object label) 
    { 
        flagEffectLabels[ConvertToInt(id)] = ConvertToInt(label); 
    }

    public DynValue GetFlagEffectLabel(object id) 
    { 
        int key = ConvertToInt(id);
        if (flagEffectLabels.ContainsKey(key)) return DynValue.NewNumber(flagEffectLabels[key]);
        return DynValue.Nil;
    }

    public void CreateEffectRelation(object e) { Debug.LogWarning($"[LUA STUB] CreateEffectRelation chamado em {unityData?.name}"); }
    public void ReleaseEffectRelation(object e) { Debug.LogWarning($"[LUA STUB] ReleaseEffectRelation chamado em {unityData?.name}"); }
    public void ClearEffectRelation() { Debug.LogWarning($"[LUA STUB] ClearEffectRelation chamado em {unityData?.name}"); }
    
    public void ReverseInDeck() 
    { 
        Debug.Log($"<color=green>[Parasite Paracide]</color> {unityData?.name} ativou ReverseInDeck (Ficará virado para cima no baralho)!");
        // [FUTURO] Adicionar flag na UI do Deck para mostrar esta carta caso o jogador visualize o Deck.
    }
    
    public void SetHint(params object[] args) 
    { 
        if (args == null || args.Length < 2 || unityCard == null) return;
        int type = ConvertToInt(args[0]);
        int value = ConvertToInt(args[1]);
        
        Debug.Log($"<color=magenta>[LuaCard LOG]</color> SetHint chamado! Tipo: {type}, Valor: {value}");
        
        string hintText = "";
        
        if (type == 1) // CHINT_TURN
        {
            unityCard.SetTurnHintVisual(value);
        }
        if (type == 3) // CHINT_RACE
        {
            List<string> races = new List<string>();
            if ((value & 0x1) != 0) races.Add("Warrior");
            if ((value & 0x2) != 0) races.Add("Spellcaster");
            if ((value & 0x4) != 0) races.Add("Fairy");
            if ((value & 0x8) != 0) races.Add("Fiend");
            if ((value & 0x10) != 0) races.Add("Zombie");
            if ((value & 0x20) != 0) races.Add("Machine");
            if ((value & 0x40) != 0) races.Add("Aqua");
            if ((value & 0x80) != 0) races.Add("Pyro");
            if ((value & 0x100) != 0) races.Add("Rock");
            if ((value & 0x200) != 0) races.Add("Winged Beast");
            if ((value & 0x400) != 0) races.Add("Plant");
            if ((value & 0x800) != 0) races.Add("Insect");
            if ((value & 0x1000) != 0) races.Add("Thunder");
            if ((value & 0x2000) != 0) races.Add("Dragon");
            if ((value & 0x4000) != 0) races.Add("Beast");
            if ((value & 0x8000) != 0) races.Add("Beast-Warrior");
            if ((value & 0x10000) != 0) races.Add("Dinosaur");
            if ((value & 0x20000) != 0) races.Add("Fish");
            if ((value & 0x40000) != 0) races.Add("Sea Serpent");
            if ((value & 0x80000) != 0) races.Add("Reptile");
            hintText = "Declared Race: " + string.Join(", ", races);
        }
        else if (type == 4) // CHINT_ATTRIBUTE
        {
            List<string> attrs = new List<string>();
            if ((value & 0x1) != 0) attrs.Add("EARTH");
            if ((value & 0x2) != 0) attrs.Add("WATER");
            if ((value & 0x4) != 0) attrs.Add("FIRE");
            if ((value & 0x8) != 0) attrs.Add("WIND");
            if ((value & 0x10) != 0) attrs.Add("DARK");
            if ((value & 0x20) != 0) attrs.Add("LIGHT");
            if ((value & 0x40) != 0) attrs.Add("DIVINE");
            hintText = "Declared Attribute: " + string.Join(", ", attrs);
        }
        else if (type == 5) // CHINT_NUMBER
        {
            hintText = "Declared Number: " + value;
        }
        else if (type == 6) // CHINT_DESC_ADD
        {
            int cardId = value / 16;
            int strIdx = value % 16;
            string cardName = "Card";
            if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
            {
                var cData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.password == cardId.ToString() || c.id == cardId.ToString());
                if (cData != null) cardName = cData.name;
            }
            hintText = $"Active Effect: {cardName} ({strIdx + 1})";
        }
        else if (type == 7) // CHINT_DESC_REMOVE
        {
            unityCard.clientHintText = "";
            if (GameManager.Instance != null) GameManager.Instance.RefreshAllCardsVisuals();
            return;
        }

        if (!string.IsNullOrEmpty(hintText))
        {
            unityCard.clientHintText = hintText;
            if (GameManager.Instance != null) GameManager.Instance.RefreshAllCardsVisuals();
        }
    }
    
    private static readonly Dictionary<int, string> setcodeMap = new Dictionary<int, string>
    {
        {0x4, "Amazoness"}, {0x45, "Archfiend"}, {0x2e, "Gravekeeper"}, {0x2b, "Ninja"},
        {0x3b, "Red-Eyes"}, {0xdd, "Blue-Eyes"}, {0xa2, "Magician"}, {0x10a2, "Dark Magician"},
        {0x40, "Exodia"}, {0x52, "Guardian"}, {0x62, "Toon"}, {0x64, "Harpie"},
        {0x3a, "Ojama"}, {0x8, "HERO"}, {0x28, "Batteryman"}, {0x1048, "Gaia The Fierce Knight"}
    };

    private HashSet<int> GetSetcodes()
    {
        HashSet<int> currentSetcodes = new HashSet<int>();
        if (unityData == null) return currentSetcodes;

        string baseArchetype = unityData.archetype ?? "";
        string cardName = unityData.name ?? "";
        foreach(var kvp in setcodeMap)
        {
            if (baseArchetype.Contains(kvp.Value) || cardName.Contains(kvp.Value))
            {
                currentSetcodes.Add(kvp.Key);
            }
        }
        if (registeredEffects != null) {
            foreach (var eff in registeredEffects.FindAll(e => e.isTypeSingle)) {
                if (eff.singleRange && (eff.range & GetLocation()) == 0) continue;
                int val = ConvertToInt(eff.GetValue());
                if (eff.code == 350) { currentSetcodes.Clear(); currentSetcodes.Add(val); } // CHANGE_SETCODE
                else if (eff.code == 334) currentSetcodes.Add(val); // ADD_SETCODE
                else if (eff.code == 349) currentSetcodes.Remove(val); // REMOVE_SETCODE
            }
        }
        
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null) {
            CardLocation myLoc = (CardLocation)GetLocation();
            var changeAuras = CardEffectManager.Instance.auraManager.GetAuraValues(this, "CHANGE_SETCODE", myLoc);
            if (changeAuras.Count > 0) { currentSetcodes.Clear(); currentSetcodes.Add(changeAuras.Last()); }
            
            var addAuras = CardEffectManager.Instance.auraManager.GetAuraValues(this, "ADD_SETCODE", myLoc);
            foreach(var v in addAuras) currentSetcodes.Add(v);
            
            var remAuras = CardEffectManager.Instance.auraManager.GetAuraValues(this, "REMOVE_SETCODE", myLoc);
            foreach(var v in remAuras) currentSetcodes.Remove(v);
        }

        return currentSetcodes;
    }
    
    public void AddMonsterAttribute(params object[] args) { Debug.LogWarning($"[LUA STUB] AddMonsterAttribute chamado em {unityData?.name}"); }
    public void AddSetcodes(params object[] args) { Debug.LogWarning($"[LUA STUB] AddSetcodes chamado em {unityData?.name}"); }

    public void RegisterEffect(LuaEffect e, bool forced = false)
    {
        if (e == null) return;
        registeredEffects.Add(e);
        // Debug.Log($"[Lua] Efeito tipo {e.type} (Code {e.code}) registrado em {unityData?.name}.");

        // Se for um efeito de equipamento (0x4) e a carta já estiver no campo, avisa o alvo para recalcular!
        if ((e.type & 0x0004) != 0 && unityCard != null && unityCard.isOnField && CardEffectManager.Instance != null)
        {
            CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(UnityEngine.FindObjectsSortMode.None);
            foreach (var link in links)
            {
                if (link.source == unityCard && link.target != null)
                {
                    CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.RecalculateStatsNextFrame(link.target));
                }
            }
        }
        else if (e.type == 0x0001 && unityCard != null && unityCard.isOnField) // Efeito SINGLE (Proteção contra loops de Preload)
        {
            // Se o código for de ATK, DEF ou Level, força a atualização visual imediatamente
            if ((e.code >= 1 && e.code <= 8) || e.code == 10)
            {
                GameManager.Instance.RefreshAllCardsVisuals();
            }
        }
        
        // EFFECT_FLAG_CLIENT_HINT: Exibe uma mensagem amigável no painel da carta na tela do jogador!
        if (e.clientHint && !string.IsNullOrEmpty(e.description))
        {
            SetHint(6, e.description); // CHINT_DESC_ADD = 6
        }
    }

    public bool IsSetCard(params object[] setCodes)
    { 
        HashSet<int> mySetcodes = GetSetcodes();
        if (mySetcodes.Count == 0) return false;
        
        foreach(var s in setCodes)
        {
            int code = ConvertToInt(s) & 0xffff; 
            if (mySetcodes.Contains(code)) return true;
        }
        return false;
    }
        
    public bool IsCode(params object[] codes)
    {
        int myId = GetCode();
        foreach(var c in codes) 
        {
            int targetCode = ConvertToInt(c);
            if (myId == targetCode) return true;
            
            // Fallback de Segurança: Busca no Banco de Dados pelo Nome
            if (unityData != null && GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
            {
                string targetStr = targetCode.ToString();
                CardData dbCard = GameManager.Instance.cardDatabase.cardDatabase.Find(card => card.password == targetStr || card.id == targetStr || card.password == "0" + targetStr);
                if (dbCard != null && dbCard.name == unityData.name) return true;
            }
        }
        return false;
    }

    public bool IsContinuousTrap() { return IsTrap() && unityData != null && unityData.property == "Continuous"; }
    public bool IsNormalTrap() { return IsTrap() && unityData != null && (unityData.property == "Normal" || string.IsNullOrEmpty(unityData.property)); }
    public int GetEquipCount() { return (CardEffectManager.Instance != null && unityCard != null) ? CardEffectManager.Instance.GetEquippedCards(unityCard).Count : 0; }
    public bool IsOriginalType(object t) { return IsType(t); }
    public bool IsOriginalAttribute(object attr) { return IsAttribute(attr); }
    public bool IsOriginalRace(object race) { return IsRace(race); }
    public bool IsOriginalCode(params object[] codes) { return IsCode(codes); }
    public LuaGroup GetEquippedGroup() { 
        LuaGroup g = new LuaGroup(); 
        if (CardEffectManager.Instance != null && unityCard != null) 
            foreach(var c in CardEffectManager.Instance.GetEquippedCards(unityCard)) g.AddCard(new LuaCard(c));
        return g; 
    }
        
    // Muitos scripts LUA do EDOPro chamam estes métodos em vez de IsSetCard
    public bool IsHasSetcode(params object[] setCodes) { return IsSetCard(setCodes); }
    public bool IsHasSetCard(params object[] setCodes) { return IsSetCard(setCodes); }
    
    public LuaEffect GetActivateEffect()
    {
        if (registeredEffects != null)
        {
            return registeredEffects.Find(e => e.isTypeActivate); // EFFECT_TYPE_ACTIVATE (0x0010)
        }
        return null;
    }

    public LuaEffect CheckActivateResult(object b) 
    { 
        return new LuaEffect { 
            owner = this, 
            conditionFunc = CardEffectManager.Instance.dummyClosureTrue, 
            costFunc = CardEffectManager.Instance.dummyClosureTrue, 
            targetFunc = CardEffectManager.Instance.dummyClosureTrue, 
            operationFunc = CardEffectManager.Instance.dummyClosureTrue 
        }; 
    }

    public static LuaGroup operator +(LuaCard a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null) g.AddCard(a);
        if (b != null && a != b) g.AddCard(b);
        return g;
    }
    public static LuaGroup operator -(LuaCard a, LuaCard b)
    {
        LuaGroup g = new LuaGroup();
        if (a != null && a != b) g.AddCard(a);
        return g;
    }
}

// ==============================================================================
