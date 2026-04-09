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

public enum AttackTrailType { Shadows, ContinuousLine }
public enum ControlSwapImpactType { Squeeze, Pulse }

public class DuelFXManager : MonoBehaviour
{
    public static DuelFXManager Instance;

    [Header("Configurações Globais")]
    public bool enableAnimations = true;
    public float animationSpeed = 1.5f;

    [Header("Referências da Cena")]
    public Transform boardCenter; // Arraste um objeto vazio no centro do campo
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
    [Tooltip("Material Aditivo para ignorar o fundo preto dos sprites.")]
    public Material chainLinkMaterial;
    public bool useChainLinkPrefab = true;

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
    [Tooltip("Move as cartas para o centro da tela durante o embaralhamento.")]
    public bool shuffleAtCenter = false;
    public float shuffleDuration = 0.8f; // Tempo total da animação
    [Tooltip("Instancia o prefab definido em 'Shuffle VFX'. Útil para usar sua Sprite Sheet Animada.")]
    public bool useShufflePrefab = true;

    [Header("Opções de Invocação de Ficha (Token)")]
    [Tooltip("A carta da Ficha (Token) treme e faz um Fade In ao surgir.")]
    public bool useTokenSummonRoutine = true;
    public float tokenSummonDuration = 0.8f; // Sincronizado com a Sprite Sheet de fumaça
    [Tooltip("A cor inicial da carta enquanto ela faz o Fade In (Padrão: Cinza/Escuro).")]
    public Color tokenSummonFadeColor = Color.gray; 
    [Tooltip("Instancia o prefab definido em 'Token Summon VFX'. Ideal para usar com a fumaça 2D.")]
    public bool useTokenSummonPrefab = true;

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

    [Header("Opções de Flip (Revelação)")]
    [Tooltip("Usa a rotina nativa para adicionar um Pulse ao final do Flip.")]
    public bool useFlipRoutine = true;
    public float flipVfxDuration = 0.3f;
    public bool useFlipPulse = true;
    public float flipPulseScale = 1.2f;
    public float flipPulseDuration = 0.2f;
    public float flipPulseOutlineWidth = 8f;
    public Color flipPulseColor = Color.white;
    [Tooltip("Instancia o Prefab de Flip definido nos Slots visuais.")]
    public bool useFlipPrefab = true;

    [Header("Opções de Ataque (Combate)")]
    [Tooltip("Usa o prefab 'TargetingSwordUI' para mirar ataques. Se desmarcado, usa cores de outline (Hover).")]
    public bool useTargetingSwordPrefab = true;
    public Color colorAttackReadyHover = new Color(1f, 0.5f, 0f, 1f); // Laranja (Pronto para Atacar)
    public Color colorAttackTargetHover = Color.red; // Vermelho (Alvo Inimigo)
    [Tooltip("Habilita o efeito de escurecer a carta ou pintar a borda ao selecioná-la para atacar.")]
    public bool enableAttackSelectionVisual = true;
    public Color colorAttackSelection = Color.red; // Cor do hover/borda de atacante selecionado
    [Tooltip("Se marcado, ataques diretos visam o avatar do oponente. Se desmarcado, visam o centro da mão/campo.")]
    public bool targetAvatarOnDirectAttack = true;
    [Tooltip("A carta dá um 'bote' (recua e avança) no momento do ataque.")]
    public bool useAttackHeadbutt = true;
    [Tooltip("Gera a espada/projétil voador dinamicamente. Se desmarcado, usará o Prefab 'Attack Projectile VFX'.")]
    public bool useAttackSword = true;
    public Sprite attackSwordSprite;
    public Color attackSwordColor = Color.white;
    public Vector2 attackSwordSize = new Vector2(60, 60);
    [Tooltip("Material para a espadinha nativa (Use o VFX_Additive_UI para fundos pretos desaparecerem).")]
    public Material attackSwordMaterial;
    [Tooltip("Usa o Prefab de projétil caso não queira usar a rotina nativa da espada.")]
    public bool useAttackProjectilePrefab = true;
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

    [Header("Opções de Ícone de Tributo (Tribute Icon)")]
    public bool useTributeIcon = true;
    public bool useNativeTributeIcon = true;
    public Color tributeIconColor = new Color(1f, 0.5f, 0f, 1f);
    public float tributeIconBlinkFrequency = 5f;
    public float tributeIconSize = 80f;
    public float tributeIconDuration = 1.0f;
    public bool useTributeIconPrefab = true;

    [Header("Cinemáticas de Invocação (Etapa 2)")]
    [Tooltip("Ícone de fogo/alma que fica sobre os monstros selecionados para sacrifício.")]
    public Sprite tributeIconSprite;
    public GameObject tributeFieldMarkerVFX; // Marcação que surge no campo no Tribute Summon
    public GameObject specialFieldMarkerVFX; // Marcação (Ritual, Fusão, etc) no campo

    [Header("Opções de Field Markers (Marcadores de Campo)")]
    public bool useFieldMarkers = true;
    public bool useNativeFieldMarker = true;
    public Color specialMarkerColor = new Color(0.8f, 0.2f, 1f, 0.5f);
    public Color fusionMarkerColor = new Color(0.6f, 0.1f, 0.9f, 0.5f);
    public Color ritualMarkerColor = new Color(0.1f, 0.3f, 1f, 0.5f);
    public float markerDuration = 1.0f;
    public float markerStartSize = 0.1f;
    public float markerEndSize = 2.0f;
    public bool useFieldMarkerPrefabs = true;

    [Header("Cinemáticas de Invocação (Etapa 3 - Fusão e Ritual)")]
    public GameObject fusionFlashVFX; // Clarão branco com raios amarelos
    public Sprite fusionBackgroundSymbol; // Símbolo da fusão (Imagem de fundo)
    public Sprite ritualBackgroundSymbol; // Símbolo do ritual (Imagem de fundo)
    public GameObject ritualFlashVFX; // Clarão do ritual
    public GameObject fusionFieldMarkerVFX; // Marcação no campo (pouso da fusão)
    public GameObject ritualFieldMarkerVFX; // Marcação no campo (pouso do ritual)

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
        if (useAttackSword)
        {
            PlaySound(attackTravelSound);
            
            GameObject proj = new GameObject("AttackSword", typeof(RectTransform), typeof(Image));
            Transform uiParent = GetUIParent();
            if (uiParent != null) proj.transform.SetParent(uiParent, true);
            
            RectTransform rt = proj.GetComponent<RectTransform>();
            rt.position = startPos; rt.sizeDelta = attackSwordSize;
            
            Image img = proj.GetComponent<Image>();
            img.color = attackSwordColor;
            if (attackSwordSprite != null) img.sprite = attackSwordSprite;
            else img.color = Color.clear; // Esconde o quadrado branco se esquecer a imagem!
            
            if (attackSwordMaterial != null) img.material = attackSwordMaterial;

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
                if (attackSwordMaterial != null) lineImg.material = attackSwordMaterial;
            }

            float duration = attackFlightDuration / animSpeed; float t = 0; float spawnTrailTimer = 0;
            
            while (t < 1f) {
                if (proj == null || rt == null) break;
                t += Time.deltaTime / duration;
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                rt.position = currentPos;
                
                if (useAttackTrail) {
                    if (attackTrailType == AttackTrailType.Shadows && attackSwordSprite != null) {
                        spawnTrailTimer -= Time.deltaTime;
                        if (spawnTrailTimer <= 0) {
                            spawnTrailTimer = 0.015f; 
                            SpawnSwordGhost(rt, img.sprite, attackSwordMaterial);
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
        else if (useAttackProjectilePrefab && attackProjectileVFX != null)
        {
            PlaySound(attackTravelSound);
            GameObject proj = SpawnVFXPublic(attackProjectileVFX, startPos);
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
                            SpawnSwordGhost(rt, img.sprite);
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
        if (useAttackImpactVFX && attackVFX != null) SpawnVFXPublic(attackVFX, targetPos);
        
        if (target != null) PlayCardShake(target);
        
        yield return null; 
        Debug.Log($"[VFX] ✔ [ROUTINE CONCLUÍDA] AttackRoutine finalizada.");
        onHit?.Invoke();
    }

    private void SpawnSwordGhost(RectTransform sourceRT, Sprite sprite, Material mat = null)
    {
        GameObject ghost = new GameObject("SwordGhost", typeof(RectTransform), typeof(Image));
        ghost.transform.SetParent(sourceRT.parent, true);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        
        Image img = ghost.GetComponent<Image>();
        img.sprite = sprite; img.color = attackTrailColor;
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
            // Afina o rastro para criar o efeito "Swoosh" de golpe de espada!
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, t);
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

    public void PlayAttackFail(CardDisplay attacker)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayAttackFail | Alvo (Atacante): {attacker?.CurrentCardData?.name} | Momento: ATK <= DEF do alvo (Ricochete).");
        PlaySound(reflectSound);
        SpawnVFXPublic(reflectVFX, attacker.transform.position);
    }

    // --- INVOCAÇÃO / TRIBUTO ---

    public void PlaySummonEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlaySummonEffect (Impacto) | Alvo: {card?.CurrentCardData?.name} | Momento: Monstro acaba de ser invocado com sucesso.");
        PlaySound(summonSound);
        SpawnVFXPublic(summonVFX, card.transform.position);
    }

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

        if (useTokenSummonRoutine) cg.alpha = 0f; // Esconde a carta inicialmente

        // Instancia a sua fumaça (Dust_2D)
        if (useTokenSummonPrefab)
        {
            GameObject prefab = tokenSummonVFX != null ? tokenSummonVFX : summonVFX;
            if (prefab != null) SpawnVFXPublic(prefab, card.transform.position);
        }

        if (useTokenSummonRoutine)
        {
            float duration = tokenSummonDuration / (animationSpeed > 0 ? animationSpeed : 1f);
            float t = 0;
            Vector3 originalPos = card.transform.position;
            
            // Controle de cor para evitar o "Flash Branco" e materializar suavemente das sombras
            RawImage ri = card.cardImage;
            Color targetColor = Color.white;
            if (ri != null) { targetColor = ri.color; ri.color = tokenSummonFadeColor; }

            while (t < 1f)
            {
                if (card == null) yield break;
                t += Time.deltaTime / duration;
                
                float shake = Mathf.Lerp(6f, 0f, t); // Tremor violento que vai enfraquecendo
                card.transform.position = originalPos + new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0);
                
                if (t > 0.4f) 
                {
                    float p = (t - 0.4f) / 0.6f;
                    cg.alpha = Mathf.Lerp(0f, 1f, p); // Fade In apenas após 40% da fumaça
                    if (ri != null) ri.color = Color.Lerp(tokenSummonFadeColor, targetColor, p);
                }
                yield return null;
            }
            if (card != null) { card.transform.position = originalPos; cg.alpha = 1f; if (ri != null) ri.color = targetColor; }
        }
    }

    public void PlayTributeSummonEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayTributeSummonEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Invocação por Tributo aterrissou no campo.");
        PlaySound(summonSound); // Pode ter um som específico se quiser
        // Usa o prefab específico se existir, senão usa o padrão do GameManager, senão o de summon comum
        GameObject prefab = tributeSummonVFX != null ? tributeSummonVFX : summonVFX;
        SpawnVFXPublic(prefab, card.transform.position);
    }

    public void PlayFusionEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayFusionEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Fusão instanciada visualmente.");
        PlaySound(fusionSound);
        SpawnVFXPublic(fusionVFX, card.transform.position);
    }

    public void PlayTributeEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayTributeEffect (Alma) | Alvo (Sacrifício): {card?.CurrentCardData?.name} | Momento: Selecionado/Enviado ao GY como custo de Tributo.");
        if (!enableAnimations || card == null) return;
        PlaySound(tributeSound);
        
        if (useTributeIcon)
        {
            if (useTributeIconPrefab && tributeVFX != null)
            {
                GameObject vfx = SpawnVFXPublic(tributeVFX, card.transform.position);
                if (vfx != null) vfx.transform.SetParent(card.transform);
            }
            if (useNativeTributeIcon && tributeIconSprite != null)
            {
                StartCoroutine(NativeTributeIconRoutine(card));
            }
        }
    }

    private IEnumerator NativeTributeIconRoutine(CardDisplay card)
    {
        GameObject iconObj = new GameObject("NativeTributeIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform rt = iconObj.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(tributeIconSize, tributeIconSize);
        
        Image img = iconObj.GetComponent<Image>();
        img.sprite = tributeIconSprite;
        img.color = tributeIconColor;
        
        float t = 0; float duration = tributeIconDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        
        while (t < 1f) {
            if (card == null || iconObj == null) break;
            t += Time.deltaTime / duration;
            float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI * tributeIconBlinkFrequency));
            Color c = tributeIconColor; c.a = alpha * tributeIconColor.a; img.color = c;
            yield return null;
        }
        if (iconObj != null) Destroy(iconObj);
    }

    // --- NOVOS EFEITOS DE BATALHA E JOGO ---

    public void PlayBanishEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayBanishEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Carta Removida de Jogo (Banida).");
        if (!enableAnimations || card == null) return;
        PlaySound(banishSound);
        AnimateCardDeath(card, true); // Morte por Sugador (Fantasmas)
        // Instancia o VFX por ÚLTIMO para garantir que o Vórtice cubra a carta no Canvas 2D
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
        float t = 0; Vector3 startScale = ghost.transform.localScale; RawImage ri = ghost.GetComponent<RawImage>();
        while(t < 1f) {
            if (ghost == null) yield break;
            t += Time.deltaTime * 2.5f; // Rápido!
            ghost.transform.Rotate(0, 0, 720f * Time.deltaTime); // Gira violentamente para o centro
            ghost.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            ri.color = new Color(0.5f, 0.5f, 0.5f, 1f - t); // Fica cinza e esmaece
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
        PlaySound(damageSound);
        SpawnVFXPublic(damageVFX, position);
    }

    public void PlayDefenseSuccessEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayDefenseSuccessEffect (Escudo) | Alvo (Defensor): {card?.CurrentCardData?.name} | Momento: Sobreviveu ao ataque.");
        PlaySound(defenseSound);
        SpawnVFXPublic(defenseSuccessVFX, card.transform.position);
    }

    public void PlayCardShake(CardDisplay card)
    {
        if (!enableAnimations || card == null) return;
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayCardShake | Alvo: {card.CurrentCardData?.name} | Momento: Tremedeira de impacto físico.");
        StartCoroutine(ShakeRoutine(card.transform));
    }

    private IEnumerator ShakeRoutine(Transform target)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0f;
        float duration = 0.3f; // Tremedeira super rápida
        float magnitude = 15f; // Intensidade (pixels de deslocamento)

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
        textContainerRT.anchoredPosition = new Vector2(0, -25);
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
            leftRT.sizeDelta = new Vector2(60, 60);
            leftRT.anchoredPosition = new Vector2(-100, 25);
            leftImg = leftObj.GetComponent<Image>();
            leftImg.sprite = chainLinkLeftSprite;
            if (chainLinkMaterial != null) leftImg.material = chainLinkMaterial;

            GameObject rightObj = new GameObject("RightLink", typeof(RectTransform), typeof(Image));
            rightObj.transform.SetParent(chainObj.transform, false);
            rightRT = rightObj.GetComponent<RectTransform>();
            rightRT.sizeDelta = new Vector2(60, 60);
            rightRT.anchoredPosition = new Vector2(100, 25);
            rightImg = rightObj.GetComponent<Image>();
            
            if (chainLinkRightSprite != null) rightImg.sprite = chainLinkRightSprite;
            else { rightImg.sprite = chainLinkLeftSprite; rightRT.localScale = new Vector3(-1, 1, 1); }
            if (chainLinkMaterial != null) rightImg.material = chainLinkMaterial;

            if (chainLinkJoinedSprite != null)
            {
                GameObject joinedObj = new GameObject("JoinedLink", typeof(RectTransform), typeof(Image));
                joinedObj.transform.SetParent(chainObj.transform, false);
                RectTransform joinedRT = joinedObj.GetComponent<RectTransform>();
                joinedRT.sizeDelta = new Vector2(90, 60);
                joinedRT.anchoredPosition = new Vector2(0, 25);
                joinedImg = joinedObj.GetComponent<Image>();
                joinedImg.sprite = chainLinkJoinedSprite;
                if (chainLinkMaterial != null) joinedImg.material = chainLinkMaterial;
                joinedImg.enabled = false;
            }
        }

        float duration = chainLinkDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        Vector2 startAnchored = chainRT.anchoredPosition;
        Vector2 endAnchored = startAnchored + new Vector2(0, 120f); 
        
        bool isClashed = false;

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
                if (joinedImg != null) joinedImg.color = new Color(1, 1, 1, alpha);
                if (leftImg != null) leftImg.color = new Color(1, 1, 1, alpha);
                if (rightImg != null) rightImg.color = new Color(1, 1, 1, alpha);
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

    private IEnumerator PulseGhostRoutine(CardDisplay card, float targetScaleMult, float halfDuration, Color outlineColor, float outlineWidth)
    {
        if (card == null) yield break;

        Vector3 originalScale = card.transform.localScale;
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

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.GetFrontTexture() != null ? card.GetFrontTexture() : card.cardImage.texture; 
        ri.color = new Color(1f, 1f, 1f, 0.8f);
        
        Outline outline = ghost.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);

        float durationActual = halfDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        while (t < 1f) {
            if (card == null) break;
            t += Time.deltaTime / durationActual; float smooth = Mathf.SmoothStep(0, 1, t);
            card.transform.localScale = Vector3.Lerp(originalScale, peakScale, smooth);
            if (ghost != null) { ghost.transform.localScale = Vector3.Lerp(originalScale, peakScale * 1.15f, smooth); ri.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, smooth)); }
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

        Color targetColor = GetAuraColor(card);

        if (usePlacementAuraRoutine)
        {
            StartCoroutine(PlacementAuraRoutine(card, targetColor));
        }

        if (!usePlacementAuraPrefab || placementAuraVFX == null) return;
        
        GameObject vfx = Instantiate(placementAuraVFX);
        
        Transform originalParent = card.transform.parent;
        if (originalParent != null)
        {
            vfx.transform.SetParent(originalParent, false);
            vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // ATRÁS da carta
            
            RectTransform rt = vfx.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = card.transform.localRotation; // Acompanha 90º
            }
            else
            {
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = card.transform.localRotation;
            }
        }
        else
        {
            vfx.transform.position = card.transform.position;
            vfx.transform.rotation = card.transform.rotation;
        }

        ParticleSystemRenderer[] renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (renderers.Length > 0)
        {
            Canvas rootCanvas = card.GetComponentInParent<Canvas>();
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder - 1 : -1; // Atrás da carta 2D
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
        }

        if (colorizeAuraByType)
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

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        float destroyTime = ps != null ? (ps.main.duration + ps.main.startLifetime.constantMax + 0.5f) : 3.0f;
        Destroy(vfx, destroyTime);
        
        Debug.Log($"[VFX] ✔ [AURA INSTANCIADA] '{placementAuraVFX.name}' renderizada atrás da carta. Vida: {destroyTime}s.");
    }

    private Color GetAuraColor(CardDisplay card)
    {
        if (!colorizeAuraByType || card == null || card.CurrentCardData == null) return colorMonsterNormal;
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
        ri.texture = card.GetFrontTexture() != null ? card.GetFrontTexture() : card.cardImage.texture; 
        
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
        ri.texture = source.GetFrontTexture() != null ? source.GetFrontTexture() : source.cardImage.texture; 
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
        ri.texture = target.GetFrontTexture() != null ? target.GetFrontTexture() : target.cardImage.texture;
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
        
        if (useShufflePrefab && shuffleVFX != null)
        {
            Vector3 spawnPos = shuffleAtCenter && boardCenter != null ? boardCenter.position : pileTransform.position;
            SpawnVFXPublic(shuffleVFX, spawnPos);
        }

        if (useShuffleRoutine)
        {
            StartCoroutine(ShuffleRoutine(pileTransform));
        }
        else
        {
            PlaySound(shuffleSound);
        }
    }

    private IEnumerator ShuffleRoutine(Transform pileTransform)
    {
        PlaySound(shuffleSound);

        Transform uiParent = GetUIParent();
        if (uiParent == null) yield break;

        int deckSize = 6; // Cria 6 cartas fakes para criar o volume do baralho
        List<GameObject> fakeCards = new List<GameObject>();
        
        Vector3 startPos = pileTransform.position;
        Vector3 centerPos = shuffleAtCenter && boardCenter != null ? boardCenter.position : startPos;
        
        // Tenta capturar o tamanho e escala do slot do baralho original
        RectTransform pileRT = pileTransform.GetComponent<RectTransform>();

        for (int i = 0; i < deckSize; i++)
        {
            GameObject card = new GameObject($"FakeCard_{i}", typeof(RectTransform), typeof(RawImage));
            
            // Herda âncoras primeiro para não distorcer a escala
            if (pileRT != null && pileRT.parent != null)
            {
                card.transform.SetParent(pileRT.parent, false);
                RectTransform rt = card.GetComponent<RectTransform>();
                rt.anchorMin = pileRT.anchorMin; rt.anchorMax = pileRT.anchorMax; rt.pivot = pileRT.pivot;
                rt.sizeDelta = pileRT.sizeDelta; rt.localRotation = pileRT.localRotation; rt.localScale = pileRT.localScale;
            }

            card.transform.SetParent(uiParent, true); // Joga pro Canvas Master mantendo o tamanho World Space
            card.transform.SetAsLastSibling();

            RawImage ri = card.GetComponent<RawImage>();
            ri.texture = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;

            fakeCards.Add(card);
        }

        float halfDuration = (shuffleDuration / 2f) / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;

        // Fase 1: Levanta e Divide o Baralho em Leque
        while (t < 1f)
        {
            t += Time.deltaTime / halfDuration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            Vector3 currentBasePos = Vector3.Lerp(startPos, centerPos, smooth);

            for (int i = 0; i < deckSize; i++)
            {
                if (fakeCards[i] == null) continue;
                
                // Metade das cartas vai pra esquerda, metade pra direita
                float xTargetOffset = (i % 2 == 0) ? -60f : 60f;
                float yTargetOffset = Mathf.Sin(smooth * Mathf.PI) * 40f; // Efeito de parábola subindo
                
                // Adiciona um leque (espelhamento)
                float rotationZ = (i % 2 == 0) ? Mathf.Lerp(0, 20f, smooth) : Mathf.Lerp(0, -20f, smooth);
                
                fakeCards[i].transform.position = currentBasePos + new Vector3(xTargetOffset * smooth, yTargetOffset * smooth, 0);
                fakeCards[i].transform.rotation = Quaternion.Euler(0, 0, pileTransform.eulerAngles.z + rotationZ);

                if (shuffleAtCenter)
                {
                    float scaleMult = 1f + (smooth * 0.5f); // Cresce 50% no centro
                    fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                }
            }
            yield return null;
        }

        PlaySound(shuffleSound); // Barulho de mesclar as cartas

        // Fase 2: Une as metades entrelaçando e desce
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
                
                // Mistura as camadas alternando a hierarquia (Zipper effect)
                int siblingTarget = fakeCards[i].transform.parent.childCount - (i % 2 == 0 ? i : deckSize - i);
                fakeCards[i].transform.SetSiblingIndex(siblingTarget);

                fakeCards[i].transform.position = currentBasePos + new Vector3(currentXOffset, 0, 0);
                fakeCards[i].transform.rotation = Quaternion.Euler(0, 0, pileTransform.eulerAngles.z + rotationZ);

                if (shuffleAtCenter)
                {
                    float scaleMult = 1f + ((1f - smooth) * 0.5f); // Encolhe de volta
                    fakeCards[i].transform.localScale = (GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one) * scaleMult;
                }
            }
            yield return null;
        }

        foreach (var c in fakeCards) Destroy(c);
    }

    public void PlaySummonCinematic(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlaySummonCinematic | Alvo: {card?.CurrentCardData?.name} | Momento: Invocação Maior aterrissando na mesa.");
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(SummonCinematicRoutine(card, isTribute, isSpecial, onComplete));
    }

    private void SpawnFieldMarker(Vector3 targetPos, Color markerColor, GameObject prefabMarker)
    {
        if (!useFieldMarkers) return;
        if (useFieldMarkerPrefabs && prefabMarker != null) SpawnVFXPublic(prefabMarker, targetPos);
        if (useNativeFieldMarker) StartCoroutine(NativeFieldMarkerRoutine(targetPos, markerColor));
    }

    private IEnumerator NativeFieldMarkerRoutine(Vector3 pos, Color color)
    {
        Transform uiParent = GetUIParent();
        GameObject marker = new GameObject("NativeFieldMarker", typeof(RectTransform), typeof(Image));
        if (uiParent != null) marker.transform.SetParent(uiParent, true);
        
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.position = pos; rt.sizeDelta = new Vector2(200, 200); 
        
        Image img = marker.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        Outline outline = marker.AddComponent<Outline>();
        outline.effectColor = color; outline.effectDistance = new Vector2(5f, -5f);
        
        float t = 0; float duration = markerDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        while (t < 1f) {
            if (marker == null) break;
            t += Time.deltaTime / duration; float smooth = Mathf.SmoothStep(0, 1, t);
            rt.localScale = Vector3.Lerp(Vector3.one * markerStartSize, Vector3.one * markerEndSize, smooth);
            Color c = color; c.a = Mathf.Lerp(color.a, 0f, smooth); outline.effectColor = c;
            yield return null;
        } 
        if (marker != null) Destroy(marker);
    }

    private IEnumerator SummonCinematicRoutine(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        GameObject darkOverlay = null;
        Image darkImg = null;
        
        // 1. Fundo Escurecido
        darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
        darkOverlay.transform.SetParent(uiParent, false);
        RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
        rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
        rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
        darkImg = darkOverlay.GetComponent<Image>();
        darkImg.color = new Color(0, 0, 0, 0f);

        // 2. Carta Gigante Deslumbrante
        GameObject cinCard = new GameObject("CinematicCard", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        
        RectTransform rt = cinCard.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 290f);
        
        RawImage ri = cinCard.GetComponent<RawImage>();
        
        // REGRA DE OURO: Se for oponente, NÃO DA SPOILER! Mostra o verso caindo!
        bool showBack = !card.isPlayerCard;
        if (showBack && GameManager.Instance != null) ri.texture = GameManager.Instance.GetCardBackTexture();
        else ri.texture = card.cardImage.texture;

        PlaySound(summonSound);
        float t = 0; float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f;

        // FADE IN
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / 0.3f);
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p);
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.7f, p));
            yield return null; }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        SpawnFieldMarker(targetPos, isTribute ? specialMarkerColor : specialMarkerColor, isTribute ? tributeFieldMarkerVFX : specialFieldMarkerVFX);

        yield return new WaitForSeconds(0.6f / animSpeed); // SUSPENSE

        // 3. Marca no Chão e Pouso
        PlaySound(attackTravelSound);

        Vector3 startPos = rt.position; 
        Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p);
            rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); // Rotação final (ex: deitar p/ Defesa)
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.7f, 0f, p));
            yield return null; }

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay);
        onComplete?.Invoke();
    }

    public void PlayFusionCinematic(CardDisplay card, List<CardData> materials, CardData polyCard, System.Action onComplete)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayFusionCinematic | Alvo: {card?.CurrentCardData?.name} | Momento: Materiais enviados pro GY, Magia de Fusão resolvida.");
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(FusionCinematicRoutine(card, materials, polyCard, onComplete));
    }

    private IEnumerator FusionCinematicRoutine(CardDisplay card, List<CardData> materials, CardData polyCard, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        GameObject darkOverlay = null; Image darkImg = null;
        
        darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
        darkOverlay.transform.SetParent(uiParent, false);
        RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
        rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
        rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
        darkImg = darkOverlay.GetComponent<Image>();
        darkImg.color = new Color(0, 0, 0, 0f);

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / 0.3f)); yield return null; }

        GameObject symbolObj = null;
        if (fusionBackgroundSymbol != null) { 
            symbolObj = new GameObject("FusionSymbol", typeof(RectTransform), typeof(Image)); 
            symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(400f, 400f);
            Image imgSym = symbolObj.GetComponent<Image>(); imgSym.sprite = fusionBackgroundSymbol; imgSym.color = new Color(1, 1, 1, 0); 
        }

        List<RectTransform> matRects = new List<RectTransform>();
        if (materials != null && materials.Count > 0 && GameManager.Instance != null)
        {
            foreach (var mat in materials)
            {
                GameObject matObj = new GameObject("MatCard", typeof(RectTransform), typeof(RawImage));
                matObj.transform.SetParent(uiParent, false);
                RectTransform rtMat = matObj.GetComponent<RectTransform>(); 
                rtMat.anchorMin = new Vector2(0.5f, 0.5f); rtMat.anchorMax = new Vector2(0.5f, 0.5f);
                rtMat.anchoredPosition = Vector2.zero; rtMat.sizeDelta = new Vector2(100f, 145f);
                matObj.GetComponent<RawImage>().texture = GameManager.Instance.GetCardBackTexture();
                matRects.Add(rtMat);
            }
        }

        if (matRects.Count > 0)
        {
            PlaySound(spellSound);
            float orbitDuration = 1.5f; float orbitTime = 0f;
            while (orbitTime < orbitDuration)
            {
                orbitTime += Time.deltaTime * animSpeed; float progress = orbitTime / orbitDuration;
                float radius = Mathf.Lerp(300f, 0f, progress * progress); float angleSpeed = 360f * 3f;
                if (symbolObj != null) {
                    symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, progress)); 
                    symbolObj.transform.Rotate(0, 0, -45f * Time.deltaTime * animSpeed); // Gira o universo!
                }
                for (int i = 0; i < matRects.Count; i++) {
                    float offsetAngle = (360f / matRects.Count) * i; float currentAngle = (orbitTime * angleSpeed) + offsetAngle;
                    float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad) * radius; float y = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * radius;
                    matRects[i].anchoredPosition = new Vector2(x, y); matRects[i].rotation = Quaternion.Euler(0, 0, currentAngle);
                }
                yield return null;
            }
        }
        else yield return new WaitForSeconds(0.5f);

        foreach (var m in matRects) Destroy(m.gameObject);
        PlaySound(fusionSound);
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (fusionFlashVFX != null) SpawnVFXPublic(fusionFlashVFX, worldCenter);

        GameObject cinCard = new GameObject("CinematicFusion", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false);
        cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>();
        ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / 0.3f); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p); 
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; }
        
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        SpawnFieldMarker(targetPos, fusionMarkerColor, fusionFieldMarkerVFX != null ? fusionFieldMarkerVFX : specialFieldMarkerVFX);

        yield return new WaitForSeconds(0.6f / animSpeed);

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, targetScale, p); rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; }

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay); if (symbolObj != null) Destroy(symbolObj);
        onComplete?.Invoke();
    }

    public void PlayRitualCinematic(CardDisplay card, CardData ritualSpell, System.Action onComplete)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayRitualCinematic | Alvo: {card?.CurrentCardData?.name} | Momento: Tributos enviados, Magia de Ritual resolvida.");
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(RitualCinematicRoutine(card, ritualSpell, onComplete));
    }

    private IEnumerator RitualCinematicRoutine(CardDisplay card, CardData ritualSpell, System.Action onComplete)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) { onComplete?.Invoke(); yield break; }

        GameObject darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image)); 
        darkOverlay.transform.SetParent(uiParent, false);
        RectTransform rtDark = darkOverlay.GetComponent<RectTransform>(); 
        rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; 
        rtDark.offsetMin = Vector2.zero; rtDark.offsetMax = Vector2.zero;
        Image darkImg = darkOverlay.GetComponent<Image>(); darkImg.color = new Color(0, 0, 0, 0f);

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / 0.3f)); yield return null; }

        GameObject symbolObj = null;
        if (ritualBackgroundSymbol != null) { symbolObj = new GameObject("RitualSymbol", typeof(RectTransform), typeof(Image)); symbolObj.transform.SetParent(uiParent, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); 
            rtSym.anchorMin = new Vector2(0.5f, 0.5f); rtSym.anchorMax = new Vector2(0.5f, 0.5f);
            rtSym.anchoredPosition = Vector2.zero; rtSym.sizeDelta = new Vector2(400f, 400f);
            Image imgSym = symbolObj.GetComponent<Image>(); imgSym.sprite = ritualBackgroundSymbol; imgSym.color = new Color(1, 1, 1, 0); }

        PlaySound(spellSound); t = 0;
        while (t < 1.0f) { t += Time.deltaTime * animSpeed; if (symbolObj != null) {
                symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, t)); symbolObj.transform.Rotate(0, 0, 45f * Time.deltaTime * animSpeed); } yield return null; }

        yield return new WaitForSeconds(0.3f); PlaySound(fusionSound); 
        Vector3 worldCenter = symbolObj != null ? symbolObj.transform.position : uiParent.position;
        if (ritualFlashVFX != null) SpawnVFXPublic(ritualFlashVFX, worldCenter); else if (fusionFlashVFX != null) SpawnVFXPublic(fusionFlashVFX, worldCenter); 

        GameObject cinCard = new GameObject("CinematicRitual", typeof(RectTransform), typeof(RawImage));
        cinCard.transform.SetParent(uiParent, false); cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); 
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>(); ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / 0.3f); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p);
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); yield return null; }

        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        SpawnFieldMarker(targetPos, ritualMarkerColor, ritualFieldMarkerVFX != null ? ritualFieldMarkerVFX : specialFieldMarkerVFX);

        if (symbolObj != null) Destroy(symbolObj); 
        
        yield return new WaitForSeconds(0.6f / animSpeed);

        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f)); rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; }

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay);
        onComplete?.Invoke();
    }

    // --- UTILITÁRIOS ---

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public GameObject SpawnVFXPublic(GameObject prefab, Vector3 position)
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
            
            // FIX: Lê a duração real do próprio sistema de partículas para a destruição
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps != null)
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
            ri.texture = card.GetFrontTexture() != null ? card.GetFrontTexture() : card.cardImage.texture;
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
                        spawnTrailTimer -= Time.deltaTime; if (spawnTrailTimer <= 0) { spawnTrailTimer = 0.03f; SpawnCardTrailGhost(card.GetComponent<RectTransform>(), card.GetFrontTexture() != null ? card.GetFrontTexture() : card.cardImage.texture, controlSwapTrailColor); }
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

    private void SpawnCardTrailGhost(RectTransform sourceRT, Texture tex, Color color)
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
        
        StartCoroutine(FadeAndDestroyCardGhost(ghost, ri, 0.3f));
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
        ri.texture = card.GetFrontTexture() != null ? card.GetFrontTexture() : card.cardImage.texture;
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
}
