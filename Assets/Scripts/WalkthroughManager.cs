using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class WalkthroughManager : MonoBehaviour
{
    public static WalkthroughManager Instance;

    [Header("Painéis dos Atos")]
    [Tooltip("Arraste os painéis Panel_Act1, Panel_Act2, etc. nesta ordem.")]
    public GameObject[] actPanels;

    [Header("Configuração de Texto")]
    public float typeSpeed = 0.02f; // Velocidade da escrita

    // Dados temporários para iniciar o duelo
    private CharacterData pendingOpponent;
    private int pendingDuelIndex;
    private TextMeshProUGUI activeTextComponent;
    private string fullTextToShow;
    private bool isTyping = false;

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
                bool isActive = (i == arrayIndex);
                actPanels[i].SetActive(isActive);

                if (isActive)
                {
                    // Tenta encontrar o componente de texto dentro do painel ativo
                    activeTextComponent = actPanels[i].GetComponentInChildren<TextMeshProUGUI>();
                    
                    // Define o texto baseado no Database (se disponível)
                    if (GameManager.Instance != null && GameManager.Instance.campaignDatabase != null)
                    {
                        var actData = GameManager.Instance.campaignDatabase.GetActData(actIndex - 1);
                        if (actData != null)
                        {
                            fullTextToShow = actData.description;
                            // Adiciona info do oponente
                            fullTextToShow += $"\n\nOponente: {opponent.name}\nDificuldade: {opponent.difficulty}";
                            
                            StartCoroutine(TypeTextRoutine());
                        }
                    }
                }
            }
        }
    }

    IEnumerator TypeTextRoutine()
    {
        if (activeTextComponent == null) yield break;

        isTyping = true;
        activeTextComponent.text = "";

        foreach (char letter in fullTextToShow.ToCharArray())
        {
            activeTextComponent.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
    }

    void CompleteTextImmediately()
    {
        StopAllCoroutines();
        if (activeTextComponent != null)
            activeTextComponent.text = fullTextToShow;
        isTyping = false;
    }

    // --- Funções para os Botões (Ligue no Inspector) ---

    // Ligue nos botões "Next" e "Skip"
    public void Btn_StartDuel()
    {
        // Se estiver escrevendo, completa o texto primeiro
        if (isTyping)
        {
            CompleteTextImmediately();
            return;
        }

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
