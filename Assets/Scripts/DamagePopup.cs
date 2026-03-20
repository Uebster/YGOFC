using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DamagePopup : MonoBehaviour
{
    [Header("Animação")]
    public float moveSpeedY = 50f;
    public float lifetime = 1.5f;
    public float fadeSpeed = 2f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private HorizontalLayoutGroup layoutGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 2f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
        }

        // Previne que o popup empurre cartas ou desorganize painéis se spawnar dentro de um Layout
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        // Pega o RectTransform por último, pois adicionar os componentes de UI acima força a Unity
        // a converter um Transform comum em RectTransform caso o prefab tenha sido criado vazio.
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(int amount, bool isHeal, DamagePopupManager manager)
    {
        string amountStr = amount.ToString();
        // Trava de segurança (cap em 99999)
        if (amount > 99999) amountStr = "99999";

        if (manager.useSpritesForNumbers)
        {
            // Cria os elementos usando Imagens
            Sprite[] sourceArray = isHeal ? manager.healSprites : manager.damageSprites;

            // 1. Instancia o operador (+ ou -), que é o índice 10
            if (sourceArray.Length >= 11 && sourceArray[10] != null)
            {
                CreateSpriteElement(sourceArray[10], manager.spriteSize);
            }

            // 2. Instancia os dígitos
            foreach (char c in amountStr)
            {
                int digit = int.Parse(c.ToString());
                if (sourceArray.Length > digit && sourceArray[digit] != null)
                {
                    CreateSpriteElement(sourceArray[digit], manager.spriteSize);
                }
            }
        }
        else
        {
            // Cria o elemento usando TextMeshPro (Fallback)
            GameObject textObj = new GameObject("TextValue");
            textObj.transform.SetParent(transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();

            txt.text = (isHeal ? "+" : "-") + amountStr;
            txt.color = isHeal ? manager.textHealColor : manager.textDamageColor;
            txt.fontSize = manager.fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontStyle = FontStyles.Bold;
            if (manager.customFont != null) txt.font = manager.customFont;

            // Previne que a fonte quebre de linha caso não caiba na largura e fique com aspecto estranho
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Overflow;
            
            // Adiciona Outline pra ficar bonito
            txt.outlineWidth = 0.2f;
            txt.outlineColor = Color.black;
        }

        // Inicia animação e auto-destruição
        StartCoroutine(AnimateAndDestroy());
    }

    private void CreateSpriteElement(Sprite spr, Vector2 size)
    {
        GameObject imgObj = new GameObject("Digit");
        imgObj.transform.SetParent(transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.sprite = spr;
        img.preserveAspect = true;
        
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
    }

    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0f;
        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            
            // Flutua para cima
            rectTransform.anchoredPosition += new Vector2(0, moveSpeedY * Time.deltaTime);

            // Inicia o Fade out perto do final da vida útil
            if (timer > lifetime / 2f)
            {
                canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
