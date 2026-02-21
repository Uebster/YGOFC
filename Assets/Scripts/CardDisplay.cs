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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // FIX: Configura a Borda para esticar na carta toda e não bloquear cliques
        if (outlineImage != null)
        {
            outlineImage.raycastTarget = false; // Importante: Ignora cliques
            // Coloca a borda atrás de tudo (primeiro filho) para não cobrir a carta
            outlineImage.transform.SetAsFirstSibling();
            outlineImage.rectTransform.anchorMin = Vector2.zero;
            outlineImage.rectTransform.anchorMax = Vector2.one;
            // Expande mais para fora (-10px) para garantir que apareça por fora
            outlineImage.rectTransform.offsetMin = new Vector2(-10, -10);
            outlineImage.rectTransform.offsetMax = new Vector2(10, 10);
            outlineImage.gameObject.SetActive(false);
        }

        // FIX: Adiciona Canvas para controlar a ordem de desenho sem quebrar o Layout (evita tremedeira)
        canvas = gameObject.AddComponent<Canvas>();
        graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
    }

    // Este método será chamado pelo GameManager para definir os dados da carta
    public void SetCard(CardData card, Texture2D cardBackTexture, bool startFaceUp = true)
    {
        if (card == null) return;
        StopAllCoroutines(); // Para carregamentos anteriores

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

        StartCoroutine(LoadCardFrontTexture(card.image_filename));
    }

    public void SetCardBackOnly(Texture2D cardBackTexture)
    {
        StopAllCoroutines();
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
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                frontTexture = DownloadHandlerTexture.GetContent(request);
                frontTexture.filterMode = FilterMode.Trilinear;
                if (cardImage != null)
                {
                    // Só aplica a textura da frente se a carta NÃO estiver virada (isFlipped == false)
                    if (!isFlipped) cardImage.texture = frontTexture;
                }
            }
            else
            {
                Debug.LogError($"Falha ao carregar imagem da frente da carta: {fullPath} | Erro: {request.error}");
            }
        }
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
        // --- Efeito de Borda (Hover) ---
        if (enableHoverOutline && outlineImage != null)
        {
            outlineImage.color = hoverColor;
            outlineImage.gameObject.SetActive(true);
        }

        // --- Efeito de Subir (Apenas Mão) ---
        // Verifica se é interativo E se o hover está habilitado no GameManager
        if (isInteractable && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHover)
        {
            // FIX: Usa Canvas Sorting para trazer para frente visualmente sem recalcular o Layout
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10; // Valor alto para ficar por cima de tudo
            
            // Move para cima (Y) mantendo a escala original
            rectTransform.anchoredPosition += new Vector2(0, hoverYOffset);
        }

        if (GameManager.Instance != null)
        {
            // Evita que o visualizador atualize a si mesmo se o mouse passar por cima dele
            if (GameManager.Instance.cardDisplayArea != null && cardImage == GameManager.Instance.cardDisplayArea) return;

            // Lógica para visualizar extra deck mesmo virado para baixo
            bool forceShowFaceUp = false;
            if (GameManager.Instance.playerExtraDeckDisplay != null && transform.parent == GameManager.Instance.playerExtraDeckDisplay.contentParent)
            {
                forceShowFaceUp = true;
            }

            // Se a carta estiver virada para baixo (isFlipped), mostra o verso no viewer (showFaceUp = false)
            bool showFaceUp = !isFlipped || forceShowFaceUp;
            GameManager.Instance.UpdateCardViewer(currentCardData, showFaceUp);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // --- Remove Borda ---
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(false);
        }

        // --- Remove Efeito de Subir ---
        if (isInteractable && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHover)
        {
            // FIX: Reseta o Canvas e a Escala
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            rectTransform.anchoredPosition -= new Vector2(0, hoverYOffset);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearCardViewer();
        }
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
        else if (DuelActionMenu.Instance == null)
        {
            Debug.LogError("CardDisplay: DuelActionMenu.Instance não encontrado na cena!");
        }
    }
}