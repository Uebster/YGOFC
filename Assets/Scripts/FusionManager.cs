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

        // A descrição dos materiais de fusão é um problema complexo de parsing.
        // Por enquanto, vamos assumir um formato simples como: "Material 1" + "Material 2"
        // Exemplo: "Blue-Eyes White Dragon" + "Blue-Eyes White Dragon" + "Blue-Eyes White Dragon"
        
        string requiredMaterialsText = GetMaterialsFromDescription(fusionMonster.description);
        if (string.IsNullOrEmpty(requiredMaterialsText))
        {
            Debug.LogWarning($"Materiais de fusão para {fusionMonster.name} não estão definidos em um formato esperado.");
            return false; // Não é possível validar se não conhecemos os materiais.
        }

        List<string> requiredMaterialNames = requiredMaterialsText.Split('+').Select(s => s.Trim()).ToList();

        if (selectedMaterials.Count != requiredMaterialNames.Count)
        {
            Debug.Log("Número incorreto de materiais.");
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
        // Exemplo para King of the Swamp (ID 1019)
        if (material.id == "1019")
        {
            // King of the Swamp pode substituir qualquer 1 Monstro Material de Fusão.
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
