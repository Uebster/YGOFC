using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Fluxo de Telas")]
    public GameObject openingScreen;     // 1. Panel_Opening (Logo/Vídeo)
    public GameObject pressStartScreen;  // 2. Panel_PressStart (Tela de Título)
    public GameObject mainMenuScreen;    // 3. Panel_MainMenu (Menu Principal)
    public GameObject duelScreen;        // 4. Panel_Duel (O Jogo)

    [Header("Popups")]
    public GraveyardViewer graveyardViewer;

    // Adicione outras telas aqui conforme precisar (Library, Campaign, etc)
    // public GameObject libraryScreen; 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Ao iniciar o jogo, começa pela Abertura
        ShowScreen(openingScreen);

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
    }
}
