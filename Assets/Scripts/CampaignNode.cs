using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class CampaignNode : MonoBehaviour
{
    public enum NodeType { Home, Duel }

    [Header("Configuração do Nível")]
    public int actIndex; // 0 = Home, 1 = Act 1, 2 = Act 2...
    public NodeType nodeType = NodeType.Duel;
    
    // Referência ao banco de dados (pode ser estático ou arrastado)
    public CampaignDatabase campaignDB; 
    
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

        // Lógica Fixa: Cada ato tem 10 níveis.
        // Act 1 começa no 1, Act 2 no 11, etc.
        int startLevel = (actIndex - 1) * 10 + 1;

        if (nodeType == NodeType.Home) startLevel = 0;

        bool isUnlocked = CampaignManager.Instance.IsLevelUnlocked(startLevel);

        // Configura interatividade
        btn.interactable = isUnlocked;

        // Ícones visuais
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        
        // Verifica se completou todos os oponentes deste nó
        int endLevel = startLevel + 10;
        bool isNodeCompleted = CampaignManager.Instance.maxUnlockedLevel >= endLevel;
        
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

        if (campaignDB == null || campaignDB.acts.Count < actIndex)
        {
            Debug.LogError($"CampaignNode {name}: Database não configurado ou Act Index inválido!");
            return;
        }

        var actData = campaignDB.acts[actIndex - 1]; // Ajuste de índice (Act 1 é index 0 na lista)

        // Determina qual oponente enfrentar com base no progresso global
        int currentGlobalLevel = CampaignManager.Instance.maxUnlockedLevel;
        
        // O nível inicial deste ato (ex: Act 2 começa no 11)
        int actStartLevel = (actIndex - 1) * 10 + 1;
        int localIndex = currentGlobalLevel - actStartLevel;

        // Se já completou este nó, enfrenta o último (boss) ou mantém o índice dentro do limite para replay
        if (localIndex >= actData.opponentIDs.Count) localIndex = actData.opponentIDs.Count - 1;
        if (localIndex < 0) localIndex = 0;

        string currentOpponentID = actData.opponentIDs[localIndex];

        // Busca os dados do oponente
        CharacterData opponent = GameManager.Instance.characterDatabase.GetCharacterById(currentOpponentID);
        
        if (opponent != null)
        {
            int duelIndex = actStartLevel + localIndex;
            Debug.Log($"Iniciando duelo contra: {opponent.name} (Act {actIndex} - Batalha {localIndex + 1}/10)");
            GameManager.Instance.StartDuel(opponent, duelIndex);
            UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
        }
        else
        {
            Debug.LogError($"Oponente com ID '{currentOpponentID}' não encontrado no CharacterDatabase!");
        }
    }
}