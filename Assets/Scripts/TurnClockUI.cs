using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Painel central gigante que aparece na tela para animar a passagem do tempo de uma carta.
/// </summary>
public class TurnClockUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI cardNameText; // Ex: "Swords of Revealing Light"
    public TextMeshProUGUI turnsLeftText; // Ex: "2 Turns Left"
    
    [Tooltip("A imagem da 'fatia de pizza' (Image Type: Filled, Radial 360).")]
    public Image clockFillImage;
    [Tooltip("O ponteiro do relógio.")]
    public RectTransform clockHand;

    [Header("Theme Images (Opcional)")]
    public Image clockBaseImage; // Fundo estático do relógio
    public Image clockHandImage; // O sprite do ponteiro

    [Header("Cores")]
    public Color startColor = Color.cyan;
    public Color warningColor = Color.red;

    [Header("Animação")]
    public float animationDuration = 0.5f; // Tempo que o ponteiro leva girando
    public float displayTime = 1.5f;       // Tempo que o relógio fica na tela antes de sumir

    private Action onAnimationComplete;
    private bool useFillEffect = true;

    void Awake()
    {
        // Garante que comece invisível
        gameObject.SetActive(false);
        
        // Força o relógio a aparecer na frente de todas as cartas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;
        
        // Tenta auto-atribuir as imagens baseadas na sua hierarquia
        if (clockBaseImage == null) 
        {
            Transform baseTr = transform.Find("BaseClock");
            if (baseTr != null) clockBaseImage = baseTr.GetComponent<Image>();
        }
        
        if (clockFillImage == null)
        {
            Transform fillTr = transform.Find("BaseClock/Fill");
            if (fillTr != null) clockFillImage = fillTr.GetComponent<Image>();
        }
        
        if (clockHand == null)
        {
            Transform handTr = transform.Find("BaseClock/Hand");
            if (handTr != null) clockHand = handTr.GetComponent<RectTransform>();
        }
        
        if (clockHandImage == null && clockHand != null) clockHandImage = clockHand.GetComponent<Image>();
    }

    public void ApplyThemeSettings(Vector2 handCenter, bool useFill, Vector2 baseSize, Vector2 handSize, bool preserveAspect, Vector2 handPivot)
    {
        useFillEffect = useFill;
        if (clockHand != null) 
        {
            // Força a âncora para o meio exato do relógio. Assim o X e Y funcionam como coordenadas reais a partir do centro!
            clockHand.anchorMin = new Vector2(0.5f, 0.5f);
            clockHand.anchorMax = new Vector2(0.5f, 0.5f);
            clockHand.pivot = handPivot;
            clockHand.anchoredPosition = handCenter;
        }
        
        if (clockFillImage != null)
            clockFillImage.gameObject.SetActive(useFill);

        if (clockBaseImage != null)
        {
            clockBaseImage.preserveAspect = preserveAspect;
            if (baseSize != Vector2.zero) clockBaseImage.rectTransform.sizeDelta = baseSize;
        }

        if (clockFillImage != null)
        {
            clockFillImage.preserveAspect = preserveAspect;
            if (baseSize != Vector2.zero) clockFillImage.rectTransform.sizeDelta = baseSize;
        }

        if (clockHandImage != null)
        {
            clockHandImage.preserveAspect = preserveAspect;
            if (handSize != Vector2.zero) clockHandImage.rectTransform.sizeDelta = handSize;
        }
    }

    /// <summary>
    /// Chama o relógio gigante na tela, anima o ponteiro caindo 1 turno, e depois fecha.
    /// </summary>
    public void AnimateTick(string cardName, int oldTurns, int newTurns, int maxTurns, Action onComplete)
    {
        gameObject.SetActive(true);
        onAnimationComplete = onComplete;

        // Puxa o tema dinamicamente para blindar o sistema de testes!
        if (DuelThemeManager.Instance != null && DuelThemeManager.Instance.currentTheme != null)
        {
            DuelTheme theme = DuelThemeManager.Instance.currentTheme;
            if (clockBaseImage != null && theme.clockBaseSprite != null) clockBaseImage.sprite = theme.clockBaseSprite;
            if (clockFillImage != null && theme.clockBaseSprite != null) clockFillImage.sprite = theme.clockBaseSprite;
            if (clockHandImage != null && theme.clockHandSprite != null) clockHandImage.sprite = theme.clockHandSprite;
            ApplyThemeSettings(theme.clockHandCenter, theme.useClockFillEffect, theme.clockBaseSize, theme.clockHandSize, theme.preserveClockAspect, theme.clockHandPivot);
        }

        if (cardNameText != null) cardNameText.text = cardName;
        if (turnsLeftText != null) turnsLeftText.text = $"{newTurns} Turn(s) Left";

        StartCoroutine(TickCoroutine(oldTurns, newTurns, maxTurns));
    }

    private IEnumerator TickCoroutine(int oldTurns, int newTurns, int maxTurns)
    {
        // Se maxTurns for 0 (erro), previne divisão por zero
        if (maxTurns <= 0) maxTurns = 1;

        // Calcula as porcentagens (0.0 a 1.0)
        float oldFill = (float)oldTurns / maxTurns;
        float newFill = (float)newTurns / maxTurns;

        // Define a cor baseada na urgência (se for o último turno, fica vermelho)
        if (clockFillImage != null && useFillEffect)
        {
            clockFillImage.color = (newTurns <= 1) ? warningColor : startColor;
        }

        // Animação do ponteiro e da fatia de pizza
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Usa curva de aceleração (Ease Out) para dar um efeito de ponteiro pesado
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); 

            float currentFill = Mathf.Lerp(oldFill, newFill, smoothT);

            if (clockFillImage != null && useFillEffect) clockFillImage.fillAmount = currentFill;
            
            if (clockHand != null)
            {
                // Multiplica por -360 porque o eixo Z na Unity gira anti-horário
                float rotationZ = currentFill * -360f;
                clockHand.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            yield return null;
        }

        // Garante os valores finais exatos
        if (clockFillImage != null && useFillEffect) clockFillImage.fillAmount = newFill;
        if (clockHand != null) clockHand.localRotation = Quaternion.Euler(0f, 0f, newFill * -360f);

        // Toca um som de relógio aqui se quiser! (Ex: AudioManager.Play("Tick"))

        // Deixa o jogador ler a tela por um tempinho
        yield return new WaitForSeconds(displayTime);

        // Fecha e devolve o controle para o jogo
        gameObject.SetActive(false);
        onAnimationComplete?.Invoke();
    }
}
