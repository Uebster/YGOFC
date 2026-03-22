using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SynchroManager: Gerencia invocações Synchro
/// Regras: 1+ Tuner + 1+ Não-Tuner, soma de níveis = nível do Synchro
/// </summary>
public class SynchroManager : MonoBehaviour
{
    public static SynchroManager Instance;
    private SummonManager summonManager;
    private GameManager gameManager;

    private void Awake()
    {
        Instance = this;
        summonManager = FindObjectOfType<SummonManager>();
        gameManager = GameManager.Instance;
    }

    /// <summary>
    /// Valida se pode invocar Synchro com os materiais selecionados
    /// </summary>
    public bool ValidateSynchro(CardData synchroMonster, List<CardData> selectedMaterials, bool isPlayerSide)
    {
        if (synchroMonster == null || !synchroMonster.type.Contains("Synchro"))
        {
            Debug.LogWarning("[SynchroManager] Carta não é Synchro Monster!");
            return false;
        }

        if (selectedMaterials == null || selectedMaterials.Count == 0)
        {
            Debug.LogWarning("[SynchroManager] Nenhum material selecionado!");
            return false;
        }

        int tunerCount = 0;
        int nonTunerCount = 0;
        int levelSum = 0;

        foreach (var material in selectedMaterials)
        {
            if (!material.type.Contains("Monster"))
            {
                Debug.LogWarning($"[SynchroManager] {material.name} não é monstro!");
                return false;
            }

            // Contar Tuner vs Não-Tuner
            if (material.property.Contains("Tuner") || material.type.Contains("Tuner"))
            {
                tunerCount++;
            }
            else
            {
                nonTunerCount++;
            }

            levelSum += material.level;
        }

        // Validar regras
        if (tunerCount == 0)
        {
            Debug.LogWarning("[SynchroManager] Precisa de pelo menos 1 Tuner!");
            return false;
        }

        if (nonTunerCount == 0)
        {
            Debug.LogWarning("[SynchroManager] Precisa de pelo menos 1 Não-Tuner!");
            return false;
        }

        if (levelSum != synchroMonster.level)
        {
            Debug.LogWarning($"[SynchroManager] Soma de níveis inválida: {levelSum} != {synchroMonster.level}");
            return false;
        }

        Debug.Log($"[SynchroManager] ✅ Validação PASSOU: {tunerCount} Tuner(s) + {nonTunerCount} Não-Tuner(s) = Nível {levelSum}");
        return true;
    }

    /// <summary>
    /// Realiza a invocação Synchro
    /// </summary>
    public CardDisplay PerformSynchroSummon(CardData synchroMonster, List<CardData> materials, bool isPlayer)
    {
        Debug.Log($"[SynchroManager] 🔄 Invocando Synchro: {synchroMonster.name}");

        // 1. Verificar se é válido
        if (!ValidateSynchro(synchroMonster, materials, isPlayer))
        {
            Debug.LogError("[SynchroManager] Validação falhou!");
            return null;
        }

        // 2. Remover materiais do campo e enviar para GY
        foreach (var material in materials)
        {
            gameManager.SendToGraveyard(material, isPlayer, CardLocation.Field, SendReason.Cost);
        }

        // 3. Invocar Synchro Monster
        CardDisplay synchroCard = gameManager.SpecialSummonFromData(synchroMonster, isPlayer, summonType: 0x46000000, faceUp: true, defense: false);

        // 4. Incrementar contador
        if (summonManager != null)
        {
            summonManager.synchroSummonCount++;
        }

        Debug.Log($"[SynchroManager] ✅ Synchro Summon realizado! Total: {(summonManager != null ? summonManager.synchroSummonCount : 1)}");
        return synchroCard;
    }
}
