using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ReorderCardsUI : MonoBehaviour
{
    public static ReorderCardsUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform contentArea; // Deve ter um HorizontalLayoutGroup
    public GameObject cardItemPrefab; // Deve conter CardDisplay, TextMeshProUGUI e receber CanvasGroup
    public Button confirmButton;

    private List<CardData> originalCards;
    private System.Action<List<CardData>> onConfirm;
    private List<ReorderableCardItem> spawnedItems = new List<ReorderableCardItem>();

    void Awake()
    {
        Instance = this;
        if (confirmButton) confirmButton.onClick.AddListener(Confirm);
        gameObject.SetActive(false);
    }

    public void Show(List<CardData> cards, string title, System.Action<List<CardData>> callback)
    {
        originalCards = new List<CardData>(cards);
        onConfirm = callback;

        if (titleText) titleText.text = title;

        foreach (var obj in spawnedItems) Destroy(obj.gameObject);
        spawnedItems.Clear();

        for (int i = 0; i < cards.Count; i++)
        {
            GameObject go = Instantiate(cardItemPrefab, contentArea);
            ReorderableCardItem item = go.GetComponent<ReorderableCardItem>();
            if (item == null) item = go.AddComponent<ReorderableCardItem>();
            
            item.Setup(cards[i]);
            spawnedItems.Add(item);
        }

        UpdateLabels();
        gameObject.SetActive(true);
    }

    public void UpdateLabels()
    {
        int order = 1;
        int total = spawnedItems.Count;
        for (int i = 0; i < contentArea.childCount; i++)
        {
            Transform child = contentArea.GetChild(i);
            ReorderableCardItem item = child.GetComponent<ReorderableCardItem>();
            
            if (item != null || child.name == "Placeholder")
            {
                if (item != null)
                {
                    string label = (order == 1) ? "1º (Topo)" : (order == total) ? $"{order}º (Fundo)" : $"{order}º";
                    item.SetLabel(label);
                }
                order++;
            }
        }
    }

    void Confirm()
    {
        List<CardData> reordered = new List<CardData>();
        for (int i = 0; i < contentArea.childCount; i++)
        {
            ReorderableCardItem item = contentArea.GetChild(i).GetComponent<ReorderableCardItem>();
            if (item != null) reordered.Add(item.cardData);
        }
        gameObject.SetActive(false);
        onConfirm?.Invoke(reordered);
    }
}

public class ReorderableCardItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    private TextMeshProUGUI orderLabel;
    private Transform originalParent;
    private int placeholderIndex;
    private GameObject placeholder;

    public void Setup(CardData data)
    {
        cardData = data;
        orderLabel = GetComponentInChildren<TextMeshProUGUI>();

        CardDisplay cd = GetComponent<CardDisplay>();
        if (cd != null)
        {
            cd.SetCard(data, GameManager.Instance.GetCardBackTexture(), true);
            cd.isInteractable = false; // Desativa popups e clicks do duelo
        }
        
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetLabel(string text)
    {
        if (orderLabel != null) orderLabel.text = text;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        placeholderIndex = transform.GetSiblingIndex();

        // Cria um espaço falso (Placeholder) que empurra as cartas usando o Layout
        placeholder = new GameObject("Placeholder");
        RectTransform rt = placeholder.AddComponent<RectTransform>();
        RectTransform myRt = GetComponent<RectTransform>();
        rt.sizeDelta = myRt.sizeDelta;
        
        LayoutElement le = placeholder.AddComponent<LayoutElement>();
        LayoutElement myLe = GetComponent<LayoutElement>();
        if (myLe != null) {
            le.preferredWidth = myLe.preferredWidth; le.preferredHeight = myLe.preferredHeight;
        } else {
            le.preferredWidth = myRt.rect.width; le.preferredHeight = myRt.rect.height;
        }

        placeholder.transform.SetParent(originalParent, false);
        placeholder.transform.SetSiblingIndex(placeholderIndex);

        // Move a carta arrastada para fora do layout para seguir o mouse livremente
        transform.SetParent(originalParent.parent, true); 
        GetComponent<CanvasGroup>().blocksRaycasts = false; // Permite que o mouse veja o que está atrás da carta
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
        int newSiblingIndex = originalParent.childCount;

        // Verifica onde o mouse está em relação as outras cartas e empurra o placeholder
        for (int i = 0; i < originalParent.childCount; i++)
        {
            if (this.transform.position.x < originalParent.GetChild(i).position.x)
            {
                newSiblingIndex = i;
                if (placeholder.transform.GetSiblingIndex() < newSiblingIndex)
                    newSiblingIndex--;
                break;
            }
        }
        placeholder.transform.SetSiblingIndex(newSiblingIndex);
        ReorderCardsUI.Instance.UpdateLabels();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Solta a carta no espaço do placeholder
        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        Destroy(placeholder);
        ReorderCardsUI.Instance.UpdateLabels();
    }
}