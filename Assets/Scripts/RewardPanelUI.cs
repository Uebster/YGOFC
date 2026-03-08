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
    public void Show(string rank, CardData wonCard)
    {
        gameObject.SetActive(true);

        if (titleText) titleText.text = "VITÓRIA"; // Ou "DERROTA", se aplicável
        if (rankText) rankText.text = $"RANK: {rank}";

        if (wonCard != null && cardDisplay != null)
        {
            cardDisplay.gameObject.SetActive(true);
            // Usa a textura de verso do GameManager se disponível
            Texture2D backTex = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;
            // true = Face Up (Virada para cima)
            cardDisplay.SetCard(wonCard, backTex, true);
        }
        else if (cardDisplay != null)
        {
            // Caso nenhuma carta seja ganha (ex: Rank D em alguns cenários)
            cardDisplay.gameObject.SetActive(false);
        }
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
