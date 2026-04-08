using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para Shuffle
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum GamePhase
{
    Draw,
    Standby,
    Main1,
    Battle,
    Main2,
    End
}

public enum StatDisplayMode
{
    None,
    AboveCard,
    BelowCard
}

public enum FlipAnimationMode
{
    AnimateOnReveal, // Anima apenas ao virar para cima (Padrão)
    AnimateAlways,   // Anima em ambas as direções (Cima -> Baixo, Baixo -> Cima)
    NoAnimation      // Nunca anima, troca a textura instantaneamente
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
    [Tooltip("Controla como a animação de virar a carta (flip) se comporta.")]
    public FlipAnimationMode flipMode = FlipAnimationMode.AnimateOnReveal;
    [Tooltip("Se marcado, usa o componente Outline do Unity para o contorno. Se desmarcado, usa uma imagem filha (OutlineImage).")]
    public bool useSimpleOutline = true;
    [Tooltip("Habilita a animação de comprar carta voando do deck para a mão.")]
    public bool enableDrawAnimation = true;

    [Header("Card Visualization")]
    [Tooltip("A escala das cartas no campo.")]
    public Vector3 fieldCardScale = new Vector3(0.8f, 0.8f, 0.8f);
    [Tooltip("Se marcado, o embaralhamento do deck fará o movimento 3D levantando cartas. Se desmarcado, usará um deslize 2D rápido.")]
    public bool use3DDeckShuffle = true;
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

    [Header("On-Field Stats Display")]
    [Tooltip("Exibe o ATK/DEF atual dos monstros no campo.")]
    public StatDisplayMode monsterStatDisplayMode = StatDisplayMode.None;
    [Tooltip("O prefab que contém o TextMeshPro para exibir os stats.")]
    public GameObject fieldStatDisplayPrefab;
    [Tooltip("A distância (em pixels) do centro da carta para exibir os stats.")]
    public float statDisplayYOffset = 45f;
    [Tooltip("Cor para stats que foram aumentados (buff).")]
    public Color statBuffColor = new Color(0.1f, 1f, 0.1f);
    [Tooltip("Cor para stats que foram reduzidos (debuff).")]
    public Color statDebuffColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Cor para o stat que não está sendo usado (ATK em defesa, DEF em ataque).")]
    public Color statInactiveColor = Color.gray;
    [Tooltip("Quanto a carta do monstro deve se deslocar para dar espaço ao texto de stats.")]
    public float cardYAdjustmentForStats = 15f;
    [Tooltip("Se verdadeiro, acompanha a rotação do dono da carta (cartas do oponente mostram stats de ponta-cabeça para o jogador).")]
    public bool statDisplayMatchCardRotation = true;

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
    [Tooltip("Ativa o Painel de Testes Completo (Full Test Mode).")]
    public bool fullTestMode = false;
    [Tooltip("Se marcado, pula os menus e inicia um duelo livre imediatamente ao dar Play.")]
    public bool testDuelDirectly = false;
    [Tooltip("Se marcado, pula para a página de Construção de Deck imediatamente ao dar Play.")]
    public bool testDeckBuilderDirectly = false;
    [Tooltip("Se marcado, pula para a página de Card Library imediatamente ao dar Play.")]
    public bool testLibraryDirectly = false; 
    [Tooltip("Habilita o menu de testes de efeitos visuais e sonoros.")]
    public bool effectTestMode = false;
    [Tooltip("ID do oponente para teste rápido (ex: 021_kaiba). Deixe vazio para usar o fluxo normal.")]
    public string testOpponentID = ""; 
    [Tooltip("ID do personagem para substituir o deck do jogador (ex: 020_pegasus). Deixe vazio para usar o deck do save.")]
    public string testPlayerID = "";
    [Tooltip("Força a variante do deck do oponente em testes (0 = Aleatório, 1 = Deck A, 2 = Deck B, 3 = Deck C).")]
    public int testOpponentDeckVariant = 0;
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
    [Tooltip("Se marcado, o oponente preenche as zonas da direita para a esquerda (perspectiva do jogador), simulando a esquerda dele.")]
    public bool fillOpponentZonesFromRight = true;
    [Tooltip("Se marcado, desbloqueia 3 cópias de todas as cartas no Baú (Trunk) ao iniciar o jogo (Requer DevMode).")]
    public bool unlockAllCards = false;
    [Tooltip("Indica se uma simulação automática está em andamento (Modo Fantasma).")]
    public bool isSimulating = false;
    [Tooltip("Se marcado, os decks não serão embaralhados no início (útil para testar ordem de saque).")]
    public bool disableDeckShuffle = false;
    [Tooltip("Se marcado, o jogador sempre começa o duelo.")]
    public bool invertDecksDevMode = false;
    [Tooltip("Se marcado, inverte a exibição da pilha de deck.")]
    public bool forcePlayerGoingFirst = false;
    [Tooltip("Se marcado, o jogador não perde LP (Modo Deus).")]
    public bool infiniteLP = false;
    [Tooltip("Se marcado, permite invocar monstros de nível alto sem tributo.")]
    public bool disableTributeRequirements = false;
    [Tooltip("Se marcado, Invocação-Fusão não consome materiais nem a carta mágica.")]
    public bool disableFusionCost = false;
    [Tooltip("Se marcado, Invocação-Ritual não consome tributos nem a carta mágica.")]
    public bool disableRitualCost = false;
    [Tooltip("Se marcado, remove o limite de 1 Normal Summon por turno.")]
    public bool infiniteNormalSummons = false;
    [Tooltip("Se marcado, o resultado da moeda será sempre CARA (Heads).")]
    public bool alwaysCoinHead = false;
    [Tooltip("Se marcado, o resultado do dado será sempre 6. Útil para testar cartas de aposta.")]
    public bool alwaysDiceSix = false;
    [Tooltip("Impede que as fases avancem sozinhas (útil para testar efeitos manualmente).")]
    public bool disableAutoPhases = false;
    [Tooltip("Habilita ou desabilita todos os Efeitos Visuais (VFX).")]
    public bool enableVFX = true;
    [Tooltip("Habilita ou desabilita todos os Efeitos Sonoros (SFX).")]
    public bool enableSFX = true;
 
    [Header("Turn Clock Visuals")]
    [Tooltip("Habilita o visual de relógio central para contagem de turnos.")]
    public bool enableTurnClockVisuals = true;
    public TurnClockUI turnClockUI; // Referência ao painel central

    [Header("Game Rules")]
    [Tooltip("Se marcado, aplica a regra de limite de cartas na mão na End Phase.")]
    public bool enableHandLimit = true;
    [Tooltip("Número máximo de cartas na mão ao final do turno (Padrão TCG/OCG: 6).")]
    public int handLimit = 6;
    [Tooltip("Se marcado, permite o uso de cartas Proibidas (Limit 0) como Limitadas (Limit 1).")]
    public bool allowForbiddenCards = false;
    [Tooltip("Se marcado, ignora completamente a Banlist (todas as cartas podem ter 3 cópias).")]
    public bool disableBanlist = false;
    [Tooltip("Se marcado, o monstro invocado por tributo ocupará a zona do primeiro monstro sacrificado.")]
    public bool placeTributeSummonInTributeZone = true;
    [Tooltip("Se marcado (Regra Oficial MR3+), o jogador que começar o duelo NÃO saca uma carta no 1º turno.")]
    public bool applyModernFirstTurnDrawRule = true;

    [Header("Input & UI Options")]
    [Tooltip("Se marcado, clicar com o botão direito no campo (vazio) abre um menu para trocar de fase.")]
    public bool enableRightClickPhaseMenu = true;
    [Tooltip("Se marcado, exibe uma janela de confirmação ao tentar mudar a posição de batalha de um monstro.")]
    public bool confirmBattlePositionChange = true;
    [Tooltip("Se marcado, exibe uma janela de confirmação ao selecionar um alvo para ataque.")]
    public bool confirmAttackTarget = true;
    [Tooltip("Se marcado, permite selecionar cartas diretamente da mão para efeitos (ex: descarte) sem abrir janela.")]
    public bool useDirectHandSelection = true;
    [Tooltip("Se marcado, pede confirmação após selecionar a(s) carta(s) na mão durante a seleção direta.")]
    public bool confirmHandSelection = true;
    [Tooltip("Se marcado, permite invocar monstros da mão com um clique (Esq=Ataque, Dir=Defesa).")]
    public bool quickSummonFromHand = false;
    [Tooltip("Se marcado, permite jogar Spells/Traps da mão com um clique (Esq=Ativar, Dir=Setar).")]
    public bool quickSpellTrapFromHand = false;
    [Tooltip("Se marcado, clicar no próprio monstro atacante quando o oponente não tem monstros realiza o ataque direto imediatamente.")]
    public bool quickAttackDirectly = false;
    [Tooltip("Se ativado, usa atalhos de Esquerdo/Direito do mouse com Tooltip. Se desativado, usa o Menu de Ação clássico.")]
    public bool useMouseTooltipUI = true;
    [Tooltip("Habilita o painel visual pop-up de Dano/Cura saltando no campo.")]
    public bool enableDamagePopups = true;
    [Tooltip("Se marcado, usa o painel customizado Panel_Fusion. Se desmarcado, usa a seleção tática no tabuleiro.")]
    public bool useCustomFusionUI = true;
    [Tooltip("Se marcado, usa o painel customizado Panel_Ritual. Se desmarcado, usa a seleção tática no tabuleiro.")]
    public bool useCustomRitualUI = true;
    [Tooltip("Se marcado, sempre pedirá confirmação para seleção de alvo, mesmo que haja apenas um alvo possível.")]
    public bool alwaysConfirmSingleTarget = false;

    [Header("Minigames Settings")]
    [Tooltip("Ativa a escolha manual de Cara/Coroa para os lançamentos de moeda.")]
    public bool coinTossRequiresChoice = true;
    [Tooltip("Exige que o jogador clique fisicamente na moeda na tela para ela pular e girar.")]
    public bool coinTossManualSpin = true;
    public CoinTossUI coinTossUI;
    [Tooltip("Exige que o jogador clique fisicamente no dado na tela para ele rolar.")]
    public bool diceRollManualSpin = true;
    public DiceRollUI diceRollUI;
    
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

    [Header("UI Interaction Control")]
    public CanvasGroup playerHandCanvasGroup;
    public CanvasGroup opponentHandCanvasGroup;

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

    // FASE 26: Deck manager references for LuaAPI compatibility
    public DeckManager playerDeckManager => DeckManager.Instance;
    public DeckManager opponentDeckManager => DeckManager.Instance;
    
    public bool checkPlayerForbiddenStatus(DeckManager deckManager, string forbiddenType)
    {
        // Check if a forbidden status applies to this deck
        // For now, return false (no forbidden status)
        return false;
    }

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
    [Tooltip("Habilita a animação visual ao realizar uma Invocação-Tributo.")]
    public bool enableTributeSummonAnimation = true;
    [Tooltip("Habilita a cinemática (tela escura e carta gigante) para invocações Especiais e de Tributo.")]
    public bool enableSummonCinematics = true;
    [Tooltip("Habilita a cinemática para Invocação-Fusão.")]
    public bool enableFusionCinematic = true;
    [Tooltip("Habilita a cinemática para Invocação-Ritual.")]
    public bool enableRitualCinematic = true;

    [Header("Game Speed Settings")]
    [Tooltip("Tempo em segundos entre cada carta comprada pelo jogador no início.")]
    public float playerDrawSpeed = 0.5f;
    [Tooltip("Tempo em segundos entre cada carta comprada pelo oponente no início.")]
    public float opponentDrawSpeed = 0.3f;

    public bool revealOpponentDraw = false; // Pikeru's Second Sight (1431)
    private UnityWebRequest backTextureRequest; // Para evitar memory leak

    [HideInInspector] public int lpPaidThisTurnPlayer = 0;
    [HideInInspector] public int lpPaidLastTurnPlayer = 0;
    [HideInInspector] public int lpPaidThisTurnOpponent = 0;
    [HideInInspector] public int lpPaidLastTurnOpponent = 0;

    [HideInInspector] public int normalSummonsThisTurnPlayer = 0;
    [HideInInspector] public int normalSummonsThisTurnOpponent = 0;

    // --- ESTADO DE SELEÇÃO DE MÃO ---
    [HideInInspector] public bool isSelectingFromHand = false;
    private List<CardData> handSelectionCandidates;
    private List<CardData> currentHandSelection;
    private int handSelectionCountRequired;
    private System.Action<List<CardData>> handSelectionCallback;
    private System.Func<List<CardData>, bool> customSelectionValidator;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Atalho de Desenvolvedor para Fullscreen (F11)
        bool toggleFullscreen = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame) toggleFullscreen = true;
#else
        if (Input.GetKeyDown(KeyCode.F11)) toggleFullscreen = true;
#endif

        if (devMode && toggleFullscreen)
        {
            ToggleFullscreen();
        }

        // Menu de Fases com Botão Direito (se não estiver clicando em uma carta/UI)
        bool rightClick = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) rightClick = true;
#else
        if (Input.GetMouseButtonDown(1)) rightClick = true;
#endif

        if (isSelectingFromHand && rightClick)
        {
            FinishHandSelection(true); // Cancela a seleção
        }
        else if (enableRightClickPhaseMenu && isPlayerTurn && !isDuelOver && rightClick)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                OpenPhaseSelectionMenu();
            }
        }
    }

    // Helper para encontrar o Canvas principal da UI
    private Transform GetUIParent()
    {
        if (duelFieldUI != null && duelFieldUI.transform != null)
        {
            Canvas canvas = duelFieldUI.transform.GetComponentInParent<Canvas>();
            if (canvas != null) return canvas.transform;
        }
        
        // Busca o Canvas Principal da UI (Root) para evitar pegar o Canvas escondido de uma carta
        Canvas[] allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases) if (c.isRootCanvas && c.gameObject.name.Contains("Panel_Duel")) return c.transform;
        foreach (Canvas c in allCanvases) if (c.isRootCanvas) return c.transform;
            
        return null;
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

        // Garante que o InitialDeckBuilder exista para gerar cartas se necessário
        if (InitialDeckBuilder.Instance == null)
        {
            GameObject go = new GameObject("InitialDeckBuilder");
            go.AddComponent<InitialDeckBuilder>();
        }

        // Inicializa o Baú com todas as cartas se estiver vazio (Modo Sandbox/Teste)
        // OU se o cheat unlockAllCards estiver ativo (e devMode on)
        if (cardDatabase != null)
        {
            if (devMode && unlockAllCards)
            {
                playerTrunk.Clear();
                foreach (var card in cardDatabase.cardDatabase)
                {
                    // Adiciona 3 cópias de cada para deck building irrestrito
                    playerTrunk.Add(card.id); playerTrunk.Add(card.id); playerTrunk.Add(card.id);
                    // Marca como usada para não poluir a biblioteca com "NEW"
                    if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
                }
                Debug.Log("[GameManager] Cheat ativado: Todas as cartas desbloqueadas (3x).");
            }
            // FIX: Se o baú estiver vazio (Novo Jogo), gera o deck inicial imediatamente
            else if (playerTrunk.Count == 0)
            {
                if (InitialDeckBuilder.Instance != null)
                {
                    List<CardData> starterDeck = InitialDeckBuilder.Instance.GenerateInitialDeck();
                    foreach(var card in starterDeck)
                    {
                        playerTrunk.Add(card.id);
                        if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
                    }
                    playerMainDeck = new List<CardData>(starterDeck); // Define como deck atual
                }
            }
        }

        yield return StartCoroutine(LoadCardBackTexture());
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

    public void EnableHandInteraction()
    {
        if (playerHandCanvasGroup != null)
        {
            playerHandCanvasGroup.interactable = true;
            Debug.Log("[GameManager] Interação com a mão do jogador ATIVADA.");
        }
    }

    // Novo método chamado pelo botão "Free Duel"
    public void StartDuel()
    {
        // Limpa o estado anterior (destrói cartas visuais, limpa listas)
        CleanupDuelState();

        // FIX: Garante que a referência da UI do campo esteja atualizada antes de qualquer coisa
        if (duelFieldUI == null) duelFieldUI = FindFirstObjectByType<DuelFieldUI>();
        if (duelFieldUI == null) Debug.LogError("GameManager: DuelFieldUI não encontrado na cena! O placar não funcionará.");

        // Garante que os gerenciadores essenciais existam na cena
        EnsureCoreManagers();

        // NOVA LÓGICA: Desabilita cliques na mão até o jogo estar pronto
        if (playerHandLayoutGroup != null && playerHandCanvasGroup == null)
            playerHandCanvasGroup = playerHandLayoutGroup.GetComponent<CanvasGroup>();
        if (playerHandLayoutGroup != null && playerHandCanvasGroup == null)
            playerHandCanvasGroup = playerHandLayoutGroup.gameObject.AddComponent<CanvasGroup>();
        
        if (playerHandCanvasGroup != null)
        {
            playerHandCanvasGroup.interactable = false;
        }

        List<CardData> pDeck = InitializePlayerDeck();
        (List<CardData> oMain, List<CardData> oExtra) = InitializeOpponentDeck();
        DeckManager.Instance.SetupDecks(pDeck, playerExtraDeck, oMain, oExtra);

        // Inicializa LP
        playerLP = 8000;
        opponentLP = 8000;
        turnCount = 0;
        isDuelOver = false;

        // Define quem começa
        if (forcePlayerGoingFirst) isPlayerTurn = true;
        else if (devMode) isPlayerTurn = true; // Padrão Dev
        else isPlayerTurn = (Random.value > 0.5f); // Aleatório real

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
            // Fallback Robusto: Só aceita o tema da campanha se ele tiver pelo menos o tabuleiro preenchido!
            if (theme != null && theme.boardBackground != null)
                DuelThemeManager.Instance.ApplyTheme(theme);
            else if (DuelThemeManager.Instance.defaultTheme != null)
                DuelThemeManager.Instance.ApplyTheme(DuelThemeManager.Instance.defaultTheme);
        }
        else if (DuelThemeManager.Instance != null && DuelThemeManager.Instance.defaultTheme != null)
        {
            DuelThemeManager.Instance.ApplyTheme(DuelThemeManager.Instance.defaultTheme);
            }

                StartCoroutine(DuelStartSequence());    
        }
    private IEnumerator DuelStartSequence()
    {
        yield return new WaitForSeconds(0.5f); // Delay inicial para a cena abrir e a UI estabilizar

        // Embaralha os decks com animação visual ANTES de comprar as cartas
        if (!disableDeckShuffle && DeckManager.Instance != null)
        {
            DeckManager.Instance.ShuffleDeck(true);
            DeckManager.Instance.ShuffleDeck(false);
            // Aguarda a animação de shuffle terminar (dura 0.6s)
            yield return new WaitForSeconds(0.8f);
        }

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
        if (CardEffectManager.Instance == null) CreateManager<CardEffectManager>();
        if (OpponentAI.Instance == null) CreateManager<OpponentAI>(); // Garante que a IA exista
        if (DeckManager.Instance == null) CreateManager<DeckManager>(); // Garante que o DeckManager exista
        if (FusionManager.Instance == null) CreateManager<FusionManager>();
        if (RitualManager.Instance == null) CreateManager<RitualManager>();

        // Cria o gerenciador de testes se o modo estiver ativo
        if (effectTestMode && FindFirstObjectByType<EffectTestManager>() == null) CreateManager<EffectTestManager>();
        
        // Garante o TrophyManager
        if (TrophyManager.Instance == null) CreateManager<TrophyManager>();
    }

    void CreateManager<T>() where T : MonoBehaviour
    {
        GameObject go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        Debug.Log($"GameManager: Auto-criado gerenciador ausente: {typeof(T).Name}");
    }

    public void CleanupDuelState()
    {
        // Destrói objetos visuais das mãos
        foreach (GameObject card in playerHand) if (card != null) Destroy(card);
        playerHand.Clear();

        foreach (GameObject card in opponentHand) if (card != null) Destroy(card);
        opponentHand.Clear();

        // FASE DE LIMPEZA DO CAMPO (Visual e Físico para Simulações)
        if (duelFieldUI != null)
        {
            System.Action<Transform[]> ClearZones = (zones) => {
                if (zones == null) return;
                foreach (var z in zones) {
                    foreach (Transform child in z) Destroy(child.gameObject);
                }
            };
            ClearZones(duelFieldUI.playerMonsterZones);
            ClearZones(duelFieldUI.opponentMonsterZones);
            ClearZones(duelFieldUI.playerSpellZones);
            ClearZones(duelFieldUI.opponentSpellZones);
            if (duelFieldUI.playerFieldSpell != null) foreach (Transform child in duelFieldUI.playerFieldSpell) Destroy(child.gameObject);
            if (duelFieldUI.opponentFieldSpell != null) foreach (Transform child in duelFieldUI.opponentFieldSpell) Destroy(child.gameObject);
        }

        // Destrói vínculos remanescentes (Equipamentos)
        CardLink[] links = UnityEngine.Object.FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links) Destroy(link.gameObject);

        // Desbloqueia as zonas presas por efeitos (ex: Ojama)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.blockedZonesByCard.Clear();

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

    public void ClearFieldZonesOnly()
    {
        if (duelFieldUI == null) return;

        List<CardDisplay> cardsToClear = new List<CardDisplay>();

        System.Action<Transform[]> CollectCardsFromZones = (zones) => {
            if (zones == null) return;
            foreach (var z in zones) {
                if (z == null) continue;
                foreach (Transform child in z) {
                    CardDisplay cd = child.GetComponent<CardDisplay>();
                    if (cd != null) cardsToClear.Add(cd);
                }
            }
        };

        CollectCardsFromZones(duelFieldUI.playerMonsterZones);
        CollectCardsFromZones(duelFieldUI.opponentMonsterZones);
        CollectCardsFromZones(duelFieldUI.playerSpellZones);
        CollectCardsFromZones(duelFieldUI.opponentSpellZones);
        
        if (duelFieldUI.playerFieldSpell != null && duelFieldUI.playerFieldSpell.childCount > 0) {
            CardDisplay cd = duelFieldUI.playerFieldSpell.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null) cardsToClear.Add(cd);
        }
        if (duelFieldUI.opponentFieldSpell != null && duelFieldUI.opponentFieldSpell.childCount > 0) {
            CardDisplay cd = duelFieldUI.opponentFieldSpell.GetChild(0).GetComponent<CardDisplay>();
            if (cd != null) cardsToClear.Add(cd);
        }

        foreach (CardDisplay card in cardsToClear)
        {
            if (card != null && card.gameObject != null)
            {
                // Simula destruição: envia para o GY e destrói o objeto
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
                SendToGraveyard(card.CurrentCardData, card.isPlayerCard, CardLocation.Field, SendReason.Destroyed);
                
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
                
                Destroy(card.gameObject);
            }
        }
    }

    public void SetSpellTrapFromData(CardData cardData, bool isPlayer, int zoneIndex, bool faceUp = false)
    {
        if (cardData == null || (!cardData.type.Contains("Spell") && !cardData.type.Contains("Trap")))
        {
            Debug.LogError("[GameManager] SetSpellTrapFromData: Card is not a Spell or Trap.");
            return;
        }
        
        if (duelFieldUI == null) return;
        Transform[] spellZones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        if (zoneIndex < 0 || zoneIndex >= spellZones.Length) return;

        Transform zone = spellZones[zoneIndex];
        if (zone.childCount > 0)
        {
            Debug.LogWarning($"[GameManager] SetSpellTrapFromData: Zona {zoneIndex} já está ocupada.");
            return;
        }

        GameObject cardGO = Instantiate(cardPrefab, zone);
        CardDisplay cardDisplay = cardGO.GetComponent<CardDisplay>();

        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = fieldCardScale;
        cardDisplay.isInteractable = true; // Set to true so it can be flipped up
        cardDisplay.isPlayerCard = isPlayer;
        cardDisplay.isOnField = true;
        
        cardDisplay.SetCard(cardData, cardBackTexture, faceUp);
        cardGO.transform.localRotation = Quaternion.identity;
    }

    public CardDisplay SpecialSummonFromData(CardData cardData, bool isPlayer, int zoneIndex = -1, bool inAttackPosition = true, bool faceDown = false)
    {
        if (cardData == null || !cardData.type.Contains("Monster"))
        {
            Debug.LogError("[GameManager] SpecialSummonFromData: Card is not a Monster.");
            return null;
        }

        if (duelFieldUI == null) return null;
        Transform[] monsterZones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        Transform zone = null;
        
        if (zoneIndex >= 0 && zoneIndex < monsterZones.Length)
        {
            zone = monsterZones[zoneIndex];
            if (zone.childCount > 0)
            {
                Debug.LogWarning($"[GameManager] SpecialSummonFromData: Zona {zoneIndex} já está ocupada.");
                return null;
            }
        }
        else
        {
            zone = GetFreeMonsterZone(isPlayer);
            if (zone == null)
            {
                Debug.LogWarning("[GameManager] SpecialSummonFromData: Nenhuma zona livre encontrada.");
                return null;
            }
        }
        
        GameObject cardGO = Instantiate(cardPrefab, zone);
        CardDisplay cardDisplay = cardGO.GetComponent<CardDisplay>();

        cardGO.transform.localPosition = Vector3.zero; // Explicitamente centraliza dentro da zona
        cardGO.transform.localScale = fieldCardScale;
        cardGO.transform.localRotation = Quaternion.identity; // Reseta a rotação antes de aplicar a específica

        cardDisplay.isInteractable = false; // Cartas no campo não são interativas como as da mão
        cardDisplay.isPlayerCard = isPlayer;
        cardDisplay.isOnField = true;
        cardDisplay.position = inAttackPosition ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense;
        cardDisplay.summonedTurnCount = turnCount;
        cardDisplay.hasChangedPositionThisTurn = false;
        
        cardDisplay.SetCard(cardData, cardBackTexture, !faceDown);
        
        if (!inAttackPosition) // Defesa
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 90f : -90f);
        }
        else
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, isPlayer ? 0f : 180f);
        }

        // Adiciona o display de stats se a opção estiver ativa
        if (monsterStatDisplayMode != StatDisplayMode.None && fieldStatDisplayPrefab != null && cardData.type.Contains("Monster"))
        {
            // Instancia como irmão da carta (no mesmo pai, a zona) para evitar problemas de renderização
            GameObject statGO = Instantiate(fieldStatDisplayPrefab, cardGO.transform.parent);
            FieldStatUI statUI = statGO.GetComponent<FieldStatUI>();
            if (statUI != null)
            {
                statUI.Setup(cardDisplay); // Passa a referência da carta para o script de UI segui-la
            }

            // Desloca a carta para dar espaço ao texto
            float adjustment = (monsterStatDisplayMode == StatDisplayMode.AboveCard) ? -cardYAdjustmentForStats : cardYAdjustmentForStats;
            if (statDisplayMatchCardRotation && !isPlayer) adjustment = -adjustment; // oponente inverte
            cardGO.transform.localPosition += new Vector3(0, adjustment, 0);
        }

        OnSummon(cardDisplay);
        return cardDisplay;
    }

    public void OnSummon(CardDisplay card)
    {
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnSummon(card);
        }
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
                
                // --- SELEÇÃO DE DECK ALEATÓRIA (A, B, C) para o Jogador ---
                List<string> selectedMain = charData.deck_A;
                List<string> selectedExtra = charData.extra_deck_A;
                string variant = "A";

                int rng = Random.Range(0, 3);
                if (rng == 1 && charData.deck_B != null && charData.deck_B.Count > 0) { selectedMain = charData.deck_B; selectedExtra = charData.extra_deck_B; variant = "B"; }
                else if (rng == 2 && charData.deck_C != null && charData.deck_C.Count > 0) { selectedMain = charData.deck_C; selectedExtra = charData.extra_deck_C; variant = "C"; }

                Debug.Log($"[DevMode] Usando Deck Variante do Jogador: {variant}");

                if (selectedMain != null) foreach (string id in selectedMain) { CardData c = cardDatabase.GetCardById(id); if (c != null) devDeck.Add(c); }
                
                playerExtraDeck.Clear();
                if (selectedExtra != null) foreach (string id in selectedExtra) { CardData c = cardDatabase.GetCardById(id); if (c != null) playerExtraDeck.Add(c); }

                playerMainDeck = devDeck;
                
                // FIX: Adiciona as cartas do dev deck ao Trunk para que o Deck Builder não fique vazio e não crashe
                playerTrunk.Clear();
                foreach (var c in devDeck) playerTrunk.Add(c.id);
                foreach (var c in playerExtraDeck) playerTrunk.Add(c.id);
                
                // Atualiza nome do jogador na UI
                playerName = charData.name;
                if (duelFieldUI != null && duelFieldUI.playerNameText != null)
                    duelFieldUI.playerNameText.text = playerName;

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

                // FIX: Cartas do deck inicial já começam "usadas" (sem tag NEW na biblioteca)
                if (SaveLoadSystem.Instance != null)
                {
                    SaveLoadSystem.Instance.MarkCardAsUsed(card.id);
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

        // DEV MODE: Se um ID de teste estiver definido, limpa o oponente atual para forçar o carregamento correto
        if (devMode && !string.IsNullOrEmpty(testOpponentID))
        {
            currentOpponent = null;
        }

        // Validação de segurança: Se o objeto oponente existe mas está vazio/inválido (sem ID), reseta
        if (currentOpponent != null && string.IsNullOrEmpty(currentOpponent.id))
        {
            Debug.LogWarning("[GameManager] Oponente atual é inválido (ID vazio). Resetando para tentar carregar novamente.");
            currentOpponent = null;
        }

        // Se o oponente ainda for nulo aqui (não foi setado no StartDuel), tenta o ID de teste novamente como última tentativa
        if (currentOpponent == null && !string.IsNullOrEmpty(testOpponentID) && characterDatabase != null)
        {
            currentOpponent = characterDatabase.GetCharacterById(testOpponentID);
        }

        // NOVO: Se ainda for nulo (Free Duel sem ID definido), escolhe um aleatório da campanha
        if (currentOpponent == null && campaignDatabase != null && campaignDatabase.acts.Count > 0)
        {
            var allIds = campaignDatabase.acts.SelectMany(a => a.opponentIDs).ToList();
            if (allIds.Count > 0)
            {
                string randomId = allIds[Random.Range(0, allIds.Count)];
                currentOpponent = characterDatabase.GetCharacterById(randomId);
                Debug.Log($"[GameManager] Oponente aleatório selecionado: {currentOpponent?.name}");
            }
        }
        else if (currentOpponent == null && campaignDatabase == null)
        {
            Debug.LogWarning("[GameManager] CampaignDatabase não atribuído! Não foi possível selecionar um oponente aleatório.");
        }

        // Se temos um oponente definido (Campanha), carregamos o deck dele
        if (currentOpponent != null)
        {
            // Garante referência da UI antes de usar e atualiza o nome
            if (duelFieldUI == null) duelFieldUI = FindFirstObjectByType<DuelFieldUI>();
            if (duelFieldUI != null && duelFieldUI.opponentNameText != null)
                duelFieldUI.opponentNameText.text = currentOpponent.name;

            // --- SELEÇÃO DE DECK ALEATÓRIA (A, B, C) ---
            List<string> selectedMain = currentOpponent.deck_A;
            List<string> selectedExtra = currentOpponent.extra_deck_A;
            string variant = "A";

            int rng = Random.Range(0, 3);
            if (testOpponentDeckVariant == 1) rng = 0;
            else if (testOpponentDeckVariant == 2) rng = 1;
            else if (testOpponentDeckVariant == 3) rng = 2;

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

            Debug.Log($"[GameManager] Inicializando oponente: {currentOpponent.name} | Tentando Deck Variante: {variant}");

            // Carrega Main Deck
            if (selectedMain != null && selectedMain.Count > 0) 
            {
                foreach (string id in selectedMain) { 
                    CardData c = cardDatabase.GetCardById(id); 
                    if (c != null) {
                        // Pente-Fino: Se a carta for proibida e o jogo não permitir trapaça da IA, ela é removida e a de respaldo assume seu lugar!
                        if (!allowForbiddenCards && !disableBanlist && c.goat_banlist == "Banned") continue;
                        newDeck.Add(c); 
                    } else Debug.LogWarning($"[GameManager] Carta ID '{id}' não encontrada no DB para o oponente {currentOpponent.name}."); 
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] A lista do Deck {variant} do oponente {currentOpponent.name} está vazia ou nula.");
            }

            // --- FALLBACK DE VARIANTE ---
            // Se a variante escolhida (B ou C) resultou em deck vazio, tenta o Deck A
            if (newDeck.Count == 0 && variant != "A")
            {
                Debug.LogWarning($"[GameManager] Deck {variant} do oponente resultou em 0 cartas válidas. Tentando Deck A como fallback.");
                selectedMain = currentOpponent.deck_A;
                selectedExtra = currentOpponent.extra_deck_A;
                
                if (selectedMain != null)
                {
                    foreach (string id in selectedMain)
                    {
                        CardData c = cardDatabase.GetCardById(id);
                        if (c != null) newDeck.Add(c);
                    }
                }
            }

            // Carrega Extra Deck
            opponentExtraDeck.Clear();
            if (selectedExtra != null) foreach (string id in selectedExtra) { 
                CardData c = cardDatabase.GetCardById(id); 
                if (c != null) {
                    if (!allowForbiddenCards && !disableBanlist && c.goat_banlist == "Banned") continue;
                    opponentExtraDeck.Add(c); 
                } 
            }
        }
        else
        {
            // Fallback: Se não tiver oponente (Free Duel genérico), carrega tudo ou aleatório
            if (duelFieldUI != null && duelFieldUI.opponentNameText != null)
                duelFieldUI.opponentNameText.text = "Training Opponent";

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
            Debug.LogError("[GameManager] CRÍTICO: Deck do oponente vazio após todas as tentativas! Gerando deck de emergência aleatório.");
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
            // Debug.LogWarning($"Safeguard (Oponente): {oppNonFusionsInExtra.Count} carta(s) não-fusão encontradas no Extra Deck. Movendo para o Deck Principal.");
            newDeck.AddRange(oppNonFusionsInExtra);
            opponentExtraDeck.RemoveAll(c => !c.type.Contains("Fusion"));
        }
        var oppFusionsInMain = newDeck.Where(c => c.type.Contains("Fusion")).ToList();
        if (oppFusionsInMain.Any())
        {
            // Debug.LogWarning($"Safeguard (Oponente): {oppFusionsInMain.Count} carta(s) de fusão encontradas no Deck Principal. Movendo para o Extra Deck.");
            opponentExtraDeck.AddRange(oppFusionsInMain);
            newDeck.RemoveAll(c => c.type.Contains("Fusion"));
        }
        
        // Shuffle logic moved to DeckManager, but we shuffle the list here before passing
        // Actually DeckManager.SetupDecks doesn't shuffle, so we can shuffle here or call ShuffleDeck later.
        // Let's shuffle the list here.
        for (int i = 0; i < newDeck.Count; i++) { CardData temp = newDeck[i]; int r = Random.Range(i, newDeck.Count); newDeck[i] = newDeck[r]; newDeck[r] = temp; }
        
        opponentMainDeck = newDeck;
        Debug.Log($"[GameManager] Deck do oponente finalizado com {newDeck.Count} cartas (Extra: {opponentExtraDeck.Count}).");
        UpdatePileVisuals();
        return (opponentMainDeck, opponentExtraDeck);
    }

        public IEnumerator DrawInitialHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(true); // Ignora o limite para a mão inicial
            yield return new WaitForSeconds(playerDrawSpeed); // Delay configurável
        }
    }
    public IEnumerator DrawInitialOpponentHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
           DrawOpponentCard();            
           yield return new WaitForSeconds(opponentDrawSpeed); // Delay configurável
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

    public void DiscardCard(CardDisplay card, bool causedByOpponent = false)
    {
        if (card == null) return;

        if (card.isPlayerCard) playerHand.Remove(card.gameObject);
        else opponentHand.Remove(card.gameObject);

        SendToGraveyard(card.CurrentCardData, card.isPlayerCard, CardLocation.Hand, SendReason.Discarded);

        // Remove modificadores (caso raro de efeito na mão, mas seguro)
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardDiscarded(card, causedByOpponent);

        Destroy(card.gameObject);
    }

    public void DiscardRandomHand(bool isPlayer, int amount, bool causedByOpponent = false)
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
            DiscardCard(cd, causedByOpponent);
        }
    }

    // Novo método para Mind Crush e similares
    public void DiscardCardsByName(bool isPlayer, string cardName, bool causedByOpponent = false)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        // Itera de trás para frente para remover com segurança
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            GameObject go = hand[i];
            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd != null && cd.CurrentCardData.name == cardName)
            {
                DiscardCard(cd, causedByOpponent);
            }
        }
    }

    public void DiscardHand(bool isPlayer, bool causedByOpponent = false)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        // Cria uma cópia da lista para iterar com segurança enquanto removemos
        List<GameObject> toDiscard = new List<GameObject>(hand);

        foreach (GameObject cardGO in toDiscard)
        {
            CardDisplay cd = cardGO.GetComponent<CardDisplay>();
            if (cd != null) DiscardCard(cd, causedByOpponent);
        }
    }

    public void ReturnHandToDeck(bool isPlayer)
    {
        List<GameObject> hand = isPlayer ? playerHand : opponentHand;
        List<CardData> deck = isPlayer ? GetPlayerMainDeck() : GetOpponentMainDeck();
        
        foreach (GameObject cardGO in new List<GameObject>(hand))
        {
            CardDisplay cd = cardGO.GetComponent<CardDisplay>();
            if (cd != null) deck.Add(cd.CurrentCardData);
            Destroy(cardGO);
        }
        hand.Clear();
        ShuffleDeck(isPlayer);
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

        // 0343 - Criosphinx
        if (data.type.Contains("Monster") && IsCardActiveOnField("0343"))
        {
            Debug.Log("Criosphinx: Monstro retornou à mão, oponente descarta 1 carta.");
            DiscardRandomHand(!isPlayer, 1);
        }
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

        if (isPlayer) {
            playerLP -= amount;
            lpPaidThisTurnPlayer += amount;
        }
        else {
            opponentLP -= amount;
            lpPaidThisTurnOpponent += amount;
        }

        UpdateLPUI();
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} pagou {amount} LP.");
        return true;
    }

    public void GainLifePoints(bool isPlayer, int amount)
    {
        if (isPlayer) playerLP += amount;
        else opponentLP += amount;

        UpdateLPUI();
        
        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, true, isPlayer);
        }
        
        Debug.Log($"{(isPlayer ? "Player" : "Oponente")} ganhou {amount} LP.");

        // Notifica sistema de efeitos (Ex: Fire Princess)
        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnLifePointsGained(isPlayer, amount);
    }

    // Helper para adicionar carta à mão visualmente (usado por ReturnToHand e Search)
    public void AddCardToHand(CardData cardData, bool isPlayer)
    {
        // Tokens não podem existir na mão. Evaporam.
        if (cardData == null || cardData.id == "TOKEN") return;

        Transform handTransform = isPlayer ? playerHandLayoutGroup : opponentHandLayoutGroup;
        if (cardPrefab == null || handTransform == null) return;

        GameObject newCardGO = Instantiate(cardPrefab, handTransform);
        CardDisplay newCardDisplay = newCardGO.GetComponent<CardDisplay>();
        if (newCardDisplay == null) newCardDisplay = newCardGO.AddComponent<CardDisplay>();

        newCardGO.transform.localScale = handCardScale;
        newCardDisplay.hoverYOffset = isPlayer ? playerHandHoverYOffset : opponentHandHoverYOffset;
        newCardDisplay.isInteractable = true;
        newCardDisplay.isPlayerCard = isPlayer;

        // Adiciona à lista lógica IMEDIATAMENTE para o LayoutGroup calcular a posição
        if (isPlayer) playerHand.Add(newCardGO);
        else opponentHand.Add(newCardGO);

        // Lógica de animação de saque
        if (enableDrawAnimation && !isSimulating && gameObject.activeInHierarchy)
        {
            // Começa virada para baixo APENAS se for animar
            newCardDisplay.SetCard(cardData, cardBackTexture, false);
            StartCoroutine(AnimateCardToHand(newCardGO, isPlayer));
        }
        else
        {
            // Lógica antiga: Apenas mostra
            if (isPlayer) newCardDisplay.SetCard(cardData, cardBackTexture, true);
            else
            {
                newCardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
                newCardDisplay.SetCard(cardData, cardBackTexture, showOpponentHand);
            }
        }

        // Notifica adição à mão (Watapon) - Acontece aqui, pois a carta já está logicamente na mão
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardAddedToHand(newCardDisplay);
    }

    private IEnumerator AnimateCardToHand(GameObject realCard, bool isPlayer)
    {
        CardDisplay realCardDisplay = realCard.GetComponent<CardDisplay>();
        CanvasGroup realCardCG = realCard.GetComponent<CanvasGroup>();
        if (realCardCG == null) realCardCG = realCard.AddComponent<CanvasGroup>();
        realCardCG.alpha = 0; // Esconde a carta real

        // Espera o LayoutGroup fazer sua mágica
        yield return new WaitForEndOfFrame();

        // Cria um "fantasma" na posição do deck para animar
        Transform startZone = isPlayer ? playerDeckDisplay.transform : opponentDeckDisplay.transform;
        if (startZone == null) { // Fallback de segurança se a pilha não existir
             realCardCG.alpha = 1;
             if (isPlayer) realCardDisplay.ShowFront(false);
             else if (showOpponentHand) realCardDisplay.ShowFront(false);
             else realCardDisplay.ShowBack(false);
             yield break;
        }

        // OTIMIZAÇÃO: Instancia o prefab mas não chama SetCard() para evitar o lag de carregar a textura duas vezes.
        GameObject ghost = Instantiate(cardPrefab, GetUIParent());
        CardDisplay ghostDisplay = ghost.GetComponent<CardDisplay>();
        if (ghostDisplay != null && ghostDisplay.cardImage != null)
        {
            ghostDisplay.cardImage.texture = cardBackTexture;
        }
        ghost.transform.position = startZone.position;

        // Anima o fantasma até a posição da carta real
        Vector3 endPos = realCard.transform.position;
        float duration = 0.6f / (DuelFXManager.Instance != null && DuelFXManager.Instance.animationSpeed > 0 ? DuelFXManager.Instance.animationSpeed : 1f);
        float elapsed = 0f;
        bool textureSwapped = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            ghost.transform.position = Vector3.Lerp(startZone.position, endPos, t);

            // Rotação 3D suave no eixo Y para simular o flip natural
            float yRot = Mathf.Lerp(180f, 0f, t);
            ghost.transform.rotation = Quaternion.Euler(0, yRot, 0);

            // Vira a textura na metade do caminho
            if (t >= 0.5f && !textureSwapped)
            {
                textureSwapped = true;
                // FIX: Só revela a frente se for o jogador OU se a opção de ver a mão do oponente estiver ativa
                if (isPlayer || showOpponentHand)
                {
                    Texture2D frontTex = realCardDisplay.GetFrontTexture();
                    if (frontTex != null) ghostDisplay.ForceTexture(frontTex);
                }
            }
            yield return null;
        }

        // Destrói o fantasma e revela a carta real
        Destroy(ghost);
        
        // Garante o estado final correto da carta real ANTES de torná-la visível
        if (isPlayer) 
        {
            realCardDisplay.ShowFront(false); // Chama ShowFront SEM animar para evitar o "duplo flip"
        }
        else
        {
            if (showOpponentHand) realCardDisplay.ShowFront(false);
            else realCardDisplay.ShowBack(false);
        }
        
        realCardCG.alpha = 1;
    }

    public void DrawCard(bool ignoreLimit = false)
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetPlayerDeck() != null && DeckManager.Instance.GetPlayerDeck().Count > 0)
        {
            DeckManager.Instance.DrawCard(true, ignoreLimit);
        }
        // Retiramos o avanço de fase redundante daqui, pois o DeckManager já cuida disso.
    }

    public void DrawOpponentCard()
    {
        if (DeckManager.Instance != null && DeckManager.Instance.GetOpponentDeck() != null && DeckManager.Instance.GetOpponentDeck().Count > 0)
        {
            DeckManager.Instance.DrawCard(false);
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
    public void SendToGraveyard(CardData card, bool isPlayer, CardLocation fromLocation = CardLocation.Unknown, SendReason reason = SendReason.Unknown)
    {
        // Tokens evaporam ao sair do campo, não entram no GY.
        if (card == null || card.id == "TOKEN") return;

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

    // === MÉTODO UNIFICADO DE MOVIMENTAÇÃO DE CARTAS ===
    /// <summary>
    /// Move uma carta entre zonas de forma centralizada e consistente.
    /// Detecta automaticamente a zona de origem e aplica a lógica apropriada.
    /// </summary>
    public void MoveCard(CardDisplay card, CardLocation destination, SendReason reason = SendReason.Unknown)
    {
        if (card == null) return;
        
        bool isPlayer = card.isPlayerCard;
        CardData data = card.CurrentCardData;
        string logPrefix = $"[MoveCard] {data.name} ({(isPlayer ? "Player" : "Opponent")})";
        
        // FASE 13: Track previous location before moving
        card.previousPreviousLocation = card.previousLocation;
        card.previousLocation = card.CurrentLocation;
        card.previousOwner = (isPlayer ? 0 : 1);
        Debug.Log($"{logPrefix} | Tracked history: from {card.previousLocation} (owner: {card.previousOwner})");
        
        // Remove modificadores antes de sair do campo
        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(card);

        // Aplica a movimentação baseada no destino
        switch (destination)
        {
            case CardLocation.Graveyard:
                Debug.Log($"{logPrefix} → Graveyard");
                SendToGraveyard(data, isPlayer, CardLocation.Field, reason);
                Destroy(card.gameObject);
                break;

            case CardLocation.Hand:
                Debug.Log($"{logPrefix} → Hand");
                // Remove da mão se estava lá
                if (isPlayer) playerHand.Remove(card.gameObject);
                else opponentHand.Remove(card.gameObject);
                // Destrói e retorna à mão
                Destroy(card.gameObject);
                AddCardToHand(data, isPlayer);
                break;

            case CardLocation.Deck:
                Debug.Log($"{logPrefix} → Deck (Top)");
                // Delega para DeckManager
                if (DeckManager.Instance != null) 
                    DeckManager.Instance.ReturnToDeck(card, true);
                break;

            case CardLocation.Banished:
                Debug.Log($"{logPrefix} → Banished");
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayBanishEffect(card);
                if (isPlayer) playerHand.Remove(card.gameObject);
                else opponentHand.Remove(card.gameObject);
                RemoveFromPlay(data, isPlayer);
                Destroy(card.gameObject);
                break;

            case CardLocation.ExtraDeck:
                Debug.Log($"{logPrefix} → Extra Deck");
                if (isPlayer) playerExtraDeck.Add(data);
                else opponentExtraDeck.Add(data);
                Destroy(card.gameObject);
                break;

            default:
                Debug.LogWarning($"{logPrefix} → Destino desconhecido: {destination}");
                break;
        }
    }

    // === CONSULTAS DE ESPAÇO LIVRE ===
    /// <summary>
    /// Retorna quantidade de zonas de monstro livres para invocação
    /// </summary>
    public int GetFreeMonsterZones(bool isPlayer)
    {
        if (duelFieldUI == null) return 0;
        
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        int freeCount = 0;
        
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount == 0)
                freeCount++;
        }
        
        return freeCount;
    }

    /// <summary>
    /// Retorna quantidade de zonas de S/T livres para setar
    /// </summary>
    public int GetFreeSpellTrapZones(bool isPlayer)
    {
        if (duelFieldUI == null) return 0;
        
        Transform[] zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        int freeCount = 0;
        
        foreach (var zone in zones)
        {
            if (zone != null && zone.childCount == 0)
                freeCount++;
        }
        
        return freeCount;
    }

    // Novo método para remover de jogo (Banir)
    public void RemoveFromPlay(CardData card, bool isPlayer)
    {
        // Tokens evaporam, não vão para a pilha de banidos
        if (card == null || card.id == "TOKEN") return;

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

    public void UpdateCardViewer(CardDisplay hoveredCard, bool isFaceUp)
    {
        if (cardViewerDisplay == null) return;

        if (isFaceUp && hoveredCard != null && hoveredCard.CurrentCardData != null)
        {
            cardViewerDisplay.SetCard(hoveredCard.CurrentCardData, cardBackTexture, true);
            
            // ATUALIZAÇÃO: Se for monstro, injeta os valores dinâmicos com cores (Verde = Buff, Vermelho = Debuff)
            if (hoveredCard.CurrentCardData.type.Contains("Monster") && cardViewerDisplay.cardStatsText != null)
            {
                cardViewerDisplay.currentAtk = hoveredCard.currentAtk;
                cardViewerDisplay.currentDef = hoveredCard.currentDef;

                string atkColor = hoveredCard.currentAtk > hoveredCard.originalAtk ? "<color=#00FF00>" : (hoveredCard.currentAtk < hoveredCard.originalAtk ? "<color=#FF4444>" : "");
                string defColor = hoveredCard.currentDef > hoveredCard.originalDef ? "<color=#00FF00>" : (hoveredCard.currentDef < hoveredCard.originalDef ? "<color=#FF4444>" : "");
                
                cardViewerDisplay.cardStatsText.text = $"ATK/ {atkColor}{hoveredCard.currentAtk}{(atkColor != "" ? "</color>" : "")}  DEF/ {defColor}{hoveredCard.currentDef}{(defColor != "" ? "</color>" : "")}";
            }
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

        if (isPlayerTurn) {
            lpPaidLastTurnPlayer = lpPaidThisTurnPlayer;
            lpPaidThisTurnPlayer = 0;
        } else {
            lpPaidLastTurnOpponent = lpPaidThisTurnOpponent;
            lpPaidThisTurnOpponent = 0;
        }

        if (PhaseManager.Instance != null) PhaseManager.Instance.StartTurn();

        // Atualiza as cores de hover dos botões de fase para o turno atual
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.UpdateHoverColors(isPlayerTurn);
        }
        
        // Se for turno do oponente, inicia a IA
        if (!isPlayerTurn && OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeInHierarchy)
        {
            OpponentAI.Instance.StartAITurn(!isSimulating);
        }
    }

    // Chamado pelo PhaseManager quando entra na Draw Phase
    public void OnDrawPhaseStart()
    {
        normalSummonsThisTurnPlayer = 0;
        normalSummonsThisTurnOpponent = 0;

        // Reseta os ataques dos monstros em campo
        if (duelFieldUI != null)
        {
            if (duelFieldUI.playerMonsterZones != null)
            {
                foreach (var zone in duelFieldUI.playerMonsterZones)
                {
                    if (zone != null && zone.childCount > 0)
                    {
                        var card = zone.GetChild(0).GetComponent<CardDisplay>();
                        if (card != null) {
                            card.hasAttackedThisTurn = false;
                            card.hasChangedPositionThisTurn = false;
                        }
                    }
                }
            }
            if (duelFieldUI.opponentMonsterZones != null)
            {
                foreach (var zone in duelFieldUI.opponentMonsterZones)
                {
                    if (zone != null && zone.childCount > 0)
                    {
                        var card = zone.GetChild(0).GetComponent<CardDisplay>();
                        if (card != null) {
                            card.hasAttackedThisTurn = false;
                            card.hasChangedPositionThisTurn = false;
                        }
                    }
                }
            }
        }

        turnCount++; // Incrementa o turno
        if (DeckManager.Instance != null) DeckManager.Instance.ResetTurnStats();
        
        // Validação da Regra do Primeiro Turno
        bool skipDraw = (turnCount == 1 && applyModernFirstTurnDrawRule);

        if (isPlayerTurn)
        {
            if (!canPlayerDrawFromDeck && !skipDraw)
            {
                for (int i = 0; i < 1; i++)
                    DrawCard();
            }
            else if (skipDraw) Debug.Log("[GameManager] Regra Moderna: Turno 1. Nenhuma carta comprada.");
        }
        else
        {
            if (!skipDraw) DrawOpponentCard();
            else Debug.Log("[GameManager] Regra Moderna: Turno 1 (Oponente). Nenhuma carta comprada.");
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

        // Inicia verificação de Limite de Mão
        StartCoroutine(HandleHandLimitSequence());
    }

    private IEnumerator HandleHandLimitSequence()
    {
        // Verifica se a regra está ativa
        if (!enableHandLimit)
        {
            // Se for turno do jogador, troca o turno automaticamente (já que não haverá descarte)
            if (isPlayerTurn && !isSimulating)
            {
                yield return new WaitForSeconds(0.5f);
                SwitchTurn();
            }
            yield break;
        }

        // Verifica Infinite Cards (0942)
        int currentLimit = handLimit;
        if (IsCardActiveOnField("Infinite Cards") || IsCardActiveOnField("0942")) currentLimit = 99;

        List<GameObject> hand = isPlayerTurn ? playerHand : opponentHand;
        
        if (hand.Count > currentLimit)
        {
            int toDiscard = hand.Count - currentLimit;
            Debug.Log($"[GameManager] Limite de mão excedido ({hand.Count}/{currentLimit}). Descartando {toDiscard} cartas.");

            if (isPlayerTurn)
            {
                if (isSimulating)
                {
                    for (int i = 0; i < toDiscard; i++) DiscardCard(playerHand[0].GetComponent<CardDisplay>());
                }
                else
                {
                    yield return StartCoroutine(PlayerDiscardHandLimit(toDiscard));
                }
            }
            else
            {
                // IA descarta
                if (OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeSelf && !isSimulating)
                {
                    OpponentAI.Instance.PerformHandLimitDiscard(toDiscard);
                    yield return new WaitForSeconds(1.0f); // Tempo para visualização
                }
                else if (isSimulating)
                {
                    for (int i = 0; i < toDiscard; i++) DiscardCard(opponentHand[0].GetComponent<CardDisplay>());
                }
            }
        }

        // Se for turno do jogador, troca o turno automaticamente após processar a End Phase e Limite de Mão
        // (A IA troca o turno no final da rotina dela, então não precisamos chamar aqui para ela)
        if (isPlayerTurn && !isSimulating)
        {
            yield return new WaitForSeconds(0.5f);
            SwitchTurn();
        }
    }

    private IEnumerator PlayerDiscardHandLimit(int count)
    {
        bool done = false;
        List<CardData> handData = GetPlayerHandData();
        
        if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"Limite de mão excedido. Descarte {count} cartas.");
        yield return new WaitForSeconds(1.5f);

        OpenCardMultiSelection(handData, $"Descarte {count} cartas (Limite de Mão)", count, count, (selected) => {
            foreach(var c in selected)
            {
                GameObject go = playerHand.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == c);
                if (go != null) DiscardCard(go.GetComponent<CardDisplay>());
            }
            done = true;
        });

        while (!done) yield return null;
    }

    // --- SISTEMA DE MOEDAS ---
    public void TossCoin(int numberOfCoins, System.Action<int> onResult)
    {
        if (isSimulating)
        {
            int headsCount = 0;
            for (int i = 0; i < numberOfCoins; i++)
                if (alwaysCoinHead || Random.value > 0.5f) headsCount++;
            onResult?.Invoke(headsCount);
            return;
        }

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
            
            // Debug: Força Cara se a opção estiver ativa
            if (alwaysCoinHead) isHeads = true;

            if (isHeads) headsCount++;
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage(isHeads ? "CARA!" : "COROA!");
            yield return new WaitForSeconds(0.5f);
        }

        // --- SISTEMA 3: SECOND COIN TOSS ---
        if (CardEffectManager.Instance != null && CardEffectManager.Instance.HasActiveSecondCoinToss(out bool isPlayerToss))
        {
            if (isPlayerToss && UIManager.Instance != null)
            {
                bool isWaiting = true;
                bool doReroll = false;
                UIManager.Instance.ShowConfirmation($"Você tirou {headsCount} cara(s). Usar Second Coin Toss para tentar de novo?",
                    () => { doReroll = true; isWaiting = false; },
                    () => { isWaiting = false; }
                );
                while(isWaiting) yield return null;

                if (doReroll)
                {
                    CardEffectManager.Instance.ConsumeSecondCoinToss(true);
                    StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
                    yield break; // Cancela este fluxo e inicia o novo
                }
            }
            else if (!isPlayerToss && headsCount == 0) // IA simples: Rerola se tirou 0 caras
            {
                CardEffectManager.Instance.ConsumeSecondCoinToss(false);
                StartCoroutine(CoinTossRoutine(numberOfCoins, onResult));
                yield break;
            }
        }
        
        onResult?.Invoke(headsCount);
    }

    // --- SISTEMA DE DADOS ---
    public void RollDice(int count, bool requireChoice, Action<List<int>> callback)
    {
        if (isSimulating)
        {
            List<int> results = new List<int>();
            for (int i = 0; i < count; i++) results.Add(alwaysDiceSix ? 6 : UnityEngine.Random.Range(1, 7));
            callback?.Invoke(results);
            return;
        }

        Action<List<int>> interceptCallback = (results) => {
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.HasActiveDiceReRoll(out bool isPlayerToss))
            {
                if (isPlayerToss && UIManager.Instance != null)
                {
                    string resStr = string.Join(", ", results);
                    UIManager.Instance.ShowConfirmation($"Você tirou {resStr} nos dados. Usar Dice Re-Roll para tentar de novo?",
                        () => {
                            CardEffectManager.Instance.ConsumeDiceReRoll(true);
                            RollDice(count, requireChoice, callback);
                        },
                        () => {
                            callback?.Invoke(results);
                        }
                    );
                }
                else if (!isPlayerToss) // IA simples: Rerola se a soma for muito baixa
                {
                    int sum = results.Sum();
                    if (sum <= count * 3) {
                        CardEffectManager.Instance.ConsumeDiceReRoll(false);
                        RollDice(count, requireChoice, callback);
                    } else {
                        callback?.Invoke(results);
                    }
                }
            }
            else
            {
                callback?.Invoke(results);
            }
        };

        if (diceRollUI != null)
        {
            diceRollUI.ShowRoll(count, requireChoice, diceRollManualSpin, interceptCallback, alwaysDiceSix);
        }
        else
        {
            List<int> results = new List<int>();
            for (int i = 0; i < count; i++) results.Add(alwaysDiceSix ? 6 : UnityEngine.Random.Range(1, 7));
            interceptCallback(results);
        }
    }

    // --- MENU DE SELEÇÃO DE FASE (Botão Direito) ---
    public void OpenPhaseSelectionMenu()
    {
        if (PhaseManager.Instance == null) return;

        GamePhase current = PhaseManager.Instance.currentPhase;
        List<CardData> phaseOptions = new List<CardData>();

        // Helper para criar "Cartas de Fase"
        void AddPhaseOption(GamePhase phase, string desc)
        {
            CardData p = new CardData();
            p.id = "PHASE_" + phase.ToString();
            p.name = phase.ToString().Replace("Main", "Main ").Replace("1", " 1").Replace("2", " 2"); // Ex: Main Phase 1
            p.description = desc;
            p.type = "Game Phase";
            phaseOptions.Add(p);
        }

        // Lógica de Fases Possíveis
        if (devMode)
        {
            // DevMode: Todas as fases
            AddPhaseOption(GamePhase.Draw, "Ir para Draw Phase");
            AddPhaseOption(GamePhase.Standby, "Ir para Standby Phase");
            AddPhaseOption(GamePhase.Main1, "Ir para Main Phase 1");
            AddPhaseOption(GamePhase.Battle, "Ir para Battle Phase");
            AddPhaseOption(GamePhase.Main2, "Ir para Main Phase 2");
            AddPhaseOption(GamePhase.End, "Ir para End Phase");
        }
        else
        {
            // Fluxo Normal
            if (current == GamePhase.Draw) AddPhaseOption(GamePhase.Standby, "Avançar para Standby Phase");
            else if (current == GamePhase.Standby) AddPhaseOption(GamePhase.Main1, "Avançar para Main Phase 1");
            else if (current == GamePhase.Main1)
            {
                AddPhaseOption(GamePhase.Battle, "Entrar na Battle Phase");
                AddPhaseOption(GamePhase.End, "Encerrar Turno");
            }
            else if (current == GamePhase.Battle)
            {
                AddPhaseOption(GamePhase.Main2, "Ir para Main Phase 2");
                AddPhaseOption(GamePhase.End, "Encerrar Turno");
            }
            else if (current == GamePhase.Main2) AddPhaseOption(GamePhase.End, "Encerrar Turno");
        }

        if (phaseOptions.Count > 0)
        {
            OpenCardMultiSelection(phaseOptions, "Selecionar Próxima Fase", 1, 1, (selected) => {
                if (selected != null && selected.Count > 0)
                {
                    string phaseName = selected[0].id.Replace("PHASE_", "");
                    if (System.Enum.TryParse(phaseName, out GamePhase nextPhase))
                    {
                        TryChangePhase(nextPhase);
                    }
                }
            });
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
        string fullPath = Path.Combine(Application.streamingAssetsPath, "DMCardImages/0000 - Background.jpg");
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
        // 1. Mostra a mensagem de WIN/LOSE (Pula no modo simulador rápido para não atrasar)
        if (!isSimulating && UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            Transform winImg = UIManager.Instance.endDuelMessagePanel.transform.Find("ImageWin");
            Transform loseImg = UIManager.Instance.endDuelMessagePanel.transform.Find("ImageLose");
            
            if (winImg != null) winImg.gameObject.SetActive(playerWon);
            if (loseImg != null) loseImg.gameObject.SetActive(!playerWon);

            CanvasGroup cg = UIManager.Instance.endDuelMessagePanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = UIManager.Instance.endDuelMessagePanel.AddComponent<CanvasGroup>();
            
            cg.alpha = 0f;
            UIManager.Instance.endDuelMessagePanel.SetActive(true);

            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
                yield return null;
            }
            cg.alpha = 1f;
        }

        // Pausa dramática apenas se não estiver simulando
        if (!isSimulating) yield return new WaitForSeconds(1.5f);

        GameObject blackOverlay = null;
        Image blackImg = null;

        // 2. Transição suave para tela preta antes de esconder a mensagem e ir pros rewards
        if (!isSimulating && UIManager.Instance != null && UIManager.Instance.endDuelMessagePanel != null)
        {
            blackOverlay = new GameObject("BlackOverlay_EndDuel", typeof(RectTransform), typeof(Canvas), typeof(Image));
            Canvas canvas = blackOverlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // Fica por cima de tudo
            
            blackImg = blackOverlay.GetComponent<Image>();
            blackImg.color = new Color(0, 0, 0, 0);

            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                blackImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, elapsed / fadeTime));
                yield return null;
            }

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
            bool isNewCard = false;

            if (playerWon)
            {
                if (currentOpponent != null)
                {
                    // Registra vitória na Biblioteca
                    if (SaveLoadSystem.Instance != null)
                    {
                        SaveLoadSystem.Instance.RegisterDuelistWin(currentOpponent.id);
                    }

                    // --- SISTEMA DE DROP RATE PERCENTUAL ---
                    rewardCard = CalculateDrop(rank, currentOpponent);
                }

                // FALLBACK DE TESTE: Se não tiver oponente ou o drop dele falhar, dá uma carta aleatória do banco inteiro!
                if (rewardCard == null && cardDatabase != null && cardDatabase.cardDatabase.Count > 0)
                {
                    rewardCard = cardDatabase.cardDatabase[UnityEngine.Random.Range(0, cardDatabase.cardDatabase.Count)];
                }

                Debug.Log($"Recompensa Drop Rate: {rewardCard?.name ?? "Nenhuma"}");

                // LÓGICA DE DROP: Adiciona ao Baú do Jogador
                if (rewardCard != null)
                {
                    // Verifica se é nova (não existe no baú) ANTES de adicionar
                    isNewCard = !playerTrunk.Contains(rewardCard.id);

                    playerTrunk.Add(rewardCard.id);
                    
                    // Salva o progresso imediatamente para garantir o drop
                    if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame(currentSaveID);
                }
            }
            
            if (!isSimulating && UIManager.Instance != null)
            {
                // Se venceu, passa o Rank real. Se perdeu, passa "LOSE" para a UI de Recompensas saber.
                if (playerWon) 
                {
                    UIManager.Instance.ShowRewardScreen(rank.ToString(), rewardCard, isNewCard);
                }
                else 
                {
                    UIManager.Instance.ShowRewardScreen("LOSE", null, false);
                }
            }
        }
        else
        {
            if (UIManager.Instance != null) UIManager.Instance.Btn_BackToMenu();
        }

        // 4. Remove a tela preta suavemente revelando a tela de Rewards
        if (blackOverlay != null && blackImg != null)
        {
            float fadeTime = 1.0f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                blackImg.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
                yield return null;
            }
            Destroy(blackOverlay);
        }
    }

    private CardData CalculateDrop(DuelRank rank, CharacterData opp)
    {
        // Percentuais definidos na documentação do Game Design (ScoringAndDropRate.md)
        float pSPlus = 0, pS = 0, pB = 0, pC = 0, pD = 0;

        switch (rank)
        {
            case DuelRank.SPlus: pSPlus = 15f; pS = 34f; pB = 25.5f; pC = 17f; pD = 8.5f; break;
            case DuelRank.S:
            case DuelRank.APlus:
            case DuelRank.A:     pSPlus = 0f; pS = 40f; pB = 30f; pC = 20f; pD = 10f; break;
            case DuelRank.BPlus:
            case DuelRank.B:     pSPlus = 0f; pS = 10f; pB = 40f; pC = 30f; pD = 20f; break;
            case DuelRank.CPlus:
            case DuelRank.C:     pSPlus = 0f; pS = 3f; pB = 17f; pC = 40f; pD = 40f; break;
            case DuelRank.DPlus:
            case DuelRank.D:     pSPlus = 0f; pS = 1f; pB = 4f; pC = 25f; pD = 70f; break;
            case DuelRank.F:     return null;
        }

        List<string> poolSPlus = new List<string>();
        List<string> poolS = new List<string>();
        List<string> poolB = new List<string>();
        List<string> poolC = new List<string>();
        List<string> poolD = new List<string>();

        // Os drops S+ (A Única) e S (Super Raros) vêm diretamente do "unique_drops" gerado pelo Python
        if (opp.unique_drops != null && opp.unique_drops.Count > 0)
        {
            poolSPlus.Add(opp.unique_drops[0]); // Primeira carta é a Assinatura S+
            for (int i = 1; i < opp.unique_drops.Count; i++)
            {
                poolS.Add(opp.unique_drops[i]);
            }
        }

        // Coleta todas as cartas de todos os decks do personagem para fatiar nas tiers inferiores
        HashSet<string> allDeckCards = new HashSet<string>();
        if (opp.deck_A != null) foreach (var id in opp.deck_A) allDeckCards.Add(id);
        if (opp.deck_B != null) foreach (var id in opp.deck_B) allDeckCards.Add(id);
        if (opp.deck_C != null) foreach (var id in opp.deck_C) allDeckCards.Add(id);
        if (opp.extra_deck_A != null) foreach (var id in opp.extra_deck_A) allDeckCards.Add(id);
        if (opp.extra_deck_B != null) foreach (var id in opp.extra_deck_B) allDeckCards.Add(id);
        if (opp.extra_deck_C != null) foreach (var id in opp.extra_deck_C) allDeckCards.Add(id);

        // Remove as cartas que já foram separadas como S/S+
        foreach (var id in poolSPlus) allDeckCards.Remove(id);
        foreach (var id in poolS) allDeckCards.Remove(id);

        // Prepara para ordenar pelo poder do Pool (Ex: 5.5 -> 1.1)
        List<CardData> fillerCards = new List<CardData>();
        foreach (var id in allDeckCards)
        {
            CardData c = cardDatabase.GetCardById(id);
            if (c != null) fillerCards.Add(c);
        }

        fillerCards.Sort((x, y) => {
            float px = 1.1f; float py = 1.1f;
            float.TryParse(x.pool, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out px);
            float.TryParse(y.pool, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out py);
            int comp = py.CompareTo(px);
            if (comp == 0) return y.atk.CompareTo(x.atk); // Desempata pelo ATK
            return comp;
        });

        // Fatiamento das cartas restantes (25% B, 30% C, 45% D)
        int totalFiller = fillerCards.Count;
        int bCount = Mathf.CeilToInt(totalFiller * 0.25f);
        int cCount = Mathf.CeilToInt(totalFiller * 0.30f);

        for (int i = 0; i < totalFiller; i++)
        {
            if (i < bCount) poolB.Add(fillerCards[i].id);
            else if (i < bCount + cCount) poolC.Add(fillerCards[i].id);
            else poolD.Add(fillerCards[i].id);
        }

        // Fallbacks de segurança (evita lista vazia)
        if (poolSPlus.Count == 0) pSPlus = 0f;
        if (poolS.Count == 0) { poolS.AddRange(poolB); pS = pS / 2f; } // Reduz a chance de S se não houver drops únicos reais
        if (poolB.Count == 0) poolB.AddRange(poolC);
        if (poolC.Count == 0) poolC.AddRange(poolD);
        if (poolD.Count == 0) poolD.AddRange(poolC);

        float totalWeight = pSPlus + pS + pB + pC + pD;
        if (totalWeight <= 0) return null;

        // Roleta de Porcentagens
        float roll = Random.Range(0f, totalWeight);
        List<string> selectedPool = null;

        if (roll < pSPlus && poolSPlus.Count > 0) selectedPool = poolSPlus;
        else
        {
            roll -= pSPlus;
            if (roll < pS) selectedPool = poolS;
            else
            {
                roll -= pS;
                if (roll < pB) selectedPool = poolB;
                else
                {
                    roll -= pB;
                    if (roll < pC) selectedPool = poolC;
                    else selectedPool = poolD;
                }
            }
        }

        if (selectedPool == null || selectedPool.Count == 0) selectedPool = poolD;

        if (selectedPool != null && selectedPool.Count > 0)
        {
            string finalId = selectedPool[Random.Range(0, selectedPool.Count)];
            return cardDatabase.GetCardById(finalId);
        }

        return null;
    }

    // Alterna entre Tela Cheia e Modo Janela
    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        Debug.Log($"[GameManager] Fullscreen alterado para: {Screen.fullScreen}");
    }

    // --- SISTEMA DE DANO E LP ---

    public void DamagePlayer(int amount)
    {
        if (infiniteLP) return;

        playerLP -= amount;
        if (playerLP < 0) playerLP = 0;
        UpdateLPUI();

        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, false, true);
        }

        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageTaken(amount);
        Debug.Log($"Player tomou {amount} de dano. LP Restante: {playerLP}");

        // TROFÉU: Dano Sofrido
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("damage_taken", amount);

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

        if (enableDamagePopups && DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.ShowPopup(amount, false, false);
        }

        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordDamageDealt(amount);
        Debug.Log($"Oponente tomou {amount} de dano. LP Restante: {opponentLP}");

        // TROFÉU: Dano Causado
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("damage_dealt", amount);

        // Atualiza a música baseada na nova situação de vida
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.UpdateBGM(playerLP, opponentLP);

        if (opponentLP <= 0) EndDuel(true);

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
        string[] exodiaParts = { "DM0595", "DM1490", "DM1030", "DM1491", "DM1031" };
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
            // TROFÉU: Exodia
            if (TrophyManager.Instance != null) TrophyManager.Instance.Unlock(80);
            
            // Tenta forçar a busca caso o painel tenha começado desativado no Inspector
            if (ExodiaWinUI.Instance == null)
            {
                ExodiaWinUI.Instance = Resources.FindObjectsOfTypeAll<ExodiaWinUI>().FirstOrDefault();
            }

            if (ExodiaWinUI.Instance != null)
            {
                ExodiaWinUI.Instance.ShowWinSequence(true, () => EndDuel(true));
            }
            else
            {
                EndDuel(true);
            }
        }
    }

    private void UpdateLPUI()
    {
        if (duelFieldUI != null)
        {
            if (duelFieldUI.playerLPText != null) 
                duelFieldUI.playerLPText.text = playerLP.ToString();
            else 
                Debug.LogWarning("GameManager: PlayerLPText não atribuído no DuelFieldUI.");

            if (duelFieldUI.opponentLPText != null) 
                duelFieldUI.opponentLPText.text = opponentLP.ToString();
            else
                Debug.LogWarning("GameManager: OpponentLPText não atribuído no DuelFieldUI.");
        }
        else
        {
            Debug.LogWarning("GameManager: Tentativa de atualizar LP, mas DuelFieldUI é nulo.");
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

    public int GetMonsterCount(bool isPlayer)
    {
        int count = 0;
        if (duelFieldUI != null)
        {
            Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
            foreach (var z in zones) if (z.childCount > 0) count++;
        }
        return count;
    }

    // --- LÓGICA DE INVOCACÃO ---

    // Renomeado para TrySummonMonster para indicar que é o início do processo
    public bool TrySummonMonster(GameObject cardGO, CardData cardData, bool isSet, bool ignoreLimit = false)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;
        string cardName = cardData?.name ?? "Unknown";

        // 0.1 Validação de Limite de Invocação Normal (se não for ignorado por efeito)
        if (!ignoreLimit && !infiniteNormalSummons)
        {
            int currentSummons = isPlayer ? normalSummonsThisTurnPlayer : normalSummonsThisTurnOpponent;
            if (currentSummons > 0)
            {
                Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: Limite de 1 Invocação Normal por turno atingido.");
                if (isPlayer && !isSimulating && UIManager.Instance != null) 
                {
                    UIManager.Instance.ShowMessage("Você já realizou uma Invocação Normal/Set neste turno.");
                }
                return false;
            }
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: canPlacePlayerCards desativado.");
            return false;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        // A IA (que roda no turno do oponente) não será bloqueada por esta verificação.
        // FIX: Se estiver simulando (!isSimulating), ignora essa trava para permitir que o simulador jogue pelos dois lados rapidamente.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards && !isSimulating)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: canPlaceOpponentCards desativado durante turno do jogador.");
            return false;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2 && !isSimulating)
        {
            Debug.LogWarning($"[TrySummonMonster BLOCKED] {cardName}: Invocação só permitida em Main Phase 1/2. Fase atual: {currentPhase}");
            return false;
        }

        if (CardEffectManager.Instance != null)
        {
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(display);
            if (lc == null) lc = new LuaCard(display);

            int tp = isPlayer ? 0 : 1;
            var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("NormalSummon").Function;
            CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, tp, lc, isSet));
        }
        else
        {
            if (!ignoreLimit && !infiniteNormalSummons) {
                if (isPlayer) normalSummonsThisTurnPlayer++;
                else normalSummonsThisTurnOpponent++;
            }
            FinalizeSummon(cardGO, cardData, isSet, isPlayer, isSet, cardData.level >= 5, null);
        }
        
        return true;
    }

    // Novo método para Special Summon que pede a posição
    public void PerformSpecialSummon(GameObject cardGO, CardData cardData)
    {
        if (isSimulating)
        {
            FinalizeSummon(cardGO, cardData, false, true, false); // false = Face-Up
            return;
        }

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
    public void FinalizeSummon(GameObject cardGO, CardData cardData, bool isDefensePos, bool isPlayer, bool isFaceDown = false, bool isTributeSummon = false, Transform specificZone = null, List<CardData> specialMaterials = null)
    {
        // 2. Encontrar Zona Livre
        Transform targetZone = specificZone;
        if (targetZone == null) targetZone = GetFreeMonsterZone(isPlayer);

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
            display.summonedTurnCount = turnCount; // Registra o turno de invocação
            display.hasChangedPositionThisTurn = false;

            // 1081 - Light of Intervention
            if (isFaceDown && IsCardActiveOnField("1081"))
            {
                isFaceDown = false;
                Debug.Log("Light of Intervention: Monstro Setado forçado a Face-up Defense.");
            }

            if (isDefensePos)
            {
                display.position = CardDisplay.BattlePosition.Defense;
                // Modo Defesa (Set): Virado para baixo e Rotacionado 90 graus
                float zRotation = isPlayer ? 90f : -90f;

                if (isFaceDown) display.ShowBack(); else display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }
            else
            {
                display.position = CardDisplay.BattlePosition.Attack;
                // Modo Ataque: Virado para cima e Reto
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
            }

            // Toca efeito visual e notifica a Corrente (Trap Holes)
            bool isFusion = cardData.type.Contains("Fusion");
            bool isRitual = cardData.type.Contains("Ritual");

            // Adiciona o display de stats se a opção estiver ativa (COPIADO DE SpecialSummonFromData)
            if (monsterStatDisplayMode != StatDisplayMode.None && fieldStatDisplayPrefab != null && cardData.type.Contains("Monster"))
            {
                GameObject statGO = Instantiate(fieldStatDisplayPrefab, cardGO.transform.parent);
                FieldStatUI statUI = statGO.GetComponent<FieldStatUI>();
                if (statUI != null)
                {
                    statUI.Setup(display);
                }
                float adjustment = (monsterStatDisplayMode == StatDisplayMode.AboveCard) ? -cardYAdjustmentForStats : cardYAdjustmentForStats;
                if (statDisplayMatchCardRotation && !isPlayer) adjustment = -adjustment; // oponente inverte
                cardGO.transform.localPosition += new Vector3(0, adjustment, 0);
            }

            bool useCinematic = false;
            
            if (isFusion) useCinematic = enableFusionCinematic && !isSimulating && !isFaceDown;
            else if (isRitual) useCinematic = enableRitualCinematic && !isSimulating && !isFaceDown;
            else useCinematic = enableSummonCinematics && isTributeSummon && !isSimulating && !isFaceDown;
            
            if (useCinematic && DuelFXManager.Instance != null)
            {
                display.SetVisibility(false);
                System.Action onCinematicComplete = () => {
                    display.SetVisibility(true);
                    DuelFXManager.Instance.PlaySummonAura(display);
                    if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSummon(display);
                };

                if (isFusion)
                {
                    DuelFXManager.Instance.PlayFusionCinematic(display, specialMaterials, null, onCinematicComplete);
                }
                else if (isRitual)
                {
                    DuelFXManager.Instance.PlayRitualCinematic(display, null, onCinematicComplete);
                }
                else
                {
                    DuelFXManager.Instance.PlaySummonCinematic(display, true, false, onCinematicComplete);
                }
            }
            else
            {
                if (DuelFXManager.Instance != null && !isDefensePos)
                {
                    if (isTributeSummon && enableTributeSummonAnimation)
                        DuelFXManager.Instance.PlayTributeSummonEffect(display);
                    else
                        DuelFXManager.Instance.PlaySummonEffect(display);
                }
                else if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayFlipEffect(display); // Poeirinha ao setar monstro
            
                // Sempre toca a Aura de Pouso ao colocar na mesa, independente da posição
                if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayPlacementAura(display);

                // Dispara gatilho síncrono
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSummon(display);
            }
        }

        // 5. Pontuação
        if (DuelScoreManager.Instance != null) DuelScoreManager.Instance.RecordSummon();
        
        // TROFÉU: Tributo
        if (isTributeSummon && TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("tribute_summon", 1);

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnSummon(display);
    }

    public Transform GetFreeMonsterZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerMonsterZones : duelFieldUI.opponentMonsterZones;
        if (zones == null) return null;

        // Se for oponente e a opção estiver marcada, inverte a ordem de busca
        if (!isPlayer && fillOpponentZonesFromRight)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (zones[i].childCount == 0) return zones[i];
            }
        }
        else
        {
            foreach (Transform zone in zones)
            {
            if (zone.childCount == 0 && !duelFieldUI.IsZoneBlocked(zone)) return zone;
            }
        }
        return null;
    }

    // --- SISTEMA DE RITUAL ---

    public void BeginRitualSummon(CardDisplay sourceCard)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        var handData = isPlayer ? GetPlayerHandData() : GetOpponentHandData();
        
        List<CardData> possibleRituals = RitualManager.Instance.GetPossibleRitualMonsters(sourceCard.CurrentCardData, handData);

        if (possibleRituals.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Ritual válido na mão para esta magia.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        // Bypass para a IA não travar a tela esperando clique
        if (!isPlayer || isSimulating)
        {
            CardData chosenRitual = possibleRituals[0];
            List<CardData> tributes = new List<CardData>();
            int sum = 0;
            foreach (var c in handData) {
                if (c != chosenRitual && c.level > 0) {
                    tributes.Add(c);
                    sum += c.level;
                    if (sum >= chosenRitual.level) break;
                }
            }
            if (sum >= chosenRitual.level) {
                PerformRitualSummon(sourceCard, chosenRitual, tributes, false);
            } else {
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
            }
            return;
        }

        // Se a opção estiver ativa, desvia o fluxo para os painéis customizados!
        if (useCustomRitualUI)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowRitualUI(sourceCard);
            return;
        }

        List<CardData> possibleTributes = new List<CardData>();
        possibleTributes.AddRange(handData.Where(c => c.type.Contains("Monster")));
        Transform[] playerZones = duelFieldUI != null ? duelFieldUI.playerMonsterZones : new Transform[0];
        foreach (var zone in playerZones) {
            if (zone.childCount > 0) {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && !cd.isFlipped) possibleTributes.Add(cd.CurrentCardData);
            }
        }

        // Passo 1: Seleção Tátil no Tabuleiro e Mão
        System.Func<List<CardData>, bool> ritualValidator = (selectedTributes) => {
            foreach (var r in possibleRituals) {
                if (RitualManager.Instance.ValidateRitual(sourceCard.CurrentCardData, r, selectedTributes))
                    return true;
            }
            return false;
        };

        StartDirectSelection(possibleTributes, 1, 5, ritualValidator, "Selecione os Tributos (Botão Direito cancela)", (selectedTributes) => {
            if (selectedTributes == null || selectedTributes.Count == 0) {
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
                return;
            }

            // Verifica quais Monstros de Ritual da mão podem ser feitos com EXATAMENTE esses tributos
            var validRituals = possibleRituals.Where(r => RitualManager.Instance.ValidateRitual(sourceCard.CurrentCardData, r, selectedTributes)).ToList();

            if (validRituals.Count == 0) {
                UIManager.Instance.ShowMessage("Tributos insuficientes ou inválidos!");
                SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
                Destroy(sourceCard.gameObject);
                return;
            }

            // Passo 3: Executa direto (a posição será pedida durante a cinemática)
            System.Action<CardData> onTargetSelected = (targetRitual) => {
                PerformRitualSummon(sourceCard, targetRitual, selectedTributes, false);
            };

            // Passo 2: Dedução automática ou Desempate
            if (validRituals.Count == 1) onTargetSelected(validRituals[0]);
            else UIManager.Instance.ShowCardSelection(validRituals, "Selecione o Monstro de Ritual", 1, 1, (rList) => {
                if (rList != null && rList.Count > 0) onTargetSelected(rList[0]);
                else { SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule); Destroy(sourceCard.gameObject); }
            });
        });
    }

   public void PerformRitualSummon(CardDisplay sourceCard, CardData ritualMonster, List<CardData> tributes, bool isDefense)
    {
        if (ritualMonster == null || tributes == null || tributes.Count == 0)
        {
            Debug.LogError("PerformRitualSummon: Dados inválidos.");
            return;
        }

        Debug.Log($"Realizando Invocação-Ritual de {ritualMonster.name}...");

        if (disableRitualCost) Debug.Log("[Cheat] Ritual Cost Disabled: Tributos e Magia preservados.");

        // 1. Envia a Magia de Ritual para o cemitério
        if (!disableRitualCost)
        {
            SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Effect);
            Destroy(sourceCard.gameObject);
        }

        // 2. Envia os tributos para o cemitério (da mão ou do campo)
        foreach (var tribute in tributes)
        {
            if (disableRitualCost) break;

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
       
       System.Action<bool> doSummon = (def) => {
           // Usa FinalizeSummon para ativar a Cinemática de Ritual Oficialmente
           GameObject cardGO = Instantiate(cardPrefab);
           CardDisplay display = cardGO.GetComponent<CardDisplay>();
           display.SetCard(ritualMonster, cardBackTexture, true);
           display.isPlayerCard = sourceCard.isPlayerCard;

           FinalizeSummon(cardGO, ritualMonster, def, sourceCard.isPlayerCard, false, false, null, tributes);
       };

       if (sourceCard.isPlayerCard && UIManager.Instance != null && !isSimulating)
       {
           UIManager.Instance.ShowPositionSelection(ritualMonster, (pos) => {
               doSummon(pos == CardDisplay.BattlePosition.Defense);
           });
       }
       else
       {
           doSummon(isDefense);
       }

        // TROFÉU: Ritual
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("ritual_summon", 1);
    }

    // --- LÓGICA DE SPELL / TRAP ---

    public bool PlaySpellTrap(GameObject cardGO, CardData cardData, bool isSet)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        bool isPlayer = display != null ? display.isPlayerCard : true;
        string cardName = cardData?.name ?? "Unknown";

        // 0.5 Validação de Armadilha
        if (!devMode && isPlayer && cardData.type.Contains("Trap") && !isSet)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Traps devem ser setadas antes, não ativadas direto.");
            return false;
        }

        // 0. Validação de Permissão
        if (isPlayer && !canPlacePlayerCards)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: canPlacePlayerCards desativado.");
            return false;
        }
        // Bloqueia se for uma ação para o oponente, durante o turno do jogador, e o modo dev de controle do oponente estiver desligado.
        // FIX: Se estiver simulando (!isSimulating), ignora essa trava.
        if (!isPlayer && isPlayerTurn && !canPlaceOpponentCards && !isSimulating)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: canPlaceOpponentCards desativado durante turno do jogador.");
            return false;
        }

        // 1. Validação de Fase
        GamePhase currentPhase = PhaseManager.Instance != null ? PhaseManager.Instance.currentPhase : GamePhase.Main1;
        if (!devMode && isPlayer && currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Spells só permitidos em Main Phase 1/2. Fase atual: {currentPhase}");
            return false;
        }

        // Validação de Condição de Ativação LUA antes de mover a carta para o campo
        if (!isSet && CardEffectManager.Instance != null)
        {
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(display);
            if (lc != null)
            {
                LuaEffect activationEffect = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);
                if (activationEffect != null)
                {
                    int tp = isPlayer ? 0 : 1;
                    if (!CardEffectManager.Instance.CanActivateEffect(lc, activationEffect, tp, null))
                    {
                        Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Falhou na validação Lua de ativação.");
                        if (!isSimulating && UIManager.Instance != null) UIManager.Instance.ShowMessage("Não é possível ativar esta carta agora.");
                        return false; // Cancela a jogada antes de mover a carta!
                    }
                }
            }
        }

        Transform targetZone = null;

        string propStr = cardData.property != null ? cardData.property.ToLower() : "";
        string typeStr = cardData.type != null ? cardData.type.ToLower() : "";

        // 2. Verifica se é Field Spell
        if (propStr.Contains("field") || typeStr.Contains("field"))
        {
            if (duelFieldUI != null)
                targetZone = isPlayer ? duelFieldUI.playerFieldSpell : duelFieldUI.opponentFieldSpell;

            // Regra Clássica: Destrói qualquer Field Spell ativo no campo antes de colocar o novo
            if (duelFieldUI != null)
            {
                if (duelFieldUI.playerFieldSpell.childCount > 0)
                {
                    var oldField = duelFieldUI.playerFieldSpell.GetChild(0).GetComponent<CardDisplay>();
                    if (oldField != null) {
                        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(oldField);
                        SendToGraveyard(oldField.CurrentCardData, true, CardLocation.Field, SendReason.Rule);
                        Destroy(oldField.gameObject);
                    }
                }
                if (duelFieldUI.opponentFieldSpell.childCount > 0)
                {
                    var oldField = duelFieldUI.opponentFieldSpell.GetChild(0).GetComponent<CardDisplay>();
                    if (oldField != null) {
                        if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(oldField);
                        SendToGraveyard(oldField.CurrentCardData, false, CardLocation.Field, SendReason.Rule);
                        Destroy(oldField.gameObject);
                    }
                }
            }
        }
        else
        {
            // Encontrar Zona de Spell Livre
            targetZone = GetFreeSpellZone(isPlayer);
        }

        if (targetZone == null)
        {
            Debug.LogWarning($"[PlaySpellTrap BLOCKED] {cardName}: Sem zonas de magia/armadilha livres!");
            return false;
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

            if (isSet)
            {
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowBack();
                // Spells/Traps setadas ficam verticais (não rotacionam como monstros em defesa)
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);
                
                if (DuelFXManager.Instance != null)
                {
                    DuelFXManager.Instance.PlayFlipEffect(display); // Toca "poeirinha" ao baixar
                    DuelFXManager.Instance.PlayPlacementAura(display); // Toca a Aura visual por trás da magia setada
                }            
            }
            else
            {
                float zRotation = isPlayer ? 0f : 180f;
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, zRotation);

                // Efeito de Ativação
                bool isTrap = cardData.type.Contains("Trap");

                if (DuelScoreManager.Instance != null)
                {
                    if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
                    else DuelScoreManager.Instance.RecordSpellActivation();
                }
                
                // TROFÉU: Ativação
                if (TrophyManager.Instance != null)
                {
                    if (isTrap) TrophyManager.Instance.TrackStat("trap_activated", 1);
                    else TrophyManager.Instance.TrackStat("spell_activated", 1);
                }

                System.Action onActivationComplete = () => {
                    // Integração com Interfaces Customizadas e LUA
                    if (cardData.property == "Ritual")
                    {
                        BeginRitualSummon(display);
                    }
                    else if (cardData.name == "Polymerization" || cardData.name.Contains("Fusion"))
                    {
                        BeginFusionSummon(display);
                    }
                    else if (CardEffectManager.Instance != null)
                    {
                        CardEffectManager.Instance.ActivateCard(display, null, null);
                    }
                };

                if (DuelFXManager.Instance != null)
                    DuelFXManager.Instance.PlayCardActivation(display, isTrap, onActivationComplete);
                else
                    onActivationComplete();
            }
        }
        
        Debug.Log($"[PlaySpellTrap SUCCESS] {cardName} foi {(isSet ? "setada" : "ativada")} com sucesso. {(isPlayer ? "Jogador" : "Oponente")}");
        return true;
    }

    public Transform GetFreeSpellZone(bool isPlayer)
    {
        if (duelFieldUI == null) return null;
        Transform[] zones = isPlayer ? duelFieldUI.playerSpellZones : duelFieldUI.opponentSpellZones;
        if (zones == null) return null;

        // Se for oponente e a opção estiver marcada, inverte a ordem de busca
        if (!isPlayer && fillOpponentZonesFromRight)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (zones[i].childCount == 0) return zones[i];
            }
        }
        else
        {
            foreach (Transform zone in zones)
            {
                if (zone.childCount == 0) return zone;
            }
        }
        return null;
    }

    public void ActivateFieldSpellTrap(GameObject cardGO)
    {
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display == null || !display.isOnField) return;

        CardData cardData = display.CurrentCardData;
        bool isPlayer = display.isPlayerCard;

        // Validação LUA antes de revelar a carta
        if (CardEffectManager.Instance != null)
        {
            LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(display);
            if (lc != null)
            {
                LuaEffect activationEffect = lc.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0040 || e.type == 0x0080);
                if (activationEffect != null)
                {
                    int tp = isPlayer ? 0 : 1;
                    if (!CardEffectManager.Instance.CanActivateEffect(lc, activationEffect, tp, null))
                    {
                        if (!isSimulating) {
                            Debug.LogWarning($"[GameManager] {cardData.name} não cumpre os requisitos para ser ativada.");
                            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Não é possível ativar esta carta agora (condições não atendidas ou recém-baixada).");
                        }
                        return;
                    }
                }
            }
        }

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
        
        // TROFÉU: Ativação (Field Spell)
        if (TrophyManager.Instance != null)
        {
            if (isTrap) TrophyManager.Instance.TrackStat("trap_activated", 1);
            else TrophyManager.Instance.TrackStat("spell_activated", 1);
        }

        System.Action onActivationComplete = () => {
            // Integração com Interfaces Customizadas e LUA
            if (cardData.property == "Ritual")
            {
                BeginRitualSummon(display);
            }
            else if (cardData.name == "Polymerization" || cardData.name.Contains("Fusion"))
            {
                BeginFusionSummon(display);
            }
            else if (CardEffectManager.Instance != null)
            {
                CardEffectManager.Instance.ActivateCard(display, null, null);
            }
        };

        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayCardActivation(display, isTrap, onActivationComplete);
        else
            onActivationComplete();
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
        
        if (cardId == "2015" || cardId == "0013" || cardId == "Umi") {
            if (IsCardActiveOnField("1142")) return true; // Maiden of the Aqua
            if (IsCardActiveOnField("0013")) return true; // A Legendary Ocean
            cardId = "2015"; // Força a busca pelo ID real do Umi Clássico abaixo
        }

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
    public void SpawnToken(bool forPlayer, int atk, int def, string name, int level = 1, string race = "Token", string attribute = "Earth")
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
        tokenData.race = race;
        tokenData.attribute = attribute;
        tokenData.atk = atk;
        tokenData.def = def;
        tokenData.level = level;
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

        // Ajusta rotação visual baseada na posição de batalha e novo dono
        float targetZRot = 0;
        if (card.position == CardDisplay.BattlePosition.Defense) targetZRot = newOwnerIsPlayer ? 90f : -90f;
        else targetZRot = newOwnerIsPlayer ? 0f : 180f;

        if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations && !isSimulating)
        {
            DuelFXManager.Instance.PlayControlSwap(card, newZone, targetZRot, newOwnerIsPlayer, () => {
                card.isPlayerCard = newOwnerIsPlayer;
                if (CardEffectManager.Instance != null && (card.CurrentCardData.id == "0834" || card.CurrentCardData.id == "0050"))
                    CardEffectManager.Instance.ExecuteCardEffect(card);
            });
        }
        else
        {
            card.transform.SetParent(newZone);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.Euler(0, 0, targetZRot);
            card.isPlayerCard = newOwnerIsPlayer;

            if (CardEffectManager.Instance != null && (card.CurrentCardData.id == "0834" || card.CurrentCardData.id == "0050"))
                CardEffectManager.Instance.ExecuteCardEffect(card);
        }
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
        if (isSimulating)
        {
            int count = Mathf.Max(min, 1);
            onSelected?.Invoke(sourceList.Take(count).ToList());
            return;
        }
        // Verifica se TODAS as cartas da seleção estão fisicamente presentes na Mesa ou na Mão
        bool isHandOrFieldSubset = sourceList.Count > 0 && sourceList.All(c => {
            if (playerHand.Exists(go => go.GetComponent<CardDisplay>().CurrentCardData == c)) return true;
            if (opponentHand.Exists(go => go.GetComponent<CardDisplay>().CurrentCardData == c)) return true;
            if (FindCardOnField(c.id, true) != null) return true;
            if (FindCardOnField(c.id, false) != null) return true;
            return false;
        });

        bool isFusionOrRitual = title.Contains("Fusão") || title.Contains("Ritual") || title.Contains("Tributo");

        if (useDirectHandSelection && isHandOrFieldSubset && min == max && !isFusionOrRitual)
        {
            StartDirectSelection(sourceList, min, max, null, title, onSelected);
            return;
        }

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

    // --- LÓGICA DE SELEÇÃO DIRETA DA MÃO ---

    private void StartDirectSelection(List<CardData> candidates, int min, int max, System.Func<List<CardData>, bool> validator, string title, System.Action<List<CardData>> callback)
    {
        isSelectingFromHand = true;
        handSelectionCandidates = candidates;
        handSelectionCountRequired = max;
        customSelectionValidator = validator;
        handSelectionCallback = callback;
        currentHandSelection = new List<CardData>();

        if (UIManager.Instance != null) UIManager.Instance.ShowMessage(title);

        // Destaca as cartas válidas na mão E no campo
        List<GameObject> allCards = new List<GameObject>(playerHand);
        if (duelFieldUI != null) {
            foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
        }

        foreach (var go in allCards)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null && handSelectionCandidates.Contains(cd.CurrentCardData))
            {
                cd.SetTributeHighlight(true); // Usa o destaque azul para indicar candidatos
            }
        }
        Debug.Log($"[GameManager] Iniciando seleção tátil: {title}");
    }

    public void HandleHandCardClick(CardDisplay card)
    {
        if (!isSelectingFromHand) return;
        if (!handSelectionCandidates.Contains(card.CurrentCardData)) return;

        if (currentHandSelection.Contains(card.CurrentCardData))
        {
            // Deselecionar
            currentHandSelection.Remove(card.CurrentCardData);
            card.SetAttackSelectionVisual(false); // Remove destaque de seleção (Vermelho)
            card.SetTributeHighlight(true); // Volta para destaque de candidato (Azul)
        }
        else
        {
            // Selecionar
            if (currentHandSelection.Count < handSelectionCountRequired)
            {
                currentHandSelection.Add(card.CurrentCardData);
                card.SetTributeHighlight(false); // Remove destaque de candidato
                card.SetAttackSelectionVisual(true); // Adiciona destaque de seleção (Vermelho)
            }
        }

        // Verifica se completou a seleção
        bool isValid = false;
        if (customSelectionValidator != null) {
            isValid = customSelectionValidator(currentHandSelection);
        } else {
            isValid = currentHandSelection.Count == handSelectionCountRequired;
        }

        if (isValid)
        {
            if (confirmHandSelection)
            {
                string msg = customSelectionValidator != null ? "Confirmar os materiais selecionados?" : (handSelectionCountRequired == 1 ? $"Selecionar {currentHandSelection[0].name}?" : "Confirmar seleção?");
                UIManager.Instance.ShowConfirmation(msg, () => FinishHandSelection(false), () => {
                    // Se cancelar, remove a última seleção para permitir trocar
                    if (currentHandSelection.Count > 0)
                    {
                        var last = currentHandSelection[currentHandSelection.Count - 1];
                        currentHandSelection.RemoveAt(currentHandSelection.Count - 1);
                        
                        // Atualiza visual da carta cancelada
                        List<GameObject> allCards = new List<GameObject>(playerHand);
                        if (duelFieldUI != null) { 
                            foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject); }
                            foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
                            foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
                            foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);

                        var go = allCards.Find(g => g.GetComponent<CardDisplay>().CurrentCardData == last);
                        if (go != null)
                        {
                            var cd = go.GetComponent<CardDisplay>();
                            cd.SetAttackSelectionVisual(false);
                            cd.SetTributeHighlight(true);
                        }
                    }
                });
            }
            else
            {
                FinishHandSelection(false);
            }
        }
    }

    private void FinishHandSelection(bool isCancel)
    {
        // Limpa visuais (Mão e Campo)
        List<GameObject> allCards = new List<GameObject>(playerHand);
        if (duelFieldUI != null) {
            foreach(var z in duelFieldUI.playerMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.playerSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.opponentMonsterZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
            foreach(var z in duelFieldUI.opponentSpellZones) if(z.childCount>0) allCards.Add(z.GetChild(0).gameObject);
        }

        foreach (var go in allCards)
        {
            var cd = go.GetComponent<CardDisplay>();
            if (cd != null)
            {
                cd.SetTributeHighlight(false);
                cd.SetAttackSelectionVisual(false);
            }
        }

        isSelectingFromHand = false;
        handSelectionCallback?.Invoke(isCancel ? new List<CardData>() : currentHandSelection);
    }

    // Invocação Especial direta por dados (para Monster Reborn, etc)

    private CardDisplay DefaultSpecialSummon(CardData data, bool forPlayer, bool faceUp = true, bool defense = false)
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

        bool isFusion = data.type.Contains("Fusion");
        bool isRitual = data.type.Contains("Ritual");
        bool useCinematic = false;
        
        if (isFusion) useCinematic = enableFusionCinematic && !isSimulating;
        else if (isRitual) useCinematic = enableRitualCinematic && !isSimulating;
        else useCinematic = enableSummonCinematics && !isSimulating;

        if (useCinematic && DuelFXManager.Instance != null)
        {
            display.SetVisibility(false);
            
            System.Action onCinematicComplete = () => {
                display.SetVisibility(true);
                DuelFXManager.Instance.PlaySummonAura(display);
                if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSpecialSummon(display);
            };

            if (isFusion)
            {
                DuelFXManager.Instance.PlayFusionCinematic(display, null, null, onCinematicComplete);
            }
            else if (isRitual)
            {
                DuelFXManager.Instance.PlayRitualCinematic(display, null, onCinematicComplete);
            }
            else
            {
                DuelFXManager.Instance.PlaySummonCinematic(display, false, true, onCinematicComplete);
            }
        }
        else
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySummonEffect(display);
            if (CardEffectManager.Instance != null) CardEffectManager.Instance.OnSpecialSummon(display);
        }
        
        // TROFÉU: Special Summon
        if (TrophyManager.Instance != null)
            TrophyManager.Instance.TrackStat("special_summon", 1);

        return display;
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

        if (type == CardLink.LinkType.Equipment)
        {
            // Toca a animação do Fantasma indo em direção ao alvo
            if (DuelFXManager.Instance != null)
                DuelFXManager.Instance.PlayEquipEffect(source, target);
                
            if (CardEffectManager.Instance != null)
                CardEffectManager.Instance.OnCardEquipped(source, target);
        }
    }

    public void BeginFusionSummon(CardDisplay sourceCard)
    {
        bool isPlayer = sourceCard.isPlayerCard;
        var extraDeck = isPlayer ? playerExtraDeck : opponentExtraDeck;
        var fusions = extraDeck.Where(c => c.type.Contains("Fusion")).ToList();

        if (fusions.Count == 0)
        {
            Debug.Log("Nenhum Monstro de Fusão no Extra Deck.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        List<CardData> possibleMaterials = new List<CardData>();
        possibleMaterials.AddRange(GetPlayerHandData().Where(c => c.type.Contains("Monster")));
        
        Transform[] playerZones = duelFieldUI != null ? duelFieldUI.playerMonsterZones : new Transform[0];
        foreach (var zone in playerZones)
        {
            if (zone != null && zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && !cd.isFlipped) possibleMaterials.Add(cd.CurrentCardData);
            }
        }

        List<CardData> possibleFusions = fusions.Where(f => FusionManager.Instance.CanBeFusionSummoned(f, possibleMaterials)).ToList();

        if (possibleFusions.Count == 0)
        {
            Debug.Log("Materiais insuficientes para qualquer Fusão.");
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule); Destroy(sourceCard.gameObject);
            return;
        }

        if (!isPlayer || isSimulating)
        {
            // A IA de Fusão pura será tratada pela LUA. Por ora, abortamos para não travar a UI de humano.
            SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
            return;
        }

        // Se a opção estiver ativa, desvia o fluxo para os painéis customizados!
        if (useCustomFusionUI)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowFusionUI(sourceCard);
            return;
        }

       // Passo 1: Seleção Tátil de Fusão
       System.Func<List<CardData>, bool> fusionValidator = (selectedMaterials) => {
           foreach (var f in possibleFusions) {
               if (FusionManager.Instance.ValidateFusion(f, selectedMaterials)) return true;
           }
           return false;
       };

       StartDirectSelection(possibleMaterials, 2, 5, fusionValidator, "Selecione os Materiais de Fusão (Botão Direito cancela)", (selectedMaterials) => {
           if (selectedMaterials == null || selectedMaterials.Count < 2) {
               SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
               Destroy(sourceCard.gameObject);
               return;
           }

           // Verifica quais Fusões do Extra Deck podem ser feitas com EXATAMENTE os materiais selecionados
           var validFusions = possibleFusions.Where(f => FusionManager.Instance.ValidateFusion(f, selectedMaterials)).ToList();

           if (validFusions.Count == 0) {
               UIManager.Instance.ShowMessage("Materiais incompatíveis com o Extra Deck!");
               SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule);
               Destroy(sourceCard.gameObject);
               return;
           }

           // Passo 3: Executa direto (a posição será pedida durante a cinemática)
           System.Action<CardData> onTargetSelected = (targetFusion) => {
               FusionManager.Instance.PerformFusionSummon(sourceCard, targetFusion, selectedMaterials, false);
           };

           // Passo 2: Dedução automática ou Desempate
           if (validFusions.Count == 1) onTargetSelected(validFusions[0]);
           else UIManager.Instance.ShowCardSelection(validFusions, "Selecione a Fusão do Extra Deck", 1, 1, (fList) => {
               if (fList != null && fList.Count > 0) onTargetSelected(fList[0]);
               else { SendToGraveyard(sourceCard.CurrentCardData, isPlayer, CardLocation.Field, SendReason.Rule); Destroy(sourceCard.gameObject); }
           });
       });
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