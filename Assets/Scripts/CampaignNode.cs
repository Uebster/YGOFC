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
    public Color lockedColor = Color.black; // Cor de sombra (preto sólido)
    public Color unlockedColor = Color.white; // Cor normal
    
    private Button btn;
    private MillenniumButton milleniumEffect; // Se estiver usando o efeito visual

    void Awake()
    {
        btn = GetComponent<Button>();
        milleniumEffect = GetComponent<MillenniumButton>();
        
        // Tenta pegar a imagem do botão se não foi atribuída manualmente
        if (targetImage == null) targetImage = GetComponent<Image>();

        // IMPORTANTE: Desativa a transição padrão do botão para não brigar com nossa cor
        if (btn != null) btn.transition = Selectable.Transition.None;
    }

    void Start()
    {
        btn.onClick.AddListener(OnNodeClick);
        UpdateVisualState();
        // Força uma atualização extra após um breve momento para garantir que o CampaignManager carregou
        StartCoroutine(DelayedUpdate());
    }

    private System.Collections.IEnumerator DelayedUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateVisualState();
    }

    void OnEnable()
    {
        UpdateVisualState();
    }

    // Permite testar clicando com botão direito no componente no Inspector
    [ContextMenu("Force Update Visual")]
    public void UpdateVisualState()
    {
        // Verificação de segurança com aviso
        if (CampaignManager.Instance == null || GameManager.Instance == null)
        {
            // Só avisa se estivermos jogando, para não spammar no editor
            if (Application.isPlaying) 
                Debug.LogWarning($"CampaignNode '{name}': CampaignManager.Instance não encontrado! O botão pode ficar escuro incorretamente.");
            return;
        }
        
        // Garante que temos a imagem antes de tentar pintar
        if (targetImage == null) targetImage = GetComponent<Image>();

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
            int endLevel = startLevel + 9; // O último nível do ato (ex: 10, 20)
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
            if (isUnlocked)
            {
                // Desbloqueado: Desativa tintura para não escurecer no hover, usa apenas escala
                milleniumEffect.useColorTint = false;
            }
            else
            {
                // Bloqueado: Ativa tintura para aplicar a cor de sombra (preto)
                milleniumEffect.useColorTint = true;
                milleniumEffect.bgNormalColor = finalColor;
            }
            
            if (targetImage != null) targetImage.color = finalColor;
        }
        else if (targetImage != null)
        {
            // Se estiver desbloqueado, garante Alpha 1 (sólido). Se bloqueado, usa a cor de sombra.
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
                Debug.Log("Abrindo Home...");
                if (UIManager.Instance != null) UIManager.Instance.ShowScreen(UIManager.Instance.homeScreen);
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

        // Fallback: Se não foi atribuído no Inspector, tenta pegar do GameManager
        if (campaignDB == null && GameManager.Instance != null)
        {
            campaignDB = GameManager.Instance.campaignDatabase;
        }

        // AUTO-CORREÇÃO: Se o Act Index for 0, tenta adivinhar pelo nome do objeto (ex: "Act1" -> 1)
        if (actIndex == 0 && name.StartsWith("Act"))
        {
            string numberPart = name.Replace("Act", "");
            int.TryParse(numberPart, out actIndex);
        }

        // Debug detalhado para identificar a causa exata do erro
        if (campaignDB == null)
        {
            Debug.LogError($"CampaignNode '{name}': ERRO CRÍTICO! 'campaignDB' está NULO. Verifique se o MainCampaignDB está arrastado no campo 'Campaign Database' do GameManager.");
            return;
        }

        if (actIndex < 1)
        {
            Debug.LogError($"CampaignNode '{name}': ERRO! Act Index inválido ({actIndex}). O nome do botão deve ser 'Act1', 'Act2'... ou o índice deve ser definido manualmente.");
            return;
        }

        if (campaignDB.acts == null || campaignDB.acts.Count < actIndex)
        {
            int count = (campaignDB.acts == null) ? 0 : campaignDB.acts.Count;
            Debug.LogError($"CampaignNode '{name}': ERRO! O Banco de Dados existe mas não tem o Ato {actIndex}. Total de Atos no DB: {count}. (Dica: No Project, clique com botão direito no arquivo do DB e escolha 'Load Optimized Campaign').");
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
            
            // Tenta encontrar o WalkthroughManager (mesmo se o painel estiver desativado)
            WalkthroughManager wm = WalkthroughManager.Instance;
            if (wm == null && UIManager.Instance != null && UIManager.Instance.walkthroughScreen != null)
            {
                wm = UIManager.Instance.walkthroughScreen.GetComponent<WalkthroughManager>();
            }

            // Se encontrou, usa ele
            if (wm != null)
            {
                wm.ShowAct(actIndex, opponent, duelIndex);
            }
            // Fallback: Se não tiver Walkthrough configurado, vai direto pro duelo
            else
            {
                Debug.LogWarning("WalkthroughManager não encontrado. Iniciando duelo direto.");
                GameManager.Instance.StartDuel(opponent, duelIndex);
                UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
            }
        }
        else
        {
            Debug.LogError($"Oponente com ID '{currentOpponentID}' não encontrado no CharacterDatabase!");
        }
    }
}