using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GraveyardViewer : MonoBehaviour
{
    [Header("Referências")]
    public GameObject cardPrefab; // Prefab da carta para instanciar
    public Transform contentArea; // O painel com GridLayoutGroup onde as cartas serão colocadas
    public Button closeButton;

    private float enableTime;
    void OnEnable() { enableTime = Time.unscaledTime; }

    void Update()
    {
        if (gameObject.activeSelf && Time.unscaledTime - enableTime > 0.1f)
        {
            bool cancelPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame) cancelPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) cancelPressed = true;
            if (Input.GetMouseButtonDown(1)) cancelPressed = true;
#endif
            if (cancelPressed)
            {
                if (GameManager.Instance != null) GameManager.Instance.justCanceledSomething = true;
                Hide();
            }
        }
    }

    void Start()
    {
        // Garante que o painel comece desativado
        gameObject.SetActive(false);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void Show(List<CardData> cards, Texture2D cardBack)
    {
        // 1. Limpa o conteúdo anterior
        ClearContent();

        // 2. Popula com as novas cartas
        if (cardPrefab != null && contentArea != null)
        {
            foreach (CardData cardData in cards)
            {
                GameObject newCardGO = Instantiate(cardPrefab, contentArea);
                CardDisplay display = newCardGO.GetComponent<CardDisplay>();
                if (display != null)
                {
                    display.SetCard(cardData, cardBack, true); // Sempre mostra a face no cemitério
                    display.isInteractable = false; // Desabilita o efeito de hover dentro do viewer
                }
            }
        }

        // 3. Ativa o painel
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        ClearContent();
    }

    private void ClearContent()
    {
        if (contentArea == null) return;

        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }
    }
}
