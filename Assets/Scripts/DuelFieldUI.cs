// Assets/Scripts/DuelFieldUI.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DuelFieldUI : MonoBehaviour
{
    [Header("Zonas do Jogador")]
    public Transform[] playerMonsterZones; // 5 zonas
    public Transform[] playerSpellZones;   // 5 zonas
    public Transform playerDeck;
    public Transform playerGraveyard;
    public Transform playerFieldSpell;
    public Transform playerExtraDeck;
    public Transform playerHand;

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
        // 1. Limpa filhos antigos para evitar duplicatas
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

        // 3. Cria o Background do Tabuleiro
        CreateImage("BoardBackground", transform, new Color(0.1f, 0.1f, 0.15f, 1f));

        // 4. Cria as áreas principais (Oponente em cima, Jogador embaixo)
        RectTransform opponentArea = CreateArea("OpponentArea", transform, 0.5f, 1f, 0f, 1f);
        RectTransform playerArea = CreateArea("PlayerArea", transform, 0f, 0.5f, 0f, 1f);

        // --- GERAÇÃO DO LADO DO JOGADOR ---
        // Mão
        playerHand = CreateHandArea("Hand", playerArea, 0f, 0.3f);

        // Campo (Monstros + Magias)
        RectTransform pField = CreateArea("Field", playerArea, 0.3f, 1f, 0.15f, 0.85f);
        
        // Linha de Monstros (Top do campo do jogador)
        RectTransform pMonstersRow = CreateRow("MonsterRow", pField, 0.5f, 1f);
        playerMonsterZones = CreateZones(pMonstersRow, "M_Zone", 5, Color.gray);

        // Linha de Magias (Bottom do campo do jogador)
        RectTransform pSpellsRow = CreateRow("SpellRow", pField, 0f, 0.5f);
        playerSpellZones = CreateZones(pSpellsRow, "S_Zone", 5, new Color(0.2f, 0.4f, 0.4f));

        // Zonas Especiais (Esquerda/Direita)
        playerFieldSpell = CreateSpecialZone("FieldSpell", playerArea, 0.65f, 0.85f, 0.02f, 0.12f, Color.green);
        playerExtraDeck = CreateSpecialZone("ExtraDeck", playerArea, 0.45f, 0.65f, 0.02f, 0.12f, new Color(0.3f, 0.1f, 0.3f));
        
        playerGraveyard = CreateSpecialZone("Graveyard", playerArea, 0.65f, 0.85f, 0.88f, 0.98f, Color.gray);
        playerDeck = CreateSpecialZone("Deck", playerArea, 0.45f, 0.65f, 0.88f, 0.98f, new Color(0.4f, 0.2f, 0.1f));


        // --- GERAÇÃO DO LADO DO OPONENTE (Espelhado) ---
        opponentHand = CreateHandArea("Hand", opponentArea, 0.7f, 1f);
        
        RectTransform oField = CreateArea("Field", opponentArea, 0f, 0.7f, 0.15f, 0.85f);
        
        // Linha de Magias (Top do campo do oponente - visualmente invertido)
        RectTransform oSpellsRow = CreateRow("SpellRow", oField, 0.5f, 1f);
        opponentSpellZones = CreateZones(oSpellsRow, "S_Zone", 5, new Color(0.2f, 0.4f, 0.4f));

        // Linha de Monstros (Bottom do campo do oponente)
        RectTransform oMonstersRow = CreateRow("MonsterRow", oField, 0f, 0.5f);
        opponentMonsterZones = CreateZones(oMonstersRow, "M_Zone", 5, Color.gray);

        // Zonas Especiais Oponente
        opponentFieldSpell = CreateSpecialZone("FieldSpell", opponentArea, 0.15f, 0.35f, 0.88f, 0.98f, Color.green);
        opponentExtraDeck = CreateSpecialZone("ExtraDeck", opponentArea, 0.35f, 0.55f, 0.88f, 0.98f, new Color(0.3f, 0.1f, 0.3f));

        opponentGraveyard = CreateSpecialZone("Graveyard", opponentArea, 0.15f, 0.35f, 0.02f, 0.12f, Color.gray);
        opponentDeck = CreateSpecialZone("Deck", opponentArea, 0.35f, 0.55f, 0.02f, 0.12f, new Color(0.4f, 0.2f, 0.1f));

        Debug.Log("Tabuleiro gerado com sucesso!");
    }

    // --- FUNÇÕES AUXILIARES DE CRIAÇÃO ---

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
        RectTransform rt = CreateArea(name, parent, yMin, yMax, 0.1f, 0.9f);
        // Adiciona Layout Horizontal para as cartas na mão
        HorizontalLayoutGroup layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = -50; // Cartas se sobrepõem um pouco
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
            zone.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.3f); // Semi-transparente
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

    void CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        go.GetComponent<RectTransform>().anchorMax = Vector2.one;
        go.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        go.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
    }
#endif
}
