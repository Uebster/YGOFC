using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance;

    // Dados em tempo de execução
    public LibrarySaveData libraryData = new LibrarySaveData();
    public TrophySaveData trophyData = new TrophySaveData();

    [System.Serializable]
    public class DuelistWinRecord
    {
        public string duelistID;
        public int wins;
    }

    [System.Serializable]
    public class LibrarySaveData
    {
        public List<string> usedCardIDs = new List<string>(); // Cartas que já perderam o status "New"
        public List<DuelistWinRecord> duelistRecords = new List<DuelistWinRecord>();
    }

    [System.Serializable]
    public class TrophySaveData
    {
        public List<int> unlockedTrophies = new List<int>();
        public PlayerStatistics stats = new PlayerStatistics();
    }

    [System.Serializable]
    public class PlayerStatistics
    {
        public long totalDamageDealt;
        public long totalDamageTaken;
        public long totalDirectDamage;
        public long totalEffectDamage;
        public long totalReflectDamage;
        
        public int arenaWins;
        public int arenaDuels;
        
        public int spellsActivated;
        public int trapsActivated;
        public int monsterEffectsActivated;
        
        public int fusionSummons;
        public int ritualSummons;
        public int tributeSummons;
        public int specialSummons;
        
        public int highestDamageDealt; // Dano máximo em um único ataque
    }

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
        public List<string> playerExtraDeckIDs; // For player's extra deck
        public LibrarySaveData libraryData;
        public TrophySaveData trophyData;
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
        var side = GameManager.Instance.GetPlayerSideDeck(); // This is GameManager's playerSideDeck
        var extra = GameManager.Instance.GetPlayerExtraDeck(); // This is GameManager's playerExtraDeck

        data.mainDeck = main.Select(c => c.id).ToList();
        data.sideDeck = side.Select(c => c.id).ToList();
        data.extraDeck = extra.Select(c => c.id).ToList();
        data.playerExtraDeckIDs = GameManager.Instance.GetPlayerExtraDeck().Select(c => c.id).ToList(); // Save player's extra deck
        data.libraryData = this.libraryData;
        data.trophyData = this.trophyData;

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
        List<CardData> playerExtra = IDsToCards(data.playerExtraDeckIDs); // Load player's extra deck

        GameManager.Instance.SetPlayerDeck(main, side, playerExtra); // Pass playerExtra

        CampaignManager.Instance.maxUnlockedLevel = data.campaignProgress;
        CampaignManager.Instance.SaveProgress(); // Atualiza PlayerPrefs para sincronizar

        // Carrega dados da biblioteca
        if (data.libraryData != null) this.libraryData = data.libraryData;
        if (data.trophyData != null) this.trophyData = data.trophyData;

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

    // --- MÉTODOS DA BIBLIOTECA ---

    public void RegisterDuelistWin(string duelistID)
    {
        var record = libraryData.duelistRecords.Find(r => r.duelistID == duelistID);
        if (record != null)
        {
            record.wins++;
        }
        else
        {
            libraryData.duelistRecords.Add(new DuelistWinRecord { duelistID = duelistID, wins = 1 });
        }
        // Salva automaticamente após registrar vitória para não perder progresso
        if (!string.IsNullOrEmpty(GameManager.Instance.currentSaveID))
            SaveGame(GameManager.Instance.currentSaveID);
    }

    public int GetDuelistWins(string duelistID)
    {
        var record = libraryData.duelistRecords.Find(r => r.duelistID == duelistID);
        return record != null ? record.wins : 0;
    }

    public void MarkCardAsUsed(string cardID)
    {
        if (!libraryData.usedCardIDs.Contains(cardID))
        {
            libraryData.usedCardIDs.Add(cardID);
        }
    }

    public bool IsCardNew(string cardID)
    {
        // É nova se o jogador tem no Trunk mas ainda não está na lista de usados
        return GameManager.Instance.playerTrunk.Contains(cardID) && !libraryData.usedCardIDs.Contains(cardID);
    }

    // --- MÉTODOS DE TROFÉUS ---

    public bool IsTrophyUnlocked(int trophyId)
    {
        return trophyData.unlockedTrophies.Contains(trophyId);
    }

    public void UnlockTrophy(int trophyId)
    {
        if (!IsTrophyUnlocked(trophyId))
        {
            trophyData.unlockedTrophies.Add(trophyId);
            // Salva imediatamente para garantir a conquista
            if (!string.IsNullOrEmpty(GameManager.Instance.currentSaveID))
                SaveGame(GameManager.Instance.currentSaveID);
            
            Debug.Log($"[SaveLoadSystem] Troféu Desbloqueado: {trophyId}");
            // Aqui você pode chamar um evento de UI para mostrar o popup do troféu
        }
    }

    public PlayerStatistics GetStats()
    {
        return trophyData.stats;
    }
}