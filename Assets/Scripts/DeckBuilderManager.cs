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
    
    [Header("UI References - Pagination")]
    public Button btnPrevPage;
    public Button btnNextPage;
    public TextMeshProUGUI txtPageInfo;
    public int itemsPerPage = 50;
    private int currentPage = 1;
    private int totalPages = 1;
    
    [Header("Filter Buttons")]
    public Button btnSortABC;
    public Button btnSortAtk;
    public Button btnSortDef;
    public Button btnFilterNormal;
    public Button btnFilterEffect;
    public Button btnFilterSpell;
    public Button btnFilterTrap;
    public Button btnFilterFusion;
    public Button btnFilterRitual;

    [Header("Filter Visuals")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

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

    // Banlist (ID -> Limit)
    private Dictionary<string, int> banList = new Dictionary<string, int>()
    {
        // Forbidden (0)
        { "0289", 0 }, // Chaos Emperor Dragon - Envoy of the End
        { "1588", 0 }, // Sangan
        { "2097", 0 }, // Witch of the Black Forest
        { "2128", 0 }, // Yata-Garasu
        { "0414", 0 }, // Dark Hole
        { "0465", 0 }, // Delinquent Duo
        { "0791", 0 }, // Graceful Charity
        { "0872", 0 }, // Harpie's Feather Duster
        { "1268", 0 }, // Monster Reborn
        { "1480", 0 }, // Raigeki
        { "2020", 0 }, // United We Stand
        { "0932", 0 }, // Imperial Order
        { "1251", 0 }, // Mirror Force
        // Outras proibições clássicas mantidas para equilíbrio
        { "0639", 0 }, // Fiber Jar
        { "0363", 0 }, // Cyber Jar
        { "1134", 0 }, // Magical Scientist
        { "0370", 0 }, // Cyber-Stein
        { "1252", 0 }, // Mirror Wall
        { "0485", 0 }, // Destruction Ring
        { "0287", 0 }, // Change of Heart
        { "0321", 0 }, // Confiscation
        { "1863", 0 }, // The Forceful Sentry

        // Limited (1)
        { "0189", 1 }, // Black Luster Soldier - Envoy of the Beginning
        { "0188", 1 }, // Black Luster Soldier (Ritual)
        { "0293", 1 }, // Chaos Sorcerer
        { "0240", 1 }, // Breaker the Magical Warrior
        { "1587", 1 }, // Sanga of the Thunder (Mantido 1 se for boss piece, ou erro de ID do Sangan antigo)
        { "0975", 1 }, // Jinzo
        { "1973", 1 }, // Tribe-Infecting Virus
        { "1651", 1 }, // Sinister Serpent
        { "0616", 1 }, // Exiled Force
        { "0378", 1 }, // D.D. Assailant
        { "0388", 1 }, // D.D. Warrior Lady
        { "0058", 1 }, // Ancient Gear Beast
        { "0097", 1 }, // Armed Dragon LV5
        { "0098", 1 }, // Armed Dragon LV7
        { "0166", 1 }, // Behemoth the King of All Animals
        { "0288", 1 }, // Chaos Command Magician
        { "0944", 1 }, // Injection Fairy Lily
        { "1507", 1 }, // Reflect Bounder
        { "1989", 1 }, // Twin-Headed Behemoth
        { "2031", 1 }, // Vampire Lord
        { "1277", 1 }, // Morphing Jar
        { "1457", 1 }, // Primal Seed
        { "0422", 1 }, // Dark Magician of Chaos
        { "1513", 1 }, // Relinquished
        { "1790", 1 }, // Summoner Monk
        { "1517", 1 }, // Rescue Cat
        { "1447", 1 }, // Pot of Greed
        { "0881", 1 }, // Heavy Storm
        { "1683", 1 }, // Snatch Steal
        { "1453", 1 }, // Premature Burial
        { "0259", 1 }, // Call of the Haunted
        { "1533", 1 }, // Ring of Destruction
        { "1955", 1 }, // Torrential Tribute
        { "1120", 1 }, // Magic Cylinder
        { "0275", 1 }, // Ceasefire
        { "1499", 1 }, // Reckless Greed
        { "1811", 1 }, // Swords of Revealing Light
        { "0228", 1 }, // Book of Moon
        { "1318", 1 }, // Mystical Space Typhoon
        { "0757", 1 }, // Giant Trunade
        { "1563", 1 }, // Royal Decree
        { "1119", 1 }, // Mage Power
        { "1397", 1 }, // Painful Choice
        { "1462", 1 }, // Protector of the Sanctuary
        { "1138", 1 }, // Magician of Faith
        { "1170", 1 }, // Mask of Darkness
        { "1236", 1 }, // Mind Control
        { "0237", 1 }, // Brain Control
        { "1088", 1 }, // Limiter Removal
        { "1200", 1 }, // Megamorph
        { "1929", 1 }, // Time Seal
        { "2050", 1 }, // Wall of Revealing Light
        { "1610", 1 }, // Self-Destruct Button
        { "0264", 1 }, // Card Destruction
        { "0497", 1 }, // Dimension Fusion
        { "1523", 1 }, // Return from the Different Dimension
        
        // Exodia Pieces (Limited)
        { "0618", 1 }, // Exodia Head
        { "1061", 1 }, // Left Arm
        { "1062", 1 }, // Left Leg
        { "1530", 1 }, // Right Arm
        { "1531", 1 }, // Right Leg

        // Semi-Limited (2)
        { "0338", 2 }, // Creature Swap
        { "1055", 2 }, // Last Turn (User Request)
        { "1162", 2 }, // Manticore of Darkness
        { "1163", 2 }, // Marauding Captain
        { "1278", 2 }, // Morphing Jar #2
        { "1353", 2 }, // Nobleman of Crossout
        { "1509", 2 }, // Reinforcement of the Army
        { "2024", 2 }, // Upstart Goblin
        { "0359", 2 }, // Cyber Dragon
        { "0011", 2 }, // A Feint Plan
        { "0602", 2 }, // Enemy Controller
        { "1209", 2 }, // Messenger of Peace
        { "1077", 2 }, // Level Limit - Area B
        { "0817", 2 }, // Gravity Bind
        { "1245", 2 }, // Miracle Dig
        { "0786", 2 }, // Good Goblin Housekeeping
        { "1329", 2 }, // Needle Worm
        { "0077", 2 }, // Apprentice Magician
        { "0460", 2 }, // Deck Devastation Virus
        { "1604", 2 }, // Second Coin Toss
        { "1498", 2 }  // Reasoning
    };

    // Estado dos Filtros
    private bool showNormal = true;
    private bool showEffect = true;
    private bool showSpell = true;
    private bool showTrap = true;
    private bool showFusion = true;
    private bool showRitual = true;
    
    private enum SortType { ABC, Atk, Def }
    private SortType currentSort = SortType.ABC;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Configura os listeners dos botões
        if (btnSortABC) btnSortABC.onClick.AddListener(() => SetSort(SortType.ABC));
        if (btnSortAtk) btnSortAtk.onClick.AddListener(() => SetSort(SortType.Atk));
        if (btnSortDef) btnSortDef.onClick.AddListener(() => SetSort(SortType.Def));

        if (btnFilterNormal) btnFilterNormal.onClick.AddListener(() => ToggleFilter("Normal"));
        if (btnFilterEffect) btnFilterEffect.onClick.AddListener(() => ToggleFilter("Effect"));
        if (btnFilterSpell) btnFilterSpell.onClick.AddListener(() => ToggleFilter("Spell"));
        if (btnFilterTrap) btnFilterTrap.onClick.AddListener(() => ToggleFilter("Trap"));
        if (btnFilterFusion) btnFilterFusion.onClick.AddListener(() => ToggleFilter("Fusion"));
        if (btnFilterRitual) btnFilterRitual.onClick.AddListener(() => ToggleFilter("Ritual"));
        
        if (searchInput) searchInput.onValueChanged.AddListener(delegate { 
            currentPage = 1; // Reseta página ao buscar
            RefreshTrunkUI(); 
        });

        if (btnPrevPage) btnPrevPage.onClick.AddListener(PrevPage);
        if (btnNextPage) btnNextPage.onClick.AddListener(NextPage);

        UpdateFilterVisuals();
    }

    void OnEnable()
    {
        LoadCurrentDeckFromManager();
        LoadTrunk();
        currentPage = 1; // Reseta página ao abrir
        RefreshAllUI();
    }

    void LoadCurrentDeckFromManager()
    {
        if (GameManager.Instance != null)
        {
            currentMainDeck = new List<CardData>(GameManager.Instance.GetPlayerMainDeck());
            currentSideDeck = new List<CardData>(GameManager.Instance.GetPlayerSideDeck()); // This is GameManager's playerSideDeck
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
        
        // Filtra a lista
        var filteredList = currentTrunk.Where(card => 
        {
            // Filtro de Texto
            if (!string.IsNullOrEmpty(searchText) && !card.name.ToLower().Contains(searchText)) return false;

            // Filtros de Tipo
            bool isSpell = card.type.Contains("Spell");
            bool isTrap = card.type.Contains("Trap");
            bool isFusion = card.type.Contains("Fusion");
            bool isRitual = card.type.Contains("Ritual");
            bool isEffect = card.type.Contains("Effect") && !isFusion && !isRitual; // Efeito puro
            bool isNormal = card.type.Contains("Normal") && !isFusion && !isRitual; // Normal puro

            // Lógica "Power of Chaos": Se o botão está ativo, mostra o tipo.
            if (isSpell && !showSpell) return false;
            if (isTrap && !showTrap) return false;
            if (isFusion && !showFusion) return false;
            if (isRitual && !showRitual) return false;
            if (isEffect && !showEffect) return false;
            if (isNormal && !showNormal) return false;

            return true;
        }).ToList();

        // Ordena a lista
        switch (currentSort)
        {
            case SortType.ABC: filteredList = filteredList.OrderBy(c => c.name).ToList(); break;
            case SortType.Atk: filteredList = filteredList.OrderByDescending(c => c.atk).ToList(); break;
            case SortType.Def: filteredList = filteredList.OrderByDescending(c => c.def).ToList(); break;
        }

        // Lógica de Paginação
        totalPages = Mathf.CeilToInt((float)filteredList.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        UpdatePaginationUI();

        // Pega apenas os itens da página atual
        var paginatedList = filteredList.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ToList();

        // Cria os objetos
        foreach (CardData card in paginatedList)
        {
            GameObject go = Instantiate(cardItemPrefab, trunkContent);
            SetupCardItem(go, card, DeckZoneType.Trunk);
        }
    }

    void UpdatePaginationUI()
    {
        if (txtPageInfo) txtPageInfo.text = $"{currentPage}/{totalPages}";
        if (btnPrevPage) btnPrevPage.interactable = currentPage > 1;
        if (btnNextPage) btnNextPage.interactable = currentPage < totalPages;
    }

    public void NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            RefreshTrunkUI();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            RefreshTrunkUI();
        }
    }

    // --- Funções de Controle dos Botões ---

    void SetSort(SortType type)
    {
        currentSort = type;
        currentPage = 1; // Reseta página ao ordenar
        UpdateFilterVisuals();
        RefreshTrunkUI();
    }

    void ToggleFilter(string filterName)
    {
        switch (filterName)
        {
            case "Normal": showNormal = !showNormal; break;
            case "Effect": showEffect = !showEffect; break;
            case "Spell": showSpell = !showSpell; break;
            case "Trap": showTrap = !showTrap; break;
            case "Fusion": showFusion = !showFusion; break;
            case "Ritual": showRitual = !showRitual; break;
        }
        currentPage = 1; // Reseta página ao filtrar
        UpdateFilterVisuals();
        RefreshTrunkUI();
    }

    void UpdateFilterVisuals()
    {
        // Atualiza cores dos botões de Ordenação (Radio Button style)
        SetButtonColor(btnSortABC, currentSort == SortType.ABC);
        SetButtonColor(btnSortAtk, currentSort == SortType.Atk);
        SetButtonColor(btnSortDef, currentSort == SortType.Def);

        // Atualiza cores dos botões de Filtro (Toggle style)
        SetButtonColor(btnFilterNormal, showNormal);
        SetButtonColor(btnFilterEffect, showEffect);
        SetButtonColor(btnFilterSpell, showSpell);
        SetButtonColor(btnFilterTrap, showTrap);
        SetButtonColor(btnFilterFusion, showFusion);
        SetButtonColor(btnFilterRitual, showRitual);
    }

    void SetButtonColor(Button btn, bool isActive)
    {
        if (btn != null)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? activeColor : inactiveColor;
            }
        }
    }

    void SetupCardItem(GameObject go, CardData card, DeckZoneType zoneType)
    {
        // Configura imagem (assumindo que o prefab tem CardDisplay ou RawImage)
        CardDisplay display = go.GetComponent<CardDisplay>();
        if (display == null) display = go.AddComponent<CardDisplay>();
        
        // Carrega textura (usando o sistema existente)
        // Nota: Para otimização em listas grandes, idealmente usaríamos Object Pooling e carregamento assíncrono leve
        display.SetCard(card, GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null, true); 
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
        
        int limitCopies = MAX_COPIES;
        if (banList.ContainsKey(card.id))
        {
            limitCopies = banList[card.id];
            if (GameManager.Instance != null && GameManager.Instance.allowForbiddenCards && limitCopies == 0) limitCopies = 1;
        }
        
        if (totalCopies >= limitCopies) return false;

        // Regra de Tipo (Fusão vai para Extra)
        if (card.type.Contains("Fusion") && targetZone != DeckZoneType.Extra) return false;
        if (!card.type.Contains("Fusion") && targetZone == DeckZoneType.Extra) return false;

        // Marca como usada (remove tag "New")
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
        }

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
        {   // SetPlayerDeck now expects playerExtraDeck
            GameManager.Instance.SetPlayerDeck(currentMainDeck, currentSideDeck, currentExtraDeck); 
        }
        
        // Volta para o menu anterior
        if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu();
    }
}

public enum DeckZoneType { Trunk, Main, Side, Extra }
