// Assets/Scripts/CardDisplay.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class CardDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardInfoText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardStatsText;

    private CardData currentCardData;
    private Texture2D frontTexture;
    private Texture2D backTexture;
    private bool isFlipped = false;

    // Este método será chamado pelo GameManager para definir os dados da carta
    public void SetCard(CardData card, Texture2D cardBackTexture, bool startFaceUp = true)
    {
        if (card == null) return;
        currentCardData = card;
        backTexture = cardBackTexture;
        isFlipped = !startFaceUp; // Se startFaceUp for false, isFlipped será true (verso)
        
        DisplayCardDetails();
        
        // Se começar virada, já aplica o verso imediatamente
        if (isFlipped && cardImage != null && backTexture != null)
        {
            cardImage.texture = backTexture;
        }

        StartCoroutine(LoadCardFrontTexture(card.image_filename));
    }

    private void DisplayCardDetails()
    {
        if (currentCardData == null) return;

        if (cardNameText != null) cardNameText.text = currentCardData.name;
        if (cardDescriptionText != null) cardDescriptionText.text = currentCardData.description;

        string info = $"[{currentCardData.type}]";
        if (!string.IsNullOrEmpty(currentCardData.race)) info += $" / {currentCardData.race}";
        if (currentCardData.level > 0) info += $" / LV: {currentCardData.level}";
        if (cardInfoText != null) cardInfoText.text = info;

        if (cardStatsText != null)
        {
            if (currentCardData.type.Contains("Monster"))
                cardStatsText.text = $"ATK/ {currentCardData.atk}  DEF/ {currentCardData.def}";
            else
                cardStatsText.text = "";
        }
    }

    IEnumerator LoadCardFrontTexture(string imagePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, imagePath);
        UnityWebRequest request = UnityWebRequestTexture.GetTexture("file://" + fullPath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            frontTexture = DownloadHandlerTexture.GetContent(request);
            frontTexture.filterMode = FilterMode.Trilinear;
            if (cardImage != null)
            {
                // Só aplica a textura da frente se a carta NÃO estiver virada (isFlipped == false)
                if (!isFlipped) cardImage.texture = frontTexture;
            }
        }
        else
        {
            Debug.LogError($"Falha ao carregar imagem da frente da carta: {fullPath} | Erro: {request.error}");
        }
    }

    public void FlipCard()
    {
        if (cardImage == null || frontTexture == null || backTexture == null) return;

        isFlipped = !isFlipped;
        cardImage.texture = isFlipped ? backTexture : frontTexture;
    }

    public void ShowFront()
    {
        if (cardImage == null || frontTexture == null) return;
        isFlipped = false;
        cardImage.texture = frontTexture;
    }
}