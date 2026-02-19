using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Fluxo de Telas")]
    public GameObject openingScreen;     // 1. Panel_Opening (Logo/Vídeo)
    public GameObject pressStartScreen;  // 2. Panel_PressStart (Tela de Título)
    public GameObject mainMenuScreen;    // 3. Panel_MainMenu (Menu Principal)
    public GameObject duelScreen;        // 4. Panel_Duel (O Jogo)

    [Header("Sub-Menus Principais")]
    public GameObject newGameMenu;       // Tela com Campaign, Arcade, Build Deck...
    public GameObject continueMenu;      // Tela com Load, Delete
    public GameObject onlineMenu;        // Tela com Create Room, Search...
    public GameObject optionsMenu;       // Tela de Opções (Audio, Video...)

    [Header("Telas de Funcionalidades")]
    public GameObject campaignScreen;
    public GameObject arcadeScreen;
    public GameObject deckBuilderScreen;
    public GameObject libraryMenu;       // Menu da Biblioteca
    public GameObject saveScreen;
    public GameObject loadScreen;
    public GameObject libDuelistsScreen;
    public GameObject libCardsScreen;
    public GameObject libArenasScreen;
    public GameObject libDecksScreen;

    [Header("Sub-Menus de Opções")]
    public GameObject audioScreen;
    public GameObject videoScreen;
    public GameObject controlsScreen;

    [Header("Popups")]
    public GraveyardViewer graveyardViewer;

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
        // Ao iniciar o jogo, começa pela Abertura
        ShowScreen(openingScreen);

        // Verificações de segurança para evitar erros silenciosos
        if (openingScreen == null) Debug.LogWarning("UIManager: Opening Screen não atribuída!");
        if (mainMenuScreen == null) Debug.LogWarning("UIManager: Main Menu Screen não atribuída!");
        if (duelScreen == null) Debug.LogWarning("UIManager: Duel Screen não atribuída!");
        if (graveyardViewer == null) Debug.LogWarning("UIManager: Graveyard Viewer não atribuído!");

        // DICA: Se quiser que a abertura passe sozinha após 3 segundos, descomente a linha abaixo:
        // Invoke("FinishOpening", 3f);
    }

    // Função central que desativa tudo e ativa só o que queremos
    public void ShowScreen(GameObject screenToShow)
    {
        // 1. Desativa todas as telas conhecidas para garantir
        if (openingScreen != null) openingScreen.SetActive(false);
        if (pressStartScreen != null) pressStartScreen.SetActive(false);
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (duelScreen != null) duelScreen.SetActive(false);

        // 2. Ativa apenas a tela desejada
        if (screenToShow != null)
        {
            screenToShow.SetActive(true);
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

    public void Btn_NewGame() { ShowScreen(newGameMenu); }
    public void Btn_Continue() { ShowScreen(continueMenu); }
    public void Btn_Online() { ShowScreen(onlineMenu); }
    public void Btn_Options() { ShowScreen(optionsMenu); }
    public void Btn_Exit() { Btn_ExitGame(); }

    // --- Navegação New Game ---

    public void Btn_Campaign() { ShowScreen(campaignScreen); }
    public void Btn_Arcade() { ShowScreen(arcadeScreen); }
    public void Btn_BuildDeck() { ShowScreen(deckBuilderScreen); }
    
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
        Debug.Log("Abrir popup de deletar save..."); 
        // Implementar lógica de delete
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

    public void Btn_ExitGame()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
