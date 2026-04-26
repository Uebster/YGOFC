using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class DuelFXManager
{
    // ==============================================================================
    // COMBAT
    // ==============================================================================

    // --- BATALHA ---

    public void PlayAttack(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayAttack");
        if (!enableAnimations)
        {
            onHit?.Invoke();
            return;
        }

        StartCoroutine(AttackRoutine(attacker, target, onHit));
    }

    private IEnumerator AttackRoutine(CardDisplay attacker, CardDisplay target, System.Action onHit)
    {
        Vector3 startPos = attacker.transform.position;
        Vector3 targetPos = target != null ? target.transform.position : GetDirectAttackTargetPublic(attacker, startPos + new Vector3(0, 200, 0));
        
        PlaySound(attackDeclareSound);
        float animSpeed = animationSpeed > 0 ? animationSpeed : 1f;

        int originalIndex = attacker.transform.GetSiblingIndex();
        attacker.transform.SetAsLastSibling();

        // 1. Animação da Carta (Cabeçada / Bote)
        if (useAttackHeadbutt && attacker != null)
        {
            Vector3 dir = targetPos - startPos;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
            dir.Normalize();
            
            Vector3 anticipationPos = startPos - dir * 15f; 
            
            float t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.15f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(startPos, anticipationPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            
            t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.1f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(anticipationPos, startPos + dir * 30f, t);
                yield return null;
            }
            
            t = 0;
            while (t < 1f) {
                if (attacker == null) break;
                t += (Time.deltaTime / 0.1f) * animSpeed;
                attacker.transform.position = Vector3.Lerp(startPos + dir * 30f, startPos, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            if (attacker != null) attacker.transform.position = startPos;
        }

        // 2. Viagem da Espada ou Projétil
        if (flightSwordIndicator.useNative && flightSwordIndicator.sprite != null)
        {
            PlaySound(attackTravelSound);
            
            GameObject proj = new GameObject("AttackFlightSword", typeof(RectTransform), typeof(Image));
            Transform uiParent = GetUIParent();
            if (uiParent != null) proj.transform.SetParent(uiParent, true);
            
            RectTransform rt = proj.GetComponent<RectTransform>();
            rt.position = startPos; rt.sizeDelta = flightSwordIndicator.size;
            
            Image img = proj.GetComponent<Image>();
            img.color = flightSwordIndicator.color;
            img.sprite = flightSwordIndicator.sprite;
            img.preserveAspect = flightSwordIndicator.preserveAspect;
            if (flightSwordIndicator.material != null) img.material = flightSwordIndicator.material;

            if (flightSwordIndicator.useOutline)
            {
                Outline outline = proj.AddComponent<Outline>();
                outline.effectColor = flightSwordIndicator.outlineColor;
                outline.effectDistance = new Vector2(flightSwordIndicator.outlineWidth, -flightSwordIndicator.outlineWidth);
            }

            Vector3 dir = targetPos - startPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.rotation = Quaternion.Euler(0, 0, angle + attackSwordRotationOffset); 
            
            GameObject lineObj = null; RectTransform lineRT = null; Image lineImg = null;
            if (useAttackTrail && attackTrailType == AttackTrailType.ContinuousLine) {
                lineObj = new GameObject("AttackLineTrail", typeof(RectTransform), typeof(Image));
                lineObj.transform.SetParent(proj.transform.parent, true);
                lineObj.transform.SetSiblingIndex(proj.transform.GetSiblingIndex());
                lineRT = lineObj.GetComponent<RectTransform>();
                lineRT.pivot = new Vector2(0, 0.5f); 
                lineRT.position = startPos;
                lineImg = lineObj.GetComponent<Image>(); lineImg.color = attackTrailColor;
                if (flightSwordIndicator.material != null) lineImg.material = flightSwordIndicator.material;
            }

            float duration = attackFlightDuration / animSpeed; float t = 0; float spawnTrailTimer = 0;
            
            while (t < 1f) {
                if (proj == null || rt == null) break;
                t += Time.deltaTime / duration;
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                rt.position = currentPos;
                
                if (useAttackTrail) {
                    if (attackTrailType == AttackTrailType.Shadows && flightSwordIndicator.sprite != null) {
                        spawnTrailTimer -= Time.deltaTime;
                        if (spawnTrailTimer <= 0) {
                            spawnTrailTimer = 0.015f; 
                            SpawnSwordGhost(rt, img.sprite, flightSwordIndicator.preserveAspect, flightSwordIndicator.material);
                        }
                    } else if (attackTrailType == AttackTrailType.ContinuousLine && lineObj != null) {
                        Vector3 dirToCurrent = currentPos - startPos;
                        float dist = dirToCurrent.magnitude;
                        float canvasScale = lineObj.transform.lossyScale.x;
                        if (canvasScale > 0) lineRT.sizeDelta = new Vector2(dist / canvasScale, attackTrailWidth);
                        lineRT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg);
                    }
                }
                yield return null;
            }
            if (lineObj != null) StartCoroutine(FadeAndDestroyLine(lineObj, lineImg, 0.2f));
            Destroy(proj);
        }
        else if (flightSwordIndicator.usePrefab && (flightSwordIndicator.prefab != null || attackProjectileVFX != null))
        {
            PlaySound(attackTravelSound);
            GameObject prefabToUse = flightSwordIndicator.prefab != null ? flightSwordIndicator.prefab : attackProjectileVFX;
            GameObject proj = SpawnVFXPublic(prefabToUse, startPos);
            if (proj != null) {
                RectTransform rt = proj.GetComponent<RectTransform>();
                Image img = proj.GetComponentInChildren<Image>();
                
                Vector3 dir = targetPos - startPos;
                if (rt != null) {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    rt.rotation = Quaternion.Euler(0, 0, angle + attackSwordRotationOffset); 
                }

                float duration = 0.3f / animSpeed; float t = 0; float spawnTrailTimer = 0;
                while (t < 1f) {
                    if (proj == null) break;
                    t += Time.deltaTime / duration;
                    
                    if (rt != null) rt.position = Vector3.Lerp(startPos, targetPos, t);
                    else proj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    
                    if (useAttackTrail && rt != null && img != null && img.sprite != null) {
                        spawnTrailTimer -= Time.deltaTime;
                        if (spawnTrailTimer <= 0) {
                            spawnTrailTimer = 0.015f;
                            SpawnSwordGhost(rt, img.sprite, img.preserveAspect);
                        }
                    }
                    yield return null;
                }
                Destroy(proj);
            }
        }
        else { yield return new WaitForSeconds(0.2f / animSpeed); }

        attacker.transform.SetSiblingIndex(originalIndex);

        // 3. Impacto no Alvo
        PlaySound(attackImpactSound);
        PlayHitEffect(target);
        
        yield return null; 
        // Debug.Log($"[VFX] [ROUTINE CONCLUÍDA] AttackRoutine finalizada.");
        onHit?.Invoke();
    }

    private void SpawnSwordGhost(RectTransform sourceRT, Sprite sprite, bool preserveAspect, Material mat = null)
    {
        GameObject ghost = new GameObject("SwordGhost", typeof(RectTransform), typeof(Image));
        ghost.transform.SetParent(sourceRT.parent, true);
        ghost.transform.SetSiblingIndex(sourceRT.GetSiblingIndex()); 
        
        RectTransform rt = ghost.GetComponent<RectTransform>();
        rt.position = sourceRT.position; rt.rotation = sourceRT.rotation;
        rt.sizeDelta = sourceRT.sizeDelta; rt.localScale = sourceRT.localScale;
        
        Image img = ghost.GetComponent<Image>();
        img.sprite = sprite; img.color = attackTrailColor;
        img.preserveAspect = preserveAspect;
        if (mat != null) img.material = mat;
        
        StartCoroutine(FadeAndDestroyGhost(ghost, img, 0.2f));
    }

    private IEnumerator FadeAndDestroyGhost(GameObject obj, Image img, float duration)
    {
        float t = 0; Color startColor = img.color;
        Vector3 startScale = obj.transform.localScale;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            // Afina o rastro levemente para criar o efeito "Swoosh" de golpe de espada!
            obj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.6f, t);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    private IEnumerator FadeAndDestroyLine(GameObject obj, Image img, float duration)
    {
        float t = 0; Color startColor = img.color;
        while (t < 1f) {
            if (obj == null || img == null) break;
            t += Time.deltaTime / duration;
            img.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    public void PlayDestruction(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayDestruction");
        if (!enableAnimations || card == null) return;
        PlaySound(destroySound);
        
        if (useDestructionRoutine)
            AnimateCardDeath(card, false); // Morte Explosiva (Fantasmas)
            
        // Instancia o VFX por ÚLTIMO para garantir que a explosão cubra a carta no Canvas 2D
        if (useExplosionPrefab && explosionVFX != null)
            SpawnVFXPublic(explosionVFX, card.transform.position);
    }

    // --- NOVOS EFEITOS DE BATALHA E JOGO ---

    public void PlayBanishEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayBanishEffect");
        if (!enableAnimations || card == null) return;
        PlaySound(banishSound);
        
        if (useBanishRoutine) AnimateCardDeath(card, true); // Morte por Sugador (Fantasmas)
        if (useBanishPrefab && banishVFX != null)
            SpawnVFXPublic(banishVFX, card.transform.position);
    }

    private void AnimateCardDeath(CardDisplay card, bool isBanish)
    {
        if (card == null) return;
        // Debug.Log($"[VFX] [ROUTINE INICIADA] AnimateCardDeath");
        
        Transform uiParent = GetUIParent();
        if (uiParent == null) return;
        
        // Cria um clone visual perfeito (Fantasma) antes da carta original ser deletada pelo jogo
        GameObject ghost = new GameObject("CardGhost", typeof(RectTransform), typeof(RawImage));
        
        // FIX: Clona a hierarquia exata primeiro para herdar as âncoras e a escala (ex: 0.8) da carta original!
        Transform originalParent = card.transform.parent;
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
        ri.texture = card.cardImage.texture;
        ri.color = card.cardImage.color;
        
        if (uiParent != null) { ghost.transform.SetParent(uiParent, true); ghost.transform.SetAsLastSibling(); }
        
        if (isBanish) StartCoroutine(BanishGhostRoutine(ghost));
        else StartCoroutine(DestroyGhostRoutine(ghost));
    }

    private IEnumerator BanishGhostRoutine(GameObject ghost)
    {
        float duration = banishShrinkDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float t = 0; 
        Vector3 startScale = ghost.transform.localScale; 
        RawImage ri = ghost.GetComponent<RawImage>();
        while(t < 1f) {
            if (ghost == null) yield break;
            t += Time.deltaTime / duration;
            ghost.transform.Rotate(0, 0, banishSpinSpeed * Time.deltaTime); // Lê do Inspector
            ghost.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (ri != null) ri.color = new Color(0.5f, 0.5f, 0.5f, 1f - t); 
            yield return null;
        } Destroy(ghost);
    }

    private IEnumerator DestroyGhostRoutine(GameObject ghost)
    {
        float totalDuration = destructionDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float waitDuration = destructionWaitHalfExplosion ? totalDuration * 0.5f : 0f;
        float fadeDuration = totalDuration - waitDuration;
        
        RawImage ri = ghost.GetComponent<RawImage>(); 
        Vector3 startPos = ghost.transform.position;
        Vector3 startScale = ghost.transform.localScale;

        // Fase 1: Espera a explosão (Apenas Treme)
        float t = 0;
        while (t < waitDuration)
        {
            if (ghost == null) yield break;
            t += Time.deltaTime;
            
            if (destructionShakeMagnitude > 0f)
            {
                float shake = Mathf.Lerp(2f, destructionShakeMagnitude, t / waitDuration); 
                ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0); 
            }
            yield return null;
        }
        
        // Fase 2: Tosta e Desaparece Encolhendo
        t = 0;
        while (t < fadeDuration) 
        {
            if (ghost == null) yield break;
            t += Time.deltaTime;
            float p = t / fadeDuration;
            
            if (destructionShakeMagnitude > 0f)
            {
                float shake = Mathf.Lerp(destructionShakeMagnitude, 0f, p); 
                ghost.transform.position = startPos + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0); 
            }

            if (destructionFadeAndDarken)
            {
                Color targetColor = destructionFadeColor;
                targetColor.a = Mathf.Lerp(1f, 0f, p);
                ri.color = Color.Lerp(Color.white, targetColor, p);
            }
            
            if (destructionShrink)
            {
                ghost.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, p); // Encolhe mais drástico
            }
            
            yield return null;
        } 
        Destroy(ghost); // Some instantaneamente no ápice do fogo!
    }

    public void PlayDamageEffect(Vector3 position)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayDamageEffect");
        if (!enableAnimations) return;
        PlaySound(damageSound);
        
        if (useDamagePrefab && damageVFX != null)
            SpawnVFXPublic(damageVFX, position, damagePrefabColor != Color.white, damagePrefabColor);

        if (useDamageScreenShake)
        {
            Transform canvas = GetUIParent();
            if (canvas != null) StartCoroutine(ShakeRoutine(canvas, screenShakeDuration, screenShakeMagnitude));
        }
    }

    public void PlayAttackFail(CardDisplay attacker)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayAttackFail");
        if (!enableAnimations || attacker == null) return;
        PlaySound(reflectSound);
        
        if (useReflectPrefab && reflectVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(reflectVFX, attacker.transform.position, reflectPrefabColor != Color.white, reflectPrefabColor);
            if (vfx != null)
            {
                vfx.transform.SetParent(attacker.transform, true);
                vfx.transform.SetAsLastSibling();
            }
        }

        if (useReflectAsStandardDamage)
        {
            StartCoroutine(ShakeRoutine(attacker.transform, reflectShakeDuration, reflectShakeMagnitude));
        }
        else if (useReflectRoutine)
        {
            StartCoroutine(PulseGhostRoutine(attacker, reflectPulseScale, reflectPulseDuration, reflectPulseColor, reflectPulseOutlineWidth, true));
            StartCoroutine(ShakeRoutine(attacker.transform, reflectShakeDuration, reflectShakeMagnitude));
        }
    }

    public void PlayHitEffect(CardDisplay target)
    {
        if (target == null || !enableAnimations) return;
        
        if (useAttackImpactPrefab && attackVFX != null)
        {
            GameObject impact = SpawnVFXPublic(attackVFX, target.transform.position, applyAttackImpactColor, attackImpactPrefabColor, overrideAttackImpactDuration ? attackImpactPrefabDuration : -1f);
            if (impact != null)
            {
                if (attackImpactRotationOffset != 0f)
                    impact.transform.Rotate(0, 0, attackImpactRotationOffset);
                
                impact.transform.localScale = attackImpactPrefabScale;
            }
        }

        if (useAttackImpactRoutine)
        {
            StartCoroutine(PulseGhostRoutine(target, attackImpactPulseScale, attackImpactPulseDuration, attackImpactPulseColor, 8f));
        }

        StartCoroutine(ShakeRoutine(target.transform, hitShakeDuration, hitShakeMagnitude));
    }

    public void PlayDefenseSuccessEffect(CardDisplay card)
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayDefenseSuccessEffect");
        if (!enableAnimations || card == null) return;
        PlaySound(defenseSound);
        
        if (useDefensePrefab && defenseSuccessVFX != null)
        {
            GameObject vfx = SpawnVFXPublic(defenseSuccessVFX, card.transform.position, defensePrefabColor != Color.white, defensePrefabColor);
            if (vfx != null)
            {
                vfx.transform.SetParent(card.transform, true);
                vfx.transform.SetAsLastSibling();
            }
        }

        if (useNativeDefenseShield && defenseShieldSprite != null)
        {
            StartCoroutine(NativeDefenseShieldRoutine(card));
        }

        if (useDefenseRoutine)
        {
            StartCoroutine(PulseGhostRoutine(card, defensePulseScale, defensePulseDuration, defensePulseColor, defensePulseOutlineWidth));
            StartCoroutine(ShakeRoutine(card.transform, defenseShakeDuration, defenseShakeMagnitude));
        }
    }

    private IEnumerator NativeDefenseShieldRoutine(CardDisplay card)
    {
        GameObject shieldObj = new GameObject("DefenseShield_Native", typeof(RectTransform), typeof(Image));
        shieldObj.transform.SetParent(card.transform, false);
        shieldObj.transform.SetAsLastSibling(); // Por cima da carta

        RectTransform rt = shieldObj.GetComponent<RectTransform>();
        rt.anchoredPosition = defenseShieldOffset;
        rt.sizeDelta = defenseShieldSize;
        if (defenseShieldFlip180) rt.localRotation = Quaternion.Euler(0, 0, 180f);

        Image img = shieldObj.GetComponent<Image>();
        img.sprite = defenseShieldSprite;
        img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, 0f);
        if (defenseShieldMaterial != null) img.material = defenseShieldMaterial;

        float duration = defenseShieldDuration / (animationSpeed > 0 ? animationSpeed : 1f);
        float halfDuration = duration / 2f;
        float t = 0;

        Vector3 startScale = Vector3.one;
        Vector3 peakScale = Vector3.one * defenseShieldPulseScale;

        // Fase 1: Aparece e cresce
        while (t < halfDuration) {
            if (shieldObj == null || card == null) break;
            t += Time.deltaTime; float p = Mathf.SmoothStep(0, 1, t / halfDuration);
            rt.localScale = Vector3.Lerp(startScale, peakScale, p);
            img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, Mathf.Lerp(0f, defenseShieldColor.a, p));
            yield return null;
        }
        t = 0;
        // Fase 2: Diminui e esmaece
        while (t < halfDuration) {
            if (shieldObj == null || card == null) break;
            t += Time.deltaTime; float p = Mathf.SmoothStep(0, 1, t / halfDuration);
            rt.localScale = Vector3.Lerp(peakScale, startScale, p);
            img.color = new Color(defenseShieldColor.r, defenseShieldColor.g, defenseShieldColor.b, Mathf.Lerp(defenseShieldColor.a, 0f, p));
            yield return null;
        }
        if (shieldObj != null) Destroy(shieldObj);
    }

    public void PlayCardShake(CardDisplay card)
    {
        if (!enableAnimations || card == null) return;
        // Debug.Log($"[VFX] > [CHAMADA] PlayCardShake");
        StartCoroutine(ShakeRoutine(card.transform, hitShakeDuration, hitShakeMagnitude));
    }

    private IEnumerator ShakeRoutine(Transform target, float duration, float magnitude)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            float x = originalPos.x + Random.Range(-magnitude, magnitude);
            float y = originalPos.y + Random.Range(-magnitude, magnitude);
            target.position = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (target != null) target.position = originalPos;
    }

    public void PlayAttackDeclare()
    {
        // Debug.Log($"[VFX] > [CHAMADA] PlayAttackDeclare");
        PlaySound(attackDeclareSound);
    }

    public Vector3 GetDirectAttackTargetPublic(CardDisplay attacker, Vector3 fallbackPos)
    {
        if (GameManager.Instance == null || GameManager.Instance.duelFieldUI == null) return fallbackPos;

        bool targetAvatar = targetAvatarOnDirectAttack;
        DuelFieldUI ui = GameManager.Instance.duelFieldUI;
        Vector3 targetPos = fallbackPos;

        if (attacker.isPlayerCard) // Player attacking Opponent
        {
            if (targetAvatar && ui.opponentAvatarImage != null) targetPos = ui.opponentAvatarImage.transform.position;
            else if (!targetAvatar && ui.opponentHand != null) targetPos = ui.opponentHand.position;
            else if (ui.opponentAvatarImage != null) targetPos = ui.opponentAvatarImage.transform.position;
        }
        else // Opponent attacking Player
        {
            if (targetAvatar && ui.playerAvatarImage != null) targetPos = ui.playerAvatarImage.transform.position;
            else if (!targetAvatar && ui.playerHand != null) targetPos = ui.playerHand.position;
            else if (ui.playerAvatarImage != null) targetPos = ui.playerAvatarImage.transform.position;
        }
        return targetPos;
    }
}