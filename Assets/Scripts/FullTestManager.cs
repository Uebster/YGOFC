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
    public Button btnExodiaWin;
    public Button btnDestinyBoardWin;
    public Button btnSimulateAttack;
    public Button btnSimulateTrap;
    public Button btnCleanField;
    public Button btnRestartDuel;
    public Button btnSwitchTurn;
    public Button btnFusion;
    public Button btnRitual;
    public Button btnEquip;

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
        if (btnExodiaWin == null) btnExodiaWin = allButtons.FirstOrDefault(b => b.name.Contains("Exodia"));
        if (btnDestinyBoardWin == null) btnDestinyBoardWin = allButtons.FirstOrDefault(b => b.name.Contains("Destiny"));
        if (btnSimulateAttack == null) btnSimulateAttack = allButtons.FirstOrDefault(b => b.name.Contains("Attack"));
        if (btnSimulateTrap == null) btnSimulateTrap = allButtons.FirstOrDefault(b => b.name.Contains("Trap"));
        
        Button[] allSceneBtns = Resources.FindObjectsOfTypeAll<Button>();
        var cleanBtns = allSceneBtns.Where(b => b.name.Contains("Clean") && b.gameObject.scene.IsValid()).ToList();
        if (btnCleanField == null && cleanBtns.Count > 0) btnCleanField = cleanBtns[0];

        if (btnRestartDuel == null) btnRestartDuel = allButtons.FirstOrDefault(b => b.name.Contains("Restart"));
        if (btnSwitchTurn == null) btnSwitchTurn = allButtons.FirstOrDefault(b => b.name.Contains("Switch"));
        if (btnFusion == null) btnFusion = allButtons.FirstOrDefault(b => b.name.Contains("Fusion"));
        if (btnRitual == null) btnRitual = allButtons.FirstOrDefault(b => b.name.Contains("Ritual"));
        if (btnEquip == null) btnEquip = allButtons.FirstOrDefault(b => b.name.Contains("Equip"));

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
            dropOpponent.onValueChanged.AddListener(v => {
                GameManager.Instance.testOpponentID = v == 0 ? "" : GameManager.Instance.characterDatabase.characterDatabase[v - 1].id;
                GameManager.Instance.StartDuel(); // Força o reinício para aplicar o novo baralho
            });
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
            dropPlayer.onValueChanged.AddListener(v => {
                GameManager.Instance.testPlayerID = v == 0 ? "" : GameManager.Instance.characterDatabase.characterDatabase[v - 1].id;
                GameManager.Instance.StartDuel(); // Força o reinício para aplicar o novo baralho
            });
        }

        if (dropDeckVariant)
        {
            dropDeckVariant.ClearOptions();
            dropDeckVariant.AddOptions(new List<string> { "Aleatório", "Deck A", "Deck B", "Deck C" });
            dropDeckVariant.value = GameManager.Instance.testOpponentDeckVariant;
            dropDeckVariant.onValueChanged.AddListener(v => {
                GameManager.Instance.testOpponentDeckVariant = v;
                GameManager.Instance.StartDuel();
            });
        }

        // Configura Botões
        if (btnCoin) btnCoin.onClick.AddListener(TestCoin);
        if (btnDice) btnDice.onClick.AddListener(TestDice);
        if (btnClock) btnClock.onClick.AddListener(TestClock);
        if (btnSpawnCard) btnSpawnCard.onClick.AddListener(TestSpawnCard);
        if (btnExodiaWin) btnExodiaWin.onClick.AddListener(TestExodiaWin);
        if (btnDestinyBoardWin) btnDestinyBoardWin.onClick.AddListener(TestDestinyBoardWin);
        if (btnSimulateAttack) btnSimulateAttack.onClick.AddListener(TestSimulateAttack);
        if (btnSimulateTrap) btnSimulateTrap.onClick.AddListener(TestSimulateTrap);
        foreach(var btn in cleanBtns) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(TestCleanField); }
        if (btnRestartDuel) btnRestartDuel.onClick.AddListener(() => GameManager.Instance.StartDuel());
        if (btnFusion) btnFusion.onClick.AddListener(TestFusion);
        if (btnRitual) btnRitual.onClick.AddListener(TestRitual);
        if (btnEquip != null) btnEquip.onClick.AddListener(TestEquip);
    }

    void Update()
    {
        // Atalho para abrir/fechar o painel (Ctrl + T)
        bool openPanel = false;
        bool shiftPressed = false;
        bool rightClicked = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.tKey.wasPressedThisFrame)
            openPanel = true;
            
        shiftPressed = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        rightClicked = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            openPanel = true;
            
        shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        rightClicked = Input.GetMouseButtonDown(1);
        mousePos = Input.mousePosition;
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

                    // INJEÇÃO INTELIGENTE DE DEPENDÊNCIAS:
                    GameManager.Instance.Dev_InjectDependencies(data);
                }
            });
        }
        else
        {
            Debug.LogError("[TestMode] ERRO: GlobalCardSearchUI não encontrado na cena! Verifique se ele foi apagado acidentalmente.");
        }
    }

    public void TestExodiaWin()
    {
        Debug.Log("[TestMode] Simulando Exodia...");
        
        // Limpa a mão do jogador
        foreach (var go in new List<GameObject>(GameManager.Instance.playerHand)) Destroy(go);
        GameManager.Instance.playerHand.Clear();

        string[] names = { "Exodia the Forbidden One", "Right Arm of the Forbidden One", "Left Arm of the Forbidden One", "Right Leg of the Forbidden One", "Left Leg of the Forbidden One" };
        foreach(var n in names) {
            CardData data = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == n);
            if (data != null) GameManager.Instance.AddCardToHand(data, true);
        }
        
        GameManager.Instance.CheckExodiaWin();
    }

    public void TestDestinyBoardWin()
    {
        Debug.Log("[TestMode] Simulando Destiny Board...");
        StartCoroutine(DestinyBoardRoutine());
    }

    private IEnumerator DestinyBoardRoutine()
    {
        TestCleanField();
        
        string[] letters = { "Destiny Board", "Spirit Message \"I\"", "Spirit Message \"N\"", "Spirit Message \"A\"", "Spirit Message \"L\"" };
        
        for (int i = 0; i < letters.Length; i++)
        {
            CardData data = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == letters[i]);
            if (data != null)
            {
                GameManager.Instance.SetSpellTrapFromData(data, true, i, true); // True = Face-up
                
                if (GameManager.Instance.turnClockUI != null)
                {
                    bool clockDone = false;
                    GameManager.Instance.turnClockUI.AnimateTick(data.name, 5 - i, 4 - i, 5, () => clockDone = true);
                    yield return new WaitUntil(() => clockDone);
                }
                else yield return new WaitForSeconds(1.0f);
            }
        }
        
        yield return new WaitForSeconds(0.5f);

        // Tenta achar o painel mesmo se estiver desativado no Inspector
        if (DestinyBoardWinUI.Instance == null)
            DestinyBoardWinUI.Instance = Resources.FindObjectsOfTypeAll<DestinyBoardWinUI>().FirstOrDefault();

        if (DestinyBoardWinUI.Instance != null)
            DestinyBoardWinUI.Instance.ShowWinSequence(true, () => GameManager.Instance.EndDuel(true));
        else GameManager.Instance.EndDuel(true);
    }

    public void TestFusion()
    {
        Debug.Log("[TestMode] Configurando Fusão na Mão...");
        TestCleanField();
        
        CardData gaia = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Gaia The Fierce Knight");
        CardData curse = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Curse of Dragon");
        CardData poly = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Polymerization");
        CardData fusionMon = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Gaia the Dragon Champion");

        if (gaia != null && curse != null && poly != null && fusionMon != null)
        {
            if (!GameManager.Instance.GetPlayerExtraDeck().Contains(fusionMon))
                GameManager.Instance.GetPlayerExtraDeck().Add(fusionMon);
            GameManager.Instance.AddCardToHand(gaia, true);
            GameManager.Instance.AddCardToHand(curse, true);
            GameManager.Instance.AddCardToHand(poly, true);
            StartCoroutine(ActivateSpellAfterDelay("Polymerization"));
        }
    }

    public void TestRitual()
    {
        Debug.Log("[TestMode] Configurando Ritual na Mão...");
        TestCleanField();
        
        CardData relinquished = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Relinquished");
        CardData ritualSpell = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Black Illusion Ritual");
        CardData tributeFodder = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Sangan"); 

        if (relinquished != null && ritualSpell != null && tributeFodder != null)
        {
            GameManager.Instance.AddCardToHand(relinquished, true);
            GameManager.Instance.AddCardToHand(tributeFodder, true);
            GameManager.Instance.AddCardToHand(ritualSpell, true);
            StartCoroutine(ActivateSpellAfterDelay("Black Illusion Ritual"));
        }
    }

    public void TestEquip()
    {
        Debug.Log("[TestMode] Configurando Equipamento...");
        TestCleanField();

        // Puxamos Axe Raider (Monstro) e Axe of Despair (Equip)
        CardData axeRaider = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Axe Raider");
        CardData equipSpell = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == "Axe of Despair");
        
        if (axeRaider == null) axeRaider = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.type.Contains("Monster"));
        if (equipSpell == null) equipSpell = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.id == "0103"); // Fallback para Axe of Despair por ID

        if (axeRaider != null && equipSpell != null)
        {
            // 1. Invoca o monstro no campo do JOGADOR para testar o equipamento em um alvo próprio.
            CardDisplay monsterDisplay = GameManager.Instance.SpecialSummonFromData(axeRaider, true, 2, true, false);
            
            // 2. Coloca a magia na mão do jogador
            GameManager.Instance.AddCardToHand(equipSpell, true);
            
            // 3. Força a transição para a Main Phase 1 para que o Activate seja legal
            if (PhaseManager.Instance != null)
            {
                PhaseManager.Instance.currentPhase = GamePhase.Main1;
                GameManager.Instance.isPlayerTurn = true;
            }

            // 4. Informa o jogador para testar
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage($"Cenário de Equip pronto. Ative '{equipSpell.name}' da sua mão e selecione o seu '{axeRaider.name}' como alvo.");
            }
        }
    }

    private IEnumerator ActivateSpellAfterDelay(string spellName)
    {
        if (PhaseManager.Instance != null && PhaseManager.Instance.currentPhase != GamePhase.Main1) { PhaseManager.Instance.currentPhase = GamePhase.Main1; GameManager.Instance.isPlayerTurn = true; }
        yield return new WaitForSeconds(0.5f);
        GameObject spellGO = GameManager.Instance.playerHand.FirstOrDefault(go => go.GetComponent<CardDisplay>().CurrentCardData.name == spellName);
        if (spellGO != null) GameManager.Instance.PlaySpellTrap(spellGO, spellGO.GetComponent<CardDisplay>().CurrentCardData, false);
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
        Debug.Log("[TestMode] Iniciando simulação de Trap...");
        StartCoroutine(SimulateTrapRoutine());
    }

    private IEnumerator SimulateTrapRoutine()
    {
        TestCleanField();
        
        CardData trapData = GameManager.Instance.cardDatabase.GetCardById("0164"); // Trap Hole
        if (trapData == null && GameManager.Instance.cardDatabase.cardDatabase.Count > 0)
            trapData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.type.Contains("Trap"));

        if (trapData != null)
        {
            GameManager.Instance.SetSpellTrapFromData(trapData, true, 2, false); // False = Face-Down
        }

        yield return new WaitForSeconds(1.0f);

        if (GameManager.Instance.isPlayerTurn) GameManager.Instance.SwitchTurn();
        yield return new WaitForSeconds(1.0f);

        Debug.Log("[TestMode] Oponente invoca monstro. Verifique a janela de Chain para sua Armadilha!");
        CardData oMon = GameManager.Instance.cardDatabase.GetCardById("0001"); // Blue-Eyes ou algo forte
        GameManager.Instance.SpecialSummonFromData(oMon, false, 2, true, false);
    }

    public void TestCleanField()
    {
        Debug.Log("[TestMode] Limpando APENAS o campo cirurgicamente...");
        if (GameManager.Instance.duelFieldUI != null)
        {
            System.Action<Transform[]> ClearZones = (zones) => {
                if (zones == null) return;
                foreach (var z in zones) {
                    foreach (Transform child in z) {
                        var cd = child.GetComponent<CardDisplay>();
                        if (cd != null && CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd);
                        Destroy(child.gameObject);
                    }
                }
            };
            ClearZones(GameManager.Instance.duelFieldUI.playerMonsterZones);
            ClearZones(GameManager.Instance.duelFieldUI.opponentMonsterZones);
            ClearZones(GameManager.Instance.duelFieldUI.playerSpellZones);
            ClearZones(GameManager.Instance.duelFieldUI.opponentSpellZones);
            
            if (GameManager.Instance.duelFieldUI.playerFieldSpell != null) foreach (Transform child in GameManager.Instance.duelFieldUI.playerFieldSpell) {
                var cd = child.GetComponent<CardDisplay>(); if (cd != null && CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd); Destroy(child.gameObject);
            }
            if (GameManager.Instance.duelFieldUI.opponentFieldSpell != null) foreach (Transform child in GameManager.Instance.duelFieldUI.opponentFieldSpell) {
                var cd = child.GetComponent<CardDisplay>(); if (cd != null && CardEffectManager.Instance != null) CardEffectManager.Instance.OnCardLeavesField(cd); Destroy(child.gameObject);
            }
        }
        
        CardLink[] links = FindObjectsByType<CardLink>(FindObjectsSortMode.None);
        foreach (var link in links) Destroy(link.gameObject);

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.blockedZonesByCard.Clear();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GetPlayerGraveyard().Clear();
            GameManager.Instance.GetOpponentGraveyard().Clear();
            GameManager.Instance.GetPlayerRemoved().Clear();
            GameManager.Instance.GetOpponentRemoved().Clear();
            if (GameManager.Instance.playerGraveyardDisplay != null) GameManager.Instance.playerGraveyardDisplay.UpdatePile(GameManager.Instance.GetPlayerGraveyard(), GameManager.Instance.GetCardBackTexture());
            if (GameManager.Instance.opponentGraveyardDisplay != null) GameManager.Instance.opponentGraveyardDisplay.UpdatePile(GameManager.Instance.GetOpponentGraveyard(), GameManager.Instance.GetCardBackTexture());
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
            "6. Virar Face-Up/Down",
            "7. Trocar Controle (Change of Heart)",
            "8. Colocar no Campo (Forçar)"
        };

        // Força a busca da UI caso ela comece desligada no Inspector
        if (MultipleChoiceUI.Instance == null)
        {
            MultipleChoiceUI.Instance = Resources.FindObjectsOfTypeAll<MultipleChoiceUI>().FirstOrDefault(x => x.gameObject.scene.IsValid());
        }

        if (MultipleChoiceUI.Instance != null)
        {
            MultipleChoiceUI.Instance.Show(options, $"[DEV] Ações para {card.CurrentCardData.name}", 1, 1, (selected) => {
                if (selected == null || selected.Count == 0) return; // Blinda contra cliques no botão Cancel/Close

                string opt = selected[0];
                if (opt.Contains("Cemitério")) {
                    if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(card);
                    GameManager.Instance.MoveCard(card, CardLocation.Graveyard, SendReason.Effect);
                }
                else if (opt.Contains("Banir")) {
                    GameManager.Instance.MoveCard(card, CardLocation.Banished, SendReason.Effect);
                }
                else if (opt.Contains("Mão")) {
                    GameManager.Instance.MoveCard(card, CardLocation.Hand, SendReason.Effect);
                }
                else if (opt.Contains("Deck")) {
                    GameManager.Instance.MoveCard(card, CardLocation.Deck, SendReason.Effect);
                }
                else if (opt.Contains("Posição")) {
                    card.ChangePosition();
                }
                else if (opt.Contains("Face-Up")) {
                    if (card.isFlipped) card.RevealCard();
                    else card.ShowBack();
                }
                else if (opt.Contains("Controle")) {
                    GameManager.Instance.SwitchControl(card);
                }
                else if (opt.Contains("Campo")) {
                    GameManager.Instance.Dev_ForceCardToField(card);
                }
            });
        }
        else
        {
            Debug.LogError("[DevMenu] ERRO: O painel 'MultipleChoiceUI' não foi encontrado na cena! Siga as instruções para criar o Panel_MultipleChoice no Canvas.");
        }
    }
}