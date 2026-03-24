using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
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
    public TMP_Dropdown dropPlayer;

    [Header("Botões de Ação")]
    public Button btnCoin;
    public Button btnDice;
    public Button btnClock;
    public Button btnSpawnCard;
    public Button btnSimulateAttack;
    public Button btnSimulateTrap;
    public Button btnCleanField;
    public Button btnRestartDuel;
    public Button btnSwitchTurn;

    void Awake()
    {
        Instance = this;
        if (testPanel != null) testPanel.SetActive(GameManager.Instance != null && GameManager.Instance.fullTestMode);
    }

    IEnumerator Start()
    {
        // Aguarda a inicialização do GameManager (evita o erro do return prematuro que matava os botões)
        while (GameManager.Instance == null) yield return null;

        // Auto-atribuição de botões caso tenham se perdido no Inspector
        Button[] allButtons = testPanel != null ? testPanel.GetComponentsInChildren<Button>(true) : GetComponentsInChildren<Button>(true);
        if (btnCoin == null) btnCoin = allButtons.FirstOrDefault(b => b.name.Contains("Coin"));
        if (btnDice == null) btnDice = allButtons.FirstOrDefault(b => b.name.Contains("Dice"));
        if (btnClock == null) btnClock = allButtons.FirstOrDefault(b => b.name.Contains("Clock"));
        if (btnSpawnCard == null) btnSpawnCard = allButtons.FirstOrDefault(b => b.name.Contains("Spawn"));
        if (btnSimulateAttack == null) btnSimulateAttack = allButtons.FirstOrDefault(b => b.name.Contains("Attack"));
        if (btnSimulateTrap == null) btnSimulateTrap = allButtons.FirstOrDefault(b => b.name.Contains("Trap"));
        if (btnCleanField == null) btnCleanField = allButtons.FirstOrDefault(b => b.name.Contains("Clean"));
        if (btnRestartDuel == null) btnRestartDuel = allButtons.FirstOrDefault(b => b.name.Contains("Restart"));
        if (btnSwitchTurn == null) btnSwitchTurn = allButtons.FirstOrDefault(b => b.name.Contains("Switch"));

        TMP_Dropdown[] allDropdowns = testPanel != null ? testPanel.GetComponentsInChildren<TMP_Dropdown>(true) : GetComponentsInChildren<TMP_Dropdown>(true);
        if (dropPlayer == null) dropPlayer = allDropdowns.FirstOrDefault(d => d.name.Contains("Player"));

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

        if (dropPlayer)
        {
            dropPlayer.ClearOptions();
            List<string> playerOptions = new List<string> { "Default/Save" };
            if (GameManager.Instance.characterDatabase != null)
                foreach (var c in GameManager.Instance.characterDatabase.characterDatabase) playerOptions.Add($"{c.name} ({c.id})");
            dropPlayer.AddOptions(playerOptions);
            int currentPlayerIdx = 0;
            if (!string.IsNullOrEmpty(GameManager.Instance.testPlayerID) && GameManager.Instance.characterDatabase != null)
                currentPlayerIdx = GameManager.Instance.characterDatabase.characterDatabase.FindIndex(c => c.id == GameManager.Instance.testPlayerID) + 1;
            dropPlayer.value = Mathf.Max(0, currentPlayerIdx);
            dropPlayer.onValueChanged.AddListener(v => GameManager.Instance.testPlayerID = v == 0 ? "" : GameManager.Instance.characterDatabase.characterDatabase[v - 1].id);
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
        if (btnCleanField) btnCleanField.onClick.AddListener(TestCleanField);
        if (btnRestartDuel) btnRestartDuel.onClick.AddListener(() => GameManager.Instance.StartDuel());
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
        Debug.Log("[TestMode] Simulando Relógio de Turno...");
        if (GameManager.Instance != null && GameManager.Instance.turnClockUI != null)
        {
            GameObject clockGo = GameManager.Instance.turnClockUI.gameObject;
            clockGo.SetActive(!clockGo.activeSelf);
        }
        else
        {
            Debug.LogWarning("[TestMode] Referência para TurnClockUI não encontrada no GameManager.");
        }
    }

    public void TestSpawnCard()
    {
        // Se o UI de Busca começar desativado na cena, o Instance dele será nulo.
        // Isso força a Unity a encontrá-lo mesmo desligado e ligá-lo à força!
        if (GlobalCardSearchUI.Instance == null)
        {
            GlobalCardSearchUI.Instance = Resources.FindObjectsOfTypeAll<GlobalCardSearchUI>().FirstOrDefault();
        }

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
        else
        {
            Debug.LogError("[TestMode] ERRO: GlobalCardSearchUI não encontrado na cena! Verifique se ele foi apagado acidentalmente.");
        }
    }

    public void TestSimulateAttack()
    {
        Debug.Log("[TestMode] Configurando cenário de batalha simulado...");
        TestCleanField();

        StartCoroutine(SimulateAttackRoutine());
    }

    private IEnumerator SimulateAttackRoutine()
    {
        yield return new WaitForSeconds(0.2f); // Pequeno delay para o campo limpar

        // Invoca monstros para o jogador
        CardData p_monster1_data = GameManager.Instance.cardDatabase.GetCardById("0001"); // Blue-Eyes White Dragon
        if (p_monster1_data != null) GameManager.Instance.SpecialSummonFromData(p_monster1_data, true, 0, true, false);

        yield return new WaitForSeconds(0.1f);

        // Invoca monstros para o oponente
        CardData o_monster1_data = GameManager.Instance.cardDatabase.GetCardById("0005"); // Dark Magician
        if (o_monster1_data != null) GameManager.Instance.SpecialSummonFromData(o_monster1_data, false, 0, true, false);
        
        yield return new WaitForSeconds(0.1f);

        CardData o_monster2_data = GameManager.Instance.cardDatabase.GetCardById("0010"); // Giant Soldier of Stone
        if (o_monster2_data != null) GameManager.Instance.SpecialSummonFromData(o_monster2_data, false, 0, true, true); // Em defesa

        Debug.Log("[TestMode] Cenário de batalha pronto. É o turno do jogador. Mude para a Battle Phase para atacar.");
    }

    public void TestSimulateTrap()
    {
        Debug.Log($"[TestMode] Teste de trap simulado (Em migração para Lua API)");
    }

    public void TestCleanField()
    {
        Debug.Log("[TestMode] Limpando o campo...");
        GameManager.Instance.CleanupDuelState();
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