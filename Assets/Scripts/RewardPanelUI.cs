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
        continueButton.onClick.AddListener(HidePanel);
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

        titleText.text = "VITÓRIA"; // Ou "DERROTA", se aplicável
        rankText.text = $"RANK: {rank}";

        if (wonCard != null)
        {
            cardDisplay.gameObject.SetActive(true);
            cardDisplay.SetCard(wonCard, GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null, true);
        }
        else
        {
            // Caso nenhuma carta seja ganha (ex: Rank D em alguns cenários)
            cardDisplay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Esconde o painel de recompensas.
    /// </summary>
    private void HidePanel()
    {
        gameObject.SetActive(false);

        // Aqui, você chamaria o GameManager ou ThemeManager para retornar ao mapa da campanha ou menu.
        // Exemplo:
        // GameManager.Instance.ReturnToCampaignMap();
    }
}
