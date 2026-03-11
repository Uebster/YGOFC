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

        // Calcula total de páginas
        totalPages = Mathf.CeilToInt((float)allCards.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;
        
        // Atualiza contador de coleção (Únicas obtidas / Total no DB)
        if (cardCountText != null)
        {
            HashSet<string> ownedIDs = new HashSet<string>(GameManager.Instance.playerTrunk);
            int ownedCount = 0;
            foreach (var card in allCards)
            {
                if (ownedIDs.Contains(card.id)) ownedCount++;
            }
            cardCountText.text = $"{ownedCount}/{allCards.Count}";
        }

        currentPage = 1;
        UpdatePage();
    }

    void UpdatePage()
    {
        if (gridContent == null || cardItemPrefab == null) return;

        // Limpa grid atual
        foreach (Transform child in gridContent) Destroy(child.gameObject);

        int startIndex = (currentPage - 1) * itemsPerPage;
        int count = Mathf.Min(itemsPerPage, allCards.Count - startIndex);

        // Cache do Trunk para performance (HashSet é mais rápido para Contains)
        HashSet<string> ownedIDs = new HashSet<string>(GameManager.Instance.playerTrunk);
        Texture2D backTexture = GameManager.Instance.GetCardBackTexture();

        for (int i = 0; i < count; i++)
        {
            CardData card = allCards[startIndex + i];
            GameObject go = Instantiate(cardItemPrefab, gridContent);
            
            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display == null) display = go.AddComponent<CardDisplay>();
            
            bool isOwned = ownedIDs.Contains(card.id);

            if (isOwned)
            {
                // Se possui, mostra a frente
                display.SetCard(card, backTexture, true);
                
                // Verifica se é "New"
                if (SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsCardNew(card.id))
                {
                    CreateNewBanner(go.transform);
                }
            }
            else
            {
                // Se não possui, mostra apenas o verso (bloqueada)
                display.SetCardBackOnly(backTexture);
            }

            display.isInteractable = false; // Desativa o efeito de "subir" (comportamento de mão)
            display.isPlayerCard = true;    // Força o uso da cor de outline do jogador

            // Adiciona interatividade para o Viewer
            LibraryCardInteraction interaction = go.AddComponent<LibraryCardInteraction>();
            interaction.manager = this;
            interaction.myCard = card;
            interaction.isOwned = isOwned; // Passa o estado para saber se pode ver detalhes
        }

        // Atualiza UI
        if (pageText) pageText.text = $"Page {currentPage}/{totalPages}";
        if (prevButton) prevButton.interactable = currentPage > 1;
        if (nextButton) nextButton.interactable = currentPage < totalPages;
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
            
            bannerContainer.transform.SetAsLastSibling();
        }
        else
        {
            // Lógica antiga para o banner dinâmico (fallback)
            GameObject bannerObj = new GameObject("NewTag_Dynamic", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(parent, false);
            // ... (código do banner dinâmico continua aqui) ...
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