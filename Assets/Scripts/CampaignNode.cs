using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Button))]
public class CampaignNode : MonoBehaviour
{
    public enum NodeType { Act, Home, Arena, Menu }

    [Header("Configuração do Nível")]
    public NodeType nodeType = NodeType.Act;
    [Tooltip("Apenas para NodeType = Act. Define qual ato este botão representa (1 a 10).")]
    public int actIndex; 
    
    // Referência ao banco de dados (pode ser estático ou arrastado)
    public CampaignDatabase campaignDB; 
    
    [Header("Visual")]
    public Image targetImage; // A imagem do losango (se nulo, pega do botão)
    public GameObject lockIcon; // Ícone de cadeado (filho do botão)
    public GameObject clearIcon; // Ícone de "V" ou estrela (filho do botão)
    public Color lockedColor = new Color(0f, 0f, 0f, 0.5f); // Cor de sombra (preto transparente)
    public Color unlockedColor = Color.white; // Cor normal
    
    private Button btn;
    private MillenniumButton milleniumEffect; // Se estiver usando o efeito visual

    void Start()
    {
        btn = GetComponent<Button>();
        milleniumEffect = GetComponent<MillenniumButton>();
        
        // Tenta pegar a imagem do botão se não foi atribuída manualmente
        if (targetImage == null) targetImage = GetComponent<Image>();
        
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

        bool isUnlocked = true; // Home e Menu começam desbloqueados
        bool isCompleted = false;
        bool devModeActive = GameManager.Instance != null && GameManager.Instance.devMode;

        if (nodeType == NodeType.Arena)
        {
            // Arena só desbloqueia após vencer o Act 1 (nível 10), ou seja, quando o nível 11 estiver acessível
            isUnlocked = devModeActive || CampaignManager.Instance.IsLevelUnlocked(11);
        }
        else if (nodeType == NodeType.Act)
        {
            // Lógica Fixa: Cada ato tem 10 níveis. Act 1 começa no 1, Act 2 no 11, etc.
            int startLevel = (actIndex - 1) * 10 + 1;
            
            // Verifica se está desbloqueado (Progresso OU DevMode)
            isUnlocked = devModeActive || CampaignManager.Instance.IsLevelUnlocked(startLevel);

            // Verifica se completou todos os oponentes deste nó
            int endLevel = startLevel + 10;
            isCompleted = CampaignManager.Instance.maxUnlockedLevel >= endLevel;
        }

        // --- Aplicação Visual ---

        // Configura interatividade
        btn.interactable = isUnlocked;

        // Ícones visuais
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        
        if (clearIcon != null) clearIcon.SetActive(isCompleted && nodeType == NodeType.Act);

        // Aplica o efeito de "Sombra" (cor) na imagem do losango
        Color finalColor = isUnlocked ? unlockedColor : lockedColor;

        // Se tiver o efeito Millennium, atualizamos a cor base dele para não haver conflito
        if (milleniumEffect != null)
        {
            milleniumEffect.useColorTint = true;
            milleniumEffect.bgNormalColor = finalColor;
            // Força a atualização visual imediata
            if (targetImage != null) targetImage.color = finalColor;
        }
        else if (targetImage != null)
        {
            targetImage.color = finalColor;
        }
    }

    void OnNodeClick()
    {
        if (UIManager.Instance == null) return;

        switch (nodeType)
        {
            case NodeType.Home:
                // Abre o "Acampamento" (onde ver troféus, salvar, etc)
                Debug.Log("Abrindo Home/Acampamento...");
                // TODO: Criar e conectar a tela de Camp/Trophies no UIManager
                // UIManager.Instance.ShowScreen(UIManager.Instance.campScreen);
                break;

            case NodeType.Arena:
                // Abre o Free Duel (Arcade)
                Debug.Log("Indo para Arena...");
                UIManager.Instance.Btn_Arcade(); 
                break;

            case NodeType.Menu:
                // Volta para o Menu Principal
                Debug.Log("Voltando ao Menu Principal...");
                UIManager.Instance.ShowScreen(UIManager.Instance.mainMenuScreen);
                break;
            case NodeType.Act:
                StartCampaignDuel();
                break;
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