using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class PositionSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI Text_PositionAsk;
    public Button Btn_SummonPosition;
    public Image SummonPosition; // A imagem da carta dentro do botão de ataque
    public Button Btn_SetPosition;
    public Image SetPosition; // A imagem da carta dentro do botão de defesa

    private System.Action<CardDisplay.BattlePosition> onSelectionMade;

    // Variável para rastrear o sprite criado e destruí-lo depois
    private Sprite currentLoadedSprite;
    private Texture2D currentLoadedTexture;

    void Awake()
    {
        // Auto-configuração baseada na hierarquia fornecida
        if (Text_PositionAsk == null) 
            Text_PositionAsk = transform.Find("Text_PositionAsk")?.GetComponent<TextMeshProUGUI>();
        
        if (Btn_SummonPosition == null) 
        {
            Transform btnTr = transform.Find("Btn_SummonPosition");
            if (btnTr != null)
            {
                Btn_SummonPosition = btnTr.GetComponent<Button>();
                // Procura a imagem filha chamada "SummonPosition"
                if (SummonPosition == null) 
                    SummonPosition = btnTr.Find("SummonPosition")?.GetComponent<Image>();
            }
        }

        if (Btn_SetPosition == null) 
        {
            Transform btnTr = transform.Find("Btn_SetPosition");
            if (btnTr != null)
            {
                Btn_SetPosition = btnTr.GetComponent<Button>();
                // Procura a imagem filha chamada "SetPosition"
                if (SetPosition == null) 
                    SetPosition = btnTr.Find("SetPosition")?.GetComponent<Image>();
            }
        }
        if (Btn_SummonPosition) 
            Btn_SummonPosition.onClick.AddListener(() => SelectPosition(CardDisplay.BattlePosition.Attack));
        
        if (Btn_SetPosition) 
            Btn_SetPosition.onClick.AddListener(() => SelectPosition(CardDisplay.BattlePosition.Defense));
        
        // Garante que comece fechado
        gameObject.SetActive(false);
    }

    public void Show(CardData card, System.Action<CardDisplay.BattlePosition> callback)
    {
        onSelectionMade = callback;
        gameObject.SetActive(true);

        if (Text_PositionAsk) 
            Text_PositionAsk.text = $"Invocar {card.name}?";

        // Limpa texturas anteriores antes de carregar novas
        CleanupPreviousImages();

        // Carrega a imagem da carta nas opções
        if (card != null && !string.IsNullOrEmpty(card.image_filename))
        {
            StartCoroutine(LoadCardImage(card.image_filename));
        }
        
        // Ajusta rotação para representar Ataque (Vertical) e Defesa (Horizontal)
        if (SummonPosition) SummonPosition.transform.localRotation = Quaternion.identity;
        if (SetPosition) SetPosition.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    void SelectPosition(CardDisplay.BattlePosition position)
    {
        gameObject.SetActive(false);
        // Limpa as imagens ao fechar para liberar memória
        CleanupPreviousImages();
        onSelectionMade?.Invoke(position);
    }

    // Método para limpar memória
    private void CleanupPreviousImages()
    {
        if (SummonPosition) SummonPosition.sprite = null;
        if (SetPosition) SetPosition.sprite = null;

        if (currentLoadedSprite != null)
        {
            Destroy(currentLoadedSprite);
            currentLoadedSprite = null;
        }
        if (currentLoadedTexture != null)
        {
            Destroy(currentLoadedTexture);
            currentLoadedTexture = null;
        }
    }

    IEnumerator LoadCardImage(string imagePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);
        string url = "file://" + fullPath;
        
        // Tenta escapar a URL para evitar erros com caracteres especiais
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Salva referência para destruir depois
                currentLoadedTexture = DownloadHandlerTexture.GetContent(request);
                
                // Cria um sprite a partir da textura carregada
                currentLoadedSprite = Sprite.Create(currentLoadedTexture, new Rect(0, 0, currentLoadedTexture.width, currentLoadedTexture.height), new Vector2(0.5f, 0.5f));
                
                if (SummonPosition) SummonPosition.sprite = currentLoadedSprite;
                if (SetPosition) SetPosition.sprite = currentLoadedSprite;
            }
        }
    }

    // Garante limpeza se o objeto for destruído
    void OnDestroy()
    {
        CleanupPreviousImages();
    }
}
