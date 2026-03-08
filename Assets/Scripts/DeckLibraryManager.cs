using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class DeckLibraryManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform listContent;
    public GameObject deckItemPrefab; // Botão na lista

    [Header("Detail View")]
    public TextMeshProUGUI deckNameText;
    public TextMeshProUGUI deckListText; // Texto rolável com a lista de cartas
    public GameObject variantButtonsPanel; // Painel com botões A, B, C
    public Button btnDeckA;
    public Button btnDeckB;
    public Button btnDeckC;

    private CharacterData currentCharacter;
    private int currentWins;

    void Start()
    {
        if (btnDeckA) btnDeckA.onClick.AddListener(() => ShowDeckList("A"));
        if (btnDeckB) btnDeckB.onClick.AddListener(() => ShowDeckList("B"));
        if (btnDeckC) btnDeckC.onClick.AddListener(() => ShowDeckList("C"));
    }

    void OnEnable()
    {
        LoadDeckLibrary();
    }

    public void LoadDeckLibrary()
    {
        if (GameManager.Instance == null || GameManager.Instance.characterDatabase == null) return;

        foreach (Transform child in listContent) Destroy(child.gameObject);

        var characters = GameManager.Instance.characterDatabase.characterDatabase;
        
        foreach (var character in characters)
        {
            // TODO: Integrar com SaveLoadSystem
            // int wins = SaveLoadSystem.Instance.GetWins(character.id);
            int wins = 55; // DEBUG: Simula 55 vitórias (Libera Deck A)

            if (wins > 0) // Aparece na lista se venceu pelo menos 1 vez
            {
                GameObject item = Instantiate(deckItemPrefab, listContent);
                TextMeshProUGUI txt = item.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = character.name + "'s Deck";

                Button btn = item.GetComponent<Button>();
                if (btn)
                {
                    btn.onClick.AddListener(() => SelectCharacter(character, wins));
                }
            }
        }
    }

    void SelectCharacter(CharacterData character, int wins)
    {
        currentCharacter = character;
        currentWins = wins;
        
        if (deckNameText) deckNameText.text = $"{character.name}'s Decks";
        if (variantButtonsPanel) variantButtonsPanel.SetActive(true);

        // Atualiza estado dos botões baseado nas vitórias
        if (btnDeckA) btnDeckA.interactable = (wins >= 50);
        if (btnDeckB) btnDeckB.interactable = (wins >= 100);
        if (btnDeckC) btnDeckC.interactable = (wins >= 200);

        // Mostra o primeiro disponível por padrão
        if (wins >= 50) ShowDeckList("A");
        else 
        {
            if (deckListText) deckListText.text = "Vença 50 duelos contra este oponente na Arena para desbloquear o Deck A.";
        }
    }

    void ShowDeckList(string variant)
    {
        if (currentCharacter == null) return;

        List<string> deckIDs = null;
        if (variant == "A") deckIDs = currentCharacter.deck_A;
        else if (variant == "B") deckIDs = currentCharacter.deck_B;
        else if (variant == "C") deckIDs = currentCharacter.deck_C;

        if (deckIDs == null || deckIDs.Count == 0)
        {
            if (deckListText) deckListText.text = "Deck vazio ou não existente.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>Deck {variant} List:</b>\n");

        // Agrupa e conta cartas
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string id in deckIDs)
        {
            if (counts.ContainsKey(id)) counts[id]++;
            else counts[id] = 1;
        }

        foreach (var kvp in counts)
        {
            CardData card = GameManager.Instance.cardDatabase.GetCardById(kvp.Key);
            if (card != null)
            {
                sb.AppendLine($"{kvp.Value}x {card.name}");
            }
        }

        if (deckListText) deckListText.text = sb.ToString();
    }
}
