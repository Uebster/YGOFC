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
    [Tooltip("Ativa o contorno no visualizador de carta grande.")]


    public bool enableCardViewerOutline = true;    
    
    [Header("Pile Outlines")]
    [Tooltip("Ativa o contorno de hover nas cartas do Deck.")]
    public bool enableDeckHoverOutline = false;
    [Tooltip("Ativa o contorno de hover nas cartas do Cemitério.")]
    public bool enableGraveyardHoverOutline = true;
    [Tooltip("Ativa o contorno de hover nas cartas do Extra Deck.")]
    public bool enableExtraDeckHoverOutline = true;
    [Tooltip("Ativa o contorno de hover nas cartas Removidas.")]
    public bool enableRemovedHoverOutline = true;

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
    [Tooltip("ID do oponente para teste rápido (ex: 021_kaiba). Deixe vazio para usar o fluxo normal.")]
    public string testOpponentID = ""; 
    [Tooltip("ID do personagem para substituir o jogador (ex: 020_pegasus). Deixe vazio para usar o deck do save.")]
    public string testPlayerID = "";
    [Tooltip("Número do Ato (1-10) para forçar o tema visual. -1 para usar o padrão.")]
    public int testActThemeIndex = -1;
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
    public PileDisplay playerGraveyardDisplay;
    public PileDisplay opponentGraveyardDisplay;
    public PileDisplay playerExtraDeckDisplay;
    public PileDisplay opponentExtraDeckDisplay;
    public PileDisplay playerRemovedDisplay; // Novo
    public PileDisplay opponentRemovedDisplay; // Novo

    // Propriedades de compatibilidade para acesso via DeckManager (Corrige erro no CardEffectManager)
    public PileDisplay playerDeckDisplay => DeckManager.Instance != null ? DeckManager.Instance.playerDeckDisplay : null;
    public PileDisplay opponentDeckDisplay => DeckManager.Instance != null ? DeckManager.Instance.opponentDeckDisplay : null;

    // Adicionado: Side Deck e Baú (Trunk)
    public List<CardData> playerMainDeck = new List<CardData>(); // Deck principal persistente
    public List<CardData> opponentMainDeck = new List<CardData>(); // Deck oponente persistente
    private List<CardData> playerSideDeck = new List<CardData>();
    public List<string> playerTrunk = new List<string>(); // IDs das cartas que o jogador possui

    public List<GameObject> playerHand = new List<GameObject>();
    private List<CardData> playerGraveyard = new List<CardData>();
    public List<GameObject> opponentHand = new List<GameObject>();
    private List<CardData> opponentGraveyard = new List<CardData>();
    private List<CardData> playerExtraDeck = new List<CardData>();
    public List<CardData> opponentExtraDeck = new List<CardData>(); // Changed to public
    private List<CardData> playerRemoved = new List<CardData>(); // Novo
    private List<CardData> opponentRemoved = new List<CardData>(); // Novo

    // Lista de nomes de cartas proibidas para o duelo atual (ex: Cursed Seal)
    public List<string> forbiddenSpells = new List<string>();
    public List<string> prohibitedCards = new List<string>(); // Prohibition (1460)

    [Header("Current Duel Info")]
    public CharacterData currentOpponent; // Oponente atual carregado
    public int currentDuelIndex = -1; // Índice do duelo atual na campanha (para salvar progresso)

    [Header("Player Profile")]
    public string playerName = "Duelist";
    public string currentSaveID = "default";
    public int playerLP = 8000;
    public int opponentLP = 8000;
    public int turnCount = 0; // Contagem de turnos
    public bool isPlayerTurn = true; // Rastreia de quem é o turno

    [Header("Runtime Theme Settings")]
    public bool isDuelOver = false;
    public Color playerHoverColor = new Color(0.5f, 1f, 0.5f, 1f); // Verde claro
    public Color opponentHoverColor = Color.yellow;
    [Header("Phase Indicator Settings")]
    public bool enablePhaseHoverEffect = true;
    public Color phaseHoverColorPlayer = new Color(0.5f, 1f, 0.5f, 0.5f); // Verde semitransparente
    public Color phaseHoverColorOpponent = new Color(1f, 0.92f, 0.016f, 0.5f); // Amarelo semitransparente

    [Header("Visual Feedback")]
    [Tooltip("Habilita o efeito de escurecer a carta ao selecioná-la para atacar.")]
    public bool enableAttackSelectionVisual = true;
    public Color attackSelectionColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [Tooltip("Habilita a animação de projétil (espada) durante o ataque.")]
    public bool enableAttackAnimation = true;
    public GameObject attackAnimationPrefab; // Prefab da espada/projétil

    public bool revealOpponentDraw = false; // Pikeru's Second Sight (1431)
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
            foreach (var card in cardDatabase.cardDatabase)
                playerTrunk.Add(card.id);
        }

        yield return StartCoroutine(LoadCardBackTexture());

        // FIX: Inicializa o deck automaticamente se estiver vazio (para testar o Draw imediatamente)
        if (DeckManager.Instance != null && DeckManager.Instance.GetPlayerDeck().Count == 0)
        {
            InitializePlayerDeck();
            InitializeOpponentDeck();
            yield return StartCoroutine(DrawInitialHandRoutine(5));
            yield return StartCoroutine(DrawInitialOpponentHandRoutine(5)); // Garante que o oponente comece com cartas
        }
    }

    public void RemoveCardFromHand(CardData card, bool isPlayer)
    {
        if (card == null) return;
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        GameObject cardToRemove = hand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == card);
        if (cardToRemove != null)
        {
            hand.Remove(cardToRemove);
            Destroy(cardToRemove);
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

        List<CardData> pDeck = InitializePlayerDeck();
        (List<CardData> oMain, List<CardData> oExtra) = InitializeOpponentDeck();
        DeckManager.Instance.SetupDecks(pDeck, playerExtraDeck, oMain, oExtra);

        // DrawInitialHand(5); // Substituído pela sequência de corrotina
        // DrawInitialOpponentHand(5);

        // Inicializa LP
        playerLP = 8000;
        opponentLP = 8000;
        turnCount = 0;
        isDuelOver = false;
        UpdateLPUI();

        // Reseta flag de draw antes de começar o turno
        if (DeckManager.Instance != null) DeckManager.Instance.ResetTurnStats();

        // PhaseManager.Instance.StartTurn() agora é chamado dentro de DuelStartSequence

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

            // DEV MODE: Sobrescreve o tema se um Ato de teste for definido
            if (devMode && testActThemeIndex > 0)
            {
                levelToUse = (testActThemeIndex - 1) * 10 + 1; // Ato 1 = Nível 1, Ato 2 = Nível 11...
            }

            DuelTheme theme = campaignDatabase.GetThemeForLevel(levelToUse);
            if (theme != null)
                DuelThemeManager.Instance.ApplyTheme(theme);
            }
                StartCoroutine(DuelStartSequence());    
        }
    private IEnumerator DuelStartSequence()
    {
        yield return new WaitForSeconds(0.5f); // Delay inicial para respirar
        yield return StartCoroutine(DrawInitialHandRoutine(5));
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(DrawInitialOpponentHandRoutine(5));
        yield return new WaitForSeconds(1.0f); // Pausa dramática antes de começar

        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();
        else Debug.LogError("PhaseManager não encontrado mesmo após tentativa de criação!");
    }

    // Cria automaticamente os gerenciadores se eles não estiverem na cena
    void EnsureCoreManagers()
    {
        if (PhaseManager.Instance == null) CreateManager<PhaseManager>();
        if (SummonManager.Instance == null) CreateManager<SummonManager>();
        if (BattleManager.Instance == null) CreateManager<BattleManager>();
        if (SpellTrapManager.Instance == null) CreateManager<SpellTrapManager>();
        if (ChainManager.Instance == null) CreateManager<ChainManager>();
        if (SpellCounterManager.Instance == null) CreateManager<SpellCounterManager>();
        if (CardEffectManager.Instance == null) CreateManager<CardEffectManager>();
        if (OpponentAI.Instance == null) CreateManager<OpponentAI>(); // Garante que a IA exista
        if (DeckManager.Instance == null) CreateManager<DeckManager>(); // Garante que o DeckManager exista

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
        playerGraveyard.Clear();
        opponentGraveyard.Clear();
        playerExtraDeck.Clear();
        playerSideDeck.Clear();
        opponentExtraDeck.Clear();
        playerRemoved.Clear();
        opponentRemoved.Clear();
        forbiddenSpells.Clear();

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

    List<CardData> InitializePlayerDeck()
    {
        // DEV MODE: Sobrescreve o deck do jogador com o de um personagem
        if (devMode && !string.IsNullOrEmpty(testPlayerID) && characterDatabase != null)
        {
            CharacterData charData = characterDatabase.GetCharacterById(testPlayerID);
            if (charData != null)
            {
                Debug.Log($"[DevMode] Jogando como: {charData.name} ({charData.id})");
                List<CardData> devDeck = new List<CardData>();
                
                // Usa o Deck A do personagem como padrão para o jogador
                if (charData.deck_A != null)
                {
                    foreach (string id in charData.deck_A)
                    {
                        CardData c = cardDatabase.GetCardById(id);
                        if (c != null) devDeck.Add(c);
                    }
                }
                
                playerExtraDeck.Clear();
                if (charData.extra_deck_A != null)
                {
                    foreach (string id in charData.extra_deck_A)
                    {
                        CardData c = cardDatabase.GetCardById(id);
                        if (c != null) playerExtraDeck.Add(c);
                    }
                }

                playerMainDeck = devDeck;
                UpdatePileVisuals();
                return playerMainDeck;
            }
            else
            {
                Debug.LogWarning($"[DevMode] Personagem {testPlayerID} não encontrado. Usando deck do save.");
            }
        }

        // Se já temos um deck carregado (do Save ou anterior), usamos ele
        if (playerMainDeck.Count > 0)
        {
            UpdatePileVisuals();
            return playerMainDeck;
        }

        List<CardData> newDeck = new List<CardData>();
        // Verifica se já existe um deck salvo no SaveLoadSystem
        // Se não, gera um novo
        if (InitialDeckBuilder.Instance != null)
        {
            Debug.Log("Gerando Deck Inicial Único...");
            List<CardData> generatedDeck = InitialDeckBuilder.Instance.GenerateInitialDeck();

            playerExtraDeck.Clear(); // Deck inicial não tem fusões prontas

            foreach (var card in generatedDeck)
            {
                newDeck.Add(card);
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
        }

        // Salva o jogo imediatamente para persistir o deck gerado
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame(currentSaveID);
        }

        playerMainDeck = newDeck;
        UpdatePileVisuals();
        return newDeck;
    }

    (List<CardData>, List<CardData>) InitializeOpponentDeck()
    {
        List<CardData> newDeck = new List<CardData>();

        // Se o oponente ainda for nulo aqui (não foi setado no StartDuel), tenta o ID de teste novamente como última tentativa
        if (currentOpponent == null && !string.IsNullOrEmpty(testOpponentID) && characterDatabase != null)
        {
            currentOpponent = characterDatabase.GetCharacterById(testOpponentID);
        }

        // Se temos um oponente definido (Campanha), carregamos o deck dele
        if (currentOpponent != null)
        {
            // Atualiza visual do nome na UI (se disponível)
            if (duelFieldUI != null && duelFieldUI.opponentNameText != null)
                duelFieldUI.opponentNameText.text = currentOpponent.name;

            // --- SELEÇÃO DE DECK ALEATÓRIA (A, B, C) ---
            List<string> selectedMain = currentOpponent.deck_A;
            List<string> selectedExtra = currentOpponent.extra_deck_A;
            string variant = "A";

            int rng = Random.Range(0, 3); // 0, 1, 2

            // Tenta carregar B ou C se sorteado e existente
            if (rng == 1 && currentOpponent.deck_B != null && currentOpponent.deck_B.Count > 0)
            {
                selectedMain = currentOpponent.deck_B;
                selectedExtra = currentOpponent.extra_deck_B;
                variant = "B";
            }
            else if (rng == 2 && currentOpponent.deck_C != null && currentOpponent.deck_C.Count > 0)
            {
                selectedMain = currentOpponent.deck_C;
                selectedExtra = currentOpponent.extra_deck_C;
                variant = "C";
            }

            Debug.Log($"Inicializando oponente: {currentOpponent.name} | Deck Variante: {variant}");

            // Carrega Main Deck
            if (selectedMain != null) 
            {
                foreach (string id in selectedMain) 
                { 
                    CardData c = cardDatabase.GetCardById(id); 
                    if (c != null) newDeck.Add(c); 
                    // else Debug.LogWarning($"Carta ID {id} não encontrada para o oponente.");
                }
            }
            // Carrega Extra Deck
            opponentExtraDeck.Clear();
            if (selectedExtra != null) foreach (string id in selectedExtra) { CardData c = cardDatabase.GetCardById(id); if (c != null) opponentExtraDeck.Add(c); }
        }
        else
        {
            // Fallback: Se não tiver oponente (Free Duel genérico), carrega tudo ou aleatório
            Debug.Log("Nenhum oponente específico definido. Carregando banco de dados inteiro (Modo Teste).");
            foreach (var card in cardDatabase.cardDatabase)
            {
                if (card.type.Contains("Fusion"))
                {
                    // Fallback should also respect extra_deck
                }
                else
                {
                    newDeck.Add(card);
                }
            }
        }

        // SAFEGUARD CRÍTICO: Se o deck ainda estiver vazio (falha no load ou IDs errados), gera um aleatório
        if (newDeck.Count == 0)
        {
            Debug.LogWarning("Deck do oponente vazio após inicialização! Gerando deck de emergência.");
            List<CardData> allCards = new List<CardData>(cardDatabase.cardDatabase);
            var validCards = allCards.Where(c => !c.type.Contains("Fusion") && !c.type.Contains("Synchro") && !c.type.Contains("Xyz") && !c.type.Contains("Token")).ToList();

            for (int i = 0; i < 40; i++)
            {
                if (validCards.Count == 0) break;
                CardData randomCard = validCards[Random.Range(0, validCards.Count)];
                newDeck.Add(randomCard);
            }
        }

                // Safeguard para garantir que apenas fusões estejam no Extra Deck do oponente
        var oppNonFusionsInExtra = opponentExtraDeck.Where(c => !c.type.Contains("Fusion")).ToList();
        if (oppNonFusionsInExtra.Any())
        {
            Debug.LogWarning($"Safeguard (Oponente): {oppNonFusionsInExtra.Count} carta(s) não-fusão encontradas no Extra Deck. Movendo para o Deck Principal.");
            newDeck.AddRange(oppNonFusionsInExtra);
            opponentExtraDeck.RemoveAll(c => !c.type.Contains("Fusion"));
        }
        var oppFusionsInMain = newDeck.Where(c => c.type.Contains("Fusion")).ToList();
        if (oppFusionsInMain.Any())
        {
            Debug.LogWarning($"Safeguard (Oponente): {oppFusionsInMain.Count} carta(s) de fusão encontradas no Deck Principal. Movendo para o Extra Deck.");
            opponentExtraDeck.AddRange(oppFusionsInMain);
            newDeck.RemoveAll(c => c.type.Contains("Fusion"));
        }
        
        // Shuffle logic moved to DeckManager, but we shuffle the list here before passing
        // Actually DeckManager.SetupDecks doesn't shuffle, so we can shuffle here or call ShuffleDeck later.
        // Let's shuffle the list here.
        for (int i = 0; i < newDeck.Count; i++) { CardData temp = newDeck[i]; int r = Random.Range(i, newDeck.Count); newDeck[i] = newDeck[r]; newDeck[r] = temp; }
        
        opponentMainDeck = newDeck;
        Debug.Log($"Deck do oponente inicializado com {newDeck.Count} cartas.");
        UpdatePileVisuals();
        return (opponentMainDeck, opponentExtraDeck);
    }

        public IEnumerator DrawInitialHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(true); // Ignora o limite para a mão inicial
            yield return new WaitForSeconds(0.5f); // Delay aumentado para melhor visualização
        }
    }
    public IEnumerator DrawInitialOpponentHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
           DrawOpponentCard();            
           yield return new WaitForSeconds(0.3f); // Oponente compra um pouco mais rápido
        }
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
        if (SpellCounterManager.Instance != null) SpellCounterManager.Instance.OnCardLeavesField(card);

        // Destrói o objeto visual (Banish não vai pro GY, então não chama SendToGraveyard)
        Destroy(card.gameObject);
    }

public void ShuffleDeck(bool isPlayer)
{
    if (DeckManager.Instance != null) DeckManager.Instance.ShuffleDeck(isPlayer);
}
    public void ShuffleOpponentDeck()
    {
        if (DeckManager.Instance != null) DeckManager.Instance.ShuffleDeck(false);
    }

    public void MillCards(bool isPlayer, int amount)
    {
        if (DeckManager.Instance != null) DeckManager.Instance.MillCards(isPlayer, amount);
    }

    public void DiscardCard(CardDisplay card)
    {
        if (card == null) return;

        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        SendToGraveyard(card.CurrentCardData, card.isPlayerCard, CardLocation.Hand, SendReason.Discarded);

        // Remove modificadores (caso raro de efeito na mão, mas seguro)
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardDiscarded(card);
        if (SpellCounterManager.Instance != null) SpellCounterManager.Instance.OnCardLeavesField(card);

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

    // Novo método para Mind Crush e similares
    public void DiscardCardsByName(bool isPlayer, string cardName)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        // Itera de trás para frente para remover com segurança
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            GameObject go = hand[i];
            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd != null && cd.CurrentCardData.name == cardName)
            {
                DiscardCard(cd);
            }
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
        if (SpellCounterManager.Instance != null) SpellCounterManager.Instance.OnCardLeavesField(card);

        // Destrói objeto do campo
        Destroy(card.gameObject);

        // Adiciona à mão
        AddCardToHand(data, isPlayer);
        Debug.Log($"{data.name} retornada para a mão.");
    }

    public void ReturnToDeck(CardDisplay card, bool toTop)
    {
        if (DeckManager.Instance != null) DeckManager.Instance.ReturnToDeck(card, toTop);
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

        // Notifica adição à mão (Watapon)
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardAddedToHand(newCardDisplay);
    }

    public void DrawCard(bool ignoreLimit = false)
    {
        if (DeckManager.Instance != null) DeckManager.Instance.DrawCard(true, ignoreLimit);
        
        // A lógica de fase foi movida para o DeckManager, mas precisamos garantir que o PhaseManager seja chamado lá.
        // DeckManager chama PhaseManager.ChangePhase(GamePhase.Standby) se for Draw Phase.
        
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Draw;
        if (!ignoreLimit && currentPhase == GamePhase.Draw && PhaseManager.Instance != null)
        {
            // Avança para Standby via PhaseManager
            PhaseManager.Instance.ChangePhase(GamePhase.Standby);
        }
    }

    public void DrawOpponentCard()
    {
        if (DeckManager.Instance != null) DeckManager.Instance.DrawCard(false);
    }

    public void DrawInitialHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(true); // Ignora o limite para a mão inicial
        }
    }

    // Método para enviar carta para o cemitério (Exemplo de uso)
    public void SendToGraveyard(CardData card, bool isPlayer, CardLocation fromLocation = CardLocation.Unknown, SendReason reason = SendReason.Unknown)
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

        // Nota: A remoção de modificadores é feita no método Destroy/OnCardLeavesField do CardDisplay, não aqui.

        // Notifica o sistema de efeitos (Gatilhos Globais)
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnCardSentToGraveyard(card, isPlayer, fromLocation, reason);
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

    // Chamado para trocar o turno (ex: botão End Phase ou automático)
    public void SwitchTurn()
    {
        isPlayerTurn = !isPlayerTurn;
        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();

        // Atualiza as cores de hover dos botões de fase para o turno atual
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.UpdateHoverColors(isPlayerTurn);
        }
        
        // Se for turno do oponente, inicia a IA
        if (!isPlayerTurn && OpponentAI.Instance != null)
        {
            OpponentAI.Instance.StartAITurn();
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
        if (DeckManager.Instance != null) DeckManager.Instance.ResetTurnStats();
        
        if (isPlayerTurn)
        {
            if (!canPlayerDrawFromDeck)
            {
                // Verifica exceções de Draw via SpellTrapManager
                int draws = 1;
                if (SpellTrapManager.Instance != null) draws += SpellTrapManager.Instance.extraDrawsPerTurn;

                for (int i = 0; i < draws; i++)
                    DrawCard();
            }
        }
        else
        {
            // Turno do Oponente: Saca automaticamente
            DrawOpponentCard();
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

    // Chamado pelo PhaseManager quando entra na End Phase
    public void OnEndPhaseStart()
    {
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnPhaseStart(GamePhase.End);
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
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Jogando moeda {i + 1}...");
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
        if (DeckManager.Instance != null) DeckManager.Instance.UpdateDeckVisuals();
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
        if (isDuelOver) return; // Previne chamadas múltiplas
        isDuelOver = true;

        StartCoroutine(EndDuelRoutine(playerWon, isDeckOut));
    }

    private IEnumerator EndDuelRoutine(bool playerWon, bool isDeckOut)
    {
        // 1. Mostra a mensagem de WIN/LOSE
        if (UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            TextMeshProUGUI messageText = UIManager.Instance.endDuelMessagePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (messageText != null)
            {
                messageText.text = playerWon ? "YOU WIN" : "YOU LOSE";
                messageText.color = playerWon ? Color.yellow : new Color(1f, 0.2f, 0.2f);
            }
            UIManager.Instance.endDuelMessagePanel.SetActive(true);
        }

        // Pausa dramática
        yield return new WaitForSeconds(2.5f);

        // 2. Esconde a mensagem
        if (UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            UIManager.Instance.endDuelMessagePanel.SetActive(false);
        }

        // 3. Calcula pontuação e recompensas
        if (DuelScoreManager.Instance != null)
        {
            DuelScoreManager.Instance.StopDuelTracking(playerWon, isDeckOut, playerLP);

            int score;
            DuelRank rank = DuelScoreManager.Instance.CalculateFinalRank(out score);
            Debug.Log($"DUELO FINALIZADO! Venceu: {playerWon} | Rank: {rank} | Pontos: {score}");
            Debug.Log(DuelScoreManager.Instance.GetScoreReport());

            CardData rewardCard = null;
            if (playerWon && currentOpponent != null && currentOpponent.rewards.Count > 0)
            {
                string rewardId = currentOpponent.rewards[Random.Range(0, currentOpponent.rewards.Count)];
                rewardCard = cardDatabase.GetCardById(rewardId);
                Debug.Log($"Recompensa: {rewardCard?.name ?? "Nenhuma"}");
            }
            
            RewardPanelUI rewardPanel = FindFirstObjectByType<RewardPanelUI>(FindObjectsInactive.Include);
            if (rewardPanel != null)
            {
                rewardPanel.Show(rank.ToString(), rewardCard);
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.Btn_BackToMenu();
            }
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.Btn_BackToMenu();
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

        if (playerLP <= 0) EndDuel(false);

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

        if (opponentLP <= 0) EndDuel(true);

        // Notifica dano
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnDamageTaken(false, amount);
        }

        // Notifica dano de batalha causado (para Robbin' Goblin, etc)
        if (CardEffectManager.Instance != null && BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
        {
            CardEffectManager.Instance.OnDamageDealt(BattleManager.Instance.currentAttacker, null, amount);
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

    // Helper para contar cartas no campo de um jogador
    public int GetFieldCardCount(bool isPlayer)
    {
        int count = 0;
        if (duelFieldUI != null)
        {
            Transform[] monsterZones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
            Transform[] spellZones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
            Transform fieldZone = isPlayer ? duelFieldUI.playerFieldSpell : duelFieldUI.opponentFieldSpell;

            foreach (var z in monsterZones) if (z.childCount > 0) count++;
            foreach (var z in spellZones) if (z.childCount > 0) count++;
            if (fieldZone.childCount > 0) count++;
        }
        return count;
    }

    // --- LÓGICA DE INVOCACÃO ---

    // Renomeado para TrySummonMonster para indicar que é o início do processo
    public void TrySummonMonster(GameObject cardGO, CardData cardData, bool isSet, bool ignoreLimit = false)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;

        // Validações de Efeitos Contínuos
        if (CardEffectManager.Instance != null)
        {
            if (!CardEffectManager.Instance.CheckChainEnergy(isPlayer)) return;
            if (!CardEffectManager.Instance.CheckSpatialCollapse(isPlayer)) return;
            if (!CardEffectManager.Instance.CheckRivalryOfWarlords(isPlayer, cardData.race)) return;
        }

        // 0. Validação de Regras de Invocação (SummonManager)
        if (SummonManager.Instance != null)
        {
            // Verifica se pode invocar (Normal Summon, Tributos, etc)
            // Nota: isSpecial = false (por enquanto, invocação da mão é Normal)
            // Passamos o GameObject agora para o SummonManager poder controlar o fluxo manual
            if (!SummonManager.Instance.PerformSummon(cardGO, cardData, isSet, false, isPlayer, ignoreLimit))
            {
                // Se retornou false, pode ser porque iniciou a seleção manual de tributo
                // ou porque falhou a validação. Em ambos os casos, paramos aqui.
                return;
            }
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning("Ação do jogador bloqueada: 'canPlacePlayerCards' está desativado.");
            return;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        // A IA (que roda no turno do oponente) não será bloqueada por esta verificação.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards)
        {
            Debug.LogWarning("Ação de controle do oponente bloqueada: 'canPlaceOpponentCards' está desativado.");
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
        // Nota: Se houve tributo automático, SummonManager deveria ter lidado, mas aqui assumimos fluxo simples ou 0 tributos
        bool wasTribute = (cardData.level >= 5); // Simplificação para Auto-Tribute implícito se passou pelo SummonManager
        FinalizeSummon(cardGO, cardData, isSet, isPlayer, isSet, wasTribute);
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
    public void FinalizeSummon(GameObject cardGO, CardData cardData, bool isDefensePos, bool isPlayer, bool isFaceDown = false, bool isTributeSummon = false)
    {
        // 2. Encontrar Zona Livre
        Transform targetZone = GetFreeMonsterZone(isPlayer);
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
                display.isTributeSummoned = isTributeSummon;
                // Modo Defesa (Set): Virado para baixo e Rotacionado 90 graus
                float zRotation = isPlayer ? 90f : -90f;

                if (isFaceDown) display.ShowBack(); else display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }
            else
            {
                display.position = CardDisplay.BattlePosition.Attack;
                display.summonedThisTurn = true; // Marca invocação
                display.isTributeSummoned = isTributeSummon;
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

    public Transform GetFreeMonsterZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        if (zones == null) return null;

        foreach (Transform zone in zones)
        {
            if (zone.childCount == 0) return zone;
        }
        return null;
    }

    // --- SISTEMA DE RITUAL ---

    public void BeginRitualSummon(CardDisplay sourceCard)
    {
        // 1. Verifica se tem Monstros de Ritual na mão
        var ritualMonsters = GetPlayerHandData().Where(c => c.type.Contains("Ritual")).ToList();
        if (ritualMonsters.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Ritual na mão.");
            return;
        }

        // 2. Abre a UI de Ritual
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowRitualUI(sourceCard);
        }
    }

    public void PerformRitualSummon(CardDisplay sourceCard, CardData ritualMonster, List<CardData> tributes)
    {
        if (ritualMonster == null || tributes == null || tributes.Count == 0)
        {
            Debug.LogError("PerformRitualSummon: Dados inválidos.");
            return;
        }

        Debug.Log($"Realizando Invocação-Ritual de {ritualMonster.name}...");

        // 1. Envia a Magia de Ritual para o cemitério
        SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Effect);
        Destroy(sourceCard.gameObject);

        // 2. Envia os tributos para o cemitério (da mão ou do campo)
        foreach (var tribute in tributes)
        {
            var handObject = playerHand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == tribute);
            if (handObject != null)
            {
                DiscardCard(handObject.GetComponent<CardDisplay>());
            }
            else
            {
                var fieldObject = FindCardOnField(tribute.id, true);
                if (fieldObject != null) TributeCard(fieldObject);
            }
        }

        // 3. Invoca o Monstro de Ritual da mão
        RemoveCardFromHand(ritualMonster, sourceCard.isPlayerCard);
        SpecialSummonFromData(ritualMonster, sourceCard.isPlayerCard);
    }

    // --- LÓGICA DE SPELL / TRAP ---

    public void PlaySpellTrap(GameObject cardGO, CardData cardData, bool isSet)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;

        // Validações de Efeitos Contínuos
        if (CardEffectManager.Instance != null)
        {
            if (!CardEffectManager.Instance.CheckChainEnergy(isPlayer)) return;
            if (!CardEffectManager.Instance.CheckSpatialCollapse(isPlayer)) return;
        }

        // 0.5 Validação de Armadilha
        if (!devMode && isPlayer && cardData.type.Contains("Trap") && !isSet)
        {
            Debug.LogWarning("Cartas de Armadilha devem ser baixadas (Set) antes de serem ativadas.");
            return;
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning("Ação do jogador bloqueada: 'canPlacePlayerCards' está desativado.");
            return;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards)
        {
            Debug.LogWarning("Ação de controle do oponente bloqueada: 'canPlaceOpponentCards' está desativado.");
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
            targetZone = GetFreeSpellZone(isPlayer);
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
                    // NÃO resolve imediatamente. O AddToChain agora chama CheckForResponse,
                    // que eventualmente chamará ResolveChain.
                    // A execução do efeito (ExecuteCardEffect) foi movida para dentro
                    // do loop de resolução do ChainManager.
                }
            }
        }
    }

    public Transform GetFreeSpellZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        if (zones == null) return null;

        foreach (Transform zone in zones)
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
            // A resolução e execução agora são controladas pelo ChainManager
            // após verificar respostas.
        }
    }

    // --- CONTROLE DE FASE (UI) ---

    public void TryChangePhase(GamePhase newPhase)
    {
        if (PhaseManager.Instance != null) PhaseManager.Instance.TryChangePhase(newPhase);
    }

    // --- GERENCIAMENTO DE DECK (ACESSO PÚBLICO) ---

    public List<CardData> GetPlayerSideDeck() { return playerSideDeck; }

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
        Transform targetZone = GetFreeMonsterZone(forPlayer);
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
        Transform newZone = GetFreeMonsterZone(newOwnerIsPlayer);

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
        OpenCardMultiSelection(sourceList, title, 1, 1, (selectedList) =>
        {
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
    public CardDisplay SpecialSummonFromData(CardData data, bool forPlayer, bool faceUp = true, bool defense = false)
    {
        Transform targetZone = GetFreeMonsterZone(forPlayer);
        if (targetZone == null) return null;

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

        // Notifica o ChainManager sobre a invocação para abrir janela de resposta (ex: Trap Hole)
        if (ChainManager.Instance != null)
        {
            ChainManager.Instance.AddToChain(display, forPlayer, ChainManager.TriggerType.Summon);
        }

        return display;
    }

    public void OnSummon(CardDisplay card)
    {
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnSummon(card);
    }

    // Helper para encontrar um CardDisplay no campo pelo ID
    public CardDisplay FindCardOnField(string cardId, bool isPlayer)
    {
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetChild(0).GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData.id == cardId)
                {
                    return display;
                }
            }
        }

        zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetChild(0).GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData.id == cardId)
                {
                    return display;
                }
            }
        }
        return null;
    }

    // Ferramenta de Dev: Visualizar Deck
    public void ViewDeck(bool isPlayer)
    {
        if (UIManager.Instance == null) return;

        // Se for oponente e não estiver em modo Dev, bloqueia (a menos que uma carta permita, mas aí seria via efeito específico)
        if (!isPlayer && !devMode) return;

        List<CardData> deck = isPlayer ? DeckManager.Instance.GetPlayerDeck() : DeckManager.Instance.GetOpponentDeck();
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
    }

    // Ferramenta de DEV para trocar oponente
    [ContextMenu("DEV: Next Opponent")]
    public void Dev_NextOpponent()
    {
        if (!devMode || campaignDatabase == null) return;

        int actIndex = 0;
        int oppIndex = 0;
        bool found = false;

        // Encontra o índice do oponente atual
        if (currentOpponent != null)
        {
            for(int i=0; i<campaignDatabase.acts.Count; i++)
            {
                for(int j=0; j<campaignDatabase.acts[i].opponentIDs.Count; j++)
                {
                    if (campaignDatabase.acts[i].opponentIDs[j] == currentOpponent.id)
                    {
                        actIndex = i;
                        oppIndex = j;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }

        // Avança para o próximo
        oppIndex++;
        if (oppIndex >= campaignDatabase.acts[actIndex].opponentIDs.Count)
        {
            oppIndex = 0;
            actIndex++;
            if (actIndex >= campaignDatabase.acts.Count) actIndex = 0;
        }

        string nextID = campaignDatabase.acts[actIndex].opponentIDs[oppIndex];
        CharacterData nextChar = characterDatabase.GetCharacterById(nextID);
        
        if (nextChar != null)
        {
            Debug.Log($"Dev: Trocando para {nextChar.name} (Ato {actIndex+1})");
            StartDuel(nextChar, (actIndex * 10) + oppIndex + 1);
        }
    }

    // --- SISTEMA DE FUSÃO ---

    public void PerformFusionSummon(CardDisplay sourceCard, CardData fusionMonster, List<CardData> materials)
    {
        if (fusionMonster == null || materials == null || materials.Count == 0) return;

        Debug.Log($"Realizando Invocação-Fusão de {fusionMonster.name}...");

        // 1. Envia a Magia de Fusão para o cemitério (se existir e for Spell)
        if (sourceCard != null)
        {
            SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Effect);
            Destroy(sourceCard.gameObject);
        }

        // 2. Envia os materiais para o cemitério
        foreach (var mat in materials)
        {
            // Tenta achar na mão
            var handObj = playerHand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData == mat);
            if (handObj != null)
            {
                SendToGraveyard(mat, true, CardLocation.Hand, SendReason.Effect); // Fusão é sempre do player neste contexto de UI
                playerHand.Remove(handObj);
                Destroy(handObj);
            }
            else
            {
                // Tenta achar no campo
                var fieldObj = FindCardOnField(mat.id, true);
                if (fieldObj != null)
                {
                    SendToGraveyard(mat, true, CardLocation.Field, SendReason.Effect);
                    Destroy(fieldObj.gameObject);
                }
            }
        }

        // 3. Invoca o Monstro de Fusão
        if (playerExtraDeck.Contains(fusionMonster))
            playerExtraDeck.Remove(fusionMonster);

        CardDisplay summonedMonster = SpecialSummonFromData(fusionMonster, true);

        // 4. Registra os materiais usados (para UFOroid Fighter, etc)
        if (summonedMonster != null)
        {
            summonedMonster.fusionMaterialsUsed = new List<CardData>(materials);
        }
    }

    // Helper para equipar um monstro em outro (Union, Relinquished)
    public void EquipMonsterToMonster(CardDisplay equipCard, CardDisplay targetMonster)
    {
        // 1. Move equipCard para zona de S/T
        Transform targetZone = GetFreeSpellZone(equipCard.isPlayerCard);
        if (targetZone == null)
        {
            Debug.LogWarning("Sem zona de S/T para equipar o monstro.");
            SendToGraveyard(equipCard.CurrentCardData, equipCard.isPlayerCard, CardLocation.Field, SendReason.Rule);
            Destroy(equipCard.gameObject);
            return;
        }

        // Remove da zona de monstro/mão se necessário (SetParent cuida da hierarquia, mas listas precisam de update)
        if (equipCard.isPlayerCard && playerHand.Contains(equipCard.gameObject)) playerHand.Remove(equipCard.gameObject);

        equipCard.transform.SetParent(targetZone);
        equipCard.transform.localPosition = Vector3.zero;
        equipCard.transform.localScale = fieldCardScale;
        equipCard.transform.localRotation = Quaternion.Euler(0, 0, 0); // Face-up
        equipCard.isOnField = true;
        equipCard.isInteractable = false;

        CreateCardLink(equipCard, targetMonster, CardLink.LinkType.Equipment);
        Debug.Log($"{equipCard.CurrentCardData.name} equipado em {targetMonster.CurrentCardData.name}.");
    }

    public void CreateCardLink(CardDisplay source, CardDisplay target, CardLink.LinkType type)
    {
        GameObject linkObj = new GameObject($"Link_{source.name}_{target.name}");
        CardLink link = linkObj.AddComponent<CardLink>();
        link.Initialize(source, target, type);
    }

    public void BeginFusionSummon(CardDisplay sourceCard)
    {
        // 1. Verifica se tem Monstros de Fusão no Extra Deck
        if (playerExtraDeck.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Fusão no Extra Deck.");
            return;
        }

        // 2. Abre a UI de Fusão
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowFusionUI(sourceCard);
        }
    }

    // --- MÉTODOS ADICIONADOS PARA CORRIGIR ERROS DE COMPILAÇÃO ---

    public List<CardData> GetPlayerMainDeck()
    {
        // Se estamos num duelo (DeckManager tem cartas), retorna o deck do duelo
        if (DeckManager.Instance != null && DeckManager.Instance.GetPlayerDeck() != null && DeckManager.Instance.GetPlayerDeck().Count > 0)
        {
            return DeckManager.Instance.GetPlayerDeck();
        }
        // Senão, retorna o deck persistente (para DeckBuilder, SaveSystem, etc)
        return playerMainDeck;
    }

    public List<CardData> GetOpponentMainDeck()
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetOpponentDeck() != null && DeckManager.Instance.GetOpponentDeck().Count > 0)
        {
            return DeckManager.Instance.GetOpponentDeck();
        }
        return opponentMainDeck;
    }
    public List<CardData> GetPlayerExtraDeck() { return playerExtraDeck; }
    public List<CardData> GetOpponentExtraDeck() { return opponentExtraDeck; }
    public List<CardData> GetPlayerGraveyard() { return playerGraveyard; }
    public List<CardData> GetOpponentGraveyard() { return opponentGraveyard; }
    public List<CardData> GetPlayerRemoved() { return playerRemoved; }
    public List<CardData> GetOpponentRemoved() { return opponentRemoved; }
    public int GetPlayerRemovedCount() { return playerRemoved.Count; }

    public List<CardData> GetPlayerHandData()
    {
        List<CardData> data = new List<CardData>();
        foreach (var go in playerHand)
        {
            if (go != null)
            {
                var display = go.GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData != null)
                    data.Add(display.CurrentCardData);
            }
        }
        return data;
    }

    public List<CardData> GetOpponentHandData()
    {
        List<CardData> data = new List<CardData>();
        foreach (var go in opponentHand)
        {
            if (go != null)
            {
                var display = go.GetComponent<CardDisplay>();
                if (display != null && display.CurrentCardData != null)
                    data.Add(display.CurrentCardData);
            }
        }
        return data;
    }

    public void SetPlayerDeck(List<CardData> main, List<CardData> side, List<CardData> extra)
    {
        playerMainDeck = new List<CardData>(main);
        playerSideDeck = new List<CardData>(side);
        playerExtraDeck = new List<CardData>(extra);
    }
}