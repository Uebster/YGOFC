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
    public Color colorSpell = new Color(0.2f, 0.9f, 0.4f, 1f); // Verde
    public Color colorTrap = new Color(0.9f, 0.2f, 0.7f, 1f); // Rosa
    public Color colorMonsterEffect = new Color(0.8f, 0.5f, 0.1f, 1f); // Marrom/Laranja
    public Color colorMonsterNormal = new Color(0.9f, 0.9f, 0.5f, 1f); // Amarelo
    public Color colorFusion = new Color(0.6f, 0.2f, 0.9f, 1f); // Roxo
    public Color colorRitual = new Color(0.2f, 0.5f, 1f, 1f); // Azul

    [Header("Cinemáticas de Invocação (Etapa 2)")]
    [Tooltip("Ícone de fogo/alma que fica sobre os monstros selecionados para sacrifício.")]
    public Sprite tributeIconSprite;
    public GameObject tributeFieldMarkerVFX; // Marcação que surge no campo no Tribute Summon
    public GameObject specialFieldMarkerVFX; // Marcação (Ritual, Fusão, etc) no campo

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
        
        // Se for Magia de Campo, toca uma cinemática exclusiva
        if (card != null && card.CurrentCardData != null && card.CurrentCardData.property == "Field")
        {
            Debug.Log($"[VFX] ➔ [DESVIO] Redirecionando para FieldSpellActivationRoutine.");
            StartCoroutine(FieldSpellActivationRoutine(card, onComplete));
        }
        else
        {
            StartCoroutine(AnimateActivationRoutine(card, isTrap, onComplete));
        }
    }

    private IEnumerator AnimateActivationRoutine(CardDisplay card, bool isTrap, System.Action onComplete)
    {
        Debug.Log($"[VFX] ⚙ [ROUTINE INICIADA] AnimateActivationRoutine | Alvo: {card?.CurrentCardData?.name} | Descrição: Levitando carta para o centro da tela.");
        // 1. Salva posição original
        Vector3 originalPos = card.transform.position;
        Vector3 originalScale = card.transform.localScale;
        Transform originalParent = card.transform.parent;
        int originalIndex = card.transform.GetSiblingIndex();

        // 2. Move para o centro, cresce e escurece a tela
        GameObject darkOverlay = null;

        if (boardCenter != null)
        {
            // Cria um fundo escuro dinamicamente
            darkOverlay = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(GetUIParent(), false);
            RectTransform rt = darkOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image img = darkOverlay.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Começa transparente

            // Traz a carta para a frente de tudo
            card.transform.SetParent(GetUIParent(), true); 
            card.transform.SetAsLastSibling();
            
            float t = 0;
            Vector3 targetScale = originalScale * 1.5f; // Cresce 50%
            
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime * (animationSpeed * 1.5f);
                float smoothT = Mathf.SmoothStep(0, 1, t);
                
                card.transform.position = Vector3.Lerp(originalPos, boardCenter.position, smoothT);
                card.transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothT);
                img.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.7f, smoothT)); // Escurece 70%
                yield return null;
            }
            
            if (card != null)
            {
                card.transform.position = boardCenter.position;
                card.transform.localScale = targetScale;
            }
            if (img != null) img.color = new Color(0, 0, 0, 0.7f);
        }

        // 3. Toca Som e VFX de Ativação
        PlaySound(isTrap ? trapSound : spellSound);
        GameObject vfxPrefab = isTrap ? trapActivateVFX : spellActivateVFX;
        Debug.Log($"[VFX] 💥 [IMPACTO] Tocando VFX de Ativação ({vfxPrefab?.name}). Aguardando 0.8s dramáticos.");
        SpawnVFXPublic(vfxPrefab, card.transform.position);

        // 4. Aguarda o show
        yield return new WaitForSeconds(0.8f);

        // 5. Retorna pro lugar (para efeitos contínuos) ou se prepara pra ser destruída
        if (boardCenter != null && darkOverlay != null)
        {
            Image img = darkOverlay.GetComponent<Image>();
            float t = 0;
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime * (animationSpeed * 2f); // Volta mais rápido
                float smoothT = Mathf.SmoothStep(0, 1, t);
                
                card.transform.position = Vector3.Lerp(boardCenter.position, originalPos, smoothT);
                card.transform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, smoothT);
                img.color = new Color(0, 0, 0, Mathf.Lerp(0.7f, 0f, smoothT));
                yield return null;
            }
            Destroy(darkOverlay);
        }

        if (card != null && originalParent != null)
        {
            card.transform.SetParent(originalParent, true);
            card.transform.SetSiblingIndex(originalIndex);
            card.transform.position = originalPos; 
            card.transform.localScale = originalScale;
        }

        Debug.Log($"[VFX] ✔ [ROUTINE CONCLUÍDA] AnimateActivationRoutine finalizada. Destravando Engine LUA.");
        onComplete?.Invoke();
    }

    private IEnumerator FieldSpellActivationRoutine(CardDisplay card, System.Action onComplete)
    {
        Debug.Log($"[VFX] ⚙ [ROUTINE INICIADA] FieldSpellActivationRoutine | Alvo: {card?.CurrentCardData?.name}");
        Vector3 originalPos = card.transform.position;
        Vector3 originalScale = card.transform.localScale;
        Transform originalParent = card.transform.parent;
        int originalIndex = card.transform.GetSiblingIndex();

        GameObject darkOverlay = null; Image darkImg = null;
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();

        if (boardCenter != null)
        {
            darkOverlay = new GameObject("FieldDarkOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(GetUIParent(), false);
            RectTransform rt = darkOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0);

            card.transform.SetParent(GetUIParent(), true); card.transform.SetAsLastSibling();
            
            float t = 0; Vector3 targetScale = originalScale * 1.5f; 
            while (t < 1f) {
                if (card == null) break;
                t += Time.deltaTime * (animationSpeed * 1.5f); float smoothT = Mathf.SmoothStep(0, 1, t);
                card.transform.position = Vector3.Lerp(originalPos, boardCenter.position, smoothT);
                card.transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothT);
                if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.7f, smoothT));
                yield return null;
            }
        }

        PlaySound(spellSound);
        SpawnVFXPublic(spellActivateVFX, card.transform.position);
        yield return new WaitForSeconds(0.6f);

        // Animação Especial do Field: Cresce engolindo a tela e some (Fade Out)
        float fadeT = 0;
        Vector3 currentScale = card.transform.localScale;
        Vector3 massiveScale = currentScale * 2.5f;

        // Determina a cor do tabuleiro baseada no nome da magia
        Color targetBoardColor = defaultBoardColor;
        if (colorizeBoardByFieldSpell)
        {
            string cardName = card.CurrentCardData.name.ToLower();
            foreach (var theme in fieldSpellThemes)
            {
                bool match = false;
                foreach (var kw in theme.keywords) { if (cardName.Contains(kw.ToLower())) { match = true; break; } }
                if (match)
                {
                    targetBoardColor = Color.Lerp(defaultBoardColor, theme.overlayColor, theme.intensity);
                    break;
                }
            }
        }

        while (fadeT < 1f)
        {
            if (card == null || cg == null) break;
            fadeT += Time.deltaTime * (animationSpeed * 1.5f);
            card.transform.localScale = Vector3.Lerp(currentScale, massiveScale, fadeT);
            cg.alpha = Mathf.Lerp(1f, 0f, fadeT);
            
            // Transição suave de cor do tabuleiro
            if (boardBackgroundImage != null) 
                boardBackgroundImage.color = Color.Lerp(boardBackgroundImage.color, targetBoardColor, fadeT);
                
            yield return null;
        }

        // Retorna pro lugar da Field Zone invisível
        if (card != null && originalParent != null) {
            card.transform.SetParent(originalParent, true); card.transform.SetSiblingIndex(originalIndex);
            card.transform.position = originalPos; card.transform.localScale = originalScale;
        }

        // Volta a ficar visível lentamente enquanto a tela clareia
        fadeT = 0;
        while (fadeT < 1f)
        {
            if (card == null || cg == null) break;
            fadeT += Time.deltaTime * (animationSpeed * 2f);
            cg.alpha = Mathf.Lerp(0f, 1f, fadeT);
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.7f, 0f, fadeT));
            yield return null;
        }
        
        if (cg != null) cg.alpha = 1f;
        if (darkOverlay != null) Destroy(darkOverlay);
        Debug.Log($"[VFX] ✔ [ROUTINE CONCLUÍDA] FieldSpellActivationRoutine. Tabuleiro substituído com sucesso.");
        onComplete?.Invoke();
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

        if (TargetingSwordUI.Instance != null)
        {
            TargetingSwordUI.Instance.PerformAttack(attacker, target, onHit);
        }
        else
        {
            Debug.LogError("[VFX] ❌ ERRO: TargetingSwordUI.Instance is null! Cannot perform attack animation.");
            // Fallback: immediately call onHit to not stall the duel.
            onHit?.Invoke();
        }
    }

    public void PlayDestruction(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayDestruction | Alvo: {card?.CurrentCardData?.name} | Momento: Após cálculo de dano / Monstro derrotado.");
        if (!enableAnimations || card == null) return;
        PlaySound(destroySound);
        AnimateCardDeath(card, false); // Morte Explosiva (Fantasmas)
        // Instancia o VFX por ÚLTIMO para garantir que a explosão cubra a carta no Canvas 2D
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
        PlaySound(tokenSummonSound != null ? tokenSummonSound : summonSound);
        GameObject prefab = tokenSummonVFX != null ? tokenSummonVFX : summonVFX;
        SpawnVFXPublic(prefab, card.transform.position);
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
        PlaySound(tributeSound);
        // Instancia o efeito de portal na carta e o torna filho dela para seguir se mover
        GameObject vfx = SpawnVFXPublic(tributeVFX, card.transform.position);
        if (vfx != null)
        {
            vfx.transform.SetParent(card.transform);
            // O VFX deve ter um script de auto-destruição ou ser destruído quando a carta for
        }
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
        ghost.transform.SetParent(uiParent, false);
        ghost.transform.SetAsLastSibling();
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = card.transform.position;
        rt.sizeDelta = card.GetComponent<RectTransform>().sizeDelta;
        rt.rotation = card.transform.rotation;
        rt.localScale = card.transform.localScale;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture;
        ri.color = card.cardImage.color;
        
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
        float duration = 0.3f; // Exatamente a metade da explosão (0.6s total / 20fps)
        float elapsed = 0f;
        RawImage ri = ghost.GetComponent<RawImage>(); 
        Vector3 startPos = ghost.transform.position;

        while (elapsed < duration) 
        {
            if (ghost == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float shake = Mathf.Lerp(2f, 12f, t); // Tremedeira aumenta conforme a explosão cresce
            ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0); 
            ri.color = new Color(1f - (t * 0.8f), 1f - (t * 0.8f), 1f - (t * 0.8f), 1f); // Carta "tosta" escurecendo antes de explodir
            yield return null;
        } 
        Destroy(ghost); // Some instantaneamente no ápice do fogo!
    }

    public void PlayFlipEffect(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayFlipEffect | Alvo: {card?.CurrentCardData?.name} | Momento: Carta virada para cima ou setada (Face-Down).");
        PlaySound(flipSound);
        SpawnVFXPublic(flipVFX, card.transform.position);
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
        
        // A corrente visual (e seu som) só deve aparecer quando é uma RESPOSTA (Link 2 em diante).
        // O Link 1 já tem sua própria cinemática e som via PlayCardActivation ou PlayMonsterEffect.
        if (linkNumber <= 1) return;

        // Toca o som de magia/armadilha ativando na corrente
        PlaySound(card.CurrentCardData.type.Contains("Trap") ? trapSound : spellSound);
        
        // Se tivermos um prefab de texto subindo, nós o instanciamos
        if (chainLinkVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(chainLinkVFX, card.transform.position);
            
            var animScript = vfx.GetComponent<ChainLinkVFX>();
            if (animScript != null) animScript.Setup(linkNumber);
            else
            {
                TMPro.TextMeshProUGUI txt = vfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = $"Link {linkNumber}";
            }
        }
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
        SpawnVFXPublic(monsterEffectVFX, card.transform.position);
    }

    // Wrapper de retrocompatibilidade
    public void PlaySummonAura(CardDisplay card) => PlayPlacementAura(card);

    public void PlayPlacementAura(CardDisplay card)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayPlacementAura | Alvo: {card?.CurrentCardData?.name} | Momento: Carta foi posicionada fisicamente na mesa (Set/Summon/Activate).");
        if (!enableAnimations || card == null || placementAuraVFX == null) 
        {
            if (placementAuraVFX == null) Debug.LogWarning($"[VFX] ⚠ PlayPlacementAura ABORTADO: Prefab 'placementAuraVFX' não assinalado no Inspector!");
            return;
        }
        
        GameObject vfx = Instantiate(placementAuraVFX);
        
        // Se for interface 2D, prende no Canvas. Se for Partícula 3D, mantém no World Space para não encolher!
        if (vfx.GetComponent<RectTransform>() != null)
        {
            Transform uiParent = GetUIParent();
            if (uiParent != null) vfx.transform.SetParent(uiParent, true);
        }
        
        vfx.transform.position = card.transform.position;
        
        // FIX CRÍTICO: Força Sistemas de Partículas 3D (como Holy-Light) a renderizarem na frente do tabuleiro escuro
        ParticleSystemRenderer[] renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (renderers.Length > 0)
        {
            Canvas rootCanvas = card.GetComponentInParent<Canvas>();
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder + 10 : 10; // Fica acima do fundo, mas atrás dos impactos principais (30000)
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
        }

        // SISTEMA DE COR PERSONALIZADA BASEADA NO TIPO DE CARTA
        if (colorizeAuraByType && card != null && card.CurrentCardData != null)
        {
            Color targetColor = colorMonsterNormal; // Default
            if (card.isFlipped) targetColor = colorFaceDown;
            else if (card.CurrentCardData.type.Contains("Spell")) targetColor = colorSpell;
            else if (card.CurrentCardData.type.Contains("Trap")) targetColor = colorTrap;
            else if (card.CurrentCardData.type.Contains("Fusion")) targetColor = colorFusion;
            else if (card.CurrentCardData.type.Contains("Ritual")) targetColor = colorRitual;
            else if (card.CurrentCardData.type.Contains("Effect")) targetColor = colorMonsterEffect;

            // Pinta Partículas 3D
            foreach (var r in renderers)
            {
                var psSystem = r.GetComponent<ParticleSystem>();
                if (psSystem != null) { var main = psSystem.main; main.startColor = targetColor; }
            }
            // Pinta Imagens 2D (UIAnimatedEffect)
            Image[] images = vfx.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.color = targetColor;
            }
        }

        // Auto-destruição lê o tempo de vida real da partícula em vez do engessado 3.0f
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        float destroyTime = ps != null ? (ps.main.duration + ps.main.startLifetime.constantMax + 0.5f) : 3.0f;
        Destroy(vfx, destroyTime);
        
        Debug.Log($"[VFX] ✔ [AURA INSTANCIADA] '{placementAuraVFX.name}' renderizada atrás da carta. Vida: {destroyTime}s.");
    }

    public void PlayEquipEffect(CardDisplay source, CardDisplay target)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayEquipEffect | Origem: {source?.CurrentCardData?.name} -> Alvo: {target?.CurrentCardData?.name} | Momento: Carta equipada com sucesso.");
        if (!enableAnimations || source == null || target == null) return; // FIX: Removida a exigência desnecessária do boardCenter
        StartCoroutine(EquipGhostRoutine(source, target));
    }

    private IEnumerator EquipGhostRoutine(CardDisplay source, CardDisplay target)
    {
        // Cria o fantasma da carta mágica
        GameObject ghost = new GameObject("EquipGhost", typeof(RectTransform), typeof(RawImage));
        Transform uiParent = GetUIParent();
        if (uiParent != null) { ghost.transform.SetParent(uiParent, false); ghost.transform.SetAsLastSibling(); }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform sourceRT = source.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.sizeDelta = sourceRT.sizeDelta;
        rt.rotation = sourceRT.rotation; rt.localScale = sourceRT.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = source.cardImage.texture; ri.color = new Color(1f, 1f, 1f, 0.8f); // Fantasma semitransparente

        PlaySound(spellSound);
        float t = 0; Vector3 startPos = rt.position; Vector3 startScale = rt.localScale;
        Vector3 endScale = target.transform.localScale * 0.3f; // Fica pequenininho ao entrar
        
        float startZ = rt.eulerAngles.z;
        float targetZ = target.transform.eulerAngles.z;
        
        while (t < 1f) {
            if (target == null || target.gameObject == null || rt == null) break;
            t += Time.deltaTime * 3f; float smoothT = Mathf.SmoothStep(0, 1, t);
            rt.position = Vector3.Lerp(startPos, target.transform.position, smoothT);
            
            // Desliza suavemente para a rotação alvo sem giros extras (0 ou 180 graus dependendo do lado do campo)
            float currentZ = Mathf.LerpAngle(startZ, targetZ, smoothT);
            rt.rotation = Quaternion.Euler(0, 0, currentZ); 
            
            rt.localScale = Vector3.Lerp(startScale, endScale, smoothT); // Encolhe para "entrar"
            yield return null;
        }

        if (target != null) {
            GameObject vfxPrefab = equipImpactVFX != null ? equipImpactVFX : spellActivateVFX;
            SpawnVFXPublic(vfxPrefab, target.transform.position);
            PlayCardShake(target); // Dá um "tranco" no monstro indicando que recebeu o poder
        }
        if (ghost != null) Destroy(ghost);
    }

    public void PlayShuffleEffect(Transform pileTransform)
    {
        if (!enableAnimations || pileTransform == null) return;
        Debug.Log($"[VFX] ➔ [CHAMADA] PlayShuffleEffect | Momento: Embaralhamento engatilhado na pilha {pileTransform.name}.");
        PlaySound(shuffleSound);
        SpawnVFXPublic(shuffleVFX, pileTransform.position);
    }

    public void PlaySummonCinematic(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        Debug.Log($"[VFX] ➔ [CHAMADA] PlaySummonCinematic | Alvo: {card?.CurrentCardData?.name} | Momento: Invocação Maior aterrissando na mesa.");
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(SummonCinematicRoutine(card, isTribute, isSpecial, onComplete));
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

        yield return new WaitForSeconds(0.6f / animSpeed); // SUSPENSE

        // 3. Marca no Chão e Pouso
        GameObject markerPrefab = isTribute ? tributeFieldMarkerVFX : specialFieldMarkerVFX;
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        GameObject marker = SpawnVFXPublic(markerPrefab, targetPos);
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

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay); if (marker != null) Destroy(marker, 0.3f);
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
        
        yield return new WaitForSeconds(0.6f / animSpeed);

        GameObject markerPrefab = fusionFieldMarkerVFX != null ? fusionFieldMarkerVFX : specialFieldMarkerVFX;
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        GameObject marker = SpawnVFXPublic(markerPrefab, targetPos); PlaySound(attackTravelSound);
        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f));
            rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, targetScale, p); rt.rotation = Quaternion.Slerp(startRot, targetRot, p); 
            if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; }

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay); if (marker != null) Destroy(marker, 0.3f); if (symbolObj != null) Destroy(symbolObj);
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

        if (symbolObj != null) Destroy(symbolObj); 
        
        yield return new WaitForSeconds(0.6f / animSpeed);

        GameObject markerPrefab = ritualFieldMarkerVFX != null ? ritualFieldMarkerVFX : specialFieldMarkerVFX;
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        GameObject marker = SpawnVFXPublic(markerPrefab, targetPos); PlaySound(attackTravelSound);
        Vector3 startPos = rt.position; Vector3 targetScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one;
        Quaternion startRot = Quaternion.identity; Quaternion targetRot = card.transform.rotation;

        t = 0;
        while (t < 0.25f) { t += Time.deltaTime * animSpeed; float p = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.25f)); rt.position = Vector3.Lerp(startPos, targetPos, p); rt.localScale = Vector3.Lerp(Vector3.one * 1.5f, targetScale, p);
            rt.rotation = Quaternion.Slerp(startRot, targetRot, p); if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 0f, p)); yield return null; }

        Destroy(cinCard); if (darkOverlay != null) Destroy(darkOverlay); if (marker != null) Destroy(marker, 0.3f);
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

        bool targetAvatar = GameManager.Instance.targetAvatarOnDirectAttack;
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
        SpawnVFXPublic(spellActivateVFX, startPos);

        // Um pouco mais de tempo para apreciar a animação
        float duration = 1.0f / (animationSpeed > 0 ? animationSpeed : 1f);
        float elapsed = 0f;
        Vector3 endPos = targetZone.position;
        Quaternion endRot = Quaternion.Euler(0, 0, targetZRot);

        while (elapsed < duration)
        {
            if (card == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            card.transform.position = Vector3.Lerp(startPos, endPos, t);
            card.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            
            // Efeito 3D: Cresce 50% no ar para dar a ilusão de parábola em direção à câmera
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
            card.transform.localScale = flightScale * scaleMultiplier;

            yield return null;
        }

        if (card != null) { 
            card.transform.SetParent(targetZone, false); 
            card.transform.localPosition = Vector3.zero; 
            card.transform.localRotation = endRot; 
            
            // FIX CRÍTICO: Garante que a carta volte ao tamanho original da mesa
            card.transform.localScale = GameManager.Instance != null ? GameManager.Instance.fieldCardScale : Vector3.one; 
            
            SpawnVFXPublic(summonVFX, endPos); 
            PlaySound(summonSound); 
        }
        onComplete?.Invoke();
    }
}
