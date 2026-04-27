using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class FieldSpellTheme
{
    public string themeName;
    public List<string> keywords;
    public Color overlayColor;
    [Range(0f, 1f)] public float intensity;
}

public enum AttackTrailType { Shadows, ContinuousLine, SmoothShadows }
public enum HandShuffleType { CrossSwap, CollapseAndFan }
public enum ControlSwapImpactType { Squeeze, Pulse }
public enum SummonVFXType { Normal, Special, Tribute, Fusion, Ritual }
public enum SelectionState { None, Available, Selected }
public enum PlacementAuraRenderMode { Behind, Above }
public enum HighlightCategory { Tribute, Fusion, Ritual, SpecialSummon, GenericTarget, LinkedCard, ChainResponse, EffectActivation }
public enum LineAnchor { Center, Top, Bottom, Left, Right }

[System.Serializable]
public class SelectionIconSettings
{
    public bool useIcon = true;
    public bool usePrefab = false;
    public GameObject prefab;
    public bool useNative = true;
    public Sprite sprite;
    public Material material;
    public Color availableColor = new Color(1f, 0.5f, 0f, 1f);
    public Color selectedColor = Color.red;
    public float size = 80f;
    public bool showAvailableState = true;

    [Header("Animation - General")]
    public float spinSpeed = 90f;
    public float pulseScale = 1.1f;
    public float blinkFrequency = 5f;

    [Header("Animation - Available State")]
    public bool blinkAvailable = true;
    public bool pulseAvailable = false;
    public bool spinAvailable = true;

    [Header("Animation - Selected State")]
    public bool blinkSelected = false;
    public bool pulseSelected = false;
    public bool spinSelected = true;

    public bool useOutline = false;
    public Color outlineColor = new Color(1f, 1f, 1f, 0.5f);
    public float outlineWidth = 5f;
    public float selectedOutlineWidth = 5f;
}

[System.Serializable]
public class FieldMarkerSettings
{
    public bool useMarker = true;
    public bool usePrefab = false;
    public GameObject prefab;
    public bool useNative = true;
    public Color color = new Color(0.8f, 0.2f, 1f, 0.5f);
    public float duration = 1.0f;
    public float startSize = 0.1f;
    public float endSize = 2.0f;
}

[System.Serializable]
public class SummonImpactSettings
{
    public bool useImpact = true;
    public bool usePrefab = true;
    public GameObject prefab;
    public bool useNativePulse = false;
    public Color pulseColor = Color.white;
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.2f;
    public float pulseOutlineWidth = 8f;
    [Tooltip("Atraso entre a marca de chão e o impacto físico da carta (em segundos).")]
    public float delayBeforeImpact = 0.3f;
}

[System.Serializable]
public class CinematicSettings
{
    public bool useCinematic = true;
    public bool useNative = true;

    [Header("Símbolo de Fundo")]
    public Sprite backgroundSymbol;
    [Tooltip("Material opcional (ex: Aditivo) para fazer o símbolo brilhar e ignorar fundos pretos.")]
    public Material backgroundMaterial;
    public bool preserveBackgroundAspect = true;
    public bool spinBackgroundSymbol = true;
    public float backgroundSpinSpeed = -45f;
    public float symbolScale = 400f;

    [Header("Materiais da Invocação")]
    [Tooltip("Mostra a arte real das cartas sendo sacrificadas no vórtice (em vez do verso).")]
    public bool showMaterialsFaceUp = true;

    [Header("Transições Nativas")]
    [Tooltip("Escurece a tela durante a animação.")]
    public bool useDarkOverlay = true;
    [Tooltip("Clarão branco nativo no clímax da animação.")]
    public bool useWhiteFlash = true;

    [Header("Partículas Extra")]
    public bool usePrefab = false;
    public GameObject flashPrefab;

    [Header("Timings & Coreografia")]
    public float darkOverlayFadeDuration = 0.3f;
    public float orbitDuration = 1.5f;
    public float cardsOrbitSpeed = 1080f;
    public float flashDuration = 0.4f;
    public float giantCardAppearDuration = 0.3f;
    public float giantCardHoldDuration = 0.6f;
    public float delayBeforeMarker = 0f;
    public float delayBeforeCardDrop = 0.3f;
    public float giantCardScale = 1.5f;
}

[System.Serializable]
public class StatusIndicatorSettings
{
    public bool useNative = true;
    public Sprite sprite;
    public Material material;
    public Color color = Color.white;
    public bool preserveAspect = true;
    public Vector2 size = new Vector2(50, 50);
    public Vector2 offset = Vector2.zero;
    public float blinkSpeed = 2f;
    public bool useOutline = true;
    public Color outlineColor = Color.black;
    public float outlineWidth = 3f;

    [Header("Prefab Alternativo")]
    public bool usePrefab = false;
    public GameObject prefab;
}

[System.Serializable]
public class SummonVFXPackage
{
    [Header("Ícone de Seleção")]
    public SelectionIconSettings selectionIcon = new SelectionIconSettings();
    [Header("Marcador de Chão (Pouso)")]
    public FieldMarkerSettings fieldMarker = new FieldMarkerSettings();
    [Header("Impacto de Invocação")]
    public SummonImpactSettings impact = new SummonImpactSettings();
    [Header("Cinemática")]
    public CinematicSettings cinematic = new CinematicSettings();
}

[System.Serializable]
public class NormalSummonVFXPackage
{
    [Header("Impacto de Invocação")]
    public SummonImpactSettings impact = new SummonImpactSettings();
}

[System.Serializable]
public class CardFlightSettings
{
    public bool enableFlight = true;
    public float duration = 0.4f;
    public float flightScale = 1.3f;
    [Tooltip("Distância (X, Y) que a carta desliza da pilha antes de voar. Ex: (40, 15) desliza pro lado e um pouco pra cima.")]
    public Vector2 popOffset = new Vector2(40f, 15f);
    public bool flipDuringFlight = false; // NOVO: Vira a carta no meio do voo
    public bool useTrail = true;
    public AttackTrailType trailType = AttackTrailType.Shadows;
    public float trailWidth = 15f;
    public Color trailColor = new Color(0.8f, 0.8f, 1f, 0.5f);
    public bool useImpact = false;
    public ControlSwapImpactType impactType = ControlSwapImpactType.Pulse;
    public Color impactColor = new Color(0.5f, 0.8f, 1f, 1f);
    public float impactScale = 1.4f;
    public float impactOutlineWidth = 8f;
    public float startScaleMult = 1.0f;
    public float endScaleMult = 1.0f;
}

[System.Serializable]
public class ConnectionLineSettings
{
    public bool drawLine = true;
    public Color lineColor = new Color(0f, 0.6f, 1f, 0.7f);
    public float thickness = 15f;
    public Material material;
    public LineAnchor sourceAnchor = LineAnchor.Center;
    public LineAnchor targetAnchor = LineAnchor.Center;
    public float blinkFrequency = 5f;
    public bool usePulse = false;
    public float pulseThicknessMult = 1.5f;
}

public partial class DuelFXManager : MonoBehaviour
{
    public static DuelFXManager Instance;

    [Header("Configurações Globais")]
    public bool enableAnimations = true;
    public float animationSpeed = 1.5f;

    [Header("Referências da Cena")]
    public Transform boardCenter; // Arraste um objeto vazio no centro do campo
    public Transform playerBoardCenter; // Centro da área do jogador
    public Transform opponentBoardCenter; // Centro da área do oponente
    public AudioSource audioSource; // Fonte de áudio principal para SFX
    public AudioSource bgmSource;   // Fonte de áudio para Música de Fundo (Loop)

    [Header("Efeitos Visuais (Prefabs)")]
    public GameObject spellActivateVFX; // Partículas de magia
    public GameObject trapActivateVFX;  // Partículas de armadilha
    public GameObject summonVFX;        // Invocação Comum (Impacto/Poeira)
    public GameObject tokenSummonVFX;   // Invocação de Ficha (Fumaça/Poof)
    public GameObject tributeSummonVFX; // Invocação por Tributo (Novo)
    public GameObject fusionVFX;        // Fusão
    public GameObject tributeVFX;       // Luz azulada / Portal
    public GameObject attackVFX;        // Corte ou impacto
    public GameObject attackProjectileVFX; // Espada / Projétil voador (Novo)
    public GameObject explosionVFX;     // Fumaça/Explosão de destruição
    public GameObject reflectVFX;       // Escudo/Barreira (Ataque falhou)
    public GameObject banishVFX;        // Carta removida de jogo (vórtice/buraco negro)
    public GameObject flipVFX;          // Efeito de Flip
    public GameObject damageVFX;        // Dano direto ou batalha vencida
    public GameObject defenseSuccessVFX;// Defesa bem sucedida (escudo metálico)
    public GameObject chainLinkVFX;     // Prefab do texto flutuante (Ex: "Link 1")
    public GameObject monsterEffectVFX; // Brilho ao ativar efeito de monstro
    [UnityEngine.Serialization.FormerlySerializedAs("summonAuraVFX")]
    public GameObject placementAuraVFX; // Aura por trás ao colocar qualquer carta (Pouso)
    public GameObject shuffleVFX;       // Efeito visual (poeira/luz) ao embaralhar

    [Header("Cores do Tabuleiro (Magias de Campo)")]
    public Image boardBackgroundImage; // Arraste o Panel_Background do seu Tabuleiro aqui
    public Color defaultBoardColor = new Color(0.15f, 0.15f, 0.2f, 1f); // Cor normal
    [Tooltip("Permite que as Magias de Campo pintem suavemente o fundo do tabuleiro.")]
    public bool colorizeBoardByFieldSpell = true;

    [Header("Temas de Magia de Campo (Customização)")]
    public List<FieldSpellTheme> fieldSpellThemes = new List<FieldSpellTheme>();

    [Header("Colorização Automática de Auras")]
    [Tooltip("Colore a Aura de Pouso baseado no dono da carta (Cores de Hover do GameManager). Tem prioridade sobre o tipo.")]
    public bool colorizeAuraByPlayer = false;
    [Tooltip("Colore a Aura de Pouso baseado no tipo da carta automaticamente.")]
    public bool colorizeAuraByType = true;
    public Color colorFaceDown = new Color(0.7f, 0.7f, 0.7f, 1f); // Neutro/Cinza
    public Color colorSpell = new Color(0.1f, 1f, 0.2f, 1f); // Verde Neon Puro
    public Color colorTrap = new Color(0.9f, 0.2f, 0.7f, 1f); // Rosa
    public Color colorMonsterEffect = new Color(0.8f, 0.5f, 0.1f, 1f); // Marrom/Laranja
    public Color colorMonsterNormal = new Color(0.9f, 0.9f, 0.5f, 1f); // Amarelo
    public Color colorFusion = new Color(0.6f, 0.2f, 0.9f, 1f); // Roxo
    public Color colorRitual = new Color(0.2f, 0.5f, 1f, 1f); // Azul

    [Header("Opções de Aura de Pouso (Placement)")]
    [Tooltip("Faz o outline da carta pulsar suavemente na cor predefinida ao ser colocada no campo.")]
    public bool usePlacementAuraRoutine = true;
    public float placementAuraDuration = 1.2f;
    public float placementAuraOutlineWidth = 10f;
    [Tooltip("Instancia o prefab definido em 'Placement Aura VFX' atrás da carta.")]
    public bool usePlacementAuraPrefab = true;
    public float placementAuraPrefabScale = 1.0f;
    public PlacementAuraRenderMode placementAuraRenderMode = PlacementAuraRenderMode.Behind;
    [Tooltip("Se ativado, sobrescreve o tempo de vida do Prefab instanciado.")]
    public bool overridePlacementAuraPrefabDuration = false;
    public float placementAuraPrefabDuration = 3.0f;

    [Header("Opções de Ativação de Magias/Armadilhas")]
    [Tooltip("Faz a carta crescer rapidamente e gerar o fantasma com Neon colorido.")]
    public bool useActivationPulse = true;
    public float activationPulseScale = 1.3f; // O quão gigante a carta fica ao ativar
    public float activationPulseDuration = 0.2f; // Tempo da animação (Padrão: 0.2s)
    public float activationPulseOutlineWidth = 8f;
    [Tooltip("Instancia a partícula 3D/2D definida nos slots 'Spell Activate VFX' e 'Trap Activate VFX'.")]
    public bool useActivationPrefab = true;

    [Header("Opções de Efeito de Monstro")]
    [Tooltip("Faz a carta crescer rapidamente e gerar o fantasma com Neon colorido.")]
    public bool useMonsterEffectPulse = true;
    public float monsterEffectPulseScale = 1.3f; 
    public float monsterEffectPulseDuration = 0.2f; 
    public float monsterEffectPulseOutlineWidth = 8f;
    [Tooltip("Instancia o prefab definido em 'Monster Effect VFX'.")]
    public bool useMonsterEffectPrefab = true;

    [Header("Indicador: Pode Atacar (Can Attack)")]
    public StatusIndicatorSettings canAttackIndicator = new StatusIndicatorSettings();

    [Header("Indicador: Não Pode Atacar (Block)")]
    public StatusIndicatorSettings cannotAttackIndicator = new StatusIndicatorSettings();
    [Tooltip("Mostra o ícone de bloqueio apenas se a carta estiver em modo de Ataque.")]
    public bool showCannotAttackOnlyInAttackPos = false;

    [Header("Indicador: Não Pode Mudar Posição")]
    public StatusIndicatorSettings cannotChangePosIndicator = new StatusIndicatorSettings();

    [Header("Indicador: Mira de Alvo (Targeting Sword)")]
    [Tooltip("Usa a mira de alvo nativa que segue o mouse ao selecionar um atacante.")]
    public bool useTargetingSwordNative = true;
    public StatusIndicatorSettings targetingSwordIndicator = new StatusIndicatorSettings();
    [Tooltip("Ajuste de rotação da mira de alvo.")]
    public float targetingSwordRotationOffset = -90f;
    [Tooltip("Velocidade de giro (Spin) da mira nativa.")]
    public float targetingSwordSpinSpeed = -360f;

    [Header("Indicador: Voo de Ataque (Flight Sword)")]
    public StatusIndicatorSettings flightSwordIndicator = new StatusIndicatorSettings();

    [Header("--- ESTRUTURAS DE INVOCAÇÃO (NOVAS) ---")]
    public NormalSummonVFXPackage normalSummonSettings = new NormalSummonVFXPackage();
    public SummonVFXPackage specialSummonSettings = new SummonVFXPackage();
    public SummonVFXPackage tributeSummonSettings = new SummonVFXPackage();
    public SummonVFXPackage ritualSummonSettings = new SummonVFXPackage();
    public SummonVFXPackage fusionSummonSettings = new SummonVFXPackage();

    [System.Serializable]
    public class ExtractionPileSettings
    {
        public Vector2 slideOffsetPlayer = new Vector2(120f, 0f);
        public Vector2 slideOffsetOpponent = new Vector2(-120f, 0f);
        [Tooltip("Se marcado, a carta volta para o topo da pilha antes de voar pro destino.")]
        public bool returnToTop = false; 
        [Tooltip("Escala inicial da carta ao sair da pilha.")]
        public float startScaleMult = 1.0f;
        [Tooltip("Escala máxima da carta durante a exibição.")]
        public float peakScaleMult = 1.3f;
        [Tooltip("A altura (em pixels) que a carta pula para cima se já estiver no topo.")]
        public float topJumpHeight = 40f;
        [Tooltip("Se ativo, ignora a lógica de topo e força a carta a sempre deslizar lateralmente.")]
        public bool alwaysSlide = false;
    }

    [System.Serializable]
    public class ExtractionCinematicSettings
    {
        public bool enableExtraction = false;
        public float slideDuration = 0.3f;
        public float holdDuration = 0.6f;
        
        [Header("Scale Effect")]
        public bool useScaleEffect = true;

        [Header("Pile Specific Offsets")]
        public ExtractionPileSettings deck = new ExtractionPileSettings { slideOffsetPlayer = new Vector2(150f, 0f), slideOffsetOpponent = new Vector2(-150f, 0f), returnToTop = false };
        public ExtractionPileSettings extraDeck = new ExtractionPileSettings { slideOffsetPlayer = new Vector2(150f, 0f), slideOffsetOpponent = new Vector2(-150f, 0f), returnToTop = false };
        public ExtractionPileSettings graveyard = new ExtractionPileSettings { slideOffsetPlayer = new Vector2(-150f, 0f), slideOffsetOpponent = new Vector2(150f, 0f), returnToTop = false };
        public ExtractionPileSettings banished = new ExtractionPileSettings { slideOffsetPlayer = new Vector2(-150f, 0f), slideOffsetOpponent = new Vector2(150f, 0f), returnToTop = false };
    }

    [Header("--- EXTRAÇÃO CINEMÁTICA ---")]
    public ExtractionCinematicSettings extractionCinematic = new ExtractionCinematicSettings();

    [Header("--- MOVIMENTAÇÃO DE CARTAS (FLIGHT) ---")]
    public CardFlightSettings flightHandToField = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.5f, 1f, 0.5f, 0.5f) };
    public CardFlightSettings flightHandToSpellZone = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.2f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.0f, 0.0f, 0.0f, 0.4f), useImpact = false, flipDuringFlight = true };
    public CardFlightSettings flightHandToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.1f, useTrail = true, trailColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) };
    public CardFlightSettings flightHandToDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightHandToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightFieldToHand = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.5f, 0.5f, 1f, 0.5f) };
    public CardFlightSettings flightFieldToDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 0.8f, 1f, 0.5f) };
    public CardFlightSettings flightFieldToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.1f, useTrail = true, trailColor = new Color(0.3f, 0.3f, 0.3f, 0.5f) };
    public CardFlightSettings flightFieldToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.8f, 0.1f, 0.1f, 0.8f) };
    public CardFlightSettings flightFieldToExtraDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    [Header("- Field Spell Zone Specific -")]
    public CardFlightSettings flightHandToFieldSpellZone = new CardFlightSettings { enableFlight = true, duration = 0.6f, flightScale = 1.5f, useTrail = true, trailType = AttackTrailType.ContinuousLine, trailColor = new Color(0.1f, 0.8f, 0.7f, 0.6f), useImpact = true };
    public CardFlightSettings flightFieldSpellZoneToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.2f, useTrail = true, trailColor = new Color(0.1f, 0.5f, 0.4f, 0.5f) };
    public CardFlightSettings flightFieldSpellZoneToHand = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.1f, 0.7f, 0.9f, 0.5f) };
    public CardFlightSettings flightFieldSpellZoneToDeck = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.1f, 0.7f, 0.9f, 0.5f) };
    public CardFlightSettings flightFieldSpellZoneToBanished = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.9f, 0.1f, 0.2f, 0.8f) };
    [Header("- Deck, GY, Banish Piles -")]
    public CardFlightSettings flightDeckToHand = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.8f, 0.2f, 0.5f) };
    public CardFlightSettings flightDeckToField = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 1f, 0.8f, 0.5f) };
    public CardFlightSettings flightDeckToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightDeckToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightGraveyardToHand = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.8f, 0f, 0.5f) };
    public CardFlightSettings flightGraveyardToDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.5f, 0f, 0.5f) };
    public CardFlightSettings flightGraveyardToExtraDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(1f, 0.5f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightGraveyardToField = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.8f, 0.2f, 1f, 0.5f) };
    public CardFlightSettings flightGraveyardToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightExtraToField = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.8f, 0.2f, 1f, 0.5f) };
    public CardFlightSettings flightExtraToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightExtraToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };    
    public CardFlightSettings flightBanishToHand = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 0.2f, 0.2f, 0.8f) };
    public CardFlightSettings flightBanishToDeck = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.0f, 0.0f, 0.0f, 0.4f), useImpact = false };
    public CardFlightSettings flightBanishToExtraDeck = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.0f, 0.0f, 0.0f, 0.4f), useImpact = false };
    public CardFlightSettings flightBanishToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 0.2f, 0.2f, 0.8f) };
    public CardFlightSettings flightBanishToField = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.1f, 0.1f, 0.1f, 0.5f), useImpact = false };
    [Header("- UI Modals (View-Only) -")]
    public CardFlightSettings flightCardSelectionUI = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.0f, useTrail = false, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.4f), useImpact = false, startScaleMult = 1.0f, endScaleMult = 1.0f, popOffset = Vector2.zero };
    public CardFlightSettings flightCardComparisonUI = new CardFlightSettings { enableFlight = true, duration = 0.6f, flightScale = 1.5f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(1f, 0.8f, 0f, 0.5f), useImpact = false, startScaleMult = 1.0f, endScaleMult = 1.0f, popOffset = Vector2.zero };

    [Header("--- GENERIC TARGET ---")]
    public SelectionIconSettings genericTargetIcon = new SelectionIconSettings();
    [Header("--- LINKED CARD (EQUIP/HOVER) ---")]
    public SelectionIconSettings linkedCardIcon = new SelectionIconSettings();
    [Header("--- CHAIN RESPONSE (ACTIVATE) ---")]
    [Tooltip("Opções visuais para as cartas que podem ser ativadas. Desligue 'useIcon' para usar apenas aura.")]
    public SelectionIconSettings chainActivationIcon = new SelectionIconSettings { useIcon = true, usePrefab = true, useNative = true, useOutline = true, outlineColor = Color.cyan, availableColor = new Color(0,1,1,0.5f), selectedColor = Color.cyan };
    [Header("--- EFFECT ACTIVATION (HOVER) ---")]
    [Tooltip("Opções visuais para cartas no campo que podem ativar efeito com 1 clique (Prefab de balão).")]
    public SelectionIconSettings effectActivationIcon = new SelectionIconSettings { useIcon = true, usePrefab = true, useNative = true, useOutline = false, availableColor = Color.green, selectedColor = Color.green, blinkAvailable = false, pulseAvailable = false, spinAvailable = false };
    public ConnectionLineSettings linkedCardLine = new ConnectionLineSettings();

    [Header("Opções de Corrente (Chain Link)")]
    public bool useChainLinkRoutine = true;
    public float chainLinkDuration = 1.0f;
    public float chainLinkScale = 1.2f;
    public Color chainLinkTextColor = new Color(1f, 0.8f, 0f, 1f); // Amarelo Dourado
    public Color chainLinkShadowColor = Color.black;
    [Tooltip("Sprite do elo vindo da esquerda.")]
    public Sprite chainLinkLeftSprite; 
    [Tooltip("Sprite do elo vindo da direita (se vazio, espelha o da esquerda).")]
    public Sprite chainLinkRightSprite; 
    [Tooltip("Sprite dos elos unidos após o impacto.")]
    public Sprite chainLinkJoinedSprite; 
    public Color chainLinkSpriteColor = Color.white;
    [Tooltip("Material Aditivo para ignorar o fundo preto dos sprites.")]
    public Material chainLinkMaterial;
    public bool useChainLinkPrefab = true;
    
    public bool chainLinkPreserveAspect = true;
    public Vector2 chainLinkSideSize = new Vector2(60, 60);
    public Vector2 chainLinkJoinedSize = new Vector2(90, 60);
    [Tooltip("Se ativo, as correntes deslizam para cima. Se inativo, ficam paradas sobre a carta.")]
    public bool chainLinkSlideUp = true;
    public bool chainLinkFlashOnImpact = true;
    public Color chainLinkFlashColor = Color.white;

    [Header("Posicionamento (Chain Link)")]
    public Vector2 chainLinkBaseOffset = Vector2.zero;
    public Vector2 chainLinkTextOffset = new Vector2(0, -25);

    [Header("Opções de Equipamento")]
    [Tooltip("Usa a animação do fantasma da magia voando até o monstro e o impacto de encaixe (Outline).")]
    public bool useEquipGhost = true;
    public float equipGhostFlightDuration = 0.5f; // Tempo do voo da carta
    public float equipGhostFlightScale = 1.3f; // O quanto a carta cresce no meio do ar (ex: 1.3 = +30%)
    public float equipSqueezeScale = 1.4f; // Tamanho inicial da sombra de impacto (Squeeze)
    public Color equipSqueezeColor = Color.black; // Cor da sombra de impacto
    [Tooltip("Instancia o prefab definido em 'Equip Impact VFX' ao equipar.")]
    public bool useEquipPrefab = true;

    [Header("Opções de Mudança de Controle (Change of Heart)")]
    [Tooltip("Usa a animação parabólica de voo para troca de controle.")]
    public bool useControlSwapRoutine = true;
    public float controlSwapDuration = 1.0f;
    public float controlSwapFlightScale = 1.5f; // O quanto a carta cresce no ar
    [Tooltip("Adiciona um contorno na carta durante o voo.")]
    public bool useControlSwapOutline = true;
    public Color controlSwapOutlineColor = new Color(0.8f, 0.2f, 0.9f, 1f); // Roxo
    [Tooltip("Deixa um rastro fantasma atrás da carta durante o voo.")]
    public bool useControlSwapTrail = true;
    public AttackTrailType controlSwapTrailType = AttackTrailType.Shadows;
    public float controlSwapTrailWidth = 15f;
    public Color controlSwapTrailColor = new Color(0.8f, 0.2f, 0.9f, 0.5f);
    [Tooltip("A carta faz um 'Squeeze' (amassa/impacta) ao aterrissar no novo lado.")]
    public bool useControlSwapImpact = true;
    public ControlSwapImpactType controlSwapImpactType = ControlSwapImpactType.Squeeze;
    public Color controlSwapImpactColor = new Color(0.8f, 0.2f, 0.9f, 1f);
    public float controlSwapImpactScale = 1.4f;
    public float controlSwapImpactOutlineWidth = 10f;
    [Tooltip("Instancia partículas padrões (Spell Activate no voo, Summon na aterrissagem).")]
    public bool useControlSwapPrefab = true;

    [Header("Opções de Embaralhamento (Shuffle)")]
    [Tooltip("Usa a animação matemática dividindo e misturando as cartas.")]
    public bool useShuffleRoutine = true;
    [Tooltip("Usa o embaralhamento Custom (Hindu Shuffle) com inclinação 2D.")]
    public bool useCustomHinduShuffle = false;
    [Tooltip("Usa o novo estilo de embaralhamento 2D rápido lateral.")]
    public bool useSimple2DShuffle = true;
    [Tooltip("Move as cartas para o centro da tela durante o embaralhamento.")]
    public bool shuffleAtCenter = false;
    public float shuffleDuration = 0.8f; // Tempo total da animação
    [Tooltip("Número de vezes que a animação de embaralhar se repete no mesmo gatilho.")]
    public int shuffleLoopCount = 1;
    [Tooltip("Instancia o prefab definido em 'Shuffle VFX'. Útil para usar sua Sprite Sheet Animada.")]
    public bool useShufflePrefab = true;
    [Tooltip("Tempo extra (segundos) ANTES do baralho reaparecer (após o loop/fumaça acabar).")]
    public float shuffleDeckAppearDelay = 0.0f;
    [Tooltip("Tempo extra (segundos) DEPOIS do baralho reaparecer, antes do GameManager iniciar a compra de cartas.")]
    public float shufflePostDelay = 0.3f;
    [Tooltip("Multiplicador de escala das cartas quando se movem para o centro.")]
    public float shuffleScaleMultiplier = 1.5f;
    [Tooltip("Deslocamento do embaralhamento 2D. X=Distância lateral, Y=Distância vertical.")]
    public Vector2 shuffle2DOffset = new Vector2(-80f, 40f);
    [Tooltip("Inclinação (ângulo Z) do baralho durante o Hindu Shuffle.")]
    public float customShuffleTiltAngle = 15f;
    [Tooltip("Rotaciona o baralho do oponente em 180º no embaralhamento para ficar virado para ele.")]
    public bool shuffleOpponent180 = true;

    [Header("Opções de Embaralhamento da Mão (Hand Shuffle)")]
    public bool useHandShuffleAnimation = true;
    public bool useOpponentHandShuffleAnimation = true;
    public HandShuffleType handShuffleType = HandShuffleType.CrossSwap;
    [Tooltip("Duração de CADA ciclo individual de embaralhamento.")]
    public float handShuffleDuration = 0.3f;
    [Tooltip("Número mínimo de vezes que as cartas vão se misturar.")]
    public int handShuffleMinCycles = 1;
    [Tooltip("Número máximo de vezes que as cartas vão se misturar.")]
    public int handShuffleMaxCycles = 3;

    [Header("Opções de Invocação de Ficha (Token)")]
    [Tooltip("A carta da Ficha (Token) treme, surge do tamanho 0 e faz um Fade In.")]
    public bool useTokenSummonRoutine = true;
    [Tooltip("A cor inicial da carta enquanto ela faz o Fade In (Padrão: Cinza/Escuro).")]
    public Color tokenSummonFadeColor = Color.gray; 
    [Tooltip("Instancia o prefab definido em 'Token Summon VFX'. Ideal para usar com a fumaça 2D.")]
    public bool useTokenSummonPrefab = true;

    [Header("Aparição da Ficha (Tempos e Escalas)")]
    [Tooltip("Tempo que a carta fica invisível esperando a fumaça/animação subir.")]
    public float tokenAppearDelay = 0.2f;
    [Tooltip("Duração do Fade In e do Crescimento da carta.")]
    public float tokenAppearDuration = 0.5f;
    [Tooltip("Escala inicial (0 = nasce do nada, 1 = tamanho normal).")]
    public float tokenStartScaleMult = 0.0f;
    [Tooltip("Escala final (1 = 100% do tamanho de campo).")]
    public float tokenEndScaleMult = 1.0f;

    [Header("Opções de Destruição (Explosão)")]
    [Tooltip("Cria um fantasma da carta para animar a destruição.")]
    public bool useDestructionRoutine = true;
    [Tooltip("Duração do efeito de destruição. (Padrão 0.8s combina perfeitamente com a Explosion2_2D a 30fps)")]
    public float destructionDuration = 0.8f; 
    [Tooltip("Instancia o prefab definido em 'Explosion VFX'.")]
    public bool useExplosionPrefab = true;
    [Tooltip("Intensidade do tremor da carta ao ser destruída.")]
    public float destructionShakeMagnitude = 8f;
    [Tooltip("A carta vai escurecendo (tostando) e desvanecendo (fade out) na explosão.")]
    public bool destructionFadeAndDarken = true;
    public Color destructionFadeColor = Color.black; // Cor da carta ao tostar
    [Tooltip("A carta espera a explosão chegar na metade do tempo antes de encolher e sumir.")]
    public bool destructionWaitHalfExplosion = true;
    [Tooltip("A carta encolhe conforme é destruída.")]
    public bool destructionShrink = true;

    [Header("Opções de Banimento (Banish)")]
    public bool useBanishRoutine = true;
    public bool useBanishPrefab = true;
    public float banishSpinSpeed = 720f;
    public float banishShrinkDuration = 0.5f;

    [Header("Opções de Flip (Revelação)")]
    [Tooltip("Tempo (segundos) que a carta leva para se espremer e esticar, simulando o giro 3D.")]
    public float flipAnimationDuration = 0.3f;
    [Tooltip("Usa a rotina nativa para adicionar um Pulse ao final do Flip.")]
    public bool useFlipRoutine = true;
    [Tooltip("Tempo de espera APÓS a carta virar, antes do brilho do Pulse acontecer.")]
    public float flipVfxDuration = 0.1f;
    public bool useFlipPulse = true;
    public float flipPulseScale = 1.2f;
    public float flipPulseDuration = 0.2f;
    public float flipPulseOutlineWidth = 8f;
    public Color flipPulseColor = Color.white;
    [Tooltip("Instancia o Prefab de Flip definido nos Slots visuais.")]
    public bool useFlipPrefab = true;

    [Header("Opções de Dano Direto (Life Points)")]
    public bool useDamageScreenShake = true;
    public float screenShakeDuration = 0.4f;
    public float screenShakeMagnitude = 20f;
    public bool useDamagePrefab = true;
    public Color damagePrefabColor = Color.white;

    [Header("Opções de Ricochete (Reflect / Attack Fail)")]
    [Tooltip("Se ativo, o atacante apenas sofre o tremor de ataque normal em vez da implosão.")]
    public bool useReflectAsStandardDamage = false;
    public bool useReflectRoutine = true;
    public float reflectPulseScale = 1.0f; // Padrão 1.0 = Não infla, apenas gera o brilho
    public float reflectPulseDuration = 0.3f;
    public Color reflectPulseColor = new Color(0.2f, 0.8f, 1f, 1f);
    public float reflectPulseOutlineWidth = 12f;
    public float reflectShakeDuration = 0.4f;
    public float reflectShakeMagnitude = 20f;
    public bool useReflectPrefab = true;
    public Color reflectPrefabColor = Color.white;

    [Header("Opções de Hit / Corte (Impacto)")]
    public bool useAttackImpactRoutine = false;
    public bool useAttackImpactPrefab = true;
    public bool applyAttackImpactColor = false;
    public Color attackImpactPrefabColor = Color.white;
    public Vector3 attackImpactPrefabScale = Vector3.one;
    [Tooltip("Rotação (Ângulo) no eixo Z do impacto (ex: 15, 30 para cortes diagonais).")]
    public float attackImpactRotationOffset = 0f;
    public bool overrideAttackImpactDuration = false;
    public float attackImpactPrefabDuration = 1.0f;
    public float attackImpactPulseScale = 1.2f;
    public float attackImpactPulseDuration = 0.2f;
    public Color attackImpactPulseColor = Color.red;
    public float hitShakeDuration = 0.3f;
    public float hitShakeMagnitude = 15f;

    [Header("Opções de Defesa com Sucesso (Defense Block)")]
    public bool useDefenseRoutine = true;
    public float defensePulseScale = 1.1f;
    public float defensePulseDuration = 0.3f;
    public Color defensePulseColor = new Color(0.8f, 0.9f, 1f, 1f);
    public float defensePulseOutlineWidth = 15f;
    public float defenseShakeDuration = 0.2f;
    public float defenseShakeMagnitude = 10f;
    public bool useDefensePrefab = true;
    public Color defensePrefabColor = Color.white;

    [Header("Escudo de Defesa (Nativo)")]
    public bool useNativeDefenseShield = false;
    public Sprite defenseShieldSprite;
    [Tooltip("Material Aditivo para ignorar o fundo preto da imagem.")]
    public Material defenseShieldMaterial;
    public Color defenseShieldColor = Color.white;
    public Vector2 defenseShieldSize = new Vector2(150, 150);
    public Vector2 defenseShieldOffset = Vector2.zero;
    public bool defenseShieldFlip180 = false;
    public float defenseShieldDuration = 0.5f;
    public float defenseShieldPulseScale = 1.3f;

    [Header("Opções de Ataque (Combate)")]
    public Color colorAttackReadyHover = new Color(1f, 0.5f, 0f, 1f); // Laranja (Pronto para Atacar)
    public Color colorAttackTargetHover = Color.red; // Vermelho (Alvo Inimigo)
    [Tooltip("Habilita o efeito de escurecer a carta ou pintar a borda ao selecioná-la para atacar.")]
    public bool enableAttackSelectionVisual = true;
    public Color colorAttackSelection = Color.red; // Cor do hover/borda de atacante selecionado
    [Tooltip("Se marcado, ataques diretos visam o avatar do oponente. Se desmarcado, visam o centro da mão/campo.")]
    public bool targetAvatarOnDirectAttack = true;
    [Tooltip("A carta dá um 'bote' (recua e avança) no momento do ataque.")]
    public bool useAttackHeadbutt = true;
    [Tooltip("Ajuste de rotação caso o seu Sprite de espada não aponte para cima (Ex: 0, 90, 180).")]
    public float attackSwordRotationOffset = -90f;
    public float attackFlightDuration = 0.3f; // Tempo de voo
    [Tooltip("Cria o rastro acompanhando a espada.")]
    public bool useAttackTrail = true;
    public AttackTrailType attackTrailType = AttackTrailType.Shadows;
    public float attackTrailWidth = 15f; // Grossura do feixe/rastro (se ContinuousLine)
    public Color attackTrailColor = new Color(1f, 0f, 0f, 0.5f);
    [Tooltip("Instancia o Prefab 'Attack VFX' ao acertar o alvo.")]
    public bool useAttackImpactVFX = true;

    [Header("Efeitos de Magia (Field & Equip)")]
    public GameObject equipImpactVFX;   // Efeito quando o fantasma entra no alvo

    [Header("Efeitos Sonoros (AudioClips)")]
    public AudioClip spellSound;
    public AudioClip trapSound;
    public AudioClip summonSound;
    public AudioClip tokenSummonSound;   // Som de fumaça (Poof)
    public AudioClip fusionSound;
    public AudioClip tributeSound;
    public AudioClip attackDeclareSound; // 1. Clique no monstro para atacar
    public AudioClip attackTravelSound;  // 2. Espada em movimento (Whoosh)
    public AudioClip attackImpactSound;  // 3. Impacto no alvo (Corte/Explosão)
    public AudioClip destroySound;
    public AudioClip reflectSound;
    public AudioClip banishSound;
    public AudioClip flipSound;
    public AudioClip damageSound;
    public AudioClip defenseSound;
    public AudioClip shuffleSound;
    public AudioClip monsterEffectSound;

    // Variáveis de BGM do Tema Atual
    private AudioClip currentBgmNormal;
    private AudioClip currentBgmTense;
    private AudioClip currentBgmWinning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Garante que existe um AudioSource para BGM se não foi atribuído
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
        }

        // Preenche os temas de Magia de Campo padrão caso a lista esteja vazia no Inspector
        if (fieldSpellThemes == null || fieldSpellThemes.Count == 0)
        {
            fieldSpellThemes = new List<FieldSpellTheme> {
                new FieldSpellTheme { themeName = "Água", keywords = new List<string> { "umi", "ocean", "umiiruka" }, overlayColor = Color.blue, intensity = 0.2f },
                new FieldSpellTheme { themeName = "Floresta", keywords = new List<string> { "forest", "hunting ground", "gaia" }, overlayColor = Color.green, intensity = 0.2f },
                new FieldSpellTheme { themeName = "Trevas", keywords = new List<string> { "yami", "dark", "plasma", "necrovalley", "pandemonium" }, overlayColor = new Color(0.6f, 0f, 0.8f, 1f), intensity = 0.25f },
                new FieldSpellTheme { themeName = "Terra/Montanha", keywords = new List<string> { "mountain", "sogen", "wasteland", "sanctuary" }, overlayColor = new Color(0.8f, 0.4f, 0f, 1f), intensity = 0.2f },
                new FieldSpellTheme { themeName = "Fogo", keywords = new List<string> { "molten", "fusion gate" }, overlayColor = Color.red, intensity = 0.15f },
                new FieldSpellTheme { themeName = "Luz/Céu", keywords = new List<string> { "luminous", "air current", "skyscraper", "array", "centrifugal" }, overlayColor = Color.cyan, intensity = 0.15f }
            };
        }
    }

    // Busca o Canvas mais próximo na hierarquia para garantir que a UI renderize!
    private Transform GetUIParent()
    {
        if (boardCenter != null)
        {
            Canvas canvas = boardCenter.GetComponentInParent<Canvas>();
            if (canvas != null) return canvas.transform;
        }
        
        Canvas[] allCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases) if (c.isRootCanvas && c.gameObject.name.Contains("Panel_Duel")) return c.transform;
        foreach (Canvas c in allCanvases) if (c.isRootCanvas) return c.transform;
            
        return null;
    }

    // --- ROTEADORES DE PACOTE DE INVOCAÇÃO ---

    public SelectionIconSettings GetIconSettings(HighlightCategory category)
    {
        switch(category) {
            case HighlightCategory.Tribute: return tributeSummonSettings.selectionIcon;
            case HighlightCategory.Fusion: return fusionSummonSettings.selectionIcon;
            case HighlightCategory.Ritual: return ritualSummonSettings.selectionIcon;
            case HighlightCategory.SpecialSummon: return specialSummonSettings.selectionIcon;
            case HighlightCategory.GenericTarget: return genericTargetIcon;
            case HighlightCategory.LinkedCard: return linkedCardIcon;
            case HighlightCategory.ChainResponse: return chainActivationIcon;
            case HighlightCategory.EffectActivation: return effectActivationIcon;
            default: return genericTargetIcon;
        }
    }

    public SummonVFXPackage GetSummonPackage(SummonVFXType type)
    {
        switch(type) {
            case SummonVFXType.Special: return specialSummonSettings;
            case SummonVFXType.Tribute: return tributeSummonSettings;
            case SummonVFXType.Fusion: return fusionSummonSettings;
            case SummonVFXType.Ritual: return ritualSummonSettings;
            default: return null;
        }
    }

    public SummonImpactSettings GetImpactSettings(SummonVFXType type)
    {
        if (type == SummonVFXType.Normal) return normalSummonSettings.impact;
        return GetSummonPackage(type)?.impact;
    }

    public void UpdateThemeFX(DuelTheme theme)
    {
        if (theme == null) return;
        
        if (theme.spellActivateVFX != null) spellActivateVFX = theme.spellActivateVFX;
        if (theme.trapActivateVFX != null) trapActivateVFX = theme.trapActivateVFX;
        if (theme.summonVFX != null) summonVFX = theme.summonVFX;
        // if (theme.tributeSummonVFX != null) tributeSummonVFX = theme.tributeSummonVFX; // Adicionar ao DuelTheme se desejar
        if (theme.fusionVFX != null) fusionVFX = theme.fusionVFX;
        if (theme.tributeVFX != null) tributeVFX = theme.tributeVFX;
        if (theme.attackVFX != null) attackVFX = theme.attackVFX;
        if (theme.explosionVFX != null) explosionVFX = theme.explosionVFX;
        if (theme.banishVFX != null) banishVFX = theme.banishVFX;
        if (theme.flipVFX != null) flipVFX = theme.flipVFX;

        // Atualiza referências de música
        currentBgmNormal = theme.bgmNormal;
        currentBgmTense = theme.bgmTense;
        currentBgmWinning = theme.bgmWinning;

        // Força atualização da música
        if (GameManager.Instance != null)
        {
            UpdateBGM(GameManager.Instance.playerLP, GameManager.Instance.opponentLP);
        }
    }

    public void UpdateBGM(int playerLP, int opponentLP)
    {
        if (bgmSource == null) return;

        AudioClip targetClip = currentBgmNormal;

        // Lógica de Tensão/Vitória
        if (playerLP > 0 && opponentLP > 0)
        {
            if (playerLP <= opponentLP * 0.5f && currentBgmTense != null)
                targetClip = currentBgmTense; // Tensa (metade dos pontos do oponente)
            else if (playerLP >= opponentLP * 2.0f && currentBgmWinning != null)
                targetClip = currentBgmWinning; // Empolgante (dobro dos pontos)
        }

        // Troca a música se for diferente da atual
        if (bgmSource.clip != targetClip)
        {
            bgmSource.clip = targetClip;
            bgmSource.Play();
        }
        // Garante que a música toque se estiver parada (ex: início de duelo)
        else if (bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    // --- UTILITÁRIOS ---

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public GameObject SpawnVFXPublic(GameObject prefab, Vector3 position, bool applyTint = false, Color tintColor = default, float overrideDuration = -1f)
    {
        if (prefab != null)
        {
            // Debug.Log($"[VFX] [SPAWN] Instanciando Prefab");
            GameObject instance = Instantiate(prefab);

            // Se for interface 2D, prende no Canvas. Se for Partícula 3D, mantém no World Space!
            if (instance.GetComponent<RectTransform>() != null)
            {
                Transform uiParent = GetUIParent();
                if (uiParent != null)
                {
                    instance.transform.SetParent(uiParent, true); // true para manter a posição no World Space original
                    instance.transform.position = position;
                    instance.transform.SetAsLastSibling();
                }
                else instance.transform.position = position;
            }
            else
            {
                instance.transform.position = position;
            }
            
            // FIX: Força Sistemas de Partículas 3D a renderizarem por CIMA do Canvas (Interface)!
            ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Transform rootUiParent = GetUIParent();
            Canvas rootCanvas = rootUiParent != null ? rootUiParent.GetComponentInParent<Canvas>() : null;
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder + 30000 : 30000; // Valor bem alto
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
            
            if (applyTint)
            {
                foreach (var img in instance.GetComponentsInChildren<Image>(true)) img.color = tintColor;
                foreach (var ri in instance.GetComponentsInChildren<RawImage>(true)) ri.color = tintColor;
                foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = tintColor;
                foreach (var pSys in instance.GetComponentsInChildren<ParticleSystem>(true)) {
                    var main = pSys.main; 
                    main.startColor = tintColor;
                }
            }

            // FIX: Lê a duração real do próprio sistema de partículas para a destruição
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (overrideDuration > 0)
            {
                Destroy(instance, overrideDuration);
            }
            else if (ps != null)
            {
                Destroy(instance, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
            }
            else
            {
                Destroy(instance, 5.0f); // Fallback para objetos sem ParticleSystem
            }
            
            return instance;
        }
        return null;
    }
}
