using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    public enum SimulationMode { Visual, FastLog }

    [Header("Configurações da Simulação")]
    public SimulationMode currentMode = SimulationMode.Visual;
    public bool autoStartSimulation = false;
    
    [Header("Visual Mode Settings")]
    [Tooltip("Velocidade do tempo no modo Visual (1x = normal).")]
    public float visualTimeScale = 1.5f;
    [Tooltip("Delay entre ações no modo Visual.")]
    public float visualActionDelay = 1.0f;

    [Header("Fast Log Mode Settings")]
    [Tooltip("Velocidade do tempo no modo Rápido (50x = muito rápido).")]
    public float fastTimeScale = 50.0f;
    [Tooltip("Delay mínimo entre ações no modo Rápido (para não travar a thread).")]
    public float fastActionDelay = 0.05f; 

    [Header("Status")]
    public int duelsToRun = 10;
    private int duelsFinished = 0;
    private bool isRunning = false;
    
    // Logs para a GUI
    private List<string> simulationLogs = new List<string>();
    private Vector2 scrollPosition;
    private string currentLogFile;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (autoStartSimulation)
        {
            StartCoroutine(StartDelayed());
        }
    }

    IEnumerator StartDelayed()
    {
        yield return new WaitForSeconds(2f); // Espera inicialização dos outros managers
        StartSimulation(currentMode);
    }

    // Chamado pelos botões da GUI
    public void StartSimulation(SimulationMode mode)
    {
        if (isRunning) return;
        
        // Cria arquivo de log
        string fileName = $"SimLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        currentLogFile = Path.Combine(Application.persistentDataPath, fileName);
        try {
            File.WriteAllText(currentLogFile, $"=== SIMULATION STARTED: {mode} ===\n");
            Debug.Log($"[SIM] Logging to: {currentLogFile}");
        } catch (System.Exception e) {
            Debug.LogError($"[SIM] Could not create log file: {e.Message}");
            currentLogFile = null;
        }

        currentMode = mode;
        StartCoroutine(SimulationLoop());
    }

    public void StopSimulation()
    {
        StopAllCoroutines();
        isRunning = false;
        
        // Restaura estado original
        Time.timeScale = 1.0f;
        if (GameManager.Instance) GameManager.Instance.isSimulating = false;
        if (DuelFXManager.Instance) DuelFXManager.Instance.enableAnimations = true; 
        
        Log("=== SIMULAÇÃO INTERROMPIDA ===");
    }

    void Log(string msg)
    {
        string log = $"[{System.DateTime.Now:HH:mm:ss}] {msg}";
        Debug.Log(log); // Também manda pro console da Unity
        
        simulationLogs.Add(log);
        if (simulationLogs.Count > 50) simulationLogs.RemoveAt(0); // Mantém apenas os últimos 50 logs na tela
        scrollPosition.y = float.MaxValue; // Auto-scroll para o final

        if (!string.IsNullOrEmpty(currentLogFile))
        {
            try
            {
                File.AppendAllText(currentLogFile, log + "\n");
            }
            catch { }
        }
    }

    void OnGUI()
    {
        // Desenha uma caixa no canto direito da tela
        GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 450));
        GUI.skin.box.fontSize = 14;
        GUILayout.Box("--- SIMULADOR DE CAOS ---");
        
        if (!isRunning)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Iniciar Visual (Assistir)", GUILayout.Height(30))) 
                StartSimulation(SimulationMode.Visual);
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Iniciar Rápido (Log)", GUILayout.Height(30))) 
                StartSimulation(SimulationMode.FastLog);
            
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Abrir Pasta de Logs", GUILayout.Height(25))) 
                Application.OpenURL(Application.persistentDataPath);
        }
        else
        {
            GUI.backgroundColor = Color.white;
            GUILayout.Label($"Modo: {currentMode}");
            GUILayout.Label($"Duelos: {duelsFinished}/{duelsToRun}");
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("PARAR SIMULAÇÃO", GUILayout.Height(30))) 
                StopSimulation();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        GUILayout.Label("Logs Recentes:");
        
        // Área de scroll para os logs
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(250));
        GUIStyle logStyle = new GUIStyle(GUI.skin.label);
        logStyle.fontSize = 11;
        logStyle.wordWrap = true;
        
        foreach (var l in simulationLogs) 
        {
            GUILayout.Label(l, logStyle);
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    IEnumerator SimulationLoop()
    {
        isRunning = true;
        duelsFinished = 0;
        
        // Configurações baseadas no modo escolhido
        if (currentMode == SimulationMode.Visual)
        {
            Time.timeScale = visualTimeScale;
            if (DuelFXManager.Instance) DuelFXManager.Instance.enableAnimations = true;
        }
        else
        {
            Time.timeScale = fastTimeScale;
            if (DuelFXManager.Instance) DuelFXManager.Instance.enableAnimations = false; // Desativa VFX para velocidade máxima
        }

        GameManager.Instance.isSimulating = true;
        
        // Fecha janelas que possam estar abertas
        if (UIManager.Instance != null) UIManager.Instance.CloseAllPopups();

        Log($"=== INÍCIO DA SIMULAÇÃO ({currentMode}) ===");

        while (duelsFinished < duelsToRun && isRunning)
        {
            Log($"--- Iniciando Duelo {duelsFinished + 1}/{duelsToRun} ---");
            
            // Inicia Duelo
            GameManager.Instance.StartDuel();
            
            // Espera a animação de compra inicial (no modo rápido, isso passa voando devido ao TimeScale)
            yield return new WaitForSeconds(3.0f); 

            int turn = 0;
            int maxTurns = 40;
            float currentDelay = (currentMode == SimulationMode.Visual) ? visualActionDelay : fastActionDelay;

            while (!GameManager.Instance.isDuelOver && turn < maxTurns && isRunning)
            {
                turn++;
                bool isPlayer = GameManager.Instance.isPlayerTurn;
                // Log($"Turno {turn} - {(isPlayer ? "JOGADOR" : "OPONENTE")}");

                // --- Draw Phase ---
                if (PhaseManager.Instance.currentPhase != GamePhase.Draw)
                    PhaseManager.Instance.ChangePhase(GamePhase.Draw);
                
                yield return new WaitForSeconds(currentDelay);

                // --- Standby Phase ---
                PhaseManager.Instance.ChangePhase(GamePhase.Standby);
                yield return new WaitForSeconds(currentDelay);

                // --- Main Phase 1 ---
                PhaseManager.Instance.ChangePhase(GamePhase.Main1);
                yield return SimulateMainPhase(isPlayer, currentDelay);

                // --- Battle Phase ---
                if (turn > 1) // Regra: Não pode atacar no turno 1
                {
                    PhaseManager.Instance.ChangePhase(GamePhase.Battle);
                    yield return SimulateBattlePhase(isPlayer, currentDelay);
                }

                // --- Main Phase 2 ---
                PhaseManager.Instance.ChangePhase(GamePhase.Main2);
                yield return SimulateMainPhase(isPlayer, currentDelay);

                // --- End Phase ---
                PhaseManager.Instance.ChangePhase(GamePhase.End);
                yield return new WaitForSeconds(currentDelay);

                // Troca Turno
                GameManager.Instance.SwitchTurn();
                yield return new WaitForSeconds(currentDelay);
            }

            if (!GameManager.Instance.isDuelOver)
            {
                Log("Limite de turnos atingido. Empate forçado.");
                GameManager.Instance.EndDuel(false);
            }
            else
            {
                Log("Duelo finalizado.");
            }

            duelsFinished++;
            yield return new WaitForSeconds(2.0f); // Pausa entre duelos
        }

        Log("=== FIM DA SIMULAÇÃO ===");
        StopSimulation();
    }

    IEnumerator SimulateMainPhase(bool isPlayer, float delay)
    {
        // Tenta realizar ações aleatórias da mão
        List<GameObject> hand = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
        
        int actionsToTry = Random.Range(1, 3); // Tenta 1 ou 2 ações

        for (int i = 0; i < actionsToTry; i++)
        {
            hand = isPlayer ? GameManager.Instance.playerHand : GameManager.Instance.opponentHand;
            if (hand.Count == 0) break;

            GameObject cardObj = hand[Random.Range(0, hand.Count)];
            if (cardObj == null) continue;

            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            if (display == null) continue;

            CardData data = display.CurrentCardData;

            if (data.type.Contains("Monster"))
            {
                bool set = Random.value > 0.5f;
                Log($"[SIM] {(isPlayer?"P":"O")} tenta {(set ? "Setar" : "Invocar")} {data.name}");
                GameManager.Instance.TrySummonMonster(cardObj, data, set);
            }
            else if (data.type.Contains("Spell") || data.type.Contains("Trap"))
            {
                bool set = data.type.Contains("Trap") || Random.value > 0.3f;
                Log($"[SIM] {(isPlayer?"P":"O")} tenta {(set ? "Setar" : "Ativar")} {data.name}");
                GameManager.Instance.PlaySpellTrap(cardObj, data, set);
            }

            yield return new WaitForSeconds(delay);
        }

        // Tenta mudar posições
        Transform[] myZones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        foreach (var zone in myZones)
        {
            if (zone.childCount > 0)
            {
                if (Random.value > 0.8f) // 20% chance
                {
                    CardDisplay cd = zone.GetChild(0).GetComponent<CardDisplay>();
                    if (cd != null && !cd.hasChangedPositionThisTurn && !cd.summonedThisTurn)
                    {
                        BattleManager.Instance.TryChangePosition(cd);
                        yield return new WaitForSeconds(delay);
                    }
                }
            }
        }
    }

    IEnumerator SimulateBattlePhase(bool isPlayer, float delay)
    {
        Transform[] myZones = isPlayer ? GameManager.Instance.duelFieldUI.playerMonsterZones : GameManager.Instance.duelFieldUI.opponentMonsterZones;
        Transform[] oppZones = isPlayer ? GameManager.Instance.duelFieldUI.opponentMonsterZones : GameManager.Instance.duelFieldUI.playerMonsterZones;

        List<CardDisplay> potentialAttackers = new List<CardDisplay>();
        foreach (var z in myZones)
        {
            if (z.childCount > 0)
            {
                var cd = z.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null && cd.position == CardDisplay.BattlePosition.Attack && !cd.hasAttackedThisTurn) 
                    potentialAttackers.Add(cd);
            }
        }

        foreach (var attacker in potentialAttackers)
        {
            if (BattleManager.Instance == null) break;

            BattleManager.Instance.PrepareAttack(attacker);

            if (BattleManager.Instance.currentAttacker == attacker)
            {
                List<CardDisplay> targets = new List<CardDisplay>();
                foreach (var z in oppZones)
                {
                    if (z.childCount > 0) targets.Add(z.GetChild(0).GetComponent<CardDisplay>());
                }

                if (targets.Count > 0)
                {
                    CardDisplay target = targets[Random.Range(0, targets.Count)];
                    Log($"[SIM] {attacker.CurrentCardData.name} ataca {target.CurrentCardData.name}");
                    BattleManager.Instance.SelectTarget(target);
                }
                else
                {
                    Log($"[SIM] {attacker.CurrentCardData.name} ataca direto");
                    BattleManager.Instance.TryDirectAttack();
                }
                
                yield return new WaitForSeconds(delay);
            }
        }
    }
}
