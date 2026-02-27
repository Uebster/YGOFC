// Assets/Scripts/CardDisplay.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    public RawImage cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    [Header("Visual Effects")]
    public Image outlineImage; // Arraste uma imagem de borda (filha) aqui
    [Tooltip("Se marcado, usa o componente Outline do Unity para criar uma borda simples (senão, usa a imagem 'outlineImage').")]
    public bool useSimpleOutline = true;
    public bool enableHoverOutline = true;
    public Color hoverColor = Color.yellow;
    public Color tributeColor = new Color(0f, 0.6f, 1f); // Azul ciano brilhante
    public Color attackColor = Color.red;

    private CardData currentCardData;
    private Texture2D frontTexture;
    private Texture2D backTexture;
    private bool isFlipped = false;
    private RectTransform rectTransform;
    
    // Componentes para corrigir a renderização e tremedeira
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Vector3 originalScale = Vector3.one;

    [HideInInspector] public float hoverYOffset = 30f;
    [HideInInspector] public bool isInteractable = false; // Usado para habilitar hover apenas para cartas na mão
    [HideInInspector] public bool isPlayerCard = false; // Define se a carta pertence ao jogador (para visualização)
    [HideInInspector] public bool isOnField = false; // Define se a carta está no campo
    [HideInInspector] public bool isInPile = false; // Define se a carta está em uma pilha (Deck, GY, Extra)
    private UnityWebRequest currentRequest; // Rastreia a requisição ativa para descarte correto

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Se esta for a instância do CardViewer, tenta encontrar os textos automaticamente
        if (GameManager.Instance != null && GameManager.Instance.cardViewerDisplay == this)
        {
            if (transform.parent != null)
            {
                var allTexts = transform.parent.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach(var t in allTexts)
                {
                    if (t.name.Contains("Name")) cardNameText = t;
                    if (t.name.Contains("Info")) cardInfoText = t;
                    if (t.name.Contains("Description")) cardDescriptionText = t;
                    if (t.name.Contains("Stats")) cardStatsText = t;
                }
            }
        }

        // AUTO-FIX: Se cardImage não estiver atribuído, tenta encontrar automaticamente
        if (cardImage == null)
        {
            // 1. Tenta achar o filho "Art" (Padrão novo)
            Transform art = transform.Find("Art");
            if (art != null) 
            {
                cardImage = art.GetComponent<RawImage>();
                // FIX EXTRA: Se o objeto "Art" existe mas está sem o componente RawImage, adiciona agora
                if (cardImage == null) cardImage = art.gameObject.AddComponent<RawImage>();
            }

            // 2. Se não achou, tenta pegar do próprio objeto (Padrão antigo) ou qualquer filho
            if (cardImage == null) cardImage = GetComponentInChildren<RawImage>();
        }

        // FIX: Garante que a imagem da carta não bloqueie o mouse (para o Hover no pai funcionar)
        if (cardImage != null)
            cardImage.raycastTarget = false;
        
        // FIX: Configura a Borda para esticar na carta toda e não bloquear cliques
        if (outlineImage != null)
        {
            outlineImage.raycastTarget = false; // Importante: Ignora cliques
            // FIX: Coloca a borda atrás de tudo (primeiro filho) para não cobrir a carta
            outlineImage.transform.SetAsFirstSibling();
            outlineImage.rectTransform.anchorMin = Vector2.zero;
            outlineImage.rectTransform.anchorMax = Vector2.one;
            // O tamanho será controlado pelo layout ou sprite
            
            outlineImage.gameObject.SetActive(false);
        }

        // FIX: Adiciona Canvas para controlar a ordem de desenho sem quebrar o Layout (evita tremedeira)
        canvas = gameObject.AddComponent<Canvas>();
        graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        // Tenta aplicar máscara de arredondamento se houver sprite configurado no GameManager
        ApplyRoundedCorners();
    }

    void ApplyRoundedCorners()
    {
        if (GameManager.Instance == null || cardImage == null) return;

        bool useRounded = false;
        if (GameManager.Instance.cardViewerDisplay == this)
        {
            useRounded = GameManager.Instance.useCardViewerRounded;
        }
        else if (isOnField || isInPile)
        {
            useRounded = GameManager.Instance.useFieldCardsRounded; // Piles use field settings
        }
        else // Hand
        {
            useRounded = GameManager.Instance.useHandCardsRounded;
        }

        Mask mask = GetComponent<Mask>();
        // Se não tiver máscara e precisarmos arredondar, adiciona. Se não precisar, não adiciona à toa.
        if (mask == null && useRounded) mask = gameObject.AddComponent<Mask>();
        
        Image parentImage = GetComponent<Image>();
        if (parentImage == null && useRounded) parentImage = gameObject.AddComponent<Image>();

        if (useRounded && GameManager.Instance.cardMaskSprite != null)
        {
            if (parentImage != null)
            {
                parentImage.sprite = GameManager.Instance.cardMaskSprite;
                parentImage.enabled = true;
            }
            if (mask != null)
            {
                // A máscara DEVE ser visível para que o Outline tenha onde se desenhar
                mask.showMaskGraphic = true; 
                mask.enabled = true;
            }
        }
        else
        {
            if (mask != null) mask.enabled = false;
            if (parentImage != null) parentImage.enabled = false;
        }
    }

    // Este método será chamado pelo GameManager para definir os dados da carta
    public void SetCard(CardData card, Texture2D cardBackTexture, bool startFaceUp = true)
    {
        if (card == null) return;
        
        // Limpa requisição anterior se ainda estiver rodando
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        StopAllCoroutines(); // Para carregamentos anteriores
        // Limpa textura anterior para liberar memória
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }

        currentCardData = card;
        backTexture = cardBackTexture;
        isFlipped = !startFaceUp; // Se startFaceUp for false, isFlipped será true (verso)
        originalScale = transform.localScale; // Salva a escala inicial definida pelo GameManager
        
        DisplayCardDetails();
        
        // Se começar virada, já aplica o verso imediatamente
        if (isFlipped && cardImage != null && backTexture != null)
        {
            cardImage.texture = backTexture;
        }

        // FIX: Verifica se o objeto está ativo antes de iniciar a corrotina para evitar erro
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(LoadCardFrontTexture(card.image_filename));
        }
        ApplyRoundedCorners();
    }

    public void SetCardBackOnly(Texture2D cardBackTexture)
    {
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        StopAllCoroutines();
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }

        backTexture = cardBackTexture;
        if (cardImage != null && backTexture != null)
        {
            cardImage.texture = backTexture;
        }
        // Limpa os textos
        if (cardNameText != null) cardNameText.text = "";
        if (cardInfoText != null) cardInfoText.text = "";
        if (cardDescriptionText != null) cardDescriptionText.text = "";
        if (cardStatsText != null) cardStatsText.text = "";
    }

    private void DisplayCardDetails()
    {
        if (currentCardData == null) return;

        if (cardNameText != null) cardNameText.text = currentCardData.name;
        if (cardDescriptionText != null) cardDescriptionText.text = currentCardData.description;

        string info = $"[{currentCardData.type}]";
        if (!string.IsNullOrEmpty(currentCardData.race)) info += $" / {currentCardData.race}";
        if (currentCardData.level > 0) info += $" / LV: {currentCardData.level}";
        if (cardInfoText != null) cardInfoText.text = info;

        if (cardStatsText != null)
        {
            if (currentCardData.type.Contains("Monster"))
                cardStatsText.text = $"ATK/ {currentCardData.atk}  DEF/ {currentCardData.def}";
            else
                cardStatsText.text = "";
        }
    }

    IEnumerator LoadCardFrontTexture(string imagePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);
        
        // FIX: Usa System.Uri para escapar caracteres especiais (como #) corretamente no caminho
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        // Não usamos 'using' aqui para poder descartar manualmente se a corrotina for interrompida
        currentRequest = UnityWebRequestTexture.GetTexture(url);
        
        yield return currentRequest.SendWebRequest();

        if (currentRequest.result == UnityWebRequest.Result.Success)
        {
            frontTexture = DownloadHandlerTexture.GetContent(currentRequest);
            frontTexture.filterMode = FilterMode.Trilinear;
            if (cardImage != null)
            {
                // Só aplica a textura da frente se a carta NÃO estiver virada (isFlipped == false)
                if (!isFlipped) cardImage.texture = frontTexture;
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar imagem da frente da carta: {fullPath} | Erro: {currentRequest.error}");
        }

        currentRequest.Dispose();
        currentRequest = null;
    }

    public void FlipCard()
    {
        if (cardImage == null || frontTexture == null || backTexture == null) return;

        isFlipped = !isFlipped;
        cardImage.texture = isFlipped ? backTexture : frontTexture;
    }

    public void ShowFront()
    {
        if (cardImage == null || frontTexture == null) return;
        isFlipped = false;
        cardImage.texture = frontTexture;
    }

    public void ShowBack()
    {
        if (cardImage == null || backTexture == null) return;
        isFlipped = true;
        cardImage.texture = backTexture;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool shouldShowOutline = enableHoverOutline;
        if (GameManager.Instance != null)
        {
            if (isOnField) shouldShowOutline = GameManager.Instance.enableFieldHoverOutline;
            else if (isInPile) shouldShowOutline = GameManager.Instance.enablePileHoverOutline;
            else if (isInteractable) shouldShowOutline = GameManager.Instance.enableHandHoverOutline; // Mão
            // Verifica se é o Card Viewer
            else if (GameManager.Instance.cardViewerDisplay == this) shouldShowOutline = GameManager.Instance.enableCardViewerOutline;
        }

        // --- Efeito de Borda (Hover) ---
        if (shouldShowOutline)
        {
            if (useSimpleOutline)
            {
                // Opção 1: Usa o componente Outline do Unity no PAI (gameObject)
                // Aplicar no pai garante que a máscara não corte o contorno externo
                Outline outline = GetComponent<Outline>();
                if (outline == null) outline = gameObject.AddComponent<Outline>();                
                
                outline.effectColor = hoverColor;
                outline.effectDistance = new Vector2(4, -4); // Espessura da borda
                // FIX: Usa o alpha do gráfico (sprite arredondado) para desenhar a borda
                outline.useGraphicAlpha = true; 
                outline.enabled = true;
            }
            else if (outlineImage != null)
            {
                // Opção 2: Usa a imagem separada (se useSimpleOutline for false)
                outlineImage.color = hoverColor;
                outlineImage.gameObject.SetActive(true);
            }
        }

        // --- Efeito de Subir (Apenas Mão) ---
        // Verifica se é interativo E se o hover está habilitado no GameManager
        if (isInteractable && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect)
        {
            // FIX: Usa Canvas Sorting para trazer para frente visualmente sem recalcular o Layout
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10; // Valor alto para ficar por cima de tudo
            
            // Move para cima (Y) mantendo a escala original
            rectTransform.anchoredPosition += new Vector2(0, hoverYOffset);
        }

        // Prioridade: Se o DeckBuilder estiver aberto, usa o visualizador dele
        if (DeckBuilderManager.Instance != null && DeckBuilderManager.Instance.gameObject.activeInHierarchy)
        {
            DeckBuilderManager.Instance.OnCardHover(currentCardData);
        }
        // Caso contrário, usa o visualizador do Duelo (GameManager)
        else if (GameManager.Instance != null)
        {
            // Evita que o visualizador atualize a si mesmo se o mouse passar por cima dele
            if (GameManager.Instance.cardViewerDisplay != null && this == GameManager.Instance.cardViewerDisplay) return;

            // Lógica para visualizar extra deck mesmo virado para baixo
            bool forceShowFaceUp = false;
            if (GameManager.Instance.playerExtraDeckDisplay != null && transform.parent == GameManager.Instance.playerExtraDeckDisplay.contentParent)
            {
                forceShowFaceUp = true;
            }

            // Se a carta estiver virada para baixo (isFlipped), mostra o verso, a menos que seja do jogador ou extra deck
            bool showFaceUp = !isFlipped || forceShowFaceUp || isPlayerCard;
            GameManager.Instance.UpdateCardViewer(currentCardData, showFaceUp);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // --- Remove Borda ---
        if (useSimpleOutline)
        {
            Outline outline = GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(false);
        }

        // --- Remove Efeito de Subir ---
        if (isInteractable && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect)
        {
            // FIX: Reseta o Canvas e a Escala
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            rectTransform.anchoredPosition -= new Vector2(0, hoverYOffset);
        }

        // FIX 4: NÃO limpamos o Card Viewer aqui para ele ficar "travado".
    }

    // Método para ativar o brilho de tributo (Luz Azul)
    public void SetTributeHighlight(bool active)
    {
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(active);
            outlineImage.color = tributeColor;
            // Dica: Você pode adicionar um componente de animação (ping-pong alpha) na imagem da borda para pulsar
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"CardDisplay: Clique detectado na carta {currentCardData?.name}");

        // Só abre o menu se a carta estiver na mão (isInteractable) e o menu existir
        if (isInteractable && DuelActionMenu.Instance != null)
        {
            DuelActionMenu.Instance.ShowMenu(gameObject, currentCardData);
        }
        else if (isInteractable && DuelActionMenu.Instance == null)
        {
            Debug.LogError("CardDisplay: DuelActionMenu.Instance não encontrado na cena!");
        }
    }

#if UNITY_EDITOR
    // Chamado automaticamente quando você altera algo no Inspector
    void OnValidate()
    {
        // Se a referência estiver vazia, tenta encontrar automaticamente um filho chamado "Art"
        if (cardImage == null)
        {
            Transform art = transform.Find("Art");
            if (art != null) cardImage = art.GetComponent<RawImage>();
        }
    }

    // Adiciona uma opção no menu de contexto do componente (Botão Direito -> Configurar Hierarquia)
    [ContextMenu("Configurar Hierarquia (Art)")]
    public void SetupHierarchy()
    {
        // 1. Verifica ou Cria o objeto filho "Art"
        Transform artTransform = transform.Find("Art");
        if (artTransform == null)
        {
            GameObject artObj = new GameObject("Art");
            artObj.transform.SetParent(transform, false);
            artTransform = artObj.transform;
            
            // Configura RectTransform para esticar (Stretch) em toda a carta
            RectTransform rt = artObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; // Zera offsets para preencher tudo
        }

        // 2. Garante que o filho tem o componente RawImage
        RawImage childRawImage = artTransform.GetComponent<RawImage>();
        if (childRawImage == null) childRawImage = artTransform.gameObject.AddComponent<RawImage>();
        childRawImage.raycastTarget = false; // IMPORTANTE: Desativa Raycast para não bloquear o pai

        // 3. Se existir RawImage no pai (configuração antiga), migra para o filho e remove do pai
        RawImage parentRawImage = GetComponent<RawImage>();
        if (parentRawImage != null)
        {
            if (childRawImage.texture == null) childRawImage.texture = parentRawImage.texture;
            childRawImage.color = parentRawImage.color;
            
            // Remove o componente do pai para evitar conflito com a máscara
            UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(parentRawImage);
        }

        // 4. Vincula a referência no script
        cardImage = childRawImage;

        // 5. Garante que o pai tenha Image e Mask para o arredondamento funcionar
        if (GetComponent<Image>() == null) gameObject.AddComponent<Image>();
        if (GetComponent<Mask>() == null) gameObject.AddComponent<Mask>().showMaskGraphic = true;

        Debug.Log("Hierarquia da carta ajustada com sucesso: Pai(Mask) -> Art(RawImage).");
    }
#endif

    void OnDisable()
    {
        // Garante que a requisição seja cancelada se o objeto for desativado
        if (currentRequest != null && !currentRequest.isDone)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
    }

    void OnDestroy()
    {
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }
    }
}