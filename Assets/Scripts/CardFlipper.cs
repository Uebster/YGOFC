// Assets/Scripts/CardFlipper.cs

using UnityEngine;
using System.Collections;

public class CardFlipper : MonoBehaviour
{
    public Renderer cardRenderer;
    public Material frontMaterial;
    public Material backMaterial;
    
    private bool isShowingFront = true;

    void Awake()
    {
        // Tenta encontrar o Renderer automaticamente se não estiver atribuído manualmente
        if (cardRenderer == null) cardRenderer = GetComponent<Renderer>();
        
        // Verifica se tem Collider para o clique funcionar
        if (GetComponent<Collider>() == null) Debug.LogWarning("CardFlipper: ATENÇÃO! Este objeto não tem um Collider. O clique 3D não vai funcionar.");
    }

    // Esta função é chamada automaticamente pelo Unity quando você clica no objeto com o mouse.
    // Requer apenas que o objeto tenha um Collider (Box Collider).
    private void OnMouseDown()
    {
        Debug.Log("CardFlipper: Clique 3D detectado no objeto!");
        Flip();
    }

    // Inicia a animação de flip
    public void Flip()
    {
        if (cardRenderer == null)
        {
            Debug.LogError("CardFlipper: ERRO - O componente Renderer não foi encontrado!");
            return;
        }        
        if (frontMaterial == null || backMaterial == null)
        {
            Debug.LogWarning("CardFlipper: AVISO - Materiais da frente ou verso não estão atribuídos no script!");
        }

        Debug.Log("CardFlipper: Iniciando animação de rotação...");
        StopAllCoroutines();
        StartCoroutine(FlipAnimation());
    }

    // Anima a rotação e troca o material no meio do caminho
    private IEnumerator FlipAnimation()
    {
        isShowingFront = !isShowingFront;
        float duration = 0.3f; // Duração do flip em segundos
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 180, 0);

        // Anima até a metade
        while (elapsed < duration / 2f)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Troca o material
        cardRenderer.material = isShowingFront ? frontMaterial : backMaterial;

        // Continua a animação
        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = endRotation; // Garante a rotação final
    }
    
    // Força a carta a mostrar a frente, sem animar (útil ao trocar de carta)
    public void ShowFront(bool instant)
    {
        if (!isShowingFront)
        {
            if (instant)
            {
                transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
            else
            {
                Flip();
                return;
            }
        }
        isShowingFront = true;
        cardRenderer.material = frontMaterial;
    }
}
