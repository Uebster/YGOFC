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
    public TrunkScrollManager trunkScrollManager;

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
    
    [Header("UI References - Actions")]
    public Button btnSave;
    public Button btnExit;
    public Button btnImport;
    public Button btnExport;
    public GameObject importExportPanel; // O painel que contém os painéis de Import e Export
    
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
    private Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();
    private List<IGrouping<string, CardData>> filteredCardGroups = new List<IGrouping<string, CardData>>();

    private Coroutine searchDebounceCoroutine;

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
        InitializeIconDictionaries();
    }

    void Start()
    {
        // Esconde o painel de import/export no início
        if (importExportPanel != null)
            importExportPanel.SetActive(false);

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

        // Pesquisa
        searchInput?.onValueChanged.AddListener(OnSearchInputChanged);

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

    void SetupDropZones()
    {
        // Adiciona DeckDropZone aos containers se não tiverem
        AddDropZoneIfNeeded(mainDeckContent, DeckZoneType.Main);
        AddDropZoneIfNeeded(sideDeckContent, DeckZoneType.Side);
        AddDropZoneIfNeeded(extraDeckContent, DeckZoneType.Extra);
        if (trunkScrollManager != null) AddDropZoneIfNeeded(trunkScrollManager.GetComponent<ScrollRect>().content, DeckZoneType.Trunk);
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
    // Erro CS0103 corrigido aqui
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
    Debug.Log("[DeckBuilderManager] LoadData: Processo de carregamento de dados finalizado.");
}

    void LoadCurrentDeckFromManager()
    {
        if (GameManager.Instance == null) return;
        Debug.Log("[DeckBuilderManager] LoadCurrentDeckFromManager: Carregando decks do GameManager.");
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
        
        Debug.Log($"[DeckBuilderManager] LoadTrunk: Carregou {currentTrunk.Count} cartas do banco de dados para o baú.");
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
        Debug.Log("[DeckBuilderManager] RefreshTrunkUI: Iniciando atualização da UI do baú.");
        // DEBUG: Verifique se o baú tem cartas antes de filtrar.

        string searchText = searchInput != null ? searchInput.text.ToLowerInvariant() : "";
        bool anyFilterActive = activeFilters.Any(kvp => kvp.Value);

        filteredCardGroups = currentTrunk
            .GroupBy(c => c.id) // Agrupa por ID para tratar cópias
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
                
                string cardType = card.type.ToLowerInvariant();
                if (activeFilters["Normal"] && cardType.Contains("normal") && cardType.Contains("monster")) return true;
                if (activeFilters["Effect"] && cardType.Contains("effect") && cardType.Contains("monster")) return true;
                if (activeFilters["Ritual"] && cardType.Contains("ritual")) return true;
                if (activeFilters["Fusion"] && cardType.Contains("fusion")) return true;
                if (activeFilters["Spell"] && cardType.Contains("spell")) return true;
                if (activeFilters["Trap"] && cardType.Contains("trap")) return true;

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
        Debug.Log($"[DeckBuilderManager] RefreshTrunkUI: Encontrados {filteredCardGroups.Count} grupos de cartas após filtros e ordenação.");

        if (trunkScrollManager != null)
        {
            Debug.Log("[DeckBuilderManager] RefreshTrunkUI: Chamando trunkScrollManager.Initialize().");
            trunkScrollManager.Initialize(filteredCardGroups);
        }
        else
        {
            Debug.LogError("TrunkScrollManager não está atribuído no DeckBuilderManager!");
        }
    }

    // --- MÉTODOS PÚBLICOS PARA O SCROLL MANAGER ---
    public IGrouping<string, CardData> GetFilteredCardGroup(int index)
    {
        if (index < 0 || index >= filteredCardGroups.Count)
            return null;
        return filteredCardGroups[index];
    }

    public int GetCardLimit(string cardName)
    {
        if (GameManager.Instance != null && GameManager.Instance.disableBanlist)
            return MAX_COPIES;

        if (banList.TryGetValue(cardName, out int limit))
            return limit;

        return MAX_COPIES;
    }

    public int GetCopiesInDecks(string cardId)
    {
        int count = 0;
        count += mainDeck.Count(c => c.id == cardId);
        count += sideDeck.Count(c => c.id == cardId);
        count += extraDeck.Count(c => c.id == cardId);
        return count;
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

        hasUnsavedChanges = false;
        Debug.Log("Deck salvo com sucesso!");
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
        // Correção para o erro de Coroutine: Só atualiza o visualizador se o painel dele estiver ativo
        // e se não estivermos no meio de uma resolução de chain (evita pop-ups indesejados).
        if (cardViewer != null && cardViewer.gameObject.activeInHierarchy)
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
       int currentCopies = GetCopiesInDecks(card.id);
       int limit = GetCardLimit(card.name);

       // Permite 1 cópia de cartas proibidas se a opção estiver ativa
       if (GameManager.Instance != null && GameManager.Instance.allowForbiddenCards && limit == 0) limit = 1;

       if (currentCopies >= limit) return false;

       bool isExtra = card.type.ToLowerInvariant().Contains("fusion") || card.type.ToLowerInvariant().Contains("synchro") || card.type.ToLowerInvariant().Contains("xyz");

       // Validação de Zona
       if (targetZone == DeckZoneType.Main)
       {
           if (isExtra) return false; // Extra Deck cards não vão no Main Deck
           if (mainDeck.Count >= MAX_MAIN) return false;
           mainDeck.Add(card);
       }
       else if (targetZone == DeckZoneType.Side)
       {
           if (isExtra) return false;
           if (sideDeck.Count >= MAX_SIDE) return false;
           sideDeck.Add(card);
       }
       else if (targetZone == DeckZoneType.Extra)
       {
           if (!isExtra) return false; // Apenas Fusões/Synchro/Xyz no Extra Deck
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
       // ATENÇÃO: A atualização da UI agora é responsabilidade do chamador (ex: DeckDropZone)
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
            // ATENÇÃO: A atualização da UI agora é responsabilidade do chamador (ex: DeckDropZone)
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
        Debug.Log($"[DeckBuilderManager] Deck '{deckName}' exportado para o save.");
    }

    public void ImportDeck(string deckName)
    {
        if (SaveLoadSystem.Instance == null) return;

        if (SaveLoadSystem.Instance.LoadDeckFromRecipe(deckName, out var mainIDs, out var sideIDs, out var extraIDs))
        {
            mainDeck.Clear();
            sideDeck.Clear();
            extraDeck.Clear();

            // Carrega apenas cartas que o jogador possui
            foreach (string id in mainIDs) { CardData c = GameManager.Instance.cardDatabase.GetCardById(id); if (c != null) mainDeck.Add(c); }
            foreach (string id in sideIDs) { CardData c = GameManager.Instance.cardDatabase.GetCardById(id); if (c != null) sideDeck.Add(c); }
            foreach (string id in extraIDs) { CardData c = GameManager.Instance.cardDatabase.GetCardById(id); if (c != null) extraDeck.Add(c); }

            hasUnsavedChanges = true;
            RefreshAllUI();
            Debug.Log($"Deck '{deckName}' carregado.");

            // Adiciona um aviso se o deck importado for inválido
            if (!IsDeckValid())
            {
                UIManager.Instance?.ShowMessage($"Atenção: O deck importado '{deckName}' é inválido (Principal: {mainDeck.Count} cartas). Ajuste-o para poder salvá-lo.");
            }
        }
    }

    public void DeleteDeckRecipe(string deckName)
    {
        if (string.IsNullOrWhiteSpace(deckName)) return;

        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteDeckRecipe(deckName);
            SaveLoadSystem.Instance.SaveGame(GameManager.Instance.currentSaveID); // Salva a deleção
            Debug.Log($"Receita de deck deletada: {deckName}");
        }
    }

    /// <summary>
    /// Atualiza os textos de contagem de cartas para todos os decks.
    /// </summary>

    // --- SISTEMA DE IMPORTAÇÃO/EXPORTAÇÃO (INTERNO) ---

    private void OnImportButtonClicked()
    {
        if (importExportPanel == null) return;
        importExportPanel.SetActive(true);
        DeckImportExportManager manager = importExportPanel.GetComponent<DeckImportExportManager>();
        if (manager != null)
        {
            manager.Setup(DeckImportExportManager.MenuType.Import);
        }
    }

    private void OnExportButtonClicked()
    {
        if (importExportPanel == null) return;
        importExportPanel.SetActive(true);
        DeckImportExportManager manager = importExportPanel.GetComponent<DeckImportExportManager>();
        if (manager != null)
        {
            manager.Setup(DeckImportExportManager.MenuType.Export);
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
            bool wasActive = activeFilters[filterKey];
            
            // Desativa todos os filtros para manter a exclusividade mútua (apenas 1 por vez)
            var keys = new List<string>(activeFilters.Keys);
            foreach (var key in keys)
            {
                activeFilters[key] = false;
            }

            activeFilters[filterKey] = !wasActive; // Alterna apenas o clicado
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
