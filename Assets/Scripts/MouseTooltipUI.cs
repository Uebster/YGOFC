using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseTooltipUI : MonoBehaviour
{
    public static MouseTooltipUI Instance;

    [Header("UI References")]
    public GameObject tooltipContainer; // O Image principal que contém os textos
    public TextMeshProUGUI leftClickText;
    public TextMeshProUGUI rightClickText;
    
    [Header("Settings")]
    public Vector3 mouseOffset = new Vector3(5f, -5f, 0f); // Mais próximo ao cursor

    void Awake()
    {
        Instance = this;
        if (tooltipContainer != null) 
        {
            // Garante que o tooltip SEMPRE apareça na frente de todas as cartas
            Canvas canvas = tooltipContainer.GetComponent<Canvas>();
            if (canvas == null) canvas = tooltipContainer.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000; // Valor bem alto
            
            tooltipContainer.SetActive(false);
        }
    }

    void Update()
    {
        if (tooltipContainer != null && tooltipContainer.activeSelf)
        {
            Vector3 mousePos;
#if ENABLE_INPUT_SYSTEM
            mousePos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            mousePos = Input.mousePosition;
#endif

            RectTransform rect = tooltipContainer.GetComponent<RectTransform>();

            if (rect != null)
            {
                // Inteligência Espacial: Muda o lado (Pivot) para a UI nunca sair da tela
                float pivotX = mousePos.x > Screen.width * 0.75f ? 1f : 0f; // Vira pra esquerda se estiver nos 25% da direita
                float pivotY = mousePos.y < Screen.height * 0.25f ? 0f : 1f; // Vira pra cima se estiver nos 25% do fundo
                
                rect.pivot = new Vector2(pivotX, pivotY);

                // Ajusta o offset para acompanhar a virada do pivot
                float offsetX = pivotX == 1f ? -Mathf.Abs(mouseOffset.x) : Mathf.Abs(mouseOffset.x);
                float offsetY = pivotY == 0f ? Mathf.Abs(mouseOffset.y) : -Mathf.Abs(mouseOffset.y);

                transform.position = mousePos + new Vector3(offsetX, offsetY, 0);
            }
        }
    }

    public void Show(string leftAction, string rightAction)
    {
        if (tooltipContainer == null) return;
        
        if (string.IsNullOrEmpty(leftAction) && string.IsNullOrEmpty(rightAction))
        {
            Hide();
            return;
        }

        leftClickText.text = "L: " + (string.IsNullOrEmpty(leftAction) ? "-" : leftAction);
        rightClickText.text = "R: " + (string.IsNullOrEmpty(rightAction) ? "-" : rightAction);
        tooltipContainer.SetActive(true);
    }

    public void Hide()
    {
        if (tooltipContainer != null) tooltipContainer.SetActive(false);
    }
}