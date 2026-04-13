using UnityEngine;
using UnityEngine.UI;

public class VfxAutoAnim : MonoBehaviour
{
    public enum FadeType { None, FadeIn, FadeOut, Blink }
    public enum ScaleType { None, ScaleUp, ScaleDown, Pulse, PulseY }

    [Header("Configurações Gerais")]
    public float duration = 0.3f;
    [Tooltip("Se verdadeiro, o objeto gira no eixo Z infinitamente.")]
    public bool spinEffect = false;
    public float spinSpeed = 180f;

    [Header("Comportamento de Transparência")]
    public FadeType fadeType = FadeType.FadeOut;
    public float blinkSpeed = 10f; // Usado apenas se FadeType = Blink
    
    [Header("Comportamento de Tamanho")]
    public ScaleType scaleType = ScaleType.ScaleUp;
    public float startScale = 0.5f;
    public float endScale = 2.5f;
    private Image img;
    private float timer = 0f;
    public Color baseColor = Color.white;

    void Start()
    {
        img = GetComponent<Image>();
        if (img != null)
        {
            baseColor = img.color;
            // Se for Fade In, garante que começa invisível (Alpha = 0)
            if (fadeType == FadeType.FadeIn) img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }
        
        if (scaleType != ScaleType.None)
            transform.localScale = new Vector3(startScale, startScale, 1);
            
        // Se o objeto não preencher a tela, dá uma giradinha aleatória pra ficar dinâmico
        if (scaleType == ScaleType.ScaleUp)
            transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    void Update()
    {
        timer += Time.deltaTime;
        float percent = timer / duration;

        // Aplica o Tamanho (Scale)
        if (scaleType == ScaleType.ScaleUp)
        {
            float s = Mathf.Lerp(startScale, endScale, Mathf.Sqrt(percent)); // Raiz quadrada faz crescer rápido no início
            transform.localScale = new Vector3(s, s, 1);
        }
        else if (scaleType == ScaleType.ScaleDown)
        {
            float s = Mathf.Lerp(startScale, endScale, percent);
            transform.localScale = new Vector3(s, s, 1);
        }
        else if (scaleType == ScaleType.Pulse)
        {
            float s = Mathf.Lerp(startScale, endScale, Mathf.PingPong(timer * blinkSpeed, 1f));
            transform.localScale = new Vector3(s, s, 1);
        }
        else if (scaleType == ScaleType.PulseY)
        {
            float sy = Mathf.Lerp(startScale, endScale, Mathf.PingPong(timer * blinkSpeed, 1f));
            transform.localScale = new Vector3(1f, sy, 1f);
        }

        // Aplica a Transparência (Fade)
        if (img != null)
        {
            if (fadeType == FadeType.FadeOut)
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(baseColor.a, 0f, percent));
            else if (fadeType == FadeType.FadeIn)
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, baseColor.a, percent));
            else if (fadeType == FadeType.Blink)
            {
                float alpha = Mathf.PingPong(timer * blinkSpeed, baseColor.a);
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
        }
        
        if (spinEffect)
        {
            transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
        }

        if (timer >= duration) Destroy(gameObject); // Se mata quando acaba
    }
}
