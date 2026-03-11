using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

[System.Serializable]
public class IconMapping
{
    public string name;
    public Sprite icon;
}

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
    
    [Header("UI References - Actions")]
    public Button btnSave;
    public Button btnExit;
    
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

    [Header("Feedback Visual")]
    public Color invalidMoveColor = Color.red;
    public float flashDuration = 0.3f;

    [Header("Prefabs")]
    public GameObject cardItemPrefab; // Prefab da carta na lista (pequeno)
    
    [Header("Customização Tag")]
    [Tooltip("Prefab opcional para a tag 'NEW'. Se vazio, será gerado via código.")]
    public GameObject newTagPrefab;
    [Tooltip("Cor da faixa da tag 'NEW' dinâmica.")]
    public Color newTagBannerColor = new Color(0, 0, 0, 0.7f);
    [Tooltip("Cor do texto da tag 'NEW' dinâmica.")]
    public Color newTagTextColor = Color.white;

    [Header("Icon Mapping")]
    [Tooltip("Mapeia o nome do atributo (ex: 'DARK') para seu sprite.")]
    public List<IconMapping> attributeIcons;
    [Tooltip("Mapeia o nome do tipo (ex: 'Spell') para seu sprite.")]
    public List<IconMapping> typeIcons;
    [Tooltip("Mapeia a raça do monstro (ex: 'Warrior') para seu sprite.")]
    public List<IconMapping> monsterRaceIcons;

    // Listas de dados atuais
    private List<CardData> mainDeck = new List<CardData>();
    private List<CardData> sideDeck = new List<CardData>();
    private List<CardData> extraDeck = new List<CardData>();
    private List<CardData> currentTrunk = new List<CardData>();
    private List<CardData> displayedTrunk = new List<CardData>();

    private bool hasUnsavedChanges = false;

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
    
    private enum SortType { ABC, Atk, Def }
    private SortType currentSort = SortType.ABC;

    private bool sortAscending = true;
    private Dictionary<string, bool> activeFilters = new Dictionary<string, bool>()
    {
        {"Normal", true}, {"Effect", true}, {"Spell", true},
        {"Trap", true}, {"Fusion", true}, {"Ritual", true}
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Configura os listeners dos botões
        if (btnSortABC) btnSortABC.onClick.AddListener(() => SetSort(SortType.ABC, btnSortABC));
        if (btnSortAtk) btnSortAtk.onClick.AddListener(() => SetSort(SortType.Atk, btnSortAtk));
        if (btnSortDef) btnSortDef.onClick.AddListener(() => SetSort(SortType.Def, btnSortDef));

        if (btnFilterNormal) btnFilterNormal.onClick.AddListener(() => ToggleFilter("Normal", btnFilterNormal));
        if (btnFilterEffect) btnFilterEffect.onClick.AddListener(() => ToggleFilter("Effect", btnFilterEffect));
        if (btnFilterSpell) btnFilterSpell.onClick.AddListener(() => ToggleFilter("Spell", btnFilterSpell));
        if (btnFilterTrap) btnFilterTrap.onClick.AddListener(() => ToggleFilter("Trap", btnFilterTrap));
        if (btnFilterFusion) btnFilterFusion.onClick.AddListener(() => ToggleFilter("Fusion", btnFilterFusion));
        if (btnFilterRitual) btnFilterRitual.onClick.AddListener(() => ToggleFilter("Ritual", btnFilterRitual));
        
        if (searchInput) searchInput.onValueChanged.AddListener(delegate { 
            currentPage = 1; // Reseta página ao buscar
            RefreshTrunkUI(); 
        });

        if (btnPrevPage) btnPrevPage.onClick.AddListener(() => ChangePage(-1));
        if (btnNextPage) btnNextPage.onClick.AddListener(() => ChangePage(1));
        
        if (btnSave) btnSave.onClick.AddListener(SaveDeck);
        if (btnExit) btnExit.onClick.AddListener(ExitDeckBuilder);

        UpdateFilterVisuals();
    }

    void OnEnable()
    {
        LoadCurrentDeckFromManager();
        LoadTrunk();
        currentPage = 1; // Reseta página ao abrir
        hasUnsavedChanges = false;
        RefreshAllUI();
    }

    void LoadCurrentDeckFromManager()
    {
        if (GameManager.Instance != null)
        {
            mainDeck = new List<CardData>(GameManager.Instance.GetPlayerMainDeck());
            sideDeck = new List<CardData>(GameManager.Instance.GetPlayerSideDeck());
            extraDeck = new List<CardData>(GameManager.Instance.GetPlayerExtraDeck());
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
        RefreshZone(mainDeckContent, mainDeck, DeckZoneType.Main);
        RefreshZone(sideDeckContent, sideDeck, DeckZoneType.Side);
        RefreshZone(extraDeckContent, extraDeck, DeckZoneType.Extra);
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
            if (card.type.Contains("Spell") && !activeFilters["Spell"]) return false;
            if (card.type.Contains("Trap") && !activeFilters["Trap"]) return false;
            if (card.type.Contains("Fusion") && !activeFilters["Fusion"]) return false;
            if (card.type.Contains("Ritual") && !activeFilters["Ritual"]) return false;
            if (card.type.Contains("Effect") && !card.type.Contains("Fusion") && !card.type.Contains("Ritual") && !activeFilters["Effect"]) return false;
            if (card.type.Contains("Normal") && !card.type.Contains("Fusion") && !card.type.Contains("Ritual") && !activeFilters["Normal"]) return false;

            return true;
        }).ToList();

        displayedTrunk = filteredList; // Salva a lista filtrada para paginação

        // Ordena a lista
        switch (currentSort)
        {
            case SortType.ABC:
                displayedTrunk = sortAscending ? displayedTrunk.OrderBy(c => c.name).ToList() : displayedTrunk.OrderByDescending(c => c.name).ToList();
                break;
            case SortType.Atk:
                displayedTrunk = sortAscending ? displayedTrunk.OrderByDescending(c => c.atk).ToList() : displayedTrunk.OrderBy(c => c.atk).ToList();
                break;
            case SortType.Def:
                displayedTrunk = sortAscending ? displayedTrunk.OrderByDescending(c => c.def).ToList() : displayedTrunk.OrderBy(c => c.def).ToList();
                break;
        }

        // Lógica de Paginação
        totalPages = Mathf.CeilToInt((float)filteredList.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        UpdatePaginationUI();

        // Pega apenas os itens da página atual
        var paginatedList = displayedTrunk.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ToList();

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

    void ChangePage(int direction)
    {
        int newPage = currentPage + direction;
        if (newPage >= 1 && newPage <= totalPages)
        {
            currentPage = newPage;
            RefreshTrunkUI();
        }
    }

    // --- Funções de Controle dos Botões ---

    void SetSort(SortType type, Button clickedButton)
    {
        if (currentSort == type)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortAscending = true;
        }
        currentSort = type;
        currentPage = 1; // Reseta página ao ordenar
        UpdateFilterVisuals();
        RefreshTrunkUI();
    }

    void ToggleFilter(string filterName, Button clickedButton)
    {
        activeFilters[filterName] = !activeFilters[filterName];
        currentPage = 1; // Reseta página ao filtrar
        UpdateFilterVisuals();
    }

    void UpdateFilterVisuals()
    {
        // Atualiza cores dos botões de Ordenação (Radio Button style)
        SetButtonColor(btnSortABC, currentSort == SortType.ABC);
        SetButtonColor(btnSortAtk, currentSort == SortType.Atk);
        SetButtonColor(btnSortDef, currentSort == SortType.Def);

        // Atualiza cores dos botões de Filtro (Toggle style)
        SetButtonColor(btnFilterNormal, activeFilters["Normal"]);
        SetButtonColor(btnFilterEffect, activeFilters["Effect"]);
        SetButtonColor(btnFilterSpell, activeFilters["Spell"]);
        SetButtonColor(btnFilterTrap, activeFilters["Trap"]);
        SetButtonColor(btnFilterFusion, activeFilters["Fusion"]);
        SetButtonColor(btnFilterRitual, activeFilters["Ritual"]);
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
        // --- Configuração do CardDisplay (Arte e Interação) ---
        CardDisplay display = go.GetComponent<CardDisplay>();
        if (display == null) display = go.AddComponent<CardDisplay>();
        display.SetCard(card, GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null, true);
        display.isInteractable = false;

        // --- Referências do Prefab ---
        var cardNameText = go.transform.Find("CardNameText")?.GetComponent<TextMeshProUGUI>();
        var cardStatsText = go.transform.Find("CardStatsText")?.GetComponent<TextMeshProUGUI>();
        var monsterLvlText = go.transform.Find("MonsterLvl")?.GetComponent<TextMeshProUGUI>();
        var attributeImage = go.transform.Find("AttributeIcon")?.GetComponent<Image>();
        var typeImage = go.transform.Find("TypeIcon")?.GetComponent<Image>();
        var subTypeImage = go.transform.Find("SubTypeIcon")?.GetComponent<Image>();

        // --- Lógica de Exibição ---
        if (card.type.Contains("Monster")) {
            // É um Monstro
            if (cardNameText) cardNameText.text = card.name;
            if (cardStatsText) cardStatsText.text = $"ATK/{card.atk}  DEF/{card.def}";
            if (monsterLvlText) {
                monsterLvlText.gameObject.SetActive(true);
                monsterLvlText.text = $"x{card.level}";
            }
            // Mostra o ícone de Atributo (DARK, LIGHT, etc.)
            if (attributeImage) {
                attributeImage.gameObject.SetActive(true);
                attributeImage.sprite = attributeIcons.Find(i => i.name.Equals(card.attribute, System.StringComparison.OrdinalIgnoreCase))?.icon;
            }
            // Mostra o ícone de Raça (Warrior, Fiend, etc.)
            if (typeImage) {
                typeImage.gameObject.SetActive(true);
                typeImage.sprite = monsterRaceIcons.Find(i => i.name.Equals(card.race, System.StringComparison.OrdinalIgnoreCase))?.icon;
            }
            // Monstros não usam o SubTypeIcon nesta visualização
            if (subTypeImage) subTypeImage.gameObject.SetActive(false);
        } else {
            // É Magia ou Armadilha
            if (cardNameText) cardNameText.text = card.name;
            if (cardStatsText) cardStatsText.text = "";
            if (monsterLvlText) monsterLvlText.gameObject.SetActive(false);
            // Esconde o ícone de Atributo, pois S/T não têm
            if (attributeImage) attributeImage.gameObject.SetActive(false);

            // Usa o TypeIcon para o símbolo principal (Magia ou Armadilha)
            if (typeImage) {
                typeImage.gameObject.SetActive(true);
                string mainType = card.type.Contains("Spell") ? "Spell" : "Trap";
                typeImage.sprite = typeIcons.Find(i => i.name.Equals(mainType, System.StringComparison.OrdinalIgnoreCase))?.icon;
            }
            
            // Usa o SubTypeIcon para a Propriedade (Continuous, Equip, etc.)
            if (subTypeImage) {
                Sprite subTypeSprite = typeIcons.Find(i => i.name.Equals(card.property, System.StringComparison.OrdinalIgnoreCase))?.icon;
                subTypeImage.sprite = subTypeSprite;
                subTypeImage.gameObject.SetActive(subTypeSprite != null);
            }
        }

        // --- Drag Handler ---
        DeckDragHandler drag = go.GetComponent<DeckDragHandler>();
        if (drag == null) drag = go.AddComponent<DeckDragHandler>();
        drag.cardData = card;
        drag.sourceZone = zoneType;

        // --- Indicador "NEW" ---
        if (SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id))
        {
            CreateNewBanner(go.transform);
        }
    }

    void CreateNewBanner(Transform parent)
    {
        if (newTagPrefab != null)
        {
            // Cria um container para o prefab
            GameObject bannerContainer = new GameObject("NewTagContainer", typeof(RectTransform));
            bannerContainer.transform.SetParent(parent, false);

            // Configura o RectTransform do container para ser uma faixa central
            RectTransform containerRect = bannerContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f);
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, 36);

            // Adiciona o AspectRatioFitter para manter a proporção
            AspectRatioFitter fitter = bannerContainer.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 120f / 36f; // Proporção do prefab (120x36)

            // Instancia o prefab do usuário DENTRO do container
            GameObject bannerInstance = Instantiate(newTagPrefab, bannerContainer.transform);
            bannerInstance.name = "NewTag_Custom_Instance";
            bannerInstance.SetActive(true);

            // Força o prefab a preencher o container com a proporção correta
            RectTransform bannerRect = bannerInstance.GetComponent<RectTransform>();
            if (bannerRect != null)
            {
                bannerRect.anchorMin = Vector2.zero;
                bannerRect.anchorMax = Vector2.one;
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.sizeDelta = Vector2.zero;
                bannerRect.anchoredPosition = Vector2.zero;
            }
            
            bannerContainer.transform.SetAsLastSibling();
        }
        else
        {
            // Fallback: Lógica para criar banner dinâmico se nenhum prefab for fornecido
            GameObject bannerObj = new GameObject("NewTag_Dynamic", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(parent, false);
            Image img = bannerObj.GetComponent<Image>();
            img.color = newTagBannerColor;
            // ... (código do banner dinâmico continua aqui) ...
        }
    }

    void UpdateCounts()
    {
        if (mainDeckCountText) mainDeckCountText.text = $"{mainDeck.Count}/{MAX_MAIN}";
        if (sideDeckCountText) sideDeckCountText.text = $"{sideDeck.Count}/{MAX_SIDE}";
        if (extraDeckCountText) extraDeckCountText.text = $"{extraDeck.Count}/{MAX_EXTRA}";
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
        if (targetZone == DeckZoneType.Main) { targetList = mainDeck; limit = MAX_MAIN; }
        else if (targetZone == DeckZoneType.Side) { targetList = sideDeck; limit = MAX_SIDE; }
        else if (targetZone == DeckZoneType.Extra) { targetList = extraDeck; limit = MAX_EXTRA; }
        else return false; // Não pode adicionar ao Trunk (ele é fixo)

        // Validações
        if (targetList.Count >= limit) return false;
        
        // Regra de 3 cópias (soma Main + Side + Extra)
        int totalCopies = mainDeck.Count(c => c.id == card.id) + 
                          sideDeck.Count(c => c.id == card.id) + 
                          extraDeck.Count(c => c.id == card.id);
        
        int limitCopies = MAX_COPIES;
        if (banList.ContainsKey(card.id))
        {
            // Se a banlist estiver desativada globalmente, ignora os limites específicos
            if (GameManager.Instance == null || !GameManager.Instance.disableBanlist)
            {
                limitCopies = banList[card.id];
                // Se permitir proibidas, trata Limit 0 como Limit 1
                if (GameManager.Instance != null && GameManager.Instance.allowForbiddenCards && limitCopies == 0) limitCopies = 1;
            }
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
        hasUnsavedChanges = true;
        RefreshAllUI();
        return true;
    }

    public void RemoveCard(CardData card, DeckZoneType sourceZone)
    {
        bool removed = false;
        if (sourceZone == DeckZoneType.Main) removed = mainDeck.Remove(card);
        else if (sourceZone == DeckZoneType.Side) removed = sideDeck.Remove(card);
        else if (sourceZone == DeckZoneType.Extra) removed = extraDeck.Remove(card);
        
        if(removed) hasUnsavedChanges = true;

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
            mainDeckIDs = mainDeck.Select(c => c.id).ToList(),
            sideDeckIDs = sideDeck.Select(c => c.id).ToList(),
            extraDeckIDs = extraDeck.Select(c => c.id).ToList()
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

            mainDeck.Clear();
            sideDeck.Clear();
            extraDeck.Clear();

            // Valida se o jogador tem as cartas
            AddCardsIfOwned(deckFile.mainDeckIDs, mainDeck);
            AddCardsIfOwned(deckFile.sideDeckIDs, sideDeck);
            AddCardsIfOwned(deckFile.extraDeckIDs, extraDeck);
            
            hasUnsavedChanges = true;
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

    public void SaveDeck()
    {
        if (mainDeck.Count < MIN_MAIN)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Deck Inválido! O deck principal precisa de no mínimo {MIN_MAIN} cartas.");
            Debug.LogWarning($"Deck inválido! Mínimo de {MIN_MAIN} cartas.");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerDeck(mainDeck, sideDeck, extraDeck);
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Deck Salvo!");
            Debug.Log("Deck salvo com sucesso no perfil atual!");
        }

        hasUnsavedChanges = false;
    }

    public void ExitDeckBuilder()
    {
        // Primeiro, verifica se o deck atual (não salvo) é inválido
        if (mainDeck.Count < MIN_MAIN)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowConfirmation(
                    $"Seu deck está inválido (menos de {MIN_MAIN} cartas) e não pode ser salvo. Deseja sair mesmo assim?",
                    () => { if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu(); }
                );
            }
        }
        else if (hasUnsavedChanges)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowConfirmation(
                    "Você possui alterações não salvas. Deseja sair mesmo assim?",
                    () => { if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu(); }
                );
            }
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.Btn_BackToNewGameMenu();
        }
    }

    public void TriggerInvalidMoveFeedback(DeckZoneType zoneType)
    {
        Transform targetZone = null;
        switch (zoneType)
        {
            case DeckZoneType.Main:
                targetZone = mainDeckContent.parent.parent; // Pega a imagem de fundo do ScrollView
                break;
            case DeckZoneType.Side:
                targetZone = sideDeckContent.parent.parent;
                break;
            case DeckZoneType.Extra:
                targetZone = extraDeckContent.parent.parent;
                break;
        }

        if (targetZone != null)
        {
            StartCoroutine(FlashZoneCoroutine(targetZone));
        }
    }

    private IEnumerator FlashZoneCoroutine(Transform zoneTransform)
    {
        Image image = zoneTransform.GetComponent<Image>(); // A imagem de fundo
        if (image != null)
        {
            Color originalColor = image.color;
            image.color = invalidMoveColor;
            yield return new WaitForSeconds(flashDuration);
            image.color = originalColor;
        }
    }
}
public enum DeckZoneType { Trunk, Main, Side, Extra }
