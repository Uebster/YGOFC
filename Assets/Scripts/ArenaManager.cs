using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ArenaManager : MonoBehaviour
{
    [Header("Configuração da Grade")]
    public GameObject slotPrefab;
    public Transform gridContent; // O objeto com o Grid Layout Group
    public CampaignDatabase campaignDB;

    [Header("Painel Lateral (Detalhes)")]
    public Image largeAvatar;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText; // Pode usar para mostrar Deck/Dificuldade
    public TextMeshProUGUI statsText; // Ex: "Dificuldade: Difícil"
    public Button duelButton;
    public GameObject infoPanel; // Para esconder se nada selecionado

    private List<ArenaSlot> allSlots = new List<ArenaSlot>();
    private CharacterData selectedCharacter;
    private ArenaSlot selectedSlot;

    void Start()
    {
        if (duelButton != null)
            duelButton.onClick.AddListener(OnDuelClick);
            
        // Gera a grade na primeira vez
        GenerateGrid();
    }

    void OnEnable()
    {
        // Atualiza o estado de desbloqueio toda vez que abre a tela
        RefreshGridState();
        UpdateInfoPanel();
    }

    public void GenerateGrid()
    {
        // Limpa slots antigos
        foreach (Transform child in gridContent) Destroy(child.gameObject);
        allSlots.Clear();

        if (campaignDB == null)
        {
            Debug.LogError("ArenaManager: CampaignDatabase não atribuído!");
            return;
        }

        // Cria 100 slots (10 Atos x 10 Oponentes)
        for (int i = 1; i <= 100; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, gridContent);
            ArenaSlot newSlot = newSlotObj.GetComponent<ArenaSlot>();
            
            if (newSlot == null) newSlot = newSlotObj.AddComponent<ArenaSlot>();

            // Obtém dados do oponente para este nível global (1 a 100)
            string charID = campaignDB.GetOpponentIdByGlobalLevel(i);
            CharacterData charData = GameManager.Instance.characterDatabase.GetCharacterById(charID);

            // Configuração inicial (será atualizada no Refresh)
            newSlot.Setup(i, charData, false, this);
            allSlots.Add(newSlot);
        }
    }

    public void RefreshGridState()
    {
        int maxUnlocked = 1;
        if (CampaignManager.Instance != null)
            maxUnlocked = CampaignManager.Instance.maxUnlockedLevel;

        bool devMode = GameManager.Instance != null && GameManager.Instance.devMode;

        for (int i = 0; i < allSlots.Count; i++)
        {
            int levelIndex = i + 1;
            // Um personagem é liberado na Arena se já foi VENCIDO na campanha.
            // Se eu venci o nível 1, maxUnlocked vira 2. Então o char 1 está livre.
            // Lógica: Se maxUnlocked > levelIndex, então já venci esse level.
            bool isUnlocked = (maxUnlocked > levelIndex) || devMode;

            // Recarrega o slot com o estado correto
            string charID = campaignDB.GetOpponentIdByGlobalLevel(levelIndex);
            CharacterData charData = GameManager.Instance.characterDatabase.GetCharacterById(charID);
            
            allSlots[i].Setup(levelIndex, charData, isUnlocked, this);
        }
    }

    public void SelectCharacter(ArenaSlot slot, CharacterData character)
    {
        // Desmarca o anterior
        if (selectedSlot != null) selectedSlot.Deselect();

        selectedSlot = slot;
        selectedCharacter = character;
        
        UpdateInfoPanel();
    }

    void UpdateInfoPanel()
    {
        if (selectedCharacter == null)
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            if (duelButton != null) duelButton.interactable = false;
            return;
        }

        if (infoPanel != null) infoPanel.SetActive(true);
        if (duelButton != null) duelButton.interactable = true;

        if (nameText != null) nameText.text = selectedCharacter.name;
        
        // Descrição e Stats
        if (descriptionText != null) 
            descriptionText.text = $"Deck: {selectedCharacter.deck_A?.Count ?? 40} cartas\nEstratégia: {selectedCharacter.difficulty}";
            
        if (statsText != null)
            statsText.text = $"Recompensa: {selectedCharacter.rewards?.Count ?? 0} cartas";

        // Avatar Grande
        if (largeAvatar != null)
        {
            Sprite charSprite = Resources.Load<Sprite>($"Characters/{selectedCharacter.id}");
            if (charSprite != null) largeAvatar.sprite = charSprite;
            else largeAvatar.color = Color.gray; // Placeholder
        }
    }

    void OnDuelClick()
    {
        if (selectedCharacter != null && GameManager.Instance != null)
        {
            Debug.Log($"Arena: Iniciando duelo contra {selectedCharacter.name}");
            // Inicia duelo sem índice de campanha (Free Duel)
            GameManager.Instance.StartDuel(selectedCharacter, -1);
            
            if (UIManager.Instance != null)
                UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
        }
    }
}
