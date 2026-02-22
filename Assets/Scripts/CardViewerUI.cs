using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class CardViewerUI : MonoBehaviour
{
    [Header("Database Connection")]
    public CardDatabase cardDatabase;

    // --- CONTROLES GERAIS ---
    [Header("MODOS DE VISUALIZAÇÃO")]
    [Tooltip("Ativa o visualizador 2D (RawImage na tela)")]
    public bool modo2D_Ativado = true;

    // --- REFERÊNCIAS 2D ---
    [Header("REFERÊNCIAS DO MODO 2D")]
    public RawImage cardImage2D; // A imagem 2D que fica na UI
    private Texture2D frontTexture2D;
    private Texture2D backTexture2D;
    private bool is2DFlipped = false;

    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    private int currentIndex = 0;

    void Start()
    {
        // Fallback: Tenta pegar do GameManager se não estiver atribuído
        if (cardDatabase == null && GameManager.Instance != null)
        {
            cardDatabase = GameManager.Instance.cardDatabase;
        }

        if (cardDatabase == null || cardDatabase.cardDatabase.Count == 0)
        {
            Debug.LogError("Banco de dados não conectado ou está vazio!");
            return;
        }

        StartCoroutine(LoadCardBackTexture());
        DisplayCard(currentIndex);
    }

    void DisplayCard(int index)
    {
        if (index < 0 || index >= cardDatabase.cardDatabase.Count) return;

        CardData card = cardDatabase.cardDatabase[index];

        if (cardNameText != null) cardNameText.text = card.name;
        if (cardDescriptionText != null) cardDescriptionText.text = card.description;

        string info = $"[{card.type}]";
        if (!string.IsNullOrEmpty(card.race)) info += $" / {card.race}";
        if (card.level > 0) info += $" / LV: {card.level}";
        if (cardInfoText != null) cardInfoText.text = info;

        if (cardStatsText != null)
        {
            if (card.type.Contains("Monster"))
                cardStatsText.text = $"ATK/ {card.atk}  DEF/ {card.def}";
            else
                cardStatsText.text = "";
        }

        StartCoroutine(LoadCardTexture(card.image_filename));
    }

    // Novo método para exibir uma carta específica (usado pelo Deck Builder)
    public void DisplayCardData(CardData card)
    {
        if (card == null) return;

        if (cardNameText) cardNameText.text = card.name;
        if (cardDescriptionText) cardDescriptionText.text = card.description;

        string info = $"[{card.type}]";
        if (!string.IsNullOrEmpty(card.race)) info += $" / {card.race}";
        if (card.level > 0) info += $" / LV: {card.level}";
        if (cardInfoText) cardInfoText.text = info;

        if (cardStatsText)
            cardStatsText.text = card.type.Contains("Monster") ? $"ATK/ {card.atk}  DEF/ {card.def}" : "";
        else if (cardStatsText) cardStatsText.text = "";

        StartCoroutine(LoadCardTexture(card.image_filename));
    }

    IEnumerator LoadCardBackTexture()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "YuGiOh_OCG_Classic_2147/0000 - Background.jpg");
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.filterMode = FilterMode.Trilinear;
                backTexture2D = texture; // Salva para o flip 2D
            }
        }
    }

    IEnumerator LoadCardTexture(string imagePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath);
        using (request)
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.filterMode = FilterMode.Trilinear;

                // Atualiza o 2D
                frontTexture2D = texture;
                if (cardImage2D != null) {
                    is2DFlipped = false;
                    cardImage2D.texture = frontTexture2D;
                }
            }
        }
    }

    // --- Funções de Interação ---

    // Chamado pelo clique na imagem 2D
    public void OnCardClicked()
    {
        if (!modo2D_Ativado || cardImage2D == null) return;

        is2DFlipped = !is2DFlipped;
        cardImage2D.texture = is2DFlipped ? backTexture2D : frontTexture2D;
    }

    public void ShowNextCard()
    {
        currentIndex++;
        if (currentIndex >= cardDatabase.cardDatabase.Count) currentIndex = 0;
        DisplayCard(currentIndex);
    }

    public void ShowPreviousCard()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = cardDatabase.cardDatabase.Count - 1;
        DisplayCard(currentIndex);
    }

    // Chamado automaticamente no Editor quando você muda um valor
    void OnValidate()
    {
        // Ativa/Desativa os objetos com base nos toggles
        if (cardImage2D != null)
        {
            cardImage2D.gameObject.SetActive(modo2D_Ativado);
        }
    }
}
