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

public enum AttackIndicatorMode { HoverOnly, AlwaysInBattlePhase }

[System.Serializable]
public class PhaseAnnouncementSettings
{
    public bool enableAnnouncements = true;
    public float displayDuration = 1.2f;
    public float fadeDuration = 0.3f;
    public float slideDistance = 50f;
    public Vector2 offset = Vector2.zero;
    public float fontSize = 80f;
    
    [Header("Colors")]
    public Color playerTextColor = new Color(0.2f, 0.8f, 1f, 1f); // Ciano para Jogador
    public Color opponentTextColor = new Color(1f, 0.3f, 0.3f, 1f); // Vermelho para Oponente
    public Color neutralTextColor = new Color(1f, 0.8f, 0f, 1f); // Dourado (Start Duel)
    
    public bool useOutline = true;
    public Color outlineColor = Color.black;
    public Vector2 outlineThickness = new Vector2(4, -4);
    
    [Header("Timings & Delays")]
    [Tooltip("Tempo em segundos que o jogo aguarda após a Draw Phase para entrar na Standby (Ex: 1.5).")]
    public float drawToStandbyDelay = 1.5f;
    
    [Tooltip("Multiplicador global para a velocidade de anúncios e pausas. (1.0 = Normal, 0.5 = 2x Rápido, 0.0 = Instantâneo).")]
    [Range(0f, 3f)]
    public float masterDelayMultiplier = 1.0f;

    [Header("Text Customization")]
    public string textStartDuel = "DUEL START!";
    public string textPlayerTurn = "YOUR TURN";
    public string textOpponentTurn = "OPPONENT'S TURN";
    public string textDrawPhase = "DRAW PHASE";
    public string textStandbyPhase = "STANDBY PHASE";
    public string textMainPhase1 = "MAIN PHASE 1";
    public string textBattlePhase = "BATTLE PHASE";
    public string textMainPhase2 = "MAIN PHASE 2";
    public string textEndPhase = "END PHASE";
}

public partial class GameManager : MonoBehaviour
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

    [Header("Attack Indicators")]
    public AttackIndicatorMode attackIndicatorMode = AttackIndicatorMode.HoverOnly;

    [Header("Phase & Turn Announcements")]
    public PhaseAnnouncementSettings phaseAnnouncements = new PhaseAnnouncementSettings();
 
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
    [Tooltip("Se marcado, exibe um aviso caso você tente atacar diretamente enquanto o inimigo possui monstros no campo.")]
    public bool showInvalidDirectAttackWarning = true;
    [Tooltip("Se ativado, usa atalhos de Esquerdo/Direito do mouse com Tooltip. Se desativado, usa o Menu de Ação clássico.")]
    public bool useMouseTooltipUI = true;
    [Tooltip("Permite clicar em uma carta no campo para ativar seu efeito imediatamente (sem abrir o Action Menu).")]
    public bool activateEffectsWithOneClick = true;
    [Tooltip("Habilita o painel visual pop-up de Dano/Cura saltando no campo.")]
    public bool enableDamagePopups = true;
    [Tooltip("Se marcado, usa o painel customizado Panel_Fusion. Se desmarcado, usa a seleção tática no tabuleiro.")]
    public bool useCustomFusionUI = true;
    [Tooltip("Se marcado, usa o painel customizado Panel_Ritual. Se desmarcado, usa a seleção tática no tabuleiro.")]
    public bool useCustomRitualUI = true;
    [Tooltip("Se marcado, anima a carta voando do cemitério para o topo do deck.")]
    public bool enableReturnToDeckAnimation = true;
    [Tooltip("Se marcado, sempre pedirá confirmação para seleção de alvo, mesmo que haja apenas um alvo possível.")]
    public bool alwaysConfirmSingleTarget = false;

    [Tooltip("Se marcado, exibe balões de texto (Tooltip) sobre o Avatar dos jogadores quando efeitos adicionam dicas.")]
    public bool enableProfileTooltip = false;
    [Tooltip("Se marcado, exibe números flutuantes e piscantes na carta indicando a contagem de turnos.")]
    public bool enableFloatingTurnCounters = false;

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

    [Header("Current Duel Info (Auto-Assigned - Não Edite)")]
    public CharacterData currentOpponent; // Oponente atual carregado
    public int currentDuelIndex = -1; // Índice do duelo atual na campanha (para salvar progresso)

    // --- Memória de Dicas de Perfil (LUA PHINT) ---
    [HideInInspector] public string playerHintDesc = "";
    [HideInInspector] public string opponentHintDesc = "";

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
    [HideInInspector] public int specialSummonsThisTurnPlayer = 0;
    [HideInInspector] public int specialSummonsThisTurnOpponent = 0;
    [HideInInspector] public int flipSummonsThisTurnPlayer = 0;
    [HideInInspector] public int flipSummonsThisTurnOpponent = 0;
    [HideInInspector] public int attacksThisTurnPlayer = 0;
    [HideInInspector] public int attacksThisTurnOpponent = 0;

    [HideInInspector] public bool allowAttacks => turnCount > 1;

    [HideInInspector] public int pendingEffectDraws = 0;

    [HideInInspector] public int pendingVisualTasks = 0;

    // --- ESTADO DE SELEÇÃO DE MÃO ---
    [HideInInspector] public bool justCanceledSomething = false;
    [HideInInspector] public bool isSelectingFromHand = false;
    private List<CardData> handSelectionCandidates;
    private List<CardDisplay> handSelectionDisplayCandidates;
    private System.Action<List<CardDisplay>> displaySelectionCallback;

    private List<GameObject> currentHandSelectionObjects;
    private int handSelectionCountRequired;
    private System.Action<List<CardData>> handSelectionCallback;
    private System.Func<List<CardData>, bool> customSelectionValidator;
    private HighlightCategory currentSelectionHighlightCategory = HighlightCategory.GenericTarget;

    // --- ESTADO DE SELEÇÃO DE RESPOSTA ---
    [HideInInspector] public bool isSelectingResponse = false;
    private List<CardDisplay> responseCandidates;
    private System.Action<CardDisplay> responseCallback;
    private System.Action responseCancelCallback;
    private bool cardViewerShowingBack = false;

    private GamePhase _lastTrackedPhase = GamePhase.Draw;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Atalho de Desenvolvedor para Fullscreen (F11)
        bool toggleFullscreen = false;
        bool rightClick = false;
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame) toggleFullscreen = true;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) rightClick = true;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.F11)) toggleFullscreen = true;
        if (Input.GetMouseButtonDown(1)) rightClick = true;
        if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;
#endif

        if (devMode && toggleFullscreen)
        {
            ToggleFullscreen();
        }

        // Preserva a flag até o clique ser finalizado (Mouse Up) no DuelFieldUI
        if (rightClick || escPressed)
        {
            justCanceledSomething = false;
        }

        if (isSelectingFromHand && (rightClick || escPressed))
        {
            if (currentHandSelectionObjects != null && currentHandSelectionObjects.Count >= handSelectionMinRequired && currentHandSelectionObjects.Count < handSelectionCountRequired)
            {
                // O jogador atingiu o mínimo necessário e escolheu parar. Confirma a seleção parcial!
                FinishHandSelection(false);
            }
            else
            {
                FinishHandSelection(true); // Cancela a seleção
            }
            justCanceledSomething = true;
        }
        else if (isSelectingResponse && (rightClick || escPressed))
        {
            CancelResponseSelection(); // Cancela a resposta
            justCanceledSomething = true;
        }
        else if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null && (rightClick || escPressed))
        {
            CancelAttackTargeting();
            justCanceledSomething = true;
        }
        
        // Trava de Segurança Final: Desfaz instantaneamente qualquer tentativa de ataque no Turno 1
        // Isso resolve a "travada" caso a carta inicie o fluxo de ataque internamente.
        if (turnCount == 1 && CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
        {
            CancelAttackTargeting();
        }
        
        // Rastreador de Fases para Recálculo de Status Dinâmicos (Ex: Banner of Courage na Battle Phase)
        if (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != _lastTrackedPhase)
        {
            _lastTrackedPhase = PhaseManager.Instance.currentPhase;
            RefreshAllCardsVisuals();
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
            // Debug.Log("[GameManager] Interação com a mão do jogador ATIVADA.");

            // Verificação Retroativa de Hover (Melhora de UX)
            // Se o mouse já estiver sobre uma carta quando a mão se torna interativa, levanta-a.
            Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
            mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            mousePos = Input.mousePosition;
#endif
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    CardDisplay cardDisplay = result.gameObject.GetComponent<CardDisplay>();
                    if (cardDisplay != null && cardDisplay.isInteractable)
                    {
                        // Encontrou uma carta sob o mouse, força o estado de hover.
                        cardDisplay.ForceHover();
                        break; // Para após encontrar a primeira carta.
                    }
                }
            }
        }
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
}