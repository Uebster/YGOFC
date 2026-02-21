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
    private int originalSiblingIndex;

    [HideInInspector] public float hoverYOffset = 30f;
    [HideInInspector] public bool isInteractable = false; // Usado para habilitar hover apenas para cartas na mão

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Este método será chamado pelo GameManager para definir os dados da carta
    public void SetCard(CardData card, Texture2D cardBackTexture, bool startFaceUp = true)
    {
        if (card == null) return;
        StopAllCoroutines(); // Para carregamentos anteriores

        currentCardData = card;
        backTexture = cardBackTexture;
        isFlipped = !startFaceUp; // Se startFaceUp for false, isFlipped será true (verso)
        
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
        UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath);
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
            rectTransform.anchoredPosition += new Vector2(0, hoverYOffset);
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
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
            rectTransform.anchoredPosition -= new Vector2(0, hoverYOffset);
            transform.SetSiblingIndex(originalSiblingIndex);
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
        // Só abre o menu se a carta estiver na mão (isInteractable) e o menu existir
        if (isInteractable && DuelActionMenu.Instance != null)
        {
            DuelActionMenu.Instance.ShowMenu(gameObject, currentCardData);
        }
    }
}