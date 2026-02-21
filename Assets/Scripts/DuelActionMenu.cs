using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DuelActionMenu : MonoBehaviour
{
    public static DuelActionMenu Instance;

    [Header("UI References")]
    public GameObject menuPanel;
    public Button summonBtn;   // Botão para Invocação de Ataque
    public Button setBtn;      // Botão para Invocação de Defesa (Set)
    public Button activateBtn; // Botão para Ativar Magia/Armadilha
    public Button cancelBtn;   // Botão Cancelar

    private GameObject currentCardGO;
    private CardData currentCardData;

    void Awake()
    {
        Instance = this;
        if (menuPanel != null) menuPanel.SetActive(false);
        
        if (summonBtn) summonBtn.onClick.AddListener(OnSummon);
        if (setBtn) setBtn.onClick.AddListener(OnSet);
        if (activateBtn) activateBtn.onClick.AddListener(OnActivate);
        if (cancelBtn) cancelBtn.onClick.AddListener(CloseMenu);
    }

    void Start()
    {
        // Garante que comece fechado ao iniciar a cena
        CloseMenu();
    }

    void Update()
    {
        // Fecha o menu se clicar com o botão direito ou Esc
        if (menuPanel != null && menuPanel.activeSelf)
        {
            bool rightClick = false;
            bool escape = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) rightClick = true;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escape = true;
#else
            if (Input.GetMouseButtonDown(1)) rightClick = true;
            if (Input.GetKeyDown(KeyCode.Escape)) escape = true;
#endif

            if (rightClick || escape) CloseMenu();
        }
    }

    public void ShowMenu(GameObject cardGO, CardData data)
    {
        currentCardGO = cardGO;
        currentCardData = data;

        // Configura quais botões aparecem baseado no tipo da carta
        bool isMonster = data.type.Contains("Monster");
        
        if (summonBtn) 
        {
            summonBtn.gameObject.SetActive(isMonster);
            // Opcional: Mudar texto para "Summon"
        }
        
        if (setBtn) 
        {
            setBtn.gameObject.SetActive(true); // Monstros e Armadilhas podem ser "Setados"
            // Opcional: Mudar texto para "Set" (Defesa)
        }

        if (activateBtn) activateBtn.gameObject.SetActive(!isMonster);

        // Posiciona o menu perto da carta clicada
        if (menuPanel != null)
        {
            menuPanel.transform.position = cardGO.transform.position;
            menuPanel.SetActive(true);
        }
    }

    void OnSummon()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SummonMonster(currentCardGO, currentCardData, false); // false = Ataque
        CloseMenu();
    }

    void OnSet()
    {
        if (GameManager.Instance == null) return;

        if (currentCardData.type.Contains("Monster")) GameManager.Instance.SummonMonster(currentCardGO, currentCardData, true);
        else GameManager.Instance.PlaySpellTrap(currentCardGO, currentCardData, true);
        
        CloseMenu();
    }

    void OnActivate()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PlaySpellTrap(currentCardGO, currentCardData, false);
        CloseMenu();
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        currentCardGO = null;
        currentCardData = null;
    }
}