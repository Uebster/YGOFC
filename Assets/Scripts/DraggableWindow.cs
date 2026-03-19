using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Traz a janela para a frente de tudo ao clicar nela
        rectTransform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}