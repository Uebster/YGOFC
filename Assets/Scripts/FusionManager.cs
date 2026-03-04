using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FusionManager : MonoBehaviour
{
    public static FusionManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Valida se os materiais selecionados podem invocar o monstro de fusão escolhido.
    public bool ValidateFusion(CardData fusionMonster, List<CardData> selectedMaterials)
    {
        if (fusionMonster == null || !fusionMonster.type.Contains("Fusion") || selectedMaterials == null || selectedMaterials.Count < 2)
        {
            return false;
        }

        List<string> requiredMaterialNames = new List<string>();

        // 1. Prioridade: Ler do campo de dados dedicado (fusion_materials)
        if (fusionMonster.fusion_materials != null && fusionMonster.fusion_materials.Count > 0)
        {
            requiredMaterialNames = new List<string>(fusionMonster.fusion_materials);
        }
        else
        {
            // 2. Fallback: Tentar ler da descrição (Formato "Mat1 + Mat2")
            string requiredMaterialsText = GetMaterialsFromDescription(fusionMonster.description);
            if (!string.IsNullOrEmpty(requiredMaterialsText))
            {
                requiredMaterialNames = requiredMaterialsText.Split('+').Select(s => s.Trim()).ToList();
            }
        }

        if (requiredMaterialNames.Count == 0)
        {
            Debug.LogWarning($"Materiais de fusão para {fusionMonster.name} não definidos (nem em dados, nem na descrição).");
            return false;
        }

        if (selectedMaterials.Count != requiredMaterialNames.Count)
        {
            Debug.Log($"Número incorreto de materiais. Necessário: {requiredMaterialNames.Count}, Selecionado: {selectedMaterials.Count}");
            return false;
        }

        // Verifica se os materiais selecionados correspondem aos nomes necessários.
        // Esta é uma verificação simples que não lida com substitutos ou materiais genéricos ("1 monstro Guerreiro").
        var selectedNames = selectedMaterials.Select(m => m.name).ToList();
        foreach (string requiredName in requiredMaterialNames)
        {
            if (selectedNames.Contains(requiredName))
            {
                selectedNames.Remove(requiredName);
            }
            else
            {
                // Lida com monstros substitutos como "King of the Swamp"
                var substitute = selectedMaterials.FirstOrDefault(m => IsSubstitute(m, requiredName) && selectedNames.Contains(m.name));
                if (substitute != null)
                {
                    selectedNames.Remove(substitute.name);
                }
                else
                {
                    Debug.Log($"Material faltando: {requiredName}");
                    return false; // Um material necessário está faltando.
                }
            }
        }

        return true; // Todos os materiais foram encontrados.
    }

    private bool IsSubstitute(CardData material, string requiredName)
    {
        // Lista de Substitutos de Fusão
        string[] substitutes = {
            "1019", // King of the Swamp
            "0781", // Goddess with the Third Eye
            "2037", // Versago the Destroyer
            "1315", // Mystical Sheep #1
            "0158", // Beastking of the Swamps
            "1877", // The Light - Hex-Sealed Fusion
            "1850", // The Dark - Hex-Sealed Fusion
            "1856"  // The Earth - Hex-Sealed Fusion
        };

        if (substitutes.Contains(material.id))
        {
            return true;
        }
        return false;
    }

    // Este é um placeholder para um sistema de parsing mais robusto.
    private string GetMaterialsFromDescription(string description)
    {
        // Exemplo: "Dark Magician" + "Buster Blader"
        // Isto é muito frágil. Uma maneira melhor seria ter um campo dedicado em CardData.
        if (description.Contains("+"))
        {
            return description;
        }
        return null;
    }
}
