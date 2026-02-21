using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class MillenniumButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuração Visual - Texto")]
    public TextMeshProUGUI buttonText;
    public Color textNormalColor = Color.white;
    public Color textHoverColor = new Color(1f, 0.84f, 0f); // Dourado (Gold)

    [Header("Configuração Visual - Fundo")]
    public Image backgroundImage;
    public bool useColorTint = true; // Desmarque se estiver usando uma textura no fundo
    [Tooltip("Usado se 'useColorTint' for true.")]
    public Color bgNormalColor = Color.white; // Branco Opaco (Padrão corrigido)
    [Tooltip("Usado se 'useColorTint' for true.")]
    public Color bgHoverColor = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Cinza escuro quase opaco

    [Header("Chroma Key (Remover Fundo)")]
    [Tooltip("Ativa a remoção automática de uma cor (ex: fundo branco) da imagem.")]
    public bool useChromaKey = false;
    [Tooltip("A cor que será tornada transparente.")]
    public Color colorToMask = Color.white;
    [Tooltip("O quão parecida a cor precisa ser para ser removida (0 = exata, 1 = tudo).")]
    [Range(0f, 1f)]
    public float colorTolerance = 0.1f;

    [Header("Animação")]
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
        
        // Tenta achar a imagem de fundo (o próprio botão)
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        // Define a cor inicial
        ResetVisuals();
    }

    void Start()
    {
        if (useChromaKey)
        {
            ApplyChromaKey();
        }
    }

    void OnEnable()
    {
        // Garante que o botão volte ao normal quando a tela for reaberta
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        transform.localScale = originalScale;
        if (buttonText != null) 
            buttonText.color = textNormalColor;
        if (backgroundImage != null)
        {
            // Se usar textura, a cor base deve ser branca para não tingir.
            backgroundImage.color = useColorTint ? bgNormalColor : Color.white;
        }
    }

    private void ApplyChromaKey()
    {
        if (backgroundImage == null || backgroundImage.sprite == null) return;

        try
        {
            // Obtém a textura original
            Texture2D sourceTex = backgroundImage.sprite.texture;
            
            // Cria uma nova textura para não estragar o asset original
            Texture2D newTex = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
            
            Color[] pixels = sourceTex.GetPixels();
            
            for (int i = 0; i < pixels.Length; i++)
            {
                // Calcula a diferença entre a cor do pixel e a cor que queremos remover
                float diff = Mathf.Abs(pixels[i].r - colorToMask.r) + 
                             Mathf.Abs(pixels[i].g - colorToMask.g) + 
                             Mathf.Abs(pixels[i].b - colorToMask.b);
                
                // Se for parecida o suficiente, torna transparente
                if (diff < colorTolerance)
                {
                    pixels[i] = Color.clear;
                }
            }
            
            newTex.SetPixels(pixels);
            newTex.Apply();
            
            // Cria um novo sprite com a textura processada
            Sprite newSprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), backgroundImage.sprite.pivot);
            backgroundImage.sprite = newSprite;
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"MillenniumButton: Erro ao remover fundo da imagem '{backgroundImage.sprite.name}'. Verifique se 'Read/Write Enabled' está marcado nas configurações da textura. Erro: {e.Message}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        // Efeito de Escala
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale * scaleAmount));

        // Efeito de Cor (Texto e Fundo, se habilitado)
        if (buttonText != null) 
            buttonText.color = textHoverColor;
        if (backgroundImage != null && useColorTint) 
            backgroundImage.color = bgHoverColor;

        // Som
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        // Volta ao normal
        StopAllCoroutines();
        StartCoroutine(AnimateScale(originalScale));

        if (buttonText != null) 
            buttonText.color = textNormalColor;
        if (backgroundImage != null && useColorTint) 
            backgroundImage.color = bgNormalColor;
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
