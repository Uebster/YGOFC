using UnityEngine;
using System.Collections;

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
    public GameObject fusionVFX;        // Fusão
    public GameObject tributeVFX;       // Luz azulada / Portal
    public GameObject attackVFX;        // Corte ou impacto
    public GameObject explosionVFX;     // Fumaça/Explosão de destruição
    public GameObject reflectVFX;       // Escudo/Barreira (Ataque falhou)
    public GameObject banishVFX;        // Carta removida de jogo (vórtice/buraco negro)
    public GameObject flipVFX;          // Efeito de Flip
    public GameObject damageVFX;        // Dano direto ou batalha vencida
    public GameObject defenseSuccessVFX;// Defesa bem sucedida (escudo metálico)

    [Header("Efeitos Sonoros (AudioClips)")]
    public AudioClip spellSound;
    public AudioClip trapSound;
    public AudioClip summonSound;
    public AudioClip fusionSound;
    public AudioClip tributeSound;
    public AudioClip attackSound;
    public AudioClip destroySound;
    public AudioClip reflectSound;
    public AudioClip banishSound;
    public AudioClip flipSound;
    public AudioClip damageSound;
    public AudioClip defenseSound;

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
    }

    // --- ATIVAÇÃO DE MAGIA / ARMADILHA ---

    public void PlayCardActivation(CardDisplay card, bool isTrap, System.Action onComplete = null)
    {
        if (!enableAnimations)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(AnimateActivationRoutine(card, isTrap, onComplete));
    }

    private IEnumerator AnimateActivationRoutine(CardDisplay card, bool isTrap, System.Action onComplete)
    {
        // 1. Salva posição original
        Vector3 originalPos = card.transform.position;
        Transform originalParent = card.transform.parent;
        int originalIndex = card.transform.GetSiblingIndex();

        // 2. Move para o centro (traz para frente na UI)
        if (boardCenter != null)
        {
            // Muda o pai para o root ou um canvas overlay para ficar por cima de tudo
            card.transform.SetParent(boardCenter.root, true); 
            
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * animationSpeed;
                card.transform.position = Vector3.Lerp(originalPos, boardCenter.position, t);
                yield return null;
            }
        }

        // 3. Toca Som e VFX
        PlaySound(isTrap ? trapSound : spellSound);
        GameObject vfxPrefab = isTrap ? trapActivateVFX : spellActivateVFX;
        SpawnVFX(vfxPrefab, card.transform.position);

        // 4. Aguarda um pouco (efeito visual acontecer)
        yield return new WaitForSeconds(1.0f);

        // 5. Retorna (ou destrói, dependendo da lógica do jogo, mas aqui devolvemos o controle)
        // Nota: Normalmente a carta vai para o cemitério depois, o GameManager cuidará disso.
        // Por segurança visual, podemos devolver ao pai original se ela não for destruída imediatamente.
        if (card != null && originalParent != null)
        {
            card.transform.SetParent(originalParent, true);
            card.transform.SetSiblingIndex(originalIndex);
            // Opcional: Lerpar de volta
            card.transform.position = originalPos; 
        }

        onComplete?.Invoke();
    }

    // --- BATALHA ---

    public void PlayAttack(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        if (!enableAnimations)
        {
            onHit?.Invoke();
            return;
        }

        // Verifica se deve usar a nova animação de projétil (espada) ou a clássica (mover carta)
        // FIX: Só usa a nova se estiver habilitada E se o prefab existir
        if (GameManager.Instance != null && GameManager.Instance.enableAttackAnimation && GameManager.Instance.attackAnimationPrefab != null)
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
        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target.transform.position;

        // 1. Recuo (Anticipation)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (animationSpeed * 2);
            attacker.transform.position = Vector3.Lerp(startPos, startPos - (targetPos - startPos).normalized * 50f, t);
            yield return null;
        }

        // 2. Avanço (Strike)
        PlaySound(attackSound);
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (animationSpeed * 5); // Muito rápido
            attacker.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 3. Impacto
        SpawnVFX(attackVFX, targetPos);
        onHit?.Invoke(); // Chama o callback de dano/cálculo

        // 4. Retorno
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            attacker.transform.position = Vector3.Lerp(targetPos, startPos, t);
            yield return null;
        }
    }

    // Esqueleto da nova animação de ataque (Espada/Projétil)
    private IEnumerator AttackProjectileRoutine(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        Vector3 startPos = attacker.transform.position;
        Vector3 endPos;

        // Define o destino: Centro da carta alvo ou Avatar do oponente (Ataque Direto)
        if (target != null)
        {
            endPos = target.transform.position;
        }
        else
        {
            // Ataque Direto: Tenta pegar a posição do avatar do oponente
            if (GameManager.Instance != null && GameManager.Instance.duelFieldUI != null && GameManager.Instance.duelFieldUI.opponentAvatarImage != null)
                endPos = GameManager.Instance.duelFieldUI.opponentAvatarImage.transform.position;
            else
                endPos = startPos + Vector3.up * 5f; // Fallback (cima)
        }

        // Instancia o projétil (Espada) se houver prefab
        GameObject projectile = null;
        if (GameManager.Instance != null && GameManager.Instance.attackAnimationPrefab != null)
        {
            projectile = Instantiate(GameManager.Instance.attackAnimationPrefab, startPos, Quaternion.identity);
            // Faz o projétil olhar para o alvo
            projectile.transform.LookAt(endPos); 
            // Se for 2D, pode precisar de ajuste de rotação diferente (ex: LookAt2D)
        }

        PlaySound(attackSound);

        float duration = 0.4f; // Duração do voo
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (projectile != null) projectile.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (projectile != null) Destroy(projectile);
        
        // Impacto
        SpawnVFX(attackVFX, endPos);
        Debug.Log("[DuelFXManager] Projétil atingiu o alvo. Chamando callback.");
        onHit?.Invoke();
    }

    public void PlayDestruction(CardDisplay card)
    {
        PlaySound(destroySound);
        SpawnVFX(explosionVFX, card.transform.position);
        // A lógica de destruir o objeto (Destroy) fica no GameManager/CardDisplay
    }

    public void PlayAttackFail(CardDisplay attacker)
    {
        PlaySound(reflectSound);
        SpawnVFX(reflectVFX, attacker.transform.position);
    }

    // --- INVOCAÇÃO / TRIBUTO ---

    public void PlaySummonEffect(CardDisplay card)
    {
        PlaySound(summonSound);
        SpawnVFX(summonVFX, card.transform.position);
    }

    public void PlayFusionEffect(CardDisplay card)
    {
        PlaySound(fusionSound);
        SpawnVFX(fusionVFX, card.transform.position);
    }

    public void PlayTributeEffect(CardDisplay card)
    {
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
        PlaySound(banishSound);
        SpawnVFX(banishVFX, card.transform.position);
    }

    public void PlayFlipEffect(CardDisplay card)
    {
        PlaySound(flipSound);
        SpawnVFX(flipVFX, card.transform.position);
    }

    public void PlayDamageEffect(Vector3 position)
    {
        PlaySound(damageSound);
        SpawnVFX(damageVFX, position);
    }

    public void PlayDefenseSuccessEffect(CardDisplay card)
    {
        PlaySound(defenseSound);
        SpawnVFX(defenseSuccessVFX, card.transform.position);
    }

    // --- UTILITÁRIOS ---

    private void PlaySound(AudioClip clip)
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
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            // Garante que o VFX fique na frente da UI
            if (boardCenter != null) instance.transform.SetParent(boardCenter.root);
            
            Destroy(instance, 3.0f); // Limpeza automática
            return instance;
        }
        return null;
    }
}
