using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;

public class ExodiaWinUI : MonoBehaviour
{
    public static ExodiaWinUI Instance;

    [Header("Hierarquia da UI (Arraste do Inspector)")]
    public GameObject panelExodiaWin;
    public RectTransform exodiaHead;
    public RectTransform exodiaLeftArm;
    public RectTransform exodiaRightArm;
    public RectTransform exodiaLeftLeg;
    public RectTransform exodiaRightLeg;
    public RectTransform spawnPoint;

    [Header("Efeitos Visuais")]
    public GameObject flashPrefab; // Opcional: Prefab do clarão
    public float animDuration = 0.5f; // Tempo de voo de cada peça

    private List<GameObject> spawnedCards = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        if (panelExodiaWin == null) panelExodiaWin = this.gameObject;
        panelExodiaWin.SetActive(false);
        
        // Remove alphas antigos das Imagens placeholders se houver
        SetAlpha(exodiaHead, 0);
        SetAlpha(exodiaLeftArm, 0);
        SetAlpha(exodiaRightArm, 0);
        SetAlpha(exodiaLeftLeg, 0);
        SetAlpha(exodiaRightLeg, 0);
    }

    private void SetAlpha(RectTransform rt, float alpha)
    {
        if (rt == null) return;
        Image img = rt.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    public void ShowWinSequence(bool isPlayer, Action onComplete)
    {
        gameObject.SetActive(true); // Garante que a raiz do script acorde para a Corrotina
        if (panelExodiaWin != null) panelExodiaWin.SetActive(true);
        
        // Trava as interações da mão do jogador durante a animação
        if (GameManager.Instance != null && GameManager.Instance.playerHandCanvasGroup != null)
            GameManager.Instance.playerHandCanvasGroup.interactable = false;

        StartCoroutine(ExodiaRoutine(onComplete));
    }

    private IEnumerator ExodiaRoutine(Action onComplete)
    {
        // Limpa resquícios de animações anteriores
        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();

        // A ordem clássica de revelação das peças do Exodia
        RectTransform[] parts = { 
            exodiaRightLeg, 
            exodiaLeftLeg, 
            exodiaRightArm, 
            exodiaLeftArm, 
            exodiaHead 
        };
        
        string[] cardNames = { 
            "Right Leg of the Forbidden One", 
            "Left Leg of the Forbidden One", 
            "Right Arm of the Forbidden One", 
            "Left Arm of the Forbidden One", 
            "Exodia the Forbidden One" 
        };
        
        GameObject cardPrefab = GameManager.Instance?.cardPrefab;
        
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null || cardPrefab == null) continue;
            
            CardData cardData = GameManager.Instance.cardDatabase.cardDatabase.Find(c => c.name == cardNames[i]);
            if (cardData != null) yield return StartCoroutine(AnimateRealCard(parts[i], cardData, cardPrefab));
            yield return new WaitForSeconds(0.15f);
        }

        // OBLITERATE! (Invoca o Clarão Final)
        yield return new WaitForSeconds(0.5f);
        if (flashPrefab != null) Instantiate(flashPrefab, panelExodiaWin.transform);
        
        // Treme a câmera e aguarda exatamente os 2.0s de duração do clarão para dar o veredito
        yield return StartCoroutine(ScreenShake(2.0f, 10f));

        // Limpa a sujeira
        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();
        
        if (panelExodiaWin != null) panelExodiaWin.SetActive(false);

        // Informa ao GameManager para finalizar a partida matematicamente
        onComplete?.Invoke();
    }

    private IEnumerator AnimateRealCard(RectTransform targetPart, CardData cardData, GameObject cardPrefab)
    {
        Transform startTransform = spawnPoint != null ? spawnPoint : targetPart;
        
        // Instancia a carta real do seu jogo
        GameObject cardGO = Instantiate(cardPrefab, startTransform.position, Quaternion.identity, targetPart);
        spawnedCards.Add(cardGO);

        // Iguala o tamanho da carta ao tamanho exato do espaço vazio (target) do Exodia
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = targetPart.sizeDelta; 
        }

        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        if (display != null)
        {
            LayoutElement le = cardGO.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);

            display.SetCard(cardData, GameManager.Instance.GetCardBackTexture(), true);
            display.isInteractable = false; 
        }

        float t = 0;
        Vector3 startPos = startTransform.position;
        Vector3 endPos = targetPart.position;
        
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one; 

        while (t < 1)
        {
            t += Time.deltaTime / animDuration;
            float easedT = Mathf.SmoothStep(0, 1, t);

            cardGO.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            cardGO.transform.localScale = Vector3.Lerp(startScale, endScale, easedT);

            yield return null;
        }

        cardGO.transform.position = endPos;
        cardGO.transform.localRotation = Quaternion.identity;
        cardGO.transform.localScale = endScale;

        Image targetImg = targetPart.GetComponent<Image>();
        if (targetImg != null)
        {
            Color originalColor = targetImg.color;
            targetImg.color = new Color(2f, 2f, 2f, 1f); // Estoura o branco
            yield return new WaitForSeconds(0.05f);
            targetImg.color = originalColor;
        }
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = originalPos.x + UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = originalPos.y + UnityEngine.Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = originalPos;
    }
}