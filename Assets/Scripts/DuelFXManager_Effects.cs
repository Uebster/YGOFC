using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DuelFXManager
{
    // ==============================================================================
    // EFFECTS
    // ==============================================================================
 
    // --- ÍCONE DE SELEÇÃO E TRIBUTO ---
    private Dictionary<CardDisplay, Dictionary<HighlightCategory, GameObject>> activeSelectionIcons = new Dictionary<CardDisplay, Dictionary<HighlightCategory, GameObject>>();

    public void SetSelectionIcon(CardDisplay card, SummonVFXType type, SelectionState state)
    {
        if (card == null || !enableAnimations) return;

        SummonVFXPackage package = GetSummonPackage(type);
        if (package == null || !package.selectionIcon.useIcon) return;
        SelectionIconSettings settings = package.selectionIcon;

        HighlightCategory cat = HighlightCategory.GenericTarget;
        if (type == SummonVFXType.Tribute) cat = HighlightCategory.Tribute;
        else if (type == SummonVFXType.Fusion) cat = HighlightCategory.Fusion;
        else if (type == SummonVFXType.Ritual) cat = HighlightCategory.Ritual;
        else if (type == SummonVFXType.Special) cat = HighlightCategory.SpecialSummon;

        ApplySelectionIcon(card, cat, settings, state);
    }

    public void SetSelectionIcon(CardDisplay card, HighlightCategory category, SelectionState state)
    {
        if (card == null || !enableAnimations) return;
        SelectionIconSettings settings = GetIconSettings(category);
        if (settings == null || !settings.useIcon) return;

        ApplySelectionIcon(card, category, settings, state);
    }

    private void ApplySelectionIcon(CardDisplay card, HighlightCategory category, SelectionIconSettings settings, SelectionState state)
    {
        if (!activeSelectionIcons.ContainsKey(card))
            activeSelectionIcons[card] = new Dictionary<HighlightCategory, GameObject>();

        if (activeSelectionIcons[card].TryGetValue(category, out GameObject existingIcon))
        {
            if (existingIcon != null) Destroy(existingIcon);
            activeSelectionIcons[card].Remove(category);
        }

        if (state == SelectionState.None) return;
        if (state == SelectionState.Available && !settings.showAvailableState) return;

        GameObject newIcon = null;

        if (settings.usePrefab && settings.prefab != null)
        {
            GameObject prefabInstance = SpawnVFXPublic(settings.prefab, card.transform.position);
            if (prefabInstance != null) {
                prefabInstance.transform.SetParent(card.transform, true);
                newIcon = prefabInstance;
            }
        }
        else if (settings.useNative && settings.sprite != null)
        {
            GameObject iconObj = new GameObject("SelectionIcon_" + category.ToString(), typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(card.transform, false);
            iconObj.transform.SetAsLastSibling();
            
            RectTransform rt = iconObj.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            
            Image img = iconObj.GetComponent<Image>();
            img.sprite = settings.sprite;
            if (settings.material != null) img.material = settings.material;

            newIcon = iconObj;

            if (state == SelectionState.Available)
            {
                rt.sizeDelta = new Vector2(settings.size, settings.size);
                img.color = settings.availableColor;

                if (settings.useOutline)
                {
                    Outline outline = iconObj.AddComponent<Outline>();
                    outline.effectColor = settings.outlineColor;
                    outline.effectDistance = new Vector2(settings.outlineWidth, -settings.outlineWidth);
                }

                StartCoroutine(AnimateSelectionIcon(iconObj, img, rt, settings, true));
            }
            else if (state == SelectionState.Selected)
            {
                rt.sizeDelta = new Vector2(settings.size, settings.size);
                img.color = settings.selectedColor;
                
                Outline outline = iconObj.AddComponent<Outline>();
                outline.effectColor = settings.useOutline ? settings.outlineColor : settings.selectedColor;
                outline.effectDistance = new Vector2(settings.selectedOutlineWidth, -settings.selectedOutlineWidth);
                
                StartCoroutine(AnimateSelectionIcon(iconObj, img, rt, settings, false));
            }
        }

        if (newIcon != null)
        {
            activeSelectionIcons[card][category] = newIcon;
        }
    }

    private IEnumerator AnimateSelectionIcon(GameObject iconObj, Image img, RectTransform rt, SelectionIconSettings settings, bool isAvailableState)
    {
        float t = 0;
        Color baseColor = img.color;
        Outline outline = iconObj.GetComponent<Outline>();
        Color baseOutlineColor = outline != null ? outline.effectColor : Color.clear;
        Vector3 baseScale = rt.localScale;

        // Determina quais animações usar baseado no estado (Disponível vs Selecionado)
        bool shouldBlink = isAvailableState ? settings.blinkAvailable : settings.blinkSelected;
        bool shouldPulse = isAvailableState ? settings.pulseAvailable : settings.pulseSelected;
        bool shouldSpin = isAvailableState ? settings.spinAvailable : settings.spinSelected;

        while (iconObj != null)
        {
            t += Time.deltaTime;

            // Lógica de Piscar (Blink)
            if (shouldBlink)
            {
                float alpha = Mathf.Abs(Mathf.Sin(t * Mathf.PI * settings.blinkFrequency));
                Color c = baseColor; c.a = alpha * baseColor.a;
                img.color = c;
                if (outline != null) {
                    Color oc = baseOutlineColor; oc.a = alpha * baseOutlineColor.a;
                    outline.effectColor = oc;
                }
            }

            // Lógica de Pulsar (Pulse)
            if (shouldPulse)
            {
                float scaleMultiplier = 1f + (Mathf.Sin(t * Mathf.PI * settings.blinkFrequency) + 1f) / 2f * (settings.pulseScale - 1f);
                rt.localScale = baseScale * scaleMultiplier;
            }

            // Lógica de Girar (Spin)
            if (shouldSpin) rt.Rotate(0, 0, settings.spinSpeed * Time.deltaTime);

            yield return null;
        }
    }

    // --- ATIVAÇÃO DE MAGIA / ARMADILHA ---

    public void PlayCardActivation(CardDisplay card, bool isTrap, System.Action onComplete = null)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayCardActivation | Alvo: {card?.CurrentCardData?.name}");
        if (!enableAnimations)
        {
            onComplete?.Invoke();
            return;
        }
        
        bool isFieldSpell = card != null && card.CurrentCardData != null && card.CurrentCardData.property == "Field";
        StartCoroutine(AnimateActivationRoutine(card, isTrap, isFieldSpell, onComplete));
    }

    private IEnumerator AnimateActivationRoutine(CardDisplay card, bool isTrap, bool isFieldSpell, System.Action onComplete)
    {
        // Debug.Log($"[VFX] [ROUTINE INICIADA] AnimateActivationRoutine");
        PlaySound(isTrap ? trapSound : spellSound);

        // 1. Instancia a Partícula Clássica (Se a opção estiver marcada)
        if (useActivationPrefab)
        {
            GameObject vfxPrefab = isTrap ? trapActivateVFX : spellActivateVFX;
            SpawnVFXPublic(vfxPrefab, card.transform.position);
        }
        
        // 2. Executa o Pulse Dinâmico e a Cor do Field Spell
        if (useActivationPulse && card != null)
        {
            Vector3 originalScale = card.transform.localScale;
            if (GameManager.Instance != null)
            {
                originalScale = card.isOnField ? GameManager.Instance.fieldCardScale : GameManager.Instance.handCardScale;
            }
            Vector3 peakScale = originalScale * activationPulseScale;
            Transform originalParent = card.transform.parent;
            int originalIndex = card.transform.GetSiblingIndex();

            card.transform.SetAsLastSibling(); // Traz para frente na UI

            GameObject ghost = new GameObject("ActivationGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
            if (originalParent != null) {
                ghost.transform.SetParent(originalParent, false);
                ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 
            }

            RectTransform rt = ghost.GetComponent<RectTransform>();
            RectTransform cardRt = card.GetComponent<RectTransform>();
            rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
            rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
            rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

            RawImage ri = ghost.GetComponent<RawImage>();
            ri.texture = card.cardImage.texture; ri.color = new Color(1f, 1f, 1f, 0.8f);
            
            Outline outline = ghost.GetComponent<Outline>();
            outline.effectColor = isTrap ? colorTrap : colorSpell;
            outline.effectDistance = new Vector2(activationPulseOutlineWidth, -activationPulseOutlineWidth);

            float t = 0;
            float halfDuration = activationPulseDuration / (animationSpeed > 0 ? animationSpeed : 1f);

            // Fase 1: Subida
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime / halfDuration;
                float smooth = Mathf.SmoothStep(0, 1, t);
                card.transform.localScale = Vector3.Lerp(originalScale, peakScale, smooth);
                if (ghost != null)
                {
                    ghost.transform.localScale = Vector3.Lerp(originalScale, peakScale * 1.15f, smooth);
                    ri.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.8f, 0f, smooth)); 
                }
                yield return null;
            }

            if (ghost != null) Destroy(ghost);

            if (isFieldSpell && colorizeBoardByFieldSpell && boardBackgroundImage != null)
            {
                Color targetColor = GetFieldSpellColor(card.CurrentCardData.name);
                StartCoroutine(TransitionBoardColorRoutine(boardBackgroundImage.color, targetColor, 0.6f));
            }

            // Fase 2: Descida
            t = 0;
            while (t < 1f)
            {
                if (card == null) break;
                t += Time.deltaTime / halfDuration;
                float smooth = Mathf.SmoothStep(0, 1, t);
                card.transform.localScale = Vector3.Lerp(peakScale, originalScale, smooth);
                yield return null;
            }

            if (card != null)
            {
                card.transform.localScale = originalScale;
                if (originalParent != null) { card.transform.SetParent(originalParent, true); card.transform.SetSiblingIndex(originalIndex); }
            }
        }
        else
        {
            // Se o Pulse estiver desativado, processa a Field Spell imediatamente e dá um pequeno delay visual
            if (isFieldSpell && colorizeBoardByFieldSpell && boardBackgroundImage != null)
            {
                Color targetColor = GetFieldSpellColor(card.CurrentCardData.name);
                StartCoroutine(TransitionBoardColorRoutine(boardBackgroundImage.color, targetColor, 0.6f));
            }
            
            if (useActivationPrefab) 
                yield return new WaitForSeconds(0.6f / (animationSpeed > 0 ? animationSpeed : 1f));
        }

        // Debug.Log($"[VFX] [ROUTINE CONCLUÍDA] AnimateActivationRoutine.");
        onComplete?.Invoke();
    }

    private Color GetFieldSpellColor(string cardName)
    {
        cardName = cardName.ToLower();
        foreach (var theme in fieldSpellThemes)
        {
            foreach (var kw in theme.keywords) { 
                if (cardName.Contains(kw.ToLower())) return Color.Lerp(defaultBoardColor, theme.overlayColor, theme.intensity); 
            }
        }
        return defaultBoardColor;
    }
    
    private IEnumerator TransitionBoardColorRoutine(Color startColor, Color endColor, float duration)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            if (boardBackgroundImage != null)
                boardBackgroundImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }
    public void PlayFlipEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayFlipEffect");
        if (!enableAnimations || card == null) return;
        PlaySound(flipSound);
        if (useFlipPrefab && flipVFX != null) SpawnVFXPublic(flipVFX, card.transform.position);
        if (useFlipRoutine) StartCoroutine(FlipVFXRoutine(card));
    }

    private IEnumerator FlipVFXRoutine(CardDisplay card)
    {
        yield return new WaitForSeconds(flipVfxDuration / (animationSpeed > 0 ? animationSpeed : 1f));
        if (useFlipPulse && card != null) {
            StartCoroutine(PulseGhostRoutine(card, flipPulseScale, flipPulseDuration, flipPulseColor, flipPulseOutlineWidth));
        }
    }

    public void PlayChainLinkEffect(CardDisplay card, int linkNumber)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayChainLinkEffect");
        
        if (linkNumber <= 1 || !enableAnimations) return;

        PlaySound(card.CurrentCardData.type.Contains("Trap") ? trapSound : spellSound);
        
        if (useChainLinkPrefab && chainLinkVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(chainLinkVFX, card.transform.position);
            
            MonoBehaviour[] scripts = vfx.GetComponents<MonoBehaviour>();
            bool setupCalled = false;
            foreach(var script in scripts) {
                var method = script.GetType().GetMethod("Setup");
                if (method != null) { method.Invoke(script, new object[] { linkNumber }); setupCalled = true; break; }
            }
            if (!setupCalled)
            {
                TMPro.TextMeshProUGUI txt = vfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = $"Link {linkNumber}";
            }
        }

        if (useChainLinkRoutine && card != null)
        {
            StartCoroutine(ChainLinkRoutine(card, linkNumber));
        }
    }

    private IEnumerator ChainLinkRoutine(CardDisplay card, int linkNumber)
    {
        Transform uiParent = GetUIParent();
        if (uiParent == null) yield break;

        GameObject chainObj = new GameObject("ChainLink_Visual", typeof(RectTransform));
        chainObj.transform.SetParent(uiParent, false);
        
        RectTransform chainRT = chainObj.GetComponent<RectTransform>();
        chainRT.position = card.transform.position;
        chainObj.transform.SetAsLastSibling();

        GameObject textContainer = new GameObject("TextContainer", typeof(RectTransform));
        textContainer.transform.SetParent(chainObj.transform, false);
        RectTransform textContainerRT = textContainer.GetComponent<RectTransform>();
        textContainerRT.anchoredPosition = chainLinkTextOffset;
        textContainerRT.localScale = Vector3.zero;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textObj.transform.SetParent(textContainer.transform, false);
        TMPro.TextMeshProUGUI txt = textObj.GetComponent<TMPro.TextMeshProUGUI>();
        txt.text = $"Link {linkNumber}";
        txt.fontSize = 40;
        txt.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
        txt.color = chainLinkTextColor;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.overflowMode = TMPro.TextOverflowModes.Overflow;
        
        GameObject shadowObj = Instantiate(textObj, textContainer.transform);
        shadowObj.name = "Shadow";
        shadowObj.transform.SetAsFirstSibling();
        TMPro.TextMeshProUGUI shadowTxt = shadowObj.GetComponent<TMPro.TextMeshProUGUI>();
        shadowTxt.color = chainLinkShadowColor;
        shadowTxt.rectTransform.anchoredPosition = new Vector2(4, -4);

        Image leftImg = null, rightImg = null, joinedImg = null;
        RectTransform leftRT = null, rightRT = null;

        if (chainLinkLeftSprite != null)
        {
            GameObject leftObj = new GameObject("LeftLink", typeof(RectTransform), typeof(Image));
            leftObj.transform.SetParent(chainObj.transform, false);
            leftRT = leftObj.GetComponent<RectTransform>();
            leftRT.sizeDelta = chainLinkSideSize;
            leftRT.anchoredPosition = new Vector2(-100, 25);
            leftImg = leftObj.GetComponent<Image>();
            leftImg.sprite = chainLinkLeftSprite;
            leftImg.preserveAspect = chainLinkPreserveAspect;
            leftImg.color = chainLinkSpriteColor;
            if (chainLinkMaterial != null) leftImg.material = chainLinkMaterial;

            GameObject rightObj = new GameObject("RightLink", typeof(RectTransform), typeof(Image));
            rightObj.transform.SetParent(chainObj.transform, false);
            rightRT = rightObj.GetComponent<RectTransform>();
            rightRT.sizeDelta = chainLinkSideSize;
            rightRT.anchoredPosition = new Vector2(100, 25);
            rightImg = rightObj.GetComponent<Image>();
            
            if (chainLinkRightSprite != null) rightImg.sprite = chainLinkRightSprite;
            else { rightImg.sprite = chainLinkLeftSprite; rightRT.localScale = new Vector3(-1, 1, 1); }
            rightImg.preserveAspect = chainLinkPreserveAspect;
            rightImg.color = chainLinkSpriteColor;
            if (chainLinkMaterial != null) rightImg.material = chainLinkMaterial;

            if (chainLinkJoinedSprite != null)
            {
                GameObject joinedObj = new GameObject("JoinedLink", typeof(RectTransform), typeof(Image));
                joinedObj.transform.SetParent(chainObj.transform, false);
                RectTransform joinedRT = joinedObj.GetComponent<RectTransform>();
                joinedRT.sizeDelta = chainLinkJoinedSize;
                joinedRT.anchoredPosition = new Vector2(0, 25);
                joinedImg = joinedObj.GetComponent<Image>();
                joinedImg.sprite = chainLinkJoinedSprite;
                joinedImg.preserveAspect = chainLinkPreserveAspect;
                joinedImg.color = chainLinkSpriteColor;
                if (chainLinkMaterial != null) joinedImg.material = chainLinkMaterial;
                joinedImg.enabled = false;
            }
        }

        float duration = chainLinkDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        Vector2 startAnchored = chainRT.anchoredPosition + chainLinkBaseOffset;
        chainRT.anchoredPosition = startAnchored;
        Vector2 endAnchored = chainLinkSlideUp ? startAnchored + new Vector2(0, 120f) : startAnchored; 
        
        bool isClashed = false;
        GameObject flashObj = null; Image flashImg = null;

        while (t < 1f)
        {
            if (chainObj == null) break;
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            chainRT.anchoredPosition = Vector2.Lerp(startAnchored, endAnchored, smooth);
            
            if (t < 0.2f)
            {
                float clashT = t / 0.2f;
                if (leftRT != null) leftRT.anchoredPosition = Vector2.Lerp(new Vector2(-100, 25), new Vector2(-15, 25), clashT);
                if (rightRT != null) rightRT.anchoredPosition = Vector2.Lerp(new Vector2(100, 25), new Vector2(15, 25), clashT);
            }
            else if (!isClashed)
            {
                isClashed = true;
                if (leftImg != null) leftImg.enabled = false;
                if (rightImg != null) rightImg.enabled = false;
                if (joinedImg != null) joinedImg.enabled = true;

                if (chainLinkFlashOnImpact)
                {
                    flashObj = new GameObject("ClashFlash", typeof(RectTransform), typeof(Image));
                    flashObj.transform.SetParent(chainObj.transform, false);
                    RectTransform flashRT = flashObj.GetComponent<RectTransform>();
                    flashRT.anchoredPosition = new Vector2(0, 25);
                    flashRT.sizeDelta = chainLinkJoinedSize * 2f; 
                    flashImg = flashObj.GetComponent<Image>();
                    flashImg.color = chainLinkFlashColor;
                }
            }

            if (flashObj != null)
            {
                float flashT = (t - 0.2f) / 0.15f;
                if (flashT <= 1f) {
                    flashImg.color = new Color(chainLinkFlashColor.r, chainLinkFlashColor.g, chainLinkFlashColor.b, Mathf.Lerp(chainLinkFlashColor.a, 0f, flashT));
                    flashObj.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, flashT);
                } else if (flashObj.activeSelf) {
                    flashObj.SetActive(false);
                }
            }

            if (t >= 0.2f && t < 0.4f)
            {
                float popT = (t - 0.2f) / 0.2f;
                float overshootScale = 1f + Mathf.Sin(popT * Mathf.PI) * 0.3f;
                textContainerRT.localScale = Vector3.one * (chainLinkScale * overshootScale);
                if (joinedImg != null) joinedImg.rectTransform.localScale = Vector3.one * overshootScale;
            }
            else if (t >= 0.4f)
            {
                textContainerRT.localScale = Vector3.one * chainLinkScale;
                if (joinedImg != null) joinedImg.rectTransform.localScale = Vector3.one;
            }

            if (t > 0.7f)
            {
                float alpha = 1f - ((t - 0.7f) / 0.3f);
                txt.color = new Color(chainLinkTextColor.r, chainLinkTextColor.g, chainLinkTextColor.b, alpha);
                shadowTxt.color = new Color(chainLinkShadowColor.r, chainLinkShadowColor.g, chainLinkShadowColor.b, alpha);
                if (joinedImg != null) joinedImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
                if (leftImg != null) leftImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
                if (rightImg != null) rightImg.color = new Color(chainLinkSpriteColor.r, chainLinkSpriteColor.g, chainLinkSpriteColor.b, alpha);
            }

            yield return null;
        }

        if (chainObj != null) Destroy(chainObj);
    }

    public void PlayMonsterEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayMonsterEffect");
        if (!enableAnimations || card == null) return;
        PlaySound(monsterEffectSound);
        if (useMonsterEffectPrefab && monsterEffectVFX != null) SpawnVFXPublic(monsterEffectVFX, card.transform.position);
        if (useMonsterEffectPulse) StartCoroutine(PulseGhostRoutine(card, monsterEffectPulseScale, monsterEffectPulseDuration, colorMonsterEffect, monsterEffectPulseOutlineWidth));
    }

    private IEnumerator PulseGhostRoutine(CardDisplay card, float targetScaleMult, float halfDuration, Color outlineColor, float outlineWidth, bool tintGhost = false)
    {
        if (card == null) yield break;

        Vector3 originalScale = card.transform.localScale;
        if (GameManager.Instance != null)
        {
            originalScale = card.isOnField ? GameManager.Instance.fieldCardScale : GameManager.Instance.handCardScale;
        }
        Vector3 peakScale = originalScale * targetScaleMult;
        Transform originalParent = card.transform.parent;
        int originalIndex = card.transform.GetSiblingIndex();

        card.transform.SetAsLastSibling(); 

        GameObject ghost = new GameObject("PulseGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); 
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform cardRt = card.GetComponent<RectTransform>();
        rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
        rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
        rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

        Color ghostColor = tintGhost ? new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.8f) : new Color(1f, 1f, 1f, 0.8f);

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture; 
        ri.color = ghostColor;
        
        Outline outline = ghost.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);

        float durationActual = halfDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        while (t < 1f) {
            if (card == null) break;
            t += Time.deltaTime / durationActual; float smooth = Mathf.SmoothStep(0, 1, t);
            card.transform.localScale = Vector3.Lerp(originalScale, peakScale, smooth);
            if (ghost != null) { ghost.transform.localScale = Vector3.Lerp(originalScale, peakScale * 1.15f, smooth); ri.color = new Color(ghostColor.r, ghostColor.g, ghostColor.b, Mathf.Lerp(ghostColor.a, 0f, smooth)); }
            yield return null;
        }
        if (ghost != null) Destroy(ghost);

        t = 0;
        while (t < 1f) {
            if (card == null) break; t += Time.deltaTime / durationActual; float smooth = Mathf.SmoothStep(0, 1, t);
            card.transform.localScale = Vector3.Lerp(peakScale, originalScale, smooth); yield return null;
        }
        if (card != null) { card.transform.localScale = originalScale; if (originalParent != null) { card.transform.SetParent(originalParent, true); card.transform.SetSiblingIndex(originalIndex); } }
    }

    // Wrapper de retrocompatibilidade
    public void PlaySummonAura(CardDisplay card) => PlayPlacementAura(card);

    public void PlayPlacementAura(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayPlacementAura");
        if (!enableAnimations || card == null) return;

        StartCoroutine(PlacementAuraWrapper(card));
    }

    private IEnumerator PlacementAuraWrapper(CardDisplay card)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            while (cg.alpha < 0.5f) yield return null;
        }

        Color targetColor = GetAuraColor(card);

        if (usePlacementAuraRoutine)
        {
            StartCoroutine(PlacementAuraRoutine(card, targetColor));
        }

        if (!usePlacementAuraPrefab || placementAuraVFX == null) yield break;
        
        GameObject vfx = Instantiate(placementAuraVFX);
        
        Transform originalParent = card.transform.parent;
        if (originalParent != null)
        {
            vfx.transform.SetParent(originalParent, false);
            
            if (placementAuraRenderMode == PlacementAuraRenderMode.Behind)
                vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // ATRÁS da carta
            else
                vfx.transform.SetSiblingIndex(card.transform.GetSiblingIndex() + 1); // NA FRENTE da carta
            
            RectTransform rt = vfx.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = card.transform.localRotation; // Acompanha 90º
                rt.localScale = Vector3.one * placementAuraPrefabScale;
            }
            else
            {
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = card.transform.localRotation;
                vfx.transform.localScale = Vector3.one * placementAuraPrefabScale;
            }
        }
        else
        {
            vfx.transform.position = card.transform.position;
            vfx.transform.rotation = card.transform.rotation;
            vfx.transform.localScale = Vector3.one * placementAuraPrefabScale;
        }

        ParticleSystemRenderer[] renderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        if (renderers.Length > 0)
        {
            Canvas rootCanvas = card.GetComponentInParent<Canvas>();
            string targetLayer = rootCanvas != null ? rootCanvas.sortingLayerName : "Default";
            int targetOrder = rootCanvas != null ? rootCanvas.sortingOrder + 1 : 1;
            
            if (placementAuraRenderMode == PlacementAuraRenderMode.Above)
                targetOrder += 10; // Coloca bem na frente da carta
            
            foreach (var r in renderers)
            {
                r.sortingLayerName = targetLayer;
                r.sortingOrder = targetOrder;
            }
        }

        if (colorizeAuraByType || colorizeAuraByPlayer)
        {
            foreach (var r in renderers)
            {
                var psSystem = r.GetComponent<ParticleSystem>();
                if (psSystem != null) { var main = psSystem.main; main.startColor = targetColor; }
            }
            Image[] images = vfx.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.color = targetColor;
            }
        }

        float destroyTime = 3.0f;
        if (overridePlacementAuraPrefabDuration)
        {
            destroyTime = placementAuraPrefabDuration;
        }
        else
        {
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            destroyTime = ps != null ? (ps.main.duration + ps.main.startLifetime.constantMax + 0.5f) : 3.0f;
        }
        Destroy(vfx, destroyTime);
        
        // Debug.Log($"[VFX] [AURA INSTANCIADA]");
    }

    private Color GetAuraColor(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return colorMonsterNormal;
        
        if (colorizeAuraByPlayer && GameManager.Instance != null)
        {
            return card.isPlayerCard ? GameManager.Instance.playerHoverColor : GameManager.Instance.opponentHoverColor;
        }

        if (!colorizeAuraByType) return colorMonsterNormal;
        if (card.isFlipped) return colorFaceDown;
        if (card.CurrentCardData.type.Contains("Spell")) return colorSpell;
        if (card.CurrentCardData.type.Contains("Trap")) return colorTrap;
        if (card.CurrentCardData.type.Contains("Fusion")) return colorFusion;
        if (card.CurrentCardData.type.Contains("Ritual")) return colorRitual;
        if (card.CurrentCardData.type.Contains("Effect")) return colorMonsterEffect;
        return colorMonsterNormal;
    }

    private IEnumerator PlacementAuraRoutine(CardDisplay card, Color outlineColor)
    {
        if (card == null) yield break;

        Transform originalParent = card.transform.parent;

        GameObject ghost = new GameObject("PlacementAuraGhost", typeof(RectTransform), typeof(RawImage), typeof(Outline));
        if (originalParent != null) {
            ghost.transform.SetParent(originalParent, false);
            ghost.transform.SetSiblingIndex(card.transform.GetSiblingIndex()); // Nasce ATRÁS da carta real!
        }

        RectTransform rt = ghost.GetComponent<RectTransform>();
        RectTransform cardRt = card.GetComponent<RectTransform>();
        rt.anchorMin = cardRt.anchorMin; rt.anchorMax = cardRt.anchorMax; rt.pivot = cardRt.pivot;
        rt.sizeDelta = cardRt.sizeDelta; rt.anchoredPosition = cardRt.anchoredPosition;
        rt.localRotation = cardRt.localRotation; rt.localScale = cardRt.localScale;

        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = card.cardImage.texture; 
        
        Outline outline = ghost.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(placementAuraOutlineWidth, -placementAuraOutlineWidth);

        float durationActual = placementAuraDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0;
        
        while (t < 1f) {
            if (card == null || ghost == null) break;
            t += Time.deltaTime / durationActual; 
            float smooth = Mathf.SmoothStep(0, 1, t);
            
            // Curva Perfeita: Invisível (0) -> Fica forte (1) -> Invisível (0)
            float alpha = Mathf.Sin(smooth * Mathf.PI); 
            
            Color c = outlineColor; c.a = alpha;
            ri.color = c; 
            outline.effectColor = c;
            
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
    }

    private void SpawnCardTrailGhost(RectTransform sourceRT, Texture tex, Color color, float duration = 0.3f)
    {
        GameObject ghost = new GameObject("CardTrailGhost", typeof(RectTransform), typeof(RawImage));
        ghost.transform.SetParent(sourceRT.parent, true);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        rt.pivot = sourceRT.pivot;
        
        RawImage ri = ghost.GetComponent<RawImage>();
        ri.texture = tex; ri.color = color;
        
        StartCoroutine(FadeAndDestroyCardGhost(ghost, ri, duration));
    }

    private IEnumerator FadeAndDestroyCardGhost(GameObject obj, RawImage img, float duration)
    {
        float t = 0; Color startColor = img.color;
        Vector3 startScale = obj.transform.localScale;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    // --- INDICADORES DE STATUS (NATIVOS) ---
    private Dictionary<CardDisplay, GameObject> activeCanAttackIcons = new Dictionary<CardDisplay, GameObject>();
    private Dictionary<CardDisplay, GameObject> activeCannotAttackIcons = new Dictionary<CardDisplay, GameObject>();
    private Dictionary<CardDisplay, GameObject> activeCannotChangePosIcons = new Dictionary<CardDisplay, GameObject>();

    public void SetCanAttackIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, canAttackIndicator, activeCanAttackIcons, "CanAttackIcon");
    }

    public void SetCannotAttackIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, cannotAttackIndicator, activeCannotAttackIcons, "CannotAttackIcon");
    }

    public void SetCannotChangePosIndicator(CardDisplay card, bool show)
    {
        SetStatusIndicator(card, show, cannotChangePosIndicator, activeCannotChangePosIcons, "CannotChangePosIcon");
    }

    private void SetStatusIndicator(CardDisplay card, bool show, StatusIndicatorSettings settings, Dictionary<CardDisplay, GameObject> dict, string name)
    {
        if (card == null) return;

        if (show)
        {
            if (!dict.ContainsKey(card) || dict[card] == null)
            {
                GameObject iconObj = null;
                
                if (settings.usePrefab && settings.prefab != null)
                {
                    iconObj = Instantiate(settings.prefab, card.transform);
                    iconObj.name = name;
                    iconObj.transform.SetAsLastSibling();
                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    if (rt != null) {
                        rt.anchoredPosition = settings.offset;
                        rt.localScale = Vector3.one;
                        rt.localRotation = Quaternion.identity;
                    } else {
                        iconObj.transform.localPosition = new Vector3(settings.offset.x, settings.offset.y, 0);
                        iconObj.transform.localScale = Vector3.one;
                        iconObj.transform.localRotation = Quaternion.identity;
                    }
                }
                else if (settings.useNative && settings.sprite != null)
                {
                    iconObj = new GameObject(name, typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(card.transform, false);
                    iconObj.transform.SetAsLastSibling();

                    RectTransform rt = iconObj.GetComponent<RectTransform>();
                    rt.anchoredPosition = settings.offset;
                    rt.sizeDelta = settings.size;

                    Image img = iconObj.GetComponent<Image>();
                    img.sprite = settings.sprite;
                    img.color = settings.color;
                    img.preserveAspect = settings.preserveAspect;
                    if (settings.material != null) img.material = settings.material;

                    if (settings.useOutline)
                    {
                        Outline outline = iconObj.AddComponent<Outline>();
                        outline.effectColor = settings.outlineColor;
                        outline.effectDistance = new Vector2(settings.outlineWidth, -settings.outlineWidth);
                    }

                    if (settings.blinkSpeed > 0)
                    {
                        VfxAutoAnim anim = iconObj.AddComponent<VfxAutoAnim>();
                        anim.duration = 9999f;
                        anim.fadeType = VfxAutoAnim.FadeType.Blink;
                        anim.blinkSpeed = settings.blinkSpeed;
                        anim.scaleType = VfxAutoAnim.ScaleType.None;
                    }
                }

                if (iconObj != null) dict[card] = iconObj;
            }
        }
        else
        {
            if (dict.ContainsKey(card) && dict[card] != null)
            {
                Destroy(dict[card]);
                dict.Remove(card);
            }
        }
    }      
}