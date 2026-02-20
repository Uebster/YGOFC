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
        string key = "CampaignProgress";
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.currentSaveID))
        {
            key += "_" + GameManager.Instance.currentSaveID;
        }
        PlayerPrefs.SetInt(key, maxUnlockedLevel);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        string key = "CampaignProgress";
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.currentSaveID))
        {
            key += "_" + GameManager.Instance.currentSaveID;
        }
        maxUnlockedLevel = PlayerPrefs.GetInt(key, 1);
    }

    // Chama a atualização visual em todos os nós do mapa
    public void UpdateMapVisuals()
    {
        CampaignNode[] nodes = FindObjectsByType<CampaignNode>(FindObjectsSortMode.None);
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