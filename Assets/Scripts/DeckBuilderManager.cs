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
        // ... (Banlist data remains unchanged)
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
        // Adiciona listeners para os botões de ação principais
        if (btnSave) btnSave.onClick.AddListener(SaveDeck);
        if (btnExit) btnExit.onClick.AddListener(Exit);

        // Outros listeners podem estar configurados no Inspector ou em outros scripts
    }

    void OnEnable()
    {
        LoadCurrentDeckFromManager();
        LoadTrunk();
        currentPage = 1;
        hasUnsavedChanges = false;
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

        RefreshDeckZone(mainDeckContent, mainDeck, DeckZoneType.Main);
        RefreshDeckZone(sideDeckContent, sideDeck, DeckZoneType.Side);
        RefreshDeckZone(extraDeckContent, extraDeck, DeckZoneType.Extra);
        RefreshTrunkUI();
        UpdateCounts();
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

        foreach (Transform child in trunkContent)
        {
            Destroy(child.gameObject);
        }

        string searchText = searchInput != null ? searchInput.text.ToLowerInvariant() : "";

        var filteredGroups = currentTrunk
            .GroupBy(c => c.id)
            .Where(g =>
            {
                CardData card = g.First();
                if (!string.IsNullOrEmpty(searchText) && !card.name.ToLowerInvariant().Contains(searchText)) return false;
                
                bool isMonster = card.type.Contains("Monster");
                bool isSpell = card.type.Contains("Spell");
                bool isTrap = card.type.Contains("Trap");

                if (isSpell && !activeFilters["Spell"]) return false;
                if (isTrap && !activeFilters["Trap"]) return false;

                if (isMonster)
                {
                    bool isFusion = card.type.Contains("Fusion");
                    bool isRitual = card.type.Contains("Ritual");
                    bool isNormal = card.type.Contains("Normal");
                    bool isEffect = !isNormal && !isFusion && !isRitual; // Assume que se não for Normal/Fusion/Ritual, é de Efeito

                    if (isFusion && !activeFilters["Fusion"]) return false;
                    if (isRitual && !activeFilters["Ritual"]) return false;
                    if (isNormal && !activeFilters["Normal"]) return false;
                    if (isEffect && !activeFilters["Effect"]) return false;
                }
                return true;
            }).ToList();

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

        totalPages = Mathf.CeilToInt((float)filteredGroups.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

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

        foreach (var group in paginatedGroups)
        {
            CardData card = group.First();
            int totalOwned = group.Count();
            int copiesInDecks = deckCardCounts.GetValueOrDefault(card.id, 0);

            GameObject go = Instantiate(cardChestItemPrefab, trunkContent);
            SetupChestItem(go, card, totalOwned, copiesInDecks);
        }
    }
    
    void SetupChestItem(GameObject go, CardData card, int totalOwned, int copiesInDecks)
    {
        // --- 1. Encontra os componentes na hierarquia do prefab ---
        Transform card2DTr = go.transform.Find("Card2D");

        CardDisplay display = card2DTr?.GetComponent<CardDisplay>();

        if (display != null)
        {
            display.SetCard(card, GameManager.Instance?.GetCardBackTexture(), true);
            display.isInteractable = false;
        }

        // --- 2. Preenche os textos e ícones ---
        var cardNameText = go.transform.Find("CardNameText")?.GetComponent<TextMeshProUGUI>();
        var cardStatsText = go.transform.Find("CardStatsText")?.GetComponent<TextMeshProUGUI>();
        var monsterLvlText = go.transform.Find("MonsterLvl")?.GetComponent<TextMeshProUGUI>();
        var attributeImage = go.transform.Find("AttributeIcon")?.GetComponent<Image>();
        var typeImage = go.transform.Find("TypeIcon")?.GetComponent<Image>();
        var subTypeImage = go.transform.Find("SubTypeIcon")?.GetComponent<Image>();
        var raceImage = go.transform.Find("RaceIcon")?.GetComponent<Image>();

        int availableCopies = totalOwned - copiesInDecks;

        if (cardNameText) cardNameText.text = card.name;
        if (cardStatsText) cardStatsText.text = $"x{availableCopies}";

        bool isMonster = card.type.Contains("Monster");

        if (isMonster)
        {
            // LÓGICA PARA MONSTROS
            if (monsterLvlText) monsterLvlText.gameObject.SetActive(true);
            if (monsterLvlText) monsterLvlText.text = card.level.ToString();
            
            // Atributo (AttributeIcon)
            if (attributeImage)
            {
                attributeImage.gameObject.SetActive(true);
                var attributeMapping = attributeIcons.Find(i => i.name.Equals(card.attribute, System.StringComparison.OrdinalIgnoreCase));
                if (attributeMapping != null && attributeMapping.icon != null)
                {
                    attributeImage.sprite = attributeMapping.icon;
                    attributeImage.enabled = true;
                }
                else
                {
                    attributeImage.enabled = false;
                    Debug.LogWarning($"[ICONS] Ícone de ATRIBUTO não encontrado para '{card.attribute}' na carta '{card.name}'. Verifique a lista 'attributeIcons'.");
                }
            }
            
            // Raça (RaceIcon)
            if (raceImage) // Usar RaceIcon para a raça do monstro
            {
                raceImage.gameObject.SetActive(true);
                var raceMapping = raceIcons.Find(i => i.name.Equals(card.race, System.StringComparison.OrdinalIgnoreCase));
                if (raceMapping != null && raceMapping.icon != null)
                {
                    raceImage.sprite = raceMapping.icon;
                    raceImage.enabled = true;
                }
                else
                {
                    raceImage.enabled = false;
                    Debug.LogWarning($"[ICONS] Ícone de RAÇA não encontrado para '{card.race}' na carta '{card.name}'. Verifique a lista 'raceIcons'.");
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
                var subTypeMapping = subTypeIcons.Find(i => i.name.Equals(card.property, System.StringComparison.OrdinalIgnoreCase));
                if (subTypeMapping != null && subTypeMapping.icon != null)
                {
                    subTypeImage.sprite = subTypeMapping.icon;
                    subTypeImage.enabled = true;
                }
                else
                {
                    subTypeImage.enabled = false;
                    if (!string.IsNullOrEmpty(card.property) && card.property != "Normal" && card.property != "N/A")
                    {
                        Debug.LogWarning($"[ICONS] Ícone de SUBTIPO (Property) não encontrado para '{card.property}' na carta '{card.name}'. Verifique a lista 'subTypeIcons'.");
                    }
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
        Transform newTagParent = go.transform.Find("Card2D");
        if (isNew && !isInDeck && newTagParent != null)
        {
            CreateNewBanner(newTagParent);
        }

        // --- 4. Lógica de Estado (Inativo) ---
        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
        DeckDragHandler dragHandler = go.GetComponent<DeckDragHandler>();

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

        // --- 5. Adiciona listener para o CardViewer ---
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { OnCardHover(card); });
        trigger.triggers.Add(entry);
    }

    public void SaveDeck()
    {
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
    }

    public void Exit()
    {
        if (hasUnsavedChanges)
        {
            UIManager.Instance?.ShowConfirmation(
                "Você tem alterações não salvas. Deseja sair sem salvar?",
                () => UIManager.Instance.ShowScreen(UIManager.Instance.newGameMenu) // Volta para o menu anterior sem salvar
            );
        }
        else
        {
            UIManager.Instance?.ShowScreen(UIManager.Instance.newGameMenu);
        }
    }

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
    
    public void OnCardHover(CardData card)
    {
        if (cardViewer != null)
        {
            cardViewer.DisplayCardData(card);
        }
    }
    
    public bool AddCardToDeck(CardData card, DeckZoneType targetZone)
    {
       // ... Lógica de adicionar carta ...
       hasUnsavedChanges = true;
       RefreshAllUI(); // Atualiza toda a UI para refletir a mudança
       return true; // Retorna true se foi bem sucedido
    }

    public void RemoveCard(CardData card, DeckZoneType sourceZone)
    {
        // ... Lógica de remover carta ...
        hasUnsavedChanges = true;
        RefreshAllUI(); // Atualiza toda a UI para refletir a mudança
    }

    public void TriggerInvalidMoveFeedback(DeckZoneType zone)
    {
        // Lógica para piscar o painel da zona inválida
    }

    private void UpdateCounts()
    {
        if(mainDeckCountText) mainDeckCountText.text = $"{mainDeck.Count}/{MAX_MAIN}";
        if(sideDeckCountText) sideDeckCountText.text = $"{sideDeck.Count}/{MAX_SIDE}";
        if(extraDeckCountText) extraDeckCountText.text = $"{extraDeck.Count}/{MAX_EXTRA}";
    }

    private void UpdatePaginationUI()
    {
        if (txtPageInfo != null)
        {
            txtPageInfo.text = $"Page {currentPage} / {totalPages}";
        }
        if (btnPrevPage != null) btnPrevPage.interactable = currentPage > 1;
        if (btnNextPage != null) btnNextPage.interactable = currentPage < totalPages;
    }
    
    // ... O resto dos métodos como SaveDeck, filtros, etc. permanecem ...
}
public enum DeckZoneType { Trunk, Main, Side, Extra }
