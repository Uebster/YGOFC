using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para Shuffle

public class GameManager : MonoBehaviour
{
    [Header("Database Connection")]
    public CardDatabase cardDatabase;
    public CharacterDatabase characterDatabase;

    // --- CONTROLES GERAIS ---
    [Header("MODOS DE VISUALIZAÇÃO")]
    [Tooltip("Ativa o visualizador 2D (RawImage na tela)")]
    public bool modo2D_Ativado = true;

    // --- REFERÊNCIAS 2D ---
    [Header("REFERÊNCIAS DO MODO 2D")]
    public RawImage cardDisplayArea; // A RawImage onde a carta individual será exibida
    private Texture2D cardBackTexture; // Textura do verso da carta, carregada uma vez

    [Header("Card Prefab")]
    public GameObject cardPrefab; // Prefab da carta para instanciar na mão
    public Transform playerHandLayoutGroup; // O HorizontalLayoutGroup da mão do jogador

    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    private List<CardData> playerDeck = new List<CardData>();
    private List<GameObject> playerHand = new List<GameObject>();
    private CardDisplay currentCardDisplay; // Referência ao CardDisplay na cardDisplayArea

    void Start()
    {
        if (cardDatabase == null || cardDatabase.cardDatabase.Count == 0)
        {
            Debug.LogError("Banco de dados não conectado ou está vazio!");
            return;
        }

        // Inicializa o CardDisplay na área de exibição
        if (cardDisplayArea != null)
        {
            currentCardDisplay = cardDisplayArea.GetComponent<CardDisplay>();
            if (currentCardDisplay == null)
            {
                currentCardDisplay = cardDisplayArea.gameObject.AddComponent<CardDisplay>();
            }
            // Conecta os elementos de UI do CardDisplay
            currentCardDisplay.cardImage = cardDisplayArea;
            currentCardDisplay.cardNameText = cardNameText;
            currentCardDisplay.cardInfoText = cardInfoText;
            currentCardDisplay.cardDescriptionText = cardDescriptionText;
            currentCardDisplay.cardStatsText = cardStatsText;
        }

        StartCoroutine(LoadCardBackTexture());
        InitializePlayerDeck();
        DrawInitialHand(5); // Exemplo: compra 5 cartas iniciais
    }

    void InitializePlayerDeck()
    {
        // Copia todas as cartas do banco de dados para o deck do jogador
        playerDeck = new List<CardData>(cardDatabase.cardDatabase);
        ShuffleDeck();
        Debug.Log($"Deck do jogador inicializado com {playerDeck.Count} cartas.");
    }

    void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        int n = playerDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardData value = playerDeck[k];
            playerDeck[k] = playerDeck[n];
            playerDeck[n] = value;
        }
        Debug.Log("Deck embaralhado.");
    }

    public void DrawCard()
    {
        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("Deck vazio! Não é possível comprar mais cartas.");
            return;
        }

        CardData drawnCard = playerDeck[0];
        playerDeck.RemoveAt(0);
        
        Debug.Log($"Carta comprada: {drawnCard.name}. Cartas restantes no deck: {playerDeck.Count}");

        // Exibe a carta comprada na área de visualização principal
        if (currentCardDisplay != null)
        {
            currentCardDisplay.SetCard(drawnCard, cardBackTexture);
        }

        // Adiciona a carta à mão visualmente
        if (cardPrefab != null && playerHandLayoutGroup != null)
        {
            GameObject newCardGO = Instantiate(cardPrefab, playerHandLayoutGroup);
            CardDisplay newCardDisplay = newCardGO.GetComponent<CardDisplay>();
            if (newCardDisplay == null)
            {
                newCardDisplay = newCardGO.AddComponent<CardDisplay>();
            }
            // Conecta os elementos de UI do prefab (assumindo que o prefab tem os mesmos elementos)
            newCardDisplay.cardImage = newCardGO.GetComponent<RawImage>(); // Ou encontre o RawImage filho
            // Para simplificar, não vamos conectar os textos da mão por enquanto, apenas a imagem
            // newCardDisplay.cardNameText = newCardGO.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            // ... e assim por diante para outros textos se o prefab os tiver e você quiser que apareçam na mão

            newCardDisplay.SetCard(drawnCard, cardBackTexture);
            playerHand.Add(newCardGO);
        }
        else
        {
            Debug.LogWarning("Card Prefab ou Player Hand Layout Group não atribuídos. Não foi possível adicionar a carta à mão visualmente.");
        }
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
    }

    IEnumerator LoadCardBackTexture()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "YuGiOh_OCG_Classic_2147/0000 - Background.jpg");
        UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            cardBackTexture = DownloadHandlerTexture.GetContent(request);
            cardBackTexture.filterMode = FilterMode.Trilinear;
        }
    }

    IEnumerator LoadCardTexture(string imagePath)
    {
        // Este método não é mais usado diretamente pelo GameManager para exibir cartas individuais.
        // A responsabilidade de carregar a textura da frente é do CardDisplay.
        yield break;
    }

    public void ShowNextCard()
    {
        // Lógica de navegação de cartas removida do GameManager,
        // pois agora ele gerencia o deck e mão, não um visualizador de índice.
        // Se quiser um visualizador de deck, ele precisaria de sua própria lógica.
        Debug.LogWarning("Função ShowNextCard não implementada no GameManager. Use DrawCard para comprar cartas.");
    }

    public void ShowPreviousCard()
    {
        Debug.LogWarning("Função ShowPreviousCard não implementada no GameManager. Use DrawCard para comprar cartas.");
    }

    // Chamado automaticamente no Editor quando você muda um valor
    void OnValidate()
    {
        // Ativa/Desativa os objetos com base nos toggles
        if (cardDisplayArea != null)
        {
            cardDisplayArea.gameObject.SetActive(modo2D_Ativado);
        }
    }
}
