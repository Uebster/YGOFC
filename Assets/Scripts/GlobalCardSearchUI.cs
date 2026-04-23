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
    private const int itemsPerPage = 36;
    private int totalPages = 1;
    private bool dropdownsInitialized = false;

    private float enableTime;
    void OnEnable() { enableTime = Time.unscaledTime; }

    void Update()
    {
        if (gameObject.activeSelf && Time.unscaledTime - enableTime > 0.1f)
        {
            bool cancelPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) cancelPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) cancelPressed = true;
            if (Input.GetMouseButtonDown(1)) cancelPressed = true;
#endif
            if (cancelPressed)
            {
                if (GameManager.Instance != null) GameManager.Instance.justCanceledSomething = true;
                CancelSelection();
            }
        }
    }

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
        if (dropdownsInitialized) return;

        if (typeDropdown != null)
        {
            typeDropdown.ClearOptions();
            typeDropdown.AddOptions(new List<string> { "All Types", "Monster", "Monster (Normal)", "Monster (Effect)", "Spell", "Trap" });
        }

        if (raceDropdown != null)
        {
            raceDropdown.ClearOptions();
            var races = allCards.Where(c => !string.IsNullOrEmpty(c.race)).Select(c => c.race).Distinct().OrderBy(r => r).ToList();
            races.Insert(0, "All Races");
            raceDropdown.AddOptions(races);
        }

        if (subTypeDropdown != null)
        {
            subTypeDropdown.ClearOptions();
            var props = allCards.Where(c => !string.IsNullOrEmpty(c.property)).Select(c => c.property).Distinct().OrderBy(p => p).ToList();
            props.Insert(0, "All Subtypes");
            subTypeDropdown.AddOptions(props);
        }
        
        dropdownsInitialized = true;
    }

    void OnFilterChanged()
    {
        if (allCards == null || allCards.Count == 0) return;

        string query = searchInput ? searchInput.text.ToLowerInvariant() : "";
        string selectedType = typeDropdown != null && typeDropdown.value > 0 ? typeDropdown.options[typeDropdown.value].text.ToLowerInvariant() : "";
        string selectedRace = raceDropdown != null && raceDropdown.value > 0 ? raceDropdown.options[raceDropdown.value].text : "";
        string selectedProp = subTypeDropdown != null && subTypeDropdown.value > 0 ? subTypeDropdown.options[subTypeDropdown.value].text : "";

        filteredCards = allCards.Where(c => 
        {
            if (!string.IsNullOrEmpty(query))
            {
                bool nameMatch = c.name != null && c.name.ToLowerInvariant().Contains(query);
                bool idMatch = c.id != null && c.id.ToLowerInvariant().Contains(query);
                bool passMatch = c.password != null && c.password.ToLowerInvariant().Contains(query);
                
                if (!nameMatch && !idMatch && !passMatch)
                    return false;
            }
            
            if (!string.IsNullOrEmpty(selectedType))
            {
                if (c.type == null) return false;
                string cType = c.type.ToLowerInvariant();
                if (selectedType == "monster (normal)")
                {
                    if (!cType.Contains("monster") || !cType.Contains("normal")) return false;
                }
                else if (selectedType == "monster (effect)")
                {
                    if (!cType.Contains("monster") || !cType.Contains("effect")) return false;
                }
                else if (!cType.Contains(selectedType)) return false;
            }

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
