using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    // Cache de arte estático para todos os ChestCardItems
    private static Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();

    void Awake()
    {
        manager = DeckBuilderManager.Instance;
    }

    // Método estático para tentar obter arte do cache
    private static bool TryGetArtFromCache(string cardId, out Texture2D texture)
    {
        return artCache.TryGetValue(cardId, out texture);
    }

    // Método estático para adicionar arte ao cache
    private static void AddArtToCache(string cardId, Texture2D texture)
    {
        if (!artCache.ContainsKey(cardId))
        {
            artCache.Add(cardId, texture);
        }
    }

    public void Setup(CardData card, int available, bool newFlag, bool inDeck)
    {
        cardData = card;
        availableCopies = available;
        isNew = newFlag;
        isInDeck = inDeck;

        // --- Atualiza o DragHandler para garantir que a carta arrastada tenha os dados corretos ---
        DeckDragHandler drag = GetComponent<DeckDragHandler>();
        if (drag != null)
        {
            drag.cardData = card;
            drag.sourceZone = DeckZoneType.Trunk;
        }

        // --- Arte da carta ---
        if (cardArtImage != null && GameManager.Instance != null)
        {
            StartCoroutine(LoadArtWithCache(card.image_filename));
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
        cg.blocksRaycasts = availableCopies > 0;

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
            if (attributeIcon != null && manager != null)
            {
                var mapping = manager.attributeIcons.Find(x => x.name.Equals(card.attribute, System.StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.icon != null)
                {
                    attributeIcon.sprite = mapping.icon;
                    attributeIcon.enabled = true;
                }
                else
                {
                    attributeIcon.enabled = false;
                }
            }

            // Raça
            if (raceIcon != null && manager != null)
            {
                var mapping = manager.raceIcons.Find(x => x.name.Equals(card.race, System.StringComparison.OrdinalIgnoreCase));
                if (mapping != null && mapping.icon != null)
                {
                    raceIcon.sprite = mapping.icon;
                    raceIcon.enabled = true;
                }
                else
                {
                    raceIcon.enabled = false;
                }
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
                var mapping = manager.subTypeIcons.Find(x => x.name.Equals(card.property, System.StringComparison.OrdinalIgnoreCase));
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

    private System.Collections.IEnumerator LoadArtWithCache(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || cardArtImage == null) yield break;

        Texture2D cachedTex = null;
        // 1. Tenta pegar do cache primeiro
        if (TryGetArtFromCache(cardData.id, out cachedTex))
        {
            cardArtImage.texture = cachedTex;
            yield break;
        }

        // 2. Se não estiver no cache, carrega da web/disco
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, imagePath);
        string url = "file://" + fullPath;
        try { url = new System.Uri(fullPath).AbsoluteUri; } catch { }

        using (UnityEngine.Networking.UnityWebRequest uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success && cardArtImage != null)
            {
                Texture2D tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                cardArtImage.texture = tex;
                // 3. Adiciona ao cache para uso futuro
                AddArtToCache(cardData.id, tex);
            }
            else
            {
                Debug.LogWarning($"Falha ao carregar arte da carta: {imagePath}");
                cardArtImage.texture = null; // Limpa a textura em caso de falha
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