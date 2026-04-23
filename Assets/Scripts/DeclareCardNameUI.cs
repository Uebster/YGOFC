using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using System.IO;

public class DeclareCardNameUI : MonoBehaviour
{
    public static DeclareCardNameUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI cardNameText;
    public TMP_InputField searchInput;
    public RawImage cardImage;
    public Button confirmButton;
    public Button closeButton;
    public Button eraseButton;

    private System.Action<int> onConfirm;
    private CardData currentMatch;
    private Coroutine imageCoroutine;

    void Awake()
    {
        Instance = this;
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (closeButton) closeButton.onClick.AddListener(CancelSelection);
        if (eraseButton) eraseButton.onClick.AddListener(OnEraseClicked);
        if (searchInput) searchInput.onValueChanged.AddListener(OnInputChanged);
        
        // Submete com a tecla Enter
        if (searchInput) searchInput.onSubmit.AddListener((val) => { if (confirmButton.interactable) ConfirmSelection(); });
        
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            bool cancelPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) cancelPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) cancelPressed = true;
#endif
            if (cancelPressed)
            {
                CancelSelection();
            }
        }
    }

    public void Show(string title, System.Action<int> callback)
    {
        onConfirm = callback;
        if (titleText) titleText.text = title;
        
        currentMatch = null;
        if (searchInput) {
            searchInput.text = "";
            searchInput.Select();
            searchInput.ActivateInputField();
        }
        
        UpdateVisuals();
        gameObject.SetActive(true);
    }

    void OnInputChanged(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            currentMatch = null;
            UpdateVisuals();
            return;
        }

        string lowerInput = input.ToLowerInvariant();
        var allCards = GameManager.Instance.cardDatabase.cardDatabase;

        // Prioridade 1: Nome COMEÇA exatamente com o que foi digitado (Melhor para previsões)
        currentMatch = allCards.FirstOrDefault(c => c.name.ToLowerInvariant().StartsWith(lowerInput));

        // Prioridade 2: Se não achou começando, procura em qualquer parte do nome
        if (currentMatch == null) currentMatch = allCards.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(lowerInput));

        UpdateVisuals();
    }

    void OnEraseClicked()
    {
        if (searchInput != null && searchInput.text.Length > 0)
        {
            searchInput.text = searchInput.text.Substring(0, searchInput.text.Length - 1);
            searchInput.caretPosition = searchInput.text.Length; // Mantém o cursor no final
        }
    }

    void UpdateVisuals()
    {
        if (currentMatch != null)
        {
            if (cardNameText) cardNameText.text = currentMatch.name;
            if (confirmButton) confirmButton.interactable = true;
            
            if (imageCoroutine != null) StopCoroutine(imageCoroutine);
            imageCoroutine = StartCoroutine(LoadCardImage(currentMatch.image_filename));
        }
        else
        {
            if (cardNameText) cardNameText.text = "Nenhuma carta encontrada...";
            if (confirmButton) confirmButton.interactable = false;
            if (cardImage && GameManager.Instance != null) cardImage.texture = GameManager.Instance.GetCardBackTexture();
        }
    }

    private IEnumerator LoadCardImage(string relativePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                tex.filterMode = FilterMode.Trilinear;
                if (cardImage != null) cardImage.texture = tex;
            }
        }
    }

    void ConfirmSelection()
    {
        if (currentMatch != null)
        {
            int cardId = 0;
            if (!string.IsNullOrEmpty(currentMatch.password) && int.TryParse(currentMatch.password, out cardId)) { }
            else {
                string digits = System.Text.RegularExpressions.Regex.Replace(currentMatch.id, @"\D", "");
                int.TryParse(digits, out cardId);
            }
            
            Debug.Log($"<color=green>[DeclareCardNameUI]</color> Carta Selecionada: <b>{currentMatch.name}</b> | Pass: {currentMatch.password} | ID LUA: {cardId}");

            gameObject.SetActive(false);
            onConfirm?.Invoke(cardId);
        }
        else
        {
            Debug.LogWarning("<color=orange>[DeclareCardNameUI]</color> Tentou confirmar, mas nenhuma carta correspondente foi encontrada no 'currentMatch'.");
        }
    }

    void CancelSelection()
    {
        gameObject.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.justCanceledSomething = true;
        onConfirm?.Invoke(0);
    }
}