using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class RewardPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Image rankDisplay;
    public TextMeshProUGUI rankText;
    
    [Header("Card Display")]
    public RawImage cardArt; // O "quadradinho" que receberá a textura
    public TextMeshProUGUI cardName;
    
    [Header("Buttons")]
    public Button continueButton;

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(ClosePanel);
    }

    public void Show(string rank, CardData card)
    {
        gameObject.SetActive(true);
        
        // Configura Rank
        if (rankText) rankText.text = rank;
        
        // Configura Carta
        if (card != null)
        {
            if (cardName) cardName.text = card.name;
            
            if (cardArt)
            {
                // Define a proporção correta (opcional, mas recomendado)
                // Aspect Ratio de Yu-Gi-Oh é aprox 0.68 (ex: 300x438)
                // Você define o tamanho final no RectTransform do Unity Editor
                cardArt.gameObject.SetActive(true);
                StartCoroutine(LoadCardImage(card.image_filename));
            }
        }
        else
        {
            if (cardName) cardName.text = "Nenhuma carta ganha";
            if (cardArt) cardArt.gameObject.SetActive(false);
        }
    }

    IEnumerator LoadCardImage(string filename)
    {
        // Caminho para a imagem na pasta StreamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, "YuGiOh_OCG_Classic_2147", filename);
        string url = "file://" + path;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                // Aplica a textura baixada na RawImage
                cardArt.texture = DownloadHandlerTexture.GetContent(uwr);
            }
            else
            {
                Debug.LogError($"[RewardPanel] Erro ao carregar imagem {filename}: {uwr.error}");
            }
        }
    }

    void ClosePanel()
    {
        // Limpa a textura para economizar memória
        if (cardArt != null) cardArt.texture = null;
        
        gameObject.SetActive(false);
        
        // Volta para o menu principal
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Btn_BackToMenu();
        }
    }
}
