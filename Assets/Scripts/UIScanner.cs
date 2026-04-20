using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIScanner : MonoBehaviour
{
    void Update()
    {
        bool clicked = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            mousePos = Mouse.current.position.ReadValue();
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            clicked = true;
            mousePos = Input.mousePosition;
        }
#endif

        if (clicked && EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"<color=magenta>--- SCANNER DE CLIQUES ({results.Count} objetos atingidos) ---</color>");
            for (int i = 0; i < results.Count; i++)
            {
                Debug.Log($"<color=cyan>{i + 1}º (Mais à frente):</color> {results[i].gameObject.name}");
            }
        }
    }
}