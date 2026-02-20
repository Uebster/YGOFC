using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CampaignNode : MonoBehaviour
{
    public enum NodeType { Home, Duel }

    [Header("Configuração do Nível")]
    public int levelIndex; // 0 para Home, 1 para Act 1, etc.
    public NodeType nodeType = NodeType.Duel;
    public string opponentID; // ID do personagem no CharacterDatabase (ex: "simon", "kaiba")
    
    [Header("Visual")]
    public GameObject lockIcon; // Ícone de cadeado (filho do botão)
    public GameObject clearIcon; // Ícone de "V" ou estrela (filho do botão)
    
    private Button btn;
    private MillenniumButton milleniumEffect; // Se estiver usando o efeito visual

    void Start()
    {
        btn = GetComponent<Button>();
        milleniumEffect = GetComponent<MillenniumButton>();
        
        btn.onClick.AddListener(OnNodeClick);
        UpdateVisualState();
    }

    void OnEnable()
    {
        UpdateVisualState();
    }

    public void UpdateVisualState()
    {
        if (CampaignManager.Instance == null) return;

        bool isUnlocked = CampaignManager.Instance.IsLevelUnlocked(levelIndex);
        bool isCompleted = CampaignManager.Instance.maxUnlockedLevel > levelIndex;

        // Configura interatividade
        btn.interactable = isUnlocked;

        // Ícones visuais
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        if (clearIcon != null) clearIcon.SetActive(isCompleted && nodeType == NodeType.Duel);

        // Se tiver o script MillenniumButton, podemos mudar a cor se estiver bloqueado
        if (milleniumEffect != null)
        {
            // Opcional: mudar cor se bloqueado
        }
    }

    void OnNodeClick()
    {
        if (nodeType == NodeType.Home)
        {
            // Abre menu de Home (Deck Builder, Save, etc)
            // Exemplo: UIManager.Instance.ShowScreen(UIManager.Instance.deckBuilderScreen);
            Debug.Log("Entrando no Home...");
        }
        else if (nodeType == NodeType.Duel)
        {
            StartCampaignDuel();
        }
    }

    void StartCampaignDuel()
    {
        if (GameManager.Instance == null || UIManager.Instance == null) return;

        // Busca os dados do oponente
        CharacterData opponent = GameManager.Instance.characterDatabase.GetCharacterById(opponentID);
        
        if (opponent != null)
        {
            Debug.Log($"Iniciando duelo contra: {opponent.name} (Nível {levelIndex})");
            GameManager.Instance.StartDuel(opponent);
            UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
        }
        else
        {
            Debug.LogError($"Oponente com ID '{opponentID}' não encontrado no CharacterDatabase!");
        }
    }
}