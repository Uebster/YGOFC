using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class RitualUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainPanel;
    public TextMeshProUGUI titleText;
    public Transform ritualMonstersContent; // Monstros de Ritual na mão
    public Transform handTributesContent;   // Tributos da mão
    public Transform fieldTributesContent;  // Tributos do campo
    public Button confirmButton;
    public Button cancelButton;
    public GameObject cardItemPrefab;

    private CardData selectedRitualMonster;
    private List<CardData> selectedTributes = new List<CardData>();
    private CardDisplay sourceRitualSpell; // A carta que iniciou o ritual

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        if(mainPanel != null) mainPanel.SetActive(false);
    }

    public void Show(CardDisplay source)
    {
        sourceRitualSpell = source;
        selectedRitualMonster = null;
        selectedTributes.Clear();
        mainPanel.SetActive(true);
        PopulateLists();
        UpdateConfirmButton();
    }

    private void PopulateLists()
    {
        ClearContent();
        var hand = GameManager.Instance.GetPlayerHandData();

        // Popula a lista de Monstros de Ritual na mão
        var ritualMonstersInHand = hand.Where(c => c.type.Contains("Ritual") && c.type.Contains("Monster")).ToList();
        foreach (var card in ritualMonstersInHand)
        {
            CreateCardItem(card, ritualMonstersContent, () => SelectRitualMonster(card));
        }

        // Popula a lista de possíveis tributos da mão
        var handTributes = hand.Where(c => c.type.Contains("Monster") && !c.type.Contains("Ritual")).ToList();
        foreach (var card in handTributes)
        {
            CreateCardItem(card, handTributesContent, () => ToggleTributeSelection(card));
        }

        // Popula a lista de possíveis tributos do campo
        foreach (var zone in GameManager.Instance.duelFieldUI.playerMonsterZones)
        {
            if (zone.childCount > 0)
            {
                var display = zone.GetChild(0).GetComponent<CardDisplay>();
                if (display != null && !display.isFlipped)
                {
                    CreateCardItem(display.CurrentCardData, fieldTributesContent, () => ToggleTributeSelection(display.CurrentCardData));
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

    private void SelectRitualMonster(CardData ritualMonster)
    {
        selectedRitualMonster = ritualMonster;
        Debug.Log($"Monstro de Ritual selecionado: {ritualMonster.name}");
        UpdateConfirmButton();
    }

    private void ToggleTributeSelection(CardData tribute)
    {
        if (selectedTributes.Contains(tribute))
        {
            selectedTributes.Remove(tribute);
            Debug.Log($"Tributo desmarcado: {tribute.name}");
        }
        else
        {
            selectedTributes.Add(tribute);
            Debug.Log($"Tributo selecionado: {tribute.name}");
        }
        UpdateConfirmButton();
    }

    private void RefreshHighlights()
    {
        foreach (var item in spawnedItems)
        {
            var display = item.GetComponent<CardDisplay>();
            if (display != null)
            {
                bool isSelected = (display.CurrentCardData == selectedRitualMonster) || selectedTributes.Contains(display.CurrentCardData);
                display.SetTributeHighlight(isSelected);
            }
        }
    }

    private void UpdateConfirmButton()
    {
        bool isValid = RitualManager.Instance.ValidateRitual(sourceRitualSpell.CurrentCardData, selectedRitualMonster, selectedTributes);
        confirmButton.interactable = isValid;
    }

    private void OnConfirm()
    {
        Debug.Log("Confirmando Ritual...");
        GameManager.Instance.PerformRitualSummon(sourceRitualSpell, selectedRitualMonster, selectedTributes);
        Close();
    }

    private void OnCancel()
    {
        Debug.Log("Ritual Cancelado.");
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
