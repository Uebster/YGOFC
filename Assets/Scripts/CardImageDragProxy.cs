using UnityEngine;
using UnityEngine.EventSystems;

public class CardImageDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler
{
    private DeckDragHandler parentDragHandler;
    private ChestCardItem parentCardItem; // Para o hover
    private bool hasDragHandler = false;

    void Awake()
    {
        parentDragHandler = GetComponentInParent<DeckDragHandler>();
        parentCardItem = GetComponentInParent<ChestCardItem>();

        if (parentDragHandler != null && parentCardItem != null)
        {
            hasDragHandler = true;
        }
        else
        {
            Debug.LogError("CardImageDragProxy: Não foi possível encontrar DeckDragHandler ou ChestCardItem no pai!", this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hasDragHandler)
        {
            parentDragHandler.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hasDragHandler)
        {
            parentDragHandler.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (hasDragHandler)
        {
            parentDragHandler.OnEndDrag(eventData);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasDragHandler)
        {
            parentDragHandler.OnPointerClick(eventData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hasDragHandler)
        {
            parentCardItem.OnPointerEnter(eventData);
        }
    }
}