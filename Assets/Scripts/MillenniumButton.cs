using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class MillenniumButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuração Visual")]
    public TextMeshProUGUI buttonText;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.84f, 0f); // Dourado (Gold)
    public float scaleAmount = 1.1f; // Aumenta 10%
    public float animationDuration = 0.1f;

    [Header("Áudio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private Vector3 originalScale;
    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        originalScale = transform.localScale;

        // Tenta achar o texto automaticamente se não estiver atribuído
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            
        // Define a cor inicial
        if (buttonText != null) buttonText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        // Efeito de Escala
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale * scaleAmount));

        // Efeito de Cor (Dourado)
        if (buttonText != null) buttonText.color = hoverColor;

        // Som
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        // Volta ao normal
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale));

        if (buttonText != null) buttonText.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!btn.interactable) return;
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private System.Collections.IEnumerator AnimateScale(Vector3 targetScale)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < animationDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / animationDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }
}
