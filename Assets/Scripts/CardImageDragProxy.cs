using UnityEngine;
using UnityEngine.EventSystems;

public class CardImageDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private DeckDragHandler parentDragHandler;
    private bool hasDragHandler = false;

void Start()
{
    parentDragHandler = GetComponentInParent<DeckDragHandler>();
    if (parentDragHandler == null)
        Debug.LogError("CardImageDragProxy: DeckDragHandler não encontrado no pai!", this);
    else
        Debug.Log("CardImageDragProxy: DeckDragHandler encontrado!");
}

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hasDragHandler && parentDragHandler != null)
        {
            parentDragHandler.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hasDragHandler && parentDragHandler != null)
        {
            parentDragHandler.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (hasDragHandler && parentDragHandler != null)
        {
            parentDragHandler.OnEndDrag(eventData);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasDragHandler && parentDragHandler != null)
        {
            parentDragHandler.OnPointerClick(eventData);
        }
    }
}