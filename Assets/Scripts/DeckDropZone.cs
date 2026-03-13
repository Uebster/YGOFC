using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckDropZone : MonoBehaviour, IDropHandler
{
    public DeckZoneType zoneType;

    // Garante que este objeto possa receber eventos de drop
    private void Start()
    {
        // Cria um filho invisível para receber raycasts sem afetar o layout do container
        if (transform.Find("DropTarget") == null)
        {
            GameObject dropTarget = new GameObject("DropTarget");
            dropTarget.transform.SetParent(transform, false);
            RectTransform rt = dropTarget.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = dropTarget.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Invisível
            img.raycastTarget = true;

            // Garante que fique no topo para não ser bloqueado pelas cartas
            dropTarget.transform.SetAsLastSibling();
        }
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
                else
                {
                    // Remove uma cópia do baú
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.playerTrunk.Remove(DeckDragHandler.currentDragged.cardData.id);
                    }
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