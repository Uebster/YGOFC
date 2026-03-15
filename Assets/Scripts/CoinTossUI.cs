using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class CoinTossUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public GameObject choicePanel;
    public Button btnHeads;
    public Button btnTails;
    
    [Header("Coins Board")]
    public Transform coinsContainer;
    public GameObject coinPrefab; // Um prefab simples com um componente Image
    
    [Header("Result & Actions")]
    public TextMeshProUGUI resultText;
    public GameObject actionPanel;
    public Button btnAction; // Botão de "Continue" ou "Roll Again"
    public TextMeshProUGUI btnActionText;

    [Header("Visual Configs")]
    public Sprite headsSprite;
    public Sprite tailsSprite;
    public float spinDuration = 1.5f;
    public float spinSpeed = 0.1f;

    private int currentWins = 0;
    private int coinsToToss = 1;
    private bool isLoopMode = false;
    private bool forceHeadsMode = false;
    private bool playerChoseHeads = true;
    
    private Action<int> onCompleteCallback;

    void Start()
    {
        btnHeads?.onClick.AddListener(() => StartToss(true));
        btnTails?.onClick.AddListener(() => StartToss(false));
    }

    public void ShowToss(int count, bool requireChoice, bool loopUntilTails, Action<int> callback, bool forceHeads)
    {
        gameObject.SetActive(true);
        onCompleteCallback = callback;
        coinsToToss = count;
        isLoopMode = loopUntilTails;
        forceHeadsMode = forceHeads;
        currentWins = 0;
        
        ClearCoins();
        resultText.text = "";
        actionPanel.SetActive(false);

        // Se for 1 moeda e o jogo estiver configurado para escolher, mostra os botões.
        if (requireChoice && count == 1 && !loopUntilTails)
        {
            titleText.text = "Choose: Heads or Tails?";
            choicePanel.SetActive(true);
        }
        else
        {
            // Múltiplas moedas ou Loop (assume-se que o "acerto" é sempre Cara)
            choicePanel.SetActive(false);
            StartToss(true);
        }
    }

    private void StartToss(bool choseHeads)
    {
        playerChoseHeads = choseHeads;
        choicePanel.SetActive(false);
        titleText.text = isLoopMode ? $"Winning Streak: {currentWins}" : "Tossing...";
        
        StartCoroutine(TossAnimationCoroutine());
    }

    private IEnumerator TossAnimationCoroutine()
    {
        List<Image> activeCoins = new List<Image>();
        
        // Instancia as moedas
        int spawnCount = isLoopMode ? 1 : coinsToToss;
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject coinGO = Instantiate(coinPrefab, coinsContainer);
            activeCoins.Add(coinGO.GetComponent<Image>());
        }

        // Efeito visual de giro (alternando sprites rapidamente)
        float elapsed = 0f;
        bool toggle = true;
        while (elapsed < spinDuration)
        {
            foreach (var coin in activeCoins)
                coin.sprite = toggle ? headsSprite : tailsSprite;
            
            toggle = !toggle;
            elapsed += spinSpeed;
            yield return new WaitForSeconds(spinSpeed);
        }

        // Define os resultados reais
        int roundWins = 0;
        bool loopFailed = false;

        foreach (var coin in activeCoins)
        {
            // Se forceHeads (debug) for true, sempre cai Cara (Heads)
            bool landedOnHeads = forceHeadsMode || (UnityEngine.Random.value > 0.5f);
            coin.sprite = landedOnHeads ? headsSprite : tailsSprite;

            bool isWin = (landedOnHeads == playerChoseHeads);
            if (isWin) roundWins++;
            else if (isLoopMode) loopFailed = true;
        }

        currentWins += roundWins;

        // Atualiza a UI baseado no modo
        actionPanel.SetActive(true);
        btnAction.onClick.RemoveAllListeners();

        if (isLoopMode)
        {
            if (loopFailed)
            {
                resultText.text = "Tails! The streak ended.";
                btnActionText.text = "Finish";
                btnAction.onClick.AddListener(() => { gameObject.SetActive(false); onCompleteCallback?.Invoke(currentWins); });
            }
            else
            {
                titleText.text = $"Winning Streak: {currentWins}";
                resultText.text = "Heads! You can roll again.";
                btnActionText.text = "Roll Again";
                btnAction.onClick.AddListener(() => StartToss(true));
            }
        }
        else
        {
            resultText.text = coinsToToss == 1 ? (roundWins == 1 ? "Success!" : "Failed!") : $"Result: {roundWins} / {coinsToToss} Wins";
            btnActionText.text = "Continue";
            btnAction.onClick.AddListener(() => { gameObject.SetActive(false); onCompleteCallback?.Invoke(currentWins); });
        }
    }

    private void ClearCoins() { foreach (Transform child in coinsContainer) Destroy(child.gameObject); }
}