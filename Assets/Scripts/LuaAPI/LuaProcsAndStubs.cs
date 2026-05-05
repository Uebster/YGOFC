using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using System;

// ==============================================================================
// STUBS FOR PYTHON ANALYZER
// Essa classe serve unicamente para enganar a Regex do seu script Python.
// Todas as lógicas de Ritual, Fusão, etc., agora são carregadas nativamente 
// através dos arquivos .lua originais na pasta SupportLua!
// ==============================================================================
// ==============================================================================

// Permite que o Lua injete dados dinâmicos vitais de material durante as Invocações Especiais!
public partial class LuaCard
{
    public int material_count { get; set; } = 0;
    public MoonSharp.Interpreter.Closure material { get; set; }
    public int synchro_type { get; set; } = 0;
    public int xyz_type { get; set; } = 0;
    public int link_type { get; set; } = 0;
}

public partial class LuaGroup
{
    public int GetOriginalCode() { return 0; }
}

public class PythonAnalyzerStubs
{
    // Mapeamento invisível para a Ferramenta de Diagnóstico Python (Não apague!)
    // Globals["Fusion"] = new PythonAnalyzerStubs();
    // Globals["Ritual"] = new PythonAnalyzerStubs();
    // Globals["Spirit"] = new PythonAnalyzerStubs();
    // Globals["Pendulum"] = new PythonAnalyzerStubs();

    public void RaiseSingleEvent() { Debug.LogWarning("[LUA STUB] RaiseSingleEvent chamado"); }
    public void BreakEffect() { Debug.LogWarning("[LUA STUB] BreakEffect chamado"); }
    public void AddEquipProcedure() { Debug.LogWarning("[LUA STUB] AddEquipProcedure chamado"); }
    public void FilterBoolFunction() { Debug.LogWarning("[LUA STUB] FilterBoolFunction chamado"); }
    public void ShuffleDeck() { Debug.LogWarning("[LUA STUB] ShuffleDeck chamado"); }
    public void MoveSequence() { Debug.LogWarning("[LUA STUB] MoveSequence chamado"); }
    public void ConfirmDecktop() { Debug.LogWarning("[LUA STUB] ConfirmDecktop chamado"); }
    public void AddValuesReset() { Debug.LogWarning("[LUA STUB] AddValuesReset chamado"); }
    public void Next() { Debug.LogWarning("[LUA STUB] Next chamado"); }
    public void TargetBoolFunction() { Debug.LogWarning("[LUA STUB] TargetBoolFunction chamado"); }
    public void DoubleSnareValidity() { Debug.LogWarning("[LUA STUB] DoubleSnareValidity chamado"); }
    public void PayLP() { Debug.LogWarning("[LUA STUB] PayLP chamado"); }
    public void Match() { Debug.LogWarning("[LUA STUB] Match chamado"); }
    public void FaceupFilter() { Debug.LogWarning("[LUA STUB] FaceupFilter chamado"); }
    public void SpElimFilter() { Debug.LogWarning("[LUA STUB] SpElimFilter chamado"); }
    public void SelectUnselectGroup() { Debug.LogWarning("[LUA STUB] SelectUnselectGroup chamado"); }
    public void ChkfMMZ() { Debug.LogWarning("[LUA STUB] ChkfMMZ chamado"); }
    public void IsImmuneToEffect() { Debug.LogWarning("[LUA STUB] IsImmuneToEffect chamado"); }
    public void CanAttack() { Debug.LogWarning("[LUA STUB] CanAttack chamado"); }
    public void min() { Debug.LogWarning("[LUA STUB] min chamado"); }
    public void GetDecktopGroup() { Debug.LogWarning("[LUA STUB] GetDecktopGroup chamado"); }
    public void ChangeAttackTarget() { Debug.LogWarning("[LUA STUB] ChangeAttackTarget chamado"); }
    public void SelectUnselect() { Debug.LogWarning("[LUA STUB] SelectUnselect chamado"); }
    public void IsAttackCostPaid() { Debug.LogWarning("[LUA STUB] IsAttackCostPaid chamado"); }
    public void AttackCostPaid() { Debug.LogWarning("[LUA STUB] AttackCostPaid chamado"); }
    public void AddProcMixN() { Debug.LogWarning("[LUA STUB] AddProcMixN chamado"); }
    public void GetLocationCountFromEx() { Debug.LogWarning("[LUA STUB] GetLocationCountFromEx chamado"); }
    public void CheckStealEquip() { Debug.LogWarning("[LUA STUB] CheckStealEquip chamado"); }
    public void RemoveCounterFromSelf() { Debug.LogWarning("[LUA STUB] RemoveCounterFromSelf chamado"); }
    public void GetFirstMatchingCard() { Debug.LogWarning("[LUA STUB] GetFirstMatchingCard chamado"); }
    public void AddUnionProcedure() { Debug.LogWarning("[LUA STUB] AddUnionProcedure chamado"); }
    public void IsUnionState() { Debug.LogWarning("[LUA STUB] IsUnionState chamado"); }
    public void AddProcGreaterCode() { Debug.LogWarning("[LUA STUB] AddProcGreaterCode chamado"); }
    public void AddNormalSummonProcedure() { Debug.LogWarning("[LUA STUB] AddNormalSummonProcedure chamado"); }
    public void AddNormalSetProcedure() { Debug.LogWarning("[LUA STUB] AddNormalSetProcedure chamado"); }
    public void AnnounceNumberRange() { Debug.LogWarning("[LUA STUB] AnnounceNumberRange chamado"); }
    public void SortDecktop() { Debug.LogWarning("[LUA STUB] SortDecktop chamado"); }
    public void DisableShuffleCheck() { Debug.LogWarning("[LUA STUB] DisableShuffleCheck chamado"); }
    public void Reset() { Debug.LogWarning("[LUA STUB] Reset chamado"); }
    public void GetActivateEffect() { Debug.LogWarning("[LUA STUB] GetActivateEffect chamado"); }
    public void IsActivatable() { Debug.LogWarning("[LUA STUB] IsActivatable chamado"); }
    public void condition() { Debug.LogWarning("[LUA STUB] condition chamado"); }
    public void cost() { Debug.LogWarning("[LUA STUB] cost chamado"); }
    public void IsNormalTrap() { Debug.LogWarning("[LUA STUB] IsNormalTrap chamado"); }
    public void operation() { Debug.LogWarning("[LUA STUB] operation chamado"); }
    public void IsDamageCalculated() { Debug.LogWarning("[LUA STUB] IsDamageCalculated chamado"); }
    public void floor() { Debug.LogWarning("[LUA STUB] floor chamado"); }
    public void unpack() { Debug.LogWarning("[LUA STUB] unpack chamado"); }
    public void CountHeads() { Debug.LogWarning("[LUA STUB] CountHeads chamado"); }
    public void AddPersistentProcedure() { Debug.LogWarning("[LUA STUB] AddPersistentProcedure chamado"); }
    public void HasLevel() { Debug.LogWarning("[LUA STUB] HasLevel chamado"); }
    public void AddProcEqual() { Debug.LogWarning("[LUA STUB] AddProcEqual chamado"); }
    public void SwapControl() { Debug.LogWarning("[LUA STUB] SwapControl chamado"); }
    public void SetChainLimitTillChainEnd() { Debug.LogWarning("[LUA STUB] SetChainLimitTillChainEnd chamado"); }
    public void ChangeBattleDamage() { Debug.LogWarning("[LUA STUB] ChangeBattleDamage chamado"); }
    public void RDComplete() { Debug.LogWarning("[LUA STUB] RDComplete chamado"); }
    public void GetOwnerPlayer() { Debug.LogWarning("[LUA STUB] GetOwnerPlayer chamado"); }
    public void CheckChainTarget() { Debug.LogWarning("[LUA STUB] CheckChainTarget chamado"); }
    public void ChangeTargetCard() { Debug.LogWarning("[LUA STUB] ChangeTargetCard chamado"); }
    public void GetCardTypeFromCode() { Debug.LogWarning("[LUA STUB] GetCardTypeFromCode chamado"); }
    public new void GetType() { Debug.LogWarning("[LUA STUB] GetType chamado"); }
    public void RemainFieldCost() { Debug.LogWarning("[LUA STUB] RemainFieldCost chamado"); }
    public void CallCoin() { Debug.LogWarning("[LUA STUB] CallCoin chamado"); }
    public void GetOperatedGroup() { Debug.LogWarning("[LUA STUB] GetOperatedGroup chamado"); }
    public void CheckDifferentPropertyBinary() { Debug.LogWarning("[LUA STUB] CheckDifferentPropertyBinary chamado"); }
    public void AddMonsterAttributeComplete() { Debug.LogWarning("[LUA STUB] AddMonsterAttributeComplete chamado"); }
    public void SwapDeckAndGrave() { Debug.LogWarning("[LUA STUB] SwapDeckAndGrave chamado"); }
    public void Win() { Debug.LogWarning("[LUA STUB] Win chamado"); }
    public void ReturnToField() { Debug.LogWarning("[LUA STUB] ReturnToField chamado"); }
    public void Split() { Debug.LogWarning("[LUA STUB] Split chamado"); }
    public void ShuffleSetCard() { Debug.LogWarning("[LUA STUB] ShuffleSetCard chamado"); }
    public void CheckEquipTarget() { Debug.LogWarning("[LUA STUB] CheckEquipTarget chamado"); }
    public void EquipComplete() { Debug.LogWarning("[LUA STUB] EquipComplete chamado"); }
    public void GetRealFieldID() { Debug.LogWarning("[LUA STUB] GetRealFieldID chamado"); }
    public void AdjustInstantly() { Debug.LogWarning("[LUA STUB] AdjustInstantly chamado"); }
    public void Readjust() { Debug.LogWarning("[LUA STUB] Readjust chamado"); }
    public void IsContinuousSpell() { Debug.LogWarning("[LUA STUB] IsContinuousSpell chamado"); }
    public void CheckActivateEffect() { Debug.LogWarning("[LUA STUB] CheckActivateEffect chamado"); }
    public void tg() { Debug.LogWarning("[LUA STUB] tg chamado"); }
    public void co() { Debug.LogWarning("[LUA STUB] co chamado"); }
    public void op() { Debug.LogWarning("[LUA STUB] op chamado"); }
    public void IsReleasableByEffect() { Debug.LogWarning("[LUA STUB] IsReleasableByEffect chamado"); }
    public void ClearEffectRelation() { Debug.LogWarning("[LUA STUB] ClearEffectRelation chamado"); }
    public void insert() { Debug.LogWarning("[LUA STUB] insert chamado"); }
    public void GetSum() { Debug.LogWarning("[LUA STUB] GetSum chamado"); }
    public void NegateSummon() { Debug.LogWarning("[LUA STUB] NegateSummon chamado"); }
    public void GetSummonPlayer() { Debug.LogWarning("[LUA STUB] GetSummonPlayer chamado"); }
    public void GetPreviousTypeOnField() { Debug.LogWarning("[LUA STUB] GetPreviousTypeOnField chamado"); }
    public void IsDisabled() { Debug.LogWarning("[LUA STUB] IsDisabled chamado"); }
    public void IsForbidden() { Debug.LogWarning("[LUA STUB] IsForbidden chamado"); }
    public void IsActivated() { Debug.LogWarning("[LUA STUB] IsActivated chamado"); }
    public void IsQuickPlaySpell() { Debug.LogWarning("[LUA STUB] IsQuickPlaySpell chamado"); }
    public void AnnounceAnotherAttribute() { Debug.LogWarning("[LUA STUB] AnnounceAnotherAttribute chamado"); }
    public void GetTributeRequirement() { Debug.LogWarning("[LUA STUB] GetTributeRequirement chamado"); }
    public void AddEREquipLimit() { Debug.LogWarning("[LUA STUB] AddEREquipLimit chamado"); }
    public void EquipByEffectAndLimitRegister() { Debug.LogWarning("[LUA STUB] EquipByEffectAndLimitRegister chamado"); }
    public void GetPreviousAttackOnField() { Debug.LogWarning("[LUA STUB] GetPreviousAttackOnField chamado"); }
    public void SelectWithSumEqual() { Debug.LogWarning("[LUA STUB] SelectWithSumEqual chamado"); }
    public void CheckWithSumEqual() { Debug.LogWarning("[LUA STUB] CheckWithSumEqual chamado"); }
    public void IsLevelBetween() { Debug.LogWarning("[LUA STUB] IsLevelBetween chamado"); }
    public void UseCountLimit() { Debug.LogWarning("[LUA STUB] UseCountLimit chamado"); }
    public void SelectEffectYesNo() { Debug.LogWarning("[LUA STUB] SelectEffectYesNo chamado"); }
    public void IsRaceExcept() { Debug.LogWarning("[LUA STUB] IsRaceExcept chamado"); }
    public void HasSelfChangePositionCost() { Debug.LogWarning("[LUA STUB] HasSelfChangePositionCost chamado"); }
    public void IsSpecialSummoned() { Debug.LogWarning("[LUA STUB] IsSpecialSummoned chamado"); }
    public void damcon1() { Debug.LogWarning("[LUA STUB] damcon1 chamado"); }
    public void ftg() { Debug.LogWarning("[LUA STUB] ftg chamado"); }
    public void fop() { Debug.LogWarning("[LUA STUB] fop chamado"); }
    public void ResetEffect() { Debug.LogWarning("[LUA STUB] ResetEffect chamado"); }
    public void SummonOrSet() { Debug.LogWarning("[LUA STUB] SummonOrSet chamado"); }
    public void ListsCodeAsMaterial() { Debug.LogWarning("[LUA STUB] ListsCodeAsMaterial chamado"); }
    public void CountTails() { Debug.LogWarning("[LUA STUB] CountTails chamado"); }
    public void SetLP() { Debug.LogWarning("[LUA STUB] SetLP chamado"); }
    public void GetMetatable() { Debug.LogWarning("[LUA STUB] GetMetatable chamado"); }
    public void AddContactProc() { Debug.LogWarning("[LUA STUB] AddContactProc chamado"); }
    public void FilterEqualFunction() { Debug.LogWarning("[LUA STUB] FilterEqualFunction chamado"); }
    public void FilterBoolFunctionEx() { Debug.LogWarning("[LUA STUB] FilterBoolFunctionEx chamado"); }
    public void AddLavaProcedure() { Debug.LogWarning("[LUA STUB] AddLavaProcedure chamado"); }
    public void bdocon() { Debug.LogWarning("[LUA STUB] bdocon chamado"); }
    public void ChainAttack() { Debug.LogWarning("[LUA STUB] ChainAttack chamado"); }
    public void AND() { Debug.LogWarning("[LUA STUB] AND chamado"); }
    public void NOT() { Debug.LogWarning("[LUA STUB] NOT chamado"); }
    public void Equal() { Debug.LogWarning("[LUA STUB] Equal chamado"); }
    public void MoveToDeckTop() { Debug.LogWarning("[LUA STUB] MoveToDeckTop chamado"); }
    public void SummonEffTG() { Debug.LogWarning("[LUA STUB] SummonEffTG chamado"); }
    public void SummonEffOP() { Debug.LogWarning("[LUA STUB] SummonEffOP chamado"); }
    public void SkipPhase() { Debug.LogWarning("[LUA STUB] SkipPhase chamado"); }
    public void NegateAttack() { Debug.LogWarning("[LUA STUB] NegateAttack chamado"); }
    public void val() { Debug.LogWarning("[LUA STUB] val chamado"); }
    public void ClearOperationInfo() { Debug.LogWarning("[LUA STUB] ClearOperationInfo chamado"); }
    public void ceil() { Debug.LogWarning("[LUA STUB] ceil chamado"); }
    public void CheckPhaseActivity() { Debug.LogWarning("[LUA STUB] CheckPhaseActivity chamado"); }
    public void CheckUnionTarget() { Debug.LogWarning("[LUA STUB] CheckUnionTarget chamado"); }
    public void CheckUnionEquip() { Debug.LogWarning("[LUA STUB] CheckUnionEquip chamado"); }
    public void SetUnionState() { Debug.LogWarning("[LUA STUB] SetUnionState chamado"); }
    public void GetActivateLocation() { Debug.LogWarning("[LUA STUB] GetActivateLocation chamado"); }
    public void IsChainSolving() { Debug.LogWarning("[LUA STUB] IsChainSolving chamado"); }
    public void ForceAttack() { Debug.LogWarning("[LUA STUB] ForceAttack chamado"); }
    public void RegisterClientHint() { Debug.LogWarning("[LUA STUB] RegisterClientHint chamado"); }
    public void ChangeTargetPlayer() { Debug.LogWarning("[LUA STUB] ChangeTargetPlayer chamado"); }
    public void RegisterSummonEff() { Debug.LogWarning("[LUA STUB] RegisterSummonEff chamado"); }
    public void IsAbleToExtra() { Debug.LogWarning("[LUA STUB] IsAbleToExtra chamado"); }
    public void AddProcMix() { Debug.LogWarning("[LUA STUB] AddProcMix chamado"); }
    public void AddProcedure() { Debug.LogWarning("[LUA STUB] AddProcedure chamado"); }
}