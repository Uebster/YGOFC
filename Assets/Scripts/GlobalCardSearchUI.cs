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

    [Header("Filtros e Paginação (Adicione na UI)")]
    public TMP_Dropdown typeDropdown;
    public TMP_Dropdown raceDropdown;
    public TMP_Dropdown subTypeDropdown;
    public TextMeshProUGUI pageText;
    public Button prevButton;
    public Button nextButton;

    private System.Action<CardData> onConfirm;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<CardData> allCards = new List<CardData>();
    private List<CardData> filteredCards = new List<CardData>();

    private int currentPage = 1;
    private const int itemsPerPage = 30;
    private int totalPages = 1;

    void Awake()
    {
        Instance = this;
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        
        if (searchInput) searchInput.onValueChanged.AddListener(delegate { OnFilterChanged(); });
        if (typeDropdown) typeDropdown.onValueChanged.AddListener(delegate { OnFilterChanged(); });
        if (raceDropdown) raceDropdown.onValueChanged.AddListener(delegate { OnFilterChanged(); });
        if (subTypeDropdown) subTypeDropdown.onValueChanged.AddListener(delegate { OnFilterChanged(); });

        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);

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

        InitializeDropdowns();

        gameObject.SetActive(true);
        OnFilterChanged(); 
        
        // Foca no input de texto automaticamente
        if (searchInput) searchInput.Select();
    }

    void InitializeDropdowns()
    {
        if (allCards == null || allCards.Count == 0) return;

        if (typeDropdown != null && typeDropdown.options.Count <= 1)
        {
            typeDropdown.ClearOptions();
            typeDropdown.AddOptions(new List<string> { "Todos os Tipos", "Monster", "Spell", "Trap" });
        }

        if (raceDropdown != null && raceDropdown.options.Count <= 1)
        {
            raceDropdown.ClearOptions();
            var races = allCards.Where(c => !string.IsNullOrEmpty(c.race)).Select(c => c.race).Distinct().OrderBy(r => r).ToList();
            races.Insert(0, "Todas as Raças");
            raceDropdown.AddOptions(races);
        }

        if (subTypeDropdown != null && subTypeDropdown.options.Count <= 1)
        {
            subTypeDropdown.ClearOptions();
            var props = allCards.Where(c => !string.IsNullOrEmpty(c.property)).Select(c => c.property).Distinct().OrderBy(p => p).ToList();
            props.Insert(0, "Todos os Subtipos");
            subTypeDropdown.AddOptions(props);
        }
    }

    void OnFilterChanged()
    {
        if (allCards == null || allCards.Count == 0) return;

        string query = searchInput ? searchInput.text.ToLowerInvariant() : "";
        string selectedType = typeDropdown != null && typeDropdown.value > 0 ? typeDropdown.options[typeDropdown.value].text : "";
        string selectedRace = raceDropdown != null && raceDropdown.value > 0 ? raceDropdown.options[raceDropdown.value].text : "";
        string selectedProp = subTypeDropdown != null && subTypeDropdown.value > 0 ? subTypeDropdown.options[subTypeDropdown.value].text : "";

        filteredCards = allCards.Where(c => 
        {
            if (!string.IsNullOrEmpty(query))
            {
                if (!c.name.ToLowerInvariant().Contains(query) && !c.id.Contains(query) && !(c.password != null && c.password.Contains(query)))
                    return false;
            }
            if (!string.IsNullOrEmpty(selectedType) && !c.type.Contains(selectedType)) return false;
            if (!string.IsNullOrEmpty(selectedRace) && c.race != selectedRace) return false;
            if (!string.IsNullOrEmpty(selectedProp) && c.property != selectedProp) return false;

            return true;
        }).ToList();

        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredCards.Count / itemsPerPage));
        currentPage = 1;

        RefreshPage();
    }

    void RefreshPage()
    {
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();

        int startIndex = (currentPage - 1) * itemsPerPage;
        var pageResults = filteredCards.Skip(startIndex).Take(itemsPerPage).ToList();

        foreach (var card in pageResults)
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

        if (pageText) pageText.text = $"Page {currentPage}/{totalPages}";
        if (prevButton) prevButton.interactable = currentPage > 1;
        if (nextButton) nextButton.interactable = currentPage < totalPages;
    }

    public void NextPage() { if (currentPage < totalPages) { currentPage++; RefreshPage(); } }
    public void PrevPage() { if (currentPage > 1) { currentPage--; RefreshPage(); } }

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
