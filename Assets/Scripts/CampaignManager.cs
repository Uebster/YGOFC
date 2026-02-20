using UnityEngine;
using System.Collections.Generic;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance;

    [Header("Progresso")]
    public int maxUnlockedLevel = 1; // Começa com o nível 1 desbloqueado (0 é Home)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadProgress();
    }

    public void CompleteLevel(int levelIndex)
    {
        // Se completou o nível atual, desbloqueia o próximo
        if (levelIndex == maxUnlockedLevel)
        {
            maxUnlockedLevel++;
            SaveProgress();
            UpdateMapVisuals();
        }
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        // Nível 0 (Home) sempre desbloqueado
        if (levelIndex == 0) return true;
        return levelIndex <= maxUnlockedLevel;
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt("CampaignProgress", maxUnlockedLevel);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        maxUnlockedLevel = PlayerPrefs.GetInt("CampaignProgress", 1);
    }

    // Chama a atualização visual em todos os nós do mapa
    public void UpdateMapVisuals()
    {
        CampaignNode[] nodes = FindObjectsOfType<CampaignNode>();
        foreach (var node in nodes)
        {
            node.UpdateVisualState();
        }
    }
    
    // Atalho para resetar progresso (útil para testes)
    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        maxUnlockedLevel = 1;
        SaveProgress();
        UpdateMapVisuals();
    }
}