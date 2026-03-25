using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DestinyBoardWinUI : MonoBehaviour
{
    public static DestinyBoardWinUI Instance;
    
    [Header("Referências UI")]
    public GameObject panel;
    [Tooltip("Arraste LetterF, LetterI, LetterN, LetterA, LetterL da sua Hierarchy")]
    public Transform[] letterSlots; 
    
    [Header("Configurações")]
    public float delayBetweenLetters = 0.6f; // Suspense mais rápido e dinâmico
    private float exactFlashDuration = 2.0f;  // Tempo cravado do clarão

    [Header("Animações Extras")]
    public Transform spawnPoint; // De onde as letras surgem (ex: do nada no centro)
    public GameObject finalExplosionVFX; // Prefab do clarão/energia negra
    public float flyDuration = 0.2f; // Voo mais rápido e agressivo

    private List<GameObject> spawnedCards = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (panel) panel.SetActive(false);
    }

    public void ShowWinSequence(bool playerWon, System.Action onSequenceComplete)
    {
        gameObject.SetActive(true); // Garante que a raiz do script acorde para a Corrotina
        StartCoroutine(PlayWinSequence(playerWon, onSequenceComplete));
    }

    private IEnumerator PlayWinSequence(bool playerWon, System.Action onSequenceComplete)
    {
        if (panel) panel.SetActive(true);

        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();

        // Limpa resquícios antigos nos slots
        foreach (Transform slot in letterSlots)
        {
            if (slot != null) foreach (Transform child in slot) Destroy(child.gameObject);
        }

        string[] letterNames = { "Destiny Board", "Spirit Message \"I\"", "Spirit Message \"N\"", "Spirit Message \"A\"", "Spirit Message \"L\"" };
        GameObject cardPrefab = null;

        // Pega o prefab oficial de carta da sua engine para exibir na tela
        if (GameManager.Instance != null && GameManager.Instance.playerDeckDisplay != null)
            cardPrefab = GameManager.Instance.playerDeckDisplay.cardPrefab;

        for (int i = 0; i < 5; i++)
        {
            if (i < letterSlots.Length && letterSlots[i] != null && cardPrefab != null)
            {
                Transform startTransform = spawnPoint != null ? spawnPoint : letterSlots[i];
                GameObject cardGO = Instantiate(cardPrefab, startTransform.position, Quaternion.identity, letterSlots[i]);
                spawnedCards.Add(cardGO);

                // Iguala o tamanho da carta ao tamanho exato do espaço vazio
                RectTransform cardRect = cardGO.GetComponent<RectTransform>();
                RectTransform targetRect = letterSlots[i].GetComponent<RectTransform>();
                if (cardRect != null && targetRect != null) {
                    cardRect.sizeDelta = targetRect.sizeDelta;
                }

                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                CardData data = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == letterNames[i]);
                
                if (display != null && data != null)
                {
                    // Remove layouts conflitantes para caber perfeito no seu slot Letter
                    LayoutElement le = cardGO.GetComponent<LayoutElement>();
                    if (le != null) Destroy(le);

                    display.SetCard(data, GameManager.Instance.GetCardBackTexture(), true);
                    display.isInteractable = false; 
                }

                // Faz a letra voar até o lugar e crescer
                yield return StartCoroutine(FlyCardToSlot(cardGO.transform, startTransform.position, letterSlots[i].position));

                if (DuelFXManager.Instance != null && DuelFXManager.Instance.attackImpactSound != null)
                    DuelFXManager.Instance.audioSource.PlayOneShot(DuelFXManager.Instance.attackImpactSound);
            }
            yield return new WaitForSeconds(delayBetweenLetters); // Aguarda o delay para cada letra (suspense)
        }

        // Instancia o Efeito Visual Final
        if (finalExplosionVFX != null)
        {
            Instantiate(finalExplosionVFX, transform.position, Quaternion.identity, transform);
        }

        // Treme a tela por exatos 2 segundos para sincronizar perfeitamente com o clarão e o Fade do GameManager
        yield return StartCoroutine(ScreenShake(exactFlashDuration, 8f));
        
        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();

        if (panel) panel.SetActive(false);
        onSequenceComplete?.Invoke();
    }

    private IEnumerator FlyCardToSlot(Transform card, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.5f; // Nasce gigante para assustar
        Vector3 endScale = Vector3.one;          // Volta ao normal no slot
        
        while (elapsed < flyDuration)
        {
            if (card == null) yield break;
            float t = elapsed / flyDuration;
            float easedT = Mathf.SmoothStep(0, 1, t);
            
            // Acompanha a posição do pai caso o HorizontalLayoutGroup mova os slots durante a animação
            Vector3 currentEnd = card.parent.position;

            card.position = Vector3.Lerp(start, currentEnd, easedT);
            card.localScale = Vector3.Lerp(startScale, endScale, easedT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Trava perfeitamente no centro do slot zerando a posição local!
        if (card != null) { card.localPosition = Vector3.zero; card.localScale = endScale; }
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = originalPos.x + UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + UnityEngine.Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = originalPos;
    }
}
