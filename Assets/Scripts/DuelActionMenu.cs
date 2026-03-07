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
            bool canSummon = isMonster;
            // Verifica se pode invocar (Normal Summon)
            if (isMonster && SummonManager.Instance != null)
            {
                // Verifica limite de turno
                if (!SummonManager.Instance.CanNormalSummon()) canSummon = false;
                
                // Verifica tributos (apenas visualmente, a lógica real está no SummonManager)
                int tributes = SummonManager.Instance.GetRequiredTributes(data.level);
                if (!SummonManager.Instance.HasEnoughTributes(tributes, true)) canSummon = false;
            }
            summonBtn.gameObject.SetActive(canSummon);
        }
        
        if (setBtn) 
        {
            bool canSet = true;
            if (isMonster && SummonManager.Instance != null)
            {
                // Set também respeita limite de turno e tributos
                if (!SummonManager.Instance.CanNormalSummon()) canSet = false;
                
                int tributes = SummonManager.Instance.GetRequiredTributes(data.level);
                if (!SummonManager.Instance.HasEnoughTributes(tributes, true)) canSet = false;
            }
            setBtn.gameObject.SetActive(canSet); 
        }

        if (activateBtn) 
        {
            bool canActivate = !isMonster;
            // Traps não podem ser ativadas da mão (exceto em casos raros ou DevMode)
            if (data.type.Contains("Trap"))
            {
                if (GameManager.Instance != null && !GameManager.Instance.devMode) canActivate = false;
            }
            activateBtn.gameObject.SetActive(canActivate);
        }

        // Se nenhuma ação for possível, não abre o menu
        if (!summonBtn.gameObject.activeSelf && !setBtn.gameObject.activeSelf && !activateBtn.gameObject.activeSelf)
        {
            Debug.Log("Nenhuma ação disponível para esta carta.");
            return;
        }

        // Posiciona o menu perto da carta clicada
        if (menuPanel != null)
        {
            RectTransform cardRect = cardGO.GetComponent<RectTransform>();
            CardDisplay cardDisplay = cardGO.GetComponent<CardDisplay>();
            if (cardRect != null)
            {
                // Posiciona o menu acima da carta.
                bool isOpponentCard = (cardDisplay != null && !cardDisplay.isPlayerCard);
                // Se for carta do oponente (rotacionada), o offset Y precisa ser negativo para "subir" na tela.
                float yDirection = isOpponentCard ? -1f : 1f;

                // A altura exata do offset depende do tamanho da carta e do menu.
                float cardHeight = cardRect.rect.height * cardRect.lossyScale.y;
                Vector3 offset = new Vector3(0, cardHeight * 0.6f * yDirection, 0); // Ajuste este valor (0.6f) conforme necessário
                menuPanel.transform.position = cardGO.transform.position + offset;
            }
            else
            {
                menuPanel.transform.position = cardGO.transform.position;
            }
            menuPanel.SetActive(true);
        }
    }

    void OnSummon()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TrySummonMonster(currentCardGO, currentCardData, false); // false = Ataque
        CloseMenu();
    }

    void OnSet()
    {
        if (GameManager.Instance == null) return;

        if (currentCardData.type.Contains("Monster")) GameManager.Instance.TrySummonMonster(currentCardGO, currentCardData, true);
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