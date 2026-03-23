using UnityEngine;
using System.Collections;

public class ExodiaWinUI : MonoBehaviour
{
    public static ExodiaWinUI Instance;
    
    [Header("Referências UI")]
    public GameObject panel;
    public Transform exodiaHead;
    public Transform exodiaLeftArm;
    public Transform exodiaRightArm;
    public Transform exodiaLeftLeg;
    public Transform exodiaRightLeg;
    
    [Header("Configurações")]
    public float delayBetweenPieces = 0.8f; 
    public float delayBeforeEndDuel = 3.0f;  
    [Tooltip("Som brutal para tocar quando a cabeça surgir (Opcional)")]
    public AudioClip obliterateSound; 

    [Header("Animações Extras")]
    public Transform spawnPoint; // Ponto de onde as cartas saem (Ex: Centro da tela)
    public GameObject finalExplosionVFX; // Prefab do clarão branco ou explosão
    public float flyDuration = 0.4f; // Tempo de voo até o slot

    void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    public void ShowWinSequence(bool playerWon, System.Action onSequenceComplete)
    {
        StartCoroutine(PlayWinSequence(playerWon, onSequenceComplete));
    }

    private IEnumerator PlayWinSequence(bool playerWon, System.Action onSequenceComplete)
    {
        if (panel) panel.SetActive(true);

        // Ordem dramática de aparição: Pernas, Braços e, por fim, a Cabeça!
        Transform[] slots = { exodiaLeftLeg, exodiaRightLeg, exodiaLeftArm, exodiaRightArm, exodiaHead };
        string[] pieceNames = { "Left Leg of the Forbidden One", "Right Leg of the Forbidden One", "Left Arm of the Forbidden One", "Right Arm of the Forbidden One", "Exodia the Forbidden One" };

        // Limpa resquícios antigos (se o jogador jogar outro duelo)
        foreach (Transform slot in slots)
        {
            if (slot != null) foreach (Transform child in slot) Destroy(child.gameObject);
        }

        // Usa o prefab original das cartas para exibição visual
        GameObject cardPrefab = null;
        if (GameManager.Instance != null && GameManager.Instance.playerDeckDisplay != null)
            cardPrefab = GameManager.Instance.playerDeckDisplay.cardPrefab;

        for (int i = 0; i < 5; i++)
        {
            if (slots[i] != null && cardPrefab != null)
            {
                // Instancia primeiro no spawn point
                Transform startTransform = spawnPoint != null ? spawnPoint : slots[i];
                GameObject cardGO = Instantiate(cardPrefab, startTransform.position, Quaternion.identity, slots[i]);
                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                CardData data = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == pieceNames[i]);
                
                if (display != null && data != null)
                {
                    // Remove o modificador de Layout para caber exatamente no seu slot
                    UnityEngine.UI.LayoutElement le = cardGO.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (le != null) Destroy(le);

                    display.SetCard(data, GameManager.Instance.GetCardBackTexture(), true);
                    display.isInteractable = false; 
                }

                // Faz a carta voar do spawn point até o slot dela
                yield return StartCoroutine(FlyCardToSlot(cardGO.transform, startTransform.position, slots[i].position));

                // Toca som de impacto a cada peça que surge
                if (DuelFXManager.Instance != null && DuelFXManager.Instance.attackImpactSound != null)
                    DuelFXManager.Instance.audioSource.PlayOneShot(DuelFXManager.Instance.attackImpactSound);
            }
            yield return new WaitForSeconds(delayBetweenPieces); 
        }

        // Toca o som "Obliterate!" ou explosão especial
        if (obliterateSound != null && DuelFXManager.Instance != null)
            DuelFXManager.Instance.audioSource.PlayOneShot(obliterateSound);
            
        // Instancia o Efeito Visual Final (Clarão)
        if (finalExplosionVFX != null)
        {
            Instantiate(finalExplosionVFX, transform.position, Quaternion.identity, transform);
        }

        // Aguarda a admiração do jogador
        yield return new WaitForSeconds(delayBeforeEndDuel);
        
        if (panel) panel.SetActive(false);
        onSequenceComplete?.Invoke();
    }

    private IEnumerator FlyCardToSlot(Transform card, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero; // Nasce pequena
        Vector3 endScale = Vector3.one;    // Cresce até o tamanho normal
        
        while (elapsed < flyDuration)
        {
            if (card == null) yield break;
            float t = elapsed / flyDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t); // Movimento suave
            
            card.position = Vector3.Lerp(start, end, smoothT);
            card.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (card != null) { card.position = end; card.localScale = endScale; }
    }
}