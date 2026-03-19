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

    private CardDisplay targetCard;

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

    public void ShowMenu(CardDisplay card)
    {
        targetCard = card;

        
        summonBtn.gameObject.SetActive(false);
        setBtn.gameObject.SetActive(false);
        activateBtn.gameObject.SetActive(false);
        
        if (!card.isOnField) // Na Mão
        {
            if (card.CurrentCardData.type.Contains("Monster"))
            {
                bool canSummon = true;
                bool canSet = true;
                
                if (SummonManager.Instance != null)
                {
                    if (!SummonManager.Instance.CanNormalSummon()) { canSummon = false; canSet = false; }
                    int tributes = SummonManager.Instance.GetRequiredTributes(card.CurrentCardData.level);
                    if (!SummonManager.Instance.HasEnoughTributes(tributes, true)) { canSummon = false; canSet = false; }
                }
                
                summonBtn.gameObject.SetActive(canSummon);
                setBtn.gameObject.SetActive(canSet);
            }
            else
            {
                bool canActivate = true;
                if (card.CurrentCardData.type.Contains("Trap") && !GameManager.Instance.devMode) canActivate = false;
                
                if (canActivate && SpellTrapManager.Instance != null)
                {
                    if (!SpellTrapManager.Instance.CanActivateCard(card.CurrentCardData, GameManager.Instance.isPlayerTurn))
                        canActivate = false;
                }

                activateBtn.gameObject.SetActive(canActivate);
                setBtn.gameObject.SetActive(true);
            }
        }
        else // No Campo
        {
            if (card.CurrentCardData.type.Contains("Monster") && card.CurrentCardData.type.Contains("Effect") && !card.isFlipped)
            {
                activateBtn.gameObject.SetActive(true);
            }
            else if ((card.CurrentCardData.type.Contains("Spell") || card.CurrentCardData.type.Contains("Trap")) && card.isFlipped)
            {
                bool canActivate = (GameManager.Instance.devMode) || (!card.summonedThisTurn);
                if (card.CurrentCardData.type.Contains("Spell") && card.CurrentCardData.property != "Quick-Play") canActivate = true;
                activateBtn.gameObject.SetActive(canActivate);
            }
        }

        // Se nenhuma ação for possível, não abre o menu
        if (!summonBtn.gameObject.activeSelf && !setBtn.gameObject.activeSelf && !activateBtn.gameObject.activeSelf)
        {
            Debug.Log("Nenhuma ação disponível para esta carta.");
            return;
        }

        if (menuPanel != null)
        {
            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            
            Vector3 mousePos;
#if ENABLE_INPUT_SYSTEM
            mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            mousePos = Input.mousePosition;
#endif
            
            if (panelRect != null)
            {
                // UI Inteligente: Inverte o ponto pivot se estiver perto das bordas da tela
                float pivotX = mousePos.x > Screen.width / 2f ? 1f : 0f;
                float pivotY = mousePos.y > Screen.height / 2f ? 1f : 0f;
                panelRect.pivot = new Vector2(pivotX, pivotY);
            }
            
            menuPanel.transform.position = mousePos;
            menuPanel.SetActive(true);
        }
    }

    void OnSummon()
    {
        if (GameManager.Instance != null && targetCard != null)
            GameManager.Instance.TrySummonMonster(targetCard.gameObject, targetCard.CurrentCardData, false);
        CloseMenu();
    }

    void OnSet()
    {
        if (GameManager.Instance != null && targetCard != null)
        {
            if (targetCard.CurrentCardData.type.Contains("Monster")) GameManager.Instance.TrySummonMonster(targetCard.gameObject, targetCard.CurrentCardData, true);
            else GameManager.Instance.PlaySpellTrap(targetCard.gameObject, targetCard.CurrentCardData, true);
        }
        CloseMenu();
    }

    void OnActivate()
    {
        if (GameManager.Instance != null && targetCard != null)
        {
            if (!targetCard.isOnField)
                GameManager.Instance.PlaySpellTrap(targetCard.gameObject, targetCard.CurrentCardData, false);
            else if (targetCard.CurrentCardData.type.Contains("Monster"))
                CardEffectManager.Instance.ExecuteCardEffect(targetCard);
            else
                GameManager.Instance.ActivateFieldSpellTrap(targetCard.gameObject);
        }
        CloseMenu();
    }

    public void CloseMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        targetCard = null;
    }
}