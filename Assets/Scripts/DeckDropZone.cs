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
            var dragHandler = DeckDragHandler.currentDragged;
            var manager = DeckBuilderManager.Instance;
            if (manager == null) return;

            CardData card = dragHandler.cardData;
            DeckZoneType fromZone = dragHandler.sourceZone;

            // Se soltar na mesma zona de onde veio, não faz nada, apenas finaliza o drag.
            if (fromZone == zoneType)
            {
                dragHandler.wasDropped = true;
                return;
            }

            // Lógica de Movimentação: Remove primeiro, depois tenta adicionar.
            if (fromZone != DeckZoneType.Trunk)
            {
                manager.RemoveCard(card, fromZone);
            }

            bool success = manager.AddCardToDeck(card, zoneType);

            if (!success)
            {
                // Adição falhou, devolve a carta para a zona de origem (se não for o baú)
                if (fromZone != DeckZoneType.Trunk)
                {
                    manager.AddCardToDeck(card, fromZone);
                }
                manager.TriggerInvalidMoveFeedback(zoneType);
            }

            dragHandler.wasDropped = true; // Marca como dropado para não ser removido no OnEndDrag
        }
    }
}