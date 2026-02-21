using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para Shuffle
using UnityEngine.EventSystems;

public enum GamePhase
{
    Draw,
    Standby,
    Main1,
    Battle,
    Main2,
    End
}

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
    [Tooltip("Habilita funcionalidades de desenvolvedor (ex: ver mão do oponente, sacar para oponente)")]
    public bool devMode = false;
    [Tooltip("Permite sacar cartas clicando no Deck")]
    public bool enableDeckClickDraw = true;
    [Tooltip("Define se as cartas do oponente estão visíveis")]
    public bool showOpponentHand = false;

    [Header("CONFIGURAÇÕES DE TELA")]
    public int screenWidth = 1600;
    public int screenHeight = 900;
    public bool fullScreen = false;

    [Header("Hand Settings")]
    [Tooltip("A escala das cartas na mão do jogador.")]
    public Vector3 handCardScale = new Vector3(1f, 1f, 1f);
    [Tooltip("A altura que a carta sobe ao passar o mouse sobre ela.")]
    public float handCardHoverYOffset = 30f;
    [Tooltip("Ativa ou desativa o efeito de hover (subir) nas cartas da mão.")]
    public bool enableHandHover = true;

    [Header("Game Flow")]
    public GamePhase currentPhase = GamePhase.Draw;
    public float standbyPhaseDuration = 2.0f;
    public TextMeshProUGUI phaseText; // Arraste um texto da UI aqui para ver a fase

    // --- REFERÊNCIAS 2D ---
    [Header("REFERÊNCIAS DO MODO 2D")]
    public RawImage cardDisplayArea; // A RawImage onde a carta individual será exibida
    private Texture2D cardBackTexture; // Textura do verso da carta, carregada uma vez

    [Header("Card Prefab")]
    public GameObject cardPrefab; // Prefab da carta para instanciar na mão
    public Transform playerHandLayoutGroup; // O HorizontalLayoutGroup da mão do jogador
    public Transform opponentHandLayoutGroup; // O HorizontalLayoutGroup da mão do oponente
    
    [Header("Piles Visuals")]
    public PileDisplay playerDeckDisplay;
    public PileDisplay opponentDeckDisplay;
    public PileDisplay playerGraveyardDisplay;
    public PileDisplay opponentGraveyardDisplay;
    public PileDisplay playerExtraDeckDisplay;
    public PileDisplay opponentExtraDeckDisplay;

    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    private List<CardData> playerDeck = new List<CardData>();
    private List<GameObject> playerHand = new List<GameObject>();
    private List<CardData> playerGraveyard = new List<CardData>();
    private List<CardData> opponentDeck = new List<CardData>();
    private List<GameObject> opponentHand = new List<GameObject>();
    private List<CardData> opponentGraveyard = new List<CardData>();
    private List<CardData> playerExtraDeck = new List<CardData>();
    private List<CardData> opponentExtraDeck = new List<CardData>();
    private CardDisplay currentCardDisplay; // Referência ao CardDisplay na cardDisplayArea

    [Header("Current Duel Info")]
    public CharacterData currentOpponent; // Oponente atual carregado
    public int currentDuelIndex = -1; // Índice do duelo atual na campanha (para salvar progresso)

    [Header("Player Profile")]
    public string playerName = "Duelist";
    public string currentSaveID = "default";

    void Awake()
    {
        Instance = this;
        Screen.SetResolution(screenWidth, screenHeight, fullScreen);
    }

    IEnumerator Start()
    {
        if (cardDatabase == null || cardDatabase.cardDatabase.Count == 0)
        {
            Debug.LogError("Banco de dados não conectado ou está vazio!");
            yield break;
        }
        
        // Carrega o nome salvo
        playerName = PlayerPrefs.GetString("PlayerName", "Duelist");
        currentSaveID = PlayerPrefs.GetString("CurrentSaveID", "default");

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

        // Removido do Start automático. Agora aguarda o UIManager chamar StartDuel()
        // InitializePlayerDeck();
        // InitializeOpponentDeck();
        // DrawInitialHand(5);
        // DrawInitialOpponentHand(5);
    }
    
    public void SetPlayerProfile(string newName, string newSaveID)
    {
        playerName = newName;
        currentSaveID = newSaveID;
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("CurrentSaveID", currentSaveID);
        PlayerPrefs.Save();
    }

    // Novo método chamado pelo botão "Free Duel"
    public void StartDuel()
    {
        // Limpa o estado anterior (destrói cartas visuais, limpa listas)
        CleanupDuelState();

        InitializePlayerDeck();
        InitializeOpponentDeck();
        DrawInitialHand(5); // Exemplo: compra 5 cartas iniciais
        DrawInitialOpponentHand(5);
        ChangePhase(GamePhase.Draw); // Começa o turno na Draw Phase

        // Inicia o rastreamento de pontuação para o Rank
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StartDuelTracking();
        }
    }

    void CleanupDuelState()
    {
        // Destrói objetos visuais das mãos
        foreach (GameObject card in playerHand) if (card != null) Destroy(card);
        playerHand.Clear();

        foreach (GameObject card in opponentHand) if (card != null) Destroy(card);
        opponentHand.Clear();

        // Limpa listas de dados
        playerDeck.Clear();
        opponentDeck.Clear();
        playerGraveyard.Clear();
        opponentGraveyard.Clear();
        playerExtraDeck.Clear();
        opponentExtraDeck.Clear();

        // Atualiza visuais das pilhas e limpa o viewer
        UpdatePileVisuals();
        ClearCardViewer();
    }
    
    // Sobrecarga para iniciar duelo contra personagem específico (Campanha)
    public void StartDuel(CharacterData opponent, int duelIndex = -1)
    {
        currentOpponent = opponent;
        currentDuelIndex = duelIndex;
        StartDuel(); // Chama o método principal
    }

    void InitializePlayerDeck()
    {
        // Separa as cartas do Extra Deck (Fusão) do deck principal
        // (Aqui você futuramente carregará o deck salvo do jogador, por enquanto carrega tudo)
        foreach (var card in cardDatabase.cardDatabase)
        {
            if (card.type.Contains("Fusion"))
            {
                playerExtraDeck.Add(card);
            }
            else
            {
                playerDeck.Add(card);
            }
        }
        ShuffleDeck();
        Debug.Log($"Deck do jogador inicializado com {playerDeck.Count} cartas.");
        UpdatePileVisuals();
    }

    void InitializeOpponentDeck()
    {
        // Se temos um oponente definido (Campanha), carregamos o deck dele
        if (currentOpponent != null && currentOpponent.deck_A != null && currentOpponent.deck_A.Count > 0)
        {
            foreach (string cardId in currentOpponent.deck_A)
            {
                CardData card = cardDatabase.GetCardById(cardId);
                if (card != null)
                {
                    if (card.type.Contains("Fusion")) opponentExtraDeck.Add(card);
                    else opponentDeck.Add(card);
                }
            }
        }
        else
        {
            // Fallback: Se não tiver oponente (Free Duel genérico), carrega tudo ou aleatório
            Debug.Log("Nenhum oponente específico definido. Carregando banco de dados inteiro (Modo Teste).");
            foreach (var card in cardDatabase.cardDatabase)
            {
                if (card.type.Contains("Fusion"))
                {
                    opponentExtraDeck.Add(card);
                }
                else
                {
                    opponentDeck.Add(card);
                }
            }
        }
        ShuffleOpponentDeck();
        Debug.Log($"Deck do oponente inicializado com {opponentDeck.Count} cartas.");
        UpdatePileVisuals();
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
        UpdatePileVisuals();
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
        UpdatePileVisuals();
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
        UpdatePileVisuals(); // Atualiza visual do deck após remover carta
        
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

            newCardGO.transform.localScale = handCardScale;
            newCardDisplay.hoverYOffset = handCardHoverYOffset;
            newCardDisplay.isInteractable = true; // Habilita o efeito de hover

            newCardDisplay.SetCard(drawnCard, cardBackTexture);
            playerHand.Add(newCardGO);
        }
        else
        {
            Debug.LogWarning("Card Prefab ou Player Hand Layout Group não atribuídos. Não foi possível adicionar a carta à mão visualmente.");
        }

        // Lógica de Fase: Se o jogador sacar na Draw Phase, avança para Standby
        if (currentPhase == GamePhase.Draw)
        {
            StartCoroutine(HandleStandbyPhase());
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
        UpdatePileVisuals(); // Atualiza visual do deck do oponente
        
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

            newCardGO.transform.localScale = handCardScale;
            newCardDisplay.hoverYOffset = handCardHoverYOffset;
            newCardDisplay.isInteractable = true; // Habilita o efeito de hover

            // Cartas do oponente entram viradas para baixo (false), exceto se showOpponentHand estiver ativo
            bool startFaceUp = showOpponentHand;
            newCardDisplay.SetCard(drawnCard, cardBackTexture, startFaceUp);
            opponentHand.Add(newCardGO);
            
            if (devMode) Debug.Log($"Oponente comprou: {drawnCard.name}");
        }
        else
        {
            Debug.LogError("DrawOpponentCard: CardPrefab ou OpponentHandLayoutGroup não atribuídos no GameManager!");
        }
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
    }

    // Método para enviar carta para o cemitério (Exemplo de uso)
    public void SendToGraveyard(CardData card, bool isPlayer)
    {
        if (isPlayer)
        {
            playerGraveyard.Add(card);
        }
        else
        {
            opponentGraveyard.Add(card);
        }
        
        if (DuelScoreManager.Instance != null)
        {
            // Registra pontuação se for carta do jogador indo pro cemitério
            if (isPlayer)
            {
                DuelScoreManager.Instance.RecordCardSentToGY();
            }
            // Registra pontuação se for monstro do inimigo indo pro cemitério (destruído)
            else if (card.type.Contains("Monster"))
            {
                DuelScoreManager.Instance.RecordEnemyMonsterDestroyed();
            }
        }

        UpdatePileVisuals();
    }

    public void ViewGraveyard(bool isPlayer)
    {
        if (UIManager.Instance == null) return;

        List<CardData> graveyard = isPlayer ? playerGraveyard : opponentGraveyard;
        // Opcional: não mostrar cemitério vazio
        if (graveyard.Count == 0) return;

        UIManager.Instance.ShowGraveyard(graveyard, cardBackTexture);
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

    // Função de DEV para alternar visibilidade da mão do oponente
    public void ToggleOpponentHandVisibility()
    {
        if (!devMode) return;

        showOpponentHand = !showOpponentHand; // Alterna o estado

        foreach (GameObject cardGO in opponentHand)
        {
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            if (display != null)
            {
                if (showOpponentHand) display.ShowFront();
                else display.ShowBack();
            }
        }
    }

    // --- SISTEMA DE FASES ---

    public void ChangePhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"--- FASE: {currentPhase} ---");
        
        if (phaseText != null)
        {
            phaseText.text = currentPhase.ToString().ToUpper();
        }

        // Lógica específica de entrada na fase
        switch (currentPhase)
        {
            case GamePhase.Draw:
                // Aguarda o jogador clicar no deck (DrawCard)
                break;
            case GamePhase.Standby:
                StartCoroutine(HandleStandbyPhase());
                break;
            case GamePhase.Main1:
                // Aguarda jogadas do player
                break;
            // Outras fases...
        }
    }

    IEnumerator HandleStandbyPhase()
    {
        ChangePhase(GamePhase.Standby);
        yield return new WaitForSeconds(standbyPhaseDuration);
        ChangePhase(GamePhase.Main1);
    }

    // --- FUTURAS IMPLEMENTAÇÕES DE COMANDOS (COMENTÁRIOS) ---
    /*
    public void EnterBattlePhase() {
        // Lógica: Clicar no botão "Battle Phase" ou Botão Direito em campo vazio durante Main1
        ChangePhase(GamePhase.Battle);
    }

    public void EndTurn() {
        // Lógica: Clicar em "End Phase" ou Botão Direito em campo vazio durante Main2 (ou Battle se pular M2)
        ChangePhase(GamePhase.End);
        // Trocar turno para oponente...
    }

    // Comandos de Jogo:
    // Summon: Arrastar carta de monstro para zona de monstro (Face Up Attack)
    // Set: Botão direito na carta da mão -> "Set" (Face Down Defense)
    // Activate: Clicar em Spell/Trap -> "Activate"
    // Surrender: Botão direito no Deck -> "Surrender"
    */

    // Atualiza todos os visuais de pilhas (Decks e Cemitérios)
    void UpdatePileVisuals()
    {
        if (playerDeckDisplay != null) playerDeckDisplay.UpdatePile(playerDeck, cardBackTexture);
        if (opponentDeckDisplay != null) opponentDeckDisplay.UpdatePile(opponentDeck, cardBackTexture);
        if (playerGraveyardDisplay != null) playerGraveyardDisplay.UpdatePile(playerGraveyard, cardBackTexture);
        if (opponentGraveyardDisplay != null) opponentGraveyardDisplay.UpdatePile(opponentGraveyard, cardBackTexture);
        if (playerExtraDeckDisplay != null) playerExtraDeckDisplay.UpdatePile(playerExtraDeck, cardBackTexture);
        if (opponentExtraDeckDisplay != null) opponentExtraDeckDisplay.UpdatePile(opponentExtraDeck, cardBackTexture);
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

        // Atualiza visibilidade da mão do oponente em tempo real se alterar o showOpponentHand no Inspector
        if (Application.isPlaying && opponentHand != null)
        {
            foreach (GameObject cardGO in opponentHand)
            {
                if (cardGO != null)
                {
                    CardDisplay display = cardGO.GetComponent<CardDisplay>();
                    if (display != null)
                    {
                        if (showOpponentHand) display.ShowFront();
                        else display.ShowBack();
                    }
                }
            }
        }
    }

    // Método para finalizar o duelo e calcular o Rank
    // Chame isso quando o HP de alguém chegar a 0 ou Deck acabar
    public void EndDuel(bool playerWon, bool opponentDeckOut = false)
    {
        if (playerWon)
        {
            if (DuelScoreManager.Instance != null)
            {
                DuelScoreManager.Instance.StopDuelTracking(opponentDeckOut);
                int score;
                DuelRank rank = DuelScoreManager.Instance.CalculateFinalRank(out score);
                
                Debug.Log($"DUELO VENCIDO! Rank: {rank} | Pontos: {score}");
                Debug.Log(DuelScoreManager.Instance.GetScoreReport());
                
                // TODO: Chamar UIManager para mostrar tela de Vitória com o Rank
            }
        }
        else
        {
            Debug.Log("DUELO PERDIDO. Sem Rank.");
            // TODO: Chamar UIManager para mostrar tela de Derrota
        }
    }
}
