using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class DiceRollUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public GameObject choicePanel;
    public Button[] btnGuesses; // Array de botões para os números de 1 a 6
    
    [Header("Dice Board")]
    public Transform diceContainer;
    public GameObject dicePrefab; // Prefab de dado com componente Button e Image
    
    [Header("Result & Actions")]
    public TextMeshProUGUI resultText;
    public GameObject actionPanel;
    public Button btnAction;
    public TextMeshProUGUI btnActionText;

    [Header("Visual Configs")]
    public Sprite[] diceFaces; // 6 sprites correspondentes aos lados 1, 2, 3, 4, 5, 6
    public Sprite[] diceBlurs; // Sprites borrados para simular velocidade (Opcional)
    public float spinDuration = 1.5f;
    public float spinSpeed = 0.05f; // Quão rápido troca de sprite
    public float jumpHeight = 150f;

    private int diceToRoll = 1;
    private bool isManualSpin = false;
    private bool forceSixMode = false;
    private int playerGuess = -1;
    
    private Action<List<int>> onCompleteCallback;
    private List<Button> activeDiceButtons = new List<Button>();
    private List<int> finalResults = new List<int>();
    private int diceSpunCount = 0;

    void Awake()
    {
        // Garante que o painel comece invisível, mesmo se deixado ativo no Editor
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Configura os botões de palpite de 1 a 6 automaticamente
        for (int i = 0; i < btnGuesses.Length; i++)
        {
            int guessValue = i + 1;
            if (btnGuesses[i] != null)
                btnGuesses[i].onClick.AddListener(() => StartRollSequence(guessValue));
        }
    }

    public void ShowRoll(int count, bool requireChoice, bool manualSpin, Action<List<int>> callback, bool forceSix)
    {
        gameObject.SetActive(true);
        onCompleteCallback = callback;
        diceToRoll = count;
        forceSixMode = forceSix;
        isManualSpin = manualSpin;
        
        finalResults.Clear();
        diceSpunCount = 0;
        activeDiceButtons.Clear();
        
        ClearDice();
        resultText.text = "";
        actionPanel.SetActive(false);

        if (requireChoice && count == 1) // Adivinhação costuma ser só para 1 dado
        {
            titleText.text = "Guess the Number!";
            choicePanel.SetActive(true);
        }
        else
        {
            // Pula a escolha
            StartRollSequence(-1);
        }
    }

    private void StartRollSequence(int guess)
    {
        playerGuess = guess;
        choicePanel.SetActive(false);
        titleText.text = "Rolling...";

        SetupDiceOnBoard();
    }

    private void SetupDiceOnBoard()
    {
        ClearDice();
        activeDiceButtons.Clear();
        diceSpunCount = 0;
        finalResults.Clear();

        for (int i = 0; i < diceToRoll; i++)
        {
            GameObject diceGO = Instantiate(dicePrefab, diceContainer);
            Button diceBtn = diceGO.GetComponent<Button>();
            Image diceImg = diceGO.GetComponent<Image>();
            
            if (diceFaces != null && diceFaces.Length >= 6) diceImg.sprite = diceFaces[0]; 
            
            activeDiceButtons.Add(diceBtn);
            diceBtn.onClick.AddListener(() => OnDiceClicked(diceBtn, diceImg));
            diceBtn.interactable = isManualSpin;
        }

        if (isManualSpin) resultText.text = diceToRoll == 1 ? "Click the die to roll!" : "Click each die to roll!";
        else { resultText.text = ""; StartCoroutine(AutoRollAllDice()); }
    }

    private void OnDiceClicked(Button btn, Image img)
    {
        btn.interactable = false;
        if (isManualSpin) resultText.text = "";
        StartCoroutine(SpinDiceAnimation(img, OnSingleDiceLanded));
    }

    private IEnumerator AutoRollAllDice()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (var btn in activeDiceButtons)
        {
            Image img = btn.GetComponent<Image>();
            StartCoroutine(SpinDiceAnimation(img, OnSingleDiceLanded));
            yield return new WaitForSeconds(0.2f); // Cascata de dados
        }
    }

    private IEnumerator SpinDiceAnimation(Image diceImg, Action<int> onLanded)
    {
        RectTransform rect = diceImg.GetComponent<RectTransform>();
        Vector2 startPos = rect.anchoredPosition;
        Vector3 startScale = rect.localScale;
        
        float elapsed = 0f;
        int lastFace = 0;
        
        // EFEITO PSEUDO-3D E PARÁBOLA
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / spinDuration;
            
            // Pulo
            float currentY = startPos.y + Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            rect.anchoredPosition = new Vector2(startPos.x, currentY);

            // Rotação falsa no eixo X para dar a sensação de capotar
            float scaleMod = Mathf.Cos(percent * Mathf.PI * 10f); // Oscila rapidamente
            rect.localScale = new Vector3(startScale.x, startScale.y * Mathf.Abs(scaleMod), startScale.z);

            // Troca o Sprite (Blur ou números rápidos)
            if (elapsed % spinSpeed < (spinSpeed / 2))
            {
                if (diceBlurs != null && diceBlurs.Length > 0) diceImg.sprite = diceBlurs[UnityEngine.Random.Range(0, diceBlurs.Length)];
                else
                {
                    int rFace = UnityEngine.Random.Range(0, 6);
                    if (rFace != lastFace && diceFaces != null && diceFaces.Length >= 6) { diceImg.sprite = diceFaces[rFace]; lastFace = rFace; }
                }
            }

            yield return null;
        }

        // Pouso - Restaura Posição e Escala originais
        rect.anchoredPosition = startPos;
        rect.localScale = startScale;

        // Sorteia o resultado final
        int result = forceSixMode ? 6 : UnityEngine.Random.Range(1, 7);
        if (diceFaces != null && diceFaces.Length >= 6) diceImg.sprite = diceFaces[result - 1];
        
        onLanded?.Invoke(result);
    }

    private void OnSingleDiceLanded(int result)
    {
        diceSpunCount++;
        finalResults.Add(result);

        if (diceSpunCount >= diceToRoll)
        {
            // Fim do minigame, valida o contexto
            if (playerGuess != -1)
            {
                bool won = finalResults.Contains(playerGuess);
                resultText.text = won ? "Success! You guessed right!" : $"Failed! You guessed {playerGuess}.";
            }
            else if (diceToRoll > 1)
            {
                int sum = 0; foreach (int r in finalResults) sum += r;
                resultText.text = $"Total Sum: {sum}";
            }
            else { resultText.text = $"Result: {result}"; }

            ShowFinalAction("Continue");
        }
    }

    private void ShowFinalAction(string buttonText)
    {
        actionPanel.SetActive(true);
        btnActionText.text = buttonText;
        btnAction.onClick.RemoveAllListeners();
        btnAction.onClick.AddListener(() => { gameObject.SetActive(false); onCompleteCallback?.Invoke(finalResults); });
    }

    private void ClearDice() { foreach (Transform child in diceContainer) Destroy(child.gameObject); }
}