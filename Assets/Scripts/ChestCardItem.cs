using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections.Generic; // Adicionado para resolver o erro CS0246

public class ChestCardItem : MonoBehaviour, IPointerEnterHandler
{
    [Header("UI References - Obrigatórias")]
    public RawImage cardArtImage;           // A imagem da arte da carta (filho "Art" dentro de "Card2D")
    public TextMeshProUGUI cardNameText;    // "CardNameText"
    public TextMeshProUGUI cardStatsText;   // "CardStatsText" (ATK/DEF ou vazio)
    public TextMeshProUGUI quantCardText;   // "QuantCard" (ex: "x3")
    public TextMeshProUGUI monsterLvlText;  // "MonsterLvl" (nível do monstro)
    public Image attributeIcon;              // "AttributeIcon"
    public Image raceIcon;                   // "RaceIcon"
    public Image typeIcon;                    // "TypeIcon" (Spell/Trap)
    public Image subTypeIcon;                 // "SubTypeIcon" (Continuous, Equip, etc.)
    public GameObject[] stars;                // Array com as 12 estrelas (Star01 a Star12)

    [Header("Configuração")]
    public Color availableColor = Color.white;
    public Color unavailableColor = new Color(1, 1, 1, 0.5f);

    // Referências externas (setadas pelo DeckBuilderManager)
    [HideInInspector] public CardData cardData;
    [HideInInspector] public int availableCopies;
    [HideInInspector] public bool isNew;
    [HideInInspector] public bool isInDeck;

    private DeckBuilderManager manager;

        void Awake()
    {
    manager = DeckBuilderManager.Instance;

    // Auto-assignment using a more robust method that searches all children.
    if (cardArtImage == null) cardArtImage = GetComponentsInChildren<RawImage>(true).FirstOrDefault(img => img.name == "Art");

    var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
    if (cardNameText == null) cardNameText = allTexts.FirstOrDefault(txt => txt.name == "CardNameText");
    if (cardStatsText == null) cardStatsText = allTexts.FirstOrDefault(txt => txt.name == "CardStatsText");
    if (quantCardText == null) quantCardText = allTexts.FirstOrDefault(txt => txt.name == "QuantCard");
    if (monsterLvlText == null) monsterLvlText = allTexts.FirstOrDefault(txt => txt.name == "MonsterLvl");

    var allImages = GetComponentsInChildren<Image>(true);
    if (attributeIcon == null) attributeIcon = allImages.FirstOrDefault(img => img.name == "AttributeIcon");
    if (raceIcon == null) raceIcon = allImages.FirstOrDefault(img => img.name == "RaceIcon");
    if (typeIcon == null) typeIcon = allImages.FirstOrDefault(img => img.name == "TypeIcon");
    if (subTypeIcon == null) subTypeIcon = allImages.FirstOrDefault(img => img.name == "SubTypeIcon");

    // Array de estrelas
    if (stars == null || stars.Length == 0)
    {
        List<GameObject> starList = new List<GameObject>();
        for (int i = 1; i <= 12; i++)
        {
            Transform starTransform = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == $"Star{i:00}");
            if (starTransform != null) starList.Add(starTransform.gameObject);
        }
        stars = starList.ToArray();
    }
    }

        public void Setup(CardData card, int available, bool newFlag, bool inDeck)
    {
        cardData = card;
        availableCopies = available;
        isNew = newFlag;
        isInDeck = inDeck;

        // --- Arte da carta ---
        if (cardArtImage != null && GameManager.Instance != null)
        {
            // Carrega a textura de forma assíncrona? Para performance, podemos carregar同步? 
            // Mas para 2000+ cartas, o ideal é carregar sob demanda ou usar sprites.
            // Por enquanto, usamos o CardDisplay ou carregamento simples.
            // Vamos usar o mesmo método do CardDisplay, mas de forma simplificada:
            StartCoroutine(LoadArt(card.image_filename));
        }

        // --- Nome ---
        if (cardNameText != null)
            cardNameText.text = card.name;

        // --- Quantidade disponível ---
        if (quantCardText != null)
            quantCardText.text = $"x{availableCopies}";

        // --- Transparência se indisponível ---
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = availableCopies > 0 ? 1f : 0.5f;
        cg.interactable = availableCopies > 0;
        cg.blocksRaycasts = true; // Sempre permite hover para mostrar no cardviewer

                        // --- Configurar ícones e textos conforme o tipo ---
        bool isMonster = card.type.Contains("Monster");

        if (isMonster)
        {
            // ATK/DEF
            if (cardStatsText != null)
                cardStatsText.text = $"{card.atk} / {card.def}";

            // Nível
            if (monsterLvlText != null)
            {
                monsterLvlText.gameObject.SetActive(true);
                monsterLvlText.text = card.level.ToString();
            }

            // Atributo
            if (attributeIcon != null)
            {
                if (manager != null && !string.IsNullOrEmpty(card.attribute))
                {
                    string cardAttribute = card.attribute.Trim();
                    var mapping = manager.attributeIcons.Find(x => x.name.Equals(cardAttribute, System.StringComparison.OrdinalIgnoreCase));

                    if (mapping != null && mapping.icon != null)
                    {
                        attributeIcon.sprite = mapping.icon;
                        attributeIcon.enabled = true;
                    }
                    else
                    {
                        attributeIcon.enabled = false;
                        Debug.LogWarning($"[ChestCardItem] Ícone de atributo não encontrado para '{cardAttribute}'. Verifique se o nome do ícone corresponde exatamente no DeckBuilderManager.");
                    }
                }
                else
                {
                    attributeIcon.enabled = false;
                }
            }

            // Raça
            if (raceIcon != null && manager != null)
            {
                string cardRace = card.race?.Trim() ?? "";
                var mapping = manager.raceIcons.Find(x => x.name.Equals(cardRace, System.StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.icon != null)
                {
                    raceIcon.sprite = mapping.icon;
                    raceIcon.enabled = true;
                }
                else { raceIcon.enabled = false; }
            }

            // Desativa ícones de Spell/Trap
            if (typeIcon != null) typeIcon.gameObject.SetActive(false);
            if (subTypeIcon != null) subTypeIcon.gameObject.SetActive(false);

            // Estrelas
            if (stars != null && stars.Length > 0)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] != null)
                        stars[i].SetActive(i < card.level);
                }
            }
        }
                else // Spell / Trap
        {
            // Limpa stats
            if (cardStatsText != null) cardStatsText.text = "";
            if (monsterLvlText != null) monsterLvlText.gameObject.SetActive(false);
            if (attributeIcon != null) attributeIcon.enabled = false;
            if (raceIcon != null) raceIcon.enabled = false;

            // Tipo principal (Spell/Trap)
            string mainType = card.type.Contains("Spell") ? "Spell" : "Trap";
            if (typeIcon != null && manager != null)
            {
                typeIcon.gameObject.SetActive(true);
                var mapping = manager.typeIcons.Find(x => x.name.Equals(mainType, System.StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.icon != null)
                {
                    typeIcon.sprite = mapping.icon;
                    typeIcon.enabled = true;
                }
                else
                {
                    typeIcon.enabled = false;
                }
            }

            // Subtipo (propriedade)
            if (subTypeIcon != null && manager != null && !string.IsNullOrEmpty(card.property) && card.property != "Normal")
            {
                subTypeIcon.gameObject.SetActive(true);
                string cardProperty = card.property.Trim();
                var mapping = manager.subTypeIcons.Find(x => x.name.Equals(cardProperty, System.StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.icon != null)
                {
                    subTypeIcon.sprite = mapping.icon;
                    subTypeIcon.enabled = true;
                }
                else
                {
                    subTypeIcon.enabled = false;
                }
            }
            else if (subTypeIcon != null)
            {
                subTypeIcon.gameObject.SetActive(false);
            }

            // Esconde estrelas
            if (stars != null)
            {
                foreach (var star in stars)
                    if (star != null) star.SetActive(false);
            }
        }

        // --- Tag "NEW" (opcional) ---
        if (isNew && !isInDeck)
        {
            Transform card2D = transform.Find("Card2D");
            if (card2D != null && manager != null)
            {
                manager.CreateNewBanner(card2D);
            }
                }
    }

    private System.Collections.IEnumerator LoadArt(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || cardArtImage == null) yield break;

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, imagePath);
        string url = "file://" + fullPath;
        // Escapa caracteres especiais
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                cardArtImage.texture = tex;
            }
            else
            {
                Debug.LogWarning($"Falha ao carregar arte da carta: {imagePath}");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Atualiza o card viewer
        if (manager != null && cardData != null)
        {
            manager.OnCardHover(cardData);
        }
    }
}
