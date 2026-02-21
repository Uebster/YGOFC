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

    [Header("Efeitos Visuais (Prefabs)")]
    public GameObject spellActivateVFX; // Partículas de magia
    public GameObject trapActivateVFX;  // Partículas de armadilha
    public GameObject summonVFX;        // Invocação Comum
    public GameObject fusionVFX;        // Fusão
    public GameObject tributeVFX;       // Luz azulada / Portal
    public GameObject attackVFX;        // Corte ou impacto
    public GameObject explosionVFX;     // Fumaça/Explosão de destruição
    public GameObject reflectVFX;       // Escudo/Barreira (Ataque falhou)

    [Header("Efeitos Sonoros (AudioClips)")]
    public AudioClip spellSound;
    public AudioClip trapSound;
    public AudioClip summonSound;
    public AudioClip fusionSound;
    public AudioClip tributeSound;
    public AudioClip attackSound;
    public AudioClip destroySound;
    public AudioClip reflectSound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        StartCoroutine(AttackRoutine(attacker, target, onHit));
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
