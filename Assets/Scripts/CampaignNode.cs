using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class CampaignNode : MonoBehaviour
{
    public enum NodeType { Home, Duel }

    [Header("Configuração do Nível")]
    public int levelIndex; // 0 para Home, 1 para Act 1, etc.
    public NodeType nodeType = NodeType.Duel;
    public List<string> opponentIDs; // Lista de oponentes neste Act (ex: 10 oponentes)
    
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
        
        // Verifica se completou todos os oponentes deste nó
        int nextLevelStart = levelIndex + (opponentIDs != null ? opponentIDs.Count : 0);
        bool isNodeCompleted = CampaignManager.Instance.maxUnlockedLevel >= nextLevelStart;
        
        if (clearIcon != null) clearIcon.SetActive(isNodeCompleted && nodeType == NodeType.Duel);

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

        if (opponentIDs == null || opponentIDs.Count == 0)
        {
            Debug.LogError($"CampaignNode {name} não tem oponentes configurados!");
            return;
        }

        // Determina qual oponente enfrentar com base no progresso global
        int currentGlobalLevel = CampaignManager.Instance.maxUnlockedLevel;
        int localIndex = currentGlobalLevel - levelIndex;

        // Se já completou este nó, enfrenta o último (boss) ou mantém o índice dentro do limite para replay
        if (localIndex >= opponentIDs.Count) localIndex = opponentIDs.Count - 1;
        if (localIndex < 0) localIndex = 0;

        string currentOpponentID = opponentIDs[localIndex];

        // Busca os dados do oponente
        CharacterData opponent = GameManager.Instance.characterDatabase.GetCharacterById(currentOpponentID);
        
        if (opponent != null)
        {
            int duelIndex = levelIndex + localIndex;
            Debug.Log($"Iniciando duelo contra: {opponent.name} (Act {levelIndex} - Batalha {localIndex + 1}/{opponentIDs.Count})");
            GameManager.Instance.StartDuel(opponent, duelIndex);
            UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
        }
        else
        {
            Debug.LogError($"Oponente com ID '{currentOpponentID}' não encontrado no CharacterDatabase!");
        }
    }
}