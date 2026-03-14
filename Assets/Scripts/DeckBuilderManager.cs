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
    private ScrollRect trunkScrollRect; // Para virtual scrolling

    [Header("UI References - Info")]
    public TextMeshProUGUI mainDeckCountText;
    public TextMeshProUGUI sideDeckCountText;
    public TextMeshProUGUI extraDeckCountText;
    public TMP_InputField deckNameInput;

    [Header("UI References - Main Deck Counts")]
    public TextMeshProUGUI mainNormalCountText;
    public TextMeshProUGUI mainEffectCountText;
    public TextMeshProUGUI mainSpellCountText;
    public TextMeshProUGUI mainTrapCountText;
    public TextMeshProUGUI mainRitualCountText;

    [Header("UI References - Side Deck Counts")]
    public TextMeshProUGUI sideNormalCountText;
    public TextMeshProUGUI sideEffectCountText;
    public TextMeshProUGUI sideSpellCountText;
    public TextMeshProUGUI sideTrapCountText;
    public TextMeshProUGUI sideRitualCountText;

    [Header("UI References - Extra Deck Counts")]
    public TextMeshProUGUI extraFusionCountText;

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
    public Button btnImport;
    public Button btnExport;
    
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

    // Dicionários para acesso rápido em runtime (Otimização)
    [HideInInspector] public Dictionary<string, Sprite> attributeIconsDict;
    [HideInInspector] public Dictionary<string, Sprite> raceIconsDict;
    [HideInInspector] public Dictionary<string, Sprite> typeIconsDict;
    [HideInInspector] public Dictionary<string, Sprite> subTypeIconsDict;

    private List<CardData> mainDeck = new List<CardData>();
    private List<CardData> sideDeck = new List<CardData>();
    private List<CardData> extraDeck = new List<CardData>();
    private List<CardData> currentTrunk = new List<CardData>();
    private List<IGrouping<string, CardData>> filteredCardGroups = new List<IGrouping<string, CardData>>();

    [Header("Virtual Scrolling")]
    public float itemHeight = 125f; // Altura de cada item da lista + espaçamento
    private bool isVirtualScrollInitialized = false;

    private Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();
    private Coroutine searchDebounceCoroutine;

    // Conjunto de IDs de cartas nos decks para verificação rápida da tag "NEW".
    private HashSet<string> cardIDsInDecks = new HashSet<string>();

    private bool hasUnsavedChanges = false;

        [Header("Pooling")]
    [Tooltip("Tamanho inicial do pool de itens do chest para performance.")]
    public int initialPoolSize = 30;
    
    // Sistema de Pooling para melhor performance (Queue-based)
    private Queue<GameObject> availableChestItems = new Queue<GameObject>();
    private List<GameObject> activeChestItems = new List<GameObject>();

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
        InitializeIconDictionaries();
        InitializeChestItemPool(); // MOVIDO: Garante que o pool exista antes de OnEnable/Start
    }

    void Start()
    {
        // Esconde o painel de import/export no início

        // Adiciona listeners para os botões de ação principais
        btnSave?.onClick.AddListener(SaveDeck);
        btnExit?.onClick.AddListener(Exit);

        // Adiciona listeners para os botões de Import/Export
        btnImport?.onClick.AddListener(UIManager.Instance.Btn_ShowImportPanel);
        btnExport?.onClick.AddListener(UIManager.Instance.Btn_ShowExportPanel);

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
        searchInput?.onValueChanged.AddListener(OnSearchInputChanged);

        // Virtual Scrolling Setup
        trunkScrollRect = trunkContent.GetComponentInParent<ScrollRect>();
        if (trunkScrollRect != null)
        {
            trunkScrollRect.onValueChanged.AddListener(OnScroll);
        }

        // Configura as zonas de drop
        SetupDropZones();
    }

    // Novo método para otimização
    private void InitializeIconDictionaries()
    {
        attributeIconsDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in attributeIcons)
            if (!attributeIconsDict.ContainsKey(mapping.name))
                attributeIconsDict.Add(mapping.name, mapping.icon);

        raceIconsDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in raceIcons)
            if (!raceIconsDict.ContainsKey(mapping.name))
                raceIconsDict.Add(mapping.name, mapping.icon);

        typeIconsDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in typeIcons)
            if (!typeIconsDict.ContainsKey(mapping.name))
                typeIconsDict.Add(mapping.name, mapping.icon);

        subTypeIconsDict = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in subTypeIcons)
            if (!subTypeIconsDict.ContainsKey(mapping.name))
                subTypeIconsDict.Add(mapping.name, mapping.icon);
    }

    private void OnSearchInputChanged(string newText)
    {
        if (searchDebounceCoroutine != null)
        {
            StopCoroutine(searchDebounceCoroutine);
        }
        searchDebounceCoroutine = StartCoroutine(DebounceSearch());
    }

    private IEnumerator DebounceSearch()
    {
        yield return new WaitForSeconds(0.3f); // Aguarda 300ms
        RefreshTrunkUI();
    }

        void InitializeChestItemPool()
    {
        if (cardChestItemPrefab == null) return;

        // Limpa pool antigo (destroi objetos)
        foreach (var item in availableChestItems) if (item != null) Destroy(item);
        availableChestItems.Clear();
        activeChestItems.Clear();

        int poolSize = Mathf.Max(initialPoolSize, itemsPerPage) + 5; // buffer
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(cardChestItemPrefab, trunkContent);
            go.SetActive(false);

            // Verifica se o componente existe (se não, já mostra erro)
            if (go.GetComponent<ChestCardItem>() == null)
            {
                Debug.LogError($"[Pool] Prefab não tem ChestCardItem! Objeto {go.name} será destruído.", go);
                Destroy(go);
                continue;
            }

            availableChestItems.Enqueue(go);
        }
        Debug.Log($"[Pool] Inicializado com {availableChestItems.Count} itens.");
    }

    /// <summary>
    /// Obtém um item do pool ou cria um novo se necessário
    /// </summary>
    private GameObject GetPooledChestItem()
    {
        if (availableChestItems.Count > 0)
        {
            GameObject item = availableChestItems.Dequeue();
            item.SetActive(true);
            return item;
        }
        else
        {
            Debug.LogWarning("[Pool] Pool esgotado, instanciando novo item. Ajuste o tamanho do pool.");
            GameObject go = Instantiate(cardChestItemPrefab, trunkContent);
            if (go.GetComponent<ChestCardItem>() == null)
            {
                Debug.LogError("Novo item instanciado sem ChestCardItem!");
                Destroy(go);
                return null;
            }
            return go;
        }
    }

    /// <summary>
    /// Devolve um item ao pool para reutilização
    /// </summary>
    private void ReturnPooledChestItem(GameObject item)
    {
        if (item == null) return;
        item.SetActive(false);
        availableChestItems.Enqueue(item);
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
      
      // Limpa o pool de itens
      foreach (var item in activeChestItems)
          if (item != null) ReturnPooledChestItem(item);
      activeChestItems.Clear();
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

    // A UI é atualizada em uma corrotina para garantir que o layout do Unity
    // tenha tempo de calcular as dimensões corretas, corrigindo o bug do scroll.
    StartCoroutine(RefreshAllUIAfterFrame());

    UpdateFilterButtonsVisuals();
    hasUnsavedChanges = false;
}
    private IEnumerator RefreshAllUIAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        RefreshAllUI();
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

        // Carrega TODAS as cartas do banco de dados para o chest/trunk.
        currentTrunk = new List<CardData>(GameManager.Instance.cardDatabase.cardDatabase);
        
        Debug.Log($"[DeckBuilder] Loaded {currentTrunk.Count} cards from the main database into the trunk.");
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

        // A atualização do Trunk agora é a última coisa a ser feita
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
            customLayout.RefreshLayout();
        }
    }

    void RefreshTrunkUI()
    {
        // DEBUG: Verifique se o baú tem cartas antes de filtrar.
        Debug.Log($"[DeckBuilderManager] RefreshTrunkUI called. currentTrunk has {currentTrunk.Count} cards.");

        if (trunkContent == null)
        {
            Debug.LogError("[DeckBuilder] CRITICAL ERROR: 'Trunk Content' is not assigned in the Inspector! Cards will not appear.");
            return;
        }

                  // Devolve todos os itens ativos ao pool
          foreach (var item in activeChestItems)
          {
              if (item != null) ReturnPooledChestItem(item);
          }
          activeChestItems.Clear();

        string searchText = searchInput != null ? searchInput.text.ToLowerInvariant() : "";
        bool anyFilterActive = activeFilters.Any(kvp => kvp.Value);

        // LOG ADICIONADO
        if (anyFilterActive)
        {
            var activeFilterNames = activeFilters.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
            Debug.Log($"[Filter] Filtros ativos: {string.Join(", ", activeFilterNames)}");
        }

        filteredCardGroups = currentTrunk
            .GroupBy(c => c.id)
            .Where(g =>
            {
                CardData card = g.First();
                if (!string.IsNullOrEmpty(searchText) && !card.name.ToLowerInvariant().Contains(searchText))
                {
                    return false;
                }

                if (!anyFilterActive)
                {
                    return true;
                }
                
                string cardType = card.type;
                if (activeFilters["Normal"] && cardType == "Monster (Normal)") return true;
                if (activeFilters["Effect"] && cardType == "Monster (Effect)") return true;
                if (activeFilters["Ritual"] && cardType.Contains("Ritual")) return true;
                if (activeFilters["Fusion"] && cardType.Contains("Fusion")) return true;
                if (activeFilters["Spell"] && cardType.Contains("Spell")) return true;
                if (activeFilters["Trap"] && cardType.Contains("Trap")) return true;

                return false;
            }).ToList();


        // Aplica a ordenação
        switch (currentSort)
        {
            case SortType.ABC:
                filteredCardGroups = sortAscending ? filteredCardGroups.OrderBy(g => g.First().name).ToList() : filteredCardGroups.OrderByDescending(g => g.First().name).ToList();
                break;
            case SortType.Atk:
                // Garante que monstros venham primeiro, depois ordena por ATK
                filteredCardGroups = filteredCardGroups.OrderBy(g => g.First().type.Contains("Monster") ? 0 : 1)
                                   .ThenByDescending(g => sortAscending ? -g.First().atk : g.First().atk).ToList();
                break;
            case SortType.Def:
                // Garante que monstros venham primeiro, depois ordena por DEF
                filteredCardGroups = filteredCardGroups.OrderBy(g => g.First().type.Contains("Monster") ? 0 : 1)
                                   .ThenByDescending(g => sortAscending ? -g.First().def : g.First().def).ToList();
                break;
        }

        // --- VIRTUAL SCROLLING SETUP ---
        if (btnPrevPage) btnPrevPage.gameObject.SetActive(false);
        if (btnNextPage) btnNextPage.gameObject.SetActive(false);
        if (txtPageInfo) txtPageInfo.text = $"{filteredCardGroups.Count} / {currentTrunk.Count} Cards";
 
        Debug.Log($"[DeckBuilderManager] Found {filteredCardGroups.Count} card groups after filtering.");
 
        float totalHeight = filteredCardGroups.Count * itemHeight;
        RectTransform contentRect = trunkContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, totalHeight);
        }
 
        if (trunkScrollRect != null)
        {
            trunkScrollRect.content = contentRect;
            trunkScrollRect.vertical = true;
            trunkScrollRect.horizontal = false;
            trunkScrollRect.scrollSensitivity = 30f;
 
            if (trunkScrollRect.verticalScrollbar == null)
            {
                Scrollbar scrollbar = trunkScrollRect.transform.GetComponentInChildren<Scrollbar>();
                if (scrollbar != null) trunkScrollRect.verticalScrollbar = scrollbar;
            }
        }
 
        isVirtualScrollInitialized = true;
        OnScroll(Vector2.zero);
    }

          private void OnScroll(Vector2 scrollPos)
      {
          if (!isVirtualScrollInitialized || trunkScrollRect == null || itemHeight <= 0) return;

          // --- 1. Recolher todos os itens atualmente ativos de volta ao pool ---
          foreach (var item in activeChestItems)
          {
              if (item != null) ReturnPooledChestItem(item);
          }
          activeChestItems.Clear();

          // --- 2. Calcular índices visíveis ---
          float viewportHeight = (trunkScrollRect.viewport as RectTransform).rect.height;
          float scrollPosition = (trunkContent as RectTransform).anchoredPosition.y;

          int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(scrollPosition / itemHeight));
          int lastVisibleIndex = Mathf.Min(filteredCardGroups.Count - 1, Mathf.CeilToInt((scrollPosition + 
viewportHeight) / itemHeight));

          // --- 3. Para cada índice visível, obter item do pool, posicionar e configurar ---
          for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
          {
              if (i < 0 || i >= filteredCardGroups.Count) continue;

              GameObject itemGO = GetPooledChestItem();
              if (itemGO == null) continue;

              // Posiciona
              RectTransform itemRect = itemGO.GetComponent<RectTransform>();
              itemRect.anchoredPosition = new Vector2(0, -i * itemHeight);

              // Configura os dados da carta
              var group = filteredCardGroups[i];
              CardData card = group.First();

              int limit = banList.ContainsKey(card.name) ? banList[card.name] : MAX_COPIES;
              if (GameManager.Instance != null && GameManager.Instance.disableBanlist) limit = MAX_COPIES;
              int totalOwned = limit;

              int copiesInDecks = mainDeck.Count(c => c.id == card.id) + sideDeck.Count(c => c.id == card.id) + 
extraDeck.Count(c => c.id == card.id);
              int availableCopies = totalOwned - copiesInDecks;

              SetupChestItem(itemGO, card, availableCopies, copiesInDecks);
              activeChestItems.Add(itemGO);
          }
      }

    void SetupChestItem(GameObject go, CardData card, int availableCopies, int copiesInDecks)
    {
        ChestCardItem item = go.GetComponentInChildren<ChestCardItem>();
        if (item == null)
        {
            Debug.LogError($"Objeto {go.name} no pool não tem o componente ChestCardItem!", go);
            return;
        }
        
        bool isNew = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id);
        bool isInDeck = copiesInDecks > 0;

        item.Setup(card, availableCopies, isNew, isInDeck);
        
        CardDisplay chestDisplay = go.GetComponentInChildren<CardDisplay>();
        if (chestDisplay != null)
        {
            if (usePlayerHover) chestDisplay.isPlayerCard = true;
            else if (useOpponentHover) chestDisplay.isPlayerCard = false;
            else { chestDisplay.useSimpleHover = true; chestDisplay.hoverColor = customHoverColor; }
            
            chestDisplay.enableHoverLift = false;
            chestDisplay.isInteractable = availableCopies > 0;
        }

        DeckDragHandler dragHandler = go.GetComponentInChildren<DeckDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.cardData = card;
            dragHandler.sourceZone = DeckZoneType.Trunk;
            dragHandler.enabled = availableCopies > 0;
        }
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
        Image zoneImage = null;
        switch (zone)
        {
            case DeckZoneType.Main:
                zoneImage = mainDeckContent.GetComponentInParent<Image>();
                break;
            case DeckZoneType.Side:
                zoneImage = sideDeckContent.GetComponentInParent<Image>();
                break;
            case DeckZoneType.Extra:
                zoneImage = extraDeckContent.GetComponentInParent<Image>();
                break;
        }

        if (zoneImage != null)
        {
            StartCoroutine(FlashImageColor(zoneImage, invalidMoveColor, flashDuration));
        }
        else
        {
            Debug.LogWarning($"Movimento inválido para a zona: {zone}, mas não foi possível encontrar a imagem do painel para piscar.");
        }
    }

    // --- SISTEMA DE IMPORTAÇÃO/EXPORTAÇÃO (INTERNO) ---

    public void ExportCurrentDeck(string deckName)
    {
        if (string.IsNullOrWhiteSpace(deckName))
        {
            // Idealmente, mostrar uma mensagem de erro na UI
            Debug.LogError("O nome do deck não pode ser vazio.");
            return;
        }
        SaveLoadSystem.Instance.SaveDeckRecipe(deckName, mainDeck, sideDeck, extraDeck);
        // Salva o jogo para persistir a nova receita de deck
        SaveLoadSystem.Instance.SaveGame(GameManager.Instance.currentSaveID);
        Debug.Log($"Deck '{deckName}' exportado para o save.");
    }

    public void ImportDeck(string deckName)
    {
        if (SaveLoadSystem.Instance.LoadDeckFromRecipe(deckName, out var mainIDs, out var sideIDs, out var extraIDs))
        {
            mainDeck = mainIDs.Select(id => GameManager.Instance.cardDatabase.GetCardById(id)).Where(c => c != null).ToList();
            sideDeck = sideIDs.Select(id => GameManager.Instance.cardDatabase.GetCardById(id)).Where(c => c != null).ToList();
            extraDeck = extraIDs.Select(id => GameManager.Instance.cardDatabase.GetCardById(id)).Where(c => c != null).ToList();

            hasUnsavedChanges = true;
            RefreshAllUI();
            Debug.Log($"Deck '{deckName}' importado com sucesso.");
        }
        else
        {
            Debug.LogError($"Não foi possível encontrar a receita do deck '{deckName}'.");
        }
    }

    /// <summary>
    /// Atualiza os textos de contagem de cartas para todos os decks.
    /// </summary>

    private void UpdateCounts()
    {
        // Contagem total
        if (mainDeckCountText) mainDeckCountText.text = $"{mainDeck.Count}/{MAX_MAIN}";
        if (sideDeckCountText) sideDeckCountText.text = $"{sideDeck.Count}/{MAX_SIDE}";
        if (extraDeckCountText) extraDeckCountText.text = $"{extraDeck.Count}/{MAX_EXTRA}";

        // --- Contagem por tipo - Main Deck ---
        int mainNormal = mainDeck.Count(c => c.type.Contains("Normal") && c.type.Contains("Monster"));
        int mainEffect = mainDeck.Count(c => c.type.Contains("Effect") && c.type.Contains("Monster"));
        int mainRitual = mainDeck.Count(c => c.type.Contains("Ritual") && c.type.Contains("Monster"));
        int mainSpell = mainDeck.Count(c => c.type.Contains("Spell"));
        int mainTrap = mainDeck.Count(c => c.type.Contains("Trap"));
        
        if (mainNormalCountText) mainNormalCountText.text = mainNormal.ToString();
        if (mainEffectCountText) mainEffectCountText.text = mainEffect.ToString();
        if (mainRitualCountText) mainRitualCountText.text = mainRitual.ToString();
        if (mainSpellCountText) mainSpellCountText.text = mainSpell.ToString();
        if (mainTrapCountText) mainTrapCountText.text = mainTrap.ToString();

        // --- Contagem por tipo - Side Deck ---
        int sideNormal = sideDeck.Count(c => c.type.Contains("Normal") && c.type.Contains("Monster"));
        int sideEffect = sideDeck.Count(c => c.type.Contains("Effect") && c.type.Contains("Monster"));
        int sideRitual = sideDeck.Count(c => c.type.Contains("Ritual") && c.type.Contains("Monster"));
        int sideSpell = sideDeck.Count(c => c.type.Contains("Spell"));
        int sideTrap = sideDeck.Count(c => c.type.Contains("Trap"));

        if (sideNormalCountText) sideNormalCountText.text = sideNormal.ToString();
        if (sideEffectCountText) sideEffectCountText.text = sideEffect.ToString();
        if (sideRitualCountText) sideRitualCountText.text = sideRitual.ToString();
        if (sideSpellCountText) sideSpellCountText.text = sideSpell.ToString();
        if (sideTrapCountText) sideTrapCountText.text = sideTrap.ToString();

        // --- Contagem por tipo - Extra Deck ---
        int extraFusion = extraDeck.Count(c => c.type.Contains("Fusion") && c.type.Contains("Monster"));
        if (extraFusionCountText) extraFusionCountText.text = extraFusion.ToString();
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
            // Verifica se o filtro que estamos clicando já estava ativo.
            bool wasActive = activeFilters[filterKey];

            // Primeiro, desliga todos os filtros para garantir exclusividade.
            var keys = new List<string>(activeFilters.Keys);
            foreach (var key in keys)
            {
                activeFilters[key] = false;
            }

            // Se o filtro clicado NÃO estava ativo, nós o ativamos.
            // Se ele JÁ estava ativo, ele permanecerá desligado, como todos os outros.
            if (!wasActive)
            {
                activeFilters[filterKey] = true;
            }
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

    private IEnumerator FlashImageColor(Image image, Color flashColor, float duration)
    {
        if (image == null) yield break;
        Color originalColor = image.color;
        image.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (image != null) image.color = originalColor; // Verifica se o objeto ainda existe
    }
}
public enum DeckZoneType { Trunk, Main, Side, Extra }
