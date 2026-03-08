using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DuelistLibraryManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform listContent; // Onde os botões dos duelistas serão criados
    public GameObject duelistItemPrefab; // Prefab do botão na lista
    
    [Header("Detail View")]
    public RawImage avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public GameObject detailPanel; // O painel que contém os detalhes (para ativar se necessário)

    private List<CharacterData> allCharacters;

    void OnEnable()
    {
        LoadDuelists();
    }

    public void LoadDuelists()
    {
        if (GameManager.Instance == null || GameManager.Instance.characterDatabase == null) return;

        // Limpa lista atual
        foreach (Transform child in listContent) Destroy(child.gameObject);

        allCharacters = new List<CharacterData>(GameManager.Instance.characterDatabase.characterDatabase);
        // Ordena por ID ou Nome
        allCharacters.Sort((a, b) => a.id.CompareTo(b.id));

        foreach (var character in allCharacters)
        {
            // TODO: Integrar com SaveLoadSystem para pegar vitórias reais
            // int wins = SaveLoadSystem.Instance.GetWins(character.id);
            int wins = 1; // DEBUG: Força desbloqueio para visualizar a UI

            if (wins > 0)
            {
                GameObject item = Instantiate(duelistItemPrefab, listContent);
                
                // Configura texto do botão
                TextMeshProUGUI btnText = item.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText) btnText.text = character.name;

                // Configura clique
                Button btn = item.GetComponent<Button>();
                if (btn)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowDetails(character));
                }
            }
        }
    }

    void ShowDetails(CharacterData character)
    {
        if (detailPanel) detailPanel.SetActive(true);
        if (nameText) nameText.text = character.name;
        
        // Formata a descrição
        if (descriptionText)
        {
            string desc = $"<b>Difficulty:</b> {character.difficulty}\n";
            desc += $"<b>Field:</b> {character.field}\n\n";
            // Se tiver campo de 'Lore' no CharacterData, adicionaria aqui
            desc += $"ID: {character.id}"; 
            descriptionText.text = desc;
        }

        // Carrega Avatar (Assumindo que existe um método ou sistema de recursos para isso)
        // Por enquanto, usamos um placeholder ou tentamos carregar se tiver o caminho
        // StartCoroutine(LoadAvatar(character.id)); 
    }
}
