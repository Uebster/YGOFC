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
    }

    void Start()
    {
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
        onSelectionMade?.Invoke(position);
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
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                // Cria um sprite a partir da textura carregada
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                
                if (SummonPosition) SummonPosition.sprite = sprite;
                if (SetPosition) SetPosition.sprite = sprite;
            }
        }
    }
}