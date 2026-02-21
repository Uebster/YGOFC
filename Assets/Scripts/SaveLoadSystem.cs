using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [System.Serializable]
    public class GameSaveData
    {
        public string saveID;
        public string playerName;
        public string lastPlayedTime;
        public int campaignProgress;
        public List<string> trunkCards;
        public List<string> mainDeck;
        public List<string> sideDeck;
        public List<string> extraDeck;
    }

    public void SaveGame(string saveID)
    {
        if (GameManager.Instance == null || CampaignManager.Instance == null) return;

        GameSaveData data = new GameSaveData();
        data.saveID = saveID;
        data.playerName = GameManager.Instance.playerName;
        data.lastPlayedTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.campaignProgress = CampaignManager.Instance.maxUnlockedLevel;
        
        data.trunkCards = new List<string>(GameManager.Instance.playerTrunk);
        
        var main = GameManager.Instance.GetPlayerMainDeck();
        var side = GameManager.Instance.GetPlayerSideDeck();
        var extra = GameManager.Instance.GetPlayerExtraDeck();

        data.mainDeck = main.Select(c => c.id).ToList();
        data.sideDeck = side.Select(c => c.id).ToList();
        data.extraDeck = extra.Select(c => c.id).ToList();

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, saveID + ".save");
        File.WriteAllText(path, json);
        
        Debug.Log($"Jogo salvo em: {path}");
    }

    public void LoadGame(string saveID)
    {
        string path = Path.Combine(Application.persistentDataPath, saveID + ".save");
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        // Aplica aos Gerentes
        GameManager.Instance.playerName = data.playerName;
        GameManager.Instance.currentSaveID = data.saveID;
        GameManager.Instance.playerTrunk = new List<string>(data.trunkCards);
        
        // Reconstrói os Decks (IDs -> CardData)
        List<CardData> main = IDsToCards(data.mainDeck);
        List<CardData> side = IDsToCards(data.sideDeck);
        List<CardData> extra = IDsToCards(data.extraDeck);
        GameManager.Instance.SetPlayerDeck(main, side, extra);

        CampaignManager.Instance.maxUnlockedLevel = data.campaignProgress;
        CampaignManager.Instance.SaveProgress(); // Atualiza PlayerPrefs para sincronizar

        Debug.Log("Jogo carregado com sucesso!");
    }

    public List<GameSaveData> GetAllSaves()
    {
        List<GameSaveData> saves = new List<GameSaveData>();
        string[] files = Directory.GetFiles(Application.persistentDataPath, "*.save");
        
        foreach (string file in files)
        {
            try {
                string json = File.ReadAllText(file);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data != null) saves.Add(data);
            } catch { Debug.LogWarning($"Erro ao ler save: {file}"); }
        }
        // Ordena pelo mais recente
        return saves.OrderByDescending(s => s.lastPlayedTime).ToList();
    }

    public void DeleteSave(string saveID)
    {
        string path = Path.Combine(Application.persistentDataPath, saveID + ".save");
        if (File.Exists(path)) File.Delete(path);
    }

    private List<CardData> IDsToCards(List<string> ids)
    {
        List<CardData> cards = new List<CardData>();
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) return cards;

        foreach (string id in ids)
        {
            CardData c = GameManager.Instance.cardDatabase.GetCardById(id);
            if (c != null) cards.Add(c);
        }
        return cards;
    }
}