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
    private UnityWebRequest currentRequest;
    private UnityWebRequest backTextureRequest; // Rastreia a requisição do verso
    private Coroutine loadCardCoroutine;

    void Awake()
    {
        // AUTO-CONFIGURAÇÃO: Tenta encontrar referências nos filhos se estiverem nulas
        if (cardNameText == null) cardNameText = FindChildText("Name");
        if (cardInfoText == null) cardInfoText = FindChildText("Info");
        if (cardDescriptionText == null) cardDescriptionText = FindChildText("Description");
        if (cardStatsText == null) cardStatsText = FindChildText("Stats");
        
        // Tenta achar a imagem (RawImage)
        if (cardImage2D == null) cardImage2D = GetComponentInChildren<RawImage>();
    }

    // Helper para buscar textos por nome parcial (ex: "CardNameText" acha "Name")
    TextMeshProUGUI FindChildText(string keyword)
    {
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach(var t in texts) if(t.name.Contains(keyword)) return t;
        return null;
    }

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

        // Limpeza de recursos anteriores
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (loadCardCoroutine != null) StopCoroutine(loadCardCoroutine);
        if (frontTexture2D != null)
        {
            Destroy(frontTexture2D);
            frontTexture2D = null;
        }

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

        loadCardCoroutine = StartCoroutine(LoadCardTexture(card.image_filename));
    }

    // Novo método para exibir uma carta específica (usado pelo Deck Builder)
    public void DisplayCardData(CardData card)
    {
        // Limpeza de recursos anteriores
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (loadCardCoroutine != null) StopCoroutine(loadCardCoroutine);
        if (frontTexture2D != null)
        {
            Destroy(frontTexture2D);
            frontTexture2D = null;
        }

        if (card == null)
        {
            if (cardNameText) cardNameText.text = "";
            if (cardDescriptionText) cardDescriptionText.text = "";
            if (cardInfoText) cardInfoText.text = "";
            if (cardStatsText) cardStatsText.text = "";
            if (cardImage2D != null) 
            {
                cardImage2D.texture = backTexture2D != null ? backTexture2D : (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null);
                cardImage2D.color = Color.white;
            }
            return;
        }

        if (cardNameText) cardNameText.text = card.name;
        if (cardDescriptionText) cardDescriptionText.text = card.description;

        string info = $"[{card.type}]";
        if (!string.IsNullOrEmpty(card.race)) info += $" / {card.race}";
        if (card.level > 0) info += $" / LV: {card.level}";
        if (cardInfoText) cardInfoText.text = info;

        if (cardStatsText)
            cardStatsText.text = card.type.Contains("Monster") ? $"ATK/ {card.atk}  DEF/ {card.def}" : "";
        else if (cardStatsText) cardStatsText.text = "";

        loadCardCoroutine = StartCoroutine(LoadCardTexture(card.image_filename));
    }

    IEnumerator LoadCardBackTexture()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "YuGiOh_OCG_Classic_2147/0000 - Background.jpg");
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        // Removemos o 'using' para evitar o leak se a corrotina for interrompida
        backTextureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return backTextureRequest.SendWebRequest();

        if (backTextureRequest == null) yield break; // Previne erro se cancelado

        if (backTextureRequest.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(backTextureRequest);
            texture.filterMode = FilterMode.Trilinear;
            backTexture2D = texture; // Salva para o flip 2D
        }
        backTextureRequest.Dispose();
        backTextureRequest = null;
    }

    IEnumerator LoadCardTexture(string imagePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);
        
        // FIX: Usa System.Uri para escapar caracteres especiais (como #) corretamente
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        currentRequest = request; // Salva na global para o OnDisable abortar se necessário
        
        yield return request.SendWebRequest();

        // Se a requisição global mudou (outra corrotina iniciou) ou ficou nula (OnDisable), aborta esta execução
        if (currentRequest != request) 
        {
            request.Dispose();
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            frontTexture2D = DownloadHandlerTexture.GetContent(request);
            frontTexture2D.filterMode = FilterMode.Trilinear;

            // Atualiza o 2D
            if (cardImage2D != null) {
                is2DFlipped = false;
                cardImage2D.texture = frontTexture2D;
            }
        }
        
        if (currentRequest == request) currentRequest = null;
        request.Dispose();
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

    void OnDisable()
    {
        // Garante que a requisição seja cancelada se o objeto for desativado
        if (currentRequest != null && !currentRequest.isDone)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (backTextureRequest != null && !backTextureRequest.isDone) 
        {
            backTextureRequest.Dispose();
            backTextureRequest = null; // A Causa do Crash: Faltava anular a variável após descartar!
        }
    }

    void OnDestroy()
    {
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (backTextureRequest != null)
        {
            backTextureRequest.Dispose();
            backTextureRequest = null;
        }
        if (frontTexture2D != null)
        {
            Destroy(frontTexture2D);
            frontTexture2D = null;
        }
    }
}
