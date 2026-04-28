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
        // Debug.Log($"<color=magenta>[IsLocation]</color> O LUA perguntou se '{unityData?.name}' está na zona (código {loc})");
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
    
    public int GetBaseAttack() 
    { 
        int baseAtk = unityCard != null ? unityCard.originalAtk : (unityData != null ? unityData.atk : 0); 
        if (registeredEffects != null) {
            var eff = registeredEffects.FindLast(e => e.type == 1 && e.code == 7); // 7 = EFFECT_SET_BASE_ATTACK
            if (eff != null) {
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
            var eff = registeredEffects.FindLast(e => e.type == 1 && e.code == 8); // 8 = EFFECT_SET_BASE_DEFENSE
            if (eff != null) {
                object valObj = eff.GetValue();
                if (valObj is double || valObj is long) baseDef = System.Convert.ToInt32(valObj);
            }
        }
        return baseDef;
    }
    public bool IsControlerCanBeChanged() { return true; }
    public int GetSummonType() { return 0; }
    public int GetPreviousLocation() { return 0; }

    public bool CheckFusionMaterial(object group = null, object card = null, object chkf = null) { return true; }
    public bool IsCanBeFusionMaterial(object card = null) { return true; }

    public bool IsCanBeRitualMaterial(object card = null) { return true; }
    public int GetRitualLevel(object rc = null) { return GetLevel(); }
    
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
    public LuaCard GetPreviousEquipTarget() { return _lastEquipTarget; }
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
        if (currentReason == 0) return true; 
        return (currentReason & ConvertToInt(reason)) != 0; 
    }
    public int GetReason() { return currentReason; }
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

    public void SetCardTarget(object tc) { }
    
    public bool IsStatus(object status) 
    { 
        int s = ConvertToInt(status);
        if (unityCard != null && unityCard.isOnField)
        {
            // 0x800 = STATUS_SUMMON_TURN, 0x20000000 = STATUS_FLIP_SUMMON_TURN, 0x40000000 = STATUS_SPSUMMON_TURN
            int summonTurnFlags = 0x800 | 0x20000000 | 0x40000000;
            bool passed = ((s & summonTurnFlags) != 0 && GameManager.Instance != null && unityCard.summonedTurnCount == GameManager.Instance.turnCount);
            
            if (unityCard.CurrentCardData != null && unityCard.CurrentCardData.type.Contains("Monster")) {
                Debug.Log($"<color=cyan>[IsStatus LOG]</color> Avaliando {unityData.name} | Status: {s} | Math Bitwise OK? {((s & summonTurnFlags) != 0)} | Turno Invocação: {unityCard.summonedTurnCount} == Turno Atual: {GameManager.Instance?.turnCount} -> Resultado Final: {passed}");
            }
            return passed;
        }
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
    
    public void ResetFlagEffect(object id) 
    { 
        int flagId = ConvertToInt(id);
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null)
        {
            string key = GetCardUniqueKey();
            if (CardEffectManager.Instance.luaDuel.cardFlags.ContainsKey(key)) CardEffectManager.Instance.luaDuel.cardFlags[key].Remove(flagId);
        }
    }

    public void SetFlagEffectLabel(object id, object label) { }
    public void CreateEffectRelation(object e) { }
    public void ReleaseEffectRelation(object e) { }
    
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
    
    public bool IsCode(params object[] codes)
    {
        int myId = GetCode();
        // Debug.Log($"<color=magenta>[IsCode LOG]</color> O LUA perguntou se '{unityData?.name}' (Meu ID: {myId}) é igual a: {string.Join(", ", codes.Select(c => ConvertToInt(c).ToString()))}");
        
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
    }

    public bool IsSetCard(params object[] setCodes) 
    { 
        if (unityData == null || string.IsNullOrEmpty(unityData.name)) return false;
        string arch = unityData.archetype ?? "";
        string cardName = unityData.name.ToLowerInvariant();
        
        // string codesLog = string.Join(", ", setCodes);
        // Debug.Log($"<color=magenta>[IsSetCard]</color> O LUA quer saber se '{unityData.name}' (Arquétipo: '{arch}') possui o código: {codesLog}");

        foreach(var s in setCodes)
        {
            int code = ConvertToInt(s) & 0xffff; 
            
            // 1. Checagem Oficial de Arquétipo (via Banco de Dados / JSON)
            if (!string.IsNullOrEmpty(arch) && arch != "None")
            {
                if (code == 0x04 && arch.Contains("Amazoness")) { /* Debug.Log($"<color=green>[IsSetCard]</color> '{unityData.name}' validado com sucesso como Amazoness (0x04)!"); */ return true; }
                if (code == 0x45 && arch.Contains("Archfiend")) return true;
                if (code == 0x2e && arch.Contains("Gravekeeper")) return true;
                if (code == 0x2b && arch.Contains("Ninja")) return true;
                if (code == 0x3b && arch.Contains("Red-Eyes")) return true;
                if (code == 0xdd && arch.Contains("Blue-Eyes")) return true;
                if (code == 0xa2 && arch.Contains("Magician")) return true;
                if (code == 0x10a2 && arch.Contains("Dark Magician")) return true;
                if (code == 0x40 && arch.Contains("Exodia")) return true;
                if (code == 0x52 && arch.Contains("Guardian")) return true;
                if (code == 0x62 && arch.Contains("Toon")) return true;
                if (code == 0x64 && arch.Contains("Harpie")) return true;
                if (code == 0x3a && arch.Contains("Ojama")) return true;
                if (code == 0x08 && arch.Contains("HERO")) return true;
                if (code == 0x28 && arch.Contains("Batteryman")) return true;
            }

            // 2. Fallback de Segurança (Nome da Carta)
            if (code == 0x04 && cardName.Contains("amazoness")) return true;
            if (code == 0x45 && cardName.Contains("archfiend")) return true;
            if (code == 0x2e && cardName.Contains("gravekeeper")) return true;
            if (code == 0x2b && cardName.Contains("ninja")) return true;
            if (code == 0x3b && cardName.Contains("red-eyes")) return true;
            if (code == 0xdd && cardName.Contains("blue-eyes")) return true;
            if (code == 0xa2 && cardName.Contains("magician")) return true;
            if (code == 0x10a2 && cardName.Contains("dark magician")) return true;
            if (code == 0x40 && (cardName.Contains("exodia") || cardName.Contains("forbidden one"))) return true;
            if (code == 0x52 && cardName.Contains("guardian")) return true;
            if (code == 0x62 && cardName.Contains("toon")) return true;
            if (code == 0x64 && cardName.Contains("harpie")) return true;
            if (code == 0x3a && cardName.Contains("ojama")) return true;
            if (code == 0x08 && cardName.Contains("hero")) return true;
            if (code == 0x28 && cardName.Contains("batteryman")) return true;
        }
        
        return false; // CORRIGIDO: Este fallback era o que quebrava o jogo tratando toda carta como arquétipo!
    }
        
    // Muitos scripts LUA do EDOPro chamam estes métodos em vez de IsSetCard
    public bool IsHasSetcode(params object[] setCodes) { return IsSetCard(setCodes); }
    public bool IsHasSetCard(params object[] setCodes) { return IsSetCard(setCodes); }
    
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
