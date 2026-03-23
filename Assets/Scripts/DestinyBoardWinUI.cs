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
    public float delayBetweenLetters = 1.0f; // Suspense entre o drop das letras
    public float delayBeforeEndDuel = 2.5f;  // Tempo olhando a palavra finalizada

    [Header("Animações Extras")]
    public Transform spawnPoint; // De onde as letras surgem (ex: do nada no centro)
    public GameObject finalExplosionVFX; // Prefab do clarão/energia negra
    public float flyDuration = 0.4f;

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

        yield return new WaitForSeconds(delayBeforeEndDuel);
        
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
            
            card.position = Vector3.Lerp(start, end, t);
            card.localScale = Vector3.Lerp(startScale, endScale, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (card != null) { card.position = end; card.localScale = endScale; }
    }
}
