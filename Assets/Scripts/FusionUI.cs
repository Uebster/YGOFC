using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class FusionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainPanel;
    public TextMeshProUGUI titleText;
    public Transform extraDeckContent;
    public Transform handContent;
    public Transform fieldContent;
    public Button confirmButton;
    public Button cancelButton;
    public GameObject cardItemPrefab;

    private CardData selectedFusionMonster;
    private List<CardData> selectedMaterials = new List<CardData>();
    private CardDisplay sourceCard; // A carta que iniciou a fusão (ex: Polymerization)

    private List<GameObject> spawnedItems = new List<GameObject>();

    private float enableTime;
    void OnEnable() { enableTime = Time.unscaledTime; }

    void Update()
    {
        if (mainPanel != null && mainPanel.activeSelf && Time.unscaledTime - enableTime > 0.1f)
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
                OnCancel();
            }
        }
    }

    void Awake()
    {
        if (mainPanel == null) mainPanel = this.gameObject;
        if (titleText == null) titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (extraDeckContent == null) extraDeckContent = transform.Find("ScrollView_ExtraDeck/Viewport/Content");
        if (handContent == null) handContent = transform.Find("ScrollView_Hand/Viewport/Content");
        if (fieldContent == null) fieldContent = transform.Find("ScrollView_Field/Viewport/Content");
        if (confirmButton == null) confirmButton = transform.Find("Btn_Confirm")?.GetComponent<Button>();
        if (cancelButton == null) cancelButton = transform.Find("Btn_Cancel")?.GetComponent<Button>();

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        if(mainPanel != null) mainPanel.SetActive(false);
    }

    public void Show(CardDisplay source)
    {
        sourceCard = source;
        selectedFusionMonster = null;
        selectedMaterials.Clear();
        mainPanel.SetActive(true);
        PopulateLists();
        UpdateConfirmButton();
    }

    private void PopulateLists()
    {
        ClearContent();

        // Fallback de segurança para o Prefab
        if (cardItemPrefab == null && GameManager.Instance != null) cardItemPrefab = GameManager.Instance.cardPrefab;

        // Popula o Extra Deck (Monstros de Fusão) via GameManager (compatível com Simulação/Testes)
        var extraDeck = GameManager.Instance.GetPlayerExtraDeck().Where(c => c.type.Contains("Fusion")).ToList();
        foreach (var card in extraDeck)
        {
            CreateCardItem(card, extraDeckContent, () => SelectFusionMonster(card));
        }

        // Popula a Mão
        var hand = GameManager.Instance.GetPlayerHandData();
        foreach (var card in hand)
        {
            if (card.type.Contains("Monster"))
            {
                CreateCardItem(card, handContent, () => ToggleMaterialSelection(card));
            }
        }

        // Popula o Campo
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetComponentInChildren<CardDisplay>();
                if (display != null)
                {
                    CreateCardItem(display.CurrentCardData, fieldContent, () => ToggleMaterialSelection(display.CurrentCardData));
                }
            }
        }
    }

    private void CreateCardItem(CardData card, Transform parent, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject go = Instantiate(cardItemPrefab, parent);
        spawnedItems.Add(go);
        CardDisplay display = go.GetComponent<CardDisplay>();
        display.SetCard(card, GameManager.Instance.GetCardBackTexture(), true);
        display.isInteractable = false;

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);
        btn.onClick.AddListener(RefreshHighlights);
    }

    private void SelectFusionMonster(CardData fusionMonster)
    {
        selectedFusionMonster = fusionMonster;
        Debug.Log($"Monstro de Fusão selecionado: {fusionMonster.name}");
        UpdateConfirmButton();
    }

    private void ToggleMaterialSelection(CardData material)
    {
        if (selectedMaterials.Contains(material))
        {
            selectedMaterials.Remove(material);
            Debug.Log($"Material desmarcado: {material.name}");
        }
        else
        {
            selectedMaterials.Add(material);
            Debug.Log($"Material selecionado: {material.name}");
        }
        UpdateConfirmButton();
    }

    private void RefreshHighlights()
    {
        // Itera por todos os itens de carta e os destaca se estiverem selecionados
        foreach (var item in spawnedItems)
        {
            var display = item.GetComponent<CardDisplay>();
            if (display != null)
            {
                bool isSelected = (display.CurrentCardData == selectedFusionMonster) || selectedMaterials.Contains(display.CurrentCardData);
                display.SetHighlight(HighlightCategory.Fusion, isSelected);
            }
        }
    }

    private void UpdateConfirmButton()
    {
        if (selectedFusionMonster != null && titleText != null)
        {
            int reqCount = 2;
            if (selectedFusionMonster.fusion_materials != null && selectedFusionMonster.fusion_materials.Count > 0)
                reqCount = selectedFusionMonster.fusion_materials.Count;
            else if (selectedFusionMonster.description != null && selectedFusionMonster.description.Contains("+"))
                reqCount = selectedFusionMonster.description.Split('+').Length;

            titleText.text = $"Fusão: {selectedFusionMonster.name} (Materiais: {selectedMaterials.Count} / {reqCount})";
        }
        else if (titleText != null)
        {
            titleText.text = "Selecione a Fusão e os Materiais";
        }

        bool isValid = selectedFusionMonster != null && FusionManager.Instance != null && FusionManager.Instance.ValidateFusion(selectedFusionMonster, selectedMaterials);
        confirmButton.interactable = isValid;
    }

    private void OnConfirm()
    {
        Debug.Log("Confirmando Fusão...");
        var source = sourceCard;
        var target = selectedFusionMonster;
        var mats = new List<CardData>(selectedMaterials);
        Close();
        
        FusionManager.Instance.PerformFusionSummon(source, target, mats, false);
    }

    private void OnCancel()
    {
        Debug.Log("Fusão Cancelada.");
        if (sourceCard != null)
        {
            GameManager.Instance.SendToGraveyard(sourceCard.CurrentCardData, sourceCard.isPlayerCard, CardLocation.Field, SendReason.Rule);
            Destroy(sourceCard.gameObject);
        }
        Close();
    }

    private void Close()
    {
        mainPanel.SetActive(false);
        ClearContent();
    }

    private void ClearContent()
    {
        foreach (var item in spawnedItems)
        {
            if(item != null) Destroy(item);
        }
        spawnedItems.Clear();
    }
}
