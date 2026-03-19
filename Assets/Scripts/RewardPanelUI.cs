using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia a interface do painel de recompensas exibido ao final de um duelo.
/// </summary>
public class RewardPanelUI : MonoBehaviour
{
    [System.Serializable]
    public struct RankArtwork
    {
        public string rankName; // Ex: "S", "APlus", "A", "BPlus", "B", "C", "D", "F", "SPlus"
        public Sprite artwork;
    }

    [Header("UI References")]
    [Tooltip("Texto do título, ex: 'VITÓRIA'")]
    public TextMeshProUGUI titleText;

    [Tooltip("Texto que exibirá o Rank obtido, ex: 'RANK: S+'")]
    public TextMeshProUGUI rankText;

    [Tooltip("Componente de imagem para exibir a arte do Rank (S, A, B, etc).")]
    public Image rankImage;

    [Tooltip("Lista associando o texto do Rank à sua respectiva imagem.")]
    public List<RankArtwork> rankArtworks = new List<RankArtwork>();

    [Tooltip("Referência ao CardDisplay que mostrará a carta ganha.")]
    public CardDisplay cardDisplay;

    [Tooltip("Botão para fechar o painel e continuar.")]
    public Button continueButton;

    [Header("New Card Visuals")]
    [Tooltip("Prefab customizado para a tag 'NEW'. Se deixado vazio, uma tag dinâmica será criada.")]
    public GameObject newTagPrefab;
    [Tooltip("Cor da faixa da tag 'NEW' dinâmica.")]
    public Color newTagBannerColor = new Color(0, 0, 0, 0.7f); // Preto semitransparente por padrão
    [Tooltip("Cor do texto da tag 'NEW' dinâmica.")]
    public Color newTagTextColor = Color.white;

    private GameObject currentNewBanner; // Referência ao banner instanciado

    void Start()
    {
        // Adiciona um listener para o botão de continuar, que esconderá o painel.
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(HidePanel);
        }
        gameObject.SetActive(false); // O painel começa desativado.
    }

    /// <summary>
    /// Exibe o painel de recompensas com as informações do duelo.
    /// </summary>
    public void Show(string rank, CardData wonCard, bool isNew = false)
    {
        gameObject.SetActive(true);

        if (titleText) titleText.text = "VITÓRIA";
        if (rankText) rankText.text = $"RANK: {rank}";

        if (rankImage != null)
        {
            var art = rankArtworks.Find(r => r.rankName.Equals(rank, System.StringComparison.OrdinalIgnoreCase));
            if (art.artwork != null)
            {
                rankImage.sprite = art.artwork;
                rankImage.gameObject.SetActive(true);
            }
            else
            {
                rankImage.gameObject.SetActive(false);
            }
        }

        if (wonCard != null && cardDisplay != null)
        {
            cardDisplay.gameObject.SetActive(true);
            cardDisplay.SetCard(wonCard, GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null, true);
            HandleNewBanner(isNew); // Aplica a lógica da tag "NEW"
        }
        else if (cardDisplay != null)
        {
            cardDisplay.gameObject.SetActive(false);
            HandleNewBanner(false); // Esconde a tag se não houver carta
        }
    }

    private void HandleNewBanner(bool isNew)
    {
        // Destroi banner antigo se existir
        if (currentNewBanner != null)
        {
            Destroy(currentNewBanner);
            currentNewBanner = null;
        }

        if (!isNew) return;

        if (newTagPrefab != null && cardDisplay != null)
        {
            // Cria um container para o prefab
            GameObject bannerContainer = new GameObject("NewTagContainer", typeof(RectTransform));
            bannerContainer.transform.SetParent(cardDisplay.transform, false);
            currentNewBanner = bannerContainer; // Define como banner atual para ser destruído depois

            // Configura o RectTransform do container para ser uma faixa central
            RectTransform containerRect = bannerContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0.5f);
            containerRect.anchorMax = new Vector2(1, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, 36); // Altura inicial

            // Adiciona o AspectRatioFitter para manter a proporção
            AspectRatioFitter fitter = bannerContainer.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 120f / 36f; // Proporção do prefab original (120x36)

            // Instancia o prefab do usuário DENTRO do container
            GameObject bannerInstance = Instantiate(newTagPrefab, bannerContainer.transform);
            bannerInstance.name = "NewTag_Custom_Instance";
            bannerInstance.SetActive(true);

            // Força o prefab a preencher o container
            RectTransform bannerRect = bannerInstance.GetComponent<RectTransform>();
            if (bannerRect != null)
            {
                bannerRect.anchorMin = Vector2.zero;
                bannerRect.anchorMax = Vector2.one;
                bannerRect.pivot = new Vector2(0.5f, 0.5f);
                bannerRect.sizeDelta = Vector2.zero;
                bannerRect.anchoredPosition = Vector2.zero;
            }
        }
        else if (cardDisplay != null)
        {
            // Fallback para banner dinâmico
            CreateDynamicBanner();
        }

        if (currentNewBanner != null)
        {
            currentNewBanner.transform.SetAsLastSibling();
        }
    }

    private void CreateDynamicBanner()
    {
        GameObject bannerObj = new GameObject("NewBanner_Dynamic", typeof(RectTransform), typeof(Image));
        bannerObj.transform.SetParent(cardDisplay.transform, false);
        currentNewBanner = bannerObj;
        
        // Configura Fundo (Faixa)
        Image img = bannerObj.GetComponent<Image>();
        img.color = newTagBannerColor;
        
        RectTransform rect = bannerObj.GetComponent<RectTransform>();
        // Faixa central que ocupa 80% da largura e 25% da altura
        rect.anchorMin = new Vector2(0.1f, 0.375f);
        rect.anchorMax = new Vector2(0.9f, 0.625f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Configura Texto
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(bannerObj.transform, false);
        
        TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
        txt.text = "NEW";
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 36;
        txt.fontStyle = FontStyles.Bold;
        txt.enableAutoSizing = true;
        txt.color = newTagTextColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 5); // Pequena margem interna
        textRect.offsetMax = new Vector2(-5, -5);
    }

    /// <summary>
    /// Esconde o painel de recompensas e volta ao menu.
    /// </summary>
    private void HidePanel()
    {
        gameObject.SetActive(false);

        // Retorna ao menu principal via UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Btn_BackToMenu();
        }
    }
}
