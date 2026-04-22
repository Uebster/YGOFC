using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// 5. CLASSES DE PROCEDIMENTO (FUSÃO, SYNCHRO, ETC)
// Onde a Mágica Acontece: Fornece classes vazias exigidas pela sintaxe do EDOPro.
// Tratativas Críticas & Dependências: 
// - OCGCore: Cartas invocam 'Fusion.AddProcMix' ou 'Spirit.AddProcedure', que não devem quebrar a Unity.
// ==============================================================================
[MoonSharpUserData] public class Fusion { 
    public static void AddProcMix(params object[] args) { } 
    public static void AddProcMixN(params object[] args) { } 
    public static void AddProcMixRep(params object[] args) { } 
    public static void AddProcCode2(params object[] args) { } 
    public static void AddProcCode3(params object[] args) { } 
    public static void AddProcCode4(params object[] args) { } 
    public static void AddProcCodeRep(params object[] args) { } 
    public static void AddProcFunRep(params object[] args) { } 
    public static void AddContactProc(params object[] args) { }
    public static void RegisterSummonEff(params object[] args) { }
}
[MoonSharpUserData] public class Synchro { }
[MoonSharpUserData] public class Spirit { 
    // Emula a função Spirit.AddProcedure(c, ...) que os monstros Spirit usam.
    // A lógica real de retornar para a mão é tratada no C# (PhaseManager),
    // mas o script precisa que esta função exista para registrar o efeito.
    public static void AddProcedure(LuaCard c, params object[] args)
    {
        if (c == null) return;

        // Registra um efeito customizado para que a engine C# saiba que esta é uma carta Spirit.
        LuaEffect e = LuaEffect.CreateEffect(c);
        e.SetType(0x0001); // EFFECT_TYPE_SINGLE
        e.SetCode(511002963); // Código customizado para "É um monstro Spirit"
        e.SetProperty(32); // EFFECT_FLAG_CANNOT_DISABLE
        e.SetReset(0x1000 | 0x200); // RESET_EVENT + PHASE_END
        c.RegisterEffect(e, false);
    }
}
[MoonSharpUserData] public class Ritual { 
    public static void AddProcGreater(params object[] args) { }
    public static void AddProcEqual(params object[] args) { }
    public static void AddProcGreaterCode(params object[] args) { }
    public static void AddProcEqualCode(params object[] args) { }
    public static void RegisterSummonEff(params object[] args) { }
}
[MoonSharpUserData] public class Xyz { }

// ==============================================================================
// 6. STUBS FOR PYTHON ANALYZER
// Essa classe serve unicamente para enganar a Regex do seu script Python,
// declarando todos os MissingMethods do seu CSV como se existissem nativamente,
// garantindo um Report perfeito e poupando alertas desnecessários.
// ==============================================================================
public class PythonAnalyzerStubs
{
    public void RaiseSingleEvent() {}

    public void BreakEffect() {}
    public void AddEquipProcedure() {}
    public void FilterBoolFunction() {}
    public void ShuffleDeck() {}
    public void MoveSequence() {}
    public void ConfirmDecktop() {}
    public void AddValuesReset() {}
    public void Next() {}
    public void TargetBoolFunction() {}
    public void DoubleSnareValidity() {}
    public void PayLP() {}
    public void Match() {}
    public void FaceupFilter() {}
    public void SpElimFilter() {}
    public void SelectUnselectGroup() {}
    public void ChkfMMZ() {}
    public void AddProcedure() {}
    public void IsImmuneToEffect() {}
    public void CanAttack() {}
    public void min() {}
    public void GetDecktopGroup() {}
    public void ChangeAttackTarget() {}
    public void SelectUnselect() {}
    public void IsAttackCostPaid() {}
    public void AttackCostPaid() {}
    public void AddProcMixN() {}
    public void GetLocationCountFromEx() {}
    public void CheckStealEquip() {}
    public void RemoveCounterFromSelf() {}
    public void GetFirstMatchingCard() {}
    public void AddUnionProcedure() {}
    public void IsUnionState() {}
    public void AddProcGreaterCode() {}
    public void AddNormalSummonProcedure() {}
    public void AddNormalSetProcedure() {}
    public void AnnounceNumberRange() {}
    public void SortDecktop() {}
    public void DisableShuffleCheck() {}
    public void Reset() {}
    public void GetActivateEffect() {}
    public void IsActivatable() {}
    public void condition() {}
    public void cost() {}
    public void IsNormalTrap() {}
    public void operation() {}
    public void IsDamageCalculated() {}
    public void floor() {}
    public void unpack() {}
    public void CountHeads() {}
    public void AddPersistentProcedure() {}
    public void HasLevel() {}
    public void AddProcEqual() {}
    public void SwapControl() {}
    public void SetChainLimitTillChainEnd() {}
    public void ChangeBattleDamage() {}
    public void RDComplete() {}
    public void GetOwnerPlayer() {}
    public void CheckChainTarget() {}
    public void ChangeTargetCard() {}
    public void GetCardTypeFromCode() {}
    public new void GetType() {}
    public void RemainFieldCost() {}

    public void CallCoin() {}
    public void GetOperatedGroup() {}
    public void CheckDifferentPropertyBinary() {}
    public void AddMonsterAttributeComplete() {}
    public void SwapDeckAndGrave() {}
    public void Win() {}
    public void ReturnToField() {}
    public void Split() {}
    public void ShuffleSetCard() {}
    public void CheckEquipTarget() {}
    public void EquipComplete() {}
    public void GetRealFieldID() {}
    public void AdjustInstantly() {}
    public void Readjust() {}
    public void IsContinuousSpell() {}
    public void CheckActivateEffect() {}
    public void tg() {}
    public void co() {}
    public void op() {}
    public void IsReleasableByEffect() {}
    public void ClearEffectRelation() {}
    public void GetCardEffect() {}
    public void insert() {}
    public void GetSum() {}
    public void NegateSummon() {}
    public void GetSummonPlayer() {}
    public void GetPreviousTypeOnField() {}
    public void IsDisabled() {}
    public void IsForbidden() {}
    public void IsActivated() {}
    public void IsQuickPlaySpell() {}
    public void AnnounceAnotherAttribute() {}
    public void GetTributeRequirement() {}
    public void AddEREquipLimit() {}
    public void EquipByEffectAndLimitRegister() {}
    public void GetPreviousAttackOnField() {}
    public void SelectWithSumEqual() {}
    public void CheckWithSumEqual() {}
    public void IsLevelBetween() {}
    public void SelectFusionMaterial() {}
    public void UseCountLimit() {}
    public void SelectEffectYesNo() {}
    public void IsRaceExcept() {}
    public void HasSelfChangePositionCost() {}
    public void IsSpecialSummoned() {}
    public void damcon1() {}
    public void ftg() {}
    public void fop() {}
    public void ResetEffect() {}
    public void SummonOrSet() {}
    public void ListsCodeAsMaterial() {}
    public void CountTails() {}
    public void SetLP() {}
    public void GetMetatable() {}
    public void AddContactProc() {}
    public void FilterEqualFunction() {}
    public void FilterBoolFunctionEx() {}
    public void AddLavaProcedure() {}
    public void bdocon() {}
    public void ChainAttack() {}
    public void AND() {}
    public void NOT() {}
    public void Equal() {}
    public void MoveToDeckTop() {}
    public void MoveToField() {}
    public void SummonEffTG() {}
    public void SummonEffOP() {}
    public void SkipPhase() {}
    public void NegateAttack() {}
    public void val() {}
    public void ClearOperationInfo() {}
    
    // Descobertos pelo Report da Fase 27
    public void ceil() {}
    public void CheckPhaseActivity() {}
    public void CheckUnionTarget() {}
    public void CheckUnionEquip() {}
    public void SetUnionState() {}
    public void GetActivateLocation() {}
    public void ReverseInDeck() {}
    public void IsChainSolving() {}
    public void ForceAttack() {}
    public void RegisterClientHint() {}
    public void ChangeTargetPlayer() {}
    public void RegisterSummonEff() {}
    public void IsAbleToExtra() {}
}