using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public CardData cardData;
    public DeckZoneType sourceZone;

    public static DeckDragHandler currentDragged;
    public bool wasDropped = false;

    private GameObject dragObject;
    private CanvasGroup dragObjectCanvasGroup;
    private CanvasGroup sourceCanvasGroup;
    private bool isDragging = false;
    private ScrollRect scrollRect;

    void Awake()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Verificações de segurança
        if (cardData == null)
        {
            Debug.LogError("[DeckDragHandler] cardData é nulo!", gameObject);
            return;
        }

        // Para cartas do baú, verifica se está disponível
        if (sourceZone == DeckZoneType.Trunk)
        {
            ChestCardItem chestItem = GetComponent<ChestCardItem>();
            if (chestItem != null && chestItem.availableCopies <= 0)
            {
                Debug.Log("[DeckDragHandler] Carta indisponível no baú, cancelando drag");
                return;
            }
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DeckDragHandler] Falha ao iniciar o arrasto: Não foi possível encontrar um Canvas na hierarquia pai.", gameObject);
            return;
        }

        isDragging = true;
        currentDragged = this;
        wasDropped = false;

        // Desabilitar scroll durante o drag
        if (scrollRect != null)
        {
            scrollRect.enabled = false;
        }

        // --- LÓGICA DE VISUAL DE ARRASTO LIMPO ---
        // 1. Cria um objeto vazio para ser o visual
        dragObject = new GameObject("CardDragVisual");
        dragObject.transform.SetParent(canvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        // Adiciona Canvas para garantir que fique no topo
        Canvas dragCanvas = dragObject.AddComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingOrder = 100; // Alto para ficar no topo
        dragObject.AddComponent<GraphicRaycaster>();

        // 2. Adiciona um RawImage para mostrar a arte da carta
        RawImage dragImage = dragObject.AddComponent<RawImage>();

        // 3. Copia a textura e o tamanho da imagem original
        RawImage originalCardArt = GetComponentInChildren<RawImage>(true);
        
        if (originalCardArt != null && originalCardArt.texture != null)
        {
            dragImage.texture = originalCardArt.texture;
            dragImage.rectTransform.sizeDelta = new Vector2(96f, 135f); // Tamanho fixo para o drag visual
        }
        else
        {
            dragImage.rectTransform.sizeDelta = new Vector2(96f, 135f);
        }

        // 4. Posiciona sob o cursor
        dragObject.transform.position = eventData.position;

        // 5. Adiciona CanvasGroup
        dragObjectCanvasGroup = dragObject.AddComponent<CanvasGroup>();
        dragObjectCanvasGroup.blocksRaycasts = false;
        dragObjectCanvasGroup.alpha = 0.9f;

        // 6. Torna o objeto original semi-invisível
        sourceCanvasGroup = GetComponent<CanvasGroup>();
        if (sourceCanvasGroup == null)
        {
            sourceCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        sourceCanvasGroup.alpha = 0.4f;
        sourceCanvasGroup.blocksRaycasts = false; // Importante: permite que objetos abaixo recebam eventos
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragObject != null && isDragging)
        {
            dragObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = 1f;
            sourceCanvasGroup.blocksRaycasts = true;
        }

        if (dragObject != null)
            Destroy(dragObject);

        // Se não foi dropped em uma zona, e veio de um deck, remove (lixeira)
        if (!wasDropped && sourceZone != DeckZoneType.Trunk && DeckBuilderManager.Instance != null)
        {
            var manager = DeckBuilderManager.Instance;
            manager.RemoveCard(cardData, sourceZone);
            manager.RefreshAllUI(); // Atualiza a UI após a remoção
        }

        currentDragged = null;
        isDragging = false;

        // Reabilitar scroll após o drag
        if (scrollRect != null)
        {
            scrollRect.enabled = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Previne clique durante o arrasto
        if (isDragging) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (DeckBuilderManager.Instance == null || cardData == null) return;

            if (sourceZone == DeckZoneType.Trunk)
            {
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
                DeckBuilderManager.Instance.RefreshAllUI(); // Atualiza a UI após a ação
            }
            else
            {
                DeckBuilderManager.Instance.RemoveCard(cardData, sourceZone);
                DeckBuilderManager.Instance.RefreshAllUI(); // Atualiza a UI após a ação
            }
        }
    }
}