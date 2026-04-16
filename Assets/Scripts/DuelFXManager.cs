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

public class DuelFXManager : MonoBehaviour
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
    public CardFlightSettings flightDeckToHand = new CardFlightSettings { enableFlight = true, duration = 0.3f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.8f, 0.2f, 0.5f) };
    public CardFlightSettings flightDeckToField = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 1f, 0.8f, 0.5f) };
    public CardFlightSettings flightDeckToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightDeckToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightPileToHand = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.8f, 0f, 0.5f) };
    public CardFlightSettings flightPileToDeck = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(1f, 0.5f, 0f, 0.5f) };
    public CardFlightSettings flightPileToField = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.8f, 0.2f, 1f, 0.5f) };
    public CardFlightSettings flightBanishToField = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.1f, 0.1f, 0.1f, 0.5f), useImpact = false };
    public CardFlightSettings flightGraveyardToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightBanishToDeck = new CardFlightSettings { enableFlight = true, duration = 0.5f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.Shadows, trailColor = new Color(0.0f, 0.0f, 0.0f, 0.4f), useImpact = false };
    public CardFlightSettings flightExtraToGraveyard = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };
    public CardFlightSettings flightExtraToBanished = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailType = AttackTrailType.SmoothShadows, trailColor = new Color(0f, 0f, 0f, 0.5f), useImpact = false };    
    public CardFlightSettings flightBanishedToAny = new CardFlightSettings { enableFlight = true, duration = 0.4f, flightScale = 1.3f, useTrail = true, trailColor = new Color(0.2f, 0.2f, 0.2f, 0.8f) };

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

    // --- ÍCONE DE SELEÇÃO E TRIBUTO ---

    private Dictionary<CardDisplay, Dictionary<HighlightCategory, GameObject>> activeSelectionIcons = new Dictionary<CardDisplay, Dictionary<HighlightCategory, GameObject>>();

    public void SetSelectionIcon(CardDisplay card, SummonVFXType type, SelectionState state)
    {
        if (card == null || !enableAnimations) return;

        SummonVFXPackage package = GetSummonPackage(type);
        if (package == null || !package.selectionIcon.useIcon) return;
        SelectionIconSettings settings = package.selectionIcon;

        HighlightCategory cat = HighlightCategory.GenericTarget;
        if (type == SummonVFXType.Tribute) cat = HighlightCategory.Tribute;
        else if (type == SummonVFXType.Fusion) cat = HighlightCategory.Fusion;
        else if (type == SummonVFXType.Ritual) cat = HighlightCategory.Ritual;
        else if (type == SummonVFXType.Special) cat = HighlightCategory.SpecialSummon;

        ApplySelectionIcon(card, cat, settings, state);
    }

    public void SetSelectionIcon(CardDisplay card, HighlightCategory category, SelectionState state)
    {
        if (card == null || !enableAnimations) return;
        SelectionIconSettings settings = GetIconSettings(category);
        if (settings == null || !settings.useIcon) return;

        ApplySelectionIcon(card, category, settings, state);
    }

    private void ApplySelectionIcon(CardDisplay card, HighlightCategory category, SelectionIconSettings settings, SelectionState state)
    {
        if (!activeSelectionIcons.ContainsKey(card))
            activeSelectionIcons[card] = new Dictionary<HighlightCategory, GameObject>();

        if (activeSelectionIcons[card].TryGetValue(category, out GameObject existingIcon))
        {
            if (existingIcon != null) Destroy(existingIcon);
            activeSelectionIcons[card].Remove(category);
        }

        if (state == SelectionState.None) return;
        if (state == SelectionState.Available && !settings.showAvailableState) return;

        GameObject newIcon = null;

        if (settings.usePrefab && settings.prefab != null)
        {
            GameObject prefabInstance = SpawnVFXPublic(settings.prefab, card.transform.position);
            if (prefabInstance != null) {
                prefabInstance.transform.SetParent(card.transform, true);
                newIcon = prefabInstance;
            }
        }
        else if (settings.useNative && settings.sprite != null)
        {
            GameObject iconObj = new GameObject("SelectionIcon_" + category.ToString(), typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(card.transform, false);
            iconObj.transform.SetAsLastSibling();
            
            RectTransform rt = iconObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            
            Image img = iconObj.GetComponent<Image>();
            img.sprite = settings.sprite;
            if (settings.material != null) img.material = settings.material;

            newIcon = iconObj;

            if (state == SelectionState.Available)
            {
                rt.sizeDelta = new Vector2(settings.size, settings.size);
                img.color = settings.availableColor;

                if (settings.useOutline)
                {
                    Outline outline = iconObj.AddComponent<Outline>();
                    outline.effectColor = settings.outlineColor;
                    outline.effectDistance = new Vector2(settings.outlineWidth, -settings.outlineWidth);
                }

                StartCoroutine(AnimateSelectionIcon(iconObj, img, rt, settings, true));
            }
            else if (state == SelectionState.Selected)
            {
                rt.sizeDelta = new Vector2(settings.size, settings.size);
                img.color = settings.selectedColor;
                
                Outline outline = iconObj.AddComponent<Outline>();
                outline.effectColor = settings.useOutline ? settings.outlineColor : settings.selectedColor;
                outline.effectDistance = new Vector2(settings.selectedOutlineWidth, -settings.selectedOutlineWidth);
                
                StartCoroutine(AnimateSelectionIcon(iconObj, img, rt, settings, false));
            }
        }

        if (newIcon != null)
        {
            activeSelectionIcons[card][category] = newIcon;
        }
    }

    private IEnumerator AnimateSelectionIcon(GameObject iconObj, Image img, RectTransform rt, SelectionIconSettings settings, bool isAvailableState)
    {
        float t = 0;
        Color baseColor = img.color;
        Outline outline = iconObj.GetComponent<Outline>();
        Color baseOutlineColor = outline != null ? outline.effectColor : Color.clear;
        Vector3 baseScale = rt.localScale;

        // Determina quais animações usar baseado no estado (Disponível vs Selecionado)
        bool shouldBlink = isAvailableState ? settings.blinkAvailable : settings.blinkSelected;
        bool shouldPulse = isAvailableState ? settings.pulseAvailable : settings.pulseSelected;
        bool shouldSpin = isAvailableState ? settings.spinAvailable : settings.spinSelected;

        while (iconObj != null)
        {
            t += Time.deltaTime;

            // Lógica de Piscar (Blink)
            if (shouldBlink)
            {
                float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI * settings.blinkFrequency));
                Color c = baseColor; c.a = alpha * baseColor.a;
                img.color = c;
                if (outline != null) {
                    Color oc = baseOutlineColor; oc.a = alpha * baseOutlineColor.a;
                    outline.effectColor = oc;
                }
            }

            // Lógica de Pulsar (Pulse)
            if (shouldPulse)
            {
                float scaleMultiplier = 1f + (Mathf.Sin(t * Mathf.PI * settings.blinkFrequency) + 1f) / 2f * (settings.pulseScale - 1f);
                rt.localScale = baseScale * scaleMultiplier;
            }

            // Lógica de Girar (Spin)
            if (shouldSpin) rt.Rotate(0, 0, settings.spinSpeed * Time.deltaTime);

            yield return null;
        }
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

    // --- ATIVAÇÃO DE MAGIA / ARMADILHA ---

    public void PlayCardActivation(CardDisplay card, bool isTrap, System.Action onComplete = null)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayCardActivation | Alvo: {card?.CurrentCardData?.name} | Tipo: {(isTrap ? "Trap" : "Spell")} | Momento: Carta ativada (entrando na corrente).");
        if (!enableAnimations)
        {
            onComplete?.Invoke();
            return;
        }
        
        bool isFieldSpell = card != null && card.CurrentCardData != null && card.CurrentCardData.property == "Field";
        StartCoroutine(AnimateActivationRoutine(card, isTrap, isFieldSpell, onComplete));
    }

    private IEnumerator AnimateActivationRoutine(CardDisplay card, bool isTrap, bool isFieldSpell, System.Action onComplete)
    {
        Debug.Log($"[VFX] ⚙ [ROUTINE INICIADA] AnimateActivationRoutine | Alvo: {card?.CurrentCardData?.name}");
        PlaySound(isTrap ? trapSound : spellSound);

        // 1. Instancia a Partícula Clássica (Se a opção estiver marcada)
        if (useActivationPrefab)
        {
            GameObject vfxPrefab = isTrap ? trapActivateVFX : spellActivateVFX;
            SpawnVFXPublic(vfxPrefab, card.transform.position);
        }
        
        // 2. Executa o Pulse Dinâmico e a Cor do Field Spell
        if (useActivationPulse && card != null)
        {
            Vector3 originalScale = card.transform.localScale;
            if (GameManager.Instance != null)
            {
                originalScale = card.isOnField ? GameManager.Instance.fieldCardScale : GameManager.Instance.handCardScale;
            }
            Vector3 peakScale = originalScale * activationPulseScale;
            Transform originalParent = card.transform.parent;
            int originalIndex = card.transform.GetSiblingIndex();

            card.transform.SetAsLastSibling(); // Traz para frente na UI

            GameObject ghost = new GameObject("ActivationGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
            if (originalParent != null) {
                ghost.transform.SetParent(originalParent, false);
                ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 
            }

            RectTransform rt = ghost.GetComponent<RectTransform>();
            RectTransform cardRt = card.GetComponent<RectTransform>();
            rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
            rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
            rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

            RawImage ri = ghost.GetComponent<RawImage>();
            ri.texture = card.cardImage.texture; ri.color = new Color(1f, 1f, 1f, 0.8f);
            
            Outline outline = ghost.GetComponent<Outline>();
            outline.effectColor = isTrap ? colorTrap : colorSpell;
            outline.effectDistance = new Vector2(activationPulseOutlineWidth, -activationPulseOutlineWidth);

            float t = 0;
            float halfDuration = activationPulseDuration / (animationSpeed > 0 ? animationSpeed : 1f);

            // Fase 1: Subida
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime / halfDuration;
                float smooth = Mathf.SmoothStep(0, 1, t);
                card.transform.localScale = Vector3.Lerp(originalScale, peakScale, smooth);
                if (ghost != null)
                {
                    ghost.transform.localScale = Vector3.Lerp(originalScale, peakScale * 1.15f, smooth);
                    ri.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, smooth)); 
                }
                yield return null;
            }

            if (ghost != null) Destroy(ghost);

            if (isFieldSpell && colorizeBoardByFieldSpell && boardBackgroundImage != null)
            {
                Color targetColor = GetFieldSpellColor(card.CurrentCardData.name);
                StartCoroutine(TransitionBoardColorRoutine(boardBackgroundImage.color, targetColor, 0.6f));
            }

            // Fase 2: Descida
            t = 0;
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime / halfDuration;
                float smooth = Mathf.SmoothStep(0, 1, t);
                card.transform.localScale = Vector3.Lerp(peakScale, originalScale, smooth);
                yield return null;
            }

            if (card != null)
            {
                card.transform.localScale = originalScale;
                if (originalParent != null) { card.transform.SetParent(originalParent, true); card.transform.SetSiblingIndex(originalIndex); }
            }
        }
        else
        {
            // Se o Pulse estiver desativado, processa a Field Spell imediatamente e dá um pequeno delay visual
            if (isFieldSpell && colorizeBoardByFieldSpell && boardBackgroundImage != null)
            {
                Color targetColor = GetFieldSpellColor(card.CurrentCardData.name);
                StartCoroutine(TransitionBoardColorRoutine(boardBackgroundImage.color, targetColor, 0.6f));
            }
            
            if (useActivationPrefab) 
                yield return new WaitForSeconds(0.6f / (animationSpeed > 0 ? animationSpeed : 1f));
        }

        Debug.Log($"[VFX] ✔ [ROUTINE CONCLUÍDA] AnimateActivationRoutine.");
        onComplete?.Invoke();
    }

    private Color GetFieldSpellColor(string cardName)
    {
        cardName = cardName.ToLower();
        foreach (var theme in fieldSpellThemes)
        {
            foreach (var kw in theme.keywords) { 
                if (cardName.Contains(kw.ToLower())) return Color.Lerp(defaultBoardColor, theme.overlayColor, theme.intensity); 
            }
        }
        return defaultBoardColor;
    }

    private IEnumerator TransitionBoardColorRoutine(Color startColor, Color endColor, float duration)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            if (boardBackgroundImage != null)
                boardBackgroundImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }

    // --- BATALHA ---

    public void PlayAttack(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayAttack | Atacante: {attacker?.CurrentCardData?.name} -> Alvo: {target?.CurrentCardData?.name} | Momento: Espada voando em direção ao alvo.");
        if (!enableAnimations)
        {
            onHit?.Invoke();
            return;
        }

        StartCoroutine(AttackRoutine(attacker, target, onHit));
    }

    private IEnumerator AttackRoutine(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target != null ? target.transform.position : GetDirectAttackTargetPublic(attacker, startPos + new Vector3(0, 200, 0));
        
        PlaySound(attackDeclareSound);
        float animSpeed = animationSpeed > 0 ? animationSpeed : 1f;

        int originalIndex = attacker.transform.GetSiblingIndex();
        attacker.transform.SetAsLastSibling();

        // 1. Animação da Carta (Cabeçada / Bote)
        if (useAttackHeadbutt && attacker != null)
        {
            Vector3 dir = targetPos - startPos;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
            dir.Normalize();
            
            Vector3 anticipationPos = startPos - dir * 15f; 
            
            float t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.15f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(startPos, anticipationPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            
            t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.1f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(anticipationPos, startPos + dir * 30f, t);
                yield return null;
            }
            
            t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.1f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(startPos + dir * 30f, startPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            if (attacker != null) attacker.transform.position = startPos;
        }

        // 2. Viagem da Espada ou Projétil
        if (flightSwordIndicator.useNative && flightSwordIndicator.sprite != null)
        {
            PlaySound(attackTravelSound);
            
            GameObject proj = new GameObject("AttackFlightSword", typeof(RectTransform), typeof(Image));
            Transform uiParent = GetUIParent();
            if (uiParent != null) proj.transform.SetParent(uiParent, true);
            
            RectTransform rt = proj.GetComponent<RectTransform>();
            rt.position = startPos; rt.sizeDelta = flightSwordIndicator.size;
            
            Image img = proj.GetComponent<Image>();
            img.color = flightSwordIndicator.color;
            img.sprite = flightSwordIndicator.sprite;
            img.preserveAspect = flightSwordIndicator.preserveAspect;
            if (flightSwordIndicator.material != null) img.material = flightSwordIndicator.material;

            if (flightSwordIndicator.useOutline)
            {
                Outline outline = proj.AddComponent<Outline>();
                outline.effectColor = flightSwordIndicator.outlineColor;
                outline.effectDistance = new Vector2(flightSwordIndicator.outlineWidth, -flightSwordIndicator.outlineWidth);
            }

            Vector3 dir = targetPos - startPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.rotation = Quaternion.Euler(0, 0, angle + attackSwordRotationOffset); 
            
            GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
            if (useAttackTrail && attackTrailType == AttackTrailType.ContinuousLine) {
                lineObj = new GameObject("AttackLineTrail", typeof(RectTransform), typeof(Image));
                lineObj.transform.SetParent(proj.transform.parent, true);
                lineObj.transform.SetSiblingIndex(proj.transform.GetSiblingIndex());
                lineRT = lineObj.GetComponent<RectTransform>();
                lineRT.pivot = new Vector2(0, 0.5f); 
                lineRT.position = startPos;
                lineImg = lineObj.GetComponent<Image>(); lineImg.color = attackTrailColor;
                if (flightSwordIndicator.material != null) lineImg.material = flightSwordIndicator.material;
            }

            float duration = attackFlightDuration / animSpeed; float t = 0; float spawnTrailTimer = 0;
            
            while (t < 1f) {
                if (proj == null || rt == null) break;
                t += Time.deltaTime / duration;
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                rt.position = currentPos;
                
                if (useAttackTrail) {
                    if (attackTrailType == AttackTrailType.Shadows && flightSwordIndicator.sprite != null) {
                        spawnTrailTimer -= Time.deltaTime;
                        if (spawnTrailTimer <= 0) {
                            spawnTrailTimer = 0.015f; 
                            SpawnSwordGhost(rt, img.sprite, flightSwordIndicator.preserveAspect, flightSwordIndicator.material);
                        }
                    } else if (attackTrailType == AttackTrailType.ContinuousLine && lineObj != null) {
                        Vector3 dirToCurrent = currentPos - startPos;
                        float dist = dirToCurrent.magnitude;
                        float canvasScale = lineObj.transform.lossyScale.x;
                        if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, attackTrailWidth);
                        lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                    }
                }
                yield return null;
            }
            if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));
            Destroy(proj);
        }
        else if (flightSwordIndicator.usePrefab && (flightSwordIndicator.prefab != null || attackProjectileVFX != null))
        {
            PlaySound(attackTravelSound);
            GameObject prefabToUse = flightSwordIndicator.prefab != null ? flightSwordIndicator.prefab : attackProjectileVFX;
            GameObject proj = SpawnVFXPublic(prefabToUse, startPos);
            if (proj != null) {
                RectTransform rt = proj.GetComponent<RectTransform>();
                Image img = proj.GetComponentInChildren<Image>();
                
                Vector3 dir = targetPos - startPos;
                if (rt != null) {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    rt.rotation = Quaternion.Euler(0, 0, angle + attackSwordRotationOffset); 
                }

                float duration = 0.3f / animSpeed; float t = 0; float spawnTrailTimer = 0;
                while (t < 1f) {
                    if (proj == null) break;
                    t += Time.deltaTime / duration;
                    
                    if (rt != null) rt.position = Vector3.Lerp(startPos, targetPos, t);
                    else proj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    
                    if (useAttackTrail && rt != null && img != null && img.sprite != null) {
                        spawnTrailTimer -= Time.deltaTime;
                        if (spawnTrailTimer <= 0) {
                            spawnTrailTimer = 0.015f;
                            SpawnSwordGhost(rt, img.sprite, img.preserveAspect);
                        }
                    }
                    yield return null;
                }
                Destroy(proj);
            }
        }
        else { yield return new WaitForSeconds(0.2f / animSpeed); }

        attacker.transform.SetSiblingIndex(originalIndex);

        // 3. Impacto no Alvo
        PlaySound(attackImpactSound);
        PlayHitEffect(target);
        
        yield return null; 
        Debug.Log($"[VFX] ✔ [ROUTINE CONCLUÍDA] AttackRoutine finalizada.");
        onHit?.Invoke();
    }

    private void SpawnSwordGhost(RectTransform sourceRT, Sprite sprite, bool preserveAspect, Material mat = null)
    {
        GameObject ghost = new GameObject("SwordGhost", typeof(RectTransform), typeof(Image));
        ghost.transform.SetParent(sourceRT.parent, true);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        
        Image img = ghost.GetComponent<Image>();
        img.sprite = sprite; img.color = attackTrailColor;
        img.preserveAspect = preserveAspect;
        if (mat != null) img.material = mat;
        
        StartCoroutine(FadeAndDestroyGhost(ghost, img, 0.2f));
    }

    private IEnumerator FadeAndDestroyGhost(GameObject obj, Image img, float duration)
    {
        float t = 0; Color startColor = img.color;
        Vector3 startScale = obj.transform.localScale;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            // Afina o rastro levemente para criar o efeito "Swoosh" de golpe de espada!
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.6f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator FadeAndDestroyLine(GameObject obj, Image img, float duration)
    {
        float t = 0; Color startColor = img.color;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    public void PlayDestruction(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayDestruction | Alvo: {card?.CurrentCardData?.name} | Momento: Após cálculo de dano / Monstro derrotado.");
        if (!enableAnimations || card == null) return;
        PlaySound(destroySound);
        
        if (useDestructionRoutine)
            AnimateCardDeath(card, false); // Morte Explosiva (Fantasmas)
            
        // Instancia o VFX por ÚLTIMO para garantir que a explosão cubra a carta no Canvas 2D
        if (useExplosionPrefab && explosionVFX != null)
            SpawnVFXPublic(explosionVFX, card.transform.position);
    }

    // --- INVOCAÇÃO / TRIBUTO ---

    public void PlaySummonImpact(CardDisplay card, SummonVFXType type = SummonVFXType.Normal)
    {
        if (!enableAnimations || card == null) return;
        StartCoroutine(SummonImpactRoutine(card, type));
    }

    private IEnumerator SummonImpactRoutine(CardDisplay card, SummonVFXType type)
    {
        SummonVFXPackage package = GetSummonPackage(type);
        bool hasMarker = package != null && package.fieldMarker.useMarker;
        SummonImpactSettings settings = GetImpactSettings(type);
        
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

        if (hasMarker)
        {
            cg.alpha = 0f;
            Transform zone = card.transform.parent != null ? card.transform.parent : card.transform;
            SpawnFieldMarker(zone, type);
            
            float waitTime = settings != null ? settings.delayBeforeImpact : 0.3f;
            yield return new WaitForSeconds(waitTime / (animationSpeed > 0 ? animationSpeed : 1f));
            cg.alpha = 1f;
        }

        PlaySound(summonSound); 
        
        if (settings != null && settings.useImpact)
        {
            GameObject prefabToUse = settings.prefab != null ? settings.prefab : summonVFX; 

            if (settings.usePrefab && prefabToUse != null)
                SpawnVFXPublic(prefabToUse, card.transform.position);

            if (settings.useNativePulse)
                StartCoroutine(PulseGhostRoutine(card, settings.pulseScale, settings.pulseDuration, settings.pulseColor, settings.pulseOutlineWidth));
        }
    }

    // Wrapper de compatibilidade retroativa
    public void PlaySummonEffect(CardDisplay card) => PlaySummonImpact(card, SummonVFXType.Normal);

    public void PlayTokenSummonEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayTokenSummonEffect (Fumaça) | Alvo: {card?.CurrentCardData?.name} | Momento: Ficha (Token) gerada no campo.");
        if (!enableAnimations || card == null) return;
        StartCoroutine(TokenSummonRoutine(card));
    }

    private IEnumerator TokenSummonRoutine(CardDisplay card)
    {
        PlaySound(tokenSummonSound != null ? tokenSummonSound : summonSound);

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

        Vector3 originalPos = card.transform.position;
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;

        // Sempre esconde e encolhe inicialmente, não importa se é nativo ou prefab
        cg.alpha = 0f;
        card.transform.localScale = targetScale * tokenStartScaleMult;

        // Instancia a sua fumaça (Dust_2D)
        if (useTokenSummonPrefab)
        {
            GameObject prefab = tokenSummonVFX != null ? tokenSummonVFX : summonVFX;
            if (prefab != null) SpawnVFXPublic(prefab, card.transform.position);
        }

        // Animação de Surgimento (Controlada pelo Inspector)
        float delay = tokenAppearDelay / (animationSpeed > 0 ? animationSpeed : 1f);
        if (delay > 0) yield return new WaitForSeconds(delay);

        float duration = tokenAppearDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        // Controle de cor para evitar o "Flash Branco" e materializar suavemente das sombras
        RawImage ri = card.cardImage;
        Color targetColor = Color.white;
        if (ri != null) { targetColor = ri.color; ri.color = tokenSummonFadeColor; }

        while (t < 1f)
        {
            if (card == null) yield break;
            t += Time.deltaTime / duration;
            float p = Mathf.SmoothStep(0, 1, t);
            
            // Apenas treme se o Nativo estiver ativado
            if (useTokenSummonRoutine)
            {
                float shake = Mathf.Lerp(6f, 0f, p); 
                card.transform.position = originalPos + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0);
            }
            
            cg.alpha = p; 
            card.transform.localScale = Vector3.Lerp(targetScale * tokenStartScaleMult, targetScale * tokenEndScaleMult, p);
            if (ri != null) ri.color = Color.Lerp(tokenSummonFadeColor, targetColor, p);
            
            yield return null;
        }
        if (card != null) { 
            card.transform.position = originalPos; 
            card.transform.localScale = targetScale * tokenEndScaleMult;
            cg.alpha = 1f; 
            if (ri != null) ri.color = targetColor; 
        }
    }

    public void PlayTributeSummonEffect(CardDisplay card)
    {
        PlaySummonImpact(card, SummonVFXType.Tribute);
    }

    public void PlayFusionEffect(CardDisplay card)
    {
        PlaySummonImpact(card, SummonVFXType.Fusion);
    }

    public void PlayTributeEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayTributeEffect (Alma) | Alvo (Sacrifício): {card?.CurrentCardData?.name} | Momento: Selecionado/Enviado ao GY como custo de Tributo.");
        if (!enableAnimations || card == null) return;
        PlaySound(tributeSound);
        SetSelectionIcon(card, SummonVFXType.Tribute, SelectionState.Selected);
        if (tributeVFX != null) { GameObject vfx = SpawnVFXPublic(tributeVFX, card.transform.position); if (vfx != null) vfx.transform.SetParent(card.transform); }
    }

    // --- NOVOS EFEITOS DE BATALHA E JOGO ---

    public void PlayBanishEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayBanishEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Carta Removida de Jogo (Banida).");
        if (!enableAnimations || card == null) return;
        PlaySound(banishSound);
        
        if (useBanishRoutine) AnimateCardDeath(card, true); // Morte por Sugador (Fantasmas)
        if (useBanishPrefab && banishVFX != null)
            SpawnVFXPublic(banishVFX, card.transform.position);
    }

    private void AnimateCardDeath(CardDisplay card, bool isBanish)
    {
        if (card == null) return;
        Debug.Log($"[VFX] ⚙ [ROUTINE INICIADA] AnimateCardDeath | Alvo: {card.CurrentCardData?.name} | Tipo: {(isBanish ? "Banimento" : "Destruição")}");
        
        Transform uiParent = GetUIParent();
        if (uiParent == null) return;
        
        // Cria um clone visual perfeito (Fantasma) antes da carta original ser deletada pelo jogo
        GameObject ghost = new GameObject("CardGhost", typeof(RectTransform), typeof(RawImage));
        
        // FIX: Clona a hierarquia exata primeiro para herdar as âncoras e a escala (ex: 0.8) da carta original!
        Transform originalParent = card.transform.parent;
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
        }
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform cardRt = card.GetComponent<RectTransform>();
        rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
        rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
        rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture;
        ri.color = card.cardImage.color;
        
        if (uiParent != null) { ghost.transform.SetParent(uiParent, true); ghost.transform.SetAsLastSibling(); }
        
        if (isBanish) StartCoroutine(BanishGhostRoutine(ghost));
        else StartCoroutine(DestroyGhostRoutine(ghost));
    }

    private IEnumerator BanishGhostRoutine(GameObject ghost)
    {
        float duration = banishShrinkDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0; 
        Vector3 startScale = ghost.transform.localScale; 
        RawImage ri = ghost.GetComponent<RawImage>();
        while(t < 1f) {
            if (ghost == null) yield break;
            t += Time.deltaTime / duration;
            ghost.transform.Rotate(0, 0, banishSpinSpeed * Time.deltaTime); // Lê do Inspector
            ghost.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (ri != null) ri.color = new Color(0.5f, 0.5f, 0.5f, 1f - t); 
            yield return null;
        } Destroy(ghost);
    }

    private IEnumerator DestroyGhostRoutine(GameObject ghost)
    {
        float totalDuration = destructionDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float waitDuration = destructionWaitHalfExplosion ? totalDuration * 0.5f : 0f;
        float fadeDuration = totalDuration - waitDuration;
        
        RawImage ri = ghost.GetComponent<RawImage>(); 
        Vector3 startPos = ghost.transform.position;
        Vector3 startScale = ghost.transform.localScale;

        // Fase 1: Espera a explosão (Apenas Treme)
        float t = 0;
        while (t < waitDuration)
        {
            if (ghost == null) yield break;
            t += Time.deltaTime;
            
            if (destructionShakeMagnitude > 0f)
            {
                float shake = Mathf.Lerp(2f, destructionShakeMagnitude, t / waitDuration); 
                ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0); 
            }
            yield return null;
        }
        
        // Fase 2: Tosta e Desaparece Encolhendo
        t = 0;
        while (t < fadeDuration) 
        {
            if (ghost == null) yield break;
            t += Time.deltaTime;
            float p = t / fadeDuration;
            
            if (destructionShakeMagnitude > 0f)
            {
                float shake = Mathf.Lerp(destructionShakeMagnitude, 0f, p); 
                ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0); 
            }

            if (destructionFadeAndDarken)
            {
                Color targetColor = destructionFadeColor;
                targetColor.a = Mathf.Lerp(1f, 0f, p);
                ri.color = Color.Lerp(Color.white, targetColor, p);
            }
            
            if (destructionShrink)
            {
                ghost.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, p); // Encolhe mais drástico
            }
            
            yield return null;
        } 
        Destroy(ghost); // Some instantaneamente no ápice do fogo!
    }

    public void PlayFlipEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayFlipEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Carta virada para cima ou setada (Face-Down).");
        if (!enableAnimations || card == null) return;
        PlaySound(flipSound);
        if (useFlipPrefab && flipVFX != null) SpawnVFXPublic(flipVFX, card.transform.position);
        if (useFlipRoutine) StartCoroutine(FlipVFXRoutine(card));
    }

    private IEnumerator FlipVFXRoutine(CardDisplay card)
    {
        yield return new WaitForSeconds(flipVfxDuration / (animationSpeed > 0 ? animationSpeed : 1f));
        if (useFlipPulse && card != null) {
            StartCoroutine(PulseGhostRoutine(card, flipPulseScale, flipPulseDuration, flipPulseColor, flipPulseOutlineWidth));
        }
    }

    public void PlayDamageEffect(Vector3 position)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayDamageEffect (Tremor de Tela) | Momento: Redução nos Life Points confirmada.");
        if (!enableAnimations) return;
        PlaySound(damageSound);
        
        if (useDamagePrefab && damageVFX != null)
            SpawnVFXPublic(damageVFX, position, damagePrefabColor != Color.white, damagePrefabColor);

        if (useDamageScreenShake)
        {
            Transform canvas = GetUIParent();
            if (canvas != null) StartCoroutine(ShakeRoutine(canvas, screenShakeDuration, screenShakeMagnitude));
        }
    }

    public void PlayAttackFail(CardDisplay attacker)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayAttackFail | Alvo (Atacante): {attacker?.CurrentCardData?.name} | Momento: ATK <= DEF do alvo (Ricochete).");
        if (!enableAnimations || attacker == null) return;
        PlaySound(reflectSound);
        
        if (useReflectPrefab && reflectVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(reflectVFX, attacker.transform.position, reflectPrefabColor != Color.white, reflectPrefabColor);
            if (vfx != null)
            {
                vfx.transform.SetParent(attacker.transform, true);
                vfx.transform.SetAsLastSibling();
            }
        }

        if (useReflectAsStandardDamage)
        {
            StartCoroutine(ShakeRoutine(attacker.transform, reflectShakeDuration, reflectShakeMagnitude));
        }
        else if (useReflectRoutine)
        {
            StartCoroutine(PulseGhostRoutine(attacker, reflectPulseScale, reflectPulseDuration, reflectPulseColor, reflectPulseOutlineWidth, true));
            StartCoroutine(ShakeRoutine(attacker.transform, reflectShakeDuration, reflectShakeMagnitude));
        }
    }

    public void PlayHitEffect(CardDisplay target)
    {
        if (target == null || !enableAnimations) return;
        
        if (useAttackImpactPrefab && attackVFX != null)
        {
            GameObject impact = SpawnVFXPublic(attackVFX, target.transform.position, applyAttackImpactColor, attackImpactPrefabColor, overrideAttackImpactDuration ? attackImpactPrefabDuration : -1f);
            if (impact != null)
            {
                if (attackImpactRotationOffset != 0f)
                    impact.transform.Rotate(0, 0, attackImpactRotationOffset);
                
                impact.transform.localScale = attackImpactPrefabScale;
            }
        }

        if (useAttackImpactRoutine)
        {
            StartCoroutine(PulseGhostRoutine(target, attackImpactPulseScale, attackImpactPulseDuration, attackImpactPulseColor, 8f));
        }

        StartCoroutine(ShakeRoutine(target.transform, hitShakeDuration, hitShakeMagnitude));
    }

    public void PlayDefenseSuccessEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayDefenseSuccessEffect (Escudo) | Alvo (Defensor): {card?.CurrentCardData?.name} | Momento: Sobreviveu ao ataque.");
        if (!enableAnimations || card == null) return;
        PlaySound(defenseSound);
        
        if (useDefensePrefab && defenseSuccessVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(defenseSuccessVFX, card.transform.position, defensePrefabColor != Color.white, defensePrefabColor);
            if (vfx != null)
            {
                vfx.transform.SetParent(card.transform, true);
                vfx.transform.SetAsLastSibling();
            }
        }

        if (useNativeDefenseShield && defenseShieldSprite != null)
        {
            StartCoroutine(NativeDefenseShieldRoutine(card));
        }

        if (useDefenseRoutine)
        {
            StartCoroutine(PulseGhostRoutine(card, defensePulseScale, defensePulseDuration, defensePulseColor, defensePulseOutlineWidth));
            StartCoroutine(ShakeRoutine(card.transform, defenseShakeDuration, defenseShakeMagnitude));
        }
    }

    private IEnumerator NativeDefenseShieldRoutine(CardDisplay card)
    {
        GameObject shieldObj = new GameObject("DefenseShield_Native", typeof(RectTransform), typeof(Image));
        shieldObj.transform.SetParent(card.transform, false);
        shieldObj.transform.SetAsLastSibling(); // Por cima da carta

        RectTransform rt = shieldObj.GetComponent<RectTransform>();
        rt.anchoredPosition = defenseShieldOffset;
        rt.sizeDelta = defenseShieldSize;
        if (defenseShieldFlip180) rt.localRotation = Quaternion.Euler(0, 0, 180f);

        Image img = shieldObj.GetComponent<Image>();
        img.sprite = defenseShieldSprite;
        img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, 0f);
        if (defenseShieldMaterial != null) img.material = defenseShieldMaterial;

        float duration = defenseShieldDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float halfDuration = duration / 2f;
        float t = 0;

        Vector3 startScale = Vector3.one;
        Vector3 peakScale = Vector3.one * defenseShieldPulseScale;

        // Fase 1: Aparece e cresce
        while (t < halfDuration) {
            if (shieldObj == null || card == null) break;
            t += Time.deltaTime; float p = Mathf.SmoothStep(0, 1, t / halfDuration);
            rt.localScale = Vector3.Lerp(startScale, peakScale, p);
            img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, Mathf.Lerp(0f, defenseShieldColor.a, p));
            yield return null;
        }
        t = 0;
        // Fase 2: Diminui e esmaece
        while (t < halfDuration) {
            if (shieldObj == null || card == null) break;
            t += Time.deltaTime; float p = Mathf.SmoothStep(0, 1, t / halfDuration);
            rt.localScale = Vector3.Lerp(peakScale, startScale, p);
            img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, Mathf.Lerp(defenseShieldColor.a, 0f, p));
            yield return null;
        }
        if (shieldObj != null) Destroy(shieldObj);
    }

    public void PlayCardShake(CardDisplay card)
    {
        if (!enableAnimations || card == null) return;
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayCardShake | Alvo: {card.CurrentCardData?.name} | Momento: Tremedeira de impacto físico.");
        StartCoroutine(ShakeRoutine(card.transform, hitShakeDuration, hitShakeMagnitude));
    }

    private IEnumerator ShakeRoutine(Transform target, float duration, float magnitude)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            float x = originalPos.x + Random.Range(-magnitude, magnitude);
            float y = originalPos.y + Random.Range(-magnitude, magnitude);
            target.position = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (target != null) target.position = originalPos;
    }

    public void PlayChainLinkEffect(CardDisplay card, int linkNumber)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayChainLinkEffect | Alvo: {card?.CurrentCardData?.name} | Link: {linkNumber} | Momento: Efeito engatilhado em resposta.");
        
        if (linkNumber <= 1 || !enableAnimations) return;

        PlaySound(card.CurrentCardData.type.Contains("Trap") ? trapSound : spellSound);
        
        if (useChainLinkPrefab && chainLinkVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(chainLinkVFX, card.transform.position);
            
            MonoBehaviour[] scripts = vfx.GetComponents<MonoBehaviour>();
            bool setupCalled = false;
            foreach(var script in scripts) {
                var method = script.GetType().GetMethod("Setup");
                if (method != null) { method.Invoke(script, new object[] { linkNumber }); setupCalled = true; break; }
            }
            if (!setupCalled)
            {
                TMPro.TextMeshProUGUI txt = vfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = $"Link {linkNumber}";
            }
        }

        if (useChainLinkRoutine && card != null)
        {
            StartCoroutine(ChainLinkRoutine(card, linkNumber));
        }
    }

    private IEnumerator ChainLinkRoutine(CardDisplay card, int linkNumber)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) yield break;

        GameObject chainObj = new GameObject("ChainLink_Visual", typeof(RectTransform));
        chainObj.transform.SetParent(uiParent, false);
        
        RectTransform chainRT = chainObj.GetComponent<RectTransform>();
        chainRT.position = card.transform.position;
        chainObj.transform.SetAsLastSibling();

        GameObject textContainer = new GameObject("TextContainer", typeof(RectTransform));
        textContainer.transform.SetParent(chainObj.transform, false);
        RectTransform textContainerRT = textContainer.GetComponent<RectTransform>();
        textContainerRT.anchoredPosition = chainLinkTextOffset;
        textContainerRT.localScale = Vector3.zero;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(textContainer.transform, false);
        TMPro.TextMeshProUGUI txt = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        txt.text = $"Link {linkNumber}";
        txt.fontSize = 40;
        txt.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
        txt.color = chainLinkTextColor;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.overflowMode = TMPro.TextOverflowModes.Overflow;
        
        GameObject shadowObj = Instantiate(textObj, textContainer.transform);
        shadowObj.name = "Shadow";
        shadowObj.transform.SetAsFirstSibling();
        TMPro.TextMeshProUGUI shadowTxt = shadowObj.GetComponent<TMPro.TextMeshProUGUI>();
        shadowTxt.color = chainLinkShadowColor;
        shadowTxt.rectTransform.anchoredPosition = new Vector2(4, -4);

        Image leftImg = null, rightImg = null, joinedImg = null;
        RectTransform leftRT = null, rightRT = null;

        if (chainLinkLeftSprite != null)
        {
            GameObject leftObj = new GameObject("LeftLink", typeof(RectTransform), typeof(Image));
            leftObj.transform.SetParent(chainObj.transform, false);
            leftRT = leftObj.GetComponent<RectTransform>();
            leftRT.sizeDelta = chainLinkSideSize;
            leftRT.anchoredPosition = new Vector2(-100, 25);
            leftImg = leftObj.GetComponent<Image>();
            leftImg.sprite = chainLinkLeftSprite;
            leftImg.preserveAspect = chainLinkPreserveAspect;
            leftImg.color = chainLinkSpriteColor;
            if (chainLinkMaterial != null) leftImg.material = chainLinkMaterial;

            GameObject rightObj = new GameObject("RightLink", typeof(RectTransform), typeof(Image));
            rightObj.transform.SetParent(chainObj.transform, false);
            rightRT = rightObj.GetComponent<RectTransform>();
            rightRT.sizeDelta = chainLinkSideSize;
            rightRT.anchoredPosition = new Vector2(100, 25);
            rightImg = rightObj.GetComponent<Image>();
            
            if (chainLinkRightSprite != null) rightImg.sprite = chainLinkRightSprite;
            else { rightImg.sprite = chainLinkLeftSprite; rightRT.localScale = new Vector3(-1, 1, 1); }
            rightImg.preserveAspect = chainLinkPreserveAspect;
            rightImg.color = chainLinkSpriteColor;
            if (chainLinkMaterial != null) rightImg.material = chainLinkMaterial;

            if (chainLinkJoinedSprite != null)
            {
                GameObject joinedObj = new GameObject("JoinedLink", typeof(RectTransform), typeof(Image));
                joinedObj.transform.SetParent(chainObj.transform, false);
                RectTransform joinedRT = joinedObj.GetComponent<RectTransform>();
                joinedRT.sizeDelta = chainLinkJoinedSize;
                joinedRT.anchoredPosition = new Vector2(0, 25);
                joinedImg = joinedObj.GetComponent<Image>();
                joinedImg.sprite = chainLinkJoinedSprite;
                joinedImg.preserveAspect = chainLinkPreserveAspect;
                joinedImg.color = chainLinkSpriteColor;
                if (chainLinkMaterial != null) joinedImg.material = chainLinkMaterial;
                joinedImg.enabled = false;
            }
        }

        float duration = chainLinkDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        Vector2 startAnchored = chainRT.anchoredPosition + chainLinkBaseOffset;
        chainRT.anchoredPosition = startAnchored;
        Vector2 endAnchored = chainLinkSlideUp ? startAnchored + new Vector2(0, 120f) : startAnchored; 
        
        bool isClashed = false;
        GameObject flashObj = null; Image flashImg = null;

        while (t < 1f)
        {
            if (chainObj == null) break;
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            chainRT.anchoredPosition = Vector2.Lerp(startAnchored, endAnchored, smooth);
            
            if (t < 0.2f)
            {
                float clashT = t / 0.2f;
                if (leftRT != null) leftRT.anchoredPosition = Vector2.Lerp(new Vector2(-100, 25), new Vector2(-15, 25), clashT);
                if (rightRT != null) rightRT.anchoredPosition = Vector2.Lerp(new Vector2(100, 25), new Vector2(15, 25), clashT);
            }
            else if (!isClashed)
            {
                isClashed = true;
                if (leftImg != null) leftImg.enabled = false;
                if (rightImg != null) rightImg.enabled = false;
                if (joinedImg != null) joinedImg.enabled = true;

                if (chainLinkFlashOnImpact)
                {
                    flashObj = new GameObject("ClashFlash", typeof(RectTransform), typeof(Image));
                    flashObj.transform.SetParent(chainObj.transform, false);
                    RectTransform flashRT = flashObj.GetComponent<RectTransform>();
                    flashRT.anchoredPosition = new Vector2(0, 25);
                    flashRT.sizeDelta = chainLinkJoinedSize * 2f; 
                    flashImg = flashObj.GetComponent<Image>();
                    flashImg.color = chainLinkFlashColor;
                }
            }

            if (flashObj != null)
            {
                float flashT = (t - 0.2f) / 0.15f;
                if (flashT <= 1f) {
                    flashImg.color = new Color(chainLinkFlashColor.r, chainLinkFlashColor.g, chainLinkFlashColor.b, Mathf.Lerp(chainLinkFlashColor.a, 0f, flashT));
                    flashObj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, flashT);
                } else if (flashObj.activeSelf) {
                    flashObj.SetActive(false);
                }
            }

            if (t >= 0.2f && t < 0.4f)
            {
                float popT = (t - 0.2f) / 0.2f;
                float overshootScale = 1f + Mathf.Sin(popT * Mathf.PI) * 0.3f;
                textContainerRT.localScale = Vector3.one * (chainLinkScale * overshootScale);
                if (joinedImg != null) joinedImg.rectTransform.localScale = Vector3.one * overshootScale;
            }
            else if (t >= 0.4f)
            {
                textContainerRT.localScale = Vector3.one * chainLinkScale;
                if (joinedImg != null) joinedImg.rectTransform.localScale = Vector3.one;
            }

            if (t > 0.7f)
            {
                float alpha = 1f - ((t - 0.7f) / 0.3f);
                txt.color = new Color(chainLinkTextColor.r, chainLinkTextColor.g, chainLinkTextColor.b, alpha);
                shadowTxt.color = new Color(chainLinkShadowColor.r, chainLinkShadowColor.g, chainLinkShadowColor.b, alpha);
                if (joinedImg != null) joinedImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
                if (leftImg != null) leftImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
                if (rightImg != null) rightImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
            }

            yield return null;
        }

        if (chainObj != null) Destroy(chainObj);
    }

    public void PlayAttackDeclare()
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayAttackDeclare | Momento: Início da mira de ataque na UI.");
        PlaySound(attackDeclareSound);
    }

    public void PlayMonsterEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayMonsterEffect (Brilho) | Alvo: {card?.CurrentCardData?.name} | Momento: Efeito de monstro ativado no campo.");
        if (!enableAnimations || card == null) return;
        PlaySound(monsterEffectSound);
        if (useMonsterEffectPrefab && monsterEffectVFX != null) SpawnVFXPublic(monsterEffectVFX, card.transform.position);
        if (useMonsterEffectPulse) StartCoroutine(PulseGhostRoutine(card, monsterEffectPulseScale, monsterEffectPulseDuration, colorMonsterEffect, monsterEffectPulseOutlineWidth));
    }

    private IEnumerator PulseGhostRoutine(CardDisplay card, float targetScaleMult, float halfDuration, Color outlineColor, float outlineWidth, bool tintGhost = false)
    {
        if (card == null) yield break;

        Vector3 originalScale = card.transform.localScale;
        if (GameManager.Instance != null)
        {
            originalScale = card.isOnField ? GameManager.Instance.fieldCardScale : GameManager.Instance.handCardScale;
        }
        Vector3 peakScale = originalScale * targetScaleMult;
        Transform originalParent = card.transform.parent;
        int originalIndex = card.transform.GetSiblingIndex();

        card.transform.SetAsLastSibling(); 

        GameObject ghost = new GameObject("PulseGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform cardRt = card.GetComponent<RectTransform>();
        rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
        rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
        rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

        Color ghostColor = tintGhost ? new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.8f) : new Color(1f, 1f, 1f, 0.8f);

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture; 
        ri.color = ghostColor;
        
        Outline outline = ghost.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);

        float durationActual = halfDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        while (t < 1f) {
            if (card == null) break;
            t += Time.deltaTime / durationActual; float smooth = Mathf.SmoothStep(0, 1, t);
            card.transform.localScale = Vector3.Lerp(originalScale, peakScale, smooth);
            if (ghost != null) { ghost.transform.localScale = Vector3.Lerp(originalScale, peakScale * 1.15f, smooth); ri.color = new Color(ghostColor.r, ghostColor.g, ghostColor.b, Mathf.Lerp(ghostColor.a, 0f, smooth)); }
            yield return null;
        }
        if (ghost != null) Destroy(ghost);

        t = 0;
        while (t < 1f) {
            if (card == null) break; t += Time.deltaTime / durationActual; float smooth = Mathf.SmoothStep(0, 1, t);
            card.transform.localScale = Vector3.Lerp(peakScale, originalScale, smooth); yield return null;
        }
        if (card != null) { card.transform.localScale = originalScale; if (originalParent != null) { card.transform.SetParent(originalParent, true); card.transform.SetSiblingIndex(originalIndex); } }
    }

    // Wrapper de retrocompatibilidade
    public void PlaySummonAura(CardDisplay card) => PlayPlacementAura(card);

    public void PlayPlacementAura(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayPlacementAura | Alvo: {card?.CurrentCardData?.name} | Momento: Carta foi posicionada fisicamente na mesa (Set/Summon/Activate).");
        if (!enableAnimations || card == null) return;

        StartCoroutine(PlacementAuraWrapper(card));
    }

    private IEnumerator PlacementAuraWrapper(CardDisplay card)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            while (cg.alpha < 0.5f) yield return null;
        }

        Color targetColor = GetAuraColor(card);

        if (usePlacementAuraRoutine)
        {
            StartCoroutine(PlacementAuraRoutine(card, targetColor));
        }

        if (!usePlacementAuraPrefab || placementAuraVFX == null) yield break;
        
        GameObject vfx = Instantiate(placementAuraVFX);
        
        Transform originalParent = card.transform.parent;
        if (originalParent != null)
        {
            vfx.transform.SetParent(originalParent, false);
            
            if (placementAuraRenderMode == PlacementAuraRenderMode.Behind)
                vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // ATRÁS da carta
            else
                vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex() + 1); // NA FRENTE da carta
            
            RectTransform rt = vfx.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = card.transform.localRotation; // Acompanha 90º
                rt.localScale = Vector3.one * placementAuraPrefabScale;
            }
            else
            {
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = card.transform.localRotation;
                vfx.transform.localScale = Vector3.one * placementAuraPrefabScale;
            }
        }
        else
        {
            vfx.transform.position = card.transform.position;
            vfx.transform.rotation = card.transform.rotation;
            vfx.transform.localScale = Vector3.one * placementAuraPrefabScale;
        }

        ParticleSystemRenderer[] renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (renderers.Length > 0)
        {
            Canvas rootCanvas = card.GetComponentInParent<Canvas>();
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder + 1 : 1;
            
            if (placementAuraRenderMode == PlacementAuraRenderMode.Above)
                targetOrder += 10; // Coloca bem na frente da carta
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
        }

        if (colorizeAuraByType || colorizeAuraByPlayer)
        {
            foreach (var r in renderers)
            {
                var psSystem = r.GetComponent<ParticleSystem>();
                if (psSystem != null) { var main = psSystem.main; main.startColor = targetColor; }
            }
            Image[] images = vfx.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.color = targetColor;
            }
        }

        float destroyTime = 3.0f;
        if (overridePlacementAuraPrefabDuration)
        {
            destroyTime = placementAuraPrefabDuration;
        }
        else
        {
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            destroyTime = ps != null ? (ps.main.duration + ps.main.startLifetime.constantMax + 0.5f) : 3.0f;
        }
        Destroy(vfx, destroyTime);
        
        Debug.Log($"[VFX] ✔ [AURA INSTANCIADA] '{placementAuraVFX.name}' renderizada atrás da carta. Vida: {destroyTime}s.");
    }

    private Color GetAuraColor(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return colorMonsterNormal;
        
        if (colorizeAuraByPlayer && GameManager.Instance != null)
        {
            return card.isPlayerCard ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor;
        }

        if (!colorizeAuraByType) return colorMonsterNormal;
        if (card.isFlipped) return colorFaceDown;
        if (card.CurrentCardData.type.Contains("Spell")) return colorSpell;
        if (card.CurrentCardData.type.Contains("Trap")) return colorTrap;
        if (card.CurrentCardData.type.Contains("Fusion")) return colorFusion;
        if (card.CurrentCardData.type.Contains("Ritual")) return colorRitual;
        if (card.CurrentCardData.type.Contains("Effect")) return colorMonsterEffect;
        return colorMonsterNormal;
    }

    private IEnumerator PlacementAuraRoutine(CardDisplay card, Color outlineColor)
    {
        if (card == null) yield break;

        Transform originalParent = card.transform.parent;

        GameObject ghost = new GameObject("PlacementAuraGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // Nasce ATRÁS da carta real!
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform cardRt = card.GetComponent<RectTransform>();
        rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
        rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
        rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture; 
        
        Outline outline = ghost.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(placementAuraOutlineWidth, -placementAuraOutlineWidth);

        float durationActual = placementAuraDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        while (t < 1f) {
            if (card == null || ghost == null) break;
            t += Time.deltaTime / durationActual; 
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            // Curva Perfeita: Invisível (0) -> Fica forte (1) -> Invisível (0)
            float alpha = Mathf.Sin(smooth * Mathf.PI); 
            
            Color c = outlineColor; c.a = alpha;
            ri.color = c; 
            outline.effectColor = c;
            
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
    }

    public void PlayEquipEffect(CardDisplay source, CardDisplay target)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayEquipEffect | Origem: {source?.CurrentCardData?.name} -> Alvo: {target?.CurrentCardData?.name} | Momento: Carta equipada com sucesso.");
        if (!enableAnimations || source == null || target == null) return; // FIX: Removida a exigência desnecessária do boardCenter
        
        if (useEquipPrefab && equipImpactVFX != null)
        {
            SpawnVFXPublic(equipImpactVFX, target.transform.position);
        }

        if (useEquipGhost)
        {
            StartCoroutine(EquipGhostRoutine(source, target));
        }
    }

    private IEnumerator EquipGhostRoutine(CardDisplay source, CardDisplay target)
    {
        // Cria o fantasma da carta mágica
        GameObject ghost = new GameObject("EquipGhost", typeof(RectTransform), typeof(RawImage));
        
        // FIX: Clona a hierarquia exata primeiro para herdar as âncoras e a escala (ex: 0.8) da carta original!
        Transform originalParent = source.transform.parent;
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform sourceRT = source.GetComponent<RectTransform>();
        rt.anchorMin = sourceRT.anchorMin; rt.anchorMax = sourceRT.anchorMax; rt.pivot = sourceRT.pivot;
        rt.sizeDelta = sourceRT.sizeDelta; rt.anchoredPosition = sourceRT.anchoredPosition;
        rt.localRotation = sourceRT.localRotation; rt.localScale = sourceRT.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = source.cardImage.texture; 
        ri.color = new Color(1f, 1f, 1f, 0.8f); // Fantasma semitransparente

        // Movemos para o Canvas raiz usando worldPositionStays=true para voar por cima de tudo preservando as proporções
        Transform uiParent = GetUIParent();
        if (uiParent != null) { 
            ghost.transform.SetParent(uiParent, true); 
            ghost.transform.SetAsLastSibling(); 
        }

        PlaySound(spellSound);
        float duration = equipGhostFlightDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0; 
        Vector3 startPos = rt.position; 
        Vector3 startScale = rt.localScale;
        
        // Calcula o scale final absoluto ajustado para a mesma perspectiva do pai da UI
        Vector3 endScale = target.transform.lossyScale;
        if (uiParent != null) {
            endScale = new Vector3(
                target.transform.lossyScale.x / uiParent.lossyScale.x,
                target.transform.lossyScale.y / uiParent.lossyScale.y,
                target.transform.lossyScale.z / uiParent.lossyScale.z
            );
        }
        
        float startZ = rt.eulerAngles.z;
        float targetZ = target.transform.eulerAngles.z;
        
        while (t < 1f) {
            if (target == null || target.gameObject == null || rt == null) break;
            t += Time.deltaTime / duration; 
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // FIX: Viagem em linha reta! Em Top-Down, a sensação de parábola (altura/subida) vem SÓ da Escala.
            rt.position = Vector3.Lerp(startPos, target.transform.position, smoothT);
            
            // Desliza suavemente para a rotação alvo sem giros extras (0 ou 180 graus dependendo do lado do campo)
            float currentZ = Mathf.LerpAngle(startZ, targetZ, smoothT);
            rt.rotation = Quaternion.Euler(0, 0, currentZ); 
            
            // Escala: Incha no meio do caminho (simulando "Subir" em direção à câmera) e desce para o tamanho do alvo
            float scaleMult = 1f + Mathf.Sin(smoothT * Mathf.PI) * (equipGhostFlightScale - 1f);
            rt.localScale = Vector3.Lerp(startScale, endScale, smoothT) * scaleMult; 
            
            yield return null;
        }

        if (ghost != null) Destroy(ghost);

        // Impacto de encaixe preto (Outline Squeeze)
        if (target != null)
        {
            StartCoroutine(EquipOutlineSqueezeRoutine(target));
        }
    }

    private IEnumerator EquipOutlineSqueezeRoutine(CardDisplay target)
    {
        // Cria a "Sombra de Encaixe" (Squeeze) atrás da carta
        GameObject squeezeObj = new GameObject("EquipSqueeze", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        
        Transform parentToUse = target.transform.parent;
        squeezeObj.transform.SetParent(parentToUse, false);
        // Fica logo atrás do alvo
        squeezeObj.transform.SetSiblingIndex(target.transform.GetSiblingIndex()); 
        
        RectTransform rt = squeezeObj.GetComponent<RectTransform>();
        RectTransform targetRT = target.GetComponent<RectTransform>();
        
        rt.position = targetRT.position;
        rt.sizeDelta = targetRT.sizeDelta;
        rt.rotation = targetRT.rotation;
        rt.pivot = targetRT.pivot;

        RawImage ri = squeezeObj.GetComponent<RawImage>();
        ri.texture = target.cardImage.texture;
        ri.color = equipSqueezeColor; // Sombra customizada

        Outline outline = squeezeObj.GetComponent<Outline>();
        outline.effectColor = equipSqueezeColor;
        outline.effectDistance = new Vector2(10f, -10f);
        
        float duration = 0.2f / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        Vector3 startScale = target.transform.localScale * equipSqueezeScale;
        Vector3 endScale = target.transform.localScale;
        
        while (t < 1f)
        {
            if (target == null) break;
            t += Time.deltaTime / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            rt.position = targetRT.position; 
            rt.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            
            // Esmaece no último quarto da animação para um encaixe suave
            if (smoothT > 0.7f)
            {
                float fadeT = (smoothT - 0.7f) / 0.3f;
                
                Color currentImgColor = equipSqueezeColor;
                currentImgColor.a = (1f - fadeT) * equipSqueezeColor.a;
                ri.color = currentImgColor;
                
                Color outlineColor = equipSqueezeColor;
                outlineColor.a = (1f - fadeT) * equipSqueezeColor.a;
                outline.effectColor = outlineColor;
            }
            
            yield return null;
        }

        if (squeezeObj != null) Destroy(squeezeObj);
    }

    public void PlayShuffleEffect(Transform pileTransform)
    {
        if (!enableAnimations || pileTransform == null) return;
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayShuffleEffect | Momento: Embaralhamento engatilhado na pilha {pileTransform.name}.");
        
        if (useShuffleRoutine || useCustomHinduShuffle || useSimple2DShuffle || useShufflePrefab)
        {
            StartCoroutine(ShuffleRoutine(pileTransform));
        }
        else
        {
            PlaySound(shuffleSound);
        }
    }

    // Usado pelo GameManager para saber EXATAMENTE quando liberar as compras
    public float GetShuffleTotalDuration()
    {
        float timePerLoop = shuffleDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float extraFadeOutTime = 0f;

        if (useShufflePrefab && shuffleVFX != null)
        {
            ParticleSystem ps = shuffleVFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (ps.main.duration > timePerLoop) timePerLoop = ps.main.duration;
                extraFadeOutTime = ps.main.startLifetime.constantMax;
            }
        }
        return (timePerLoop * Mathf.Max(1, shuffleLoopCount)) + extraFadeOutTime + shuffleDeckAppearDelay + shufflePostDelay;
    }

    private IEnumerator ShuffleRoutine(Transform pileTransform)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null && useShuffleRoutine) yield break;

        Vector3 startPos = pileTransform.position;
        Vector3 centerPos = startPos;
        if (shuffleAtCenter)
        {
            if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null)
            {
                bool isPlayerDeck = (pileTransform == GameManager.Instance.duelFieldUI.playerDeck);
                if (isPlayerDeck && playerBoardCenter != null) centerPos = playerBoardCenter.position;
                else if (!isPlayerDeck && opponentBoardCenter != null) centerPos = opponentBoardCenter.position;
                else if (boardCenter != null) centerPos = boardCenter.position;
            }
            else if (boardCenter != null) centerPos = boardCenter.position;
        }
        RectTransform pileRT = pileTransform.GetComponent<RectTransform>();

        // Ocultar o deck real temporariamente para evitar z-fighting e tremeliques com o VFX
        CanvasGroup pileCG = pileTransform.GetComponent<CanvasGroup>();
        bool addedCG = false;
        if (pileCG == null) { pileCG = pileTransform.gameObject.AddComponent<CanvasGroup>(); addedCG = true; }
        float originalAlpha = pileCG.alpha;
        pileCG.alpha = 0f;

        float routineDuration = shuffleDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        int loops = Mathf.Max(1, shuffleLoopCount);
        float totalTime = routineDuration * loops;
        float extraFadeOutTime = 0f;

            if (useShufflePrefab && shuffleVFX != null)
            {
                ParticleSystem ps = shuffleVFX.GetComponent<ParticleSystem>();
                if (ps != null) extraFadeOutTime = ps.main.startLifetime.constantMax;
            }

        bool runRoutine = useShuffleRoutine || useCustomHinduShuffle || useSimple2DShuffle;

        bool isOpponentDeck = (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null && pileTransform == GameManager.Instance.duelFieldUI.opponentDeck);
        Quaternion basePileRot = pileTransform.rotation;
        if (isOpponentDeck && shuffleOpponent180) basePileRot *= Quaternion.Euler(0, 0, 180f);

        if (runRoutine && uiParent != null)
            {
                int deckSize = useCustomHinduShuffle ? 40 : (useSimple2DShuffle ? 5 : 6); 
                List<GameObject> fakeCards = new List<GameObject>();

                for (int i = 0; i < deckSize; i++)
                {
                    GameObject card = new GameObject($"FakeCard_{i}", typeof(RectTransform), typeof(RawImage));
                    if (pileRT != null && pileRT.parent != null)
                    {
                        card.transform.SetParent(pileRT.parent, false);
                        RectTransform rt = card.GetComponent<RectTransform>();
                        rt.anchorMin = pileRT.anchorMin; rt.anchorMax = pileRT.anchorMax; rt.pivot = pileRT.pivot;
                        rt.sizeDelta = pileRT.sizeDelta; rt.anchoredPosition = pileRT.anchoredPosition; rt.rotation = basePileRot; rt.localScale = pileRT.localScale;
                    }
                    card.transform.SetParent(uiParent, true); 
                    card.transform.SetAsLastSibling();

                    RawImage ri = card.GetComponent<RawImage>();
                    ri.texture = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;
                    fakeCards.Add(card);
                }

                if (useCustomHinduShuffle)
                {
                    Vector3 basePos = centerPos;
                float moveCenterTime = shuffleAtCenter ? 0.2f : 0f; 
                Quaternion stackRot = basePileRot * Quaternion.Euler(0, 0, customShuffleTiltAngle);
                    
                    System.Func<Vector3, int, Vector3> GetStackPos = (center, index) => {
                        Vector3 offset = stackRot * new Vector3(index * -0.4f, index * 0.8f, 0); 
                        return center + offset;
                    };

                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        float smooth = Mathf.SmoothStep(0, 1, moveT);
                        for (int i = 0; i < deckSize; i++) {
                            fakeCards[i].transform.position = Vector3.Lerp(startPos, GetStackPos(basePos, i), smooth);
                            fakeCards[i].transform.rotation = Quaternion.Lerp(basePileRot, stackRot, smooth);
                            float scaleMult = 1f + (smooth * (shuffleScaleMultiplier - 1f));
                            fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
                else
                {
                    basePos = startPos;
                    for (int i = 0; i < deckSize; i++) {
                        fakeCards[i].transform.position = GetStackPos(basePos, i);
                        fakeCards[i].transform.rotation = stackRot; // Já está correto
                    }
                }

                int movesPerLoop = 5;
                int totalMoves = movesPerLoop * loops;
                float shufflePhaseTime = Mathf.Max(0.1f, totalTime - (moveCenterTime * 2f));
                float moveDuration = shufflePhaseTime / totalMoves;
                float stepDur = moveDuration / 3f;
                Vector3 rightOffset = basePileRot * new Vector3(110f, 0, 0);

                for (int m = 0; m < totalMoves; m++)
                {
                    if (m % movesPerLoop == 0)
                    {
                        if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                    }

                    int packetSize = 10;
                    int extractIdx = 10 + (m % 2) * 5; 
                    List<GameObject> packet = fakeCards.GetRange(extractIdx, packetSize);
                    fakeCards.RemoveRange(extractIdx, packetSize);

                        float t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, extractIdx + i), GetStackPos(basePos, extractIdx + i) + rightOffset, smooth);
                            yield return null;
                        }

                        for (int i = 0; i < packet.Count; i++) packet[i].transform.SetAsLastSibling();
                        
                        t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, extractIdx + i) + rightOffset, GetStackPos(basePos, fakeCards.Count + i) + rightOffset, smooth);
                            for (int i = extractIdx; i < fakeCards.Count; i++) fakeCards[i].transform.position = Vector3.Lerp(GetStackPos(basePos, i + packetSize), GetStackPos(basePos, i), smooth);
                            yield return null;
                        }

                        fakeCards.AddRange(packet);
                        t = 0;
                        while (t < 1f) {
                            t += Time.deltaTime / stepDur;
                            float smooth = Mathf.SmoothStep(0, 1, t);
                            for (int i = 0; i < packet.Count; i++) packet[i].transform.position = Vector3.Lerp(GetStackPos(basePos, fakeCards.Count - packetSize + i) + rightOffset, GetStackPos(basePos, fakeCards.Count - packetSize + i), smooth);
                            yield return null;
                        }
                    }
                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        float smooth = Mathf.SmoothStep(0, 1, moveT);
                        for (int i = 0; i < deckSize; i++) {
                            fakeCards[i].transform.position = Vector3.Lerp(GetStackPos(basePos, i), startPos, smooth);
                            fakeCards[i].transform.rotation = Quaternion.Lerp(stackRot, basePileRot, smooth);
                            float scaleMult = 1f + ((1f - smooth) * (shuffleScaleMultiplier - 1f));
                            fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
            }
            else if (useSimple2DShuffle)
            {
                Vector3 basePos = centerPos;
                float moveCenterTime = shuffleAtCenter ? 0.2f : 0f;
                
                    if (shuffleAtCenter)
                    {
                        float moveT = 0;
                        while (moveT < 1f) {
                            moveT += Time.deltaTime / moveCenterTime;
                            foreach(var c in fakeCards) {
                                c.transform.position = Vector3.Lerp(startPos, basePos, Mathf.SmoothStep(0, 1, moveT));
                                float scaleMult = 1f + (moveT * (shuffleScaleMultiplier - 1f));
                                c.transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                            yield return null;
                        }
                }
                else
                {
                    basePos = startPos;
                }

                int movesPerLoop = 6;
                int totalMoves = movesPerLoop * loops;
                float shufflePhaseTime = Mathf.Max(0.1f, totalTime - (moveCenterTime * 2f));
                float moveDuration = shufflePhaseTime / totalMoves;
                float halfMove = moveDuration / 2f;
                
                for (int m = 0; m < totalMoves; m++)
                {
                    if (m % movesPerLoop == 0)
                    {
                        if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                    }

                    GameObject movingCard = fakeCards[fakeCards.Count - 1];
                    fakeCards.RemoveAt(fakeCards.Count - 1);
                    
                    float yOffset = (m % 2 == 0) ? shuffle2DOffset.y : -shuffle2DOffset.y;
                    float xOffset = shuffle2DOffset.x;
                    Vector3 peakPos = basePos + (basePileRot * new Vector3(xOffset, yOffset, 0f));
                    
                    PlaySound(shuffleSound);
                    
                    float t = 0;
                    while(t < 1f) {
                        t += Time.deltaTime / halfMove;
                        movingCard.transform.position = Vector3.Lerp(basePos, peakPos, Mathf.SmoothStep(0, 1, t));
                        yield return null;
                    }
                    
                    movingCard.transform.SetSiblingIndex(fakeCards[0].transform.GetSiblingIndex());
                    fakeCards.Insert(0, movingCard);
                    
                    t = 0;
                    while(t < 1f) {
                        t += Time.deltaTime / halfMove;
                        movingCard.transform.position = Vector3.Lerp(peakPos, basePos, Mathf.SmoothStep(0, 1, t));
                        yield return null;
                    }
                }

                if (shuffleAtCenter)
                {
                    float moveT = 0;
                    while (moveT < 1f) {
                        moveT += Time.deltaTime / moveCenterTime;
                        foreach(var c in fakeCards) {
                            c.transform.position = Vector3.Lerp(basePos, startPos, Mathf.SmoothStep(0, 1, moveT));
                            float scaleMult = 1f + ((1f - moveT) * (shuffleScaleMultiplier - 1f));
                            c.transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                        }
                        yield return null;
                    }
                }
            }
            else
            {
                float timePerLoop = totalTime / loops;
                float halfDuration = timePerLoop / 2f;

                for (int loopIndex = 0; loopIndex < loops; loopIndex++)
                {
                    PlaySound(shuffleSound); 
                    if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);

                    float t = 0;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / halfDuration;
                        float smooth = Mathf.SmoothStep(0, 1, t);
                        Vector3 currentBasePos = Vector3.Lerp(startPos, centerPos, smooth);

                        for (int i = 0; i < deckSize; i++)
                        {
                            if (fakeCards[i] == null) continue;
                            float xTargetOffset = (i % 2 == 0) ? -60f : 60f;
                            float yTargetOffset = Mathf.Sin(smooth * Mathf.PI) * 40f; 
                            float rotationZ = (i % 2 == 0) ? Mathf.Lerp(0, 20f, smooth) : Mathf.Lerp(0, -20f, smooth);
                            
                            fakeCards[i].transform.position = currentBasePos + new Vector3(xTargetOffset * smooth, yTargetOffset * smooth, 0);
                            fakeCards[i].transform.rotation = basePileRot * Quaternion.Euler(0, 0, rotationZ);

                            if (shuffleAtCenter)
                            {
                                float scaleMult = 1f + (smooth * (shuffleScaleMultiplier - 1f)); 
                                fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                        }
                        yield return null;
                    }
                    for (int i = 0; i < deckSize; i++)
                    {
                        if (fakeCards[i] == null) continue;
                        int siblingTarget = fakeCards[i].transform.parent.childCount - (i % 2 == 0 ? i : deckSize - i);
                        fakeCards[i].transform.SetSiblingIndex(siblingTarget);
                    }

                    t = 0;
                    while (t < 1f)
                    {
                        t += Time.deltaTime / halfDuration;
                        float smooth = Mathf.SmoothStep(0, 1, t);
                        Vector3 currentBasePos = Vector3.Lerp(centerPos, startPos, smooth);

                        for (int i = 0; i < deckSize; i++)
                        {
                            if (fakeCards[i] == null) continue;
                            float xStartOffset = (i % 2 == 0) ? -60f : 60f;
                            float currentXOffset = Mathf.Lerp(xStartOffset, 0, smooth);
                            float rotationZ = (i % 2 == 0) ? Mathf.Lerp(20f, 0f, smooth) : Mathf.Lerp(-20f, 0f, smooth);
                            
                            fakeCards[i].transform.position = currentBasePos + new Vector3(currentXOffset, 0, 0);
                            fakeCards[i].transform.rotation = basePileRot * Quaternion.Euler(0, 0, rotationZ);

                            if (shuffleAtCenter)
                            {
                                float scaleMult = 1f + ((1f - smooth) * (shuffleScaleMultiplier - 1f)); 
                                fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                            }
                        }
                        yield return null;
                    }
                }
            }

            foreach (var c in fakeCards) Destroy(c);
        }
        else
        {
            float timePerLoop = totalTime / loops;
            for (int loopIndex = 0; loopIndex < loops; loopIndex++)
            {
                PlaySound(shuffleSound);
                if (useShufflePrefab && shuffleVFX != null) SpawnVFXPublic(shuffleVFX, centerPos);
                yield return new WaitForSeconds(timePerLoop);
            }
        }
        if (extraFadeOutTime > 0) yield return new WaitForSeconds(extraFadeOutTime);
        if (shuffleDeckAppearDelay > 0) yield return new WaitForSeconds(shuffleDeckAppearDelay);

        // Restaura a visibilidade do Deck real
        if (pileCG != null)
        {
            pileCG.alpha = originalAlpha;
            if (addedCG) Destroy(pileCG);
        }
    }

    // Wrappers de Compatibilidade Retroativa para as Cinemáticas
    public void PlaySummonCinematic(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        SummonVFXType type = isTribute ? SummonVFXType.Tribute : (isSpecial ? SummonVFXType.Special : SummonVFXType.Normal);
        PlaySummonCinematic(card, type, onComplete);
    }

    public void PlaySummonCinematic(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(SummonCinematicRoutine(card, type, onComplete));
    }

    public void SpawnFieldMarker(Transform targetZone, SummonVFXType type)
    {
        SummonVFXPackage package = GetSummonPackage(type);
        if (package == null || !package.fieldMarker.useMarker) return;
        FieldMarkerSettings settings = package.fieldMarker;

        if (settings.usePrefab && settings.prefab != null)
        {
            GameObject vfx = SpawnVFXPublic(settings.prefab, targetZone.position, true, settings.color);
            if (vfx != null) 
            {
                // Retorna ao falso para não herdar distorções de tela/canvas
                vfx.transform.SetParent(targetZone, false);
                
                // Restaura o tamanho e as âncoras exatas do Prefab original que a Unity distorceu
                RectTransform rt = vfx.GetComponent<RectTransform>();
                RectTransform prefabRt = settings.prefab.GetComponent<RectTransform>();
                if (rt != null && prefabRt != null)
                {
                    rt.anchorMin = prefabRt.anchorMin; rt.anchorMax = prefabRt.anchorMax;
                    rt.pivot = prefabRt.pivot; rt.sizeDelta = prefabRt.sizeDelta;
                    rt.anchoredPosition = Vector2.zero;
                }
                else { vfx.transform.localPosition = Vector3.zero; }
                
                vfx.transform.SetAsFirstSibling();
                // Injeta a escala original do Prefab para a matemática não enlouquecer
                StartCoroutine(PrefabFieldMarkerRoutine(vfx, settings.prefab.transform.localScale, settings));
            }
        }
        if (settings.useNative) StartCoroutine(NativeFieldMarkerRoutine(targetZone, settings));
    }

    private IEnumerator PrefabFieldMarkerRoutine(GameObject vfx, Vector3 baseScale, FieldMarkerSettings settings)
    {
        float t = 0; 
        float duration = settings.duration / (animationSpeed > 0 ? animationSpeed : 1f);

        while (t < 1f) {
            if (vfx == null) break;
            t += Time.deltaTime / duration; 
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            float currentMultiplier = Mathf.Lerp(settings.startSize, settings.endSize, smooth);
            vfx.transform.localScale = baseScale * currentMultiplier;
            yield return null;
        } 
        if (vfx != null) Destroy(vfx); // Retorna a destruição manual para objetos 2D!
    }

    private IEnumerator NativeFieldMarkerRoutine(Transform targetZone, FieldMarkerSettings settings)
    {
        GameObject marker = new GameObject("NativeFieldMarker", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(targetZone, false);
        marker.transform.SetAsFirstSibling();
        
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200, 200); 
        
        Image img = marker.GetComponent<Image>();
        img.color = new Color(settings.color.r, settings.color.g, settings.color.b, 0f);
        Outline outline = marker.AddComponent<Outline>();
        outline.effectColor = settings.color; outline.effectDistance = new Vector2(5f, -5f);
        
        float t = 0; float duration = settings.duration / (animationSpeed > 0 ? animationSpeed : 1f);
        while (t < 1f) {
            if (marker == null) break;
            t += Time.deltaTime / duration; float smooth = Mathf.SmoothStep(0, 1, t);
            rt.localScale = Vector3.Lerp(Vector3.one * settings.startSize, Vector3.one * settings.endSize, smooth);
            Color c = settings.color; c.a = Mathf.Lerp(settings.color.a, 0f, smooth); outline.effectColor = c;
            yield return null;
        } 
        if (marker != null) Destroy(marker);
    }

    private IEnumerator SummonCinematicRoutine(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;
        GameObject symbolObj = null;
        GameObject cinCard = null;
        
        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
        };

        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;

        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { 
            symbolObj = new GameObject("CinematicSymbol", typeof(RectTransform), typeof(Image)); 
            symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0.6f); 
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial;
        }

        cinCard = new GameObject("CinematicCard", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        
        RectTransform rt = cinCard.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 290f);
        
        RawImage ri = cinCard.GetComponent<RawImage>();
        
        bool showBack = !card.isPlayerCard;
        if (showBack && GameManager.Instance != null) ri.texture = GameManager.Instance.GetCardBackTexture();
        else ri.texture = card.cardImage.texture;

        PlaySound(summonSound);
        t = 0;

        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur);
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p);
            if (symbolObj != null && package.cinematic.spinBackgroundSymbol)
                symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed);
            yield return null; 
        }

        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            if (symbolObj != null && package.cinematic.spinBackgroundSymbol)
                symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed);
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        PlaySound(attackTravelSound);

        Vector3 startPos = rt.position; 
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p);
            rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p));
            if (symbolObj != null) {
                Image imgSym = symbolObj.GetComponent<Image>();
                imgSym.color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p));
            }
            yield return null; 
        }

        Cleanup();
        onComplete?.Invoke();
    }

    public void PlayFusionCinematic(CardDisplay card, List<CardData> materials, CardData polyCard, System.Action onComplete)
    {
        PlayFusionCinematic(card, materials, SummonVFXType.Fusion, onComplete);
    }

    public void PlayFusionCinematic(CardDisplay card, List<CardData> materials, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(FusionCinematicRoutine(card, materials, type, onComplete));
    }

    private IEnumerator FusionCinematicRoutine(CardDisplay card, List<CardData> materials, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float orbitDur = package != null ? package.cinematic.orbitDuration : 1.5f;
        float orbitSpeed = package != null ? package.cinematic.cardsOrbitSpeed : 1080f;
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;

        GameObject symbolObj = null;
        GameObject cinCard = null;
        List<RectTransform> matRects = new List<RectTransform>();
        
        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
            foreach (var m in matRects) { if (m) Destroy(m.gameObject); }
        };
        
        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { 
            symbolObj = new GameObject("FusionSymbol", typeof(RectTransform), typeof(Image)); 
            symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0);             
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial;
        }

        bool faceUpMats = package == null || package.cinematic.showMaterialsFaceUp;

        if (materials != null && materials.Count > 0 && GameManager.Instance != null)
        {
            foreach (var mat in materials)
            {
                GameObject matObj;
                if (GameManager.Instance.cardPrefab != null)
                {
                    matObj = Instantiate(GameManager.Instance.cardPrefab, uiParent);
                    CardDisplay cd = matObj.GetComponent<CardDisplay>();
                    if (cd != null) { cd.SetCard(mat, GameManager.Instance.GetCardBackTexture(), faceUpMats); cd.isInteractable = false; }
                    LayoutElement le = matObj.GetComponent<LayoutElement>(); if (le != null) Destroy(le);
                }
                else
                {
                    matObj = new GameObject("MatCard", typeof(RectTransform), typeof(RawImage));
                    matObj.transform.SetParent(uiParent, false);
                    matObj.GetComponent<RawImage>().texture = GameManager.Instance.GetCardBackTexture();
                }

                RectTransform rtMat = matObj.GetComponent<RectTransform>(); 
                rtMat.anchorMin = new Vector2(0.5f, 0.5f); rtMat.anchorMax = new Vector2(0.5f, 0.5f);
                rtMat.anchoredPosition = Vector2.zero; rtMat.sizeDelta = new Vector2(100f, 145f);
                rtMat.localScale = Vector3.one;
                matRects.Add(rtMat);
            }
        }

        if (matRects.Count > 0)
        {
            PlaySound(spellSound);
            float orbitTime = 0f;
            while (orbitTime < orbitDur)
            {
                if (card == null) { Cleanup(); yield break; }
                orbitTime += Time.deltaTime * animSpeed; float progress = orbitTime / orbitDur;
                float radius = Mathf.Lerp(300f, 0f, progress * progress); 
                if (symbolObj != null) {
                    symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, progress)); 
                    if (package != null && package.cinematic.spinBackgroundSymbol)
                        symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed); 
                }
                for (int i = 0; i < matRects.Count; i++) {
                    float offsetAngle = (360f / matRects.Count) * i; float currentAngle = (orbitTime * orbitSpeed) + offsetAngle;
                    float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius; float y = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
                    matRects[i].anchoredPosition = new Vector2(x, y); matRects[i].rotation = Quaternion.Euler(0, 0, currentAngle);
                }
                yield return null;
            }
        }
        else {
            float waitT = 0;
            while (waitT < 0.5f) {
                if (card == null) { Cleanup(); yield break; }
                waitT += Time.deltaTime * animSpeed;
                yield return null;
            }
        }

        foreach (var m in matRects) if (m) Destroy(m.gameObject);
        matRects.Clear();

        PlaySound(fusionSound);
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (package != null && package.cinematic.usePrefab && package.cinematic.flashPrefab != null) SpawnVFXPublic(package.cinematic.flashPrefab, worldCenter);
        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        cinCard = new GameObject("CinematicFusion", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>();
        ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p); 
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; 
        }

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p); rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; 
        }

        Cleanup();       
        onComplete?.Invoke();
    }

    public void PlayRitualCinematic(CardDisplay card, CardData ritualSpell, System.Action onComplete)
    {
        PlayRitualCinematic(card, SummonVFXType.Ritual, onComplete);
    }

    public void PlayRitualCinematic(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(RitualCinematicRoutine(card, type, onComplete));
    }

    private IEnumerator RitualCinematicRoutine(CardDisplay card, SummonVFXType type, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        SummonVFXPackage package = GetSummonPackage(type);
        bool useDark = package == null || package.cinematic.useDarkOverlay;
        bool useWhiteFlash = package != null && package.cinematic.useWhiteFlash;

        float fadeDur = package != null ? package.cinematic.darkOverlayFadeDuration : 0.3f;
        float orbitDur = package != null ? package.cinematic.orbitDuration : 1.0f; // Usado para o tempo do símbolo na tela
        float flashDur = package != null ? package.cinematic.flashDuration : 0.4f;
        float appearDur = package != null ? package.cinematic.giantCardAppearDuration : 0.3f;
        float holdDur = package != null ? package.cinematic.giantCardHoldDuration : 0.6f;
        float delayMarker = package != null ? package.cinematic.delayBeforeMarker : 0f;
        float delayDrop = package != null ? package.cinematic.delayBeforeCardDrop : 0.3f;
        float cardScale = package != null ? package.cinematic.giantCardScale : 1.5f;
        float symScale = package != null ? package.cinematic.symbolScale : 400f;
        bool preserveAspect = package == null || package.cinematic.preserveBackgroundAspect;

        GameObject darkOverlay = null; Image darkImg = null;
        GameObject symbolObj = null;
        GameObject cinCard = null;

        System.Action Cleanup = () => {
            if (darkOverlay) Destroy(darkOverlay);
            if (symbolObj) Destroy(symbolObj);
            if (cinCard) Destroy(cinCard);
        };

        if (useDark)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image)); 
            darkOverlay.transform.SetParent(uiParent, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>(); 
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
            rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>(); darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < fadeDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / fadeDur)); 
            yield return null; 
        }

        if (package != null && package.cinematic.backgroundSymbol != null) { symbolObj = new GameObject("RitualSymbol", typeof(RectTransform), typeof(Image)); symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(symScale, symScale);
            Image imgSym = symbolObj.GetComponent<Image>(); 
            imgSym.sprite = package.cinematic.backgroundSymbol; 
            imgSym.preserveAspect = preserveAspect;
            imgSym.color = new Color(1, 1, 1, 0); 
            if (package.cinematic.backgroundMaterial != null) imgSym.material = package.cinematic.backgroundMaterial; 
        }

        PlaySound(spellSound); t = 0;
        while (t < orbitDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; 
            if (symbolObj != null) {
                symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, t / orbitDur)); 
                if (package != null && package.cinematic.spinBackgroundSymbol)
                    symbolObj.transform.Rotate(0, 0, package.cinematic.backgroundSpinSpeed * Time.deltaTime * animSpeed); 
            } 
            yield return null; 
        }

        float waitT = 0;
        while (waitT < 0.3f) {
            if (card == null) { Cleanup(); yield break; }
            waitT += Time.deltaTime * animSpeed;
            yield return null;
        }

        PlaySound(fusionSound); 
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (package != null && package.cinematic.usePrefab && package.cinematic.flashPrefab != null) SpawnVFXPublic(package.cinematic.flashPrefab, worldCenter);
        if (useWhiteFlash) StartCoroutine(NativeWhiteFlashRoutine(uiParent, animSpeed, flashDur));

        cinCard = new GameObject("CinematicRitual", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false); cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>(); ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < appearDur) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / appearDur); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * cardScale, p);
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; 
        }

        float holdTimer = 0;
        while (holdTimer < holdDur) {
            if (card == null) { Cleanup(); yield break; }
            holdTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        float markerTimer = 0;
        while (markerTimer < delayMarker) {
            if (card == null) { Cleanup(); yield break; }
            markerTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        Transform targetZone = card.transform.parent != null ? card.transform.parent : card.transform;
        SpawnFieldMarker(targetZone, type);

        if (symbolObj != null) { Destroy(symbolObj); symbolObj = null; }
        
        float dropTimer = 0;
        while (dropTimer < delayDrop) {
            if (card == null) { Cleanup(); yield break; }
            dropTimer += Time.deltaTime * animSpeed;
            yield return null;
        }

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { 
            if (card == null) { Cleanup(); yield break; }
            t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f)); rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * cardScale, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; 
        }

        Cleanup();
        onComplete?.Invoke();
    }

    private IEnumerator NativeWhiteFlashRoutine(Transform uiParent, float animSpeed, float baseDuration)
    {
        GameObject flashOverlay = new GameObject("WhiteFlashOverlay", typeof(RectTransform), typeof(Image));
        flashOverlay.transform.SetParent(uiParent, false);
        flashOverlay.transform.SetAsLastSibling(); 
        
        RectTransform rtFlash = flashOverlay.GetComponent<RectTransform>();
        rtFlash.anchorMin = Vector2.zero; rtFlash.anchorMax = Vector2.one; 
        rtFlash.offsetMin = Vector2.zero; rtFlash.offsetMax = Vector2.zero;
        
        Image flashImg = flashOverlay.GetComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 1f); 

        float duration = baseDuration / animSpeed;
        float t = 0;
        while (t < duration) { t += Time.deltaTime; flashImg.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t / duration)); yield return null; }
        
        Destroy(flashOverlay);
    }

    public void PlayReturnToDeckAnimation(CardData data, bool isPlayer, CardLocation sourceLoc, Vector3? startPos, System.Action onComplete)
    {
        if (!enableAnimations) { onComplete?.Invoke(); return; }
        
        Vector3 sPos = startPos ?? (isPlayer ? GameManager.Instance.playerGraveyardDisplay.transform.position : GameManager.Instance.opponentGraveyardDisplay.transform.position);
        Vector3 ePos = isPlayer ? GameManager.Instance.playerDeckDisplay.transform.position : GameManager.Instance.opponentDeckDisplay.transform.position;
        
        CardFlightSettings settings = sourceLoc == CardLocation.Field ? flightFieldToDeck : flightPileToDeck;
        bool pop = sourceLoc != CardLocation.Field && sourceLoc != CardLocation.Hand;
        
        PlayCardFlight(data, GameManager.Instance.GetCardBackTexture(), true, true, sPos, ePos, GameManager.Instance.fieldCardScale, GameManager.Instance.fieldCardScale, Quaternion.identity, Quaternion.identity, settings, pop, onComplete);
    }

    public void PlayCardFlight(CardData data, Texture2D backTex, bool startFaceUp, bool endFaceUp, Vector3 startPos, Vector3 endPos, Vector3 startScale, Vector3 endScale, Quaternion startRot, Quaternion endRot, CardFlightSettings settings, bool popFromPile, System.Action onComplete)
    {
        if (!enableAnimations || settings == null || !settings.enableFlight) 

        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(CardFlightRoutine(data, backTex, startFaceUp, endFaceUp, startPos, endPos, startScale, endScale, startRot, endRot, settings, popFromPile, onComplete));
    }

    private IEnumerator CardFlightRoutine(CardData data, Texture2D backTex, bool startFaceUp, bool endFaceUp, Vector3 startPos, Vector3 endPos, Vector3 startScale, Vector3 endScale, Quaternion startRot, Quaternion endRot, CardFlightSettings settings, bool popFromPile, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        GameObject ghost = Instantiate(GameManager.Instance.cardPrefab, uiParent);
        CardDisplay ghostDisplay = ghost.GetComponent<CardDisplay>();
        ghostDisplay.SetCard(data, backTex, startFaceUp);
        ghostDisplay.isInteractable = false;
        
        LayoutElement le = ghost.GetComponent<LayoutElement>();
        if (le != null) Destroy(le);

        ghost.transform.SetAsLastSibling();
        PlaySound(spellSound);

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1f;
        Vector3 initialPos = startPos;
        
        if (popFromPile && settings.popOffset != Vector2.zero)
        {
            ghost.transform.position = startPos;
            ghost.transform.localScale = startScale * settings.startScaleMult;
            ghost.transform.rotation = startRot;

            float popDuration = 0.2f / animSpeed;
            float tPop = 0;
            
            // Aplica o Offset baseado no lado do jogador (esq/dir dependendo da pilha)
            float offsetX = startPos.x < Screen.width / 2f ? settings.popOffset.x : -settings.popOffset.x;
            Vector3 popTarget = startPos + new Vector3(offsetX, settings.popOffset.y, 0); 
            
            while (tPop < 1f)
            {
                tPop += Time.deltaTime / popDuration;
                float p = Mathf.SmoothStep(0, 1, tPop);
                Vector3 pos = Vector3.Lerp(startPos, popTarget, p);
                // Adiciona um pequeno arco
                pos.y += Mathf.Sin(p * Mathf.PI) * 15f;
                ghost.transform.position = pos;
                yield return null;
            }
            initialPos = popTarget;
        }

        float duration = settings.duration / animSpeed;
        float elapsed = 0f;
        
        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (settings.useTrail && settings.trailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("FlightLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(ghost.transform.parent, true);
            lineObj.transform.SetSiblingIndex(ghost.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = initialPos;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = settings.trailColor;
        }
        
        float spawnTrailTimer = 0;
        Vector3 finalStartScale = startScale * settings.startScaleMult;
        Vector3 finalEndScale = endScale * settings.endScaleMult;

        bool needsFlip = settings.flipDuringFlight && (startFaceUp != endFaceUp);
        bool hasFlipped = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            ghost.transform.position = Vector3.Lerp(initialPos, endPos, t);
            ghost.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * (settings.flightScale - 1f);
            ghost.transform.localScale = Vector3.Lerp(finalStartScale, finalEndScale, t) * scaleMultiplier;

            // Lógica de Flip no meio do voo
            if (needsFlip && !hasFlipped)
            {
                float flipStartT = 0.4f;
                float flipEndT = 0.6f;
                if (t >= flipStartT && t <= flipEndT)
                {
                    float flipProgress = (t - flipStartT) / (flipEndT - flipStartT);
                    float scaleY = Mathf.Cos(flipProgress * Mathf.PI); // Vai de 1 a -1
                    
                    ghost.transform.localScale = new Vector3(ghost.transform.localScale.x, ghost.transform.localScale.y * Mathf.Abs(scaleY), ghost.transform.localScale.z);

                    if (flipProgress >= 0.5f) {
                        hasFlipped = true;
                        ghostDisplay.ForceTexture(endFaceUp ? ghostDisplay.GetFrontTexture() : backTex);
                    }
                }
            }

            if (settings.useTrail) {
                if (settings.trailType == AttackTrailType.Shadows || settings.trailType == AttackTrailType.SmoothShadows) {
                    spawnTrailTimer -= Time.deltaTime; 
                    float interval = settings.trailType == AttackTrailType.SmoothShadows ? 0.015f : 0.04f;
                    if (spawnTrailTimer <= 0) { 
                        spawnTrailTimer = interval; 
                        SpawnCardTrailGhost(ghost.GetComponent<RectTransform>(), ghostDisplay.cardImage.texture, settings.trailColor, settings.trailType == AttackTrailType.SmoothShadows ? 0.15f : 0.3f); 
                    }
                } else if (settings.trailType == AttackTrailType.ContinuousLine && lineObj != null) {
                    Vector3 currentPos = ghost.transform.position; Vector3 dirToCurrent = currentPos - initialPos;
                    float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                    if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, settings.trailWidth);
                    lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                }
            }
            yield return null;
        }

        if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));

        ghost.transform.position = endPos;
        ghost.transform.rotation = endRot;
        ghost.transform.localScale = finalEndScale;

        if (settings.useImpact) 
        {
            if (settings.impactType == ControlSwapImpactType.Squeeze) yield return StartCoroutine(FlightImpactSqueezeRoutine(ghost, settings));
            else { StartCoroutine(PulseGhostRoutine(ghostDisplay, settings.impactScale, 0.2f, settings.impactColor, settings.impactOutlineWidth)); yield return new WaitForSeconds(0.2f); }
        }
        Destroy(ghost);
        onComplete?.Invoke();
    }

    private IEnumerator FlightImpactSqueezeRoutine(GameObject card, CardFlightSettings settings)
    {
        GameObject squeezeObj = new GameObject("FlightSqueeze", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        Transform parentToUse = card.transform.parent;
        squeezeObj.transform.SetParent(parentToUse, false);
        squeezeObj.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 

        RectTransform rt = squeezeObj.GetComponent<RectTransform>();
        RectTransform targetRT = card.GetComponent<RectTransform>();
        rt.position = targetRT.position; rt.sizeDelta = targetRT.sizeDelta;
        rt.rotation = targetRT.rotation; rt.pivot = targetRT.pivot;

        RawImage ri = squeezeObj.GetComponent<RawImage>();
        ri.texture = card.GetComponent<RawImage>().texture; ri.color = settings.impactColor; 
        
        Outline outline = squeezeObj.GetComponent<Outline>();
        outline.effectColor = settings.impactColor; outline.effectDistance = new Vector2(settings.impactOutlineWidth, -settings.impactOutlineWidth);

        float halfDuration = 0.2f / (animationSpeed > 0 ? animationSpeed : 1f); float t = 0;
        Vector3 startScale = card.transform.localScale * settings.impactScale; Vector3 endScale = card.transform.localScale;

        while (t < 1f) {
            if (card == null) break; t += Time.deltaTime / halfDuration; float smooth = Mathf.SmoothStep(0, 1, t);
            rt.position = targetRT.position; rt.localScale = Vector3.Lerp(startScale, endScale, smooth);
            if (smooth > 0.7f) {
                float fadeT = (smooth - 0.7f) / 0.3f;
                Color currentImgColor = settings.impactColor; currentImgColor.a = (1f - fadeT) * settings.impactColor.a; ri.color = currentImgColor;
                Color outlineColor = settings.impactColor; outlineColor.a = (1f - fadeT) * settings.impactColor.a; outline.effectColor = outlineColor;
            } yield return null;
        }
        if (squeezeObj != null) Destroy(squeezeObj);
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
            Debug.Log($"[VFX] ✨ [SPAWN] Instanciando Prefab de Partícula: '{prefab.name}' na posição {position}.");
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

    public Vector3 GetDirectAttackTargetPublic(CardDisplay attacker, Vector3 fallbackPos)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return fallbackPos;

        bool targetAvatar = targetAvatarOnDirectAttack;
        DuelFieldUI ui = GameManager.Instance.duelFieldUI;
        Vector3 targetPos = fallbackPos;

        if (attacker.isPlayerCard) // Player attacking Opponent
        {
            if (targetAvatar && ui.opponentAvatarImage != null) targetPos = ui.opponentAvatarImage.transform.position;
            else if (!targetAvatar && ui.opponentHand != null) targetPos = ui.opponentHand.position;
            else if (ui.opponentAvatarImage != null) targetPos = ui.opponentAvatarImage.transform.position;
        }
        else // Opponent attacking Player
        {
            if (targetAvatar && ui.playerAvatarImage != null) targetPos = ui.playerAvatarImage.transform.position;
            else if (!targetAvatar && ui.playerHand != null) targetPos = ui.playerHand.position;
            else if (ui.playerAvatarImage != null) targetPos = ui.playerAvatarImage.transform.position;
        }
        return targetPos;
    }

    public void PlayControlSwap(CardDisplay card, Transform targetZone, float targetZRot, bool newOwnerIsPlayer, System.Action onComplete)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayControlSwap (Change of Heart) | Alvo: {card?.CurrentCardData?.name} | Momento: Posse de controle transferida.");
        if (!enableAnimations || card == null || targetZone == null)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(ControlSwapRoutine(card, targetZone, targetZRot, newOwnerIsPlayer, onComplete));
    }

    private IEnumerator ControlSwapRoutine(CardDisplay card, Transform targetZone, float targetZRot, bool newOwnerIsPlayer, System.Action onComplete)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        
        Transform uiParent = GetUIParent();
        if (uiParent != null) { card.transform.SetParent(uiParent, true); card.transform.SetAsLastSibling(); }

        // Salva a escala que a Unity calculou para manter a carta com o mesmo tamanho visual na UI
        Vector3 flightScale = card.transform.localScale;

        PlaySound(spellSound);
        if (useControlSwapPrefab && spellActivateVFX != null) SpawnVFXPublic(spellActivateVFX, startPos);

        float duration = controlSwapDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float elapsed = 0f;
        Vector3 endPos = targetZone.position;
        Quaternion endRot = Quaternion.Euler(0, 0, targetZRot);
        
        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (useControlSwapTrail && controlSwapTrailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("SwapLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(card.transform.parent, true);
            lineObj.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = startPos;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = controlSwapTrailColor;
        }

        GameObject outlineGhost = null;
        if (useControlSwapOutline) {
            outlineGhost = new GameObject("SwapOutline", typeof(RectTransform), typeof(RawImage), typeof(Outline));
            outlineGhost.transform.SetParent(card.transform, false);
            outlineGhost.transform.SetAsFirstSibling(); 
            
            RectTransform rt = outlineGhost.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; 
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            
            RawImage ri = outlineGhost.GetComponent<RawImage>();
            ri.texture = card.cardImage.texture;
            ri.color = controlSwapOutlineColor; 
            
            Outline outl = outlineGhost.GetComponent<Outline>();
            outl.effectColor = controlSwapOutlineColor;
            outl.effectDistance = new Vector2(8f, -8f);
        }

        float spawnTrailTimer = 0;

        if (useControlSwapRoutine)
        {
            while (elapsed < duration)
            {
                if (card == null) break;
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                card.transform.position = Vector3.Lerp(startPos, endPos, t);
                card.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                
                float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * (controlSwapFlightScale - 1f);
                card.transform.localScale = flightScale * scaleMultiplier;

                if (useControlSwapTrail) {
                    if (controlSwapTrailType == AttackTrailType.Shadows) {
                        spawnTrailTimer -= Time.deltaTime; if (spawnTrailTimer <= 0) { spawnTrailTimer = 0.03f; SpawnCardTrailGhost(card.GetComponent<RectTransform>(), card.cardImage.texture, controlSwapTrailColor); }
                    } else if (controlSwapTrailType == AttackTrailType.ContinuousLine && lineObj != null) {
                        Vector3 currentPos = card.transform.position; Vector3 dirToCurrent = currentPos - startPos;
                        float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                        if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, controlSwapTrailWidth);
                        lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                    }
                }

                yield return null;
            }
        }
        else 
        {
            yield return new WaitForSeconds(0.6f / (animationSpeed > 0 ? animationSpeed : 1f));
        }

        if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));
        if (outlineGhost != null) Destroy(outlineGhost);

        if (card != null) { 
            card.transform.SetParent(targetZone, false); 
            card.transform.localPosition = Vector3.zero; 
            card.transform.localRotation = endRot; 
            
            card.transform.localScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one; 
            
            if (useControlSwapPrefab && summonVFX != null) SpawnVFXPublic(summonVFX, endPos); 
            PlaySound(summonSound); 
            
            if (useControlSwapImpact) 
            {
                if (controlSwapImpactType == ControlSwapImpactType.Squeeze)
                    StartCoroutine(ControlSwapImpactRoutine(card));
                else
                    StartCoroutine(PulseGhostRoutine(card, controlSwapImpactScale, 0.2f, controlSwapImpactColor, controlSwapImpactOutlineWidth));
            }
        }
        onComplete?.Invoke();
    }

    private void SpawnCardTrailGhost(RectTransform sourceRT, Texture tex, Color color, float duration = 0.3f)
    {
        GameObject ghost = new GameObject("CardTrailGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(sourceRT.parent, true);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        rt.pivot = sourceRT.pivot;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = tex; ri.color = color;
        
        StartCoroutine(FadeAndDestroyCardGhost(ghost, ri, duration));
    }

    private IEnumerator FadeAndDestroyCardGhost(GameObject obj, RawImage img, float duration)
    {
        float t = 0; Color startColor = img.color;
        Vector3 startScale = obj.transform.localScale;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator ControlSwapImpactRoutine(CardDisplay card)
    {
        GameObject squeezeObj = new GameObject("ControlSwapSqueeze", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        
        Transform parentToUse = card.transform.parent;
        squeezeObj.transform.SetParent(parentToUse, false);
        squeezeObj.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 

        RectTransform rt = squeezeObj.GetComponent<RectTransform>();
        RectTransform targetRT = card.GetComponent<RectTransform>();
        
        rt.position = targetRT.position;
        rt.sizeDelta = targetRT.sizeDelta;
        rt.rotation = targetRT.rotation;
        rt.pivot = targetRT.pivot;

        RawImage ri = squeezeObj.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture;
        ri.color = controlSwapImpactColor; 
        
        Outline outline = squeezeObj.GetComponent<Outline>();
        outline.effectColor = controlSwapImpactColor;
        outline.effectDistance = new Vector2(controlSwapImpactOutlineWidth, -controlSwapImpactOutlineWidth);

        float halfDuration = 0.2f / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        Vector3 startScale = card.transform.localScale * controlSwapImpactScale;
        Vector3 endScale = card.transform.localScale;

        while (t < 1f)
        {
            if (card == null) break;
            t += Time.deltaTime / halfDuration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            rt.position = targetRT.position; 
            rt.localScale = Vector3.Lerp(startScale, endScale, smooth);
            
            if (smooth > 0.7f)
            {
                float fadeT = (smooth - 0.7f) / 0.3f;
                Color currentImgColor = controlSwapImpactColor;
                currentImgColor.a = (1f - fadeT) * controlSwapImpactColor.a;
                ri.color = currentImgColor;
                
                Color outlineColor = controlSwapImpactColor;
                outlineColor.a = (1f - fadeT) * controlSwapImpactColor.a;
                outline.effectColor = outlineColor;
            }
            yield return null;
        }

        if (squeezeObj != null) Destroy(squeezeObj);
    }

    // --- INDICADORES DE STATUS (NATIVOS) ---
    private Dictionary<CardDisplay, GameObject> activeCanAttackIcons = new Dictionary<CardDisplay, GameObject>();
    private Dictionary<CardDisplay, GameObject> activeCannotAttackIcons = new Dictionary<CardDisplay, GameObject>();
    private Dictionary<CardDisplay, GameObject> activeCannotChangePosIcons = new Dictionary<CardDisplay, GameObject>();

    public void SetCanAttackIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, canAttackIndicator, activeCanAttackIcons, "CanAttackIcon");
    }

    public void SetCannotAttackIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, cannotAttackIndicator, activeCannotAttackIcons, "CannotAttackIcon");
    }

    public void SetCannotChangePosIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, cannotChangePosIndicator, activeCannotChangePosIcons, "CannotChangePosIcon");
    }

    private void SetStatusIndicator(CardDisplay card, bool show, StatusIndicatorSettings settings, Dictionary<CardDisplay, GameObject> dict, string name)
    {
        if (card == null) return;

        if (show)
        {
            if (!dict.ContainsKey(card) || dict[card] == null)
            {
                GameObject iconObj = null;
                
                if (settings.usePrefab && settings.prefab != null)
                {
                    iconObj = Instantiate(settings.prefab, card.transform);
                    iconObj.name = name;
                    iconObj.transform.SetAsLastSibling();
                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    if (rt != null) {
                        rt.anchoredPosition = settings.offset;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    } else {
                        iconObj.transform.localPosition = new Vector3(settings.offset.x, settings.offset.y, 0);
                        iconObj.transform.localScale = Vector3.one;
                        iconObj.transform.localRotation = Quaternion.identity;
                    }
                }
                else if (settings.useNative && settings.sprite != null)
                {
                    iconObj = new GameObject(name, typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(card.transform, false);
                    iconObj.transform.SetAsLastSibling();

                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    rt.anchoredPosition = settings.offset;
                    rt.sizeDelta = settings.size;

                    Image img = iconObj.GetComponent<Image>();
                    img.sprite = settings.sprite;
                    img.color = settings.color;
                    img.preserveAspect = settings.preserveAspect;
                    if (settings.material != null) img.material = settings.material;

                    if (settings.useOutline)
                    {
                        Outline outline = iconObj.AddComponent<Outline>();
                        outline.effectColor = settings.outlineColor;
                        outline.effectDistance = new Vector2(settings.outlineWidth, -settings.outlineWidth);
                    }

                    if (settings.blinkSpeed > 0)
                    {
                        VfxAutoAnim anim = iconObj.AddComponent<VfxAutoAnim>();
                        anim.duration = 9999f;
                        anim.fadeType = VfxAutoAnim.FadeType.Blink;
                        anim.blinkSpeed = settings.blinkSpeed;
                        anim.scaleType = VfxAutoAnim.ScaleType.None;
                    }
                }

                if (iconObj != null) dict[card] = iconObj;
            }
        }
        else
        {
            if (dict.ContainsKey(card) && dict[card] != null)
            {
                Destroy(dict[card]);
                dict.Remove(card);
            }
        }
    }
}
