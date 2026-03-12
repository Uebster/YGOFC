using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Este script atua como um proxy. Ele deve ser colocado no objeto que é o "pegador" visual para o arraste (ex: a imagem da carta).
/// Ele detecta os eventos de arrastar e os repassa para o script DeckDragHandler no objeto pai.
/// </summary>
public class CardImageDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DeckDragHandler parentDragHandler;

    void Start()
    {
        // Encontra o script principal no pai
        parentDragHandler = GetComponentInParent<DeckDragHandler>();
        if (parentDragHandler == null)
        {
            Debug.LogError("CardImageDragProxy: Não foi possível encontrar um DeckDragHandler no objeto pai!", gameObject);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentDragHandler != null)
        {
            parentDragHandler.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentDragHandler != null)
        {
            parentDragHandler.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentDragHandler != null)
        {
            parentDragHandler.OnEndDrag(eventData);
        }
    }
}
