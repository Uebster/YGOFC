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
            requiredMaterialNames = GetMaterialsFromDescription(fusionMonster.description);
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
        int substituteCount = 0;

        foreach (string requiredName in requiredMaterialNames)
        {
            if (selectedNames.Contains(requiredName))
            {
                selectedNames.Remove(requiredName);
            }
            else
            {
                // Só permite 1 substituto de fusão no máximo
                var substitute = selectedMaterials.FirstOrDefault(m => IsSubstitute(m) && selectedNames.Contains(m.name));
                if (substitute != null && substituteCount < 1)
                {
                    selectedNames.Remove(substitute.name);
                    substituteCount++;
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

    private bool IsSubstitute(CardData material)
    {
        if (CardEffectManager.Instance != null) {
            return CardEffectManager.Instance.IsFusionSubstitute(material.id) || CardEffectManager.Instance.IsFusionSubstitute(material.name);
        }
        string[] substitutes = { "1019", "0781", "2037", "1315", "0158", "1877", "1850", "1856" };
        return substitutes.Contains(material.id);
    }

    public void PerformFusionSummon(CardDisplay sourceCard, CardData fusionMonster, List<CardData> materials)
    {
        if (fusionMonster == null || materials == null || materials.Count == 0) return;

        Debug.Log($"[FusionManager] Realizando Invocação-Fusão de {fusionMonster.name}...");

        bool disableCost = GameManager.Instance.disableFusionCost;
        bool banishMaterials = sourceCard != null && (sourceCard.CurrentCardData.id == "0707" || sourceCard.CurrentCardData.id == "0536" || sourceCard.CurrentCardData.name == "Fusion Gate" || sourceCard.CurrentCardData.name == "Dragon's Mirror");

        if (sourceCard != null && !disableCost)
        {
            if (sourceCard.CurrentCardData.id != "0707" && sourceCard.CurrentCardData.name != "Fusion Gate") {
                GameManager.Instance.SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Effect);
                Destroy(sourceCard.gameObject);
            }
        }

        foreach (var mat in materials)
        {
            if (disableCost) break;

            var handObj = GameManager.Instance.playerHand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == mat);
            if (handObj != null)
            {
                if (banishMaterials) GameManager.Instance.RemoveFromPlay(mat, true);
                else GameManager.Instance.SendToGraveyard(mat, true, CardLocation.Hand, SendReason.Effect);
                GameManager.Instance.playerHand.Remove(handObj);
                Destroy(handObj);
            }
            else
            {
                var fieldObj = GameManager.Instance.FindCardOnField(mat.id, true);
                if (fieldObj != null)
                {
                    if (banishMaterials) GameManager.Instance.BanishCard(fieldObj);
                    else {
                        GameManager.Instance.SendToGraveyard(mat, true, CardLocation.Field, SendReason.Effect);
                        Destroy(fieldObj.gameObject);
                    }
                }
            }
        }

        if (GameManager.Instance.GetPlayerExtraDeck().Contains(fusionMonster))
            GameManager.Instance.GetPlayerExtraDeck().Remove(fusionMonster);

        CardDisplay summonedMonster = GameManager.Instance.SpecialSummonFromData(fusionMonster, true);
        if (summonedMonster != null) summonedMonster.fusionMaterialsUsed = new List<CardData>(materials);
        
        if (TrophyManager.Instance != null) TrophyManager.Instance.TrackStat("fusion_summon", 1);
    }

    // Este é um placeholder para um sistema de parsing mais robusto.
    private List<string> GetMaterialsFromDescription(string description)
    {
        List<string> materials = new List<string>();
        if (string.IsNullOrEmpty(description) || !description.Contains("+")) return materials;

        // Exemplo: "Dark Magician" + "Buster Blader"
        // Pega apenas a linha que contém os materiais para ignorar o resto do texto
        string[] lines = description.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        string matLine = lines.FirstOrDefault(l => l.Contains("+"));
        
        if (matLine != null)
        {
            // Remove qualquer texto que venha depois de um ponto final
            int periodIndex = matLine.IndexOf('.');
            if (periodIndex > 0) matLine = matLine.Substring(0, periodIndex);

            string[] parts = matLine.Split('+');
            foreach (string p in parts)
            {
                // Limpa as aspas e espaços em branco deixando apenas o nome puro
                string cleanName = p.Replace("\"", "").Replace("'", "").Trim();
                materials.Add(cleanName);
            }
        }
        return materials;
    }
}
