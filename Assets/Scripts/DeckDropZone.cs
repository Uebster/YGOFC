using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckDropZone : MonoBehaviour, IDropHandler
{
    public DeckZoneType zoneType;

    // Garante que este objeto possa receber eventos de drop
    private void Start()
    {
        // Adiciona uma imagem invisível para receber raycasts se não houver
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Invisível
        }
        img.raycastTarget = true;

        // Garante que fique no topo para não ser bloqueado pelas cartas
        transform.SetAsLastSibling();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DeckDragHandler.currentDragged != null)
        {
            bool success = DeckBuilderManager.Instance?.AddCardToDeck(DeckDragHandler.currentDragged.cardData, zoneType) ?? false;
            if (success)
            {
                if (DeckDragHandler.currentDragged.sourceZone != DeckZoneType.Trunk)
                {
                    DeckBuilderManager.Instance?.RemoveCard(DeckDragHandler.currentDragged.cardData, DeckDragHandler.currentDragged.sourceZone);
                }
                DeckDragHandler.currentDragged.wasDropped = true;
            }
            else
            {
                DeckBuilderManager.Instance?.TriggerInvalidMoveFeedback(zoneType);
            }
        }
    }
}