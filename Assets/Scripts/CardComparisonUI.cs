// Assets/Scripts/CardComparisonUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CardComparisonUI : MonoBehaviour
{
    public static CardComparisonUI Instance;

    [Header("Geral")]
    public TextMeshProUGUI comparisonText;
    public TextMeshProUGUI vsText; // Opcional: Crie um texto "VS" no meio da tela e arraste aqui!

    [Header("Jogador")]
    public RawImage playerCardImage; // Troque o componente Image por RawImage na Unity!
    public Outline playerOutline;
    public TextMeshProUGUI playerStatsText;
    public GameObject playerWinLoseStamp; // O WinorLoseImgPlayer
    public TextMeshProUGUI playerWinLoseText;

    [Header("Oponente")]
    public RawImage opponentCardImage; // Troque o componente Image por RawImage na Unity!
    public Outline opponentOutline;
    public TextMeshProUGUI opponentStatsText;
    public GameObject opponentWinLoseStamp; // O WinorLoseImgOpponent
    public TextMeshProUGUI opponentWinLoseText;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    public void ShowVersus(CardDisplay pCard, CardDisplay oCard, string title, string pStatStr, string oStatStr, int pVal, int oVal, System.Action onComplete, bool higherWins = true)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        comparisonText.text = title;
        if (vsText != null) vsText.gameObject.SetActive(false); // Esconde o VS no início

        // Aplica as cores do GameManager nos Outlines
        if (GameManager.Instance != null && playerOutline != null && opponentOutline != null)
        {
            playerOutline.effectColor = GameManager.Instance.playerHoverColor;
            opponentOutline.effectColor = GameManager.Instance.opponentHoverColor;
        }

        // Esconde os elementos antes da animação
        playerCardImage.color = Color.clear;
        opponentCardImage.color = Color.clear;
        
        // Previne a "sombra fantasma" limpando a textura do duelo anterior
        playerCardImage.texture = null;
        opponentCardImage.texture = null;
        
        playerStatsText.text = "";
        opponentStatsText.text = "";
        playerWinLoseStamp.SetActive(false);
        opponentWinLoseStamp.SetActive(false);
        
        // Desliga o Outline enquanto voa
        if (playerOutline != null) playerOutline.enabled = false;
        if (opponentOutline != null) opponentOutline.enabled = false;

        StartCoroutine(CinematicRoutine(pCard, oCard, pStatStr, oStatStr, pVal, oVal, onComplete, higherWins));
    }

    private IEnumerator CinematicRoutine(CardDisplay pCard, CardDisplay oCard, string pStatStr, string oStatStr, int pVal, int oVal, System.Action onComplete, bool higherWins)
    {
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.spellSound);

        // 1. ANIMAÇÃO DE VOO (Das mãos para o Painel)
        Vector3 pStart = GameManager.Instance != null ? GameManager.Instance.playerHandLayoutGroup.position : Vector3.zero;
        Vector3 oStart = GameManager.Instance != null ? GameManager.Instance.opponentHandLayoutGroup.position : Vector3.zero;

        StartCoroutine(FlyAndFlipGhost(pStart, playerCardImage.rectTransform, pCard));
        StartCoroutine(FlyAndFlipGhost(oStart, opponentCardImage.rectTransform, oCard));

        yield return new WaitForSeconds(0.6f); // Espera o voo terminar

        // Revela as cartas no painel
        playerCardImage.texture = pCard.GetFrontTexture() ?? pCard.cardImage.texture;
        opponentCardImage.texture = oCard.GetFrontTexture() ?? oCard.cardImage.texture;
        playerCardImage.color = Color.white;
        opponentCardImage.color = Color.white;

        // Acende o Outline (Hover) apenas após as cartas pousarem
        if (playerOutline != null) playerOutline.enabled = true;
        if (opponentOutline != null) opponentOutline.enabled = true;

        // 2. TEXTO VS E STATUS (Tensão)
        if (vsText != null)
        {
            vsText.gameObject.SetActive(true);
            StartCoroutine(SlamTextRoutine(vsText.rectTransform));
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackTravelSound);
        }

        yield return new WaitForSeconds(0.5f);

        playerStatsText.text = pStatStr;
        opponentStatsText.text = oStatStr;
        StartCoroutine(SlamTextRoutine(playerStatsText.rectTransform));
        StartCoroutine(SlamTextRoutine(opponentStatsText.rectTransform));
        
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.defenseSound);

        yield return new WaitForSeconds(1.0f); // Pausa dramática lendo os números

        // 3. JULGAMENTO (Win, Lose, Draw)
        bool pWin = higherWins ? (pVal > oVal) : (pVal < oVal);
        bool oWin = higherWins ? (oVal > pVal) : (oVal < pVal);
        bool isDraw = pVal == oVal;

        ApplyStamp(playerWinLoseStamp, playerWinLoseText, isDraw ? "DRAW" : (pWin ? "WIN" : "LOSE"), isDraw ? Color.yellow : (pWin ? Color.green : Color.red));
        ApplyStamp(opponentWinLoseStamp, opponentWinLoseText, isDraw ? "DRAW" : (oWin ? "WIN" : "LOSE"), isDraw ? Color.yellow : (oWin ? Color.green : Color.red));

        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackImpactSound);

        yield return new WaitForSeconds(0.5f);

        // 4. EXPLOSÃO NO PERDEDOR E LIMPEZA
        // Desliga os outlines para a explosão ficar limpa
        if (playerOutline != null) playerOutline.enabled = false;
        if (opponentOutline != null) opponentOutline.enabled = false;

        // Esconde os carimbos de WIN/LOSE exatamente no instante em que a carta explode/evapora
        if (playerWinLoseStamp != null) playerWinLoseStamp.SetActive(false);
        if (opponentWinLoseStamp != null) opponentWinLoseStamp.SetActive(false);

        if (DuelFXManager.Instance != null && DuelFXManager.Instance.explosionVFX != null)
        {
            if (!pWin) // Se empatou, os dois explodem! Se perdeu, só ele explode.
            {
                DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.explosionVFX, playerCardImage.transform.position);
                playerCardImage.color = Color.clear; // Evapora a carta (Deixa o espaço vazio)
            }
            if (!oWin)
            {
                DuelFXManager.Instance.SpawnVFXPublic(DuelFXManager.Instance.explosionVFX, opponentCardImage.transform.position);
                opponentCardImage.color = Color.clear; // Evapora a carta
            }
            if (!pWin || !oWin) DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.destroySound);
        }

        yield return new WaitForSeconds(0.5f); // Pausa dramática encurtada (fecha a janela logo após a explosão começar)

        // 5. FADE OUT E FIM
        float fadeT = 0;
        while (fadeT < 1f)
        {
            fadeT += Time.deltaTime / 0.3f; // Fade Out acelerado
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            yield return null;
        }

        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator FlyAndFlipGhost(Vector3 startPos, RectTransform targetRT, CardDisplay realCard)
    {
        CardFlightSettings settings = DuelFXManager.Instance != null ? DuelFXManager.Instance.flightCardComparisonUI : new CardFlightSettings();
        
        GameObject ghost = new GameObject("VersusGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(transform, false);
        ghost.transform.SetAsLastSibling();

        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = startPos;
        rt.sizeDelta = targetRT.sizeDelta;
        rt.pivot = targetRT.pivot;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = GameManager.Instance != null ? GameManager.Instance.GetCardBackTexture() : null;

        float duration = settings.duration > 0 ? settings.duration : 0.6f;
        float t = 0;
        bool flipped = false;
        
        Vector3 baseStartScale = rt.localScale * settings.startScaleMult;
        Vector3 baseEndScale = rt.localScale * settings.endScaleMult;
        
        GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
        if (settings.useTrail && settings.trailType == AttackTrailType.ContinuousLine) {
            lineObj = new GameObject("FlightLineTrail", typeof(RectTransform), typeof(Image));
            lineObj.transform.SetParent(ghost.transform.parent, false);
            lineObj.transform.SetSiblingIndex(ghost.transform.GetSiblingIndex());
            lineRT = lineObj.GetComponent<RectTransform>();
            lineRT.pivot = new Vector2(0, 0.5f); lineRT.position = startPos;
            lineImg = lineObj.GetComponent<Image>(); lineImg.color = settings.trailColor;
        }
        float spawnTrailTimer = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);

            // Arco parabólico
            Vector3 currentPos = Vector3.Lerp(startPos, targetRT.position, smooth);
            currentPos.y += Mathf.Sin(smooth * Mathf.PI) * 100f; 
            rt.position = currentPos;

            // Aplica Parábola de Voo + Multiplicadores de Início/Fim
            float scaleMultiplier = 1f + Mathf.Sin(smooth * Mathf.PI) * (settings.flightScale - 1f);
            Vector3 currentBaseScale = Vector3.Lerp(baseStartScale, baseEndScale, smooth) * scaleMultiplier;
            
            // Rotação de Flip no meio do voo
            if (t >= 0.4f && t <= 0.6f)
            {
                float flipP = (t - 0.4f) / 0.2f;
                float scaleX = Mathf.Cos(flipP * Mathf.PI);
                rt.localScale = new Vector3(currentBaseScale.x * Mathf.Abs(scaleX), currentBaseScale.y, currentBaseScale.z);

                if (flipP >= 0.5f && !flipped)
                {
                    flipped = true;
                    ri.texture = realCard.GetFrontTexture() ?? realCard.cardImage.texture;
                }
            }
            else
            {
                rt.localScale = currentBaseScale;
            }
            
            if (settings.useTrail) {
                if (settings.trailType == AttackTrailType.Shadows || settings.trailType == AttackTrailType.SmoothShadows) {
                    spawnTrailTimer -= Time.deltaTime; 
                    float interval = settings.trailType == AttackTrailType.SmoothShadows ? 0.015f : 0.04f;
                    if (spawnTrailTimer <= 0) { 
                        spawnTrailTimer = interval; 
                        SpawnTrailGhost(rt, ri.texture, settings.trailColor, settings.trailType == AttackTrailType.SmoothShadows ? 0.15f : 0.3f); 
                    }
                } else if (settings.trailType == AttackTrailType.ContinuousLine && lineObj != null) {
                    Vector3 dirToCurrent = currentPos - startPos;
                    float dist = dirToCurrent.magnitude; float canvasScale = lineObj.transform.lossyScale.x;
                    if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, settings.trailWidth);
                    lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                }
            }

            yield return null;
        }

        if (lineObj != null) Destroy(lineObj);
        Destroy(ghost);
    }
    
    private void SpawnTrailGhost(RectTransform sourceRT, Texture tex, Color color, float duration)
    {
        GameObject ghost = new GameObject("CardTrailGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(sourceRT.parent, false);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        rt.pivot = sourceRT.pivot;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = tex; ri.color = color;
        
        Color startColor = ri.color;
        Vector3 startScale = ghost.transform.localScale;
        StartCoroutine(FadeAndDestroy(ghost, ri, startColor, startScale, duration));
    }

    private IEnumerator FadeAndDestroy(GameObject obj, RawImage img, Color startColor, Vector3 startScale, float duration)
    {
        float t = 0;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator SlamTextRoutine(RectTransform rt)
    {
        float t = 0;
        Vector3 endScale = Vector3.one;
        Vector3 startScale = Vector3.one * 3f;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.2f;
            rt.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        rt.localScale = endScale;
    }

    private void ApplyStamp(GameObject stampObj, TextMeshProUGUI textObj, string text, Color color)
    {
        stampObj.SetActive(true);
        textObj.text = text;
        textObj.color = color;
        stampObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        StartCoroutine(SlamTextRoutine(stampObj.GetComponent<RectTransform>()));
    }
}
