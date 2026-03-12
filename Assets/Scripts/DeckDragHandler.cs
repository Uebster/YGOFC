using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckDragHandler : MonoBehaviour
{
    public CardData cardData;
    public DeckZoneType sourceZone;

    private GameObject dragObject;
    private CanvasGroup dragObjectCanvasGroup;
    private CanvasGroup sourceCanvasGroup; // Canvas group do objeto original

    // OnBeginDrag, OnDrag, OnEndDrag, e OnPointerClick agora são métodos públicos
    // que serão chamados pelo CardImageDragProxy ou por outros scripts.

    public void OnBeginDrag(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DeckDragHandler] Falha ao iniciar o arrasto: Não foi possível encontrar um Canvas na hierarquia pai.", gameObject);
            return;
        }

        // --- LÓGICA DE VISUAL DE ARRASTO LIMPO ---
        // 1. Cria um objeto vazio para ser o visual
        dragObject = new GameObject("CardDragVisual");
        dragObject.transform.SetParent(canvas.transform, false);
        dragObject.transform.SetAsLastSibling(); // Garante que renderize por cima

        // 2. Adiciona um RawImage para mostrar a arte da carta
        RawImage dragImage = dragObject.AddComponent<RawImage>();

        // 3. Copia a textura e o tamanho da imagem original que iniciou o arrasto
        // eventData.pointerDrag é o objeto com o CardImageDragProxy (neste caso, Card2D)
        RawImage originalCardArt = eventData.pointerDrag?.GetComponentInChildren<RawImage>(true);
        
        if (originalCardArt != null && originalCardArt.texture != null)
        {
            dragImage.texture = originalCardArt.texture;
            dragImage.rectTransform.sizeDelta = originalCardArt.rectTransform.sizeDelta;
        }
        else
        {
            // Fallback caso não encontre a arte, para evitar mais erros
            Debug.LogWarning("[DeckDragHandler] Não foi possível encontrar a arte da carta original para copiar. Usando tamanho padrão.");
            dragImage.rectTransform.sizeDelta = new Vector2(96f, 135f); // Um tamanho de carta padrão
        }

        // Posiciona o novo objeto sob o cursor
        dragObject.transform.position = eventData.position;

        // 4. Adiciona o CanvasGroup ao objeto limpo (isso não deve falhar)
        dragObjectCanvasGroup = dragObject.AddComponent<UnityEngine.CanvasGroup>();
        dragObjectCanvasGroup.blocksRaycasts = false;
        dragObjectCanvasGroup.alpha = 0.9f;

        // 5. Torna o objeto original semi-invisível
        sourceCanvasGroup = GetComponent<CanvasGroup>();
        if (sourceCanvasGroup == null)
        {
            sourceCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        sourceCanvasGroup.alpha = 0.4f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null)
        {
            dragObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = 1f;
        }

        if (dragObject != null) Destroy(dragObject);

        // --- NOVA LÓGICA DE DROP COM RAYCASTALL ---
        // 1. Pega todos os objetos da UI sob o cursor
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 2. Procura pela primeira DropZone válida na lista de resultados
        DeckDropZone targetZone = null;
        GameObject dropTarget = null;
        
        foreach (RaycastResult result in results)
        {
            dropTarget = result.gameObject;
            DeckDropZone zone = dropTarget.GetComponent<DeckDropZone>();
            if (zone != null)
            {
                targetZone = zone;
                break; // Encontrou a primeira, para o loop
            }
        }
        
        // DEBUG: Log do objeto que foi atingido (se algum foi)
        if (dropTarget != null)
        {
            Debug.Log($"[DragNDrop] Card '{cardData.name}' dropped near: {dropTarget.name}. Found DropZone? {(targetZone != null ? targetZone.zoneType.ToString() : "No")}");
        }
        else
        {
            Debug.Log($"[DragNDrop] Card '{cardData.name}' dropped on NOTHING.");
        }
        
        if (targetZone != null)
        {
            // Se soltar na mesma zona de onde veio (exceto o baú), não faz nada.
            if (sourceZone != DeckZoneType.Trunk && targetZone.zoneType == sourceZone)
            {
                return;
            }

            // Soltou em uma zona válida
            bool success = DeckBuilderManager.Instance.AddCardToDeck(cardData, targetZone.zoneType);
            if(success)
            {
                // Se veio de um deck, remove da origem. O baú não precisa de remoção.
                if (sourceZone != DeckZoneType.Trunk)
                {
                    DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
                }
            }
            else
            {
                // Se o movimento foi inválido (ex: deck cheio), aciona o feedback
                DeckBuilderManager.Instance.TriggerInvalidMoveFeedback(targetZone.zoneType);
            }
        }
        // Se soltou no nada (nenhuma dropzone encontrada) e veio de um deck, remove (lixeira)
        else if (sourceZone != DeckZoneType.Trunk)
        {
            DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (DeckBuilderManager.Instance == null) return;

            if (sourceZone == DeckZoneType.Trunk)
            {
                // Adiciona ao deck apropriado (Main ou Extra)
                DeckZoneType target = DeckZoneType.Main;
                if (cardData.type.Contains("Fusion") || cardData.type.Contains("Synchro") || cardData.type.Contains("Xyz"))
                {
                    target = DeckZoneType.Extra;
                }
                
                bool success = DeckBuilderManager.Instance.AddCardToDeck(cardData, target);
                if (!success)
                {
                    DeckBuilderManager.Instance.TriggerInvalidMoveFeedback(target);
                }
            }
            else
            {
                // Remove do deck atual
                DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
            }
        }
    }
}
