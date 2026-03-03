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

    void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
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

        // Popula o Extra Deck (Monstros de Fusão)
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
                var display = zone.GetChild(0).GetComponent<CardDisplay>();
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
                display.SetTributeHighlight(isSelected); // Reutilizando o destaque de tributo para seleção
            }
        }
    }

    private void UpdateConfirmButton()
    {
        bool isValid = FusionManager.Instance.ValidateFusion(selectedFusionMonster, selectedMaterials);
        confirmButton.interactable = isValid;
    }

    private void OnConfirm()
    {
        Debug.Log("Confirmando Fusão...");
        GameManager.Instance.PerformFusionSummon(sourceCard, selectedFusionMonster, selectedMaterials);
        Close();
    }

    private void OnCancel()
    {
        Debug.Log("Fusão Cancelada.");
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
