using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TargetingSwordUI : MonoBehaviour
{
    public static TargetingSwordUI Instance;

    public enum SwordState { Hidden, Hovering, FollowingMouse, LockedOnTarget, Attacking }

    [Header("State")]
    public SwordState currentState = SwordState.Hidden;

    [Header("References")]
    private RectTransform rectTransform;
    private Image img;

    [Header("Tracking")]
    private Transform attacker;
    private Transform lockedTarget;
    private System.Action onAttackHit;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        
        if (img != null)
        {
            img.enabled = false;
            img.raycastTarget = false;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case SwordState.Hidden:
            case SwordState.Attacking:
                // No update logic needed for these states
                break;

            case SwordState.Hovering:
                HoverMouse();
                break;

            case SwordState.FollowingMouse:
                FollowMouse();
                break;

            case SwordState.LockedOnTarget:
                PointAtLockTarget();
                break;
        }
    }

    private void HoverMouse()
    {
        if (attacker == null) { Hide(); return; }
        rectTransform.position = attacker.position;
        Vector3 mousePos;
#if ENABLE_INPUT_SYSTEM
        mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        mousePos = Input.mousePosition;
#endif
        RotateTowards(mousePos);
    }

    private void FollowMouse()
    {
        if (attacker == null)
        {
            Hide();
            return;
        }
        // Attaches the sword to the center of the card
        rectTransform.position = attacker.position;

        // Points the tip (Top Center) towards the Mouse Cursor
        Vector3 mousePos;
#if ENABLE_INPUT_SYSTEM
        mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        mousePos = Input.mousePosition;
#endif
        RotateTowards(mousePos);
    }

    private void PointAtLockTarget()
    {
        if (attacker == null || lockedTarget == null)
        {
            Hide();
            return;
        }
        // Attaches the sword to the center of the card
        rectTransform.position = attacker.position;
        // Points towards the target
        RotateTowards(lockedTarget.position);
    }
    
    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - rectTransform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90 because the sword sprite points up
    }


    // --- PUBLIC API ---

    public void ShowHover(Transform hoverTransform)
    {
        if (currentState != SwordState.Hidden && currentState != SwordState.Hovering) return;
        
        attacker = hoverTransform;
        currentState = SwordState.Hovering;
        if (img != null) img.enabled = true;
    }

    public void HideHover(Transform hoverTransform)
    {
        if (currentState == SwordState.Hovering && attacker == hoverTransform)
        {
            Hide();
        }
    }

    public void ShowAndFollowMouse(Transform attackerTransform)
    {
        if (attackerTransform == null) return;
        this.attacker = attackerTransform;
        this.lockedTarget = null;
        currentState = SwordState.FollowingMouse;
        if (img != null) img.enabled = true;
    }

    public void LockOn(Transform targetTransform)
    {
        if (targetTransform == null) return;
        this.lockedTarget = targetTransform;
        currentState = SwordState.LockedOnTarget;
    }

    public void Unlock()
    {
        if(currentState == SwordState.LockedOnTarget)
        {
            this.lockedTarget = null;
            currentState = SwordState.FollowingMouse;
        }
    }

    public void PerformAttack(CardDisplay target, System.Action onHit)
    {
        if (attacker == null)
        {
            Debug.LogError("[TargetingSwordUI] Attack performed without an attacker set!");
            onHit?.Invoke();
            return;
        }

        // The target for the animation can be a card or a position for a direct attack
        this.lockedTarget = (target != null) ? target.transform : null;
        this.onAttackHit = onHit;
        currentState = SwordState.Attacking;

        StartCoroutine(AnimateAttackRoutine());
    }

    public void Hide()
    {
        attacker = null;
        lockedTarget = null;
        currentState = SwordState.Hidden;
        if (img != null) img.enabled = false;
    }

    private IEnumerator AnimateAttackRoutine()
    {
        // Sound effect for the sword swing
        if (DuelFXManager.Instance != null)
        {
            DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackTravelSound);
        }

        Vector3 startPos = attacker.position;
        Vector3 endPos;

        // If the locked target (card) was destroyed mid-animation, it might become null.
        // We still need a position to fly to.
        if (lockedTarget != null)
        {
            endPos = lockedTarget.position;
        }
        else // Direct attack (or target disappeared)
        {
            // Calculate direct attack position based on who the attacker is
            if (GameManager.Instance != null && attacker.GetComponent<CardDisplay>() != null)
            {
                 endPos = DuelFXManager.Instance.GetDirectAttackTargetPublic(attacker.GetComponent<CardDisplay>(), startPos + Vector3.up * 200f);
            }
            else
            {
                // Fallback if we can't determine direction
                endPos = startPos + Vector3.up * 200f;
            }
        }
        
        // Ensure the sword is pointing at the target from the start
        RotateTowards(endPos);

        float duration = 0.4f; // A quick, sharp flight
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // The sword's position is interpolated from the attacker's current position to the target's position
            // This makes the sword feel like it's coming *from* the card, even if the card moves a bit
            Vector3 currentStart = attacker != null ? attacker.position : startPos;
            Vector3 currentTargetPos = lockedTarget != null ? lockedTarget.position : endPos;

            rectTransform.position = Vector3.Lerp(currentStart, currentTargetPos, elapsed / duration);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- IMPACT ---
        Hide(); // Hide the sword UI on impact

        // Play impact VFX and sound
        if (DuelFXManager.Instance != null)
        {
            DuelFXManager.Instance.PlaySound(DuelFXManager.Instance.attackImpactSound);
            
            // Spawn impact VFX at the destination
            GameObject vfxPrefab = DuelFXManager.Instance.attackVFX;
            if (vfxPrefab != null)
            {
                DuelFXManager.Instance.SpawnVFXPublic(vfxPrefab, endPos);
            }

            // Shake the target card if it exists
            if (lockedTarget != null)
            {
                CardDisplay targetCard = lockedTarget.GetComponent<CardDisplay>();
                if(targetCard != null) DuelFXManager.Instance.PlayCardShake(targetCard);
            }
        }
        
        // Signal that the animation has finished and the duel logic can continue
        onAttackHit?.Invoke();
    }
}
