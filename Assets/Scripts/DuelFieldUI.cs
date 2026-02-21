// Assets/Scripts/DuelFieldUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necessário para textos de alta qualidade
using System.Collections.Generic;

public class DuelFieldUI : MonoBehaviour
{
    [Header("Configuração Visual")]
    public Sprite backgroundSprite; // Arraste o fundo aqui
    public Sprite playerAvatarSprite; // Arraste a foto do Jogador aqui
    public Sprite opponentAvatarSprite; // Arraste a foto do Oponente aqui

    [Header("Info do Jogador")]
    public Image playerAvatarImage;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerLPText;
    
    [Header("Zonas do Jogador")]
    public Transform[] playerMonsterZones;
    public Transform[] playerSpellZones;
    public Transform playerDeck;
    public Transform playerGraveyard;
    public Transform playerFieldSpell;
    public Transform playerExtraDeck;
    public Transform playerHand;

    [Header("Info do Oponente")]
    public Image opponentAvatarImage;
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI opponentLPText;

    [Header("Zonas do Oponente")]
    public Transform[] opponentMonsterZones;
    public Transform[] opponentSpellZones;
    public Transform opponentDeck;
    public Transform opponentGraveyard;
    public Transform opponentFieldSpell;
    public Transform opponentExtraDeck;
    public Transform opponentHand;

    // --- FERRAMENTA DE GERAÇÃO AUTOMÁTICA (EDITOR) ---
#if UNITY_EDITOR
    [ContextMenu("Gerar Layout do Tabuleiro")]
    public void GenerateBoardLayout()
    {
        // 1. Limpa filhos antigos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // 2. Configura o objeto base
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 3. Cria o Background
        CreateImage("BoardBackground", transform, new Color(0.1f, 0.1f, 0.15f, 1f), backgroundSprite);

        // 4. Cria as áreas principais
        // Ajuste: Reservamos espaço na direita para as fases (aprox 12%)
        float playAreaRight = 0.88f;
        RectTransform opponentArea = CreateArea("OpponentArea", transform, 0.5f, 1f, 0f, playAreaRight);
        RectTransform playerArea = CreateArea("PlayerArea", transform, 0f, 0.5f, 0f, playAreaRight);

        // 5. Cria a barra de Fases na direita
        CreatePhaseIndicator("PhaseIndicator", transform, 0f, 1f, playAreaRight, 1f);

        // --- LADO DO JOGADOR ---
        // Perfil (Canto Inferior Esquerdo)
        CreateProfile("PlayerProfile", playerArea, 0.01f, 0.12f, 0.02f, 0.35f, playerAvatarSprite, "Duelist", out playerAvatarImage, out playerNameText, out playerLPText);
        
        // Mão
        playerHand = CreateHandArea("Hand", playerArea, 0f, 0.3f);

        // Campo
        RectTransform pField = CreateArea("Field", playerArea, 0.3f, 1f, 0.15f, 0.85f);
        RectTransform pMonstersRow = CreateRow("MonsterRow", pField, 0.5f, 1f);
        playerMonsterZones = CreateZones(pMonstersRow, "M_Zone", 5, Color.gray);
        RectTransform pSpellsRow = CreateRow("SpellRow", pField, 0f, 0.5f);
        playerSpellZones = CreateZones(pSpellsRow, "S_Zone", 5, new Color(0.2f, 0.4f, 0.4f));

        // Zonas Especiais
        playerFieldSpell = CreateSpecialZone("FieldSpell", playerArea, 0.65f, 0.85f, 0.02f, 0.12f, Color.green);
        playerExtraDeck = CreateSpecialZone("ExtraDeck", playerArea, 0.45f, 0.65f, 0.02f, 0.12f, new Color(0.3f, 0.1f, 0.3f));
        playerGraveyard = CreateSpecialZone("Graveyard", playerArea, 0.65f, 0.85f, 0.88f, 0.98f, Color.gray);
        playerDeck = CreateSpecialZone("Deck", playerArea, 0.45f, 0.65f, 0.88f, 0.98f, new Color(0.4f, 0.2f, 0.1f));


        // --- LADO DO OPONENTE ---
        // Perfil (Canto Superior Direito - Simétrico)
        CreateProfile("OpponentProfile", opponentArea, 0.88f, 0.99f, 0.65f, 0.98f, opponentAvatarSprite, "Kaiba", out opponentAvatarImage, out opponentNameText, out opponentLPText);

        // Mão
        opponentHand = CreateHandArea("Hand", opponentArea, 0.7f, 1f);
        
        // Campo
        RectTransform oField = CreateArea("Field", opponentArea, 0f, 0.7f, 0.15f, 0.85f);
        RectTransform oSpellsRow = CreateRow("SpellRow", oField, 0.5f, 1f);
        opponentSpellZones = CreateZones(oSpellsRow, "S_Zone", 5, new Color(0.2f, 0.4f, 0.4f));
        RectTransform oMonstersRow = CreateRow("MonsterRow", oField, 0f, 0.5f);
        opponentMonsterZones = CreateZones(oMonstersRow, "M_Zone", 5, Color.gray);

        // Zonas Especiais
        opponentFieldSpell = CreateSpecialZone("FieldSpell", opponentArea, 0.15f, 0.35f, 0.88f, 0.98f, Color.green);
        opponentExtraDeck = CreateSpecialZone("ExtraDeck", opponentArea, 0.35f, 0.55f, 0.88f, 0.98f, new Color(0.3f, 0.1f, 0.3f));
        opponentGraveyard = CreateSpecialZone("Graveyard", opponentArea, 0.15f, 0.35f, 0.02f, 0.12f, Color.gray);
        opponentDeck = CreateSpecialZone("Deck", opponentArea, 0.35f, 0.55f, 0.02f, 0.12f, new Color(0.4f, 0.2f, 0.1f));

        Debug.Log("Tabuleiro com Personagens gerado!");
    }

    // --- FUNÇÕES AUXILIARES ---

    void CreatePhaseIndicator(string name, Transform parent, float yMin, float yMax, float xMin, float xMax)
    {
        RectTransform rt = CreateArea(name, parent, yMin, yMax, xMin, xMax);
        
        // Fundo escuro para a barra de fases
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);

        VerticalLayoutGroup layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);

        // Nomes das fases devem corresponder ao Enum GamePhase para o parse funcionar (ou usar índice)
        string[] phaseNames = { "Draw", "Standby", "Main1", "Battle", "Main2", "End" };
        string[] displayNames = { "Draw Phase", "Standby Phase", "Main Phase 1", "Battle Phase", "Main Phase 2", "End Phase" };

        for (int i = 0; i < phaseNames.Length; i++)
        {
            string pName = phaseNames[i];
            string dName = displayNames[i];
            GamePhase phaseEnum = (GamePhase)i;

            GameObject pObj = new GameObject(pName, typeof(RectTransform), typeof(Image), typeof(Button));
            pObj.transform.SetParent(rt, false);
            pObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Cor do botão
            
            // Configura o botão
            Button btn = pObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.TryChangePhase(phaseEnum);
            });

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(pObj.transform, false);
            
            RectTransform tRT = tObj.GetComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(5, 0);
            tRT.offsetMax = new Vector2(-5, 0);

            TextMeshProUGUI txt = tObj.GetComponent<TextMeshProUGUI>();
            txt.text = dName;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 16;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 8;
            txt.fontSizeMax = 24;
            txt.color = Color.white;
        }
    }

    void CreateProfile(string name, Transform parent, float xMin, float xMax, float yMin, float yMax, Sprite sprite, string defaultName, out Image avatarImg, out TextMeshProUGUI nameTxt, out TextMeshProUGUI lpTxt)
    {
        RectTransform rt = CreateArea(name, parent, yMin, yMax, xMin, xMax);
        
        // Avatar
        GameObject avObj = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avObj.transform.SetParent(rt, false);
        RectTransform avRT = avObj.GetComponent<RectTransform>();
        avRT.anchorMin = new Vector2(0, 0.35f); // Ocupa a parte de cima
        avRT.anchorMax = Vector2.one;
        avRT.offsetMin = Vector2.zero;
        avRT.offsetMax = Vector2.zero;
        avatarImg = avObj.GetComponent<Image>();
        avatarImg.color = Color.white;
        if (sprite != null) avatarImg.sprite = sprite;

        // Nome
        GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(rt, false);
        RectTransform nmRT = nameObj.GetComponent<RectTransform>();
        nmRT.anchorMin = new Vector2(0, 0.2f);
        nmRT.anchorMax = new Vector2(1, 0.35f);
        nmRT.offsetMin = Vector2.zero;
        nmRT.offsetMax = Vector2.zero;
        nameTxt = nameObj.GetComponent<TextMeshProUGUI>();
        nameTxt.text = defaultName;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.fontSize = 14;
        nameTxt.enableAutoSizing = true;

        // LP
        GameObject lpObj = new GameObject("LP", typeof(RectTransform), typeof(TextMeshProUGUI));
        lpObj.transform.SetParent(rt, false);
        RectTransform lpRT = lpObj.GetComponent<RectTransform>();
        lpRT.anchorMin = Vector2.zero;
        lpRT.anchorMax = new Vector2(1, 0.2f);
        lpRT.offsetMin = Vector2.zero;
        lpRT.offsetMax = Vector2.zero;
        lpTxt = lpObj.GetComponent<TextMeshProUGUI>();
        lpTxt.text = "8000";
        lpTxt.alignment = TextAlignmentOptions.Center;
        lpTxt.fontSize = 20;
        lpTxt.fontStyle = FontStyles.Bold;
        lpTxt.color = new Color(1f, 0.8f, 0f); // Dourado/Amarelo
    }

    RectTransform CreateArea(string name, Transform parent, float yMin, float yMax, float xMin, float xMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    Transform CreateHandArea(string name, Transform parent, float yMin, float yMax)
    {
        RectTransform rt = CreateArea(name, parent, yMin, yMax, 0.15f, 0.85f); // Ajustei para dar espaço aos perfis
        HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = -50;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        return rt;
    }

    RectTransform CreateRow(string name, Transform parent, float yMin, float yMax)
    {
        RectTransform rt = CreateArea(name, parent, yMin, yMax, 0f, 1f);
        HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        return rt;
    }

    Transform[] CreateZones(Transform parent, string baseName, int count, Color color)
    {
        Transform[] zones = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            GameObject zone = new GameObject($"{baseName}_{i+1}", typeof(RectTransform), typeof(Image));
            zone.transform.SetParent(parent, false);
            zone.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.3f);
            zones[i] = zone.transform;
        }
        return zones;
    }

    Transform CreateSpecialZone(string name, Transform parent, float yMin, float yMax, float xMin, float xMax, Color color)
    {
        RectTransform rt = CreateArea(name, parent, yMin, yMax, xMin, xMax);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.3f);
        return rt;
    }

    void CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        go.GetComponent<RectTransform>().anchorMax = Vector2.one;
        go.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        
        Image img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
    }
#endif
}
