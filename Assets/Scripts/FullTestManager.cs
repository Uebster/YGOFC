using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FullTestManager : MonoBehaviour
{
    public static FullTestManager Instance;

    [Header("UI do Painel (Associe no Inspector)")]
    public GameObject testPanel;
    public Toggle tglAI;
    public Toggle tglShowOppHand;
    public Toggle tglAutoPhases;
    public Toggle tglInfiniteLP;
    public Toggle tglVFX;
    public Toggle tglSFX;

    [Header("Seletores (Dropdowns)")]
    public TMP_Dropdown dropAct;
    public TMP_Dropdown dropOpponent;
    public TMP_Dropdown dropDeckVariant;

    [Header("Botões de Ação")]
    public Button btnCoin;
    public Button btnDice;
    public Button btnClock;
    public Button btnSpawnCard;
    public Button btnSimulateAttack;
    public Button btnSimulateTrap;

    void Awake()
    {
        Instance = this;
        if (testPanel != null) testPanel.SetActive(GameManager.Instance != null && GameManager.Instance.fullTestMode);
    }

    void Start()
    {
        if (GameManager.Instance == null) return;

        // Configura Toggles baseados no GameManager
        if (tglAI) { tglAI.isOn = OpponentAI.Instance != null && OpponentAI.Instance.gameObject.activeSelf; tglAI.onValueChanged.AddListener(ToggleAI); }
        if (tglShowOppHand) { tglShowOppHand.isOn = GameManager.Instance.showOpponentHand; tglShowOppHand.onValueChanged.AddListener(v => { GameManager.Instance.showOpponentHand = v; GameManager.Instance.ToggleOpponentHandVisibility(); }); }
        if (tglAutoPhases) { tglAutoPhases.isOn = !GameManager.Instance.disableAutoPhases; tglAutoPhases.onValueChanged.AddListener(v => GameManager.Instance.disableAutoPhases = !v); }
        if (tglInfiniteLP) { tglInfiniteLP.isOn = GameManager.Instance.infiniteLP; tglInfiniteLP.onValueChanged.AddListener(v => GameManager.Instance.infiniteLP = v); }
        if (tglVFX) { tglVFX.isOn = GameManager.Instance.enableVFX; tglVFX.onValueChanged.AddListener(v => GameManager.Instance.enableVFX = v); }
        if (tglSFX) { tglSFX.isOn = GameManager.Instance.enableSFX; tglSFX.onValueChanged.AddListener(v => GameManager.Instance.enableSFX = v); }

        // Configura Seletores
        if (dropAct)
        {
            dropAct.ClearOptions();
            List<string> actOptions = new List<string> { "Default/Auto" };
            for (int i = 1; i <= 10; i++) actOptions.Add($"Act {i}");
            dropAct.AddOptions(actOptions);
            dropAct.value = GameManager.Instance.testActThemeIndex > 0 ? GameManager.Instance.testActThemeIndex : 0;
            dropAct.onValueChanged.AddListener(v => GameManager.Instance.testActThemeIndex = v == 0 ? -1 : v);
        }

        if (dropOpponent)
        {
            dropOpponent.ClearOptions();
            List<string> oppOptions = new List<string> { "Default/Random" };
            if (GameManager.Instance.characterDatabase != null)
                foreach (var c in GameManager.Instance.characterDatabase.characterDatabase) oppOptions.Add($"{c.name} ({c.id})");
            dropOpponent.AddOptions(oppOptions);
            int currentIdx = 0;
            if (!string.IsNullOrEmpty(GameManager.Instance.testOpponentID) && GameManager.Instance.characterDatabase != null)
                currentIdx = GameManager.Instance.characterDatabase.characterDatabase.FindIndex(c => c.id == GameManager.Instance.testOpponentID) + 1;
            dropOpponent.value = Mathf.Max(0, currentIdx);
            dropOpponent.onValueChanged.AddListener(v => GameManager.Instance.testOpponentID = v == 0 ? "" : GameManager.Instance.characterDatabase.characterDatabase[v - 1].id);
        }

        if (dropDeckVariant)
        {
            dropDeckVariant.ClearOptions();
            dropDeckVariant.AddOptions(new List<string> { "Aleatório", "Deck A", "Deck B", "Deck C" });
            dropDeckVariant.value = GameManager.Instance.testOpponentDeckVariant;
            dropDeckVariant.onValueChanged.AddListener(v => GameManager.Instance.testOpponentDeckVariant = v);
        }

        // Configura Botões
        if (btnCoin) btnCoin.onClick.AddListener(TestCoin);
        if (btnDice) btnDice.onClick.AddListener(TestDice);
        if (btnClock) btnClock.onClick.AddListener(TestClock);
        if (btnSpawnCard) btnSpawnCard.onClick.AddListener(TestSpawnCard);
        if (btnSimulateAttack) btnSimulateAttack.onClick.AddListener(TestSimulateAttack);
        if (btnSimulateTrap) btnSimulateTrap.onClick.AddListener(TestSimulateTrap);
    }

    void Update()
    {
        // Atalho para abrir/fechar o painel (Ctrl + T)
        bool openPanel = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
            openPanel = true;
#else
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            openPanel = true;
#endif

        if (openPanel)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.fullTestMode = !GameManager.Instance.fullTestMode;
                testPanel.SetActive(GameManager.Instance.fullTestMode);
            }
        }
    }

    public void ToggleAI(bool active)
    {
        if (OpponentAI.Instance != null)
        {
            OpponentAI.Instance.gameObject.SetActive(active);
        }
        GameManager.Instance.canPlaceOpponentCards = !active; // Se a IA tá desligada, o Dev controla o oponente
        Debug.Log($"[TestMode] Inteligência Artificial: {(active ? "LIGADA" : "DESLIGADA")}. Controle manual do oponente: {(!active ? "LIBERADO" : "BLOQUEADO")}.");
    }

    public void TestCoin()
    {
        Debug.Log("[TestMode] Simulando moeda...");
        GameManager.Instance.TossCoin(1, (r) => Debug.Log($"Resultado da Moeda: {r}"));
    }

    public void TestDice()
    {
        Debug.Log("[TestMode] Simulando dado...");
        GameManager.Instance.RollDice(1, false, (r) => Debug.Log($"Resultado do Dado: {r[0]}"));
    }

    public void TestClock()
    {
        Debug.Log("[TestMode] O Relógio será testado na próxima ativação de Swords of Revealing Light.");
    }

    public void TestSpawnCard()
    {
        if (GlobalCardSearchUI.Instance != null)
        {
            GlobalCardSearchUI.Instance.Show("Gerar Carta na Mão (ID ou Nome)", (data) => {
                if (data != null)
                {
                    GameManager.Instance.AddCardToHand(data, true);
                    Debug.Log($"[TestMode] {data.name} adicionada à mão do jogador.");
                }
            });
        }
    }

    public void TestSimulateAttack()
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection((t) => t.isOnField && !t.isPlayerCard && t.CurrentCardData.type.Contains("Monster"), (attacker) => {
                SpellTrapManager.Instance.StartTargetSelection((t2) => t2.isOnField && t2.isPlayerCard && t2.CurrentCardData.type.Contains("Monster"), (target) => {
                    Debug.Log($"[TestMode] Simulando ataque de {attacker.CurrentCardData.name} em {target.CurrentCardData.name}...");
                    if (BattleManager.Instance != null)
                    {
                        BattleManager.Instance.currentAttacker = attacker;
                        BattleManager.Instance.currentTarget = target;
                        ChainManager.Instance.AddToChain(attacker, attacker.isPlayerCard, ChainManager.TriggerType.Attack, target);
                    }
                });
            });
        }
    }

    public void TestSimulateTrap()
    {
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection((t) => t.isOnField && t.CurrentCardData.type.Contains("Trap") && t.isFlipped, (trap) => {
                Debug.Log($"[TestMode] Forçando ativação de armadilha {trap.CurrentCardData.name} sem gatilho!");
                GameManager.Instance.ActivateFieldSpellTrap(trap.gameObject);
            });
        }
    }

    // --- DEV ACTION MENU ---
    // Quando o Full Test Mode está ativo, clicar com SHIFT + Direito em uma carta abre este super menu.
    public void OpenDevCardMenu(CardDisplay card)
    {
        List<string> options = new List<string> { 
            "1. Enviar ao Cemitério (Destroy)", 
            "2. Banir (Remove from Play)", 
            "3. Retornar à Mão (Bounce)", 
            "4. Retornar ao Topo do Deck",
            "5. Mudar Posição",
            "6. Virar Face-Up/Down"
        };

        if (MultipleChoiceUI.Instance != null)
        {
            MultipleChoiceUI.Instance.Show(options, $"[DEV] Ações para {card.CurrentCardData.name}", 1, 1, (selected) => {
                string opt = selected[0];
                if (opt.Contains("Cemitério")) {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
                    GameManager.Instance.SendToGraveyard(card.CurrentCardData, card.isPlayerCard);
                    Destroy(card.gameObject);
                }
                else if (opt.Contains("Banir")) {
                    GameManager.Instance.BanishCard(card);
                }
                else if (opt.Contains("Mão")) {
                    GameManager.Instance.ReturnToHand(card);
                }
                else if (opt.Contains("Deck")) {
                    GameManager.Instance.ReturnToDeck(card, true);
                }
                else if (opt.Contains("Posição")) {
                    card.ChangePosition();
                }
                else if (opt.Contains("Face-Up")) {
                    if (card.isFlipped) card.RevealCard();
                    else card.ShowBack();
                }
            });
        }
    }
}