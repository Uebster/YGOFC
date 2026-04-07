using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UIAnimatedEffect : MonoBehaviour
{
    [Header("Animação (Frame by Frame)")]
    [Tooltip("Arraste os frames (Sprites) da sua explosão/efeito aqui na ordem correta.")]
    public Sprite[] frames;
    
    [Tooltip("Velocidade da animação (ex: 24 a 30 quadros por segundo).")]
    public float framesPerSecond = 30f;
    
    [Tooltip("Se verdadeiro, o objeto se destrói sozinho quando a animação acabar.")]
    public bool destroyOnEnd = true;
    
    [Tooltip("A animação repete sem parar? (Não marque para impactos/cortes)")]
    public bool loop = false;
    
    [Header("Efeitos Extras")]
    [Tooltip("Faz o efeito girar continuamente como um vórtice?")]
    public bool spin = false;
    [Tooltip("Velocidade do giro (graus por segundo). Valor negativo inverte o lado.")]
    public float spinSpeed = -360f;

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        img.raycastTarget = false; // Garante que o efeito não bloqueie seus cliques nas cartas!
    }

    void Start()
    {
        if (frames != null && frames.Length > 0)
        {
            StartCoroutine(PlayAnimation());
        }
    }

    private IEnumerator PlayAnimation()
    {
        // Usa tempo Delta exato para evitar os "engasgos" do WaitForSeconds da Unity
        float timePerFrame = 1f / framesPerSecond;
        float timer = 0f;
        int currentFrame = 0;
        
        img.sprite = frames[currentFrame];

        while (true)
        {
            timer += Time.deltaTime;
            
            if (spin)
            {
                transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
            }

            if (timer >= timePerFrame)
            {
                timer -= timePerFrame; // Preserva os milissegundos excedentes para perfeição de tempo
                currentFrame++;

                if (currentFrame >= frames.Length) { if (loop) currentFrame = 0; else break; }
                
                img.sprite = frames[currentFrame];
            }
            yield return null; // Acompanha a renderização exata da tela (60/144hz)
        }

        if (destroyOnEnd) Destroy(gameObject);
    }
}