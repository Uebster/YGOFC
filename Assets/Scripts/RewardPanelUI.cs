using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia a interface do painel de recompensas exibido ao final de um duelo.
/// </summary>
public class RewardPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Texto do título, ex: 'VITÓRIA'")]
    public TextMeshProUGUI titleText;

    [Tooltip("Texto que exibirá o Rank obtido, ex: 'RANK: S+'")]
    public TextMeshProUGUI rankText;

    [Tooltip("Referência ao CardDisplay que mostrará a carta ganha.")]
    public CardDisplay cardDisplay;

    [Tooltip("Botão para fechar o painel e continuar.")]
    public Button continueButton;

    [Header("New Card Visuals")]
    public GameObject newCardBanner; // Faixa "NEW"

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
    /// <param name="rank">O rank obtido pelo jogador (ex: "S+", "A", "D").</param>
    /// <param name="wonCard">O objeto CardData da carta ganha.</param>
    /// <param name="isNew">Se a carta é nova na coleção (não existia no Trunk).</param>
    public void Show(string rank, CardData wonCard, bool isNew = false)
    {
        gameObject.SetActive(true);

        if (titleText) titleText.text = "VITÓRIA"; // Ou "DERROTA", se aplicável
        if (rankText) rankText.text = $"RANK: {rank}";

        if (wonCard != null && cardDisplay != null)
        {
            cardDisplay.gameObject.SetActive(true);
            cardDisplay.SetCard(wonCard, GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null, true);
        }
        else if (cardDisplay != null)
        {
            // Caso nenhuma carta seja ganha (ex: Rank D em alguns cenários)
            cardDisplay.gameObject.SetActive(false);
            if (newCardBanner != null) newCardBanner.SetActive(false);
        }
    }

    private void HandleNewBanner(bool isNew)
    {
        if (!isNew)
        {
            if (newCardBanner != null) newCardBanner.SetActive(false);
            return;
        }

        // Se não tiver banner atribuído, cria um dinamicamente
        if (newCardBanner == null && cardDisplay != null)
        {
            CreateDynamicBanner();
        }

        if (newCardBanner != null)
        {
            newCardBanner.SetActive(true);
            // Garante que fique na frente da carta
            newCardBanner.transform.SetAsLastSibling();
        }
    }

    private void CreateDynamicBanner()
    {
        GameObject bannerObj = new GameObject("NewBanner", typeof(RectTransform), typeof(Image));
        bannerObj.transform.SetParent(cardDisplay.transform, false);
        
        // Configura Fundo (Faixa)
        Image img = bannerObj.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.7f); // Preto semitransparente
        
        RectTransform rect = bannerObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.4f);
        rect.anchorMax = new Vector2(1, 0.6f); // Faixa central
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
        
        // Usa a cor de hover do player se disponível
        if (GameManager.Instance != null)
            txt.color = GameManager.Instance.playerHoverColor;
        else
            txt.color = Color.green;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        newCardBanner = bannerObj;
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
