using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq; // Added for LINQ

public class CardDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
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

    private CardData currentCardData;
    private Texture2D frontTexture;
    private Texture2D backTexture;
    public bool isFlipped = false; // Tornado público para acesso externo
    private RectTransform rectTransform;

    // Componentes para corrigir a renderização e tremedeira
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private Vector3 originalScale = Vector3.one;

    [HideInInspector] public float hoverYOffset = 30f;
    [HideInInspector] public bool isInteractable = false; // Usado para habilitar hover apenas para cartas na mão
    [HideInInspector] public bool isPlayerCard = false; // Define se a carta pertence ao jogador (para visualização)
    [HideInInspector] public bool isOnField = false; // Define se a carta está no campo
    [HideInInspector] public bool isInPile = false; // Define se a carta está em uma pilha (Deck, GY, Extra)
    [HideInInspector] public BattlePosition position; // Posição de batalha do monstro

    // Variáveis de Estado de Turno
    [HideInInspector] public bool hasAttackedThisTurn = false;
    [HideInInspector] public bool hasChangedPositionThisTurn = false;
    [HideInInspector] public bool summonedThisTurn = false;
    [HideInInspector] public bool battledThisTurn = false;
    [HideInInspector] public bool wasSpecialSummoned = false;
    [HideInInspector] public bool hasUsedEffectThisTurn = false;
    [HideInInspector] public int attacksDeclaredThisTurn = 0; // Substitui/Complementa hasAttackedThisTurn
    [HideInInspector] public int maxAttacksPerTurn = 1; // Padrão 1
    [HideInInspector] public bool cannotInflictBattleDamage = false; // Para Union Attack
    [HideInInspector] public int paidLifePoints = 0; // Para Wall of Revealing Light

    [HideInInspector] public bool isTributeSummoned = false; // Novo: Rastreia Invocação por Tributo

    // Stats em Tempo Real (Modificados por efeitos)
    [HideInInspector] public int originalAtk;
    [HideInInspector] public int originalDef;
    [HideInInspector] public int currentAtk;
    [HideInInspector] public int currentDef;

    // Sistema de Spell Counters
    [HideInInspector] public int spellCounters = 0;

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

    // Sistema de Efeitos Retardados
    [HideInInspector] public bool scheduledForDestruction = false;
    [HideInInspector] public int destructionTurnCountdown = -1;
    [HideInInspector] public bool destructionCountdownOwnerIsPlayer;

    // Sistema de Fusão
    [HideInInspector] public List<CardData> fusionMaterialsUsed = new List<CardData>();

    // Lista de modificadores ativos nesta carta
    public List<StatModifier> activeModifiers = new List<StatModifier>(); // Changed to public

    public CardData CurrentCardData => currentCardData; // Propriedade pública para acesso seguro (Renomeado para evitar conflito)

    private UnityWebRequest currentRequest; // Rastreia a requisição ativa para descarte correto
    private bool isAttackSelected = false; // Rastreia se a carta está selecionada para atacar
    private GameObject turnClockInstance; // Instância do relógio visual

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

        activeModifiers.Clear(); // Limpa modificadores antigos ao resetar a carta
        spellCounters = 0; // Reseta contadores
        turnCounter = 0; // Reseta contadores de turno
        maxTurnCounter = 0;

        originalScale = transform.localScale; // Salva a escala inicial definida pelo GameManager

        DisplayCardDetails();

        // Se começar virada, já aplica o verso imediatamente
        if (isFlipped && cardImage != null && backTexture != null)
        {
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

        // Não usamos 'using' aqui para poder descartar manualmente se a corrotina for interrompida
        currentRequest = UnityWebRequestTexture.GetTexture(url);

        yield return currentRequest.SendWebRequest();

        if (currentRequest.result == UnityWebRequest.Result.Success)
        {
            frontTexture = DownloadHandlerTexture.GetContent(currentRequest);
            frontTexture.filterMode = FilterMode.Trilinear;

            // DEBUG: Verifica se a imagem e a textura são válidas
            if (cardImage == null)
            {
                Debug.LogError($"[CardDisplay] Falha ao aplicar textura: 'cardImage' (RawImage) é nulo na carta '{currentCardData?.name}'.", gameObject);
            }
            else
            {
                Debug.Log($"[CardDisplay] Textura para '{currentCardData?.name}' carregada com sucesso. Aplicando agora.");
                // Só aplica a textura da frente se a carta NÃO estiver virada (isFlipped == false)
                if (!isFlipped) cardImage.texture = frontTexture;
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar imagem da frente da carta: {fullPath} | Erro: {currentRequest.error}");
        }

        currentRequest.Dispose();
        currentRequest = null;
    }

    public void FlipCard()
    {
        if (cardImage == null || frontTexture == null || backTexture == null) return;

        isFlipped = !isFlipped;
        cardImage.texture = isFlipped ? backTexture : frontTexture;
    }

    public void ShowFront()
    {
        if (cardImage == null || frontTexture == null) return;
        isFlipped = false;
        cardImage.texture = frontTexture;
    }

    public void ShowBack()
    {
        if (cardImage == null || backTexture == null) return;
        isFlipped = true;
        cardImage.texture = backTexture;
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
    public void RevealCard(bool isAttackTriggered = false)
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
        if (currentCardData.description.StartsWith("FLIP:") || currentCardData.description.Contains("FLIP:"))
        {
            if (CardEffectManager.Instance != null)
                CardEffectManager.Instance.ExecuteCardEffect(this);
        }

        ShowFront();

        // Se for Spell/Trap, pode ser que precise ficar revelada ou ir pro GY dependendo do tipo (Continuous vs Normal)
        // Isso será tratado pelo GameManager/SpellTrapManager na resolução da chain.
    }

    // --- SISTEMA DE SPELL COUNTERS ---

    public void AddSpellCounter(int amount = 1)
    {
        spellCounters += amount;
        Debug.Log($"{currentCardData.name} ganhou {amount} Spell Counter(s). Total: {spellCounters}");
        // TODO: Adicionar visualização (ícone ou texto sobre a carta)
    }

    public void RemoveSpellCounter(int amount = 1)
    {
        spellCounters = Mathf.Max(0, spellCounters - amount);
        Debug.Log($"{currentCardData.name} perdeu {amount} Spell Counter(s). Total: {spellCounters}");
    }

    // --- SISTEMA VISUAL DE RELÓGIO (TURN CLOCK) ---
    
    private void UpdateTurnClockVisual()
    {
        if (GameManager.Instance == null || !GameManager.Instance.enableTurnClockVisuals) 
        {
            if (turnClockInstance != null) turnClockInstance.SetActive(false);
            return;
        }

        // Se não tem contador, não precisa de relógio
        if (_turnCounter <= 0 && turnClockInstance == null) return;

        // Instancia se necessário
        if (turnClockInstance == null && GameManager.Instance.turnClockPrefab != null)
        {
            turnClockInstance = Instantiate(GameManager.Instance.turnClockPrefab, transform);
            turnClockInstance.transform.localPosition = Vector3.zero; // Centralizado na carta
            turnClockInstance.transform.localScale = Vector3.one;
            // Dica: O prefab deve ter um Canvas ou ser um elemento de UI world space ajustado
        }

        if (turnClockInstance != null)
        {
            var controller = turnClockInstance.GetComponent<TurnClockController>();
            if (controller != null)
            {
                controller.UpdateClock(_turnCounter, maxTurnCounter);
            }
        }
    }

    // --- SISTEMA DE MODIFICADORES DE STATS ---

    public void AddStatModifier(StatModifier mod)
    {
        activeModifiers.Add(mod);
        RecalculateStats();
    }

    public void RemoveStatModifier(string modId)
    {
        activeModifiers.RemoveAll(m => m.id == modId);
        RecalculateStats();
    }

    public void RemoveModifiersFromSource(CardDisplay source)
    {
        int removed = activeModifiers.RemoveAll(m => m.source == source);
        if (removed > 0) RecalculateStats();
    }

    public void CleanExpiredModifiers()
    {
        int removed = activeModifiers.RemoveAll(m => m.removeAtEndPhase);
        if (removed > 0) RecalculateStats();
    }

    public void RecalculateStats()
    {
        if (currentCardData == null) return;

        // 1. Começa com o valor base da carta
        int finalAtk = currentCardData.atk >= 0 ? currentCardData.atk : 0;
        int finalDef = currentCardData.def >= 0 ? currentCardData.def : 0;

        // 2. Aplica modificadores que definem um valor (Set) - ex: Megamorph, Beast King Barbaros
        foreach (var mod in activeModifiers)
        {
            if (mod.operation == StatModifier.Operation.Set)
            {
                if (mod.statType == StatModifier.StatType.ATK) finalAtk = mod.value;
                if (mod.statType == StatModifier.StatType.DEF) finalDef = mod.value;
            }
        }

        // 3. Aplica adições e subtrações (Add) - ex: Equipamentos, Campos, Buffs
        foreach (var mod in activeModifiers)
        {
            // 1857 - The Emperor's Holiday: Nega Equip Spells
            if (mod.type == StatModifier.ModifierType.Equipment && GameManager.Instance != null && GameManager.Instance.IsCardActiveOnField("1857"))
            {
                continue;
            }

            if (mod.operation == StatModifier.Operation.Add)
            {
                int valueToAdd = mod.value;

                // Reverse Trap (1526): Inverte adições e subtrações
                if (CardEffectManager.Instance != null && CardEffectManager.Instance.reverseStats)
                {
                    // Se for buff (+500), vira debuff (-500). Se for debuff (-500), vira buff (+500).
                    valueToAdd = -valueToAdd;
                }

                if (mod.statType == StatModifier.StatType.ATK) finalAtk += valueToAdd;
                if (mod.statType == StatModifier.StatType.DEF) finalDef += valueToAdd;
            }
        }

        // 4. Aplica multiplicadores (Multiply) - ex: Limiter Removal, Shrink
        foreach (var mod in activeModifiers)
        {
            if (mod.operation == StatModifier.Operation.Multiply)
            {
                if (mod.statType == StatModifier.StatType.ATK) finalAtk = Mathf.FloorToInt(finalAtk * mod.multiplier);
                if (mod.statType == StatModifier.StatType.DEF) finalDef = Mathf.FloorToInt(finalDef * mod.multiplier);
            }
        }

        // Garante que não fique negativo
        currentAtk = Mathf.Max(0, finalAtk);
        currentDef = Mathf.Max(0, finalDef);

        DisplayCardDetails(); // Atualiza o texto na carta
    }

    // Método antigo mantido para compatibilidade, agora usa o novo sistema
    public void ModifyStats(int atkChange, int defChange)
    {
        // Cria modificadores temporários (até o fim do turno) por padrão para chamadas antigas
        if (atkChange != 0) AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, atkChange, null));
        if (defChange != 0) AddStatModifier(new StatModifier(StatModifier.StatType.DEF, StatModifier.ModifierType.Temporary, StatModifier.Operation.Add, defChange, null));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
        if (isInteractable && !useSimpleHover && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect)
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
            GameManager.Instance.UpdateCardViewer(currentCardData, showFaceUp);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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

        // --- Remove Efeito de Subir ---
        if (isInteractable && !useSimpleHover && rectTransform != null && GameManager.Instance != null && GameManager.Instance.enableHandHoverEffect)
        {
            // FIX: Reseta o Canvas e a Escala
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            rectTransform.anchoredPosition -= new Vector2(0, hoverYOffset);
        }

        // FIX 4: NÃO limpamos o Card Viewer aqui para ele ficar "travado".
    }

    // Método para ativar o brilho de tributo (Luz Azul)
    public void SetTributeHighlight(bool active)
    {
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(active);
            outlineImage.color = tributeColor;
            // Dica: Você pode adicionar um componente de animação (ping-pong alpha) na imagem da borda para pulsar
        }
    }

    // Define o visual de "Selecionado para Atacar"
    public void SetAttackSelectionVisual(bool selected)
    {
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
                
                // Deck: Compra carta (se permitido)
                else if (GameManager.Instance.playerDeckDisplay != null && parent == GameManager.Instance.playerDeckDisplay.contentParent && GameManager.Instance.canPlayerDrawFromDeck)
                    GameManager.Instance.DrawCard();
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

        // Lógica de Seleção de Tributo (Prioridade Máxima)
        if (SummonManager.Instance != null && SummonManager.Instance.isSelectingTributes)
        {
            if (isOnField && isPlayerCard && currentCardData.type.Contains("Monster"))
            {
                SummonManager.Instance.SelectTributeCandidate(this);
            }
            return; // Não faz mais nada se estiver selecionando tributo
        }

        // Lógica de Seleção de Alvo (Spell/Trap)
        if (SpellTrapManager.Instance != null && SpellTrapManager.Instance.isSelectingTarget)
        {
            // Passa o clique para o gerenciador validar
            SpellTrapManager.Instance.SelectTarget(this);
            return;
        }

        // Clique Direito: Mudar Posição (se no campo)
        if (eventData.button == PointerEventData.InputButton.Right && isOnField && currentCardData.type.Contains("Monster"))
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.TryChangePosition(this);
            return;
        }

        Debug.Log($"CardDisplay: Clique detectado na carta {currentCardData?.name}");

        // Lógica de Batalha (Battle Phase)
        if (GameManager.Instance != null && PhaseManager.Instance != null && PhaseManager.Instance.currentPhase == GamePhase.Battle)
        {
            // Se for carta do jogador e monstro: Seleciona como atacante
            if (isOnField && isPlayerCard && currentCardData.type.Contains("Monster"))
            {
                if (BattleManager.Instance != null)
                {
                    if (BattleManager.Instance.currentAttacker == this)
                        BattleManager.Instance.CancelAttack(); // Clicar de novo cancela
                    else
                    {
                        BattleManager.Instance.PrepareAttack(this);

                        // Lógica de Ataque Direto Rápido (Quick Attack)
                        if (GameManager.Instance.quickAttackDirectly && BattleManager.Instance.currentAttacker == this)
                        {
                            if (BattleManager.Instance.CanAttackDirectly())
                            {
                                BattleManager.Instance.TryDirectAttack();
                            }
                        }
                    }
                }
                return;
            }
            // Se for carta do oponente e monstro: Seleciona como alvo
            else if (isOnField && !isPlayerCard && currentCardData.type.Contains("Monster"))
            {
                if (BattleManager.Instance != null && BattleManager.Instance.currentAttacker != null)
                {
                    BattleManager.Instance.SelectTarget(this);
                }
                return;
            }
        }

        // Lógica para cartas na MÃO (isInteractable é o flag para isso)
        if (isInteractable)
        {
            if (DuelActionMenu.Instance == null)
            {
                Debug.LogError("CardDisplay: DuelActionMenu.Instance não encontrado na cena!");
                return;
            }

            bool canInteract = false;
            if (isPlayerCard && GameManager.Instance.canPlacePlayerCards)
            {
                canInteract = true;
            }
            else if (!isPlayerCard && GameManager.Instance.canPlaceOpponentCards)
            {
                canInteract = true;
            }

            // --- LÓGICA DE AÇÃO RÁPIDA (QUICK ACTION) ---
            if (canInteract && GameManager.Instance != null)
            {
                bool isMonster = currentCardData.type.Contains("Monster");
                bool isSpellTrap = currentCardData.type.Contains("Spell") || currentCardData.type.Contains("Trap");

                if (isMonster && GameManager.Instance.quickSummonFromHand)
                {
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        GameManager.Instance.TrySummonMonster(gameObject, currentCardData, false); // Ataque
                        return;
                    }
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        GameManager.Instance.TrySummonMonster(gameObject, currentCardData, true); // Defesa (Set)
                        return;
                    }
                }
                else if (isSpellTrap && GameManager.Instance.quickSpellTrapFromHand)
                {
                    if (eventData.button == PointerEventData.InputButton.Left)
                    {
                        GameManager.Instance.PlaySpellTrap(gameObject, currentCardData, false); // Ativar
                        return;
                    }
                    else if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        GameManager.Instance.PlaySpellTrap(gameObject, currentCardData, true); // Setar
                        return;
                    }
                }
            }

            if (canInteract)
            {
                DuelActionMenu.Instance.ShowMenu(gameObject, currentCardData);
            }
            return; // Ação para carta na mão termina aqui
        }

        // Lógica para cartas no CAMPO (Spells/Traps Setadas)
        if (isOnField && isFlipped && (currentCardData.type.Contains("Spell") || currentCardData.type.Contains("Trap")))
        {
            bool canActivate = (GameManager.Instance.devMode) || (!summonedThisTurn);
            
            // Regra: Magias Normais, Campo, Equipamento e Ritual podem ser ativadas no turno que foram setadas.
            // Apenas Armadilhas e Quick-Play Spells precisam esperar.
            if (currentCardData.type.Contains("Spell") && currentCardData.property != "Quick-Play")
            {
                canActivate = true;
            }

            if (canActivate)
            {
                UIManager.Instance.ShowConfirmation($"Ativar {currentCardData.name}?", () =>
                {
                    GameManager.Instance.ActivateFieldSpellTrap(gameObject);
                });
            }
        }
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

    void OnEnable()
    {
        // Se a carta foi configurada enquanto estava inativa (ex: dentro de um painel fechado), 
        // a textura pode não ter carregado porque corrotinas não rodam em objetos inativos.
        // Tenta carregar agora.
        if (currentCardData != null && frontTexture == null && cardImage != null)
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
    }
}
