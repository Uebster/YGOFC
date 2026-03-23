using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChainLinkVFX : MonoBehaviour
{
    [Header("Animação (Arraste da Hierarchy)")]
    public RectTransform leftChain; // Imagem do Elo esquerdo
    public RectTransform rightChain; // Imagem do Elo direito
    public TextMeshProUGUI linkText;
    
    public float slideDuration = 0.2f;

    public void Setup(int linkNumber)
    {
        if (linkText != null) linkText.text = $"Link {linkNumber}";
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // As correntes começam 500 pixels fora do centro, nas bordas da tela
        Vector2 leftStart = new Vector2(-500f, 0f);
        Vector2 rightStart = new Vector2(500f, 0f);
        Vector2 endPos = Vector2.zero;

        if (leftChain != null) leftChain.anchoredPosition = leftStart;
        if (rightChain != null) rightChain.anchoredPosition = rightStart;
        if (linkText != null) linkText.transform.localScale = Vector3.zero; // Esconde o texto

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            if (leftChain != null) leftChain.anchoredPosition = Vector2.Lerp(leftStart, endPos, smoothT);
            if (rightChain != null) rightChain.anchoredPosition = Vector2.Lerp(rightStart, endPos, smoothT);
            yield return null;
        }

        // POP! O texto surge forte após as correntes colidirem
        t = 0;
        while (t < 1f) { t += Time.deltaTime / 0.15f; if (linkText != null) linkText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t); yield return null; }
    }
}