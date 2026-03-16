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
                GameObject cardGO = Instantiate(cardPrefab, letterSlots[i]);
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

                if (DuelFXManager.Instance != null && DuelFXManager.Instance.attackSound != null)
                    DuelFXManager.Instance.audioSource.PlayOneShot(DuelFXManager.Instance.attackSound);
            }
            yield return new WaitForSeconds(delayBetweenLetters); // Aguarda o delay para cada letra (suspense)
        }

        yield return new WaitForSeconds(delayBeforeEndDuel);
        
        if (panel) panel.SetActive(false);
        onSequenceComplete?.Invoke();
    }
}
