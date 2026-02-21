using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    public DeckZoneType sourceZone;

    private GameObject dragObject;
    private CanvasGroup canvasGroup;
    private Transform originalParent;

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Cria uma cópia visual para arrastar
        dragObject = Instantiate(gameObject, transform.root); // Instancia no Canvas raiz
        dragObject.transform.position = transform.position;
        
        // Remove scripts desnecessários da cópia
        Destroy(dragObject.GetComponent<DeckDragHandler>());
        
        // Configura para não bloquear Raycast (para detectar o DropZone embaixo)
        canvasGroup = dragObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;

        // Se estiver arrastando do Deck, podemos querer remover visualmente temporariamente?
        // Por enquanto, mantemos o original.
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
        if (dragObject != null) Destroy(dragObject);

        // Verifica onde soltou
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        
        if (dropTarget != null)
        {
            DeckDropZone zone = dropTarget.GetComponent<DeckDropZone>();
            if (zone != null)
            {
                // Soltou em uma zona válida
                DeckBuilderManager.Instance.AddCardToDeck(cardData, zone.zoneType);
                
                // Se moveu de um deck para outro (não do Trunk), remove da origem
                if (sourceZone != DeckZoneType.Trunk && sourceZone != zone.zoneType)
                {
                    DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
                }
            }
            // Se soltou no nada e veio de um deck, remove (lixeira)
            else if (sourceZone != DeckZoneType.Trunk)
            {
                DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
            }
        }
    }
}