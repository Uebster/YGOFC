using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WalkthroughManager : MonoBehaviour
{
    public static WalkthroughManager Instance;

    [Header("Painéis dos Atos")]
    [Tooltip("Arraste os painéis Panel_Act1, Panel_Act2, etc. nesta ordem.")]
    public GameObject[] actPanels;

    // Dados temporários para iniciar o duelo
    private CharacterData pendingOpponent;
    private int pendingDuelIndex;

    void Awake()
    {
        Instance = this;

        // AUTO-CONFIGURAÇÃO: Se a lista estiver vazia, tenta encontrar os painéis automaticamente
        if (actPanels == null || actPanels.Length == 0)
        {
            List<GameObject> foundPanels = new List<GameObject>();
            // Procura por Panel_Act1 até Panel_Act10 (ou mais se precisar)
            for (int i = 1; i <= 20; i++)
            {
                Transform t = transform.Find($"Panel_Act{i}");
                if (t != null) foundPanels.Add(t.gameObject);
            }
            actPanels = foundPanels.ToArray();
        }
    }

    // Garante que o Instance esteja setado quando o objeto for ativado
    void OnEnable()
    {
        Instance = this;
    }

    // Chamado pelo CampaignNode
    public void ShowAct(int actIndex, CharacterData opponent, int duelIndex)
    {
        // Salva os dados para quando clicar em "Next"
        pendingOpponent = opponent;
        pendingDuelIndex = duelIndex;

        // Abre a tela de Walkthrough
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIManager.Instance.walkthroughScreen);
        }

        // Ativa apenas o painel do Ato correspondente
        // Nota: actIndex vem como 1, 2, 3... mas o array começa em 0.
        int arrayIndex = actIndex - 1;

        for (int i = 0; i < actPanels.Length; i++)
        {
            if (actPanels[i] != null)
            {
                actPanels[i].SetActive(i == arrayIndex);
            }
        }
    }

    // --- Funções para os Botões (Ligue no Inspector) ---

    // Ligue nos botões "Next" e "Skip"
    public void Btn_StartDuel()
    {
        if (pendingOpponent != null && GameManager.Instance != null)
        {
            Debug.Log("Walkthrough: Iniciando duelo...");
            GameManager.Instance.StartDuel(pendingOpponent, pendingDuelIndex);
            
            if (UIManager.Instance != null)
                UIManager.Instance.ShowScreen(UIManager.Instance.duelScreen);
        }
        else
        {
            Debug.LogError("Walkthrough: Dados do oponente perdidos ou inválidos!");
        }
    }

    // Ligue no botão "Return"
    public void Btn_ReturnToMap()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Btn_Campaign();
        }
    }
}
