using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;
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
        if (menuPanel == null) menuPanel = this.gameObject;

        // Garante que o menu clássico apareça na frente de tudo e receba cliques
        Canvas canvas = menuPanel.GetComponent<Canvas>();
        if (canvas == null) canvas = menuPanel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30005; // Na frente das cartas
        
        GraphicRaycaster raycaster = menuPanel.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = menuPanel.AddComponent<GraphicRaycaster>();

        // Auto-atribui os botões caso você tenha esquecido de arrastar no Inspector
        if (summonBtn == null || setBtn == null || activateBtn == null || cancelBtn == null)
        {
            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (Button b in btns)
            {
                string n = b.name.ToLower();
                if (n.Contains("summon")) summonBtn = b;
                else if (n.Contains("set")) setBtn = b;
                else if (n.Contains("activate")) activateBtn = b;
                else if (n.Contains("cancel")) cancelBtn = b;
            }
        }
        
        if (summonBtn) summonBtn.onClick.AddListener(OnSummon);
        if (setBtn) setBtn.onClick.AddListener(OnSet);
        if (activateBtn) activateBtn.onClick.AddListener(OnActivate);
        if (cancelBtn) cancelBtn.onClick.AddListener(CloseMenu);

        menuPanel.SetActive(false);
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
        // FIX: Só permite abrir o menu de ações da mão durante as Main Phases
        if (!card.isOnField && PhaseManager.Instance != null)
        {
            if (PhaseManager.Instance.currentPhase != GamePhase.Main1 && PhaseManager.Instance.currentPhase != GamePhase.Main2)
            {
                return; // Não faz nada se não for a fase correta
            }
        }

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
                
                // Regra 1: Limite de 1 Normal Summon
                if (GameManager.Instance != null && GameManager.Instance.normalSummonsThisTurnPlayer > 0 && !GameManager.Instance.infiniteNormalSummons)
                {
                    canSummon = false;
                    canSet = false;
                }

                // Regra 2: Tributos Suficientes
                int dynamicLevel = card.CurrentCardData.level;
                if (CardEffectManager.Instance != null && CardEffectManager.Instance.auraManager != null)
                {
                    // Verifica modificadores de Nível (como A Legendary Ocean) na mão
                    LuaCard lc = new LuaCard(card.CurrentCardData);
                    dynamicLevel += CardEffectManager.Instance.auraManager.GetStatModifier(lc, "LEVEL", CardLocation.Hand);
                }

                int tributes = 0;
                if (dynamicLevel >= 5 && dynamicLevel <= 6) tributes = 1;
                if (dynamicLevel >= 7) tributes = 2;

                if (GameManager.Instance != null && GameManager.Instance.GetMonsterCount(true) < tributes && !GameManager.Instance.disableTributeRequirements)
                {
                    canSummon = false;
                    canSet = false;
                }

                summonBtn.gameObject.SetActive(canSummon);
                setBtn.gameObject.SetActive(canSet);
            }
            else
            {
                bool canActivate = true;
                if (card.CurrentCardData.type.Contains("Trap") && !GameManager.Instance.devMode) 
                    canActivate = false;

                // Regra de Magia de Ritual: Deve ter monstro Ritual COMPATÍVEL na mão
                if (card.CurrentCardData.property == "Ritual" && GameManager.Instance != null && RitualManager.Instance != null)
                {
                    var possibleRituals = RitualManager.Instance.GetPossibleRitualMonsters(card.CurrentCardData, GameManager.Instance.GetPlayerHandData());
                    if (possibleRituals.Count == 0) canActivate = false;
                }

                // Regra de Polimerização: Deve ter Fusão e materiais compatíveis
                if ((card.CurrentCardData.name == "Polymerization" || card.CurrentCardData.name.Contains("Fusion")) && GameManager.Instance != null && FusionManager.Instance != null)
                {
                    List<CardData> availableMats = new List<CardData>();
                    availableMats.AddRange(GameManager.Instance.GetPlayerHandData().Where(c => c.type.Contains("Monster")));
                    foreach (var z in GameManager.Instance.duelFieldUI.playerMonsterZones) {
                        if (z.childCount > 0) {
                            var cd = z.GetChild(0).GetComponent<CardDisplay>();
                            if (cd != null && !cd.isFlipped) availableMats.Add(cd.CurrentCardData);
                        }
                    }
                    var fusions = GameManager.Instance.GetPlayerExtraDeck().Where(c => c.type.Contains("Fusion")).ToList();
                    bool canFuse = fusions.Any(f => FusionManager.Instance.CanBeFusionSummoned(f, availableMats));
                    if (!canFuse) canActivate = false;
                }

                activateBtn.gameObject.SetActive(canActivate);
                setBtn.gameObject.SetActive(true);
            }
        }
        else // No Campo
        {
            bool isMyTurn = GameManager.Instance != null && GameManager.Instance.isPlayerTurn;
            bool isMainPhase = PhaseManager.Instance != null && (PhaseManager.Instance.currentPhase == GamePhase.Main1 || PhaseManager.Instance.currentPhase == GamePhase.Main2);

            if (card.CurrentCardData.type.Contains("Monster") && card.CurrentCardData.type.Contains("Effect") && !card.isFlipped)
            {
                activateBtn.gameObject.SetActive(isMyTurn && isMainPhase);
            }
            else if ((card.CurrentCardData.type.Contains("Spell") || card.CurrentCardData.type.Contains("Trap")) && card.isFlipped)
            {
                bool canActivate = isMyTurn && isMainPhase;
                
                // Dry-Run LUA (Testa a ativação no fundo antes de acender o botão)
                if (canActivate && CardEffectManager.Instance != null)
                {
                    LuaCard lc = CardEffectManager.Instance.EnsureCardScriptLoaded(card);
                    LuaEffect eff = lc?.registeredEffects.Find(e => e.type == 0x0010 || e.type == 0x0080);
                    if (eff != null && !CardEffectManager.Instance.CanActivateEffect(lc, eff, 0, null))
                    {
                        canActivate = false;
                    }
                }

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