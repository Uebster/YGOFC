using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GlobalCardSearchUI : MonoBehaviour
{
    public static GlobalCardSearchUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TMP_InputField searchInput;
    public Transform contentArea; 
    public GameObject cardItemPrefab; // Use o mesmo Card_PrefabChestList do DeckBuilder
    public Button closeButton;

    private System.Action<CardData> onConfirm;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<CardData> allCards = new List<CardData>();

    void Awake()
    {
        Instance = this;
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        if (searchInput) searchInput.onValueChanged.AddListener(OnSearchValueChanged);
        gameObject.SetActive(false);
    }

    public void Show(string title, System.Action<CardData> callback)
    {
        onConfirm = callback;
        if (titleText) titleText.text = title;
        if (searchInput) searchInput.text = "";

        if (GameManager.Instance != null && GameManager.Instance.cardDatabase != null)
        {
            allCards = GameManager.Instance.cardDatabase.cardDatabase;
        }

        gameObject.SetActive(true);
        RefreshUI("");
        
        // Foca no input de texto automaticamente
        if (searchInput) searchInput.Select();
    }

    void OnSearchValueChanged(string query)
    {
        RefreshUI(query);
    }

    void RefreshUI(string query)
    {
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();

        if (allCards == null || allCards.Count == 0) return;

        IEnumerable<CardData> filtered = allCards;
        if (!string.IsNullOrEmpty(query))
        {
            filtered = allCards.Where(c => c.name.ToLowerInvariant().Contains(query.ToLowerInvariant()));
        }

        // Limita a 50 resultados para não travar o jogo enquanto digita
        var results = filtered.Take(50).ToList();

        foreach (var card in results)
        {
            GameObject go = Instantiate(cardItemPrefab, contentArea);
            spawnedObjects.Add(go);

            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display == null) display = go.AddComponent<CardDisplay>();
            
            display.SetCard(card, GameManager.Instance.GetCardBackTexture(), true);
            display.isInteractable = false; 

            Button btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            
            btn.onClick.RemoveAllListeners();
            CardData currentCard = card;
            btn.onClick.AddListener(() => ConfirmSelection(currentCard));
        }
    }

    void ConfirmSelection(CardData card)
    {
        gameObject.SetActive(false);
        onConfirm?.Invoke(card);
    }

    void CancelSelection()
    {
        gameObject.SetActive(false);
        onConfirm?.Invoke(null);
    }
}
