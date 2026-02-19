using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para Shuffle
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
    public Transform opponentHandLayoutGroup; // O HorizontalLayoutGroup da mão do oponente

    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    private List<CardData> playerDeck = new List<CardData>();
    private List<GameObject> playerHand = new List<GameObject>();
    private List<CardData> opponentDeck = new List<CardData>();
    private List<GameObject> opponentHand = new List<GameObject>();
    private CardDisplay currentCardDisplay; // Referência ao CardDisplay na cardDisplayArea

    void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        if (cardDatabase == null || cardDatabase.cardDatabase.Count == 0)
        {
            Debug.LogError("Banco de dados não conectado ou está vazio!");
            yield break;
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

        yield return StartCoroutine(LoadCardBackTexture());

        InitializePlayerDeck();
        InitializeOpponentDeck();
        DrawInitialHand(5); // Exemplo: compra 5 cartas iniciais
        DrawInitialOpponentHand(5);
    }

    void InitializePlayerDeck()
    {
        // Copia todas as cartas do banco de dados para o deck do jogador
        playerDeck = new List<CardData>(cardDatabase.cardDatabase);
        ShuffleDeck();
        Debug.Log($"Deck do jogador inicializado com {playerDeck.Count} cartas.");
    }

    void InitializeOpponentDeck()
    {
        // Copia todas as cartas do banco de dados para o deck do oponente
        opponentDeck = new List<CardData>(cardDatabase.cardDatabase);
        ShuffleOpponentDeck();
        Debug.Log($"Deck do oponente inicializado com {opponentDeck.Count} cartas.");
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

    void ShuffleOpponentDeck()
    {
        System.Random rng = new System.Random();
        int n = opponentDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardData value = opponentDeck[k];
            opponentDeck[k] = opponentDeck[n];
            opponentDeck[n] = value;
        }
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

    public void DrawOpponentCard()
    {
        if (opponentDeck.Count == 0)
        {
            Debug.LogWarning("Deck do oponente vazio! Não é possível comprar mais cartas.");
            return;
        }

        CardData drawnCard = opponentDeck[0];
        opponentDeck.RemoveAt(0);
        
        // Adiciona a carta à mão visualmente
        if (cardPrefab != null && opponentHandLayoutGroup != null)
        {
            GameObject newCardGO = Instantiate(cardPrefab, opponentHandLayoutGroup);
            CardDisplay newCardDisplay = newCardGO.GetComponent<CardDisplay>();
            if (newCardDisplay == null)
            {
                newCardDisplay = newCardGO.AddComponent<CardDisplay>();
            }
            newCardDisplay.cardImage = newCardGO.GetComponent<RawImage>();

            // Cartas do oponente entram viradas para baixo (false)
            newCardDisplay.SetCard(drawnCard, cardBackTexture, false);
            opponentHand.Add(newCardGO);
        }
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
    }

    public void DrawInitialOpponentHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawOpponentCard();
        }
    }

    public void UpdateCardViewer(CardData card, bool isFaceUp)
    {
        if (currentCardDisplay == null) return;

        if (isFaceUp && card != null)
        {
            currentCardDisplay.SetCard(card, cardBackTexture, true);
        }
        else
        {
            // Se a carta estiver virada para baixo ou for do oponente, mostra o verso
            currentCardDisplay.SetCardBackOnly(cardBackTexture);
        }
    }

    public void ClearCardViewer()
    {
        if (currentCardDisplay == null) return;
        
        // Define o visualizador para mostrar o verso da carta (estado neutro)
        // ou poderia limpar tudo. Vamos mostrar o verso.
        currentCardDisplay.SetCardBackOnly(cardBackTexture);
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
