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

    // Conjunto de IDs de cartas nos decks para verificação rápida da tag "NEW".
    private HashSet<string> cardIDsInDecks = new HashSet<string>();

    private bool hasUnsavedChanges = false;

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
    }

    void OnEnable()
    {
        // Carrega o deck ativo do GameManager
        LoadCurrentDeckFromManager();
        // Carrega o baú do jogador
        LoadTrunk();

        // Adiciona logs para depuração
        Debug.Log($"[DeckBuilder] OnEnable: {currentTrunk.Count} cards loaded into trunk.");
        foreach (var filter in activeFilters)
        {
            Debug.Log($"[DeckBuilder] Initial Filter State: {filter.Key} = {filter.Value}");
        }

        // Atualiza toda a interface gráfica com os dados carregados
        RefreshAllUI();
        // Atualiza o estado visual dos botões de filtro
        UpdateFilterButtonsVisuals();
        // Reseta o estado de "alterações não salvas" ao abrir a tela
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
            display.isPlayerCard = true; // <<<< FIX: Garante que a cor do hover seja a do jogador
            display.SetCard(card, GameManager.Instance?.GetCardBackTexture(), true);
            display.isInteractable = false;

            DeckDragHandler drag = go.GetComponent<DeckDragHandler>();
            if (drag == null) drag = go.AddComponent<DeckDragHandler>();
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
        if (trunkContent == null)
        {
            Debug.LogError("[DeckBuilder] ERRO CRÍTICO: 'Trunk Content' não está atribuído no Inspector! As cartas não aparecerão.");
            return;
        }

        // Limpa a lista de cartas do baú
        foreach (Transform child in trunkContent) Destroy(child.gameObject);

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

        // Popula a UI com as cartas da página atual
        foreach (var group in paginatedGroups)
        {
            CardData card = group.First();
            int totalOwned = group.Count();
            int copiesInDecks = deckCardCounts.GetValueOrDefault(card.id, 0);

            GameObject go = Instantiate(cardChestItemPrefab, trunkContent);
            // Configura os detalhes e interações do item
            SetupChestItem(go, card, totalOwned, copiesInDecks);
        }
    }
    
    void SetupChestItem(GameObject go, CardData card, int totalOwned, int copiesInDecks)
    {
        // --- 1. Encontra os componentes na hierarquia do prefab ---
        var display = go.GetComponentInChildren<CardDisplay>();
        if (display != null)
        {
            display.isPlayerCard = true; // <<<< FIX: Garante que a cor do hover seja a do jogador
        }

        Transform card2DTr = go.transform.Find("Card2D");

        // --- 2. Preenche os textos e ícones ---
        var cardNameText = go.transform.Find("CardNameText")?.GetComponent<TextMeshProUGUI>();
        var cardStatsText = go.transform.Find("CardStatsText")?.GetComponent<TextMeshProUGUI>();
        var quantCardText = go.transform.Find("QuantCard")?.GetComponent<TextMeshProUGUI>();
        var monsterLvlText = go.transform.Find("MonsterLvl")?.GetComponent<TextMeshProUGUI>();
        var attributeImage = go.transform.Find("AttributeIcon")?.GetComponent<Image>();
        var typeImage = go.transform.Find("TypeIcon")?.GetComponent<Image>();
        var subTypeImage = go.transform.Find("SubTypeIcon")?.GetComponent<Image>();
        var raceImage = go.transform.Find("RaceIcon")?.GetComponent<Image>();
        var newTagInstance = go.transform.Find("NewTag"); // Procura por uma tag existente

        int availableCopies = totalOwned - copiesInDecks;

        if (cardNameText) cardNameText.text = card.name; // Nome da carta
        if (quantCardText) quantCardText.text = $"x{availableCopies}"; // Quantidade disponível

        bool isMonster = card.type.Contains("Monster");

        if (isMonster)
        {
            // ATK/DEF
            if (cardStatsText) cardStatsText.text = $"{card.atk} / {card.def}";

            // LÓGICA PARA MONSTROS
            if (monsterLvlText) monsterLvlText.gameObject.SetActive(true);
            if (monsterLvlText) monsterLvlText.text = card.level.ToString();
            
            if (attributeImage) 
            {
                attributeImage.gameObject.SetActive(true);
                var iconMapping = attributeIcons.Find(i => i.name.Equals(card.attribute, System.StringComparison.OrdinalIgnoreCase)); // Busca na lista de Atributos
                if (iconMapping != null && iconMapping.icon != null)
                {
                    attributeImage.sprite = iconMapping.icon;
                    attributeImage.enabled = true;
                }
                else
                {
                    attributeImage.enabled = false;
                    Debug.LogWarning($"[ICONS] Ícone de ATRIBUTO não encontrado para '{card.attribute}' na carta '{card.name}'. Verifique a lista 'attributeIcons'.");
                }
            }
            
            if (raceImage)
            {
                raceImage.gameObject.SetActive(true);
                var iconMapping = raceIcons.Find(i => i.name.Equals(card.race, System.StringComparison.OrdinalIgnoreCase)); // Busca na lista de Raças
                if (iconMapping != null && iconMapping.icon != null)
                {
                    raceImage.sprite = iconMapping.icon;
                    raceImage.enabled = true;
                }
                else
                {
                    raceImage.enabled = false;
                    if (!string.IsNullOrEmpty(card.race)) Debug.LogWarning($"[ICONS] Ícone de RAÇA não encontrado para '{card.race}' na carta '{card.name}'. Verifique a lista 'raceIcons'.");
                }
            }

            // Desativa os ícones de Magia/Armadilha
            if (typeImage) typeImage.gameObject.SetActive(false);
            if (subTypeImage) subTypeImage.gameObject.SetActive(false);

            for (int i = 1; i <= 12; i++)
            {
                var star = go.transform.Find($"Star{i:00}");
                if (star != null) star.gameObject.SetActive(i <= card.level);
            }
        }
        else // Magias e Armadilhas
        {
            // Limpa o texto de stats para não-monstros
            if (cardStatsText) cardStatsText.text = "";

            // Desativa os ícones de Monstro
            if (monsterLvlText) monsterLvlText.gameObject.SetActive(false);
            if (attributeImage) attributeImage.gameObject.SetActive(false);
            if (raceImage) raceImage.gameObject.SetActive(false);

            // Tipo (TypeIcon)
            string mainType = card.type.Contains("Spell") ? "Spell" : "Trap";
            if (typeImage) // Usar TypeIcon para o tipo principal (Spell/Trap)
            {
                typeImage.gameObject.SetActive(true);
                var iconMapping = typeIcons.Find(i => i.name.Equals(mainType, System.StringComparison.OrdinalIgnoreCase)); // Busca na lista de Tipos (Spell/Trap)
                if (iconMapping != null && iconMapping.icon != null)
                {
                    typeImage.sprite = iconMapping.icon;
                    typeImage.enabled = true;
                }
                else
                {
                    typeImage.enabled = false;
                    Debug.LogWarning($"[ICONS] Ícone de TIPO PRINCIPAL não encontrado para '{mainType}' na carta '{card.name}'. Verifique a lista 'typeIcons'.");
                }
            }
            
            // SubTipo (SubTypeIcon)
            if (subTypeImage) // Usar SubTypeIcon para a propriedade (Continuous, Equip, etc.)
            {
                subTypeImage.gameObject.SetActive(true);
                var iconMapping = subTypeIcons.Find(i => i.name.Equals(card.property, System.StringComparison.OrdinalIgnoreCase)); // Busca na lista de SubTipos
                subTypeImage.sprite = iconMapping?.icon;
                subTypeImage.gameObject.SetActive(iconMapping?.icon != null);
                
                if (iconMapping == null && !string.IsNullOrEmpty(card.property) && card.property != "Normal" && card.property != "N/A")
                {
                    Debug.LogWarning($"[ICONS] Ícone de SUBTIPO (Property) não encontrado para '{card.property}' na carta '{card.name}'. Verifique a lista 'subTypeIcons'.");
                }
            }
            for (int i = 1; i <= 12; i++)
            {
                var star = go.transform.Find($"Star{i:00}");
                if (star != null) star.gameObject.SetActive(false);
            }
        }

        // --- 3. Lógica da Tag "New" ---
        bool isNew = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id);
        bool isInDeck = cardIDsInDecks.Contains(card.id);
        Transform newTagParent = card2DTr != null ? card2DTr : go.transform;
        if (isNew && !isInDeck && newTagParent != null)
        {
            CreateNewBanner(newTagParent);
        }

        // --- 4. Lógica de Estado (Inativo) e Drag & Drop ---
        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
        DeckDragHandler dragHandler = go.GetComponent<DeckDragHandler>();
        if (dragHandler == null) dragHandler = go.AddComponent<DeckDragHandler>();

        bool isInteractable = availableCopies > 0;
        canvasGroup.alpha = isInteractable ? 1.0f : 0.5f;
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;

        if (dragHandler != null)
        {
            dragHandler.cardData = card;
            dragHandler.sourceZone = DeckZoneType.Trunk;
            dragHandler.enabled = isInteractable;
        }

        // --- 5. Adiciona listener para o CardViewer (Hover) ---
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { OnCardHover(card); });
        trigger.triggers.Add(entry);
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
    private void CreateNewBanner(Transform parent)
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
}
public enum DeckZoneType { Trunk, Main, Side, Extra }
