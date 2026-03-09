using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlinkEffect : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Velocidade do piscar.")]
    [Range(0.1f, 10f)]
    public float speed = 3.0f;

    [Tooltip("Transparência mínima (0 = invisível, 1 = visível).")]
    [Range(0f, 1f)]
    public float minAlpha = 0.2f;

    [Tooltip("Transparência máxima.")]
    [Range(0f, 1f)]
    public float maxAlpha = 1.0f;

    private Graphic targetGraphic;
    private CanvasGroup targetGroup;

    void Awake()
    {
        // Tenta pegar componentes que podem ter alpha alterado
        targetGraphic = GetComponent<Graphic>(); // Pega Image, RawImage ou TextMeshProUGUI
        targetGroup = GetComponent<CanvasGroup>(); // Pega grupo se houver
    }

    void Update()
    {
        // Calcula o alpha usando PingPong para ir e voltar suavemente
        float t = Mathf.PingPong(Time.time * speed, 1f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        if (targetGroup != null)
        {
            targetGroup.alpha = alpha;
        }
        else if (targetGraphic != null)
        {
            Color c = targetGraphic.color;
            c.a = alpha;
            targetGraphic.color = c;
        }
    }
}