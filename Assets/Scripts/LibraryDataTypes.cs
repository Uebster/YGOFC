using System.Collections.Generic;

[System.Serializable]
public class DuelistWinRecord
{
    public string duelistID;
    public int wins;
}

[System.Serializable]
public class LibrarySaveData
{
    // Lista de IDs de cartas que o jogador já viu na biblioteca (para remover o "New")
    public List<string> seenCardIDs = new List<string>();
    
    // Registro de vitórias contra cada duelista (para desbloquear perfis e decks)
    public List<DuelistWinRecord> duelistRecords = new List<DuelistWinRecord>();
}
