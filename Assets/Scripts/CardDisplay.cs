using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq; // Added for LINQ
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CardDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static CardDisplay HoveredCard;

    public enum BattlePosition { Attack, Defense }

    [Header("UI Elements")]
    public RawImage cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    [Header("Visual Effects")]
    public Image outlineImage; // Arraste uma imagem de borda (filha) aqui
    [Tooltip("Se marcado, usa o componente Outline do Unity para criar uma borda simples (senão, usa a imagem 'outlineImage').")]
    public bool useSimpleOutline = true;
    public bool enableHoverOutline = true;
    public bool useSimpleHover = false; // Se true, usa apenas hoverColor simples
    public Color hoverColor = Color.yellow;
    public Color tributeColor = new Color(0f, 0.6f, 1f); // Azul ciano brilhante
    public Color attackColor = Color.red;
    public bool enableHoverLift = true; // Se true, permite o efeito de levantar a carta no hover

    private CardData currentCardData;
    private Texture2D frontTexture;
    private Texture2D backTexture;
    public bool isFlipped = false; // Tornado público para acesso externo
    private RectTransform rectTransform;

    // Componentes para corrigir a renderização e tremedeira
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Vector3 originalScale = Vector3.one;
    private GameObject tributeIconObj; // Efeito 2D de Tributo

    [HideInInspector] public float hoverYOffset = 30f;
    [HideInInspector] public bool isInteractable = false; // Usado para habilitar hover apenas para cartas na mão
    [HideInInspector] public bool isPlayerCard = false; // Define se a carta pertence ao jogador (para visualização)
    [HideInInspector] public bool isOnField = false; // Define se a carta está no campo
    [HideInInspector] public bool isInPile = false; // Define se a carta está em uma pilha (Deck, GY, Extra)
    [HideInInspector] public BattlePosition position; // Posição de batalha do monstro

    // Status Dinâmicos e Passivos de Batalha
    [HideInInspector] public bool hasPiercing = false;

    // Stats em Tempo Real (Modificados por efeitos)
    [HideInInspector] public int originalAtk;
    [HideInInspector] public int originalDef;
    [HideInInspector] public int currentAtk;
    [HideInInspector] public int currentDef;

    // Sistema de Contadores de Turno (para Swords of Revealing Light, etc)
    [SerializeField, HideInInspector] private int _turnCounter = 0;
    public int turnCounter 
    {
        get { return _turnCounter; }
        set 
        { 
            _turnCounter = value;
            if (_turnCounter > maxTurnCounter) maxTurnCounter = _turnCounter; // Atualiza o máximo histórico
            UpdateTurnClockVisual();
        }
    }
    [HideInInspector] public int maxTurnCounter = 0; // Armazena o valor máximo que este contador já teve (para calcular a fração)

    // FASE 13: Card History Tracking - Warp condition support
    [HideInInspector] public CardLocation previousLocation = CardLocation.Unknown;
    [HideInInspector] public int previousOwner = 0; // 0 = player, 1 = opponent
    [HideInInspector] public CardLocation previousPreviousLocation = CardLocation.Unknown; // For multi-hop warp checks

    // FASE 26: Display data and ownership tracking
    [HideInInspector] public Dictionary<string, object> displayData = new Dictionary<string, object>(); // Dynamic display properties
    [HideInInspector] public bool ownerPlayer = true; // true = player, false = opponent
    private CardLocation _currentLocation = CardLocation.Unknown;
    [HideInInspector] public CardLocation CurrentLocation
    {
        get { return _currentLocation; }
        set { _currentLocation = value; }
    }

    public CardData CurrentCardData => currentCardData; // Propriedade pública para acesso seguro (Renomeado para evitar conflito)

    private UnityWebRequest currentRequest; // Rastreia a requisição ativa para descarte correto
    private bool isAttackSelected = false; // Rastreia se a carta está selecionada para atacar
    [HideInInspector] public bool hasAttackedThisTurn = false; // Rastreia se o monstro já atacou
    
    private List<CardDisplay> linkedCardsToHighlight = new List<CardDisplay>();
    private List<GameObject> activeConnectionLines = new List<GameObject>();

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Se esta for a instância do CardViewer, tenta encontrar os textos automaticamente
        if (GameManager.Instance != null && GameManager.Instance.cardViewerDisplay == this)
        {
            if (transform.parent != null)
            {
                var allTexts = transform.parent.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTexts)
                {
                    if (t.name.Contains("Name")) cardNameText = t;
                    if (t.name.Contains("Info")) cardInfoText = t;
                    if (t.name.Contains("Description")) cardDescriptionText = t;
                    if (t.name.Contains("Stats")) cardStatsText = t;
                }
            }
        }

        // --- FIX: Lógica simplificada e mais robusta para encontrar a imagem da carta ---
        if (cardImage == null)
        {
            cardImage = GetComponentInChildren<RawImage>(true);
            if (cardImage == null)
            {
                 Debug.LogError($"[CardDisplay] Crítico: Nenhum componente 'RawImage' encontrado nos filhos do objeto '{gameObject.name}'. A arte da carta não pode ser exibida.");
            }
        }

        // FIX: Garante que a imagem da carta não bloqueie o mouse (para o Hover no pai funcionar)
        if (cardImage != null && cardImage.gameObject != this.gameObject)
        {
             // Apenas desativa o raycast se a imagem for um objeto filho, para não desativar o clique no pai.
            cardImage.raycastTarget = false;
        }

        // FIX: Configura a Borda para esticar na carta toda e não bloquear cliques
        if (outlineImage != null)
        {
            outlineImage.raycastTarget = false; // Importante: Ignora cliques
            // FIX: Coloca a borda atrás de tudo (primeiro filho) para não cobrir a carta
            outlineImage.transform.SetAsFirstSibling();
            outlineImage.rectTransform.anchorMin = Vector2.zero;
            outlineImage.rectTransform.anchorMax = Vector2.one;
            // O tamanho será controlado pelo layout ou sprite

            outlineImage.gameObject.SetActive(false);
        }

        // FIX: Adiciona Canvas para controlar a ordem de desenho sem quebrar o Layout (evita tremedeira)
        canvas = gameObject.AddComponent<Canvas>();
        graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        // Tenta aplicar máscara de arredondamento se houver sprite configurado no GameManager
        ApplyRoundedCorners();
    }

    void ApplyRoundedCorners()
    {
        if (GameManager.Instance == null || cardImage == null) return;

        bool useRounded = false;
        if (GameManager.Instance.cardViewerDisplay == this)
        {
            useRounded = GameManager.Instance.useCardViewerRounded;
        }
        else if (isOnField || isInPile)
        {
            useRounded = GameManager.Instance.useFieldCardsRounded; // Piles use field settings
        }
        else // Hand
        {
            useRounded = GameManager.Instance.useHandCardsRounded;
        }

        Mask mask = GetComponent<Mask>();
        // Se não tiver máscara e precisarmos arredondar, adiciona. Se não precisar, não adiciona à toa.
        if (mask == null && useRounded) mask = gameObject.AddComponent<Mask>();

        Image parentImage = GetComponent<Image>();
        if (parentImage == null && useRounded) parentImage = gameObject.AddComponent<Image>();

        if (useRounded && GameManager.Instance.cardMaskSprite != null)
        {
            if (parentImage != null)
            {
                parentImage.sprite = GameManager.Instance.cardMaskSprite;
                parentImage.enabled = true;
            }
            if (mask != null)
            {
                // A máscara DEVE ser visível para que o Outline tenha onde se desenhar
                mask.showMaskGraphic = true;
                mask.enabled = true;
            }
        }
        else
        {
            if (mask != null) mask.enabled = false;
            if (parentImage != null) 
            {
                // Mantém habilitado mas invisível para capturar cliques/hover
                parentImage.enabled = true;
                parentImage.color = Color.clear;
            }
        }
    }

    // Este método será chamado pelo GameManager para definir os dados da carta
    public void SetCard(CardData card, Texture2D cardBackTexture, bool startFaceUp = true)
    {
        if (card == null) 
            {
        Debug.LogError("SetCard foi chamado com CardData NULO! A carta não será exibida.", this.gameObject);
        return;
    }

        // Limpa requisição anterior se ainda estiver rodando
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        StopAllCoroutines(); // Para carregamentos anteriores
        // Limpa textura anterior para liberar memória
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }

        currentCardData = card;
        backTexture = cardBackTexture;
        isFlipped = !startFaceUp; // Se startFaceUp for false, isFlipped será true (verso)

        // Inicializa stats
        originalAtk = card.atk;
        originalDef = card.def;
        currentAtk = card.atk;
        currentDef = card.def;

        turnCounter = 0; // Reseta contadores de turno
        maxTurnCounter = 0;
        
        originalScale = transform.localScale; // Salva a escala inicial definida pelo GameManager

        DisplayCardDetails();

        // Se começar virada, já aplica o verso imediatamente
        if (isFlipped && cardImage != null && backTexture != null)
        {
            cardImage.texture = backTexture;
        }
        else if (!isFlipped && cardImage != null && backTexture != null)
        {
            // FIX: Coloca a textura do verso temporariamente para não mostrar um bloco branco
            // enquanto a textura real da carta está sendo baixada na corrotina.
            cardImage.texture = backTexture;
        }

        // FIX: Verifica se o objeto está ativo antes de iniciar a corrotina para evitar erro
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(LoadCardFrontTexture(card.image_filename));
        }
        ApplyRoundedCorners();
        UpdateTurnClockVisual(); // Garante que o relógio suma se resetar
    }

    public void SetCardBackOnly(Texture2D cardBackTexture)
    {
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        StopAllCoroutines();
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }

        backTexture = cardBackTexture;
        if (cardImage != null && backTexture != null)
        {
            cardImage.texture = backTexture;
        }
        // Limpa os textos
        if (cardNameText != null) cardNameText.text = "";
        if (cardInfoText != null) cardInfoText.text = "";
        if (cardDescriptionText != null) cardDescriptionText.text = "";
        if (cardStatsText != null) cardStatsText.text = "";
    }

    private void DisplayCardDetails()
    {
        if (currentCardData == null) return;

        if (cardNameText != null) cardNameText.text = currentCardData.name;
        if (cardDescriptionText != null) cardDescriptionText.text = currentCardData.description;

        string info = $"[{currentCardData.type}]";
        if (!string.IsNullOrEmpty(currentCardData.race)) info += $" / {currentCardData.race}";
        if (currentCardData.level > 0) info += $" / LV: {currentCardData.level}";
        if (_turnCounter > 0) info += $" / <color=yellow>⏳ {_turnCounter}</color>";
        if (cardInfoText != null) cardInfoText.text = info;

        if (cardStatsText != null)
        {
            if (currentCardData.type.Contains("Monster"))
                cardStatsText.text = $"ATK/ {currentAtk}  DEF/ {currentDef}";
            else
                cardStatsText.text = "";
        }
    }

    IEnumerator LoadCardFrontTexture(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) yield break;

        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);

        // FIX: Usa System.Uri para escapar caracteres especiais (como #) corretamente no caminho
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        currentRequest = request; // Salva na global para OnDisable poder abortar

        yield return request.SendWebRequest();

        // Se a global mudou (outra corrotina iniciou) ou ficou nula (OnDisable), aborta esta execução
        if (currentRequest != request)
        {
            request.Dispose();
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            frontTexture = DownloadHandlerTexture.GetContent(request);
            frontTexture.filterMode = FilterMode.Trilinear;

            if (cardImage == null)
            {
                Debug.LogError($"[CardDisplay] Falha ao aplicar textura: 'cardImage' (RawImage) é nulo na carta '{currentCardData?.name}'.", gameObject);
            }
            else
            {
                // Só aplica a textura da frente se a carta NÃO estiver virada (isFlipped == false)
                if (!isFlipped) cardImage.texture = frontTexture;
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar imagem da frente da carta: {fullPath} | Erro: {request.error}");
        }

        if (currentRequest == request)
        {
            currentRequest = null;
        }
        request.Dispose();
    }

    private Coroutine flipCoroutine;
    
    // Animação fluida de Flip 2D (esmagando e esticando o eixo X)
    private IEnumerator DoFlipAnimation(Texture2D targetTexture)
    {
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = new Vector3(Mathf.Lerp(startScale.x, 0f, t), startScale.y, startScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (cardImage != null) cardImage.texture = targetTexture;
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = new Vector3(Mathf.Lerp(0f, startScale.x, t), startScale.y, startScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = startScale;
    }

    private void TriggerTextureChange(Texture2D newTexture, bool animate = true)
    {
        if (animate && gameObject.activeInHierarchy && DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations)
        {
            if (flipCoroutine != null) StopCoroutine(flipCoroutine);
            flipCoroutine = StartCoroutine(DoFlipAnimation(newTexture));
            
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayFlipEffect(this);
        }
        else if (cardImage != null)
        {
            cardImage.texture = newTexture;
        }
    }

    public void FlipCard()
    {
        if (cardImage == null || frontTexture == null || backTexture == null) return;

        isFlipped = !isFlipped;
        TriggerTextureChange(isFlipped ? backTexture : frontTexture, !isFlipped);
    }

    public void ShowFront()
    {
        if (cardImage == null || frontTexture == null) return;
        if (isFlipped)
        {
            isFlipped = false;
            TriggerTextureChange(frontTexture, true);
        }
    }

    public void ShowBack()
    {
        if (cardImage == null || backTexture == null) return;
        if (!isFlipped)
        {
            isFlipped = true;
            TriggerTextureChange(backTexture, false);
        }
    }

    public void ChangePosition()
    {
        if (position == BattlePosition.Attack)
        {
            position = BattlePosition.Defense;
            // Defesa: Rotaciona 90 graus
            transform.localRotation = Quaternion.Euler(0, 0, 90);
            // Se estava virado para cima, continua virado para cima (Defesa Face-Up)
            // Se estava virado para baixo (Set), continua virado para baixo (Defesa Face-Down)
        }
        else
        {
            position = BattlePosition.Attack;
            // Ataque: Reto
            transform.localRotation = Quaternion.identity;

            // Flip Summon: Se estava virado para baixo em defesa e muda para ataque, vira para cima
            if (isFlipped)
            {
                RevealCard();
                // Flip Summon conta como invocação? Em regras oficiais sim, mas aqui tratamos como mudança de posição.
                // Efeitos de Flip seriam disparados aqui.
            }
            GameManager.Instance.OnBattlePositionChanged(this);
            // Informa o EventManager da invocação
            if (GameManager.Instance != null)
                GameManager.Instance.OnSummon(this);
        }
    }

    // Novo método para revelar carta (Flip) com verificação de exceções
    public void RevealCard(bool isAttackTriggered = false, bool triggerEffects = true)
    {
        if (!isFlipped) return; // Já está revelada

        // Verifica exceções via SpellTrapManager ou efeitos de monstros
        // Por exemplo, "Light of Intervention" impede monstros de serem setados face-down, ou revela todos.
        // Aqui é um bom lugar para hooks de efeitos de "Flip Effect" monsters.

        if (isAttackTriggered)
        {
            Debug.Log($"CardDisplay: Carta {currentCardData.name} revelada por ataque!");
        }

        // Verifica se é um monstro de efeito FLIP
        if (triggerEffects && (currentCardData.description.StartsWith("FLIP:") || currentCardData.description.Contains("FLIP:")))
        {
            if (CardEffectManager.Instance != null)
                CardEffectManager.Instance.ExecuteCardEffect(this);
        }

        ShowFront();

        // Se for Spell/Trap, pode ser que precise ficar revelada ou ir pro GY dependendo do tipo (Continuous vs Normal)
        // Isso será tratado pelo GameManager/SpellTrapManager na resolução da chain.
    }

    // --- SISTEMA VISUAL DE RELÓGIO (TURN CLOCK) ---
    
    private void UpdateTurnClockVisual()
    {
        if (GameManager.Instance == null || !GameManager.Instance.enableTurnClockVisuals) 
            return;

        // O Relógio agora é um painel central.
        // Aqui apenas atualizamos os detalhes da carta para mostrar o ícone pequeno de texto ⏳
        DisplayCardDetails();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoveredCard = this;

        bool shouldShowOutline = enableHoverOutline;
        bool isTopGraveyard = false;

        if (GameManager.Instance != null)
        {
            // Detecta automaticamente se está em uma pilha verificando os pais
            // Isso corrige casos onde isInPile não foi setado manualmente
            bool detectedInPile = isInPile;
            Transform parent = transform.parent;
            
            bool isPlayerDeck = (GameManager.Instance.playerDeckDisplay != null && parent == GameManager.Instance.playerDeckDisplay.contentParent);
            bool isOpponentDeck = (GameManager.Instance.opponentDeckDisplay != null && parent == GameManager.Instance.opponentDeckDisplay.contentParent);
            bool isPlayerGY = (GameManager.Instance.playerGraveyardDisplay != null && parent == GameManager.Instance.playerGraveyardDisplay.contentParent);
            bool isOpponentGY = (GameManager.Instance.opponentGraveyardDisplay != null && parent == GameManager.Instance.opponentGraveyardDisplay.contentParent);
            bool isPlayerExtra = (GameManager.Instance.playerExtraDeckDisplay != null && parent == GameManager.Instance.playerExtraDeckDisplay.contentParent);
            bool isOpponentExtra = (GameManager.Instance.opponentExtraDeckDisplay != null && parent == GameManager.Instance.opponentExtraDeckDisplay.contentParent);
            bool isPlayerRemoved = (GameManager.Instance.playerRemovedDisplay != null && parent == GameManager.Instance.playerRemovedDisplay.contentParent);
            bool isOpponentRemoved = (GameManager.Instance.opponentRemovedDisplay != null && parent == GameManager.Instance.opponentRemovedDisplay.contentParent);

            if (isPlayerDeck || isOpponentDeck || isPlayerGY || isOpponentGY || isPlayerExtra || isOpponentExtra || isPlayerRemoved || isOpponentRemoved)
            {
                detectedInPile = true;
            }

            if (isOnField) shouldShowOutline = GameManager.Instance.enableFieldHoverOutline;
            else if (detectedInPile)
            {
                // Deck
                if (isPlayerDeck || isOpponentDeck)
                {
                    shouldShowOutline = GameManager.Instance.enableDeckHoverOutline;
                }
                // Graveyard
                else if (isPlayerGY || isOpponentGY)
                {
                    shouldShowOutline = GameManager.Instance.enableGraveyardHoverOutline;
                    // Verifica se é o topo do cemitério
                    if (transform.GetSiblingIndex() == parent.childCount - 1) isTopGraveyard = true;
                }
                // Extra Deck
                else if (isPlayerExtra || isOpponentExtra)
                {
                    shouldShowOutline = GameManager.Instance.enableExtraDeckHoverOutline;
                }
                // Removed
                else if (isPlayerRemoved || isOpponentRemoved)
                {
                    shouldShowOutline = GameManager.Instance.enableRemovedHoverOutline;
                }
                else
                {
                    shouldShowOutline = false;
                }
            }            
            else if (isInteractable) shouldShowOutline = GameManager.Instance.enableHandHoverOutline; // Mão
            // Verifica se é o Card Viewer
            else if (GameManager.Instance.cardViewerDisplay == this) shouldShowOutline = GameManager.Instance.enableCardViewerOutline;
        }

        // Exceção: Se for o topo do cemitério, SEMPRE mostra o outline para indicar interatividade (Power of Chaos style)
        if (isTopGraveyard) shouldShowOutline = true;

        // Se estiver selecionado para ataque, não mostra o outline de hover (verde/amarelo) para não sobrescrever o vermelho
        if (isAttackSelected) shouldShowOutline = false;

        // --- Efeito de Borda (Hover) ---
        if (shouldShowOutline)
        {
            if (useSimpleOutline)
            {
                // Opção 1: Usa o componente Outline do Unity no PAI (gameObject)
                // Aplicar no pai garante que a máscara não corte o contorno externo
                Outline outline = GetComponent<Outline>();
                if (outline == null) outline = gameObject.AddComponent<Outline>();

                // Usa a cor do tema se disponível, senão usa a cor local
                if (useSimpleHover)
                {
                    outline.effectColor = hoverColor;
                }
                else
                {
                    outline.effectColor = (GameManager.Instance != null) ? (isPlayerCard ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor) : hoverColor;
                }
                outline.effectDistance = new Vector2(4, -4); // Espessura da borda
                // FIX: Usa o alpha do gráfico (sprite arredondado) para desenhar a borda
                outline.useGraphicAlpha = true;
                outline.enabled = true;
            }
            else if (outlineImage != null)
            {
                // Opção 2: Usa a imagem separada (se useSimpleOutline for false)
                outlineImage.color = (GameManager.Instance != null) ? (isPlayerCard ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor) : hoverColor;
                outlineImage.gameObject.SetActive(true);
            }
        }

        // --- Efeito de Subir (Apenas Mão) ---
        // Verifica se é interativo E se o hover está habilitado no GameManager E não é hover simples (deck)
        if (isInteractable && !useSimpleHover && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect && enableHoverLift)
        {
            // FIX: Usa Canvas Sorting para trazer para frente visualmente sem recalcular o Layout
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10; // Valor alto para ficar por cima de tudo

            // Move para cima (Y) mantendo a escala original
            rectTransform.anchoredPosition += new Vector2(0, hoverYOffset);
        }

        // Prioridade: Se o DeckBuilder estiver aberto, usa o visualizador dele
        if (DeckBuilderManager.Instance != null && DeckBuilderManager.Instance.gameObject.activeInHierarchy)
        {
            DeckBuilderManager.Instance.OnCardHover(currentCardData);
        }
        // Caso contrário, usa o visualizador do Duelo (GameManager)
        else if (GameManager.Instance != null)
        {
            // Evita que o visualizador atualize a si mesmo se o mouse passar por cima dele
            if (GameManager.Instance.cardViewerDisplay != null && this == GameManager.Instance.cardViewerDisplay) return;

            // Lógica para visualizar extra deck mesmo virado para baixo
            bool forceShowFaceUp = false;
            if (GameManager.Instance.playerExtraDeckDisplay != null && transform.parent == GameManager.Instance.playerExtraDeckDisplay.contentParent)
            {
                forceShowFaceUp = true;
            }

            // Se for topo do cemitério (calculado acima), força face-up
            if (isTopGraveyard)
            {
                forceShowFaceUp = true;
            }

            // Se a carta estiver virada para baixo (isFlipped), mostra o verso, a menos que seja do jogador ou extra deck
            bool showFaceUp = !isFlipped || forceShowFaceUp || isPlayerCard;
            GameManager.Instance.UpdateCardViewer(this, showFaceUp);
        }

        // --- LÓGICA DO MOUSE TOOLTIP ---
        if (GameManager.Instance != null && GameManager.Instance.useMouseTooltipUI && isPlayerCard && MouseTooltipUI.Instance != null && currentCardData != null)
        {
            string left = "", right = "";
            if (!isOnField) // NA MÃO
            {
                if (currentCardData.type.Contains("Monster")) { left = "Summon"; right = "Set"; }
                else { left = "Activate"; right = "Set"; }
            }
            else // NO CAMPO
            {
                if (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2)
                {
                    if (currentCardData.type.Contains("Monster")) { left = "Activate"; right = "Change Pos"; }
                    else { left = "Activate"; right = ""; }
                }
                else if (PhaseManager.Instance.currentPhase == GamePhase.Battle)
                {
                    if (currentCardData.type.Contains("Monster") && position == BattlePosition.Attack) 
                    { 
                        left = "Attack"; right = "Cancel"; 
                    }
                }
            }
            MouseTooltipUI.Instance.Show(left, right);
        }

        // FASE 4: Pulso Sincronizado para Equipamentos e Cartas Vínculadas
        HighlightLinkedCards(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (HoveredCard == this) HoveredCard = null;

        // --- Remove Borda ---
        if (useSimpleOutline)
        {
            Outline outline = GetComponent<Outline>();
            // FIX: Só desativa se NÃO estiver selecionado para ataque. Se estiver, mantém o vermelho.
            if (outline != null && !isAttackSelected) outline.enabled = false;
        }

        if (outlineImage != null && !isAttackSelected)
        {
            outlineImage.gameObject.SetActive(false);
        }

        // Desliga o Pulso
        HighlightLinkedCards(false);

        // --- Remove Efeito de Subir ---
        if (isInteractable && !useSimpleHover && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect && enableHoverLift)
        {
            // FIX: Reseta o Canvas e a Escala
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            rectTransform.anchoredPosition -= new Vector2(0, hoverYOffset);
        }

        // FIX 4: NÃO limpamos o Card Viewer aqui para ele ficar "travado".
        
        if (MouseTooltipUI.Instance != null) MouseTooltipUI.Instance.Hide();
    }

    // Método para ativar o brilho de tributo (Luz Azul)
    public void SetTributeHighlight(bool active)
    {
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(active);
            outlineImage.color = tributeColor;
        }
        
        // Efeito da imagem de tributo (Cinemática)
        if (active)
        {
            if (tributeIconObj == null)
            {
                tributeIconObj = new GameObject("TributeIcon", typeof(RectTransform), typeof(Image));
                tributeIconObj.transform.SetParent(transform, false);
                
                RectTransform rt = tributeIconObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(60f, 60f); // Tamanho do ícone no centro
                rt.anchoredPosition = Vector2.zero;

                Image img = tributeIconObj.GetComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.9f);
                if (DuelFXManager.Instance != null && DuelFXManager.Instance.tributeIconSprite != null)
                    img.sprite = DuelFXManager.Instance.tributeIconSprite;
                else
                    img.color = new Color(1f, 0.5f, 0f, 0.8f); // Fallback laranja
                
                // Pisca suavemente enquanto estiver selecionado
                VfxAutoAnim anim = tributeIconObj.AddComponent<VfxAutoAnim>();
                anim.duration = 1000f; // Infinito (some ao desativar)
                anim.fadeType = VfxAutoAnim.FadeType.Blink;
                anim.blinkSpeed = 5f;
                anim.scaleType = VfxAutoAnim.ScaleType.None;
            }
            tributeIconObj.SetActive(true);
            tributeIconObj.transform.SetAsLastSibling();
        }
        else
        {
            if (tributeIconObj != null) tributeIconObj.SetActive(false);
        }
    }

    private void ClearConnectionLines()
    {
        foreach (var line in activeConnectionLines) { if (line != null) Destroy(line); }
        activeConnectionLines.Clear();
    }

    private void HighlightLinkedCards(bool highlight)
    {
        linkedCardsToHighlight.Clear();
        ClearConnectionLines();
        
        // Encontra todos os fios invisíveis do tabuleiro
        CardLink[] links = FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links)
        {
            if (link.source == this && link.target != null) linkedCardsToHighlight.Add(link.target);
            if (link.target == this && link.source != null) linkedCardsToHighlight.Add(link.source);
        }

        // Pede para o alvo pulsar no mesmo ritmo (Brilho Ciano)
        foreach (var card in linkedCardsToHighlight)
        {
            card.SetTributeHighlight(highlight);
            
            // Se estamos ativando o Hover, desenhamos a Linha de Energia Ciano
            if (highlight)
            {
                GameObject lineObj = new GameObject("EquipConnectionLine", typeof(RectTransform), typeof(Image));
                
                // Coloca na raiz do Canvas para não ser cortado pela hierarquia das zonas
                Canvas rootCanvas = GetComponentInParent<Canvas>();
                if (rootCanvas != null) lineObj.transform.SetParent(rootCanvas.transform, false);
                
                RectTransform rt = lineObj.GetComponent<RectTransform>();
                Image img = lineObj.GetComponent<Image>();
                img.color = new Color(tributeColor.r, tributeColor.g, tributeColor.b, 0.7f); // Ciano translúcido
                
                // Matemática para desenhar uma linha reta entre a Carta A e Carta B
                Vector3 startPos = this.transform.position;
                Vector3 endPos = card.transform.position;
                Vector3 dir = endPos - startPos;
                
                rt.position = startPos + (dir / 2f); // Centro do caminho
                
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rt.rotation = Quaternion.Euler(0, 0, angle); // Aponta pra carta
                
                float canvasScale = rootCanvas != null ? rootCanvas.transform.localScale.x : 1f;
                rt.sizeDelta = new Vector2(dir.magnitude / canvasScale, 15f); // 15px de espessura de linha
                
                lineObj.transform.SetAsLastSibling(); // Fica por cima de tudo
                
                // Adiciona nosso canivete suíço para fazer a linha pulsar!
                VfxAutoAnim anim = lineObj.AddComponent<VfxAutoAnim>();
                anim.duration = 1000f; // Tempo infinito enquanto o mouse estiver sobre a carta
                anim.fadeType = VfxAutoAnim.FadeType.Blink;
                anim.blinkSpeed = 5f;
                anim.scaleType = VfxAutoAnim.ScaleType.None;
                
                activeConnectionLines.Add(lineObj);
            }
        }
    }

    // Define o visual de "Selecionado para Atacar"
    public void SetAttackSelectionVisual(bool selected)
    {
        if (this == null || gameObject == null) return;
        
        isAttackSelected = selected; // Atualiza o estado

        // Se o efeito estiver desabilitado, garante que a cor da carta esteja normal e sai.
        if (GameManager.Instance == null || !GameManager.Instance.enableAttackSelectionVisual)
        {
            if (cardImage != null) cardImage.color = Color.white;
            if (outlineImage != null) outlineImage.gameObject.SetActive(false);
            if (useSimpleOutline)
            {
                Outline outline = GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
            }
            return;
        }

        // FIX: Garante que a imagem da carta fique branca (sem tintura cinza/escura)
        if (cardImage != null)
        {
            cardImage.color = Color.white;
        }

        // Aplica o Outline Vermelho
        if (useSimpleOutline)
        {
            Outline outline = GetComponent<Outline>();
            if (outline == null && selected) outline = gameObject.AddComponent<Outline>();

            if (outline != null)
            {
                outline.enabled = selected;
                if (selected)
                {
                    outline.effectColor = attackColor; // Vermelho
                    outline.effectDistance = new Vector2(4, -4);
                    outline.useGraphicAlpha = true;
                }
            }
        }
        else if (outlineImage != null)
        {
            outlineImage.color = attackColor;
            outlineImage.gameObject.SetActive(selected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // FIX: Adiciona uma trava de segurança. Se não estivermos em um duelo ativo
        // (o PhaseManager não existe ou o duelo acabou), o clique esquerdo não faz nada.
        // O clique direito e o arrastar são gerenciados pelo DeckDragHandler.
        if (PhaseManager.Instance == null || (GameManager.Instance != null && GameManager.Instance.isDuelOver))
        {
            // No DeckBuilder, o DeckDragHandler cuida das interações.
            return;
        }

        // --- SISTEMA FULL TEST (Menu de Desenvolvedor da Carta) ---
        bool isShiftPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            isShiftPressed = true;
#else
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            isShiftPressed = true;
#endif

        if (GameManager.Instance != null && GameManager.Instance.fullTestMode && eventData.button == PointerEventData.InputButton.Right && isShiftPressed)
        {
            if (FullTestManager.Instance != null)
            {
                FullTestManager.Instance.OpenDevCardMenu(this);
                return;
            }
        }

        // Lógica para cartas em PILHAS (Cemitério, Extra Deck, Banidas)
        // Como ativamos o Raycast na carta do topo, ela intercepta o clique do PileDisplay.
        if (isInPile)
        {
            if (GameManager.Instance != null)
            {
                Transform parent = transform.parent;
                // Verifica de qual pilha é pai e abre o visualizador correspondente
                if (GameManager.Instance.playerGraveyardDisplay != null && parent == GameManager.Instance.playerGraveyardDisplay.contentParent)
                    GameManager.Instance.ViewGraveyard(true);
                else if (GameManager.Instance.opponentGraveyardDisplay != null && parent == GameManager.Instance.opponentGraveyardDisplay.contentParent)
                    GameManager.Instance.ViewGraveyard(false);
                else if (GameManager.Instance.playerExtraDeckDisplay != null && parent == GameManager.Instance.playerExtraDeckDisplay.contentParent)
                    GameManager.Instance.ViewExtraDeck(true);
                else if (GameManager.Instance.playerRemovedDisplay != null && parent == GameManager.Instance.playerRemovedDisplay.contentParent)
                    GameManager.Instance.ViewRemovedCards(true);
                else if (GameManager.Instance.opponentRemovedDisplay != null && parent == GameManager.Instance.opponentRemovedDisplay.contentParent)
                    GameManager.Instance.ViewRemovedCards(false);
                
                // Deck Principal: Comprar Carta (Esquerdo) ou Render-se (Direito)
                else if (GameManager.Instance.playerDeckDisplay != null && parent == GameManager.Instance.playerDeckDisplay.contentParent)
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        UIManager.Instance.ShowConfirmation("Deseja render-se e abandonar o duelo?", () =>
                        {
                            GameManager.Instance.EndDuel(false); // Fim de duelo, Vitória do Oponente
                        });
                    }
                    else if (eventData.button == PointerEventData.InputButton.Left && GameManager.Instance.canPlayerDrawFromDeck)
                    {
                        GameManager.Instance.DrawCard();
                    }
                }
            }
            return;
        }

        // Lógica de Seleção Direta da Mão (GameManager)
        if (GameManager.Instance != null && GameManager.Instance.isSelectingFromHand && isInteractable)
        {
            // Passa o clique para o GameManager gerenciar a seleção
            GameManager.Instance.HandleHandCardClick(this);
            return;
        }

        // Clique Direito: Mudar Posição (se no campo)
        if (eventData.button == PointerEventData.InputButton.Right && isOnField && currentCardData.type.Contains("Monster"))
        {
            // TODO LUA: Implementar comunicação genérica de intenção de mudança de posição
            ChangePosition(); // Por enquanto apenas vira visualmente
            return;
        }

        Debug.Log($"CardDisplay: Clique detectado na carta {currentCardData?.name}");

        // Lógica de Batalha (Battle Phase)
        if (GameManager.Instance != null && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle)
        {
            // Bloqueia ações de ataque se não for o turno do jogador!
            if (GameManager.Instance.isPlayerTurn)
            {
                if (isOnField && isPlayerCard && currentCardData.type.Contains("Monster"))
                {
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        if (hasAttackedThisTurn)
                        {
                            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Este monstro já atacou neste turno.");
                            return;
                        }
                        CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(this);
                        SetAttackSelectionVisual(true);
                    }
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        CardEffectManager.Instance.luaDuel.currentAttacker = null;
                        SetAttackSelectionVisual(false);
                    }
                    return;
                }
                else if (isOnField && !isPlayerCard && currentCardData.type.Contains("Monster"))
                {
                    if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.confirmAttackTarget && UIManager.Instance != null)
                        {
                            UIManager.Instance.ShowConfirmation($"Atacar {currentCardData.name}?", () => {
                                ExecuteAttackToTarget(this);
                            });
                        }
                        else
                        {
                            ExecuteAttackToTarget(this);
                        }
                    }
                    return;
                }
            }
            else // Turno do Oponente (Controle Manual)
            {
                // Apenas em modo de teste e com IA desligada
                if (GameManager.Instance.fullTestMode && OpponentAI.Instance != null && !OpponentAI.Instance.gameObject.activeSelf)
                {
                    if (isOnField && !isPlayerCard && currentCardData.type.Contains("Monster"))
                    {
                        // Seleciona o monstro do oponente como atacante
                        if (eventData.button == PointerEventData.InputButton.Left)
                        {
                            if (hasAttackedThisTurn)
                            {
                                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Este monstro já atacou neste turno.");
                                return;
                            }
                            CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(this);
                            SetAttackSelectionVisual(true);
                        }
                        else if (eventData.button == PointerEventData.InputButton.Right)
                        {
                            CardEffectManager.Instance.luaDuel.currentAttacker = null;
                            SetAttackSelectionVisual(false);
                        }
                        return;
                    }
                    else if (isOnField && isPlayerCard && currentCardData.type.Contains("Monster"))
                    {
                        // Seleciona o monstro do jogador como alvo
                        if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
                        {
                            if (GameManager.Instance != null && GameManager.Instance.confirmAttackTarget && UIManager.Instance != null)
                            {
                                UIManager.Instance.ShowConfirmation($"Oponente ataca {currentCardData.name}?", () => {
                                    ExecuteAttackToTarget(this);
                                });
                            }
                            else
                            {
                                ExecuteAttackToTarget(this);
                            }
                        }
                        return;
                    }
                }
            }
        }

        // Lógica para cartas na MÃO (isInteractable é o flag para isso)
        if (isInteractable)
        {
            bool canInteract = false;
            if (isPlayerCard && GameManager.Instance.canPlacePlayerCards)
            {
                canInteract = true;
            }
            else if (!isPlayerCard && GameManager.Instance.canPlaceOpponentCards)
            {
                canInteract = true;
            }

            if (canInteract && GameManager.Instance != null)
            {
                bool isLeftClick = eventData.button == PointerEventData.InputButton.Left;
                bool isRightClick = eventData.button == PointerEventData.InputButton.Right;
                bool isMonster = currentCardData.type.Contains("Monster");
                bool isSpellTrap = currentCardData.type.Contains("Spell") || currentCardData.type.Contains("Trap");

                // --- LÓGICA DE AÇÃO RÁPIDA (QUICK ACTION / MOUSE TOOLTIP) ---
                if (GameManager.Instance.useMouseTooltipUI || GameManager.Instance.quickSummonFromHand || GameManager.Instance.quickSpellTrapFromHand)
                {
                    if (isMonster)
                    {
                        if (isLeftClick) GameManager.Instance.TrySummonMonster(gameObject, currentCardData, false); // Ataque
                        else if (isRightClick) GameManager.Instance.TrySummonMonster(gameObject, currentCardData, true); // Defesa (Set)
                    }
                    else if (isSpellTrap)
                    {
                        if (isLeftClick) GameManager.Instance.PlaySpellTrap(gameObject, currentCardData, false); // Ativar
                        else if (isRightClick) GameManager.Instance.PlaySpellTrap(gameObject, currentCardData, true); // Setar
                    }
                    return; // Termina a ação aqui se usou atalho
                }

                // --- MENU CLÁSSICO ---
                if (isLeftClick)
                {
                    if (DuelActionMenu.Instance != null)
                    {
                        DuelActionMenu.Instance.ShowMenu(this);
                    }
                    else
                    {
                        Debug.LogWarning("DuelActionMenu.Instance está nulo! Certifique-se de que o objeto na cena está ativo e possui o script.");
                    }
                }
            }
            return; // Ação para carta na mão termina aqui
        }

        // Lógica para cartas no CAMPO (Spells/Traps Setadas ou Monstros Ativando Efeito)
        if (isOnField && isPlayerCard)
        {
            bool isLeftClick = eventData.button == PointerEventData.InputButton.Left;

            if (GameManager.Instance.useMouseTooltipUI)
            {
                if (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2)
                {
                    if (currentCardData.type.Contains("Monster")) {
                        if (isLeftClick) CardEffectManager.Instance.ExecuteCardEffect(this);
                    } else {
                        if (isLeftClick && isFlipped) GameManager.Instance.ActivateFieldSpellTrap(gameObject);
                    }
                }
                return;
            }
            else
            {
                if (isLeftClick)
                {
                    if (DuelActionMenu.Instance != null)
                    {
                        DuelActionMenu.Instance.ShowMenu(this);
                    }
                    else
                    {
                        // Fallback antigo
                        if (isFlipped && (currentCardData.type.Contains("Spell") || currentCardData.type.Contains("Trap")))
                        {
                            bool canActivate = true;
                            if (currentCardData.type.Contains("Spell") && currentCardData.property != "Quick-Play") canActivate = true;
                            if (canActivate) UIManager.Instance.ShowConfirmation($"Ativar {currentCardData.name}?", () => GameManager.Instance.ActivateFieldSpellTrap(gameObject));
                        }
                    }
                }
            }
        }
    }

    private void ExecuteAttackToTarget(CardDisplay targetCard)
    {
        if (CardEffectManager.Instance == null || CardEffectManager.Instance.luaDuel.currentAttacker == null) return;
        
        // Marca que o atacante concluiu o ataque neste turno
        CardEffectManager.Instance.luaDuel.currentAttacker.unityCard.hasAttackedThisTurn = true;

        CardEffectManager.Instance.luaDuel.currentAttackTarget = new LuaCard(targetCard);
        CardEffectManager.Instance.luaDuel.currentAttacker.unityCard.SetAttackSelectionVisual(false);

        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayAttackDeclare();
        
        var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
        CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
            CardEffectManager.Instance.luaDuel.currentAttacker, 
            CardEffectManager.Instance.luaDuel.currentAttackTarget));
            
        CardEffectManager.Instance.luaDuel.currentAttacker = null;
    }

#if UNITY_EDITOR
    // Chamado automaticamente quando você altera algo no Inspector
    void OnValidate()
    {
        // Se a referência estiver vazia, tenta encontrar automaticamente um filho chamado "Art"
        if (cardImage == null)
        {
            Transform art = transform.Find("Art");
            if (art != null) cardImage = art.GetComponent<RawImage>();
        }
    }

    // Adiciona uma opção no menu de contexto do componente (Botão Direito -> Configurar Hierarquia)
    [ContextMenu("Configurar Hierarquia (Art)")]
    public void SetupHierarchy()
    {
        // 1. Verifica ou Cria o objeto filho "Art"
        Transform artTransform = transform.Find("Art");
        if (artTransform == null)
        {
            GameObject artObj = new GameObject("Art");
            artObj.transform.SetParent(transform, false);
            artTransform = artObj.transform;
            
            // Configura RectTransform para esticar (Stretch) em toda a carta
            RectTransform rt = artObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; // Zera offsets para preencher tudo
        }

        // 2. Garante que o filho tem o componente RawImage
        RawImage childRawImage = artTransform.GetComponent<RawImage>();
        if (childRawImage == null) childRawImage = artTransform.gameObject.AddComponent<RawImage>();
        childRawImage.raycastTarget = false; // IMPORTANTE: Desativa Raycast para não bloquear o pai

        // 3. Se existir RawImage no pai (configuração antiga), migra para o filho e remove do pai
        RawImage parentRawImage = GetComponent<RawImage>();
        if (parentRawImage != null)
        {
            if (childRawImage.texture == null) childRawImage.texture = parentRawImage.texture;
            childRawImage.color = parentRawImage.color;
            
            // Remove o componente do pai para evitar conflito com a máscara
            UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(parentRawImage);
        }

        // 4. Vincula a referência no script
        cardImage = childRawImage;

        // 5. Garante que o pai tenha Image e Mask para o arredondamento funcionar
        if (GetComponent<Image>() == null) gameObject.AddComponent<Image>();
        if (GetComponent<Mask>() == null) gameObject.AddComponent<Mask>().showMaskGraphic = true;

        Debug.Log("Hierarquia da carta ajustada com sucesso: Pai(Mask) -> Art(RawImage).");
    }
#endif

    // Oculta/Exibe a carta fisicamente para a Cinemática
    public void SetVisibility(bool visible)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = visible ? 1f : 0f;
    }

    void OnEnable()
    {
        // Se a carta foi configurada enquanto estava inativa (ex: dentro de um painel fechado), 
        // a textura pode não ter carregado porque corrotinas não rodam em objetos inativos.
        // Tenta carregar agora.
        if (currentCardData != null && frontTexture == null && cardImage != null && currentRequest == null)
        {
             StartCoroutine(LoadCardFrontTexture(currentCardData.image_filename));
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
        ClearConnectionLines();
    }

    void OnDestroy()
    {
        if (currentRequest != null)
        {
            currentRequest.Dispose();
            currentRequest = null;
        }
        if (frontTexture != null)
        {
            Destroy(frontTexture);
            frontTexture = null;
        }
        ClearConnectionLines();
    }
}
