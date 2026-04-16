using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// 2. CLASSE CARD (Representação de uma Carta Física)
// Chamado no Lua como: c:GetAttack()
// ==============================================================================
[MoonSharpUserData]
public class LuaCard
{
    public CardDisplay unityCard;
    public CardData unityData;
    public List<LuaEffect> registeredEffects = new List<LuaEffect>();

    public LuaCard(CardDisplay card) { unityCard = card; unityData = card?.CurrentCardData; }
    public LuaCard(CardData data) { unityData = data; unityCard = null; }

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
    
    // Helper para gerar um dummy seguro e evitar crashes de Null Reference no LUA
    private LuaCard SafeDummyCard() { return new LuaCard(new CardData { id = "0000", type = "Monster", name = "Dummy", atk = 0, def = 0, level = 1 }); }

    public int GetAttack() { return unityCard != null ? unityCard.currentAtk : (unityData != null ? unityData.atk : 0); }
    public int GetDefense() { return unityCard != null ? unityCard.currentDef : (unityData != null ? unityData.def : 0); }
    public int GetLevel() { return unityCard != null ? unityCard.currentLevel : (unityData != null ? unityData.level : 0); }
    public int GetOriginalLevel() { return unityCard != null ? unityCard.originalLevel : (unityData != null ? unityData.level : 0); }
    public bool IsFaceup() { return unityCard != null ? !unityCard.isFlipped : false; }
    
    public int ownerPlayerIndex = -1;

    public int GetControler()
    {
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
            if (unityData.type.Contains("Spell") || unityData.type.Contains("Trap")) return (loc & 0x08) != 0;
            return (loc & 0x04) != 0;
        }
        if (unityCard != null && !unityCard.isOnField) return (loc & 0x02) != 0; // Hand
        
        if (unityData != null)
        {
            if ((loc & 0x10) != 0 && (GameManager.Instance.GetPlayerGraveyard().Contains(unityData) || GameManager.Instance.GetOpponentGraveyard().Contains(unityData))) return true;
            if ((loc & 0x01) != 0 && (GameManager.Instance.GetPlayerMainDeck().Contains(unityData) || GameManager.Instance.GetOpponentMainDeck().Contains(unityData))) return true;
            if ((loc & 0x20) != 0 && (GameManager.Instance.GetPlayerRemoved().Contains(unityData) || GameManager.Instance.GetOpponentRemoved().Contains(unityData))) return true;
            if ((loc & 0x40) != 0 && (GameManager.Instance.GetPlayerExtraDeck().Contains(unityData) || GameManager.Instance.GetOpponentExtraDeck().Contains(unityData))) return true;
        }
        return false;
    }

    // --- PERGUNTAS DE STATUS QUE O SCRIPT FAZ À CARTA ---

    public bool IsDestructable() 
    { 
        // Regra geral de protótipo: Tudo é destrutível a menos que algum escudo proíba
        return true; 
    }

    public bool IsSpellTrap()
    {
        if (unityData == null) return false;
        return unityData.type.Contains("Spell") || unityData.type.Contains("Trap");
    }

    public new int GetType()
    {
        if (unityData == null || string.IsNullOrEmpty(unityData.type)) return 0;
        int t = 0;
        string typeStr = unityData.type.Trim().ToUpperInvariant();
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
        return t;
    }

    public int GetOriginalRace() { return GetRace(); }
    public int GetOriginalAttribute() { return GetAttribute(); }
    public int GetAttribute() { 
        if (unityData == null || string.IsNullOrEmpty(unityData.attribute)) return 0;
        string a = unityData.attribute.Trim().ToUpperInvariant();
        if (a.Contains("EARTH")) return 0x01;
        if (a.Contains("WATER")) return 0x02;
        if (a.Contains("FIRE")) return 0x04;
        if (a.Contains("WIND")) return 0x08;
        if (a.Contains("DARK")) return 0x10;
        if (a.Contains("LIGHT")) return 0x20;
        if (a.Contains("DIVINE")) return 0x40;
        return 0;
    }
    public int GetTextAttack() { return GetAttack(); }
    public int GetTextDefense() { return GetDefense(); }

    public bool IsType(object t) { return (GetType() & ConvertToInt(t)) != 0; }
    
    public bool IsTrap() { return unityData != null && unityData.type.Contains("Trap"); }
    public bool IsSpell() { return unityData != null && unityData.type.Contains("Spell"); }
    public bool IsMonster() { return unityData != null && unityData.type.Contains("Monster"); }

    // Novos Stubs Descobertos pelo Mass Validator
    public CardLocation previousLocation = CardLocation.Unknown;
    
    public bool IsPreviousLocation(object loc) 
    { 
        int l = ConvertToInt(loc);
        if (previousLocation != CardLocation.Unknown)
        {
            if (l == 0x04 || l == 0x08 || l == 0x0C) return previousLocation == CardLocation.Field;
            if (l == 0x02) return previousLocation == CardLocation.Hand;
            if (l == 0x01) return previousLocation == CardLocation.Deck;
            if (l == 0x10) return previousLocation == CardLocation.Graveyard;
        }
        return true; 
    }
    public LuaCard GetBattleTarget() { return SafeDummyCard(); }
    public bool IsDiscardable(params object[] args) { return true; }
    public LuaCard GetEquipTarget() { return SafeDummyCard(); }
    public bool IsAbleToGraveAsCost() { return true; }
    public bool IsAbleToRemoveAsCost() { return true; }
    public bool IsAbleToDeck() { return true; }
    public bool IsLevelBelow(object lvl) { return GetLevel() <= ConvertToInt(lvl); }
    public bool IsHasType(object type) { return IsType(type); }
    public int GetAttackAnnouncedCount() { return 0; }
    public int GetReasonPlayer() { return GetControler(); }
    public int GetCounter(object counterType) { return 0; }
    public bool IsFacedown() { return unityCard != null ? unityCard.isFlipped : false; }
    public bool IsTributeSummoned() { return false; }
    public int GetPreviousPosition() { return GetBattlePosition(); }
    public int GetPreviousCodeOnField() { return GetCode(); }
    public int GetPreviousRaceOnField() { return GetRace(); }
    public bool IsCanBeEffectTarget(object e) { return true; }
    public bool IsSummonType(object sumtype) { return true; }
    public bool IsCanRemoveCounter(object player, object counterType, object count, object reason) { return true; }
    public bool IsPreviousRaceOnField(object race) { return true; }
    public bool IsRelateToCard(object card) { return true; }
    public bool CanSummonOrSet(object ignoreLimit, object param) { return true; }
    public LuaCard GetOwner() { return this; }
    public int GetPreviousControler() { return GetControler(); }
    public void AddCounter(object counterType, object count) { }
    public void RemoveCounter(object player, object counterType, object count, object reason) { }
    public bool IsCanAddCounter(object counterType, object count) { return true; }
    public int GetFieldID() { return 0; }
    public int GetReason() { return 0; }
    public LuaGroup GetTarget() { return new LuaGroup(); }
    public void SetMaterial(object g) { }
    public LuaGroup GetAdminGroup() { return new LuaGroup(); }
    public bool IsSummonLocation(object loc) { return true; }
    public void SetStatus(object status, object enable) { }
    
    public int GetRace() 
    { 
        if (unityData == null || string.IsNullOrEmpty(unityData.race)) return 0;
        string r = unityData.race.Trim().ToUpperInvariant();
        if (r.Contains("WARRIOR")) return 0x1;
        if (r.Contains("SPELLCASTER")) return 0x2;
        if (r.Contains("FAIRY")) return 0x4;
        if (r.Contains("FIEND")) return 0x8;
        if (r.Contains("ZOMBIE")) return 0x10;
        if (r.Contains("MACHINE")) return 0x20;
        if (r.Contains("AQUA")) return 0x40;
        if (r.Contains("PYRO")) return 0x80;
        if (r.Contains("ROCK")) return 0x100;
        if (r.Contains("WINGED BEAST")) return 0x200;
        if (r.Contains("PLANT")) return 0x400;
        if (r.Contains("INSECT")) return 0x800;
        if (r.Contains("THUNDER")) return 0x1000;
        if (r.Contains("DRAGON")) return 0x2000;
        if (r.Contains("BEAST-WARRIOR")) return 0x8000;
        else if (r.Contains("BEAST")) return 0x4000;
        if (r.Contains("DINOSAUR")) return 0x10000;
        if (r.Contains("FISH")) return 0x20000;
        if (r.Contains("SEA SERPENT")) return 0x40000;
        if (r.Contains("REPTILE")) return 0x80000;
        return 0;
    }
    public int GetMaterialCount() { return 0; }
    public bool IsAbleToDeckAsCost() { return true; }
    public bool IsSummonPlayer(object player) { return true; }
    public LuaGroup GetAttackableTarget() { return new LuaGroup(); }
    public bool IsAbleToChangeControler() { return true; }
    public bool IsSummonableCard() { return true; }
    public LuaGroup GetMaterial() { return new LuaGroup(); }
    public int GetEffectCount(object code) { return 0; }
    public int GetBaseAttack() { return unityCard != null ? unityCard.originalAtk : (unityData != null ? unityData.atk : 0); }
    public int GetBaseDefense() { return unityCard != null ? unityCard.originalDef : (unityData != null ? unityData.def : 0); }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return 0; }
    public int GetPreviousLocation() { return 0; }

    public bool CheckFusionMaterial(object group = null, object card = null, object chkf = null) { return true; }
    public bool IsCanBeFusionMaterial(object card = null) { return true; }


    public LuaCard GetHandler() { return this; }
    public int GetCardTargetCount() { return 0; }
    public bool IsOriginalCodeRule(params object[] codes) { return IsCode(codes); }
    public bool IsFieldSpell() { return IsType(0x80000); }
    public int GetLocation() { return unityCard != null && unityCard.isOnField ? (unityData.type.Contains("Spell") || unityData.type.Contains("Trap") ? 0x08 : 0x04) : 0x02; }
    public int GetDestination() { return 0; }
    public int GetLeaveFieldDest() { return 0; }
    public LuaGroup GetColumnGroup() { return new LuaGroup(); }
    public LuaGroup GetOverlayGroup() { return new LuaGroup(); }
    public int GetOverlayCount() { return 0; }
    public LuaGroup GetCardTarget() { return new LuaGroup(); }
    public bool HasNonZeroAttack() { return GetAttack() > 0; }
    public bool HasNonZeroDefense() { return GetDefense() > 0; }
    public bool IsCanChangePosition() { return true; }
    public int GetTurnID() { return 0; }
    public bool CanChainAttack() { return true; }
    public LuaCard GetPreviousEquipTarget() { return SafeDummyCard(); }
    public bool IsAbleToGrave() { return true; }
    public bool HasFlagEffect(object id) { return false; }
    public int GetTurnCounter() { return unityCard != null ? unityCard.turnCounter : 0; }
    public bool IsHasCardTarget(object c) { return false; }
    public LuaCard GetReasonCard() { return SafeDummyCard(); }
    public void CompleteProcedure() { } // Confirmação de invocações especiais/rituais
    public void CancelToGrave(params object[] args) { } // Mantém a carta no campo (Ex: Swords of Revealing Light)
    public bool IsLevelAbove(object lvl) { return GetLevel() >= ConvertToInt(lvl); }
    public int GetBattledGroupCount() { return 0; }
    public bool IsRitualMonster() { return unityData != null && unityData.type.Contains("Ritual"); }
    public bool IsRitualSpell() { return unityData != null && unityData.type.Contains("Spell") && unityData.property == "Ritual"; }
    public bool IsPublic() { return true; }
    public int GetOwnerTargetCount() { return 0; }    public bool IsFusionSummoned() { return false; }
    public bool IsDefenseBelow(object def) { return GetDefense() <= ConvertToInt(def); }
    public int GetFlagEffectLabel(object id) { return 0; }
    public bool IsAttributeExcept(object attr) { return !IsAttribute(attr); }
    public int GetBattlePosition() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense ? 0x8 : 0x1; }
    public bool IsRelateToBattle() { return true; }
    public void DeleteGroup() { }

    // Stubs para compatibilidade da API Lua
    public bool IsRace(object r) { return (GetRace() & ConvertToInt(r)) != 0; }
    public bool IsAttribute(object attr) { return (GetAttribute() & ConvertToInt(attr)) != 0; }
    public bool IsReason(object reason) { return true; }
    public bool IsRelateToEffect(object e) { return true; } // Evita crash no final de correntes (Chains)
    public bool IsAttackBelow(object atk) { return GetAttack() <= ConvertToInt(atk); }
    public bool IsAttackAbove(object atk) { return GetAttack() >= ConvertToInt(atk); }
    public bool IsDefenseAbove(object def) { return GetDefense() >= ConvertToInt(def); }
    public int GetAttackedCount() { return 0; }
    public bool IsCanTurnSet() { return true; }
    public bool IsCanBeSpecialSummoned(object e, object sumtype, object sumplayer, object nocheck, object nolimit, params object[] extraArgs) { return true; }
    public bool IsReleasable() { return true; }
    public bool IsPreviousControler(object p) { return true; }
    public bool IsAbleToHand() { return true; }
    public bool IsPreviousPosition(object pos) { return true; }
    public bool IsDefensePos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Defense; }
    public bool IsAttackPos() { return unityCard != null && unityCard.position == CardDisplay.BattlePosition.Attack; }
    public bool IsPosition(object pos) { return true; } // Stub para checagens múltiplas
    public bool IsSummonable(object ignoreLimit, object param) { return true; }
    public bool IsMSetable(object ignoreLimit, object param) { return true; }
    public bool IsSSetable(params object[] args) { return true; }
    public LuaGroup GetEquipGroup() { return new LuaGroup(); }
    public int GetSequence() { return 0; }
    public int GetFlagEffect(object id) { return 0; }
    
    public bool IsImmuneToEffect(object e) { return false; }
    public bool CanAttack() { return true; }
    public bool IsDisabled() { return false; }
    public bool IsForbidden() { return false; }
    public bool IsSpecialSummoned() { return false; }
    public bool HasLevel() { return GetLevel() > 0; }

    public int GetCode() {
        if (unityData == null) return 0;
        if (!string.IsNullOrEmpty(unityData.password) && int.TryParse(unityData.password, out int code)) return code;
        string digits = System.Text.RegularExpressions.Regex.Replace(unityData.id, @"\D", "");
        if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out int fallbackCode)) return fallbackCode;
        return 0;
    }
    public int GetOriginalCode() { return GetCode(); }
    public void RegisterFlagEffect(params object[] args) { }
    public void SetCardTarget(object tc) { }
    public bool IsStatus(object status) { return false; }
    public LuaCard GetFirstCardTarget() { return SafeDummyCard(); }
    public void SetTurnCounter(object ct) { }
    public int GetLabel() { return 0; }
    public void SetLabel(object ct) { }
    
    public void EnableReviveLimit() { }
    public void SetUniqueOnField(params object[] args) { }
    public void EnableCounterPermit(params object[] args) { }
    public void SetCounterLimit(params object[] args) { }
    public bool IsAbleToRemove() { return true; }
    public void AddMustBeSpecialSummoned(params object[] args) { }
    public void EnableUnsummonable() { }
    public void SetSPSummonOnce(params object[] args) { }
    public DynValue IsHasEffect(object effectCode) { return DynValue.Nil; }
    public void ResetFlagEffect(object id) { }
    public void SetFlagEffectLabel(object id, object label) { }
    public void CreateEffectRelation(object e) { }
    public void ReleaseEffectRelation(object e) { }
    public void SetHint(params object[] args) { }
    
    public bool IsCode(params object[] codes)
    {
        int myId = GetCode();
        foreach(var c in codes) 
        {
            int targetCode = ConvertToInt(c);
            if (myId == targetCode) return true;
            
            // Fallback Supremo: Busca no Banco de Dados se existe alguma carta com esse Password/ID
            // e compara pelo NOME. Isso salva a pátria se o nosso ID customizado for "DM0001" 
            // mas o script LUA estiver procurando "89631139" (Blue-Eyes oficial).
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
    
    public void AddMonsterAttribute(params object[] args) { }
    public void AddSetcodes(params object[] args) { }

    public void RegisterEffect(LuaEffect e, bool forced = false)
    {
        if (e == null) return;
        registeredEffects.Add(e);
        Debug.Log($"[Lua] Efeito tipo {e.type} (Code {e.code}) registrado em {unityData?.name}.");

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
    }

    public bool IsSetCard(params object[] setCodes) { return true; }
    
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
