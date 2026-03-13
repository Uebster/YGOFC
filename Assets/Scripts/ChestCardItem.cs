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

        void Awake()
    {
        manager = DeckBuilderManager.Instance;
        
        // Atribuir referências automaticamente se não estiverem setadas
        if (attributeIcon == null) attributeIcon = transform.Find("AttributeIcon")?.GetComponent<Image>();
        if (raceIcon == null) raceIcon = transform.Find("RaceIcon")?.GetComponent<Image>();
        if (typeIcon == null) typeIcon = transform.Find("TypeIcon")?.GetComponent<Image>();
        if (subTypeIcon == null) subTypeIcon = transform.Find("SubTypeIcon")?.GetComponent<Image>();
        
        // DEBUG: Verificar se o manager foi encontrado
        if (manager == null)
        {
            Debug.LogError("[DEBUG] ChestCardItem.Awake: DeckBuilderManager.Instance é nulo!");
        }
        else
        {
            Debug.Log($"[DEBUG] ChestCardItem.Awake: Manager encontrado. Atributos disponíveis: {manager.attributeIcons?.Count ?? 0}");
        }
    }

        public void Setup(CardData card, int available, bool newFlag, bool inDeck)
    {
        cardData = card;
        availableCopies = available;
        isNew = newFlag;
        isInDeck = inDeck;

        // DEBUG: Log para verificar dados da carta
        Debug.Log($"[DEBUG] ChestCardItem.Setup: Carta '{card.name}', Atributo: '{card.attribute}', Tipo: '{card.type}'");

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
        
        // DEBUG: Verificar tipo da carta
        Debug.Log($"[DEBUG] ChestCardItem: Carta '{card.name}' é monstro? {isMonster}. Tipo: '{card.type}'");
        Debug.Log($"[DEBUG] ChestCardItem: attributeIcon é nulo? {attributeIcon == null}");

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

            // Atributo - COM DEBUG
            if (attributeIcon != null)
            {
                if (manager == null)
                {
                    Debug.LogError("[DEBUG] ChestCardItem: DeckBuilderManager.Instance é nulo!");
                    attributeIcon.enabled = false;
                }
                else
                {
                    // DEBUG: Verificar lista de atributos disponíveis
                    if (manager.attributeIcons != null)
                    {
                        Debug.Log($"[DEBUG] Total de ícones de atributo disponíveis: {manager.attributeIcons.Count}");
                        foreach (var icon in manager.attributeIcons)
                        {
                            Debug.Log($"[DEBUG]   - '{icon.name}'");
                        }
                    }

                                        // DEBUG: Verificar atributo exato da carta
                    Debug.Log($"[DEBUG] Atributo da carta: '{card.attribute}' (Tamanho: {card.attribute?.Length})");
                    
                    var mapping = manager.attributeIcons.Find(x => x.name.ToLower() == card.attribute.ToLower());
                    
                    // DEBUG: Log do resultado da busca
                    Debug.Log($"[DEBUG] Buscando atributo '{card.attribute}': Encontrado = {mapping != null}");
                    
                    // DEBUG: Verificar todos os mapeamentos disponíveis
                    if (mapping == null)
                    {
                        Debug.LogWarning($"[DEBUG] Nenhum mapeamento encontrado para atributo '{card.attribute}'. Lista de atributos disponíveis:");
                        foreach (var icon in manager.attributeIcons)
                        {
                            Debug.LogWarning($"[DEBUG]   - '{icon.name}' (Tamanho: {icon.name?.Length})");
                        }
                    }
                    
                    if (mapping != null && mapping.icon != null)
                    {
                        attributeIcon.sprite = mapping.icon;
                        attributeIcon.enabled = true;
                        Debug.Log($"[DEBUG] Ícone de atributo '{card.attribute}' atribuído com sucesso!");
                    }
                    else
                    {
                        attributeIcon.enabled = false;
                        Debug.LogWarning($"[DEBUG] Ícone de atributo '{card.attribute}' não encontrado!");
                    }
                }
            }

            // Raça
            if (raceIcon != null && manager != null)
            {
                var mapping = manager.raceIcons.Find(x => x.name.ToLower() == card.race.ToLower());
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
            // DEBUG: Log para magias/armadilhas
            Debug.Log($"[DEBUG] ChestCardItem: Carta '{card.name}' é magia/armadilha. Desativando atributo e raça.");
            
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
                var mapping = manager.typeIcons.Find(x => x.name.ToLower() == mainType.ToLower());
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
                var mapping = manager.subTypeIcons.Find(x => x.name.ToLower() == card.property.ToLower());
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
