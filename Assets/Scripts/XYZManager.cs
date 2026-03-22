using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// XYZManager: Gerencia invocações XYZ/Xyz
/// Regras: Todos os materiais com MESMO NÍVEL, quantidade = Rank do XYZ
/// </summary>
public class XYZManager : MonoBehaviour
{
    public static XYZManager Instance;
    private SummonManager summonManager;
    private GameManager gameManager;

    private void Awake()
    {
        Instance = this;
        summonManager = FindObjectOfType<SummonManager>();
        gameManager = GameManager.Instance;
    }

    /// <summary>
    /// Valida se pode invocar XYZ com os materiais selecionados
    /// </summary>
    public bool ValidateXYZ(CardData xyzMonster, List<CardData> selectedMaterials, bool isPlayerSide)
    {
        if (xyzMonster == null || (!xyzMonster.type.Contains("XYZ") && !xyzMonster.type.Contains("Xyz")))
        {
            Debug.LogWarning("[XYZManager] Carta não é XYZ Monster!");
            return false;
        }

        if (selectedMaterials == null || selectedMaterials.Count == 0)
        {
            Debug.LogWarning("[XYZManager] Nenhum material selecionado!");
            return false;
        }

        // Rank do XYZ = nível (em YGO, XYZ rank é armazenado como level)
        int xyzRank = xyzMonster.level;

        // 1. Quantidade de materiais = Rank
        if (selectedMaterials.Count != xyzRank)
        {
            Debug.LogWarning($"[XYZManager] Quantidade incorreta: {selectedMaterials.Count} materiais, Rank {xyzRank} esperado");
            return false;
        }

        // 2. TODOS os materiais devem ter MESMO NÍVEL
        int firstLevel = selectedMaterials[0].level;
        foreach (var material in selectedMaterials)
        {
            if (!material.type.Contains("Monster"))
            {
                Debug.LogWarning($"[XYZManager] {material.name} não é monstro!");
                return false;
            }

            if (material.level != firstLevel)
            {
                Debug.LogWarning($"[XYZManager] Níveis diferentes: {material.name} tem {material.level}, esperado {firstLevel}");
                return false;
            }
        }

        Debug.Log($"[XYZManager] ✅ Validação PASSOU: {selectedMaterials.Count} materiais Rank {xyzRank}, Nível {firstLevel}");
        return true;
    }

    /// <summary>
    /// Realiza a invocação XYZ
    /// </summary>
    public CardDisplay PerformXYZSummon(CardData xyzMonster, List<CardData> materials, bool isPlayer)
    {
        Debug.Log($"[XYZManager] 🔄 Invocando XYZ: {xyzMonster.name} com {materials.Count} materiais");

        // 1. Verificar se é válido
        if (!ValidateXYZ(xyzMonster, materials, isPlayer))
        {
            Debug.LogError("[XYZManager] Validação falhou!");
            return null;
        }

        // 2. Remover materiais do campo e enviar para GY
        // (Em YGO real, eles iriam pro "overlay", mas por simplicidade vamos para GY por enquanto)
        foreach (var material in materials)
        {
            gameManager.SendToGraveyard(material, isPlayer, CardLocation.Field, SendReason.Cost);
        }

        // 3. Invocar XYZ Monster
        CardDisplay xyzCard = gameManager.SpecialSummonFromData(xyzMonster, isPlayer, summonType: 0x49000000, faceUp: true, defense: false);

        // 4. Incrementar contador
        if (summonManager != null)
        {
            summonManager.xyzSummonCount++;
        }

        Debug.Log($"[XYZManager] ✅ XYZ Summon realizado! Total: {(summonManager != null ? summonManager.xyzSummonCount : 1)}");
        return xyzCard;
    }
}
