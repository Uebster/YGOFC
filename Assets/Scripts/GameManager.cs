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
    public CampaignDatabase campaignDatabase; // Adicionado para acesso global

    [Header("Field References")]
    public DuelFieldUI duelFieldUI; // Referência ao script que segura as zonas

    // --- CONTROLES GERAIS ---
    [Header("View Mode")]
    [Tooltip("Se marcado, usa um efeito 3D para virar as cartas. Se desmarcado, usa uma troca de textura 2D.")]
    public bool use3DFlipEffect = false;
    [Tooltip("Se marcado, usa o componente Outline do Unity para o contorno. Se desmarcado, usa uma imagem filha (OutlineImage).")]
    public bool useSimpleOutline = true;

    [Header("Card Visualization")]
    [Tooltip("A escala das cartas no campo.")]
    public Vector3 fieldCardScale = new Vector3(0.8f, 0.8f, 0.8f);
    [Tooltip("A escala das cartas na mão.")]
    public Vector3 handCardScale = new Vector3(1f, 1f, 1f);
    [Tooltip("A altura que a carta do jogador sobe ao passar o mouse.")]
    public float playerHandHoverYOffset = 30f;
    [Tooltip("A altura que a carta do oponente desce ao passar o mouse.")]
    public float opponentHandHoverYOffset = -30f;
    [Tooltip("Ativa o efeito de subir/descer a carta na mão com o mouse.")]
    public bool enableHandHoverEffect = true;

    [Header("Outline")]
    [Tooltip("Ativa o contorno de hover nas cartas da mão.")]
    public bool enableHandHoverOutline = true;
    [Tooltip("Ativa o contorno de hover nas cartas do campo.")]
    public bool enableFieldHoverOutline = true;
    [Tooltip("Ativa o contorno de hover nas cartas de pilhas (Deck, Cemitério, etc).")]
    public bool enablePileHoverOutline = false;
    [Tooltip("Ativa o contorno no visualizador de carta grande.")]
    public bool enableCardViewerOutline = true;

    [Header("Rounded Corners")]
    [Tooltip("Arredonda as bordas das cartas no campo.")]
    public bool useFieldCardsRounded = true;
    [Tooltip("Arredonda as bordas das cartas na mão.")]
    public bool useHandCardsRounded = true;
    [Tooltip("Arredonda as bordas da carta no visualizador grande.")]
    public bool useCardViewerRounded = true;

    [Header("Game Modes")]
    [Tooltip("Habilita funcionalidades de desenvolvedor.")]
    public bool devMode = false;
    [Tooltip("Habilita o menu de testes de efeitos visuais e sonoros.")]
    public bool effectTestMode = false;
    [Tooltip("Define se as cartas na mão do oponente são visíveis.")]
    public bool showOpponentHand = false;
    [Tooltip("Permite sacar cartas clicando no Deck do jogador.")]
    public bool canPlayerDrawFromDeck = true;
    [Tooltip("Permite sacar cartas para o oponente clicando no Deck dele (Modo Dev).")]
    public bool canOpponentDrawFromDeck = false;
    [Tooltip("Permite que o jogador baixe cartas da mão para o campo.")]
    public bool canPlacePlayerCards = true;
    [Tooltip("Permite baixar cartas para o campo do oponente (Modo Dev).")]
    public bool canPlaceOpponentCards = false;

    // --- REFERÊNCIAS 2D ---
    [Header("Core References")]
    public CardDisplay cardViewerDisplay;
    public Sprite cardMaskSprite; // Sprite para arredondar cantos (opcional)
    private Texture2D cardBackTexture; // Textura do verso da carta, carregada uma vez

    [Header("Card Prefab")]
    public GameObject cardPrefab; // Prefab da carta para instanciar na mão
    public GameObject tokenPrefab; // Prefab para Tokens (Scapegoat, etc)
    public Transform playerHandLayoutGroup; // O HorizontalLayoutGroup da mão do jogador
    public Transform opponentHandLayoutGroup; // O HorizontalLayoutGroup da mão do oponente
    
    [Header("Piles Visuals")]
    public PileDisplay playerDeckDisplay;
    public PileDisplay opponentDeckDisplay;
    public PileDisplay playerGraveyardDisplay;
    public PileDisplay opponentGraveyardDisplay;
    public PileDisplay playerExtraDeckDisplay;
    public PileDisplay opponentExtraDeckDisplay;
    public PileDisplay playerRemovedDisplay; // Novo
    public PileDisplay opponentRemovedDisplay; // Novo

    private List<CardData> playerDeck = new List<CardData>();
    // Adicionado: Side Deck e Baú (Trunk)
    private List<CardData> playerSideDeck = new List<CardData>();
    public List<string> playerTrunk = new List<string>(); // IDs das cartas que o jogador possui

    private List<GameObject> playerHand = new List<GameObject>();
    private List<CardData> playerGraveyard = new List<CardData>();
    private List<CardData> opponentDeck = new List<CardData>();
    private List<GameObject> opponentHand = new List<GameObject>();
    private List<CardData> opponentGraveyard = new List<CardData>();
    private List<CardData> playerExtraDeck = new List<CardData>();
    private List<CardData> opponentExtraDeck = new List<CardData>();
    private List<CardData> playerRemoved = new List<CardData>(); // Novo
    private List<CardData> opponentRemoved = new List<CardData>(); // Novo

    [Header("Current Duel Info")]
    public CharacterData currentOpponent; // Oponente atual carregado
    public int currentDuelIndex = -1; // Índice do duelo atual na campanha (para salvar progresso)

    [Header("Player Profile")]
    public string playerName = "Duelist";
    public string currentSaveID = "default";
    public int playerLP = 8000;
    public int opponentLP = 8000;
    public int turnCount = 0; // Contagem de turnos

    [Header("Runtime Theme Settings")]
    public Color themeHoverColor = Color.yellow; // Atualizado pelo DuelThemeManager

    private bool hasDrawnThisTurn = false; // Controle de draw por turno
    private UnityWebRequest backTextureRequest; // Para evitar memory leak

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
        
        // Carrega o nome salvo
        playerName = PlayerPrefs.GetString("PlayerName", "Duelist");
        currentSaveID = PlayerPrefs.GetString("CurrentSaveID", "default");
    
        // O CardViewer agora busca seus próprios textos
        if (cardViewerDisplay == null)
        {
            Debug.LogWarning("GameManager: O campo 'Card Viewer Display' não foi atribuído no Inspector. A visualização de cartas não funcionará.");
        }

        // Tenta encontrar o DuelFieldUI se não estiver atribuído
        if (duelFieldUI == null)
            duelFieldUI = FindFirstObjectByType<DuelFieldUI>();

        // Inicializa o Baú com todas as cartas se estiver vazio (Modo Sandbox/Teste)
        if (playerTrunk.Count == 0 && cardDatabase != null)
        {
            foreach(var card in cardDatabase.cardDatabase)
                playerTrunk.Add(card.id);
        }

        yield return StartCoroutine(LoadCardBackTexture());

        // FIX: Inicializa o deck automaticamente se estiver vazio (para testar o Draw imediatamente)
        if (playerDeck.Count == 0)
        {
            InitializePlayerDeck();
            InitializeOpponentDeck();
            DrawInitialHand(5);
        }
    }
    public void RemoveCardFromHand(CardData card, bool isPlayer)
    {
        List<CardData> hand = isPlayer ? playerHandDisplay.pileData : opponentHandDisplay.pileData;
        if (hand.Contains(card))
        {
            hand.Remove(card);
             if(isPlayer) playerHandDisplay.DisplayCards();
             else opponentHandDisplay.DisplayCards();
        } else
        {
            Debug.LogWarning("Esta carta não está na mão.");
        }
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
        
        // Garante que os gerenciadores essenciais existam na cena
        EnsureCoreManagers();

        InitializePlayerDeck();
        InitializeOpponentDeck();
        DrawInitialHand(5); // Exemplo: compra 5 cartas iniciais
        DrawInitialOpponentHand(5);
        
        // Inicializa LP
        playerLP = 8000;
        opponentLP = 8000;
        turnCount = 0;
        UpdateLPUI();
        
        // Reseta flag de draw antes de começar o turno
        hasDrawnThisTurn = false;
        
        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();
        else Debug.LogError("PhaseManager não encontrado mesmo após tentativa de criação!");

        // Inicia o rastreamento de pontuação para o Rank
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StartDuelTracking();
        }

        // Aplica o tema visual (se estivermos em um duelo de campanha ou tivermos um índice válido)
        if (campaignDatabase != null && DuelThemeManager.Instance != null)
        {
            // Se currentDuelIndex for -1 (Free Duel), tenta usar o tema do Ato 1 ou um padrão
            int levelToUse = (currentDuelIndex > 0) ? currentDuelIndex : 1;
            
            DuelTheme theme = campaignDatabase.GetThemeForLevel(levelToUse);
            if (theme != null)
                DuelThemeManager.Instance.ApplyTheme(theme);
        }
    }

    // Cria automaticamente os gerenciadores se eles não estiverem na cena
    void EnsureCoreManagers()
    {
        if (PhaseManager.Instance == null) CreateManager<PhaseManager>();
        if (SummonManager.Instance == null) CreateManager<SummonManager>();
        if (BattleManager.Instance == null) CreateManager<BattleManager>();
        if (SpellTrapManager.Instance == null) CreateManager<SpellTrapManager>();
        if (ChainManager.Instance == null) CreateManager<ChainManager>();
        if (CardEffectManager.Instance == null) CreateManager<CardEffectManager>();
        
        // Cria o gerenciador de testes se o modo estiver ativo
        if (effectTestMode && FindFirstObjectByType<EffectTestManager>() == null) CreateManager<EffectTestManager>();
    }

    void CreateManager<T>() where T : MonoBehaviour
    {
        GameObject go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        Debug.Log($"GameManager: Auto-criado gerenciador ausente: {typeof(T).Name}");
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
        playerSideDeck.Clear();
        opponentExtraDeck.Clear();
        playerRemoved.Clear();
        opponentRemoved.Clear();

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
        // Verifica se já existe um deck salvo no SaveLoadSystem
        // Se não, gera um novo
        if (InitialDeckBuilder.Instance != null)
        {
            Debug.Log("Gerando Deck Inicial Único...");
            List<CardData> generatedDeck = InitialDeckBuilder.Instance.GenerateInitialDeck();
            
            playerDeck.Clear();
            playerExtraDeck.Clear(); // Deck inicial não tem fusões prontas

            foreach (var card in generatedDeck)
            {
                playerDeck.Add(card);
                // Adiciona ao Trunk também para que o jogador "possua" a carta
                if (!playerTrunk.Contains(card.id))
                {
                    playerTrunk.Add(card.id);
                }
            }
        }
        else
        {
            // Fallback antigo (carrega tudo ou aleatório simples)
            Debug.LogWarning("InitialDeckBuilder não encontrado. Usando fallback.");
            // ... código antigo ...
        }

        // Salva o jogo imediatamente para persistir o deck gerado
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame(currentSaveID);
        }

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

    public void ShuffleDeck(bool isPlayer)
    {
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
 {
            int k = rng.Next(n + 1);
            CardData value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
        Debug.Log($"Deck do {(isPlayer ? "Player" : "Oponente")} embaralhado.");
        UpdatePileVisuals();
    }

    // --- AÇÕES DE JOGO PADRONIZADAS (GAME ACTIONS) ---

    public void BanishCard(CardDisplay card)
    {
        if (card == null) return;

        // Efeito Visual
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayBanishEffect(card);

        // Remove da lista da mão se estiver lá
        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        // Adiciona à lista de removidas
        RemoveFromPlay(card.CurrentCardData, card.isPlayerCard);

        // Remove modificadores que esta carta gerou em outras
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Destrói o objeto visual
        Destroy(card.gameObject);
    }

    public void ShuffleDeck(bool isPlayer)
    {
        GameManager.Instance.ShuffleDeck(isPlayer);
    }

    // --- NOVAS AÇÕES PADRONIZADAS ---

    public void TributeCard(CardDisplay card)
    {
        if (card == null) return;

        // Efeito Visual
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayTributeEffect(card);

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Envia para o GY (Lógica de dados)
        SendToGraveyard(card.CurrentCardData, card.isPlayerCard);

        // Destrói o objeto visual
        Destroy(card.gameObject);
    }

    public bool PayLifePoints(bool isPlayer, int amount)
    {
        int currentLP = isPlayer ? playerLP : opponentLP;
        if (currentLP < amount) return false; // Não pode pagar

        if (isPlayer) playerLP -= amount;
        else opponentLP -= amount;
        
        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} pagou {amount} LP.");
        return true;
    }

    public void GainLifePoints(bool isPlayer, int amount)
    {
        if (isPlayer) playerLP += amount;
        else opponentLP += amount;
        
        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} ganhou {amount} LP.");
        
        // Notifica sistema de efeitos (Ex: Fire Princess)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnLifePointsGained(isPlayer, amount);
    }

    public void MillCards(bool isPlayer, int amount)
    {
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;
        int count = Mathf.Min(amount, deck.Count);

        for (int i = 0; i < count; i++)
        {
            CardData card = deck[0];
            deck.RemoveAt(0);
            SendToGraveyard(card, isPlayer);
            Debug.Log($"Mill: {card.name}");
        }
        UpdatePileVisuals();
    }

    public void DiscardCard(CardDisplay card)
    {
        if (card == null) return;

        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
        
        // Remove modificadores (caso raro de efeito na mão, mas seguro)
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardDiscarded(card);

        Destroy(card.gameObject);
    }

    public void DiscardRandomHand(bool isPlayer, int amount)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        if (hand.Count == 0) return;

        int count = Mathf.Min(amount, hand.Count);
        
        for (int i = 0; i < count; i++)
        {
            if (hand.Count == 0) break;
            int rnd = Random.Range(0, hand.Count);
            GameObject cardGO = hand[rnd];
            CardDisplay cd = cardGO.GetComponent<CardDisplay>();
            DiscardCard(cd);
        }
    }

    public void DiscardHand(bool isPlayer)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        // Cria uma cópia da lista para iterar com segurança enquanto removemos
        List<GameObject> toDiscard = new List<GameObject>(hand);
        
        foreach (GameObject cardGO in toDiscard)
        {
            CardDisplay cd = cardGO.GetComponent<CardDisplay>();
            if (cd != null) DiscardCard(cd);
        }
    }

    public void ReturnToHand(CardDisplay card)
    {
        if (card == null) return;

        CardData data = card.CurrentCardData;
        bool isPlayer = card.isPlayerCard;

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Destrói objeto do campo
        Destroy(card.gameObject);

        // Adiciona à mão
        AddCardToHand(data, isPlayer);
        Debug.Log($"{data.name} retornada para a mão.");
    }

    public void ReturnToDeck(CardDisplay card, bool toTop)
    {
        if (card == null) return;

        CardData data = card.CurrentCardData;
        bool isPlayer = card.isPlayerCard;
        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;

        if (toTop) deck.Insert(0, data);
        else deck.Add(data);

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        Destroy(card.gameObject);
        UpdatePileVisuals();
        Debug.Log($"{data.name} retornada ao deck (Topo: {toTop}).");
    }

    // --- NOVAS AÇÕES PADRONIZADAS ---

    public void TributeCard(CardDisplay card)
    {
        if (card == null) return;

        // Efeito Visual
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayTributeEffect(card);

        // Remove modificadores
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Envia para o GY (Lógica de dados)
        SendToGraveyard(card.CurrentCardData, card.isPlayerCard);

        // Destrói o objeto visual
        Destroy(card.gameObject);
    }

    public bool PayLifePoints(bool isPlayer, int amount)
    {
        int currentLP = isPlayer ? playerLP : opponentLP;
        if (currentLP < amount) return false; // Não pode pagar

        if (isPlayer) playerLP -= amount;
        else opponentLP -= amount;
        
        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} pagou {amount} LP.");
        return true;
    }

    public void GainLifePoints(bool isPlayer, int amount)
    {
        if (isPlayer) playerLP += amount;
        else opponentLP += amount;
        
        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} ganhou {amount} LP.");
        
        // Notifica sistema de efeitos (Ex: Fire Princess)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnLifePointsGained(isPlayer, amount);
    }

    // Helper para adicionar carta à mão visualmente (usado por ReturnToHand e Search)
    public void AddCardToHand(CardData cardData, bool isPlayer)
    {
        Transform handTransform = isPlayer ? playerHandLayoutGroup : opponentHandLayoutGroup;
        if (cardPrefab == null || handTransform == null) return;

        GameObject newCardGO = Instantiate(cardPrefab, handTransform);
        CardDisplay newCardDisplay = newCardGO.GetComponent<CardDisplay>();
        if (newCardDisplay == null) newCardDisplay = newCardGO.AddComponent<CardDisplay>();

        newCardGO.transform.localScale = handCardScale;
        newCardDisplay.hoverYOffset = isPlayer ? playerHandHoverYOffset : opponentHandHoverYOffset;
        newCardDisplay.isInteractable = true;
        newCardDisplay.isPlayerCard = isPlayer;

        if (isPlayer)
        {
            newCardDisplay.SetCard(cardData, cardBackTexture);
            playerHand.Add(newCardGO);
        }
        else
        {
            newCardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
            newCardDisplay.SetCard(cardData, cardBackTexture, showOpponentHand);
            opponentHand.Add(newCardGO);
        }
    }

    public void DrawCard(bool ignoreLimit = false)
    {
        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("Deck vazio! Não é possível comprar mais cartas.");
            return;
        }

        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Draw;

        // Se não for um saque especial (mão inicial) e não estiver em modo dev, aplicam-se as regras do turno.
        if (!ignoreLimit && !devMode)
        {
            if (currentPhase != GamePhase.Draw)
            {
                Debug.LogWarning("Você só pode comprar cartas na Draw Phase!");
                return;
            }
            if (hasDrawnThisTurn)
            {
                Debug.LogWarning("Você já comprou uma carta neste turno!");
                return;
            }
        }

        CardData drawnCard = playerDeck[0];
        playerDeck.RemoveAt(0);
        UpdatePileVisuals(); // Atualiza visual do deck após remover carta
        
        Debug.Log($"Carta comprada: {drawnCard.name}. Cartas restantes no deck: {playerDeck.Count}");
        
        // Marca que já comprou neste turno (se não for mão inicial)
        if (!ignoreLimit) hasDrawnThisTurn = true;

        // Exibe a carta comprada na área de visualização principal
        if (cardViewerDisplay != null)
        {
            cardViewerDisplay.SetCard(drawnCard, cardBackTexture);
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
            
            // Força a busca de componentes se necessário, pois Awake pode já ter rodado antes de configurarmos tudo
            // Mas como acabamos de instanciar, Awake rodou.
            // Se a carta foi instanciada desativada, Awake não rodou.
            // Vamos garantir que ela esteja pronta.
            
            newCardGO.transform.localScale = handCardScale;
            newCardDisplay.hoverYOffset = playerHandHoverYOffset;
            newCardDisplay.isInteractable = true; 

            newCardDisplay.isPlayerCard = true;
            newCardDisplay.SetCard(drawnCard, cardBackTexture);
            playerHand.Add(newCardGO);
        }

        else
        {
            Debug.LogWarning("Card Prefab ou Player Hand Layout Group não atribuídos. Não foi possível adicionar a carta à mão visualmente.");
        }

        // Verifica vitória por Exodia após comprar
        CheckExodiaWin();

        // Lógica de Fase: Se o jogador sacar na Draw Phase, avança para Standby
        if (!ignoreLimit && currentPhase == GamePhase.Draw && PhaseManager.Instance != null)
        {
            // Avança para Standby via PhaseManager
            PhaseManager.Instance.ChangePhase(GamePhase.Standby);
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

            newCardGO.transform.localScale = handCardScale;
            newCardDisplay.hoverYOffset = opponentHandHoverYOffset; // Usa o offset do oponente
            newCardDisplay.isInteractable = true; // Habilita o efeito de hover

            newCardDisplay.isPlayerCard = false; // Carta do oponente
            newCardGO.transform.localRotation = Quaternion.Euler(0, 0, 180); // Vira a carta para o jogador
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
            DrawCard(true); // Ignora o limite para a mão inicial
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

        // Remove modificadores gerados por esta carta em outras cartas
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnCardLeavesField(cardDisplay);
        }

        // Notifica o sistema de efeitos (Gatilhos Globais)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnCardSentToGraveyard(card, isPlayer);
        }

        UpdatePileVisuals();
    }

    // Novo método para remover de jogo (Banir)
    public void RemoveFromPlay(CardData card, bool isPlayer)
    {
        if (isPlayer)
        {
            playerRemoved.Add(card);
        }
        else
        {
            opponentRemoved.Add(card);
        }
        
        // TODO: Adicionar lógica de pontuação se necessário
        
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

    public void ViewExtraDeck(bool isPlayer)
    {
        if (UIManager.Instance == null) return;

        // Nota: Normalmente só se pode ver o próprio Extra Deck, a menos que um efeito permita
        if (!isPlayer && !devMode) return;

        List<CardData> extraDeck = isPlayer ? playerExtraDeck : opponentExtraDeck;
        UIManager.Instance.ShowExtraDeck(extraDeck, cardBackTexture);
    }

    public void ViewRemovedCards(bool isPlayer)
    {
        if (UIManager.Instance == null) return;

        List<CardData> removed = isPlayer ? playerRemoved : opponentRemoved;
        // Reusa o visualizador de cemitério ou um específico se criado
        UIManager.Instance.ShowRemovedCards(removed, cardBackTexture);
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
        if (cardViewerDisplay == null) return;

        if (isFaceUp && card != null)
        {
            cardViewerDisplay.SetCard(card, cardBackTexture, true);
        }
        else
        {
            // Se a carta estiver virada para baixo ou for do oponente, mostra o verso
            cardViewerDisplay.SetCardBackOnly(cardBackTexture);
        }
        
        // Força a atualização visual do Card Viewer para aplicar as configurações de borda/arredondamento
        // Isso é importante se as configurações mudarem em tempo real ou se o estado da carta mudar
        // O método SetCard já chama ApplyRoundedCorners, mas podemos garantir aqui também se necessário.
    }

    public void ClearCardViewer()
    {
        if (cardViewerDisplay == null) return;
        cardViewerDisplay.SetCardBackOnly(cardBackTexture);
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

    // Chamado pelo PhaseManager quando entra na Draw Phase
    public void OnDrawPhaseStart()
    {
        // Reseta estados de turno dos monstros no campo
        if (duelFieldUI != null)
        {
            ResetCardStates(duelFieldUI.playerMonsterZones);
            ResetCardStates(duelFieldUI.opponentMonsterZones);
        }

        turnCount++; // Incrementa o turno
        hasDrawnThisTurn = false;
        if (!canPlayerDrawFromDeck)
        {
            // Verifica exceções de Draw via SpellTrapManager
            int draws = 1;
            if (SpellTrapManager.Instance != null) draws += SpellTrapManager.Instance.extraDrawsPerTurn;
            
            for(int i=0; i<draws; i++)
                DrawCard();
        }
    }

    // Chamado pelo PhaseManager quando entra na Standby Phase
    public void OnStandbyPhaseStart()
    {
        // Verifica custos de manutenção (Imperial Order, Mirror Wall, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.CheckMaintenanceCosts();
        }
    }

    // --- SISTEMA DE MOEDAS ---
    public void TossCoin(int numberOfCoins, System.Action<int> onResult)
    {
        StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
    }

    private IEnumerator CoinTossRoutine(int numberOfCoins, System.Action<int> onResult)
    {
        int headsCount = 0;
        for (int i = 0; i < numberOfCoins; i++)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Jogando moeda {i+1}...");
            yield return new WaitForSeconds(0.8f); // Tempo para "animação"
            
            bool isHeads = Random.value > 0.5f;
            if (isHeads) headsCount++;
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage(isHeads ? "CARA!" : "COROA!");
            yield return new WaitForSeconds(0.5f);
        }
        onResult?.Invoke(headsCount);
    }

    private void ResetCardStates(Transform[] zones)
    {
        foreach (Transform zone in zones)
        {
            if (zone.childCount > 0)
            {
                CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null)
                {
                    cd.hasAttackedThisTurn = false;
                    cd.hasChangedPositionThisTurn = false;
                    cd.summonedThisTurn = false;
                }
            }
        }
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
        if (playerRemovedDisplay != null) playerRemovedDisplay.UpdatePile(playerRemoved, cardBackTexture);
        if (opponentRemovedDisplay != null) opponentRemovedDisplay.UpdatePile(opponentRemoved, cardBackTexture);
    }

    IEnumerator LoadCardBackTexture()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, "YuGiOh_OCG_Classic_2147/0000 - Background.jpg");
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        // Removemos o 'using' para evitar o leak do alocador temporário
        backTextureRequest = UnityWebRequestTexture.GetTexture(url);
        yield return backTextureRequest.SendWebRequest();

        if (backTextureRequest.result == UnityWebRequest.Result.Success)
        {
            cardBackTexture = DownloadHandlerTexture.GetContent(backTextureRequest);
            cardBackTexture.filterMode = FilterMode.Trilinear;
            UpdatePileVisuals(); // Atualiza o visual dos decks caso o duelo já tenha começado
        }
        backTextureRequest.Dispose();
        backTextureRequest = null;
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
    public void EndDuel(bool playerWon, bool isDeckOut = false)
    {
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StopDuelTracking(playerWon, isDeckOut, playerLP);
            
            int score;
            DuelRank rank = DuelScoreManager.Instance.CalculateFinalRank(out score);
            
            Debug.Log($"DUELO FINALIZADO! Venceu: {playerWon} | Rank: {rank} | Pontos: {score}");
            Debug.Log(DuelScoreManager.Instance.GetScoreReport());
            
            // TODO: Chamar UIManager para mostrar tela de Resultado (Vitória/Derrota) com o Rank
        }
    }

    // --- SISTEMA DE DANO E LP ---

    public void DamagePlayer(int amount)
    {
        playerLP -= amount;
        if (playerLP < 0) playerLP = 0;
        UpdateLPUI();
        
        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageTaken(amount);
        Debug.Log($"Player tomou {amount} de dano. LP Restante: {playerLP}");

        // Atualiza a música baseada na nova situação de vida
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.UpdateBGM(playerLP, opponentLP);

        if (playerLP == 0) EndDuel(false);

        // Notifica dano (para cartas como Numinous Healer, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageTaken(true, amount);
        }
    }

    public void DamageOpponent(int amount)
    {
        opponentLP -= amount;
        if (opponentLP < 0) opponentLP = 0;
        UpdateLPUI();

        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageDealt(amount);
        Debug.Log($"Oponente tomou {amount} de dano. LP Restante: {opponentLP}");

        // Atualiza a música baseada na nova situação de vida
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.UpdateBGM(playerLP, opponentLP);

        if (opponentLP == 0) EndDuel(true);

        // Notifica dano
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageTaken(false, amount);
        }
    }

    // --- CONDIÇÕES DE VITÓRIA ESPECIAIS ---

    public void CheckExodiaWin()
    {
        // IDs das 5 partes do Exodia (Baseado no seu JSON)
        string[] exodiaParts = { "0618", "1061", "1062", "1530", "1531" };
        HashSet<string> handIds = new HashSet<string>();
        
        foreach (GameObject cardGO in playerHand)
        {
            CardDisplay display = cardGO.GetComponent<CardDisplay>();
            if (display != null) handIds.Add(display.CurrentCardData.id);
        }

        if (exodiaParts.All(id => handIds.Contains(id)))
        {
            Debug.Log("EXODIA OBLITERATE! VITÓRIA AUTOMÁTICA!");
            // TODO: Tocar animação especial do Exodia aqui
            EndDuel(true);
        }
    }

    private void UpdateLPUI()
    {
        if (duelFieldUI != null)
        {
            if (duelFieldUI.playerLPText != null) duelFieldUI.playerLPText.text = playerLP.ToString();
            if (duelFieldUI.opponentLPText != null) duelFieldUI.opponentLPText.text = opponentLP.ToString();
        }
    }

    // --- LÓGICA DE INVOCACÃO ---

    // Renomeado para TrySummonMonster para indicar que é o início do processo
    public void TrySummonMonster(GameObject cardGO, CardData cardData, bool isSet)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;

        // 0. Validação de Regras de Invocação (SummonManager)
        if (SummonManager.Instance != null)
        {
            // Verifica se pode invocar (Normal Summon, Tributos, etc)
            // Nota: isSpecial = false (por enquanto, invocação da mão é Normal)
            // Passamos o GameObject agora para o SummonManager poder controlar o fluxo manual
            if (!SummonManager.Instance.PerformSummon(cardGO, cardData, isSet, false, isPlayer))
            {
                // Se retornou false, pode ser porque iniciou a seleção manual de tributo
                // ou porque falhou a validação. Em ambos os casos, paramos aqui.
                return;
            }
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning("Ação bloqueada: 'canPlacePlayerCards' está desativado.");
            return;
        }
        if (!isPlayer && !canPlaceOpponentCards)
        {
            Debug.LogWarning("Ação bloqueada: 'canPlaceOpponentCards' está desativado.");
            return;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning("Invocação só é permitida na Main Phase 1 ou 2.");
            return;
        }

        // Se passou por tudo (ou foi auto-tribute), finaliza
        // Normal Summon/Set: Se isSet=true, é Face-Down Defense. Se false, é Face-Up Attack.
        FinalizeSummon(cardGO, cardData, isSet, isPlayer, isSet); 
    }

    // Novo método para Special Summon que pede a posição
    public void PerformSpecialSummon(GameObject cardGO, CardData cardData)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPositionSelection(cardData, (selectedPosition) => 
            {
                bool isDefense = (selectedPosition == CardDisplay.BattlePosition.Defense);
                // Special Summon geralmente é Face-Up, mesmo em defesa
                FinalizeSummon(cardGO, cardData, isDefense, true, false); // false = Face-Up
            });
        }
    }

    // Novo método público para finalizar a invocação (chamado pelo SummonManager após tributo manual)
    // Atualizado para suportar Face-Down explicitamente
    public void FinalizeSummon(GameObject cardGO, CardData cardData, bool isDefensePos, bool isPlayer, bool isFaceDown = false)
    {
        // 2. Encontrar Zona Livre
        Transform targetZone = isPlayer ? GetFreePlayerMonsterZone() : GetFreeOpponentMonsterZone();
        if (targetZone == null)
        {
            Debug.LogWarning("Sem zonas de monstro livres!");
            return;
        }

        // 3. Mover Carta (Lógica de Dados e Visual)
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (isPlayer) playerHand.Remove(cardGO);
        else opponentHand.Remove(cardGO);
        
        cardGO.transform.SetParent(targetZone); // Coloca na zona
        cardGO.transform.localPosition = Vector3.zero; // Centraliza
        cardGO.transform.localScale = fieldCardScale; // Reseta escala para a do campo

        // 4. Configuração Visual (Ataque vs Defesa)
        if (display != null)
        {
            display.isInteractable = false; // Desativa o hover de mão (subir)
            display.isOnField = true;
            
            if (isDefensePos)
            {
                display.position = CardDisplay.BattlePosition.Defense;
                display.summonedThisTurn = true; // Marca invocação
                // Modo Defesa (Set): Virado para baixo e Rotacionado 90 graus
                float zRotation = isPlayer ? 90f : -90f;
                
                if (isFaceDown) display.ShowBack(); else display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }
            else
            {
                display.position = CardDisplay.BattlePosition.Attack;
                display.summonedThisTurn = true; // Marca invocação
                // Modo Ataque: Virado para cima e Reto
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
                
                // Toca efeito visual de invocação
                if (DuelFXManager.Instance != null)
                    DuelFXManager.Instance.PlaySummonEffect(display);
            }
        }

        // 5. Pontuação
        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordSummon();
    }

    private Transform GetFreePlayerMonsterZone()
    {
        if (duelFieldUI == null || duelFieldUI.playerMonsterZones == null) return null;
        foreach (Transform zone in duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount == 0) return zone;
        }
        return null;
    }

    private Transform GetFreeOpponentMonsterZone()
    {
        if (duelFieldUI == null || duelFieldUI.opponentMonsterZones == null) return null;
        foreach (Transform zone in duelFieldUI.opponentMonsterZones)
        {
            if (zone.childCount == 0) return zone;
        }
        return null;
    }

    // --- LÓGICA DE SPELL / TRAP ---

    public void PlaySpellTrap(GameObject cardGO, CardData cardData, bool isSet)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;

        // 0.5 Validação de Armadilha
        if (!devMode && isPlayer && cardData.type.Contains("Trap") && !isSet)
        {
            Debug.LogWarning("Cartas de Armadilha devem ser baixadas (Set) antes de serem ativadas.");
            return;
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning("Ação bloqueada: 'canPlacePlayerCards' está desativado.");
            return;
        }
        if (!isPlayer && !canPlaceOpponentCards)
        {
            Debug.LogWarning("Ação bloqueada: 'canPlaceOpponentCards' está desativado.");
            return;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning("Ativar/Setar Spells só é permitido na Main Phase 1 ou 2.");
            return;
        }

        Transform targetZone = null;

        // 2. Verifica se é Field Spell
        if (cardData.race == "Field")
        {
            if (duelFieldUI != null) 
                targetZone = isPlayer ? duelFieldUI.playerFieldSpell : duelFieldUI.opponentFieldSpell;
        }
        else
        {
            // Encontrar Zona de Spell Livre
            targetZone = isPlayer ? GetFreePlayerSpellZone() : GetFreeOpponentSpellZone();
        }

        if (targetZone == null)
        {
            Debug.LogWarning("Sem zonas de magia/armadilha livres!");
            return;
        }

        // 3. Mover Carta
        if (isPlayer) playerHand.Remove(cardGO);
        else opponentHand.Remove(cardGO);

        cardGO.transform.SetParent(targetZone);
        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;

        // 4. Configuração Visual
        if (display != null)
        {
            display.isInteractable = false;
            display.isOnField = true;
            display.summonedThisTurn = true; // Marca que foi colocada neste turno

            if (isSet)
            {
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowBack();
                // Spells/Traps setadas ficam verticais (não rotacionam como monstros em defesa)
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation); 
            }
            else
            {
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);

                // Efeito de Ativação
                bool isTrap = cardData.type.Contains("Trap");
                if (DuelFXManager.Instance != null)
                    DuelFXManager.Instance.PlayCardActivation(display, isTrap);
                
                if (DuelScoreManager.Instance != null)
                {
                    if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
                    else DuelScoreManager.Instance.RecordSpellActivation();
                }

                // Integração com Sistema de Chains
                if (ChainManager.Instance != null)
                {
                    ChainManager.Instance.AddToChain(display, isPlayer);
                    // Resolve imediatamente por enquanto (sem sistema de resposta manual implementado)
                    ChainManager.Instance.ResolveChain();
                    
                    // Executa o efeito lógico da carta
                    if (CardEffectManager.Instance != null)
                    {
                        CardEffectManager.Instance.ExecuteCardEffect(display);
                    }
                }
            }
        }
    }

    private Transform GetFreePlayerSpellZone()
    {
        if (duelFieldUI == null || duelFieldUI.playerSpellZones == null) return null;
        foreach (Transform zone in duelFieldUI.playerSpellZones)
        {
            if (zone.childCount == 0) return zone;
        }
        return null;
    }

    public void ActivateFieldSpellTrap(GameObject cardGO)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display == null || !display.isOnField) return;

        CardData cardData = display.CurrentCardData;
        bool isPlayer = display.isPlayerCard;

        // Vira a carta para cima
        display.ShowFront();

        // Efeito de Ativação
        bool isTrap = cardData.type.Contains("Trap");
        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayCardActivation(display, isTrap);
        
        if (DuelScoreManager.Instance != null)
        {
            if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
            else DuelScoreManager.Instance.RecordSpellActivation();
        }

        // Integração com Sistema de Chains
        if (ChainManager.Instance != null)
        {
            ChainManager.Instance.AddToChain(display, isPlayer);
            ChainManager.Instance.ResolveChain();
            
            // Executa o efeito lógico da carta
            if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ExecuteCardEffect(display);
            }
        }
    }

    private Transform GetFreeOpponentSpellZone()
    {
        if (duelFieldUI == null || duelFieldUI.opponentSpellZones == null) return null;
        foreach (Transform zone in duelFieldUI.opponentSpellZones)
        {
            if (zone.childCount == 0) return zone;
        }
        return null;
    }

    // --- CONTROLE DE FASE (UI) ---

    public void TryChangePhase(GamePhase newPhase)
    {
        if (PhaseManager.Instance != null) PhaseManager.Instance.TryChangePhase(newPhase);
    }

    // --- GERENCIAMENTO DE DECK (ACESSO PÚBLICO) ---
    
    public List<CardData> GetPlayerMainDeck() { return playerDeck; }
    public List<CardData> GetPlayerSideDeck() { return playerSideDeck; }
    public List<CardData> GetPlayerExtraDeck() { return playerExtraDeck; }
    public List<CardData> GetPlayerGraveyard() { return playerGraveyard; }
    public List<CardData> GetOpponentGraveyard() { return opponentGraveyard; }
    public List<CardData> GetPlayerHandData() { return playerHand.Select(g => g.GetComponent<CardDisplay>().CurrentCardData).ToList(); }
    public List<CardData> GetOpponentHandData() { return opponentHand.Select(g => g.GetComponent<CardDisplay>().CurrentCardData).ToList(); }
    public List<CardData> GetOpponentMainDeck() { return opponentDeck; }

    
    public void SetPlayerDeck(List<CardData> main, List<CardData> side, List<CardData> extra)
    {
        playerDeck = new List<CardData>(main);
        playerSideDeck = new List<CardData>(side);
        playerExtraDeck = new List<CardData>(extra);
    }

    public bool PlayerHasCard(string cardId)
    {
        // Verifica se o jogador tem a carta no baú (ou se está usando todas as cartas)
        if (devMode) return true;
        return playerTrunk.Contains(cardId);
    }

    // --- SISTEMAS DE JOGO AVANÇADOS ---

    // Verifica se uma carta específica está ativa no campo (Face-up)
    // Usado para: Jinzo, Gravity Bind, Umi, etc.
    public bool IsCardActiveOnField(string cardId)
    {
        if (duelFieldUI == null) return false;

        bool CheckZone(Transform[] zones)
        {
            foreach (var z in zones)
            {
                if (z.childCount > 0)
                {
                    var c = z.GetChild(0).GetComponent<CardDisplay>();
                    if (c != null && c.isOnField && !c.isFlipped && c.CurrentCardData.id == cardId) return true;
                }
            }
            return false;
        }

        if (CheckZone(duelFieldUI.playerSpellZones)) return true;
        if (CheckZone(duelFieldUI.opponentSpellZones)) return true;
        if (CheckZone(duelFieldUI.playerMonsterZones)) return true; // Jinzo é monstro
        if (CheckZone(duelFieldUI.opponentMonsterZones)) return true;
        
        // Checa Field Spells
        if (duelFieldUI.playerFieldSpell.childCount > 0)
        {
            var c = duelFieldUI.playerFieldSpell.GetChild(0).GetComponent<CardDisplay>();
            if (c != null && !c.isFlipped && c.CurrentCardData.id == cardId) return true;
        }
        if (duelFieldUI.opponentFieldSpell.childCount > 0)
        {
            var c = duelFieldUI.opponentFieldSpell.GetChild(0).GetComponent<CardDisplay>();
            if (c != null && !c.isFlipped && c.CurrentCardData.id == cardId) return true;
        }

        return false;
    }

    // Sistema de Tokens (Scapegoat, etc)
    public void SpawnToken(bool forPlayer, int atk, int def, string name)
    {
        Transform targetZone = forPlayer ? GetFreePlayerMonsterZone() : GetFreeOpponentMonsterZone();
        if (targetZone == null) return; // Campo cheio

        GameObject tokenGO = Instantiate(tokenPrefab != null ? tokenPrefab : cardPrefab, targetZone);
        CardDisplay display = tokenGO.GetComponent<CardDisplay>();
        if (display == null) display = tokenGO.AddComponent<CardDisplay>();

        // Cria dados fictícios para o Token
        CardData tokenData = new CardData();
        tokenData.id = "TOKEN";
        tokenData.name = name;
        tokenData.type = "Monster (Normal)";
        tokenData.race = "Token"; // Ou Beast, etc, dependendo do efeito
        tokenData.atk = atk;
        tokenData.def = def;
        tokenData.level = 1;
        tokenData.description = "Token Monster";

        display.isPlayerCard = forPlayer;
        display.isOnField = true;
        display.position = CardDisplay.BattlePosition.Defense;
        display.SetCard(tokenData, cardBackTexture, true); // Face-up

        // Configuração Visual
        tokenGO.transform.localPosition = Vector3.zero;
        tokenGO.transform.localScale = fieldCardScale;
        float zRotation = forPlayer ? 90f : -90f; // Defesa
        tokenGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySummonEffect(display);
    }

    // Troca de Controle (Change of Heart, Snatch Steal)
    public void SwitchControl(CardDisplay card)
    {
        // 0207 - Blindly Loyal Goblin
        if (card.CurrentCardData.id == "0207")
        {
            Debug.Log("Blindly Loyal Goblin: Imune a troca de controle.");
            return;
        }

        bool newOwnerIsPlayer = !card.isPlayerCard;
        Transform newZone = newOwnerIsPlayer ? GetFreePlayerMonsterZone() : GetFreeOpponentMonsterZone();

        if (newZone == null)
        {
            Debug.LogWarning("Sem espaço para trocar o controle do monstro.");
            return;
        }

        // Remove da lista da mão se por acaso estiver lá (segurança)
        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        card.transform.SetParent(newZone);
        card.transform.localPosition = Vector3.zero;
        card.isPlayerCard = newOwnerIsPlayer;

        // Ajusta rotação visual baseada na posição de batalha e novo dono
        float zRot = 0;
        if (card.position == CardDisplay.BattlePosition.Defense) zRot = newOwnerIsPlayer ? 90f : -90f;
        else zRot = newOwnerIsPlayer ? 0f : 180f;

        card.transform.localRotation = Quaternion.Euler(0, 0, zRot);
        Debug.Log($"Controle de {card.CurrentCardData.name} alterado.");
    }

    // Método para Seleção Única (Mantido para compatibilidade, redireciona para Multi)
    public void OpenCardSelection(List<CardData> sourceList, string title, System.Action<CardData> onSelected)
    {
        OpenCardMultiSelection(sourceList, title, 1, 1, (selectedList) => {
            if (selectedList != null && selectedList.Count > 0)
                onSelected?.Invoke(selectedList[0]);
        });
    }

    // Método para Seleção Múltipla
    public void OpenCardMultiSelection(List<CardData> sourceList, string title, int min, int max, System.Action<List<CardData>> onSelected)
    {
        if (sourceList.Count > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowCardSelection(sourceList, title, min, max, onSelected);
            }
            else
            {
                Debug.LogError("GameManager: UIManager não encontrado para seleção de cartas.");
            }
        }
        else
        {
            Debug.Log("Nenhuma carta válida para selecionar.");
        }
    }

    // Invocação Especial direta por dados (para Monster Reborn, etc)
    public void SpecialSummonFromData(CardData data, bool forPlayer, bool faceUp = true, bool defense = false)
    {
        Transform targetZone = forPlayer ? GetFreePlayerMonsterZone() : GetFreeOpponentMonsterZone();
        if (targetZone == null) return;

        GameObject cardGO = Instantiate(cardPrefab, targetZone);
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display == null) display = cardGO.AddComponent<CardDisplay>();

        display.isPlayerCard = forPlayer;
        display.isOnField = true;
        display.position = defense ? CardDisplay.BattlePosition.Defense : CardDisplay.BattlePosition.Attack;
        display.SetCard(data, cardBackTexture, faceUp);

        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;
        
        float zRot = 0;
        if (defense) zRot = forPlayer ? 90f : -90f;
        else zRot = forPlayer ? 0f : 180f;
        
        cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRot);

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySummonEffect(display);

        // Notifica invocação especial (para Card of Safe Return, etc)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnSpecialSummon(display);
        }
    }

    // Ferramenta de Dev: Visualizar Deck
    public void ViewDeck(bool isPlayer)
    {
        if (UIManager.Instance == null) return;

        // Se for oponente e não estiver em modo Dev, bloqueia (a menos que uma carta permita, mas aí seria via efeito específico)
        if (!isPlayer && !devMode) return;

        List<CardData> deck = isPlayer ? playerDeck : opponentDeck;
        UIManager.Instance.ShowDeck(deck, cardBackTexture);
    }

    // Helper para o EffectTestManager e outros sistemas
    public Texture2D GetCardBackTexture()
    {
        return cardBackTexture;
    }

    void OnDisable()
    {
        // Garante que a requisição seja cancelada se o objeto for desativado
        if (backTextureRequest != null && !backTextureRequest.isDone)
        {
            backTextureRequest.Dispose();
            backTextureRequest = null;
        }
    }

    void OnDestroy()
    {
        // Garante que a requisição seja limpa se o jogo parar abruptamente
        if (backTextureRequest != null)
        {
            backTextureRequest.Dispose();
            backTextureRequest = null;
        }

    }


    public void OnBattlePositionChanged(CardDisplay card)
    {
        // Notifica o sistema de efeitos
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnBattlePositionChanged(card);
        }
                // Notifica sistema de efeitos (Ex: Fire Princess)
        CardEffectManager.Instance.OnDamageDealt(attacker, target, amount);
    }



}
