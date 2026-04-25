using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

/// <summary>
/// Bridge para chamar as funções auxiliares definidas no arquivo utility.lua.
/// Todas as implementações reais estão em Lua, este código apenas redireciona.
/// </summary>
public static class LuaUtilityBridge
{
    // O script Lua que contém o utility (deve ser o mesmo usado no duelo)
    private static Script _luaScript;

    /// <summary>
    /// Inicializa o bridge com o script Lua carregado.
    /// Deve ser chamado uma vez, após o carregamento do utility.lua.
    /// </summary>
    public static void Initialize(Script script)
    {
        _luaScript = script ?? throw new ArgumentNullException(nameof(script));
    }

    // ------------------------------------------------------------------------
    // Tabela aux (Auxiliary)
    // ------------------------------------------------------------------------

    public static class Auxiliary
    {
        // Funções utilitárias gerais
        public static void NULL() => CallLuaFunction("Auxiliary.NULL");
        public static bool TRUE(params object[] args) => CallLuaFunction<bool>("Auxiliary.TRUE", args);
        public static bool FALSE(params object[] args) => CallLuaFunction<bool>("Auxiliary.FALSE", args);

        /// <summary> Combina funções com AND lógico. </summary>
        public static Func<object[], bool> AND(params Func<object[], bool>[] funs)
            => (args) => CallLuaFunction<bool>("Auxiliary.AND", funs, args);

        /// <summary> Combina funções com OR lógico. </summary>
        public static Func<object[], bool> OR(params Func<object[], bool>[] funs)
            => (args) => CallLuaFunction<bool>("Auxiliary.OR", funs, args);

        /// <summary> AND bit a bit de múltiplos retornos numéricos. </summary>
        public static Func<object[], long[]> TableAND(params Func<object[], long[]>[] funs)
            => (args) => CallLuaFunction<long[]>("Auxiliary.TableAND", funs, args);

        /// <summary> OR bit a bit de múltiplos retornos numéricos. </summary>
        public static Func<object[], long[]> TableOR(params Func<object[], long[]>[] funs)
            => (args) => CallLuaFunction<long[]>("Auxiliary.TableOR", funs, args);

        /// <summary> Inverte o resultado de uma função booleana. </summary>
        public static Func<object[], bool> NOT(Func<object[], bool> f)
            => (args) => CallLuaFunction<bool>("Auxiliary.NOT", f, args);

        // Predicados para efeitos e filtros
        public static Func<Effect, Card, bool> TargetEqualFunction<T>(Func<Card, T> getter, T value, params object[] extra)
            => (e, target) => CallLuaFunction<bool>("Auxiliary.TargetEqualFunction", getter, value, extra, e, target);

        public static Func<Effect, Card, bool> TargetBoolFunction(Func<Card, object[], bool> f, params object[] extra)
            => (e, target) => CallLuaFunction<bool>("Auxiliary.TargetBoolFunction", f, extra, e, target);

        public static Func<Card, bool> FilterEqualFunction<T>(Func<Card, T> getter, T value, params object[] extra)
            => (target) => CallLuaFunction<bool>("Auxiliary.FilterEqualFunction", getter, value, extra, target);

        public static Func<Card, Card, int, int, bool> FilterBoolFunctionEx(Func<Card, object, Card, int, int, bool> f, object value)
            => (target, scard, sumtype, tp) => CallLuaFunction<bool>("Auxiliary.FilterBoolFunctionEx", f, value, target, scard, sumtype, tp);

        public static Func<Card, Card, int, int, bool> FilterBoolFunctionEx2(Func<Card, Card, int, int, object[], bool> f, params object[] extra)
            => (target, scard, sumtype, tp) => CallLuaFunction<bool>("Auxiliary.FilterBoolFunctionEx2", f, extra, target, scard, sumtype, tp);

        public static Func<Card, bool> FilterBoolFunction(Func<Card, object[], bool> f, params object[] extra)
            => (target) => CallLuaFunction<bool>("Auxiliary.FilterBoolFunction", f, extra, target);

        // Token placeholder
        public static LuaCard TokenPlaceholder => CallLuaFunction<LuaCard>("Auxiliary.TokenPlaceholder");

        // Substituição de custo
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> CostWithReplace(
            Func<Effect, int, Group, int, int, Effect, int, int, int, bool> baseCost,
            int replaceCode,
            Func<Effect, Effect, int, Group, int, int, Effect, int, int, int, object, bool> extraCon = null,
            Func<Effect, int, Group, int, int, Effect, int, int, int, bool> alwaysExecute = null)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Auxiliary.CostWithReplace",
                baseCost, replaceCode, extraCon, alwaysExecute, e, tp, eg, ep, ev, re, r, rp, chk);

        // Atalhos de tipo (retornam predicados)
        public static Func<Card, bool> IsMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsMonster");
        public static Func<Card, bool> IsSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsSpell");
        public static Func<Card, bool> IsTrap => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsTrap");
        public static Func<Card, bool> IsSpellTrap => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsSpellTrap");

        public static Func<Card, bool> IsQuickPlaySpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsQuickPlaySpell");
        public static Func<Card, bool> IsContinuousSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsContinuousSpell");
        public static Func<Card, bool> IsEquipSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsEquipSpell");
        public static Func<Card, bool> IsFieldSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsFieldSpell");
        public static Func<Card, bool> IsRitualSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsRitualSpell");
        public static Func<Card, bool> IsLinkSpell => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsLinkSpell");
        public static Func<Card, bool> IsContinuousTrap => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsContinuousTrap");
        public static Func<Card, bool> IsCounterTrap => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsCounterTrap");
        public static Func<Card, bool> IsFusionMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsFusionMonster");
        public static Func<Card, bool> IsRitualMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsRitualMonster");
        public static Func<Card, bool> IsSynchroMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsSynchroMonster");
        public static Func<Card, bool> IsXyzMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsXyzMonster");
        public static Func<Card, bool> IsPendulumMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsPendulumMonster");
        public static Func<Card, bool> IsLinkMonster => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsLinkMonster");

        // Métodos auxiliares de Card
        public static bool IsNormalSpell(Card c) => CallLuaFunction<bool>("Auxiliary.IsNormalSpell", c);
        public static bool IsNormalTrap(Card c) => CallLuaFunction<bool>("Auxiliary.IsNormalTrap", c);
        public static bool IsNormalSpellTrap(Card c) => CallLuaFunction<bool>("Auxiliary.IsNormalSpellTrap", c);
        public static bool IsContinuousSpellTrap(Card c) => CallLuaFunction<bool>("Auxiliary.IsContinuousSpellTrap", c);
        public static Func<Card, bool> IsEquipCard => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsEquipCard");
        public static Func<Card, bool> IsMonsterCard => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsMonsterCard");
        public static Func<Card, bool> IsSpellCard => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsSpellCard");
        public static Func<Card, bool> IsTrapCard => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsTrapCard");
        public static Func<Card, bool> IsSpellTrapCard => CallLuaFunction<Func<Card, bool>>("Auxiliary.IsSpellTrapCard");
        public static bool IsTrapMonster(Card c) => CallLuaFunction<bool>("Auxiliary.IsTrapMonster", c);
        public static bool IsEquipTrap(Card c) => CallLuaFunction<bool>("Auxiliary.IsEquipTrap", c);
        public static int GetMainCardType(Card c) => CallLuaFunction<int>("Auxiliary.GetMainCardType", c);
        public static bool IsEffectMonster(Card c) => CallLuaFunction<bool>("Auxiliary.IsEffectMonster", c);
        public static bool IsNonEffectMonster(Card c) => CallLuaFunction<bool>("Auxiliary.IsNonEffectMonster", c);
        public static bool IsBaseAttack(Card c, int atk) => CallLuaFunction<bool>("Auxiliary.IsBaseAttack", c, atk);
        public static bool IsTextAttack(Card c, int atk) => CallLuaFunction<bool>("Auxiliary.IsTextAttack", c, atk);
        public static bool IsPreviousAttackOnField(Card c, int atk) => CallLuaFunction<bool>("Auxiliary.IsPreviousAttackOnField", c, atk);
        public static bool IsBaseDefense(Card c, int def) => CallLuaFunction<bool>("Auxiliary.IsBaseDefense", c, def);
        public static bool IsTextDefense(Card c, int def) => CallLuaFunction<bool>("Auxiliary.IsTextDefense", c, def);
        public static bool IsPreviousDefenseOnField(Card c, int def) => CallLuaFunction<bool>("Auxiliary.IsPreviousDefenseOnField", c, def);
        public static bool IsLevelBetween(Card c, int firstLv, int secondLv) => CallLuaFunction<bool>("Auxiliary.IsLevelBetween", c, firstLv, secondLv);
        public static bool IsOriginalLevel(Card c, int lvl) => CallLuaFunction<bool>("Auxiliary.IsOriginalLevel", c, lvl);
        public static bool IsPreviousLevelOnField(Card c, int lvl) => CallLuaFunction<bool>("Auxiliary.IsPreviousLevelOnField", c, lvl);
        public static bool IsOriginalRank(Card c, int rank) => CallLuaFunction<bool>("Auxiliary.IsOriginalRank", c, rank);
        public static bool IsPreviousRankOnField(Card c, int rank) => CallLuaFunction<bool>("Auxiliary.IsPreviousRankOnField", c, rank);
        public static bool IsScale(Card c, int scale) => CallLuaFunction<bool>("Auxiliary.IsScale", c, scale);
        public static bool IsNormalSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsNormalSummoned", c);
        public static bool IsTributeSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsTributeSummoned", c);
        public static bool IsFlipSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsFlipSummoned", c);
        public static bool IsGeminiSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsGeminiSummoned", c);
        public static bool IsSpecialSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsSpecialSummoned", c);
        public static bool IsRitualSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsRitualSummoned", c);
        public static bool IsFusionSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsFusionSummoned", c);
        public static bool IsSynchroSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsSynchroSummoned", c);
        public static bool IsXyzSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsXyzSummoned", c);
        public static bool IsPendulumSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsPendulumSummoned", c);
        public static bool IsLinkSummoned(Card c) => CallLuaFunction<bool>("Auxiliary.IsLinkSummoned", c);
        public static bool IsPreviousAttributeOnField(Card c, int attr) => CallLuaFunction<bool>("Auxiliary.IsPreviousAttributeOnField", c, attr);
        public static bool IsPreviousRaceOnField(Card c, int race) => CallLuaFunction<bool>("Auxiliary.IsPreviousRaceOnField", c, race);
        public static bool IsPreviousTypeOnField(Card c, int cardType) => CallLuaFunction<bool>("Auxiliary.IsPreviousTypeOnField", c, cardType);
        public static bool IsPreviousCodeOnField(Card c, params int[] codes) => CallLuaFunction<bool>("Auxiliary.IsPreviousCodeOnField", c, codes);
        public static bool IsPreviousSequence(Card c, int seq) => CallLuaFunction<bool>("Auxiliary.IsPreviousSequence", c, seq);
        public static bool IsOwner(Card c, int player) => CallLuaFunction<bool>("Auxiliary.IsOwner", c, player);
        public static bool IsBattlePosition(Card c, int pos) => CallLuaFunction<bool>("Auxiliary.IsBattlePosition", c, pos);
        public static bool HasCounter(Card c, int counterType) => CallLuaFunction<bool>("Auxiliary.HasCounter", c, counterType);
        public static bool HasEquipCard(Card c) => CallLuaFunction<bool>("Auxiliary.HasEquipCard", c);
        public static bool IsDestination(Card c, int dest) => CallLuaFunction<bool>("Auxiliary.IsDestination", c, dest);
        public static bool IsLeaveFieldDest(Card c, int dest) => CallLuaFunction<bool>("Auxiliary.IsLeaveFieldDest", c, dest);
        public static bool IsFieldID(Card c, int fid) => CallLuaFunction<bool>("Auxiliary.IsFieldID", c, fid);
        public static bool IsRealFieldID(Card c, int fid) => CallLuaFunction<bool>("Auxiliary.IsRealFieldID", c, fid);
        public static bool IsReasonCard(Card rc, Card c) => CallLuaFunction<bool>("Auxiliary.IsReasonCard", rc, c);
        public static bool IsReasonEffect(Card c, Effect eff) => CallLuaFunction<bool>("Auxiliary.IsReasonEffect", c, eff);
        public static bool IsReasonPlayer(Card c, int player) => CallLuaFunction<bool>("Auxiliary.IsReasonPlayer", c, player);
        public static List<Effect> AddSetcodesRule(Card c, int code, bool copyable, params int[] setcodes) => CallLuaFunction<List<Effect>>("Auxiliary.AddSetcodesRule", c, code, copyable, setcodes);
        public static bool CheckAdjacent(Card c) => CallLuaFunction<bool>("Auxiliary.CheckAdjacent", c);
        public static int? SelectAdjacent(Card c, int? selPlayer = null) => CallLuaFunction<int?>("Auxiliary.SelectAdjacent", c, selPlayer);
        public static void MoveAdjacent(Card c, int? selPlayer = null) => CallLuaFunction("Auxiliary.MoveAdjacent", c, selPlayer);
        public static bool IsColumn(Card c, int seq, int tp, int loc) => CallLuaFunction<bool>("Auxiliary.IsColumn", c, seq, tp, loc);
        public static int UpdateAttack(Card c, int amt, int? reset = null, Card rc = null, int? resetCount = null) => CallLuaFunction<int>("Auxiliary.UpdateAttack", c, amt, reset, rc, resetCount);
        public static int UpdateDefense(Card c, int amt, int? reset = null, Card rc = null, int? resetCount = null) => CallLuaFunction<int>("Auxiliary.UpdateDefense", c, amt, reset, rc, resetCount);
        public static int UpdateLevel(Card c, int amt, int? reset = null, Card rc = null) => CallLuaFunction<int>("Auxiliary.UpdateLevel", c, amt, reset, rc);
        public static int UpdateRank(Card c, int amt, int? reset = null, Card rc = null) => CallLuaFunction<int>("Auxiliary.UpdateRank", c, amt, reset, rc);
        public static int UpdateLink(Card c, int amt, int? reset = null, Card rc = null) => CallLuaFunction<int>("Auxiliary.UpdateLink", c, amt, reset, rc);
        public static int UpdateScale(Card c, int amt, int? reset = null, Card rc = null) => CallLuaFunction<int>("Auxiliary.UpdateScale", c, amt, reset, rc);
        public static long GetToBeLinkedZone(Card tc, Card c, int tp, bool clink, bool emz) => CallLuaFunction<long>("Auxiliary.GetToBeLinkedZone", tc, c, tp, clink, emz);
        public static int GetScale(Card c) => CallLuaFunction<int>("Auxiliary.GetScale", c);
        public static bool IsOddScale(Card c) => CallLuaFunction<bool>("Auxiliary.IsOddScale", c);
        public static bool IsEvenScale(Card c) => CallLuaFunction<bool>("Auxiliary.IsEvenScale", c);
        public static bool CanSummonOrSet(Card c, int player, int? summonType = null) => CallLuaFunction<bool>("Auxiliary.CanSummonOrSet", c, player, summonType);
        public static bool IsBattleDestroyed(Card c) => CallLuaFunction<bool>("Auxiliary.IsBattleDestroyed", c);
        public static bool IsInMainMZone(Card c, int? tp = null) => CallLuaFunction<bool>("Auxiliary.IsInMainMZone", c, tp);
        public static bool IsInExtraMZone(Card c, int? tp = null) => CallLuaFunction<bool>("Auxiliary.IsInExtraMZone", c, tp);
        public static int AnnounceAnotherAttribute(Card c, int tp) => CallLuaFunction<int>("Auxiliary.AnnounceAnotherAttribute", c, tp);
        public static bool IsAttributeExcept(Card c, int att, Card scard = null, int sumtype = 0, int playerid = PLAYER_NONE) => CallLuaFunction<bool>("Auxiliary.IsAttributeExcept", c, att, scard, sumtype, playerid);
        public static int AnnounceAnotherRace(Card c, int tp) => CallLuaFunction<int>("Auxiliary.AnnounceAnotherRace", c, tp);
        public static bool IsRaceExcept(Card c, int race, Card scard = null, int sumtype = 0, int playerid = PLAYER_NONE) => CallLuaFunction<bool>("Auxiliary.IsRaceExcept", c, race, scard, sumtype, playerid);
        public static bool IsSequence(Card c, params int[] seqs) => CallLuaFunction<bool>("Auxiliary.IsSequence", c, seqs);
        public static bool HasLevel(Card c) => CallLuaFunction<bool>("Auxiliary.HasLevel", c);
        public static bool HasRank(Card c) => CallLuaFunction<bool>("Auxiliary.HasRank", c);
        public static bool HasDefense(Card c) => CallLuaFunction<bool>("Auxiliary.HasDefense", c);
        public static bool HasFlagEffect(Card c, int id, int ct = 1) => CallLuaFunction<bool>("Auxiliary.HasFlagEffect", c, id, ct);
        public static bool HasFlagEffect(int tp, int id, int ct = 1) => CallLuaFunction<bool>("Auxiliary.HasFlagEffect", tp, id, ct);
        public static void NegateEffects(Card tc, Card c, int? reset = null, bool negatesCards = true, int ct = 1) => CallLuaFunction("Auxiliary.NegateEffects", tc, c, reset, negatesCards, ct);
        public static object GetMetatable(Card c, bool currentCode = false) => CallLuaFunction<object>("Auxiliary.GetMetatable", c, currentCode);
        public static object GetMetatable(int code) => CallLuaFunction<object>("Auxiliary.GetMetatable", code);
        public static void LoadCardScriptAlias(int code) => CallLuaFunction("Auxiliary.LoadCardScriptAlias", code);
        public static DynValue LoadCardScript(int code) => CallLuaFunction<DynValue>("Auxiliary.LoadCardScript", code);
        public static DynValue LoadCardScript(string fileName) => CallLuaFunction<DynValue>("Auxiliary.LoadCardScript", fileName);
        public static bool IsMonsterEffect(Effect e) => CallLuaFunction<bool>("Auxiliary.IsMonsterEffect", e);
        public static bool IsSpellEffect(Effect e) => CallLuaFunction<bool>("Auxiliary.IsSpellEffect", e);
        public static bool IsTrapEffect(Effect e) => CallLuaFunction<bool>("Auxiliary.IsTrapEffect", e);
        public static bool IsSpellTrapEffect(Effect e) => CallLuaFunction<bool>("Auxiliary.IsSpellTrapEffect", e);
        public static int Stringid(int code, int id) => CallLuaFunction<int>("Auxiliary.Stringid", code, id);
        public static void ForEach(Group g, Action<Card> f) => CallLuaFunction("Auxiliary.ForEach", g, f);
        public static IEnumerator<Card> Next(Group g) => CallLuaFunction<IEnumerator<Card>>("Auxiliary.Next", g);
        public static Func<Card, Card, int, int, bool> FilterSummonCode(params int[] codes) => CallLuaFunction<Func<Card, Card, int, int, bool>>("Auxiliary.FilterSummonCode", codes);
        public static object[] ParamsFromTable(Dictionary<object, object> tab, params object[] keys) => CallLuaFunction<object[]>("Auxiliary.ParamsFromTable", tab, keys);
        public static Func<object, object[], object> FunctionWithNamedArgs(Delegate f, params object[] argNames) => CallLuaFunction<Func<object, object[], object>>("Auxiliary.FunctionWithNamedArgs", f, argNames);
        public static (List<(Group group, Func<Card, Effect, int, Group, Group, Card, Group, int, bool> check, Effect eff)> extraMaterials, Group allMaterials) GetExtraMaterials(int tp, Group mustg, Card sc, int summonType)
            => CallLuaFunction<(List<(Group, Func<Card, Effect, int, Group, Group, Card, Group, int, bool>, Effect)>, Group)>("Auxiliary.GetExtraMaterials", tp, mustg, sc, summonType);
        public static bool CheckValidExtra(Card c, int tp, Group sg, Group mg, Card lc, List<(Group, Func<Card, Effect, int, Group, Group, Card, Group, int, bool>, Effect)> emt, List<(Group, Func<Card, Effect, int, Group, Group, Card, Group, int, bool>, Effect)> filt = null)
            => CallLuaFunction<bool>("Auxiliary.CheckValidExtra", c, tp, sg, mg, lc, emt, filt);
        public static void DeleteExtraMaterialGroups(List<(Group, Func<Card, Effect, int, Group, Group, Card, Group, int, bool>, Effect)> emt) => CallLuaFunction("Auxiliary.DeleteExtraMaterialGroups", emt);
        public static Group GetMustBeMaterialGroup(int tp, Group eg, int sump, Card sc, Group g, int r) => CallLuaFunction<Group>("Auxiliary.GetMustBeMaterialGroup", tp, eg, sump, sc, g, r);
        public static int RegisterEffectWithMarkers(Card c, Effect e, bool forced, params int[] markerCodes) => CallLuaFunction<int>("Auxiliary.RegisterEffectWithMarkers", c, e, forced, markerCodes);
        public static List<Effect> GetMarkedEffects(Card c, int code) => CallLuaFunction<List<Effect>>("Auxiliary.GetMarkedEffects", c, code);
        public static bool ListsCodeAsMaterial(Card c, params int[] codes) => CallLuaFunction<bool>("Auxiliary.ListsCodeAsMaterial", c, codes);
        public static bool ListsArchetypeAsMaterial(Card c, params int[] setcodes) => CallLuaFunction<bool>("Auxiliary.ListsArchetypeAsMaterial", c, setcodes);
        public static bool ListsCode(Card c, params int[] codes) => CallLuaFunction<bool>("Auxiliary.ListsCode", c, codes);
        public static bool ListsCodeWithArchetype(Card c, params int[] setcodes) => CallLuaFunction<bool>("Auxiliary.ListsCodeWithArchetype", c, setcodes);
        public static bool ListsCardType(Card c, params int[] cardTypes) => CallLuaFunction<bool>("Auxiliary.ListsCardType", c, cardTypes);
        public static bool ListsArchetype(Card c, params int[] setcodes) => CallLuaFunction<bool>("Auxiliary.ListsArchetype", c, setcodes);
        public static bool IsNegatableMonster(Card c) => CallLuaFunction<bool>("Auxiliary.IsNegatableMonster", c);
        public static bool IsNegatableSpellTrap(Card c) => CallLuaFunction<bool>("Auxiliary.IsNegatableSpellTrap", c);
        public static bool IsNegatable(Card c) => CallLuaFunction<bool>("Auxiliary.IsNegatable", c);
        public static bool bdcon(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.bdcon", e, tp, eg, ep, ev, re, r, rp);
        public static bool bdocon(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.bdocon", e, tp, eg, ep, ev, re, r, rp);
        public static bool bdgcon(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.bdgcon", e, tp, eg, ep, ev, re, r, rp);
        public static bool bdogcon(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.bdogcon", e, tp, eg, ep, ev, re, r, rp);
        public static bool dogcon(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.dogcon", e, tp, eg, ep, ev, re, r, rp);
        public static bool exccon(Effect e) => CallLuaFunction<bool>("Auxiliary.exccon", e);
        public static void chainreg(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction("Auxiliary.chainreg", e, tp, eg, ep, ev, re, r, rp);
        public static bool imval1(Effect e, Card c) => CallLuaFunction<bool>("Auxiliary.imval1", e, c);
        public static bool imval2(Effect e, Card c) => CallLuaFunction<bool>("Auxiliary.imval2", e, c);
        public static bool tgoval(Effect e, Effect re, int rp) => CallLuaFunction<bool>("Auxiliary.tgoval", e, re, rp);
        public static bool indsval(Effect e, Effect re, int rp) => CallLuaFunction<bool>("Auxiliary.indsval", e, re, rp);
        public static bool indoval(Effect e, Effect re, int rp) => CallLuaFunction<bool>("Auxiliary.indoval", e, re, rp);
        public static bool HasNonZeroAttack(Card c) => CallLuaFunction<bool>("Auxiliary.HasNonZeroAttack", c);
        public static bool HasNonZeroDefense(Card c) => CallLuaFunction<bool>("Auxiliary.HasNonZeroDefense", c);
        public static void sumreg(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction("Auxiliary.sumreg", e, tp, eg, ep, ev, re, r, rp);
        public static Func<Effect, Effect, int, int, bool> sumlimit(int sumtype) => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.sumlimit", sumtype);
        public static Func<Effect, Effect, int, int, bool> fuslimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.fuslimit");
        public static Func<Effect, Effect, int, int, bool> ritlimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.ritlimit");
        public static Func<Effect, Effect, int, int, bool> synlimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.synlimit");
        public static Func<Effect, Effect, int, int, bool> xyzlimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.xyzlimit");
        public static Func<Effect, Effect, int, int, bool> penlimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.penlimit");
        public static Func<Effect, Effect, int, int, bool> lnklimit => CallLuaFunction<Func<Effect, Effect, int, int, bool>>("Auxiliary.lnklimit");
        public static Effect AddCannotBeNormalSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddCannotBeNormalSummoned", c);
        public static Effect AddCannotBeFlipSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddCannotBeFlipSummoned", c);
        public static Effect AddCannotBeSpecialSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddCannotBeSpecialSummoned", c);
        public static Effect AddMustBeSpecialSummonedByCardEffect(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeSpecialSummonedByCardEffect", c);
        public static Effect AddMustBeRitualSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeRitualSummoned", c);
        public static Effect AddMustBeFusionSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeFusionSummoned", c);
        public static Effect AddMustBeSynchroSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeSynchroSummoned", c);
        public static Effect AddMustBeXyzSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeXyzSummoned", c);
        public static Effect AddMustBePendulumSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBePendulumSummoned", c);
        public static Effect AddMustBeLinkSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustBeLinkSummoned", c);
        public static Effect AddMustFirstBeRitualSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBeRitualSummoned", c);
        public static Effect AddMustFirstBeFusionSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBeFusionSummoned", c);
        public static Effect AddMustFirstBeSynchroSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBeSynchroSummoned", c);
        public static Effect AddMustFirstBeXyzSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBeXyzSummoned", c);
        public static Effect AddMustFirstBePendulumSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBePendulumSummoned", c);
        public static Effect AddMustFirstBeLinkSummoned(Card c) => CallLuaFunction<Effect>("Auxiliary.AddMustFirstBeLinkSummoned", c);
        public static Func<Effect, Card, int, int, bool> cannotmatfilter(params int[] allowed) => CallLuaFunction<Func<Effect, Card, int, int, bool>>("Auxiliary.cannotmatfilter", allowed);
        public static bool damcon1(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction<bool>("Auxiliary.damcon1", e, tp, eg, ep, ev, re, r, rp);
        public static void BeginPuzzle() => CallLuaFunction("Auxiliary.BeginPuzzle");
        public static bool StatChangeDamageStepCondition() => CallLuaFunction<bool>("Auxiliary.StatChangeDamageStepCondition");
        public static Effect DelayedOperation(object cardOrGroup, int phase, int flag, Effect e, int tp, Action<Group, Effect, int, Group, int, int, Effect, int, int> oper, Func<Group, Effect, int, Group, int, int, Effect, int, int, bool> cond = null, int? reset = null, int? resetCount = null, int? hint = null, int? effectDesc = null)
            => CallLuaFunction<Effect>("Auxiliary.DelayedOperation", cardOrGroup, phase, flag, e, tp, oper, cond, reset, resetCount, hint, effectDesc);
        public static Effect RemoveUntil(object cardOrGroup, int? pos, int reason, int phase, int flag, Effect e, int tp, Action<Group, Effect, int, Group, int, int, Effect, int, int> oper, Func<Group, Effect, int, Group, int, int, Effect, int, int, bool> cond = null, int? reset = null, int? resetCount = null, int? hint = null, int? effectDesc = null)
            => CallLuaFunction<Effect>("Auxiliary.RemoveUntil", cardOrGroup, pos, reason, phase, flag, e, tp, oper, cond, reset, resetCount, hint, effectDesc);
        public static void DefaultFieldReturnOp(Group rg, Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp) => CallLuaFunction("Auxiliary.DefaultFieldReturnOp", rg, e, tp, eg, ep, ev, re, r, rp);
    }

    // ------------------------------------------------------------------------
    // Tabela Cost (custos comuns)
    // ------------------------------------------------------------------------

    public static class Cost
    {
        public static bool SelfBanish(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfBanish", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfTribute(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfTribute", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfToGrave(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfToGrave", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfToHand(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfToHand", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfToDeck(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfToDeck", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfToExtra(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfToExtra", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfReveal(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfReveal", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfDiscard(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfDiscard", e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool SelfDiscardToGrave(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.SelfDiscardToGrave", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> RemoveCounterFromSelf(int counterType, int count)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.RemoveCounterFromSelf", counterType, count, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> RemoveCounterFromField(int counterType, int count)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.RemoveCounterFromField", counterType, count, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> SelfChangePosition(int position)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.SelfChangePosition", position, e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool HintSelectedEffect(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.HintSelectedEffect", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> Discard(Func<Card, Effect, int, bool> filter, Card other, object min, object max, Action<Effect, int, Group> op = null)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.Discard", filter, other, min, max, op, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> Reveal(Func<Card, Effect, int, bool> filter, Card other, object min, object max, Action<Effect, int, Group> op = null, int location = LOCATION_HAND)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.Reveal", filter, other, min, max, op, location, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> DetachFromSelf(object min, object max = null, Action<Effect, Group> op = null)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.DetachFromSelf", min, max, op, e, tp, eg, ep, ev, re, r, rp, chk);
        public static bool RemainFieldCost(Effect e, int tp, Group eg, int ep, int ev, Effect re, int r, int rp, int chk)
            => CallLuaFunction<bool>("Cost.RemainFieldCost", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> PayLP(int lpValue, bool payUntil = false)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.PayLP", lpValue, payUntil, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> SoftUseLimitPerChain => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.SoftUseLimitPerChain", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> SoftUseLimitPerBattle => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.SoftUseLimitPerBattle", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> HardUseLimitPerChain => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.HardUseLimitPerChain", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> HardUseLimitPerBattle => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.HardUseLimitPerBattle", e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> SoftOncePerChain => SoftUseLimitPerChain;
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> SoftOncePerBattle => SoftUseLimitPerBattle;
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> HardOncePerChain => HardUseLimitPerChain;
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> HardOncePerBattle => HardUseLimitPerBattle;
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> AND(params Func<Effect, int, Group, int, int, Effect, int, int, int, bool>[] costs)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.AND", costs, e, tp, eg, ep, ev, re, r, rp, chk);
        public static Func<Effect, int, Group, int, int, Effect, int, int, int, bool> Choice(params (Func<Effect, int, Group, int, int, Effect, int, int, int, bool> cost, int desc, Func<Effect, int, Group, int, int, Effect, int, int, int, bool> additionalCheck)[] choices)
            => (e, tp, eg, ep, ev, re, r, rp, chk) => CallLuaFunction<bool>("Cost.Choice", choices, e, tp, eg, ep, ev, re, r, rp, chk);
    }

    // ------------------------------------------------------------------------
    // Tabela bit (operações bit a bit)
    // ------------------------------------------------------------------------

    public static class bit
    {
        public static long band(long a, long b) => CallLuaFunction<long>("bit.band", a, b);
        public static long bor(long a, long b) => CallLuaFunction<long>("bit.bor", a, b);
        public static long bxor(long a, long b) => CallLuaFunction<long>("bit.bxor", a, b);
        public static long lshift(long a, int b) => CallLuaFunction<long>("bit.lshift", a, b);
        public static long rshift(long a, int b) => CallLuaFunction<long>("bit.rshift", a, b);
        public static long bnot(long a) => CallLuaFunction<long>("bit.bnot", a);
        public static long extract(long r, int field, int width = 1) => CallLuaFunction<long>("bit.extract", r, field, width);
        public static long replace(long r, long v, int field, int width = 1) => CallLuaFunction<long>("bit.replace", r, v, field, width);
    }

    // ------------------------------------------------------------------------
    // Funções de extensão para Group (Iter)
    // ------------------------------------------------------------------------

    public static IEnumerator<Card> Iter(this Group g) => Auxiliary.Next(g);

    // ------------------------------------------------------------------------
    // Funções soltas no escopo global (GetID, etc.)
    // ------------------------------------------------------------------------

    public static (DynValue selfTable, int selfCode) GetID()
        => CallLuaFunction<(DynValue, int)>("GetID");

    // ------------------------------------------------------------------------
    // Métodos auxiliares para chamar funções Lua
    // ------------------------------------------------------------------------

    private static T CallLuaFunction<T>(string functionName, params object[] args)
    {
        if (_luaScript == null)
            throw new InvalidOperationException("LuaUtilityBridge não foi inicializado. Chame Initialize() primeiro.");
        var func = _luaScript.Globals.Get(functionName);
        if (func.Type != DataType.Function)
            throw new InvalidOperationException($"Função Lua '{functionName}' não encontrada.");
        var result = _luaScript.Call(func, args);
        return result.ToObject<T>();
    }

    private static void CallLuaFunction(string functionName, params object[] args)
    {
        if (_luaScript == null)
            throw new InvalidOperationException("LuaUtilityBridge não foi inicializado. Chame Initialize() primeiro.");
        var func = _luaScript.Globals.Get(functionName);
        if (func.Type != DataType.Function)
            throw new InvalidOperationException($"Função Lua '{functionName}' não encontrada.");
        _luaScript.Call(func, args);
    }

    // Placeholder para constantes que podem ser necessárias (definir conforme seu projeto)
    private const int LOCATION_HAND = 0x02;
    private const int LOCATION_MZONE = 0x04;
    private const int LOCATION_EXTRA = 0x40;
    private const int LOCATION_REMOVED = 0x20;
    private const int LOCATION_ONFIELD = 0x0c;
    private const int LOCATION_SZONE = 0x08;
    private const int LOCATION_DECK = 0x01;
    private const int LOCATION_GRAVE = 0x10;
    private const int LOCATION_PZONE = 0x80;
    private const int LOCATION_MMZONE = 0x04;
    private const int LOCATION_EMZONE = 0x100;
    private const int PLAYER_NONE = 2;
    private const int CATEGORY_DAMAGE = 0x80000;
    private const int CATEGORY_RECOVER = 0x100000;
    private const int PHASE_DAMAGE = 0x20;
    private const int RESET_EVENT = 0x1000;
    private const int RESET_CHAIN = 0x80000000;
    private const int RESET_TURN_SET = 0x20000;
    private const int RESET_TOFIELD = 0x1000000;
    private const int RESET_LEAVE = 0x800000;
    private const int RESET_TODECK = 0x400000;
    private const int RESET_TOHAND = 0x200000;
    private const int RESET_TEMP_REMOVE = 0x100000;
    private const int RESET_REMOVE = 0x80000;
    private const int RESET_TOGRAVE = 0x40000;
    private const int RESET_DISABLE = 0x10000;
    private const int RESETS_STANDARD = 0x1000000 | 0x800000 | 0x400000 | 0x200000 | 0x100000 | 0x80000 | 0x40000 | 0x20000;
    private const int RESETS_STANDARD_DISABLE = RESETS_STANDARD | RESET_DISABLE;
    private const int RESET_PHASE = 0x40000000;
    private const int PHASE_END = 0x200;
    private const int RESET_EVENT_PHASE_END = RESET_EVENT | RESET_PHASE | PHASE_END;
    private const int EFFECT_FLAG_CANNOT_DISABLE = 0x4000;
    private const int EFFECT_FLAG_UNCOPYABLE = 0x1000;
    private const int EFFECT_FLAG_IGNORE_IMMUNE = 0x2000;
    private const int EFFECT_FLAG_SET_AVAILABLE = 0x40000;
    private const int EFFECT_FLAG_CLIENT_HINT = 0x80000;
    private const int EFFECT_FLAG_OATH = 0x200;
    private const int EFFECT_FLAG_PLAYER_TARGET = 0x400;
    private const int EFFECT_FLAG2_MAJESTIC_MUST_COPY = 0x1;
    private const int EFFECT_TYPE_SINGLE = 0x0001;
    private const int EFFECT_TYPE_FIELD = 0x0002;
    private const int EFFECT_TYPE_CONTINUOUS = 0x8000;
    private const int EFFECT_TYPE_ACTIONS = 0x8000;
    private const int TYPE_MONSTER = 0x1;
    private const int TYPE_SPELL = 0x2;
    private const int TYPE_TRAP = 0x4;
    private const int TYPE_QUICKPLAY = 0x10000;
    private const int TYPE_CONTINUOUS = 0x20000;
    private const int TYPE_EQUIP = 0x40000;
    private const int TYPE_FIELD = 0x80000;
    private const int TYPE_RITUAL = 0x80;
    private const int TYPE_LINK = 0x4000000;
    private const int TYPE_COUNTER = 0x100000;
    private const int TYPE_FUSION = 0x40;
    private const int TYPE_SYNCHRO = 0x2000;
    private const int TYPE_XYZ = 0x800000;
    private const int TYPE_PENDULUM = 0x1000000;
    private const int TYPE_EFFECT = 0x20;
    private const int TYPE_TRAPMONSTER = 0x100;
    private const int ATTRIBUTE_ALL = 0x7f;
    private const int RACE_ALL = 0x3ffffff;
    private const int POS_FACEUP = 0x5;
    private const int POS_FACEDOWN = 0xa;
    private const int POS_FACEUP_ATTACK = 0x1;
    private const int POS_FACEDOWN_DEFENSE = 0x8;
    private const int REASON_COST = 0x8;
    private const int REASON_DISCARD = 0x100;
    private const int REASON_DESTROY = 0x2;
    private const int REASON_RETURN = 0x400;
    private const int REASON_BATTLE = 0x4;
    private const int REASON_TEMPORARY = 0x2000;
    private const int REASON_EFFECT = 0x1;
    private const int REASON_RULE = 0x8000;
    private const int SUMMON_TYPE_NORMAL = 0x1000000;
    private const int SUMMON_TYPE_TRIBUTE = 0x2000000;
    private const int SUMMON_TYPE_FLIP = 0x4000000;
    private const int SUMMON_TYPE_GEMINI = 0x8000000;
    private const int SUMMON_TYPE_SPECIAL = 0x10000000;
    private const int SUMMON_TYPE_RITUAL = 0x20000000;
    private const int SUMMON_TYPE_FUSION = 0x40000000;
    private const int SUMMON_TYPE_SYNCHRO = 0x80000000;
    private const int SUMMON_TYPE_XYZ = 0x100000000;
    private const int SUMMON_TYPE_PENDULUM = 0x200000000;
    private const int SUMMON_TYPE_LINK = 0x400000000;
    private const int STATUS_BATTLE_DESTROYED = 0x4000;
    private const int STATUS_NO_LEVEL = 0x20;
    private const int STATUS_OPPO_BATTLE = 0x10000000;
    private const int STATUS_PROC_COMPLETE = 0x8;
    private const int SEQ_DECKSHUFFLE = 1;
    private const int SEQ_DECKTOP = 0;
    private const int SEQ_DECKBOTTOM = 1;
    private const int HINT_SELECTMSG = 2;
    private const int HINTMSG_RESOLVEEFFECT = 634;
    private const int HINTMSG_TOZONE = 503;
    private const int HINTMSG_ATTRIBUTE = 505;
    private const int HINTMSG_RACE = 506;
    private const int HINTMSG_CONFIRM = 508;
    private const int HINTMSG_TOFIELD = 509;
    private const int HINTMSG_TOZONE = 503;
    private const int HINTMSG_SPSUMMON = 501;
    private const int HINTMSG_MONSTER = 500;
    private const int HINTMSG_FIELD = 502;
    private const int HINTMSG_RELEASE = 504;
    private const int HINTMSG_DISCARD = 507;
    // etc. – adicione os valores que seu projeto já define.
}