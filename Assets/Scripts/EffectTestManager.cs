using UnityEngine;

public class EffectTestManager : MonoBehaviour
{
    
    // Referências para as cartas de teste (podem ser atribuídas manualmente ou criadas)
    public CardDisplay playerTestCard;
    public CardDisplay opponentTestCard;

    void OnGUI()
    {
        // Verifica se o modo de teste está ativo no GameManager
        if (GameManager.Instance == null || !GameManager.Instance.effectTestMode) return;

        // Área de botões no canto esquerdo
        GUILayout.BeginArea(new Rect(10, 10, 220, Screen.height - 20));
        
        // Estilo para o título
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        
        GUILayout.Label("--- TESTE DE EFEITOS ---", titleStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("1. Criar Cenário de Teste"))
        {
            SetupTestScenario();
        }

        GUILayout.Space(10);
        GUILayout.Label("Efeitos de Carta (Player):");

        if (playerTestCard != null)
        {
            if (GUILayout.Button("Summon (Invocação)")) DuelFXManager.Instance.PlaySummonEffect(playerTestCard);
            if (GUILayout.Button("Set / Flip")) DuelFXManager.Instance.PlayFlipEffect(playerTestCard);
            if (GUILayout.Button("Activate Spell")) DuelFXManager.Instance.PlayCardActivation(playerTestCard, false);
            if (GUILayout.Button("Activate Trap")) DuelFXManager.Instance.PlayCardActivation(playerTestCard, true);
            if (GUILayout.Button("Tribute (Sacrifício)")) DuelFXManager.Instance.PlayTributeEffect(playerTestCard);
            if (GUILayout.Button("Fusion")) DuelFXManager.Instance.PlayFusionEffect(playerTestCard);
            if (GUILayout.Button("Destruction (Destruir)")) DuelFXManager.Instance.PlayDestruction(playerTestCard);
            if (GUILayout.Button("Banish (Remover)")) DuelFXManager.Instance.PlayBanishEffect(playerTestCard);
        }
        else
        {
            GUILayout.Label("(Crie o cenário primeiro)");
        }

        GUILayout.Space(10);
        GUILayout.Label("Batalha:");

        if (playerTestCard != null && opponentTestCard != null)
        {
            if (GUILayout.Button("Ataque (Player -> Oponente)"))
            {
                DuelFXManager.Instance.PlayAttack(playerTestCard, opponentTestCard, () => Debug.Log("Ataque finalizado (Callback)"));
            }
            if (GUILayout.Button("Ataque Bloqueado (Reflect)"))
            {
                DuelFXManager.Instance.PlayAttackFail(playerTestCard);
            }
            if (GUILayout.Button("Defesa Bem Sucedida"))
            {
                DuelFXManager.Instance.PlayDefenseSuccessEffect(opponentTestCard);
            }
        }
        else
        {
            GUILayout.Label("(Precisa de 2 cartas)");
        }

        GUILayout.Space(10);
        GUILayout.Label("Globais:");
        
        if (GUILayout.Button("Dano (Damage)"))
        {
            DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
        }

        GUILayout.EndArea();
    }

    void SetupTestScenario()
    {
        if (GameManager.Instance == null) return;

        // Tenta encontrar cartas já existentes para não duplicar
        if (playerTestCard == null || opponentTestCard == null)
        {
            var cards = FindObjectsByType<CardDisplay>(FindObjectsSortMode.None);
            foreach (var c in cards)
            {
                if (c.isPlayerCard && playerTestCard == null) playerTestCard = c;
                if (!c.isPlayerCard && opponentTestCard == null) opponentTestCard = c;
            }
        }

        // Se ainda não tem, cria novas
        if (playerTestCard == null)
        {
            playerTestCard = CreateDummyCard(true, "0001"); // Ex: 3-Hump Lacooda
        }
        
        if (opponentTestCard == null)
        {
            opponentTestCard = CreateDummyCard(false, "0002"); // Ex: 30,000-Year White Turtle
        }
    }

    CardDisplay CreateDummyCard(bool isPlayer, string cardId)
    {
        if (GameManager.Instance.duelFieldUI == null) return null;

        Transform zone = isPlayer ? 
            GameManager.Instance.duelFieldUI.playerMonsterZones[2] : 
            GameManager.Instance.duelFieldUI.opponentMonsterZones[2];

        // Limpa a zona se tiver algo
        if (zone.childCount > 0) Destroy(zone.GetChild(0).gameObject);

        GameObject cardGO = Instantiate(GameManager.Instance.cardPrefab, zone);
        CardDisplay display = cardGO.GetComponent<CardDisplay>();
        
        CardData data = GameManager.Instance.cardDatabase.GetCardById(cardId);
        if (data == null && GameManager.Instance.cardDatabase.cardDatabase.Count > 0) 
            data = GameManager.Instance.cardDatabase.cardDatabase[0]; // Fallback

        display.isPlayerCard = isPlayer;
        display.isOnField = true;
        
        // Precisamos da textura do verso
        Texture2D backTex = GameManager.Instance.GetCardBackTexture();
        
        display.SetCard(data, backTex, true);
        
        cardGO.transform.localPosition = Vector3.zero;
        cardGO.transform.localScale = GameManager.Instance.fieldCardScale;
        
        if (!isPlayer)
        {
            cardGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
        }

        return display;
    }
}
