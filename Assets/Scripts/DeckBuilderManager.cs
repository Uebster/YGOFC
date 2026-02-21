using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class DeckBuilderManager : MonoBehaviour
{
    public static DeckBuilderManager Instance;

    [Header("Visualizer")]
    public CardViewerUI cardViewer; // Arraste o CardViewerUI da cena aqui

    [Header("UI References - Zones")]
    public Transform mainDeckContent;
    public Transform sideDeckContent;
    public Transform extraDeckContent;
    public Transform trunkContent;

    [Header("UI References - Info")]
    public TextMeshProUGUI mainDeckCountText;
    public TextMeshProUGUI sideDeckCountText;
    public TextMeshProUGUI extraDeckCountText;
    public TMP_InputField deckNameInput;

    [Header("UI References - Filters")]
    public TMP_InputField searchInput;
    public Toggle filterMonster;
    public Toggle filterSpell;
    public Toggle filterTrap;
    // Adicione mais toggles conforme necessário (Fusion, Ritual, etc)

    [Header("Prefabs")]
    public GameObject cardItemPrefab; // Prefab da carta na lista (pequeno)

    // Listas de dados atuais
    private List<CardData> currentMainDeck = new List<CardData>();
    private List<CardData> currentSideDeck = new List<CardData>();
    private List<CardData> currentExtraDeck = new List<CardData>();
    private List<CardData> currentTrunk = new List<CardData>();

    // Limites
    private const int MIN_MAIN = 40;
    private const int MAX_MAIN = 60;
    private const int MAX_SIDE = 15;
    private const int MAX_EXTRA = 15;
    private const int MAX_COPIES = 3;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        LoadCurrentDeckFromManager();
        LoadTrunk();
        RefreshAllUI();
    }

    void LoadCurrentDeckFromManager()
    {
        if (GameManager.Instance != null)
        {
            currentMainDeck = new List<CardData>(GameManager.Instance.GetPlayerMainDeck());
            currentSideDeck = new List<CardData>(GameManager.Instance.GetPlayerSideDeck());
            currentExtraDeck = new List<CardData>(GameManager.Instance.GetPlayerExtraDeck());
        }
    }

    void LoadTrunk()
    {
        currentTrunk.Clear();
        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            // Carrega cartas baseadas no inventário do jogador
            foreach (string id in GameManager.Instance.playerTrunk)
            {
                CardData card = GameManager.Instance.cardDatabase.GetCardById(id);
                if (card != null) currentTrunk.Add(card);
            }
            // Ordena por nome
            currentTrunk = currentTrunk.OrderBy(c => c.name).ToList();
        }
    }

    public void RefreshAllUI()
    {
        RefreshZone(mainDeckContent, currentMainDeck, DeckZoneType.Main);
        RefreshZone(sideDeckContent, currentSideDeck, DeckZoneType.Side);
        RefreshZone(extraDeckContent, currentExtraDeck, DeckZoneType.Extra);
        RefreshTrunkUI();
        UpdateCounts();
    }

    void RefreshZone(Transform container, List<CardData> cards, DeckZoneType type)
    {
        foreach (Transform child in container) Destroy(child.gameObject);

        foreach (CardData card in cards)
        {
            GameObject go = Instantiate(cardItemPrefab, container);
            SetupCardItem(go, card, type);
        }
    }

    void RefreshTrunkUI()
    {
        foreach (Transform child in trunkContent) Destroy(child.gameObject);

        string searchText = searchInput != null ? searchInput.text.ToLower() : "";
        bool showMonster = filterMonster != null && filterMonster.isOn;
        bool showSpell = filterSpell != null && filterSpell.isOn;
        bool showTrap = filterTrap != null && filterTrap.isOn;
        // Se nenhum filtro selecionado, mostra tudo
        if (!showMonster && !showSpell && !showTrap) { showMonster = showSpell = showTrap = true; }

        foreach (CardData card in currentTrunk)
        {
            // Filtros
            if (!string.IsNullOrEmpty(searchText) && !card.name.ToLower().Contains(searchText)) continue;
            
            bool isMonster = card.type.Contains("Monster");
            bool isSpell = card.type.Contains("Spell");
            bool isTrap = card.type.Contains("Trap");

            if (isMonster && !showMonster) continue;
            if (isSpell && !showSpell) continue;
            if (isTrap && !showTrap) continue;

            GameObject go = Instantiate(cardItemPrefab, trunkContent);
            SetupCardItem(go, card, DeckZoneType.Trunk);
        }
    }

    void SetupCardItem(GameObject go, CardData card, DeckZoneType zoneType)
    {
        // Configura imagem (assumindo que o prefab tem CardDisplay ou RawImage)
        CardDisplay display = go.GetComponent<CardDisplay>();
        if (display == null) display = go.AddComponent<CardDisplay>();
        
        // Carrega textura (usando o sistema existente)
        // Nota: Para otimização em listas grandes, idealmente usaríamos Object Pooling e carregamento assíncrono leve
        display.SetCard(card, null, true); 
        display.isInteractable = false; // Não sobe no hover na lista

        // Adiciona Drag Handler
        DeckDragHandler drag = go.GetComponent<DeckDragHandler>();
        if (drag == null) drag = go.AddComponent<DeckDragHandler>();
        drag.cardData = card;
        drag.sourceZone = zoneType;
    }

    void UpdateCounts()
    {
        if (mainDeckCountText) mainDeckCountText.text = $"{currentMainDeck.Count}/{MAX_MAIN}";
        if (sideDeckCountText) sideDeckCountText.text = $"{currentSideDeck.Count}/{MAX_SIDE}";
        if (extraDeckCountText) extraDeckCountText.text = $"{currentExtraDeck.Count}/{MAX_EXTRA}";
    }

    // Chamado pelo CardDisplay quando o mouse passa por cima
    public void OnCardHover(CardData card)
    {
        if (cardViewer != null && card != null)
        {
            cardViewer.DisplayCardData(card);
        }
    }

    // --- LÓGICA DE MODIFICAÇÃO ---

    public bool AddCardToDeck(CardData card, DeckZoneType targetZone)
    {
        List<CardData> targetList = null;
        int limit = 0;

        // Determina a lista alvo e limites
        if (targetZone == DeckZoneType.Main) { targetList = currentMainDeck; limit = MAX_MAIN; }
        else if (targetZone == DeckZoneType.Side) { targetList = currentSideDeck; limit = MAX_SIDE; }
        else if (targetZone == DeckZoneType.Extra) { targetList = currentExtraDeck; limit = MAX_EXTRA; }
        else return false; // Não pode adicionar ao Trunk (ele é fixo)

        // Validações
        if (targetList.Count >= limit) return false;
        
        // Regra de 3 cópias (soma Main + Side + Extra)
        int totalCopies = currentMainDeck.Count(c => c.id == card.id) + 
                          currentSideDeck.Count(c => c.id == card.id) + 
                          currentExtraDeck.Count(c => c.id == card.id);
        
        if (totalCopies >= MAX_COPIES) return false;

        // Regra de Tipo (Fusão vai para Extra)
        if (card.type.Contains("Fusion") && targetZone != DeckZoneType.Extra) return false;
        if (!card.type.Contains("Fusion") && targetZone == DeckZoneType.Extra) return false;

        targetList.Add(card);
        RefreshAllUI();
        return true;
    }

    public void RemoveCard(CardData card, DeckZoneType sourceZone)
    {
        if (sourceZone == DeckZoneType.Main) currentMainDeck.Remove(card);
        else if (sourceZone == DeckZoneType.Side) currentSideDeck.Remove(card);
        else if (sourceZone == DeckZoneType.Extra) currentExtraDeck.Remove(card);
        
        RefreshAllUI();
    }

    // --- IMPORT / EXPORT ---

    [System.Serializable]
    public class DeckFile
    {
        public string deckName;
        public List<string> mainDeckIDs;
        public List<string> sideDeckIDs;
        public List<string> extraDeckIDs;
    }

    public void ExportDeck()
    {
        string name = string.IsNullOrEmpty(deckNameInput.text) ? "NewDeck" : deckNameInput.text;
        DeckFile deckFile = new DeckFile
        {
            deckName = name,
            mainDeckIDs = currentMainDeck.Select(c => c.id).ToList(),
            sideDeckIDs = currentSideDeck.Select(c => c.id).ToList(),
            extraDeckIDs = currentExtraDeck.Select(c => c.id).ToList()
        };

        string json = JsonUtility.ToJson(deckFile, true);
        string path = Path.Combine(Application.persistentDataPath, name + ".json");
        File.WriteAllText(path, json);
        Debug.Log($"Deck exportado para: {path}");
    }

    public void ImportDeck()
    {
        // Simples: Tenta carregar pelo nome no input
        string name = string.IsNullOrEmpty(deckNameInput.text) ? "NewDeck" : deckNameInput.text;
        string path = Path.Combine(Application.persistentDataPath, name + ".json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            DeckFile deckFile = JsonUtility.FromJson<DeckFile>(json);

            currentMainDeck.Clear();
            currentSideDeck.Clear();
            currentExtraDeck.Clear();

            // Valida se o jogador tem as cartas
            AddCardsIfOwned(deckFile.mainDeckIDs, currentMainDeck);
            AddCardsIfOwned(deckFile.sideDeckIDs, currentSideDeck);
            AddCardsIfOwned(deckFile.extraDeckIDs, currentExtraDeck);

            RefreshAllUI();
            Debug.Log("Deck importado com sucesso!");
        }
        else
        {
            Debug.LogError("Arquivo de deck não encontrado: " + path);
        }
    }

    void AddCardsIfOwned(List<string> ids, List<CardData> targetList)
    {
        foreach (string id in ids)
        {
            if (GameManager.Instance.PlayerHasCard(id))
            {
                CardData card = GameManager.Instance.cardDatabase.GetCardById(id);
                if (card != null) targetList.Add(card);
            }
        }
    }

    public void SaveAndExit()
    {
        if (currentMainDeck.Count < MIN_MAIN)
        {
            Debug.LogWarning($"Deck inválido! Mínimo de {MIN_MAIN} cartas.");
            // Opcional: Mostrar popup de erro
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerDeck(currentMainDeck, currentSideDeck, currentExtraDeck);
        }
        
        // Volta para o menu anterior
        if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu();
    }
}

public enum DeckZoneType { Trunk, Main, Side, Extra }