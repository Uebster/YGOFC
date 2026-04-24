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
    public TextMeshProUGUI cardITypeText;
    public TextMeshProUGUI cardIRaceText;
    public TextMeshProUGUI cardILvlText;    
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
    private Dictionary<HighlightCategory, GameObject> fallbackHighlightIcons = new Dictionary<HighlightCategory, GameObject>();

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
    [HideInInspector] public int originalLevel;
    [HideInInspector] public int currentLevel;

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
    [HideInInspector] public string clientHintText = "";
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

    [HideInInspector] public int summonedTurnCount = -1; // Rastreia o turno em que a carta foi invocada
    [HideInInspector] public bool hasChangedPositionThisTurn = false; // Rastreia se a posição foi alterada manualmente
    
    private bool isHoveredUp = false;
    private Coroutine hoverAnimation;
    private Vector2 basePosition;
    private bool basePositionSet = false;

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
                    if (t.name.Contains("IType")) cardITypeText = t;
                    if (t.name.Contains("IRace")) cardIRaceText = t;
                    if (t.name.Contains("ILvl")) cardILvlText = t;
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
        originalLevel = card.level;
        currentLevel = card.level;

        clientHintText = ""; // Reseta o hint ao mudar de carta
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
        if (cardITypeText != null) cardITypeText.text = "";
        if (cardDescriptionText != null) cardDescriptionText.text = "";
        if (cardStatsText != null) cardStatsText.text = "";
        if (cardIRaceText != null) cardIRaceText.text = "";
        if (cardILvlText != null) cardILvlText.text = "";
    }

    private void DisplayCardDetails()
    {
        if (currentCardData == null) return;

        if (cardNameText != null) cardNameText.text = currentCardData.name;
        if (cardDescriptionText != null) 
        {
            cardDescriptionText.text = currentCardData.description;
            if (!string.IsNullOrEmpty(clientHintText) && !isFlipped)
            {
                cardDescriptionText.text += $"\n\n<color=yellow><b>[{clientHintText}]</b></color>";
            }
        }

        string displayedType = !string.IsNullOrEmpty(currentCardData.typeline) ? currentCardData.typeline : currentCardData.type;
        if (cardITypeText != null) cardITypeText.text = $"[{displayedType}]";
        if (cardIRaceText != null) cardIRaceText.text = !string.IsNullOrEmpty(currentCardData.race) ? currentCardData.race : "";

        if (cardILvlText != null)
        {
            if (currentCardData.level > 0)
            {
                string lvlColorTag = "";
                if (GameManager.Instance != null) {
                    // Verde para buff (redução), vermelho para debuff (aumento)
                    if (currentLevel < originalLevel) lvlColorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(GameManager.Instance.statBuffColor)}>";
                    else if (currentLevel > originalLevel) lvlColorTag = $"<color=#{ColorUtility.ToHtmlStringRGB(GameManager.Instance.statDebuffColor)}>";
                }
                cardILvlText.text = $"LV: {lvlColorTag}{currentLevel}{(lvlColorTag != "" ? "</color>" : "")}";
                if (_turnCounter > 0) cardILvlText.text += $" / <color=yellow>⏳ {_turnCounter}</color>";
            } else {
                cardILvlText.text = "";
            }
        }

        if (cardStatsText != null)
        {
            if (currentCardData.type.Contains("Monster"))
                cardStatsText.text = $"ATK/ {currentAtk}  DEF/ {currentDef}";
            else
                cardStatsText.text = "";
        }
    }

    public void ResetStatsToOriginal()
    {
        currentAtk = originalAtk;
        currentDef = originalDef;
        currentLevel = originalLevel;
        DisplayCardDetails();
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
    private IEnumerator DoFlipAnimation(Texture2D targetTexture, System.Action onComplete = null)
    {
        float baseDuration = 0.3f;
        if (DuelFXManager.Instance != null) baseDuration = DuelFXManager.Instance.flipAnimationDuration;
        float totalDuration = baseDuration / (DuelFXManager.Instance != null && DuelFXManager.Instance.animationSpeed > 0 ? DuelFXManager.Instance.animationSpeed : 1f);
        float halfDuration = totalDuration / 2f;
        
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = new Vector3(Mathf.Lerp(startScale.x, 0f, t), startScale.y, startScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (cardImage != null) cardImage.texture = targetTexture;
        
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            transform.localScale = new Vector3(Mathf.Lerp(0f, startScale.x, t), startScale.y, startScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = startScale;

        // O Efeito/Pulse acontece estritamente APÓS a carta terminar de desvirar
        if (DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations) DuelFXManager.Instance.PlayFlipEffect(this);
        
        onComplete?.Invoke();
    }

    private void TriggerTextureChange(Texture2D newTexture, bool isRevealing, bool allowAnimation = true, System.Action onComplete = null)
    {
        bool animate = false;
        // A animação só é permitida se o chamador permitir E o GameManager estiver configurado para isso
        if (allowAnimation && GameManager.Instance != null)
        {
            switch (GameManager.Instance.flipMode)
            {
                case FlipAnimationMode.AnimateAlways:
                    animate = true;
                    break;
                case FlipAnimationMode.AnimateOnReveal:
                    animate = isRevealing;
                    break;
                case FlipAnimationMode.NoAnimation:
                    animate = false;
                    break;
            }
        }

        if (animate && gameObject.activeInHierarchy && DuelFXManager.Instance != null && DuelFXManager.Instance.enableAnimations)
        {
            if (flipCoroutine != null) StopCoroutine(flipCoroutine);
            flipCoroutine = StartCoroutine(DoFlipAnimation(newTexture, onComplete));
        }
        else if (cardImage != null)
        {
            cardImage.texture = newTexture;
            onComplete?.Invoke();
        }
        else { onComplete?.Invoke(); }
    }

    public void FlipCard()
    {
        if (cardImage == null || frontTexture == null || backTexture == null) return;

        isFlipped = !isFlipped;
        // O segundo parâmetro é 'isRevealing'. Ele é true quando a carta VAI para a frente.
        // Se isFlipped se tornou false, significa que a carta foi revelada.
        TriggerTextureChange(isFlipped ? backTexture : frontTexture, !isFlipped, true);
    }

    public void ShowFront(bool animate = true, System.Action onComplete = null)
    {
        if (cardImage == null || frontTexture == null) { onComplete?.Invoke(); return; }
        if (isFlipped)
        {
            isFlipped = false;
            TriggerTextureChange(frontTexture, true, animate, onComplete);
        }
        else { onComplete?.Invoke(); }
    }

    public Texture2D GetFrontTexture()
    {
        return frontTexture;
    }

    public void ForceTexture(Texture2D tex)
    {
        if (flipCoroutine != null) StopCoroutine(flipCoroutine);
        if (cardImage != null && tex != null) cardImage.texture = tex;
    }

    public void ShowBack(bool animate = true, System.Action onComplete = null)
    {
        if (cardImage == null || backTexture == null) { onComplete?.Invoke(); return; }
        if (!isFlipped)
        {
            isFlipped = true;
            TriggerTextureChange(backTexture, false, animate, onComplete);
        }
        else { onComplete?.Invoke(); }
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
            if (GameManager.Instance != null) GameManager.Instance.OnBattlePositionChanged(this);
        }
        else
        {
            position = BattlePosition.Attack;
            // Ataque: Reto
            transform.localRotation = Quaternion.identity;

            // Flip Summon: Se estava virado para baixo em defesa e muda para ataque, vira para cima
            if (isFlipped)
            {
                RevealCard(false, true, () => {
                    if (GameManager.Instance != null) {
                        GameManager.Instance.OnBattlePositionChanged(this);
                        GameManager.Instance.OnFlipSummon(this);
                    }
                });
            }
            else
            {
                if (GameManager.Instance != null) {
                    GameManager.Instance.OnBattlePositionChanged(this);
                }
            }
        }
    }

    // Novo método para revelar carta (Flip) com verificação de exceções
    public void RevealCard(bool isAttackTriggered = false, bool triggerEffects = true, System.Action onComplete = null)
    {
        if (!isFlipped) { onComplete?.Invoke(); return; } // Já está revelada

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
            if (CardEffectManager.Instance != null && CardEffectManager.Instance.eventManager != null)
            {
                Debug.Log($"<color=yellow>[FLIP EFFECT]</color> {currentCardData.name} foi virado, disparando evento de FLIP para a Engine LUA.");
                CardEffectManager.Instance.eventManager.OnFlip(this);
            }
        }

        ShowFront(true, onComplete);

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
    
    public bool CanBeActivatedNow()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlayerTurn) return false;
        if (PhaseManager.Instance == null || (PhaseManager.Instance.currentPhase != GamePhase.Main1 && PhaseManager.Instance.currentPhase != GamePhase.Main2)) return false;
        if (CardEffectManager.Instance != null && (CardEffectManager.Instance.isWaitingForLuaYield || CardEffectManager.Instance.isChainResolving || CardEffectManager.Instance.isFastEffectWindowOpen)) return false;

        if (isOnField && isPlayerCard)
        {
            if (CurrentCardData.type.Contains("Monster") && CurrentCardData.type.Contains("Effect") && !isFlipped)
            {
                if (CardEffectManager.Instance != null)
                {
                    LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(this);
                    LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0040 || e.type == 0x0080);
                    if (eff != null && CardEffectManager.Instance.CanActivateEffect(lc, eff, 0, null))
                    {
                        return true;
                    }
                }
            }
            else if (CurrentCardData.type.Contains("Spell") || CurrentCardData.type.Contains("Trap"))
            {
                if (isFlipped)
                {
                    // Regra de Traps: Não podem ser ativadas no turno em que foram setadas
                    if (CurrentCardData.type.Contains("Trap") && summonedTurnCount == GameManager.Instance.turnCount) return false;
                    // Quick-Play Spells também não podem ser ativadas no turno em que foram setadas
                    if (CurrentCardData.property == "Quick-Play" && summonedTurnCount == GameManager.Instance.turnCount) return false;

                    if (CardEffectManager.Instance != null)
                    {
                        LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(this);
                        LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080);
                        if (eff != null && CardEffectManager.Instance.CanActivateEffect(lc, eff, 0, null))
                        {
                            return true;
                        }
                        else if (eff == null)
                        {
                            // Fallback: Se não tem script LUA atrelado, assumimos que é uma magia básica C# ativável
                            return true;
                        }
                        return false;
                    }
                    return true;
                }
                else
                {
                    // Face-up Spell/Trap
                    if (CardEffectManager.Instance != null)
                    {
                        LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(this);
                        LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0040); // IGNITION
                        if (eff != null && CardEffectManager.Instance.CanActivateEffect(lc, eff, 0, null))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
        return false;
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
        // A lógica do outline foi movida para dentro da corrotina de animação para melhor controle

        // --- Efeito de Subir (Apenas Mão) ---
        if (isInteractable && !isHoveredUp && !useSimpleHover && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect && enableHoverLift)
        {
            AnimateHover(true);
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

        // --- FEEDBACK VISUAL DE 1-CLICK ACTIVATE (HOVER) ---
        bool showHoverActivate = false;
        HighlightCategory hoverCategory = HighlightCategory.EffectActivation;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isSelectingResponse)
            {
                if (GameManager.Instance.IsResponseCandidate(this))
                {
                    showHoverActivate = true;
                    hoverCategory = HighlightCategory.ChainResponse;
                }
            }
            else if (GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow())
            {
                showHoverActivate = true;
                hoverCategory = HighlightCategory.EffectActivation;
            }
        }

        if (showHoverActivate)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.SetSelectionIcon(this, hoverCategory, SelectionState.Available);
            else SetHighlight(hoverCategory, true);
        }

        // --- LÓGICA DO MOUSE TOOLTIP ---
        if (GameManager.Instance != null && GameManager.Instance.useMouseTooltipUI && isPlayerCard && MouseTooltipUI.Instance != null && currentCardData != null)
        {
            string left = "", right = "";
            
            if (GameManager.Instance.isSelectingResponse)
            {
                left = "Activate"; right = "Cancel";
            }
            else if (!isOnField) // NA MÃO
            {
                if (currentCardData.type.Contains("Monster")) { left = "Summon"; right = "Set"; }
                else { left = "Activate"; right = "Set"; }
            }
            else // NO CAMPO
            {
                if (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2)
                {
                    if (currentCardData.type.Contains("Monster")) { 
                        if (GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) left = "Activate";
                        else if (!GameManager.Instance.activateEffectsWithOneClick) left = "Menu";
                        right = "Change Pos"; 
                    }
                    else { 
                        if (isFlipped && GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) left = "Activate";
                        else if (!isFlipped && GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) left = "Effect";
                        else if (!GameManager.Instance.activateEffectsWithOneClick) left = "Menu";
                        right = ""; 
                    }
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

        // --- Espadinha de Ataque no Hover ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleAttackIndicatorHover(this, true);
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

        // Desliga o Feedback Visual de 1-Click
        if (GameManager.Instance != null)
        {
            if (DuelFXManager.Instance != null)
            {
                DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.EffectActivation, SelectionState.None);
                DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.ChainResponse, SelectionState.None);
            }
            else
            {
                SetHighlight(HighlightCategory.EffectActivation, false);
                SetHighlight(HighlightCategory.ChainResponse, false);
            }
        }

        // Desliga o Pulso
        HighlightLinkedCards(false);

        // --- Remove Efeito de Subir ---
        if (isInteractable && isHoveredUp && !useSimpleHover && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect && enableHoverLift)
        {
            AnimateHover(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleAttackIndicatorHover(this, false);
        }

        // FIX 4: NÃO limpamos o Card Viewer aqui para ele ficar "travado".
        
        if (MouseTooltipUI.Instance != null) MouseTooltipUI.Instance.Hide();
    }

    public void ForceHover()
    {
        if (isInteractable && !isHoveredUp && !useSimpleHover && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect && enableHoverLift)
        {
            AnimateHover(true);
        }
    }

    private void AnimateHover(bool isUp)
    {
        if (!basePositionSet)
        {
            basePosition = rectTransform.anchoredPosition;
            basePositionSet = true;
        }
        if (hoverAnimation != null) StopCoroutine(hoverAnimation);
        hoverAnimation = StartCoroutine(HoverAnimationRoutine(isUp));
    }

    private IEnumerator HoverAnimationRoutine(bool isUp)
    {
        isHoveredUp = isUp;

        // Define a posição alvo apenas no eixo Y para não brigar com o LayoutGroup no eixo X
        float startY = rectTransform.anchoredPosition.y;
        float endY = isUp ? basePosition.y + hoverYOffset : basePosition.y;

        // Lógica de Outline
        Outline outline = GetComponent<Outline>();
        if (useSimpleOutline && outline == null) outline = gameObject.AddComponent<Outline>();
        
        Color outlineColor = hoverColor;
        if (!useSimpleHover && GameManager.Instance != null)
        {
            outlineColor = isPlayerCard ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor;
        }

        bool shouldShowOutline = enableHoverOutline && isUp && !isAttackSelected;

        if (useSimpleOutline && outline != null)
        {
            outline.enabled = shouldShowOutline;
            if (shouldShowOutline)
            {
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(4, -4);
                outline.useGraphicAlpha = true;
            }
        }
        else if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(shouldShowOutline);
            if (shouldShowOutline) outlineImage.color = outlineColor;
        }

        // Lógica de Canvas Sorting
        if (isUp)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
        }

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rectTransform == null) yield break; // Segurança se a carta for destruída
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, Mathf.Lerp(startY, endY, t));
            yield return null;
        }

        if (rectTransform != null) rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, endY);

        if (!isUp)
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }

        hoverAnimation = null;
    }

    public void SetHighlight(HighlightCategory category, bool active)
    {
        Color glowColor = tributeColor;
        SelectionIconSettings settings = null;

        if (DuelFXManager.Instance != null)
        {
            settings = DuelFXManager.Instance.GetIconSettings(category);
            if (settings != null) glowColor = settings.selectedColor;
        }

        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(active);
            outlineImage.color = glowColor;
        }
        
        if (active)
        {
            if (settings == null || !settings.useIcon) return;

            if (!fallbackHighlightIcons.ContainsKey(category) || fallbackHighlightIcons[category] == null)
            {
                GameObject iconObj = new GameObject("HighlightIcon_" + category.ToString(), typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(transform, false);
                fallbackHighlightIcons[category] = iconObj;
            }
                
            GameObject tributeIconObj = fallbackHighlightIcons[category];
            tributeIconObj.SetActive(true);
            tributeIconObj.transform.SetAsLastSibling();


            RectTransform rt = tributeIconObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(settings.size, settings.size);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;

            Image img = tributeIconObj.GetComponent<Image>();
            img.color = settings.selectedColor;
            img.sprite = settings.sprite;
            img.material = settings.material;
                
            if (settings.useOutline)
            {
                Outline outline = tributeIconObj.GetComponent<Outline>();
                if (outline == null) outline = tributeIconObj.AddComponent<Outline>();
                outline.effectColor = settings.outlineColor;
                outline.effectDistance = new Vector2(settings.selectedOutlineWidth, -settings.selectedOutlineWidth);
            }

            VfxAutoAnim anim = tributeIconObj.GetComponent<VfxAutoAnim>();
            if (anim == null) anim = tributeIconObj.AddComponent<VfxAutoAnim>();
            
            anim.duration = 10000f; // Tempo longo virtual
            anim.fadeType = (settings.blinkFrequency > 0 && settings.blinkAvailable) ? VfxAutoAnim.FadeType.Blink : VfxAutoAnim.FadeType.None;
            anim.blinkSpeed = settings.blinkFrequency;
            
            anim.baseColor = settings.selectedColor;
            anim.scaleType = settings.pulseAvailable ? VfxAutoAnim.ScaleType.Pulse : VfxAutoAnim.ScaleType.None;
            anim.startScale = 1f;
            anim.endScale = settings.pulseScale;
            
            anim.spinEffect = settings.spinAvailable;
            anim.spinSpeed = settings.spinSpeed;
        }
        else
        {
        if (fallbackHighlightIcons.ContainsKey(category) && fallbackHighlightIcons[category] != null)
        {
            fallbackHighlightIcons[category].SetActive(false);
        }
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
            card.SetHighlight(HighlightCategory.LinkedCard, highlight);
            
            if (highlight)
            {
                DrawConnectionLine(this, card);
            }
        }
    }

    private void DrawConnectionLine(CardDisplay sourceCard, CardDisplay targetCard)
    {
        if (DuelFXManager.Instance == null) return;
        ConnectionLineSettings lineSettings = DuelFXManager.Instance.linkedCardLine;
        if (!lineSettings.drawLine) return;

        GameObject lineObj = new GameObject("EquipConnectionLine", typeof(RectTransform), typeof(Image));
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) lineObj.transform.SetParent(rootCanvas.transform, false);
        
        RectTransform rt = lineObj.GetComponent<RectTransform>();
        Image img = lineObj.GetComponent<Image>();
        img.color = lineSettings.lineColor;
        if (lineSettings.material != null) img.material = lineSettings.material;
        
        Vector3 startPos = GetAnchorPosition(sourceCard.GetComponent<RectTransform>(), lineSettings.sourceAnchor);
        Vector3 endPos = GetAnchorPosition(targetCard.GetComponent<RectTransform>(), lineSettings.targetAnchor);
        
        Vector3 dir = endPos - startPos;
        rt.position = startPos + (dir / 2f);
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);
        
        float canvasScale = rootCanvas != null ? rootCanvas.transform.localScale.x : 1f;
        rt.sizeDelta = new Vector2(dir.magnitude / canvasScale, lineSettings.thickness);
        
        lineObj.transform.SetAsLastSibling();
        
        VfxAutoAnim anim = lineObj.AddComponent<VfxAutoAnim>();
        anim.duration = 10000f;
        if (lineSettings.blinkFrequency > 0) { anim.fadeType = VfxAutoAnim.FadeType.Blink; anim.blinkSpeed = lineSettings.blinkFrequency; }
        if (lineSettings.usePulse) { anim.scaleType = VfxAutoAnim.ScaleType.PulseY; anim.startScale = 1f; anim.endScale = lineSettings.pulseThicknessMult; anim.blinkSpeed = lineSettings.blinkFrequency > 0 ? lineSettings.blinkFrequency : 5f; }
        else { anim.scaleType = VfxAutoAnim.ScaleType.None; }
        
        activeConnectionLines.Add(lineObj);
    }

    private Vector3 GetAnchorPosition(RectTransform rt, LineAnchor anchor)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
        switch (anchor) {
            case LineAnchor.Top: return (corners[1] + corners[2]) / 2f;
            case LineAnchor.Bottom: return (corners[0] + corners[3]) / 2f;
            case LineAnchor.Left: return (corners[0] + corners[1]) / 2f;
            case LineAnchor.Right: return (corners[2] + corners[3]) / 2f;
            case LineAnchor.Center: default: return rt.position;
        }
    }

    // Define o visual de "Selecionado para Atacar"
    public void SetAttackSelectionVisual(bool selected)
    {
        if (this == null || gameObject == null) return;
        
        isAttackSelected = selected; // Atualiza o estado

        // Se o efeito estiver desabilitado, garante que a cor da carta esteja normal e sai.
        if (DuelFXManager.Instance == null || !DuelFXManager.Instance.enableAttackSelectionVisual)
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

        Color targetColor = (DuelFXManager.Instance != null) ? DuelFXManager.Instance.colorAttackSelection : attackColor;

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
                    outline.effectColor = targetColor;
                    outline.effectDistance = new Vector2(4, -4);
                    outline.useGraphicAlpha = true;
                }
            }
        }
        else if (outlineImage != null)
        {
            outlineImage.color = targetColor;
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

        if (GameManager.Instance != null && GameManager.Instance.pendingEffectDraws > 0 && !isInPile)
        {
            if (UIManager.Instance != null && !GameManager.Instance.isSimulating) UIManager.Instance.ShowMessage("Você precisa comprar cartas do deck pelo efeito primeiro!");
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

        // Lógica de Seleção Direta Tática (Mão ou Campo)
        if (GameManager.Instance != null && GameManager.Instance.isSelectingFromHand)
        {
            // Passa o clique para o GameManager gerenciar a seleção
            GameManager.Instance.HandleHandCardClick(this);
            return;
        }

        // Lógica de Seleção para Responder a uma Corrente
        if (GameManager.Instance != null && GameManager.Instance.isSelectingResponse)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (DuelFXManager.Instance != null) {
                    DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.EffectActivation, SelectionState.None);
                    DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.ChainResponse, SelectionState.None);
                } else {
                    SetHighlight(HighlightCategory.EffectActivation, false);
                    SetHighlight(HighlightCategory.ChainResponse, false);
                }
                GameManager.Instance.HandleResponseSelection(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (DuelFXManager.Instance != null) {
                    DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.EffectActivation, SelectionState.None);
                    DuelFXManager.Instance.SetSelectionIcon(this, HighlightCategory.ChainResponse, SelectionState.None);
                } else {
                    SetHighlight(HighlightCategory.EffectActivation, false);
                    SetHighlight(HighlightCategory.ChainResponse, false);
                }
                GameManager.Instance.CancelResponseSelection();
            }
            return;
        }

        // Clique Direito: Mudar Posição (se no campo)
        if (eventData.button == PointerEventData.InputButton.Right && isOnField && currentCardData.type.Contains("Monster"))
        {
            if (!isPlayerCard && !GameManager.Instance.devMode) return; // Só pode mudar os seus próprios monstros

            if (PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2))
            {
                if (summonedTurnCount == GameManager.Instance.turnCount)
                {
                    if (UIManager.Instance != null && !GameManager.Instance.isSimulating) UIManager.Instance.ShowMessage("Não pode mudar a posição no turno em que foi invocado.");
                    return;
                }
                if (hasAttackedThisTurn)
                {
                    if (UIManager.Instance != null && !GameManager.Instance.isSimulating) UIManager.Instance.ShowMessage("Não pode mudar a posição após atacar neste turno.");
                    return;
                }
                if (hasChangedPositionThisTurn)
                {
                    if (UIManager.Instance != null && !GameManager.Instance.isSimulating) UIManager.Instance.ShowMessage("Só pode mudar a posição de batalha 1 vez por turno.");
                    return;
                }
                
                if (GameManager.Instance.confirmBattlePositionChange && UIManager.Instance != null && !GameManager.Instance.isSimulating)
                {
                    UIManager.Instance.ShowConfirmation("Mudar a posição de batalha deste monstro?", () => {
                        hasChangedPositionThisTurn = true;
                        ChangePosition();
                    });
                }
                else
                {
                    hasChangedPositionThisTurn = true;
                    ChangePosition();
                }
            }
            return;
        }

        // Debug.Log($"CardDisplay: Clique detectado na carta {currentCardData?.name}");

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
                        if (position != BattlePosition.Attack)
                        {
                            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Monstros em Defesa não podem atacar.");
                            return;
                        }
                        if (hasAttackedThisTurn)
                        {
                            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Este monstro já atacou neste turno.");
                            return;
                        }
                        if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
                        {
                            LuaCard cachedCard = CardEffectManager.Instance.EnsureCardScriptLoaded(this) ?? new LuaCard(this);
                            if (CardEffectManager.Instance.auraManager.IsUnderRestriction(cachedCard, null, "CANNOT_ATTACK", CardLocation.Field))
                            {
                                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Este monstro está impedido de atacar por um efeito de carta.");
                                return;
                            }
                        }
                        CardEffectManager.Instance.luaDuel.currentAttacker = new LuaCard(this);
                        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.ShowAndFollowMouse(transform);
                        SetAttackSelectionVisual(true);
                        if (GameManager.Instance != null) { GameManager.Instance.RefreshAttackIndicators(); GameManager.Instance.HandleAttackIndicatorHover(this, true); }
                    }
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        CardEffectManager.Instance.luaDuel.currentAttacker = null;
                        SetAttackSelectionVisual(false);
                        if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Hide();
                        if (GameManager.Instance != null) { GameManager.Instance.RefreshAttackIndicators(); GameManager.Instance.HandleAttackIndicatorHover(this, true); }
                    }
                    return;
                }
                else if (isOnField && !isPlayerCard && currentCardData.type.Contains("Monster"))
                {
                    if (CardEffectManager.Instance != null && CardEffectManager.Instance.luaDuel.currentAttacker != null)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.confirmAttackTarget && UIManager.Instance != null)
                        {
                            if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.LockOn(transform);
                            string targetName = isFlipped ? "monstro virado para baixo" : currentCardData.name;
                            UIManager.Instance.ShowConfirmation($"Atacar {targetName}?", () => {
                                ExecuteAttackToTarget(this);
                            }, () => {
                                if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.Unlock();
                            });
                        }
                        else
                        {
                            if (TargetingSwordUI.Instance != null) TargetingSwordUI.Instance.LockOn(transform);
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
                            if (position != BattlePosition.Attack)
                            {
                                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Monstros em Defesa não podem atacar.");
                                return;
                            }
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

            if (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2)
            {
                if (currentCardData.type.Contains("Monster")) {
                    if (isLeftClick) {
                        if (GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) {
                            CardEffectManager.Instance.ExecuteCardEffect(this);
                        } else if (!GameManager.Instance.activateEffectsWithOneClick) {
                            if (DuelActionMenu.Instance != null) DuelActionMenu.Instance.ShowMenu(this);
                        }
                    }
                } else {
                    if (isLeftClick) {
                        if (isFlipped) {
                            if (GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) {
                                GameManager.Instance.ActivateFieldSpellTrap(gameObject);
                            } else if (!GameManager.Instance.activateEffectsWithOneClick) {
                                if (DuelActionMenu.Instance != null) DuelActionMenu.Instance.ShowMenu(this);
                            }
                        } else {
                            if (GameManager.Instance.activateEffectsWithOneClick && CanBeActivatedNow()) {
                                CardEffectManager.Instance.ExecuteCardEffect(this);
                            } else if (!GameManager.Instance.activateEffectsWithOneClick) {
                                if (DuelActionMenu.Instance != null) DuelActionMenu.Instance.ShowMenu(this);
                            }
                        }
                    }
                }
            }
            return;
        }
    }

    private void ExecuteAttackToTarget(CardDisplay targetCard)
    {
        if (CardEffectManager.Instance == null || CardEffectManager.Instance.luaDuel.currentAttacker == null) return;
        
        // NOVO: Previne clique duplo ou re-ataque se o monstro já iniciou o ataque
        if (CardEffectManager.Instance.luaDuel.currentAttacker.unityCard.hasAttackedThisTurn) return;
        
        // Marca que o atacante concluiu o ataque neste turno
        CardEffectManager.Instance.luaDuel.currentAttacker.unityCard.hasAttackedThisTurn = true;
        if (GameManager.Instance != null) GameManager.Instance.RefreshAttackIndicators();

        CardEffectManager.Instance.luaDuel.currentAttackTarget = new LuaCard(targetCard);
        CardEffectManager.Instance.luaDuel.currentAttacker.unityCard.SetAttackSelectionVisual(false);

        if (DuelFXManager.Instance != null)
            DuelFXManager.Instance.PlayAttackDeclare();
        
        var func = CardEffectManager.Instance.luaEngine.Globals.Get("Core").Table.Get("Attack").Function;
        CardEffectManager.Instance.StartCoroutine(CardEffectManager.Instance.RunGenericLuaCoroutine(func, 
            CardEffectManager.Instance.luaDuel.currentAttacker, 
            CardEffectManager.Instance.luaDuel.currentAttackTarget));
            
        // CardEffectManager.Instance.luaDuel.currentAttacker = null; // Removido para prevenir amnésia assíncrona no LUA
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
