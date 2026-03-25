using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public GameObject summonVFX;        // Invocação Comum
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
    public GameObject summonAuraVFX;    // Aura por trás ao invocar
    public GameObject shuffleVFX;       // Efeito visual (poeira/luz) ao embaralhar

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
        Debug.Log($"[DuelFXManager] PlayCardActivation para {card?.CurrentCardData?.name} (Trap: {isTrap})");
        if (!enableAnimations)
        {
            onComplete?.Invoke();
            return;
        }
        
        // Se for Magia de Campo, toca uma cinemática exclusiva
        if (card != null && card.CurrentCardData != null && card.CurrentCardData.property == "Field")
        {
            Debug.Log($"[DuelFXManager] Executando rotina especial de Field Spell.");
            StartCoroutine(FieldSpellActivationRoutine(card, onComplete));
        }
        else
        {
            StartCoroutine(AnimateActivationRoutine(card, isTrap, onComplete));
        }
    }

    private IEnumerator AnimateActivationRoutine(CardDisplay card, bool isTrap, System.Action onComplete)
    {
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
            darkOverlay.transform.SetParent(boardCenter.root, false);
            RectTransform rt = darkOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image img = darkOverlay.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Começa transparente

            // Traz a carta para a frente de tudo
            card.transform.SetParent(boardCenter.root, true); 
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
        SpawnVFX(vfxPrefab, card.transform.position);

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

        onComplete?.Invoke();
    }

    private IEnumerator FieldSpellActivationRoutine(CardDisplay card, System.Action onComplete)
    {
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
            darkOverlay.transform.SetParent(boardCenter.root, false);
            RectTransform rt = darkOverlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0);

            card.transform.SetParent(boardCenter.root, true); card.transform.SetAsLastSibling();
            
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
        SpawnVFX(spellActivateVFX, card.transform.position);
        yield return new WaitForSeconds(0.6f);

        // Animação Especial do Field: Cresce engolindo a tela e some (Fade Out)
        float fadeT = 0;
        Vector3 currentScale = card.transform.localScale;
        Vector3 massiveScale = currentScale * 2.5f;

        while (fadeT < 1f)
        {
            if (card == null || cg == null) break;
            fadeT += Time.deltaTime * (animationSpeed * 1.5f);
            card.transform.localScale = Vector3.Lerp(currentScale, massiveScale, fadeT);
            cg.alpha = Mathf.Lerp(1f, 0f, fadeT);
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
        onComplete?.Invoke();
    }

    // --- BATALHA ---

    public void PlayAttack(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        Debug.Log($"[DuelFXManager] PlayAttack de {attacker?.CurrentCardData?.name} para {(target != null ? target.CurrentCardData.name : "Direto")}");
        if (!enableAnimations)
        {
            onHit?.Invoke();
            return;
        }

        // Verifica se deve usar a nova animação de projétil (espada) ou a clássica (mover carta)
        // FIX: A restrição do prefab foi movida para que o Fallback da TargetingSword funcione!
        if (GameManager.Instance != null && GameManager.Instance.enableAttackAnimation)
        {
            Debug.Log("[DuelFXManager] Usando animação de Projétil (Espada).");
            StartCoroutine(AttackProjectileRoutine(attacker, target, onHit));
        }
        else
        {
            Debug.Log("[DuelFXManager] Usando animação Clássica (Mover Carta).");
            StartCoroutine(AttackRoutine(attacker, target, onHit));
        }
    }

    private IEnumerator AttackRoutine(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        // Safeguard: Evita loop infinito se a velocidade for 0 ou negativa
        if (animationSpeed <= 0.1f) animationSpeed = 1.5f;

        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos;

        if (target != null)
        {
            targetPos = target.transform.position;
        }
        else // Ataque Direto
        {
            targetPos = GetDirectAttackTarget(attacker, startPos + (attacker.transform.up * 200f));
        }

        // 1. Recuo (Anticipation)
        float t = 0;
        while (t < 1f)
        {
            if (attacker == null) { onHit?.Invoke(); yield break; }
            t += Time.deltaTime * (animationSpeed * 2);
            attacker.transform.position = Vector3.Lerp(startPos, startPos - (targetPos - startPos).normalized * 50f, t);
            yield return null;
        }

        // 2. Avanço (Strike)
        PlaySound(attackTravelSound);
        t = 0;
        while (t < 1f)
        {
            if (attacker == null) { onHit?.Invoke(); yield break; }
            t += Time.deltaTime * (animationSpeed * 5); // Muito rápido
            attacker.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 3. Impacto
        SpawnVFX(attackVFX, targetPos);
        PlaySound(attackImpactSound);
        onHit?.Invoke(); // Chama o callback de dano/cálculo

        // 4. Retorno
        t = 0;
        while (t < 1f)
        {
            if (attacker == null) { yield break; }
            t += Time.deltaTime * animationSpeed;
            attacker.transform.position = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }
    }

    // Esqueleto da nova animação de ataque (Espada/Projétil)
    private IEnumerator AttackProjectileRoutine(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        // Safeguard
        if (animationSpeed <= 0.1f) animationSpeed = 1.5f;

        Vector3 startPos = attacker.transform.position;
        Vector3 endPos;

        // Define o destino: Centro da carta alvo ou Avatar do oponente (Ataque Direto)
        if (target != null)
        {
            endPos = target.transform.position;
        }
        else
        {
            endPos = GetDirectAttackTarget(attacker, startPos + Vector3.up * 5f);
        }

        // Instancia o projétil (Espada) se houver prefab
        GameObject projectile = null;
        if (attackProjectileVFX != null)
        {
            projectile = Instantiate(attackProjectileVFX);
            
            // Arranca o script de autodestruição se ele existir, para a espada não sumir no meio do voo!
            VfxAutoAnim autoKill = projectile.GetComponent<VfxAutoAnim>();
            if (autoKill != null) 
            {
                autoKill.enabled = false; // Desativa imediatamente para garantir que o Update() não rode
                Destroy(autoKill);
            }
        }
        else
        {
            Debug.LogWarning("[DuelFXManager] Nenhum prefab de espada atribuído! Usando Fallback da espada do mouse.");
            // Fallback Genial: Se você esqueceu de colocar o Prefab de ataque, ele clona a espada do mouse!
            TargetingSwordUI mouseSword = Object.FindFirstObjectByType<TargetingSwordUI>();
            if (mouseSword != null)
            {
                projectile = Instantiate(mouseSword.gameObject);
                Destroy(projectile.GetComponent<TargetingSwordUI>()); // Tira o script de seguir o mouse
                Image img = projectile.GetComponent<Image>();
                if (img != null) img.enabled = true; // Força a ficar visível
            }
        }
            
        if (projectile != null)
        {
            if (boardCenter != null) projectile.transform.SetParent(boardCenter.root, false);
            projectile.transform.SetAsLastSibling();
            projectile.transform.localScale = Vector3.one;
            projectile.transform.position = startPos;
            
            // Rotação 2D: Calcula o ângulo para a espada apontar para o alvo
            Vector3 dir = endPos - startPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // Subtrai 90 graus assumindo que a ponta da sua espada aponta para CIMA na imagem original
            projectile.transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }

        PlaySound(attackTravelSound);

        float duration = 0.6f; // Voo mais dramático e visível
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (projectile != null) 
            {
                Vector3 currentTarget = target != null ? target.transform.position : endPos;
                projectile.transform.position = Vector3.Lerp(startPos, currentTarget, elapsed / duration);
            }            
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (projectile != null) Destroy(projectile);
        
        // Impacto
        SpawnVFX(attackVFX, endPos);
        // Treme a carta alvo se ela existir
        if (target != null) PlayCardShake(target);
        
        PlaySound(attackImpactSound);
        Debug.Log("[DuelFXManager] Projétil atingiu o alvo. Chamando callback.");
        onHit?.Invoke();
    }

    public void PlayDestruction(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayDestruction em {card?.CurrentCardData?.name}");
        if (!enableAnimations || card == null) return;
        PlaySound(destroySound);
        SpawnVFX(explosionVFX, card.transform.position);
        AnimateCardDeath(card, false); // Morte Explosiva (Fantasmas)
    }

    public void PlayAttackFail(CardDisplay attacker)
    {
        Debug.Log($"[DuelFXManager] PlayAttackFail em {attacker?.CurrentCardData?.name}");
        PlaySound(reflectSound);
        SpawnVFX(reflectVFX, attacker.transform.position);
    }

    // --- INVOCAÇÃO / TRIBUTO ---

    public void PlaySummonEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlaySummonEffect em {card?.CurrentCardData?.name}");
        PlaySound(summonSound);
        SpawnVFX(summonVFX, card.transform.position);
    }

    public void PlayTributeSummonEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayTributeSummonEffect em {card?.CurrentCardData?.name}");
        PlaySound(summonSound); // Pode ter um som específico se quiser
        // Usa o prefab específico se existir, senão usa o padrão do GameManager, senão o de summon comum
        GameObject prefab = tributeSummonVFX != null ? tributeSummonVFX : summonVFX;
        SpawnVFX(prefab, card.transform.position);
    }

    public void PlayFusionEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayFusionEffect em {card?.CurrentCardData?.name}");
        PlaySound(fusionSound);
        SpawnVFX(fusionVFX, card.transform.position);
    }

    public void PlayTributeEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayTributeEffect em {card?.CurrentCardData?.name}");
        PlaySound(tributeSound);
        // Instancia o efeito de portal na carta e o torna filho dela para seguir se mover
        GameObject vfx = SpawnVFX(tributeVFX, card.transform.position);
        if (vfx != null)
        {
            vfx.transform.SetParent(card.transform);
            // O VFX deve ter um script de auto-destruição ou ser destruído quando a carta for
        }
    }

    // --- NOVOS EFEITOS DE BATALHA E JOGO ---

    public void PlayBanishEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayBanishEffect em {card?.CurrentCardData?.name}");
        if (!enableAnimations || card == null) return;
        PlaySound(banishSound);
        SpawnVFX(banishVFX, card.transform.position);
        AnimateCardDeath(card, true); // Morte por Sugador (Fantasmas)
    }

    private void AnimateCardDeath(CardDisplay card, bool isBanish)
    {
        if (card == null || boardCenter == null) return;
        
        // Cria um clone visual perfeito (Fantasma) antes da carta original ser deletada pelo jogo
        GameObject ghost = new GameObject("CardGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(boardCenter.root, false);
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
        float t = 0; RawImage ri = ghost.GetComponent<RawImage>(); Vector3 startPos = ghost.transform.position;
        while(t < 1f) {
            if (ghost == null) yield break;
            t += Time.deltaTime * 3f;
            ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0); // Tremedeira Estilhaçada
            ri.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        } Destroy(ghost);
    }

    public void PlayFlipEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayFlipEffect em {card?.CurrentCardData?.name}");
        PlaySound(flipSound);
        SpawnVFX(flipVFX, card.transform.position);
    }

    public void PlayDamageEffect(Vector3 position)
    {
        Debug.Log($"[DuelFXManager] PlayDamageEffect (Tremor/Flash na tela)");
        PlaySound(damageSound);
        SpawnVFX(damageVFX, position);
    }

    public void PlayDefenseSuccessEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayDefenseSuccessEffect (Escudo) em {card?.CurrentCardData?.name}");
        PlaySound(defenseSound);
        SpawnVFX(defenseSuccessVFX, card.transform.position);
    }

    public void PlayCardShake(CardDisplay card)
    {
        if (!enableAnimations || card == null) return;
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
        Debug.Log($"[DuelFXManager] PlayChainLinkEffect (Corrente) Link {linkNumber}");
        // Toca o som de magia/armadilha ativando na corrente
        PlaySound(card.CurrentCardData.type.Contains("Trap") ? trapSound : spellSound);
        
        // Se tivermos um prefab de texto subindo, nós o instanciamos
        if (chainLinkVFX != null)
        {
            GameObject vfx = SpawnVFX(chainLinkVFX, card.transform.position);
            
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
        PlaySound(attackDeclareSound);
    }

    public void PlayMonsterEffect(CardDisplay card)
    {
        Debug.Log($"[DuelFXManager] PlayMonsterEffect em {card?.CurrentCardData?.name}");
        if (!enableAnimations || card == null) return;
        PlaySound(monsterEffectSound);
        SpawnVFX(monsterEffectVFX, card.transform.position);
    }

    public void PlaySummonAura(CardDisplay card)
    {
        if (!enableAnimations || card == null || summonAuraVFX == null) return;
        
        // Instancia no mesmo PAI da carta (a zona de monstros no tabuleiro)
        GameObject vfx = Instantiate(summonAuraVFX, card.transform.parent);
        vfx.transform.position = card.transform.position;
        vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // Renderiza exatamente atrás da carta na hierarquia
        
        // Impede que a Aura tente ocupar um slot na zona e quebre a fileira (Layout)
        UnityEngine.UI.LayoutElement le = vfx.GetComponent<UnityEngine.UI.LayoutElement>();
        if (le == null) le = vfx.AddComponent<UnityEngine.UI.LayoutElement>();
        le.ignoreLayout = true;
        
        Destroy(vfx, 3.0f);
    }

    public void PlayEquipEffect(CardDisplay source, CardDisplay target)
    {
        Debug.Log($"[DuelFXManager] PlayEquipEffect de {source?.CurrentCardData?.name} para {target?.CurrentCardData?.name}");
        if (!enableAnimations || source == null || target == null || boardCenter == null) return;
        StartCoroutine(EquipGhostRoutine(source, target));
    }

    private IEnumerator EquipGhostRoutine(CardDisplay source, CardDisplay target)
    {
        // Cria o fantasma da carta mágica
        GameObject ghost = new GameObject("EquipGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(boardCenter.root, false); ghost.transform.SetAsLastSibling();

        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = source.transform.position; rt.sizeDelta = source.GetComponent<RectTransform>().sizeDelta;
        rt.rotation = source.transform.rotation; rt.localScale = source.transform.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = source.cardImage.texture; ri.color = new Color(1f, 1f, 1f, 0.7f); // Fantasma semitransparente

        PlaySound(spellSound);
        float t = 0; Vector3 startPos = rt.position; Vector3 startScale = rt.localScale;
        
        // Voa em direção ao alvo encolhendo e girando
        while (t < 1f) {
            if (target == null) break;
            t += Time.deltaTime * 3f; float smoothT = Mathf.SmoothStep(0, 1, t);
            rt.position = Vector3.Lerp(startPos, target.transform.position, smoothT);
            rt.Rotate(0, 0, 360f * Time.deltaTime); // Gira
            rt.localScale = Vector3.Lerp(startScale, startScale * 0.3f, smoothT); // Encolhe para "entrar"
            yield return null;
        }

        if (target != null) {
            GameObject vfxPrefab = equipImpactVFX != null ? equipImpactVFX : spellActivateVFX;
            SpawnVFX(vfxPrefab, target.transform.position);
            PlayCardShake(target); // Dá um "tranco" no monstro indicando que recebeu o poder
        }
        Destroy(ghost);
    }

    public void PlayShuffleEffect(Transform pileTransform)
    {
        if (!enableAnimations || pileTransform == null) return;
        PlaySound(shuffleSound);
        SpawnVFX(shuffleVFX, pileTransform.position);
    }

    public void PlaySummonCinematic(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(SummonCinematicRoutine(card, isTribute, isSpecial, onComplete));
    }

    private IEnumerator SummonCinematicRoutine(CardDisplay card, bool isTribute, bool isSpecial, System.Action onComplete)
    {
        GameObject darkOverlay = null;
        Image darkImg = null;
        
        // 1. Fundo Escurecido
        if (boardCenter != null)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(boardCenter.root, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; rtDark.sizeDelta = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        // 2. Carta Gigante Deslumbrante
        GameObject cinCard = new GameObject("CinematicCard", typeof(RectTransform), typeof(RawImage));
        if (boardCenter != null) cinCard.transform.SetParent(boardCenter.root, false);
        cinCard.transform.SetAsLastSibling();
        
        RectTransform rt = cinCard.GetComponent<RectTransform>();
        rt.position = boardCenter != null ? boardCenter.position : Vector3.zero;
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
        GameObject marker = SpawnVFX(markerPrefab, targetPos);
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
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(FusionCinematicRoutine(card, materials, polyCard, onComplete));
    }

    private IEnumerator FusionCinematicRoutine(CardDisplay card, List<CardData> materials, CardData polyCard, System.Action onComplete)
    {
        GameObject darkOverlay = null; Image darkImg = null;
        
        if (boardCenter != null)
        {
            darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image));
            darkOverlay.transform.SetParent(boardCenter.root, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>();
            rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; rtDark.sizeDelta = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>();
            darkImg.color = new Color(0, 0, 0, 0f);
        }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / 0.3f)); yield return null; }

        Vector3 centerPos = boardCenter != null ? boardCenter.position : Vector3.zero;

        GameObject symbolObj = null;
        if (fusionBackgroundSymbol != null) { 
            symbolObj = new GameObject("FusionSymbol", typeof(RectTransform), typeof(Image)); 
            if (boardCenter != null) symbolObj.transform.SetParent(boardCenter.root, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); rtSym.position = centerPos; rtSym.sizeDelta = new Vector2(400f, 400f);
            Image imgSym = symbolObj.GetComponent<Image>(); imgSym.sprite = fusionBackgroundSymbol; imgSym.color = new Color(1, 1, 1, 0); 
        }

        List<RectTransform> matRects = new List<RectTransform>();
        if (materials != null && materials.Count > 0 && GameManager.Instance != null)
        {
            foreach (var mat in materials)
            {
                GameObject matObj = new GameObject("MatCard", typeof(RectTransform), typeof(RawImage));
                if (boardCenter != null) matObj.transform.SetParent(boardCenter.root, false);
                RectTransform rtMat = matObj.GetComponent<RectTransform>(); rtMat.sizeDelta = new Vector2(100f, 145f);
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
                    matRects[i].position = centerPos + new Vector3(x, y, 0); matRects[i].rotation = Quaternion.Euler(0, 0, currentAngle);
                }
                yield return null;
            }
        }
        else yield return new WaitForSeconds(0.5f);

        foreach (var m in matRects) Destroy(m.gameObject);
        PlaySound(fusionSound);
        if (fusionFlashVFX != null) SpawnVFX(fusionFlashVFX, centerPos);

        GameObject cinCard = new GameObject("CinematicFusion", typeof(RectTransform), typeof(RawImage));
        if (boardCenter != null) cinCard.transform.SetParent(boardCenter.root, false);
        cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); rt.position = centerPos; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>();
        ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / 0.3f); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p); 
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); 
            yield return null; }
        yield return new WaitForSeconds(0.6f / animSpeed);

        GameObject markerPrefab = fusionFieldMarkerVFX != null ? fusionFieldMarkerVFX : specialFieldMarkerVFX;
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        GameObject marker = SpawnVFX(markerPrefab, targetPos); PlaySound(attackTravelSound);
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
        if (!enableAnimations || card == null) { onComplete?.Invoke(); return; }
        StartCoroutine(RitualCinematicRoutine(card, ritualSpell, onComplete));
    }

    private IEnumerator RitualCinematicRoutine(CardDisplay card, CardData ritualSpell, System.Action onComplete)
    {
        GameObject darkOverlay = null; Image darkImg = null;
        if (boardCenter != null) { darkOverlay = new GameObject("CinematicOverlay", typeof(RectTransform), typeof(Image)); darkOverlay.transform.SetParent(boardCenter.root, false);
            RectTransform rtDark = darkOverlay.GetComponent<RectTransform>(); rtDark.anchorMin = Vector2.zero; rtDark.anchorMax = Vector2.one; rtDark.sizeDelta = Vector2.zero;
            darkImg = darkOverlay.GetComponent<Image>(); darkImg.color = new Color(0, 0, 0, 0f); }

        float animSpeed = animationSpeed > 0 ? animationSpeed : 1.5f; float t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; if (darkImg != null) darkImg.color = new Color(0, 0, 0, Mathf.Lerp(0f, 0.8f, t / 0.3f)); yield return null; }

        Vector3 centerPos = boardCenter != null ? boardCenter.position : Vector3.zero;
        GameObject symbolObj = null;
        if (ritualBackgroundSymbol != null) { symbolObj = new GameObject("RitualSymbol", typeof(RectTransform), typeof(Image)); if (boardCenter != null) symbolObj.transform.SetParent(boardCenter.root, false);
            RectTransform rtSym = symbolObj.GetComponent<RectTransform>(); rtSym.position = centerPos; rtSym.sizeDelta = new Vector2(400f, 400f);
            Image imgSym = symbolObj.GetComponent<Image>(); imgSym.sprite = ritualBackgroundSymbol; imgSym.color = new Color(1, 1, 1, 0); }

        PlaySound(spellSound); t = 0;
        while (t < 1.0f) { t += Time.deltaTime * animSpeed; if (symbolObj != null) {
                symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0f, 0.6f, t)); symbolObj.transform.Rotate(0, 0, 45f * Time.deltaTime * animSpeed); } yield return null; }

        yield return new WaitForSeconds(0.3f); PlaySound(fusionSound); 
        if (ritualFlashVFX != null) SpawnVFX(ritualFlashVFX, centerPos); else if (fusionFlashVFX != null) SpawnVFX(fusionFlashVFX, centerPos); 

        GameObject cinCard = new GameObject("CinematicRitual", typeof(RectTransform), typeof(RawImage));
        if (boardCenter != null) cinCard.transform.SetParent(boardCenter.root, false); cinCard.transform.SetAsLastSibling();
        RectTransform rt = cinCard.GetComponent<RectTransform>(); rt.position = centerPos; rt.sizeDelta = new Vector2(200f, 290f);
        RawImage ri = cinCard.GetComponent<RawImage>(); ri.texture = !card.isPlayerCard ? (GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null) : card.cardImage.texture;

        t = 0;
        while (t < 0.3f) { t += Time.deltaTime * animSpeed; float p = Mathf.Clamp01(t / 0.3f); rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, p);
            if (symbolObj != null) symbolObj.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Lerp(0.6f, 0f, p)); yield return null; }

        if (symbolObj != null) Destroy(symbolObj); yield return new WaitForSeconds(0.6f / animSpeed);

        GameObject markerPrefab = ritualFieldMarkerVFX != null ? ritualFieldMarkerVFX : specialFieldMarkerVFX;
        Vector3 targetPos = card.transform.parent != null ? card.transform.parent.position : card.transform.position;
        GameObject marker = SpawnVFX(markerPrefab, targetPos); PlaySound(attackTravelSound);
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

    private GameObject SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            Debug.Log($"[DuelFXManager] SpawnVFX: {prefab.name} instanciado com sucesso.");
            GameObject instance = Instantiate(prefab);
            // Garante que o VFX fique na frente da UI
            if (boardCenter != null) instance.transform.SetParent(boardCenter.root, false);
            instance.transform.position = position;
            instance.transform.SetAsLastSibling(); // Traz para a FRENTE do tabuleiro e cartas
            
            // FIX: Força Sistemas de Partículas 3D a renderizarem por CIMA do Canvas (Interface)!
            ParticleSystemRenderer[] renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Canvas rootCanvas = boardCenter != null ? boardCenter.GetComponentInParent<Canvas>() : null;
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder + 30000 : 30000; // Valor bem alto
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
            
            Destroy(instance, 3.0f); // Limpeza automática
            return instance;
        }
        return null;
    }

    private Vector3 GetDirectAttackTarget(CardDisplay attacker, Vector3 fallbackPos)
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
}
