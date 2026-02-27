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
    [Header("MODOS DE VISUALIZAÇÃO")]
    [Tooltip("Ativa o visualizador 2D (RawImage na tela)")]
    public bool modo2D_Ativado = true;
    [Tooltip("Habilita funcionalidades de desenvolvedor (ex: ver mão do oponente, sacar para oponente)")]
    public bool devMode = false;
    [Tooltip("Permite sacar cartas clicando no Deck")]
    public bool enableDeckClickDraw = true;
    [Tooltip("Define se as cartas do oponente estão visíveis")]
    public bool showOpponentHand = false;

    [Header("Hand Settings")]
    [Tooltip("A escala das cartas na mão do jogador.")]
    public Vector3 handCardScale = new Vector3(1f, 1f, 1f);
    [Tooltip("A altura que a carta do JOGADOR sobe ao passar o mouse sobre ela.")]
    public float handCardHoverYOffset = 30f;
    [Tooltip("A altura que a carta do OPONENTE desce ao passar o mouse sobre ela (use um valor negativo).")]
    public float opponentHandCardHoverYOffset = -30f;
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
    public Sprite cardMaskSprite; // Sprite para arredondar cantos (opcional)

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
    private CardDisplay currentCardDisplay; // Referência ao CardDisplay na cardDisplayArea

    [Header("Current Duel Info")]
    public CharacterData currentOpponent; // Oponente atual carregado
    public int currentDuelIndex = -1; // Índice do duelo atual na campanha (para salvar progresso)

    [Header("Player Profile")]
    public string playerName = "Duelist";
    public string currentSaveID = "default";

    private bool hasDrawnThisTurn = false; // Controle de draw por turno

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
        
        // Reseta flag de draw antes de começar o turno
        hasDrawnThisTurn = false;
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
        playerSideDeck.Clear();
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
        // TODO: Carregar do save real. Por enquanto carrega do banco.
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

    public void DrawCard(bool ignoreLimit = false)
    {
        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("Deck vazio! Não é possível comprar mais cartas.");
            return;
        }

        // Lógica de Limitação de Draw (1 por turno na Draw Phase)
        if (!devMode && !ignoreLimit)
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

            newCardDisplay.isPlayerCard = true; // Marca como carta do jogador (visível no viewer mesmo setada)
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
            newCardDisplay.hoverYOffset = opponentHandCardHoverYOffset; // Usa o offset do oponente
            newCardDisplay.isInteractable = true; // Habilita o efeito de hover

            newCardDisplay.isPlayerCard = false; // Carta do oponente
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

        // Atualiza o destaque visual no tabuleiro
        if (duelFieldUI != null) duelFieldUI.UpdatePhaseHighlight(currentPhase);

        // Lógica específica de entrada na fase
        switch (currentPhase)
        {
            case GamePhase.Draw:
                // Reseta o draw do turno ao entrar na Draw Phase
                hasDrawnThisTurn = false;
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
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                cardBackTexture = DownloadHandlerTexture.GetContent(request);
                cardBackTexture.filterMode = FilterMode.Trilinear;
                UpdatePileVisuals(); // Atualiza o visual dos decks caso o duelo já tenha começado
            }
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
    public void EndDuel(bool playerWon, bool isDeckOut = false)
    {
        if (DuelScoreManager.Instance != null)
        {
            // Pega o LP atual do jogador (você precisará implementar a variável playerLP no GameManager ou onde estiver)
            int currentLP = 8000; // Placeholder, substitua pela variável real de LP

            DuelScoreManager.Instance.StopDuelTracking(playerWon, isDeckOut, currentLP);
            
            int score;
            DuelRank rank = DuelScoreManager.Instance.CalculateFinalRank(out score);
            
            Debug.Log($"DUELO FINALIZADO! Venceu: {playerWon} | Rank: {rank} | Pontos: {score}");
            Debug.Log(DuelScoreManager.Instance.GetScoreReport());
            
            // TODO: Chamar UIManager para mostrar tela de Resultado (Vitória/Derrota) com o Rank
        }
    }

    // --- LÓGICA DE INVOCACÃO ---

    public void SummonMonster(GameObject cardGO, CardData cardData, bool isSet)
    {
        // 1. Validação de Fase
        if (currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning("Invocação só é permitida na Main Phase 1 ou 2.");
            return;
        }

        // 2. Encontrar Zona Livre
        Transform targetZone = GetFreePlayerMonsterZone();
        if (targetZone == null)
        {
            Debug.LogWarning("Sem zonas de monstro livres!");
            return;
        }

        // 3. Mover Carta (Lógica de Dados e Visual)
        playerHand.Remove(cardGO); // Remove da lista da mão
        
        cardGO.transform.SetParent(targetZone); // Coloca na zona
        cardGO.transform.localPosition = Vector3.zero; // Centraliza
        cardGO.transform.localScale = Vector3.one; // Reseta escala (tira o tamanho da mão)

        // 4. Configuração Visual (Ataque vs Defesa)
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.isInteractable = false; // Desativa o hover de mão (subir)
            
            if (isSet)
            {
                // Modo Defesa (Set): Virado para baixo e Rotacionado 90 graus
                display.ShowBack();
                cardGO.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                // Modo Ataque: Virado para cima e Reto
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.identity;
                
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

    // --- LÓGICA DE SPELL / TRAP ---

    public void PlaySpellTrap(GameObject cardGO, CardData cardData, bool isSet)
    {
        // 1. Validação de Fase
        if (currentPhase != GamePhase.Main1 && currentPhase != GamePhase.Main2)
        {
            Debug.LogWarning("Ativar/Setar Spells só é permitido na Main Phase 1 ou 2.");
            return;
        }

        Transform targetZone = null;

        // 2. Verifica se é Field Spell
        if (cardData.race == "Field")
        {
            if (duelFieldUI != null) targetZone = duelFieldUI.playerFieldSpell;
        }
        else
        {
            // Encontrar Zona de Spell Livre
            targetZone = GetFreePlayerSpellZone();
        }

        if (targetZone == null)
        {
            Debug.LogWarning("Sem zonas de magia/armadilha livres!");
            return;
        }

        // 3. Mover Carta
        playerHand.Remove(cardGO);
        cardGO.transform.SetParent(targetZone);
        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = Vector3.one;

        // 4. Configuração Visual
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.isInteractable = false;

            if (isSet)
            {
                display.ShowBack();
                // Spells/Traps setadas ficam verticais (não rotacionam como monstros em defesa)
                cardGO.transform.localRotation = Quaternion.identity; 
            }
            else
            {
                display.ShowFront();
                cardGO.transform.localRotation = Quaternion.identity;

                // Efeito de Ativação
                bool isTrap = cardData.type.Contains("Trap");
                if (DuelFXManager.Instance != null)
                    DuelFXManager.Instance.PlayCardActivation(display, isTrap);
                
                if (DuelScoreManager.Instance != null)
                {
                    if (isTrap) DuelScoreManager.Instance.RecordTrapActivation();
                    else DuelScoreManager.Instance.RecordSpellActivation();
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

    // --- CONTROLE DE FASE (UI) ---

    public void TryChangePhase(GamePhase newPhase)
    {
        // Regra: Não pode voltar fases (exceto DevMode)
        // Ordem: Draw(0) -> Standby(1) -> Main1(2) -> Battle(3) -> Main2(4) -> End(5)
        if (!devMode)
        {
            if ((int)newPhase <= (int)currentPhase)
            {
                Debug.LogWarning("Não é possível voltar para uma fase anterior neste turno.");
                return;
            }
        }

        ChangePhase(newPhase);
    }

    // --- GERENCIAMENTO DE DECK (ACESSO PÚBLICO) ---
    
    public List<CardData> GetPlayerMainDeck() { return playerDeck; }
    public List<CardData> GetPlayerSideDeck() { return playerSideDeck; }
    public List<CardData> GetPlayerExtraDeck() { return playerExtraDeck; }
    
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
}
