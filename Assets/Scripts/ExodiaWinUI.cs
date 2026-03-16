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
                GameObject cardGO = Instantiate(cardPrefab, slots[i]);
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

                // Toca som de impacto a cada peça que surge
                if (DuelFXManager.Instance != null && DuelFXManager.Instance.attackSound != null)
                    DuelFXManager.Instance.audioSource.PlayOneShot(DuelFXManager.Instance.attackSound);
            }
            yield return new WaitForSeconds(delayBetweenPieces); 
        }

        // Toca o som "Obliterate!" ou explosão especial
        if (obliterateSound != null && DuelFXManager.Instance != null)
            DuelFXManager.Instance.audioSource.PlayOneShot(obliterateSound);

        // Aguarda a admiração do jogador
        yield return new WaitForSeconds(delayBeforeEndDuel);
        
        if (panel) panel.SetActive(false);
        onSequenceComplete?.Invoke();
    }
}