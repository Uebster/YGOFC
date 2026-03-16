using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Fluxo de Telas")]
    public GameObject openingScreen;     // 1. Panel_Opening (Logo/Vídeo)
    public GameObject pressStartScreen;  // 2. Panel_PressStart (Tela de Título)
    public GameObject mainMenuScreen;    // 3. Panel_MainMenu (Menu Principal)
    public GameObject nameInputScreen;   // 3.5 Panel_NameInput (Criação de Perfil)
    public GameObject endDuelMessagePanel; // Painel para mensagem de WIN/LOSE
    public GameObject duelScreen;        // 4. Panel_Duel (O Jogo)
    public GameObject rewardScreen;      // 5. Panel_Reward (Tela de Recompensa)

    [Header("Sub-Menus Principais")]
    public GameObject newGameMenu;       // Tela com Campaign, Arcade, Build Deck...
    public GameObject continueMenu;      // Tela com Load, Delete
    public GameObject onlineMenu;        // Tela com Create Room, Search...
    public GameObject optionsMenu;       // Tela de Opções (Audio, Video...)

    [Header("Telas de Funcionalidades")]
    public GameObject campaignScreen;
    public GameObject walkthroughScreen; // Referência para o Panel_Walktrough
    public GameObject homeScreen;        // Referência para o Panel_Home
    public GameObject arcadeScreen;
    public GameObject deckBuilderScreen;
    public GameObject libraryMenu;       // Menu da Biblioteca
    public GameObject saveScreen;
    public GameObject importScreen;      // Novo: Panel_Import
    public GameObject exportScreen;      // Novo: Panel_Export
    public GameObject loadScreen;
    public GameObject deleteScreen;      // Novo: Tela de Deletar Save
    public GameObject libDuelistsScreen;
    public GameObject libCardsScreen;
    public GameObject libArenasScreen;
    public GameObject libDecksScreen;

    [Header("Sub-Menus de Opções")]
    public GameObject audioScreen;
    public GameObject videoScreen;
    public GameObject controlsScreen;

    [Header("Popups")]
    public GameObject exitScreen;
    public GameObject confirmationModal; // Novo modal genérico
    [Tooltip("Botão 'Sim' do modal de confirmação.")]
    public Button confirmationYesButton; 
    [Tooltip("Botão 'Não' do modal de confirmação.")]
    public Button confirmationNoButton;
    public GraveyardViewer graveyardViewer;
    public GraveyardViewer extraDeckViewer; // Reusa o script GraveyardViewer para o Extra Deck
    public GraveyardViewer removedCardsViewer; // Reusa o script para Cartas Removidas
    public GraveyardViewer deckViewer; // Reusa o script para visualizar o Deck (Dev Tool)
    public PositionSelectionUI positionSelectionModal; // Novo modal específico
    public CardSelectionUI cardSelectionModal; // Novo modal de seleção múltipla
    public FusionUI fusionUI; // Novo modal para Fusão
    public RitualUI ritualUI; // Novo modal para Ritual
    public GameObject destinyBoardWinPanel; // Arraste o Panel_DestinyBoardWin aqui
    public GameObject exodiaWinPanel; // Arraste o Panel_ExodiaWin aqui
    
    [Header("End Duel Assets")]
    public Sprite endDuelWinSprite;
    public Sprite endDuelLoseSprite;

    // Adicione outras telas aqui conforme precisar (Library, Campaign, etc)
    // public GameObject libraryScreen; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Lógica de Teste Direto: Verifica se alguma flag de "pular para" está ativa.
        // A ordem aqui define a prioridade: Duelo > Deck Builder > Biblioteca.
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.testDuelDirectly)
            {
                StartCoroutine(StartDuelDelayed());
                return; // Encerra para não carregar outras telas
            }
            if (GameManager.Instance.testDeckBuilderDirectly)
            {
                StartCoroutine(ShowScreenDelayed(deckBuilderScreen));
                return; // Encerra para não carregar outras telas
            }
            if (GameManager.Instance.testLibraryDirectly)
            {
                StartCoroutine(ShowScreenDelayed(libraryMenu));
                return; // Encerra para não carregar outras telas
            }
        }

        // Se nenhuma flag de teste direto estiver ativa, inicia o fluxo normal do jogo.
        ShowScreen(openingScreen);

        // Verificações de segurança para evitar erros silenciosos
        if (openingScreen == null) Debug.LogWarning("UIManager: Opening Screen não atribuída!");
        if (mainMenuScreen == null) Debug.LogWarning("UIManager: Main Menu Screen não atribuída!");
        if (duelScreen == null) Debug.LogWarning("UIManager: Duel Screen não atribuída!");
        if (graveyardViewer == null) Debug.LogWarning("UIManager: Graveyard Viewer não atribuído!");

        // Garante que os popups locais comecem desativados
        if (newGameMenu != null) {
            Transform p = newGameMenu.transform.Find("ConfirmationExitToMainMenu");
            if (p != null) p.gameObject.SetActive(false);
        }
        if (mainMenuScreen != null) {
            Transform p = mainMenuScreen.transform.Find("ConfirmationExitGame");
            if (p != null) p.gameObject.SetActive(false);
        }

        // DICA: Se quiser que a abertura passe sozinha após 3 segundos, descomente a linha abaixo:
        // Invoke("FinishOpening", 3f);
    }

    System.Collections.IEnumerator StartDuelDelayed()
    {
        // Espera um pouco para o GameManager carregar texturas (verso da carta) e banco de dados
        yield return new WaitForSeconds(0.5f);
        Btn_StartFreeDuel();
    }

    System.Collections.IEnumerator ShowScreenDelayed(GameObject screen)
    {
        // Espera um frame para garantir que o Start() do GameManager (que é uma Coroutine) tenha sido executado e populado o baú.
        yield return null;
        ShowScreen(screen);
    }

    // Função central que desativa tudo e ativa só o que queremos
    public void ShowScreen(GameObject screenToShow)
    {
        // 1. Desativa todas as telas conhecidas para garantir
        if (openingScreen != null) openingScreen.SetActive(false);
        if (pressStartScreen != null) pressStartScreen.SetActive(false);
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (nameInputScreen != null) nameInputScreen.SetActive(false);
        if (duelScreen != null) duelScreen.SetActive(false);
        if (endDuelMessagePanel != null) endDuelMessagePanel.SetActive(false);
        if (rewardScreen != null) rewardScreen.SetActive(false);

        // Adicionado: Desativa todos os outros painéis de sub-menu
        if (newGameMenu != null) newGameMenu.SetActive(false);
        if (continueMenu != null) continueMenu.SetActive(false);
        if (onlineMenu != null) onlineMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (campaignScreen != null) campaignScreen.SetActive(false);
        if (homeScreen != null) homeScreen.SetActive(false); // Garante que o Home suma ao trocar de tela
        if (walkthroughScreen != null) walkthroughScreen.SetActive(false);
        if (arcadeScreen != null) arcadeScreen.SetActive(false);
        if (deckBuilderScreen != null) deckBuilderScreen.SetActive(false);
        if (libraryMenu != null) libraryMenu.SetActive(false);
        if (saveScreen != null) saveScreen.SetActive(false);
        if (importScreen != null) importScreen.SetActive(false);
        if (exportScreen != null) exportScreen.SetActive(false);
        if (loadScreen != null) loadScreen.SetActive(false);
        if (deleteScreen != null) deleteScreen.SetActive(false); // Novo
        if (libDuelistsScreen != null) libDuelistsScreen.SetActive(false);
        if (libCardsScreen != null) libCardsScreen.SetActive(false);
        if (libArenasScreen != null) libArenasScreen.SetActive(false);
        if (libDecksScreen != null) libDecksScreen.SetActive(false);
        
        if (audioScreen != null) audioScreen.SetActive(false);
        if (videoScreen != null) videoScreen.SetActive(false);
        if (controlsScreen != null) controlsScreen.SetActive(false);
        if (exitScreen != null) exitScreen.SetActive(false);
        if (confirmationModal != null) confirmationModal.SetActive(false);

        if (graveyardViewer != null) graveyardViewer.gameObject.SetActive(false);
        if (extraDeckViewer != null) extraDeckViewer.gameObject.SetActive(false);
        if (removedCardsViewer != null) removedCardsViewer.gameObject.SetActive(false);
        if (deckViewer != null) deckViewer.gameObject.SetActive(false);
        if (positionSelectionModal != null) positionSelectionModal.gameObject.SetActive(false);
        if (cardSelectionModal != null) cardSelectionModal.gameObject.SetActive(false);
        if (fusionUI != null) fusionUI.gameObject.SetActive(false);
        if (ritualUI != null) ritualUI.gameObject.SetActive(false);
        if (destinyBoardWinPanel != null) destinyBoardWinPanel.SetActive(false);
        if (exodiaWinPanel != null) exodiaWinPanel.SetActive(false);

        // 2. Ativa apenas a tela desejada
        if (screenToShow != null)
        {
            screenToShow.SetActive(true);
        }
    }

    public void ShowEndDuelPanel(bool playerWon)
    {
        ShowScreen(endDuelMessagePanel);
        if (endDuelMessagePanel != null)
        {
            Transform imgTr = endDuelMessagePanel.transform.Find("ImageEndDuel");
            if (imgTr != null)
            {
                Image img = imgTr.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = playerWon ? endDuelWinSprite : endDuelLoseSprite;
                }
            }
        }
    }

    public void ShowRewardScreen(string rank, CardData card, bool isNew = false)
    {
        if (rewardScreen != null)
        {
            ShowScreen(rewardScreen); // Isso fecha a DuelScreen e abre a RewardScreen
            RewardPanelUI ui = rewardScreen.GetComponentInChildren<RewardPanelUI>(true);
            if (ui != null) ui.Show(rank, card, isNew);
        }
        else
        {
            Debug.LogWarning("UIManager: RewardScreen não atribuído. Voltando ao menu.");
            Btn_BackToMenu();
        }
    }

    public void ShowGraveyard(List<CardData> cards, Texture2D cardBack)
    {
        if (graveyardViewer != null)
        {
            graveyardViewer.Show(cards, cardBack);
        }
        else
        {
            Debug.LogError("GraveyardViewer não está atribuído no UIManager!");
        }
    }

    public void ShowExtraDeck(List<CardData> cards, Texture2D cardBack)
    {
        if (extraDeckViewer != null)
        {
            extraDeckViewer.Show(cards, cardBack);
        }
        else
        {
            Debug.LogError("ExtraDeckViewer não está atribuído no UIManager!");
        }
    }

    public void ShowRemovedCards(List<CardData> cards, Texture2D cardBack)
    {
        if (removedCardsViewer != null)
        {
            removedCardsViewer.Show(cards, cardBack);
        }
    }

    public void ShowDeck(List<CardData> cards, Texture2D cardBack)
    {
        if (deckViewer != null)
        {
            deckViewer.Show(cards, cardBack);
        }
    }

    public void ShowPositionSelection(CardData card, System.Action<CardDisplay.BattlePosition> onSelected)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            // Escolhe Ataque por padrão ou aleatório
            onSelected?.Invoke(Random.value > 0.5f ? CardDisplay.BattlePosition.Attack : CardDisplay.BattlePosition.Defense);
            return;
        }

        if (positionSelectionModal != null)
        {
            positionSelectionModal.Show(card, onSelected);
        }
        else
        {
            Debug.LogWarning("PositionSelectionUI não atribuído! Usando Ataque por padrão.");
            onSelected?.Invoke(CardDisplay.BattlePosition.Attack);
        }
    }

    public void ShowCardSelection(List<CardData> cards, string title, int min, int max, System.Action<List<CardData>> onConfirm)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            // Seleciona os primeiros 'min' cards automaticamente
            List<CardData> autoSelected = new List<CardData>();
            int count = Mathf.Min(min, cards.Count);
            for(int i=0; i<count; i++) autoSelected.Add(cards[i]);
            onConfirm?.Invoke(autoSelected);
            return;
        }

        if (cardSelectionModal != null)
        {
            cardSelectionModal.Show(cards, title, min, max, onConfirm);
        }
        else
        {
            Debug.LogError("UIManager: CardSelectionUI não atribuído!");
            // Fallback: Seleciona os primeiros 'min' cards automaticamente para não travar o jogo
            List<CardData> fallback = new List<CardData>();
            for(int i=0; i<Mathf.Min(min, cards.Count); i++) fallback.Add(cards[i]);
            onConfirm?.Invoke(fallback);
        }
    }

    public void ShowFusionUI(CardDisplay source)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            // Na simulação, ignoramos a fusão complexa por enquanto para não travar
            // Enviamos a carta para o GY para "consumir" a ativação
            Debug.Log("[SIM] Fusão ignorada (Auto-fusão simplificada).");
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard, CardLocation.Field, SendReason.Effect);
            Destroy(source.gameObject);
            return;
        }

        if (fusionUI != null)
        {
            fusionUI.Show(source);
        }
        else
        {
            Debug.LogError("UIManager: FusionUI não atribuído!");
        }
    }

    public void ShowRitualUI(CardDisplay source)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            Debug.Log("[SIM] Ritual ignorado (Auto-ritual simplificado).");
            GameManager.Instance.SendToGraveyard(source.CurrentCardData, source.isPlayerCard, CardLocation.Field, SendReason.Effect);
            Destroy(source.gameObject);
            return;
        }

        if (ritualUI != null)
        {
            ritualUI.Show(source);
        }
        else
        {
            Debug.LogError("UIManager: RitualUI não atribuído!");
        }
    }

    public void ShowResponseWindow(List<CardDisplay> responseCards, System.Action onPass)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            // IA/Simulador nunca ativa em resposta por enquanto (para evitar loops complexos)
            onPass?.Invoke();
            return;
        }

        if (cardSelectionModal != null)
        {
            List<CardData> cardDataList = responseCards.Select(c => c.CurrentCardData).ToList();
            
            // Reutiliza o modal de seleção de cartas. O botão "cancelar" funcionará como "passar".
            cardSelectionModal.Show(cardDataList, "Ativar em resposta?", 1, 1, (selected) => {
                if (selected != null && selected.Count > 0)
                {
                    // Jogador escolheu uma carta para ativar
                    var cardToActivate = GameManager.Instance.FindCardOnField(selected[0].id, true); // Assume que a resposta é do jogador
                    if (cardToActivate != null)
                        GameManager.Instance.ActivateFieldSpellTrap(cardToActivate.gameObject);
                }
                else { onPass?.Invoke(); } // Jogador cancelou/passou
            });
        }
        else
        {
            // FIX: Se não houver modal atribuído, passa automaticamente para evitar travamento no ChainManager
            Debug.LogWarning("UIManager: CardSelectionModal não atribuído! Passando resposta automaticamente.");
            onPass?.Invoke();
        }
    }

    // Chamado pelo botão de Importar no DeckBuilder
    public void Btn_ShowImportPanel()
    {
        if (importScreen == null) return;
        ShowScreen(importScreen);
        DeckImportExportManager manager = importScreen.GetComponent<DeckImportExportManager>();
        if (manager != null)
        {
            manager.Setup(DeckImportExportManager.MenuType.Import);
        }
    }

    // Chamado pelo botão de Exportar no DeckBuilder
    public void Btn_ShowExportPanel()
    {
        if (exportScreen == null) return;
        ShowScreen(exportScreen);
        DeckImportExportManager manager = exportScreen.GetComponent<DeckImportExportManager>();
        if (manager != null)
        {
            manager.Setup(DeckImportExportManager.MenuType.Export);
        }
    }

    // Exibe uma mensagem simples (usa o modal de confirmação adaptado)
    public void ShowMessage(string message)
    {
        ShowConfirmation(message, null, null);
    }

    // Novo método para mostrar modal de confirmação genérico
    public void ShowConfirmation(string message, System.Action onConfirm, System.Action onCancel = null)
    {
        // BYPASS DE SIMULAÇÃO
        if (GameManager.Instance != null && GameManager.Instance.isSimulating)
        {
            onConfirm?.Invoke(); // Sempre confirma na simulação
            return;
        }

        if (confirmationModal == null)
        {
            Debug.LogWarning("UIManager: Confirmation Modal não atribuído!");
            onConfirm?.Invoke(); // Fallback: confirma direto se não tiver UI
            return;
        }

        confirmationModal.SetActive(true);
        
        // Configura texto e botões (assumindo que o modal tem um script ou estrutura padrão)
        // Aqui estou buscando componentes dinamicamente para simplificar, mas o ideal é ter um script ConfirmationModalUI
        TextMeshProUGUI txt = null;
        Transform txtTr = confirmationModal.transform.Find("Text_Confirmation");
        if (txtTr != null) txt = txtTr.GetComponent<TextMeshProUGUI>();
        if (txt == null) txt = confirmationModal.GetComponentInChildren<TextMeshProUGUI>();

        if (txt) txt.text = message;

        // Usa referências explícitas se disponíveis, senão tenta encontrar
        Button btnYes = confirmationYesButton;
        Button btnNo = confirmationNoButton;

        // Tenta encontrar por nome se não estiverem atribuídos
        if (btnYes == null)
        {
            Transform tr = confirmationModal.transform.Find("Btn_Yes");
            if (tr != null) btnYes = tr.GetComponent<Button>();
        }
        if (btnNo == null)
        {
            Transform tr = confirmationModal.transform.Find("Btn_No");
            if (tr != null) btnNo = tr.GetComponent<Button>();
        }

        if (btnYes == null || btnNo == null)
        {
            Debug.LogWarning("UIManager: Botões de confirmação não encontrados por referência ou nome. Usando busca automática (pode ser impreciso). Por favor, atribua no Inspector.");
            Button[] btns = confirmationModal.GetComponentsInChildren<Button>();
            if (btns.Length >= 1) btnYes = btns[0];
            if (btns.Length >= 2) btnNo = btns[1];
        }

        if (btnYes != null)
        {
            btnYes.onClick.RemoveAllListeners();
            btnYes.onClick.AddListener(() => { CloseConfirmation(); onConfirm?.Invoke(); });
        }
        if (btnNo != null)
        {
            btnNo.onClick.RemoveAllListeners();
            btnNo.onClick.AddListener(() => { CloseConfirmation(); onCancel?.Invoke(); });
        }
    }

    // --- Funções para os Botões (Ligue no OnClick) ---

    // Chame esta função em um botão transparente na tela de abertura ou via código (Invoke)
    public void FinishOpening()
    {
        ShowScreen(pressStartScreen);
    }

    // Chame no botão "PRESS START"
    public void Btn_PressStart()
    {
        ShowScreen(mainMenuScreen);
    }

    // --- Navegação do Menu Principal ---

    // Alterado: Agora vai para a tela de digitar nome primeiro
    public void Btn_NewGame() { ShowScreen(nameInputScreen); }
    
    // Novo: Chamado pelo NameInputScreen quando o jogador confirma o nome
    public void Btn_ConfirmNameInput() { ShowScreen(newGameMenu); }

    public void Btn_Continue() { ShowScreen(continueMenu); }
    public void Btn_Online() { ShowScreen(onlineMenu); }
    public void Btn_Options() { ShowScreen(optionsMenu); }
    public void Btn_Exit() 
    { 
        if (exitScreen != null) ShowScreen(exitScreen);
        else Btn_ExitGame(); 
    }

    // Usado no botão "Não" da tela de saída
    public void Btn_CancelExit() { ShowScreen(mainMenuScreen); }

    // --- Navegação New Game ---

    public void Btn_Campaign() { ShowScreen(campaignScreen); }
    public void Btn_Arcade() { ShowScreen(arcadeScreen); }
    public void Btn_BuildDeck() { ShowScreen(deckBuilderScreen); }
    public void Btn_BackToDeckBuilder() { ShowScreen(deckBuilderScreen); }
    
    public void Btn_Library() { ShowScreen(libraryMenu); }
    // Sub-menu Library
    public void Btn_LibDuelists() { ShowScreen(libDuelistsScreen); }
    public void Btn_LibCards() { ShowScreen(libCardsScreen); }
    public void Btn_LibArenas() { ShowScreen(libArenasScreen); }
    public void Btn_LibDecks() { ShowScreen(libDecksScreen); }

    public void Btn_Save() { ShowScreen(saveScreen); }

    // --- Navegação Continue ---

    public void Btn_Load() { ShowScreen(loadScreen); }
    public void Btn_DeleteSave() 
    {
        if (deleteScreen != null) ShowScreen(deleteScreen);
        else Debug.LogWarning("UIManager: A tela de Deletar (Delete Screen) não foi atribuída no Inspector.");
    }

    // --- Navegação Online ---

    public void Btn_OnlineLoadDeck() { ShowScreen(deckBuilderScreen); } // Reusa o deck builder
    public void Btn_CreateRoom() { Debug.Log("Criar Sala..."); }
    public void Btn_EnterRoom() { Debug.Log("Entrar na Sala..."); }
    public void Btn_SearchRoom() { Debug.Log("Buscar Sala..."); }

    // --- Navegação Options ---

    public void Btn_Audio() { ShowScreen(audioScreen); }
    public void Btn_Video() { ShowScreen(videoScreen); }
    public void Btn_Controls() { ShowScreen(controlsScreen); }

    // --- Botões de Voltar ---

    public void Btn_BackToNewGameMenu()
    {
        ShowScreen(newGameMenu);
    }

    public void Btn_BackToLibraryMenu()
    {
        ShowScreen(libraryMenu);
    }

    public void Btn_BackToOptionsMenu()
    {
        ShowScreen(optionsMenu);
    }

    // Chame no botão "Free Duel" do Menu Principal
    public void Btn_StartFreeDuel()
    {
        ShowScreen(duelScreen);
        
        // Avisa o GameManager para preparar o tabuleiro
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartDuel();
        }
    }

    // Chame no botão "Surrender" ou "Quit" dentro do duelo
    public void Btn_BackToMenu()
    {
        ShowScreen(mainMenuScreen);
        // Aqui você poderia adicionar lógica para limpar o tabuleiro se quisesse
    }

    // Novo: Botão de voltar do New Game (Deslogar)
    public void Btn_LogOutToMainMenu()
    {
        ShowLocalOrGlobalConfirmation(newGameMenu, "ConfirmationExitToMainMenu", "Deseja deslogar e voltar ao Menu Principal?", () => {
            ShowScreen(mainMenuScreen);
        });
    }

    public void Btn_ExitGame()
    {
        ShowLocalOrGlobalConfirmation(mainMenuScreen, "ConfirmationExitGame", "Deseja realmente sair do jogo?", () => {
            Debug.Log("Saindo do Jogo...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }

    // Procura o popup local dentro do painel atual, configura os textos/botões e o exibe.
    private void ShowLocalOrGlobalConfirmation(GameObject parentPanel, string popupName, string message, System.Action onConfirm)
    {
        if (parentPanel != null)
        {
            Transform popupTr = parentPanel.transform.Find(popupName);
            if (popupTr != null)
            {
                GameObject popup = popupTr.gameObject;
                TextMeshProUGUI txt = popup.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>() ?? popup.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = message;

                Button btnYes = popup.transform.Find("Btn_Yes")?.GetComponent<Button>();
                Button btnNo = popup.transform.Find("Btn_No")?.GetComponent<Button>();

                if (btnYes != null) { btnYes.onClick.RemoveAllListeners(); btnYes.onClick.AddListener(() => { popup.SetActive(false); onConfirm?.Invoke(); }); }
                if (btnNo != null) { btnNo.onClick.RemoveAllListeners(); btnNo.onClick.AddListener(() => { popup.SetActive(false); }); }

                popup.SetActive(true);
                return;
            }
        }
        
        // Se não encontrou o painel local, usa o modal de confirmação global padrão
        ShowConfirmation(message, onConfirm);
    }

    // Fecha o modal de confirmação com segurança
    public void CloseConfirmation()
    {
        if (confirmationModal != null) confirmationModal.SetActive(false);
    }

    // Fecha todas as janelas flutuantes (útil para iniciar simulação ou resetar)
    public void CloseAllPopups()
    {
        CloseConfirmation();
        if (cardSelectionModal != null) cardSelectionModal.gameObject.SetActive(false);
        if (positionSelectionModal != null) positionSelectionModal.gameObject.SetActive(false);
        if (fusionUI != null) fusionUI.gameObject.SetActive(false);
        if (ritualUI != null) ritualUI.gameObject.SetActive(false);
        if (graveyardViewer != null) graveyardViewer.gameObject.SetActive(false);
        if (extraDeckViewer != null) extraDeckViewer.gameObject.SetActive(false);
        if (removedCardsViewer != null) removedCardsViewer.gameObject.SetActive(false);
    }
}
