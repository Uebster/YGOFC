using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CardLibraryManager : MonoBehaviour
{
    [Header("Referências")]
    public Transform gridContent; // O objeto 'Content' dentro do ScrollView
    public GameObject cardItemPrefab; // O mesmo prefab usado no DeckBuilder (pequeno)
    public CardViewerUI cardViewer; // O visualizador grande à esquerda

    [Header("Paginação")]
    public TextMeshProUGUI pageText; // Texto "Page 1/21"
    public Button prevButton;
    public Button nextButton;
    public int itemsPerPage = 50;

    [Header("Info")]
    public TextMeshProUGUI cardCountText; // Ex: "40/2147"

    [Header("Customização")]
    [Tooltip("Prefab opcional para a tag 'NEW'. Se vazio, será gerado via código.")]
    public GameObject newTagPrefab;
    [Tooltip("Cor da faixa da tag 'NEW' dinâmica.")]
    public Color newTagBannerColor = new Color(0, 0, 0, 0.7f);
    [Tooltip("Cor do texto da tag 'NEW' dinâmica.")]
    public Color newTagTextColor = Color.white;

    private List<CardData> allCards = new List<CardData>();
    private int currentPage = 1;
    private int totalPages = 1;
    
    private List<GameObject> pooledItems = new List<GameObject>();
    private Dictionary<GameObject, GameObject> pooledNewTags = new Dictionary<GameObject, GameObject>();
    private HashSet<string> ownedIDsCache = new HashSet<string>();

    void Start()
    {
        if (cardViewer == null) cardViewer = GetComponentInChildren<CardViewerUI>();
        
        // FIX: Garante que o Content tenha ContentSizeFitter para o scroll funcionar direito
        if (gridContent != null)
        {
            ContentSizeFitter fitter = gridContent.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gridContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // FIX: Desabilita o arrasto manual (Scroll) já que usamos paginação por botões
        ScrollRect scroll = GetComponentInChildren<ScrollRect>();
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = false;
            // Limpa referências caso o objeto da barra de rolagem tenha sido deletado
            scroll.verticalScrollbar = null;
            scroll.horizontalScrollbar = null;
        }

        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);
    }

    void OnEnable()
    {
        LoadLibrary();
    }

    public void LoadLibrary()
    {
        if (GameManager.Instance == null || GameManager.Instance.cardDatabase == null) return;
        
        allCards.Clear();
        
        // Carrega TODAS as cartas do banco de dados para mostrar os slots vazios (verso)
        allCards.AddRange(GameManager.Instance.cardDatabase.cardDatabase);
        
        // Cacheia as cartas possuídas uma única vez para performance
        ownedIDsCache = new HashSet<string>(GameManager.Instance.playerTrunk);

        // Calcula total de páginas
        totalPages = Mathf.CeilToInt((float)allCards.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        
        // Atualiza contador de coleção (Únicas obtidas / Total no DB)
        if (cardCountText != null)
        {
            int ownedCount = 0;
            foreach (var card in allCards)
            {
                if (ownedIDsCache.Contains(card.id)) ownedCount++;
            }
            cardCountText.text = $"{ownedCount}/{allCards.Count}";
        }

        InitializePool();
        currentPage = 1;
        UpdatePage();
    }

    void InitializePool()
    {
        if (gridContent == null || cardItemPrefab == null) return;
        
        // Cria exatamente a quantidade de itens por página uma única vez
        if (pooledItems.Count == 0)
        {
            for (int i = 0; i < itemsPerPage; i++)
            {
                GameObject go = Instantiate(cardItemPrefab, gridContent);
                CardDisplay display = go.GetComponent<CardDisplay>();
                if (display == null) display = go.AddComponent<CardDisplay>();
                
                LibraryCardInteraction interaction = go.AddComponent<LibraryCardInteraction>();
                interaction.manager = this;
                
                display.isInteractable = false; // Desativa comportamento de mão
                display.isPlayerCard = true;    // Cor de outline do jogador
                
                go.SetActive(false);
                pooledItems.Add(go);
            }
        }
    }

    void UpdatePage()
    {
        int startIndex = (currentPage - 1) * itemsPerPage;
        Texture2D backTexture = GameManager.Instance.GetCardBackTexture();

        for (int i = 0; i < itemsPerPage; i++)
        {
            GameObject go = pooledItems[i];
            
            if (startIndex + i < allCards.Count)
            {
                go.SetActive(true);
                CardData card = allCards[startIndex + i];
                CardDisplay display = go.GetComponent<CardDisplay>();
                LibraryCardInteraction interaction = go.GetComponent<LibraryCardInteraction>();
                
                bool isOwned = ownedIDsCache.Contains(card.id);
                interaction.myCard = card;
                interaction.isOwned = isOwned;

                if (isOwned)
                {
                    display.SetCard(card, backTexture, true);
                    bool isNew = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id);
                    ToggleNewBanner(go, isNew);
                }
                else
                {
                    display.SetCardBackOnly(backTexture);
                    ToggleNewBanner(go, false);
                }
            }
            else
            {
                go.SetActive(false);
                ToggleNewBanner(go, false);
            }
        }

        // Atualiza UI
        if (pageText) pageText.text = $"Page {currentPage}/{totalPages}";
        if (prevButton) prevButton.interactable = currentPage > 1;
        if (nextButton) nextButton.interactable = currentPage < totalPages;
    }

    void ToggleNewBanner(GameObject parentCard, bool show)
    {
        if (!pooledNewTags.ContainsKey(parentCard))
        {
            if (!show) return; // Se não precisa mostrar e não existe, nem cria.
            pooledNewTags[parentCard] = CreateNewBannerObject(parentCard.transform);
        }

        pooledNewTags[parentCard].SetActive(show);
        if (show) pooledNewTags[parentCard].transform.SetAsLastSibling();
    }

    GameObject CreateNewBannerObject(Transform parent)
    {
        if (newTagPrefab != null)
        {
            // Cria um container para o prefab
            GameObject bannerContainer = new GameObject("NewTagContainer", typeof(RectTransform));
            bannerContainer.transform.SetParent(parent, false);
            
            // Configura o RectTransform do container para ser uma faixa central
            RectTransform containerRect = bannerContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f); // Estica na largura
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f); // Centraliza
            containerRect.anchoredPosition = Vector2.zero; // Posição zero no centro
            containerRect.sizeDelta = new Vector2(0, 36); // Largura 0 (para esticar), altura inicial de 36

            // Adiciona o AspectRatioFitter para manter a proporção
            AspectRatioFitter fitter = bannerContainer.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 120f / 36f; // Proporção do prefab original (120x36)

            // Instancia o prefab do usuário DENTRO do container
            GameObject bannerInstance = Instantiate(newTagPrefab, bannerContainer.transform);
            bannerInstance.name = "NewTag_Custom_Instance";
            bannerInstance.SetActive(true);

            // Força o prefab a preencher o container que agora tem a proporção correta
            RectTransform bannerRect = bannerInstance.GetComponent<RectTransform>();
            if (bannerRect != null)
            {
                bannerRect.anchorMin = Vector2.zero;
                bannerRect.anchorMax = Vector2.one;
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.sizeDelta = Vector2.zero;
                bannerRect.anchoredPosition = Vector2.zero;
            }
            
            return bannerContainer;
        }
        else
        {
            // Lógica antiga para o banner dinâmico (fallback)
            GameObject bannerObj = new GameObject("NewTag_Dynamic", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(parent, false);
            // ... (Você pode adicionar a montagem do texto dinâmico aqui se não usar o Prefab) ...
            return bannerObj;
        }
    }

    public void NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePage();
        }
    }

    public void OnCardHover(CardData card, bool isOwned)
    {
        // Debug.Log($"Library Hover: {card?.name} (Owned: {isOwned})");
        if (cardViewer != null)
        {
            if (isOwned)
            {
                cardViewer.DisplayCardData(card);
            }
            else
            {
                // Se não tem a carta, limpa o viewer
                cardViewer.DisplayCardData(null); 
            }
        }
    }
}

// Pequena classe auxiliar para detectar o mouse na biblioteca
public class LibraryCardInteraction : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    public CardLibraryManager manager;
    public CardData myCard;
    public bool isOwned;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (manager != null) manager.OnCardHover(myCard, isOwned);
    }
}