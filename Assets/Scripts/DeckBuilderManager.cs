using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.EventSystems;

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
    public CardViewerUI cardViewer;

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
    [Tooltip("Quantas cartas únicas são mostradas por página no baú.")]
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

    [Header("Hover Customization")]
    [Tooltip("Usa cor de hover do player.")]
    public bool usePlayerHover = true;
    [Tooltip("Usa cor de hover do opponent.")]
    public bool useOpponentHover = false;
    [Tooltip("Cor customizada de hover (usada se nenhuma das acima estiver marcada).")]
    public Color customHoverColor = Color.yellow;

    [Header("Prefabs")]
    [Tooltip("Prefab para a lista de cartas no baú (formato de lista detalhada).")]
    public GameObject cardChestItemPrefab;
    [Tooltip("Prefab para as cartas nos decks (formato de ícone pequeno).")]
    public GameObject cardDeckItemPrefab;
    
    [Header("Customização Tag")]
    /// <summary>
    /// Prefab customizado para a tag 'NEW'. Se este campo for deixado vazio (None), uma tag dinâmica será criada.
    /// O sistema aplicará um AspectRatioFitter para manter a proporção da tag.
    /// </summary>
    [Tooltip("Prefab customizado para a tag 'NEW'. Se deixado vazio, uma tag dinâmica será criada.")]
    public GameObject newTagPrefab;
    /// <summary>
    /// Cor da faixa de fundo da tag 'NEW' quando nenhum prefab é fornecido.
    /// </summary>
    [Tooltip("Cor da faixa de fundo da tag 'NEW' quando nenhum prefab é fornecido.")]
    public Color newTagBannerColor = new Color(0, 0, 0, 0.7f);
    /// <summary>
    /// Cor do texto da tag 'NEW' quando nenhum prefab é fornecido.
    /// </summary>
    [Tooltip("Cor do texto da tag 'NEW' quando nenhum prefab é fornecido.")]
    public Color newTagTextColor = Color.white;

    [Header("Icon Mapping")]
    public List<IconMapping> attributeIcons;
    [Tooltip("Ícones para a Raça do monstro (Warrior, Fiend, etc.).")]
    public List<IconMapping> raceIcons;
    [Tooltip("Ícones para o Tipo principal da carta (Spell, Trap).")]
    public List<IconMapping> typeIcons;
    [Tooltip("Ícones para o SubTipo da carta (Equip, Continuous, Counter, etc.).")]
    public List<IconMapping> subTypeIcons;

    private List<CardData> mainDeck = new List<CardData>();
    private List<CardData> sideDeck = new List<CardData>();
    private List<CardData> extraDeck = new List<CardData>();
    private List<CardData> currentTrunk = new List<CardData>();

    // Performance cache
    private Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();

    // Conjunto de IDs de cartas nos decks para verificação rápida da tag "NEW".
    private HashSet<string> cardIDsInDecks = new HashSet<string>();

    private bool hasUnsavedChanges = false;

    [Header("Pooling")]
    [Tooltip("Tamanho inicial do pool de itens do chest para performance.")]
    public int initialPoolSize = 50;
    private List<GameObject> chestItemPool = new List<GameObject>();

    private const int MIN_MAIN = 40;
    private const int MAX_MAIN = 60;
    private const int MAX_SIDE = 15;
    private const int MAX_EXTRA = 15;
    private const int MAX_COPIES = 3;

    private Dictionary<string, int> banList = new Dictionary<string, int>()
    {
        // --- PROIBIDAS (0 CÓPIAS) ---
        {"Chaos Emperor Dragon - Envoy of the End", 0},
        {"Sangan", 0},
        {"Witch of the Black Forest", 0},
        {"Yata-Garasu", 0},
        {"Dark Hole", 0},
        {"Delinquent Duo", 0},
        {"Graceful Charity", 0},
        {"Harpie's Feather Duster", 0},
        {"Monster Reborn", 0},
        {"Raigeki", 0},
        {"United We Stand", 0},
        {"Imperial Order", 0},
        {"Mirror Force", 0},
        {"Change of Heart", 0},
        {"Confiscation", 0},
        {"The Forceful Sentry", 0},
        {"Fiber Jar", 0},
        {"Cyber Jar", 0},
        {"Magical Scientist", 0},
        {"Cyber-Stein", 0},
        {"Mirror Wall", 0},
        {"Destruction Ring", 0},

        // --- LIMITADAS (1 CÓPIA) ---
        {"Black Luster Soldier - Envoy of the Beginning", 1},
        {"Black Luster Soldier", 1}, // Ritual
        {"Chaos Sorcerer", 1},
        {"Breaker the Magical Warrior", 1},
        {"Jinzo", 1},
        {"Tribe-Infecting Virus", 1},
        {"Sinister Serpent", 1},
        {"Exiled Force", 1},
        {"D.D. Assailant", 1},
        {"D.D. Warrior Lady", 1},
        {"Ancient Gear Beast", 1},
        {"Armed Dragon LV5", 1},
        {"Armed Dragon LV7", 1},
        {"Behemoth the King of All Animals", 1},
        {"Chaos Command Magician", 1},
        {"Injection Fairy Lily", 1},
        {"Reflect Bounder", 1},
        {"Twin-Headed Behemoth", 1},
        {"Vampire Lord", 1},
        {"Morphing Jar", 1},
        {"Dark Magician of Chaos", 1},
        {"Relinquished", 1},
        {"Summoner Monk", 1},
        {"Rescue Cat", 1},
        {"Exodia the Forbidden One", 1},
        {"Right Arm of the Forbidden One", 1},
        {"Left Arm of the Forbidden One", 1},
        {"Right Leg of the Forbidden One", 1},
        {"Left Leg of the Forbidden One", 1},
        {"Pot of Greed", 1},
        {"Heavy Storm", 1},
        {"Snatch Steal", 1},
        {"Premature Burial", 1},
        {"Swords of Revealing Light", 1},
        {"Book of Moon", 1},
        {"Mystical Space Typhoon", 1},
        {"Giant Trunade", 1},
        {"Mage Power", 1},
        {"Painful Choice", 1},
        {"Mind Control", 1},
        {"Brain Control", 1},
        {"Limiter Removal", 1},
        {"Megamorph", 1},
        {"Card Destruction", 1},
        {"Dimension Fusion", 1},
        {"Primal Seed", 1},
        {"Call of the Haunted", 1},
        {"Ring of Destruction", 1},
        {"Torrential Tribute", 1},
        {"Magic Cylinder", 1},
        {"Ceasefire", 1},
        {"Reckless Greed", 1},
        {"Royal Decree", 1},
        {"Mask of Darkness", 1},
        {"Time Seal", 1},
        {"Wall of Revealing Light", 1},
        {"Self-Destruct Button", 1},
        {"Return from the Different Dimension", 1},
        {"Protector of the Sanctuary", 1},

        // --- SEMI-LIMITADAS (2 CÓPIAS) ---
        {"Creature Swap", 2},
        {"Manticore of Darkness", 2},
        {"Marauding Captain", 2},
        {"Nobleman of Crossout", 2},
        {"Reinforcement of the Army", 2},
        {"Upstart Goblin", 2},
        {"Cyber Dragon", 2},
        {"Enemy Controller", 2},
        {"Magician of Faith", 2}
    };
    
    public enum SortType { ABC, Atk, Def }
    private SortType currentSort = SortType.ABC;

    private bool sortAscending = true;
    private Dictionary<string, bool> activeFilters = new Dictionary<string, bool>()
    {
        {"Normal", false}, {"Effect", false}, {"Spell", false},
        {"Trap", false}, {"Fusion", false}, {"Ritual", false}
    };

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Adiciona listeners para os botões de ação principais
        btnSave?.onClick.AddListener(SaveDeck);
        btnExit?.onClick.AddListener(Exit);

        // Adiciona listeners para os botões de filtro e ordenação
        // Ordenação
        btnSortABC?.onClick.AddListener(() => SetSortType(SortType.ABC));
        btnSortAtk?.onClick.AddListener(() => SetSortType(SortType.Atk));
        btnSortDef?.onClick.AddListener(() => SetSortType(SortType.Def));

        // Filtros de Tipo
        btnFilterNormal?.onClick.AddListener(() => ToggleFilter("Normal"));
        btnFilterEffect?.onClick.AddListener(() => ToggleFilter("Effect"));
        btnFilterSpell?.onClick.AddListener(() => ToggleFilter("Spell"));
        btnFilterTrap?.onClick.AddListener(() => ToggleFilter("Trap"));
        btnFilterFusion?.onClick.AddListener(() => ToggleFilter("Fusion"));
        btnFilterRitual?.onClick.AddListener(() => ToggleFilter("Ritual"));

        // Paginação
        btnPrevPage?.onClick.AddListener(PrevPage);
        btnNextPage?.onClick.AddListener(NextPage);

        // Pesquisa
        searchInput?.onValueChanged.AddListener((text) => RefreshTrunkUI());

        // Configura as zonas de drop
        SetupDropZones();

        // Inicializa o pool de itens do chest
        InitializeChestItemPool();
    }

    void InitializeChestItemPool()
    {
        if (cardChestItemPrefab == null) return;

        // Clear existing pool
        foreach (var item in chestItemPool)
        {
            if (item != null) Destroy(item);
        }
        chestItemPool.Clear();

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject go = Instantiate(cardChestItemPrefab, trunkContent);
            go.SetActive(false);
            chestItemPool.Add(go);
        }
    }

    void SetupDropZones()
    {
        // Adiciona DeckDropZone aos containers se não tiverem
        AddDropZoneIfNeeded(mainDeckContent, DeckZoneType.Main);
        AddDropZoneIfNeeded(sideDeckContent, DeckZoneType.Side);
        AddDropZoneIfNeeded(extraDeckContent, DeckZoneType.Extra);
        AddDropZoneIfNeeded(trunkContent, DeckZoneType.Trunk);
    }

    void AddDropZoneIfNeeded(Transform container, DeckZoneType zoneType)
    {
        if (container == null) return;
        DeckDropZone dropZone = container.GetComponent<DeckDropZone>();
        if (dropZone == null)
        {
            dropZone = container.gameObject.AddComponent<DeckDropZone>();
        }
        dropZone.zoneType = zoneType;
    }

void OnEnable()
{
    // Se GameManager ainda não existe, aguarda um frame
    if (GameManager.Instance == null)
    {
        Debug.LogWarning("DeckBuilderManager: GameManager não encontrado, tentando novamente em 0.1s...");
        Invoke(nameof(LoadData), 0.1f);
        return;
    }

    LoadData();
}

void OnDisable()
{
    ClearArtCache();
}

private void ClearArtCache()
{
    foreach (var tex in artCache.Values)
        if (tex != null) Destroy(tex);
    artCache.Clear();
    Debug.Log("[DeckBuilderManager] Cache de arte limpo.");
}

private void LoadData()
{
    if (GameManager.Instance == null)
    {
        Debug.LogError("DeckBuilderManager: GameManager ainda é nulo. Não foi possível carregar.");
        return;
    }

    LoadCurrentDeckFromManager();
    LoadTrunk();

    RefreshAllUI();
    UpdateFilterButtonsVisuals();
    hasUnsavedChanges = false;
}

    void LoadCurrentDeckFromManager()
    {
        if (GameManager.Instance == null) return;
        mainDeck = new List<CardData>(GameManager.Instance.GetPlayerMainDeck());
        sideDeck = new List<CardData>(GameManager.Instance.GetPlayerSideDeck());
        extraDeck = new List<CardData>(GameManager.Instance.GetPlayerExtraDeck());
    }

    void LoadTrunk()
    {
        currentTrunk.Clear();
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null)
        {
            Debug.LogError("[DeckBuilder] LoadTrunk FAILED: GameManager ou CardDatabase é nulo.");
            return;
        }

        foreach (string id in GameManager.Instance.playerTrunk)
        {
            CardData card = GameManager.Instance.cardDatabase.GetCardById(id);
            if (card != null) currentTrunk.Add(card);
        }
    }

    public void RefreshAllUI()
    {
        // Atualiza o conjunto de IDs de cartas que já estão nos decks.
        cardIDsInDecks.Clear();
        foreach (var card in mainDeck) cardIDsInDecks.Add(card.id);
        foreach (var card in sideDeck) cardIDsInDecks.Add(card.id);
        foreach (var card in extraDeck) cardIDsInDecks.Add(card.id);

        // Ordena os decks antes de exibir para consistência
        mainDeck.Sort((a, b) => a.name.CompareTo(b.name));
        sideDeck.Sort((a, b) => a.name.CompareTo(b.name));
        extraDeck.Sort((a, b) => a.name.CompareTo(b.name));

        RefreshDeckZone(mainDeckContent, mainDeck, DeckZoneType.Main);
        RefreshDeckZone(sideDeckContent, sideDeck, DeckZoneType.Side);
        RefreshDeckZone(extraDeckContent, extraDeck, DeckZoneType.Extra);

        RefreshTrunkUI();
        UpdateCounts();
    }
    
    private bool IsDeckValid()
    {
        if (mainDeck.Count < MIN_MAIN || mainDeck.Count > MAX_MAIN) return false;
        return true;
    }

    // O tipo de deck é desnecessário pois o prefab é sempre o mesmo e a tag NEW nunca é mostrada aqui.
    void RefreshDeckZone(Transform container, List<CardData> cards, DeckZoneType zoneType)
    {
        if (cardDeckItemPrefab == null)
        {
            Debug.LogError("DeckBuilderManager: A variável 'cardDeckItemPrefab' não foi atribuída no Inspector. Não é possível popular a UI do deck.");
            return;
        }

        foreach (Transform child in container) Destroy(child.gameObject);
        foreach (CardData card in cards)
        {
            GameObject go = Instantiate(cardDeckItemPrefab, container);
            
            CardDisplay display = go.GetComponentInChildren<CardDisplay>();
            if (display == null) continue; // Already logged in SetupChestItem if missing
            // Configura hover baseado nas opções
            if (usePlayerHover) display.isPlayerCard = true;
            else if (useOpponentHover) display.isPlayerCard = false;
            else { display.useSimpleHover = true; display.hoverColor = customHoverColor; }
            display.SetCard(card, GameManager.Instance?.GetCardBackTexture(), true);
            display.isInteractable = true; // Permitir hover nas cartas do deck
            display.enableHoverLift = false; // Desabilita o efeito de levantar para deck

            DeckDragHandler drag = display.gameObject.GetComponent<DeckDragHandler>();
            if (drag == null) drag = display.gameObject.AddComponent<DeckDragHandler>();
            drag.cardData = card;
            drag.sourceZone = zoneType;
        }

        // Chama o script de layout customizado se ele existir no container
        CustomDeckLayout customLayout = container.GetComponent<CustomDeckLayout>();
        if (customLayout != null)
        {
            customLayout.UpdateLayout();
        }
    }

    void RefreshTrunkUI()
    {
        Debug.Log($"[DeckBuilderManager] RefreshTrunkUI chamado");
        if (trunkContent == null)
        {
            Debug.LogError("[DeckBuilder] ERRO CRÍTICO: 'Trunk Content' não está atribuído no Inspector! As cartas não aparecerão.");
            return;
        }

        Debug.Log($"[DeckBuilder] Limpando {trunkContent.childCount} filhos do trunkContent");
        // Desativa todos os itens do pool em vez de destruí-los
        foreach (GameObject item in chestItemPool)
        {
            if (item != null)
                item.SetActive(false);
        }

        // --- LÓGICA DE FILTRO E ORDENAÇÃO ---
        string searchText = searchInput != null ? searchInput.text.ToLowerInvariant() : "";
        bool anyFilterActive = activeFilters.Any(kvp => kvp.Value);

        var filteredGroups = currentTrunk
            .GroupBy(c => c.id)
            .Where(g =>
            {
                CardData card = g.First();
                // Primeiro, o filtro de busca de texto é sempre aplicado
                if (!string.IsNullOrEmpty(searchText) && !card.name.ToLowerInvariant().Contains(searchText))
                {
                    return false;
                }

                // Se nenhum filtro de botão estiver ativo, a carta passa (pois já passou pelo filtro de busca)
                if (!anyFilterActive)
                {
                    return true;
                }

                // Se há filtros ativos, a carta precisa corresponder a pelo menos um deles
                bool isMonster = card.type.Contains("Monster") && !card.type.Contains("Spell") && !card.type.Contains("Trap");
                bool isSpell = card.type.Contains("Spell");
                bool isTrap = card.type.Contains("Trap");

                if (isSpell && activeFilters["Spell"]) return true;
                if (isTrap && activeFilters["Trap"]) return true;

                if (isMonster)
                {
                    bool isFusion = card.type.Contains("Fusion");
                    bool isRitual = card.type.Contains("Ritual");
                    bool isNormal = card.type.Contains("Normal") && !isFusion && !isRitual;
                    bool isEffect = !isNormal && !isFusion && !isRitual; // Assume que, se não for Normal/Fusão/Ritual, é de Efeito

                    if (isFusion && activeFilters["Fusion"]) return true;
                    if (isRitual && activeFilters["Ritual"]) return true;
                    if (isNormal && activeFilters["Normal"]) return true;
                    if (isEffect && activeFilters["Effect"]) return true;
                }

                // Se nenhum dos 'return true' foi atingido, a carta não corresponde a nenhum filtro ativo
                return false;
            }).ToList();


        // Aplica a ordenação
        switch (currentSort)
        {
            case SortType.ABC:
                filteredGroups = sortAscending ? filteredGroups.OrderBy(g => g.First().name).ToList() : filteredGroups.OrderByDescending(g => g.First().name).ToList();
                break;
            case SortType.Atk:
                filteredGroups = sortAscending ? filteredGroups.OrderByDescending(g => g.First().atk).ToList() : filteredGroups.OrderBy(g => g.First().atk).ToList();
                break;
            case SortType.Def:
                filteredGroups = sortAscending ? filteredGroups.OrderByDescending(g => g.First().def).ToList() : filteredGroups.OrderBy(g => g.First().def).ToList();
                break;
        }

        // --- LÓGICA DE PAGINAÇÃO ---
        totalPages = Mathf.CeilToInt((float)filteredGroups.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        // Aplica a paginação
        UpdatePaginationUI();
        var paginatedGroups = filteredGroups.Skip((currentPage - 1) * itemsPerPage).Take(itemsPerPage).ToList();

        Dictionary<string, int> deckCardCounts = mainDeck.Concat(sideDeck).Concat(extraDeck)
            .GroupBy(c => c.id)
            .ToDictionary(g => g.Key, g => g.Count());

        if (cardChestItemPrefab == null)
        {
            Debug.LogError("DeckBuilderManager: A variável 'cardChestItemPrefab' não foi atribuída no Inspector. Não é possível popular a UI do baú.");
            return;
        }

                  // Popula a UI com as cartas da página atual (assíncrono)
          Debug.Log($"[DeckBuilder] Populando UI com {paginatedGroups.Count} grupos de cartas (assíncrono)");
          StartCoroutine(PopulateChestAsync(paginatedGroups, deckCardCounts));
        ContentSizeFitter fitter = trunkContent.GetComponent<ContentSizeFitter>();
        if (fitter == null) 
        {
            Debug.Log($"[DeckBuilder] Adicionando ContentSizeFitter ao trunkContent");
            fitter = trunkContent.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Adiciona VerticalLayoutGroup para posicionar os itens verticalmente
        VerticalLayoutGroup vlg = trunkContent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            Debug.Log($"[DeckBuilder] Adicionando VerticalLayoutGroup ao trunkContent");
            vlg = trunkContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        vlg.spacing = 5f; // Espaçamento entre itens
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false; // Altura fixa dos itens
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Garante que o ScrollRect esteja configurado para rolar o conteúdo
        ScrollRect sr = trunkContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            sr.content = trunkContent.GetComponent<RectTransform>();
            sr.vertical = true;
            sr.horizontal = false;
            sr.scrollSensitivity = 30f; // Adjust sensitivity as needed for mouse wheel

            // Garante que a barra de rolagem esteja vinculada
            if (sr.verticalScrollbar == null)
            {
                // Tenta encontrar a scrollbar como filha do objeto ScrollView
                Scrollbar scrollbar = sr.transform.GetComponentInChildren<Scrollbar>();
                if (scrollbar != null) sr.verticalScrollbar = scrollbar;
            }
        }

        Debug.Log($"[DeckBuilder] ContentSizeFitter, VerticalLayoutGroup e ScrollRect configurados");
    }

  // Método auxiliar para debug da hierarquia
  private void LogHierarchy(Transform transform, int depth)
  {
    string indent = new string(' ', depth * 2);
    Debug.Log($"{indent}- {transform.name} ({transform.GetType().Name})");
      
    for (int i = 0; i < transform.childCount; i++)
    {
        LogHierarchy(transform.GetChild(i), depth + 1);
    }
}

    // --- Métodos de Cache de Arte ---
    public bool TryGetArtFromCache(string cardId, out Texture2D texture)
    {
        return artCache.TryGetValue(cardId, out texture);
    }

    public void AddArtToCache(string cardId, Texture2D texture)
    {
        if (!artCache.ContainsKey(cardId))
        {
            artCache.Add(cardId, texture);
        }
    }

void SetupChestItem(GameObject go, CardData card, int totalOwned, int copiesInDecks)
{
    // DEBUG: Iniciando SetupChestItem
    Debug.Log($"[DEBUG] SetupChestItem: Configurando carta '{card?.name}' no objeto '{go.name}'");
    
    // DEBUG: Verificar hierarquia do objeto
    Debug.Log($"[DEBUG] SetupChestItem: Hierarquia de '{go.name}':");
    LogHierarchy(go.transform, 0);
    
    // Pega ou adiciona o componente ChestCardItem
    ChestCardItem item = go.GetComponent<ChestCardItem>();
    if (item == null)
    {
        Debug.Log("[DEBUG] SetupChestItem: Adicionando componente ChestCardItem");
        item = go.AddComponent<ChestCardItem>();
    }

        // Se as referências não foram arrastadas no prefab, tenta encontrá-las automaticamente
    if (item.cardArtImage == null)
    {
        Transform art = go.transform.Find("Card2D/Art");
        if (art != null) item.cardArtImage = art.GetComponent<RawImage>();
    }
    if (item.cardNameText == null)
    {
        Transform nameText = go.transform.Find("CardNameText");
        Debug.Log($"[DEBUG] SetupChestItem: CardNameText encontrado? {nameText != null}");
        item.cardNameText = nameText?.GetComponent<TextMeshProUGUI>();
    }
    if (item.cardStatsText == null)
    {
        Transform statsText = go.transform.Find("CardStatsText");
        Debug.Log($"[DEBUG] SetupChestItem: CardStatsText encontrado? {statsText != null}");
        item.cardStatsText = statsText?.GetComponent<TextMeshProUGUI>();
    }
    if (item.quantCardText == null)
    {
        Transform quantText = go.transform.Find("QuantCard");
        Debug.Log($"[DEBUG] SetupChestItem: QuantCard encontrado? {quantText != null}");
        item.quantCardText = quantText?.GetComponent<TextMeshProUGUI>();
    }
    if (item.monsterLvlText == null)
    {
        Transform monsterLvl = go.transform.Find("MonsterLvl");
        Debug.Log($"[DEBUG] SetupChestItem: MonsterLvl encontrado? {monsterLvl != null}");
        item.monsterLvlText = monsterLvl?.GetComponent<TextMeshProUGUI>();
    }
        if (item.attributeIcon == null)
    {
        Transform attribute = go.transform.Find("AttributeIcon");
        Debug.Log($"[DEBUG] SetupChestItem: AttributeIcon encontrado? {attribute != null} (Caminho: AttributeIcon)");
        item.attributeIcon = attribute?.GetComponent<Image>();
    }
    if (item.raceIcon == null)
    {
        Transform race = go.transform.Find("RaceIcon");
        Debug.Log($"[DEBUG] SetupChestItem: RaceIcon encontrado? {race != null} (Caminho: RaceIcon)");
        item.raceIcon = race?.GetComponent<Image>();
    }
    if (item.typeIcon == null)
    {
        Transform type = go.transform.Find("TypeIcon");
        Debug.Log($"[DEBUG] SetupChestItem: TypeIcon encontrado? {type != null} (Caminho: TypeIcon)");
        item.typeIcon = type?.GetComponent<Image>();
    }
    if (item.subTypeIcon == null)
    {
        Transform subType = go.transform.Find("SubTypeIcon");
        Debug.Log($"[DEBUG] SetupChestItem: SubTypeIcon encontrado? {subType != null} (Caminho: SubTypeIcon)");
        item.subTypeIcon = subType?.GetComponent<Image>();
    }

    // Array de estrelas
    if (item.stars == null || item.stars.Length == 0)
    {
        List<GameObject> starList = new List<GameObject>();
        for (int i = 1; i <= 12; i++)
        {
            Transform star = go.transform.Find($"Star{i:00}");
            if (star != null) starList.Add(star.gameObject);
        }
        item.stars = starList.ToArray();
    }

    int availableCopies = totalOwned - copiesInDecks;
    bool isNew = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id);
    bool isInDeck = cardIDsInDecks.Contains(card.id);

          // DEBUG: Antes de chamar Setup
      Debug.Log($"[DEBUG] SetupChestItem: Chamando item.Setup() - attributeIcon é nulo? {item.attributeIcon == null}");
      
      // Configura o item
      item.Setup(card, availableCopies, isNew, isInDeck);
      
      // DEBUG: Depois de chamar Setup
      Debug.Log($"[DEBUG] SetupChestItem: Setup concluído para '{card.name}'");

      // Configura hover para usar cores do GameManager
      CardDisplay chestDisplay = item.cardArtImage.transform.parent.GetComponent<CardDisplay>();
      if (chestDisplay != null)
      {
          // Configura hover baseado nas opções
          if (usePlayerHover) chestDisplay.isPlayerCard = true;
          else if (useOpponentHover) chestDisplay.isPlayerCard = false;
          else { chestDisplay.useSimpleHover = true; chestDisplay.hoverColor = customHoverColor; }
          chestDisplay.enableHoverLift = false; // Desabilita o efeito de levantar para chest
          chestDisplay.isInteractable = availableCopies > 0; // Permite hover apenas se houver cópias disponíveis
      }

      // Configura o DragHandler (se necessário)
      DeckDragHandler dragHandler = item.cardArtImage.transform.parent.gameObject.GetComponent<DeckDragHandler>();
      if (dragHandler == null)
          dragHandler = item.cardArtImage.transform.parent.gameObject.AddComponent<DeckDragHandler>();

      dragHandler.cardData = card;
      dragHandler.sourceZone = DeckZoneType.Trunk;
      dragHandler.enabled = availableCopies > 0;
  }

    /// <summary>
    /// Salva o deck atual no GameManager e no sistema de save persistente.
    /// </summary>
    public void SaveDeck()
    {
        if (!IsDeckValid())
        {
            UIManager.Instance?.ShowMessage($"Deck inválido! O Deck Principal deve ter entre {MIN_MAIN} e {MAX_MAIN} cartas.");
            return;
        }

       if (!hasUnsavedChanges)
        {
            Debug.Log("Nenhuma alteração para salvar.");
            return;
        }

        Debug.Log("Salvando deck...");

        // Atualiza os decks no GameManager antes de salvar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerDeck(mainDeck, sideDeck, extraDeck);
        }

        // Marca as cartas do deck como "usadas" para remover a tag "New" permanentemente
        if (SaveLoadSystem.Instance != null)
        {
            var allDeckCards = mainDeck.Concat(sideDeck).Concat(extraDeck);
            foreach (var card in allDeckCards)
            {
                SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
            }
        }

        // Salva o estado do jogo
        if (SaveLoadSystem.Instance != null && GameManager.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame(GameManager.Instance.currentSaveID);
        }

        hasUnsavedChanges = false;
        Debug.Log("Deck salvo com sucesso!");
        // Opcional: Atualizar a UI para desabilitar o botão de salvar, etc.
        // RefreshAllUI(); // Atualiza a UI para remover tags NEW
    }

    /// <summary>
    /// Sai da tela do Deck Builder, verificando se há alterações não salvas.
    /// </summary>
    public void Exit()
    {
        if (hasUnsavedChanges && !IsDeckValid())
        {
            UIManager.Instance?.ShowConfirmation(
                $"Seu Deck Principal é inválido (deve ter entre {MIN_MAIN} e {MAX_MAIN} cartas).\n\nDeseja sair mesmo assim e descartar as alterações?",
                () => {
                    // Discard changes by reloading from GameManager
                    LoadCurrentDeckFromManager();
                    hasUnsavedChanges = false;
                    UIManager.Instance.ShowScreen(UIManager.Instance.newGameMenu);
                }
            );
        }
        else if (hasUnsavedChanges)
        {
            UIManager.Instance?.ShowConfirmation(
                "Você tem alterações não salvas. Deseja sair sem salvar?",
                () => {
                    LoadCurrentDeckFromManager();
                    hasUnsavedChanges = false;
                    UIManager.Instance.ShowScreen(UIManager.Instance.newGameMenu);
                }
            );
        }
        else
        {
            UIManager.Instance?.ShowScreen(UIManager.Instance.newGameMenu);
        }
    }

    /// <summary>
    /// Cria a tag "NEW" em uma carta, usando um prefab ou gerando dinamicamente.
    /// </summary>
public void CreateNewBanner(Transform parent)
    {
        if (newTagPrefab != null)
        {
            GameObject tagInstance = Instantiate(newTagPrefab, parent);
            tagInstance.transform.localPosition = Vector3.zero;
        }
        else
        {
            GameObject tagObject = new GameObject("NewTag_Dynamic");
            tagObject.transform.SetParent(parent, false);
            RectTransform tagRect = tagObject.AddComponent<RectTransform>();

            tagRect.anchorMin = new Vector2(0.5f, 0.5f);
            tagRect.anchorMax = new Vector2(0.5f, 0.5f);
            tagRect.pivot = new Vector2(0.5f, 0.5f);
            tagRect.anchoredPosition = Vector2.zero;
            tagRect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>().rect.width * 0.8f, 30);

            Image banner = tagObject.AddComponent<Image>();
            banner.color = newTagBannerColor;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(tagRect, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = "NEW";
            text.color = newTagTextColor;
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
        }
    }
    
    /// <summary>
    /// Atualiza o CardViewer com a carta que o mouse está sobre.
    /// </summary>
    public void OnCardHover(CardData card)
    {
        if (cardViewer != null)
        {
            cardViewer.DisplayCardData(card);
        }
    }
    
    /// <summary>
    /// Adiciona uma carta a um dos decks (Main, Side, Extra), validando as regras.
    /// </summary>
    public bool AddCardToDeck(CardData card, DeckZoneType targetZone)
    {
       // Validação de Limite de Cópias
       int currentCopies = mainDeck.Count(c => c.id == card.id) + sideDeck.Count(c => c.id == card.id) + extraDeck.Count(c => c.id == card.id);
       int banlistLimit = banList.ContainsKey(card.name) ? banList[card.name] : MAX_COPIES;
       if (GameManager.Instance.disableBanlist) banlistLimit = MAX_COPIES;
       if (currentCopies >= banlistLimit) return false;

       // Validação de Zona
       if (targetZone == DeckZoneType.Main)
       {
           if (card.type.Contains("Fusion")) return false; // Fusões não vão no Main Deck
           if (mainDeck.Count >= MAX_MAIN) return false;
           mainDeck.Add(card);
       }
       else if (targetZone == DeckZoneType.Side)
       {
           if (card.type.Contains("Fusion")) return false;
           if (sideDeck.Count >= MAX_SIDE) return false;
           sideDeck.Add(card);
       }
       else if (targetZone == DeckZoneType.Extra)
       {
           if (!card.type.Contains("Fusion")) return false; // Apenas Fusões no Extra Deck
           if (extraDeck.Count >= MAX_EXTRA) return false;
           extraDeck.Add(card);
       }
       else if (targetZone == DeckZoneType.Trunk)
       {
        // Soltar de volta no Trunk não faz nada, apenas retorna true para o DragHandler
           return true;
       }
       
       hasUnsavedChanges = true;
        // Marca a carta como "usada" para remover a tag "NEW"
        if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
       RefreshAllUI(); // Atualiza toda a UI para refletir a mudança
       return true; // Retorna true se foi bem sucedido
    }

    /// <summary>
    /// Remove uma carta de um deck específico.
    /// </summary>
    public void RemoveCard(CardData card, DeckZoneType sourceZone)
    {
        bool removed = false;
        switch (sourceZone)
        {
            case DeckZoneType.Main:
                if (mainDeck.Contains(card))
                {
                    mainDeck.Remove(card);
                    removed = true;
                }
                break;
            case DeckZoneType.Side:
                if (sideDeck.Contains(card))
                {
                    sideDeck.Remove(card);
                    removed = true;
                }
                break;
            case DeckZoneType.Extra:
                if (extraDeck.Contains(card))
                {
                    extraDeck.Remove(card);
                    removed = true;
                }
                break;
        }

        if (removed)
        {
            hasUnsavedChanges = true;
            RefreshAllUI();
        }    }

    /// <summary>
    /// Aciona um feedback visual (piscar) em uma zona de deck.
    /// </summary>
    public void TriggerInvalidMoveFeedback(DeckZoneType zone)
    {
        // Lógica para piscar o painel da zona inválida
        Debug.Log($"Movimento inválido para a zona: {zone}");
    }

    /// <summary>
    /// Atualiza os textos de contagem de cartas para todos os decks.
    /// </summary>

    private void UpdateCounts()
    {
        if(mainDeckCountText) mainDeckCountText.text = $"{mainDeck.Count}/{MAX_MAIN}";
        if(sideDeckCountText) sideDeckCountText.text = $"{sideDeck.Count}/{MAX_SIDE}";
        if(extraDeckCountText) extraDeckCountText.text = $"{extraDeck.Count}/{MAX_EXTRA}";
    }

    /// <summary>
    /// Atualiza a UI de paginação (texto e botões).
    /// </summary>
    private void UpdatePaginationUI()
    {
        if (txtPageInfo != null)
        {
            txtPageInfo.text = $"Page {currentPage} / {totalPages}";
        }
        if (btnPrevPage != null) btnPrevPage.interactable = currentPage > 1;
        if (btnNextPage != null) btnNextPage.interactable = currentPage < totalPages;
    }

    // --- MÉTODOS PÚBLICOS PARA OS BOTÕES DE UI ---

    public void SetSortType(SortType newSort)
    {
        if (currentSort == newSort)
        {
            sortAscending = !sortAscending; // Inverte a direção se clicar no mesmo botão
        }
        else
        {
            currentSort = newSort;
            sortAscending = true; // Padrão para ascendente ao trocar de tipo
        }
        RefreshTrunkUI();
    }

    public void ToggleFilter(string filterKey)
    {
        if (activeFilters.ContainsKey(filterKey))
        {
            activeFilters[filterKey] = !activeFilters[filterKey];
        }
        UpdateFilterButtonsVisuals();
        RefreshTrunkUI();
    }

    private void UpdateFilterButtonsVisuals()
    {
        // Torna os botões de filtro cinza/branco para indicar se estão ativos
        if (btnFilterNormal) btnFilterNormal.GetComponent<Image>().color = activeFilters["Normal"] ? activeColor : inactiveColor;
        if (btnFilterEffect) btnFilterEffect.GetComponent<Image>().color = activeFilters["Effect"] ? activeColor : inactiveColor;
        if (btnFilterSpell) btnFilterSpell.GetComponent<Image>().color = activeFilters["Spell"] ? activeColor : inactiveColor;
        if (btnFilterTrap) btnFilterTrap.GetComponent<Image>().color = activeFilters["Trap"] ? activeColor : inactiveColor;
        if (btnFilterFusion) btnFilterFusion.GetComponent<Image>().color = activeFilters["Fusion"] ? activeColor : inactiveColor;
        if (btnFilterRitual) btnFilterRitual.GetComponent<Image>().color = activeFilters["Ritual"] ? activeColor : inactiveColor;
    }

    public void NextPage()
    {
        if (currentPage < totalPages) { currentPage++; RefreshTrunkUI(); }
    }

          public void PrevPage()
      {
          if (currentPage > 1) { currentPage--; RefreshTrunkUI(); }
      }
  
      // Método para carregar cartas de forma assíncrona (performance)
      private System.Collections.IEnumerator PopulateChestAsync(List<System.Linq.IGrouping<string, CardData>> paginatedGroups, System.Collections.Generic.Dictionary<string, int> deckCardCounts)
      {
        int batchSize = 10; // Processa 10 cartas por frame para evitar picos de lag
        int poolIndex = 0;

        for (int i = 0; i < paginatedGroups.Count; i++)
        {
            var group = paginatedGroups[i];
            CardData card = group.First();
            int totalOwned = group.Count();
            int copiesInDecks = deckCardCounts.GetValueOrDefault(card.id, 0);

            GameObject go;
            if (poolIndex < chestItemPool.Count)
            {
                go = chestItemPool[poolIndex];
            }
            else
            {
                go = Instantiate(cardChestItemPrefab, trunkContent);
                chestItemPool.Add(go);
            }
            poolIndex++;

            go.SetActive(true);
            go.transform.SetAsLastSibling();

            SetupChestItem(go, card, totalOwned, copiesInDecks);

            if (i % batchSize == 0)
                yield return null;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(trunkContent.GetComponent<RectTransform>());
        Debug.Log($"[DeckBuilder] Carregamento assíncrono concluído: {paginatedGroups.Count} cartas");
    }
}
public enum DeckZoneType { Trunk, Main, Side, Extra }
