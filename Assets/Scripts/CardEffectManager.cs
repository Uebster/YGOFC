using UnityEngine;
using System.Collections.Generic;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    // Mapeia ID da carta -> Função de efeito
    private Dictionary<string, System.Action<CardDisplay>> effectDatabase;

    void Awake()
    {
        Instance = this;
        InitializeEffects();
    }

    void InitializeEffects()
    {
        effectDatabase = new Dictionary<string, System.Action<CardDisplay>>();

        // --- MAGIAS (SPELLS) ---
        
        // 0336 - Dark Hole (Destroy all monsters) - ID no JSON fornecido é 0414
        effectDatabase.Add("0414", Effect_DarkHole); 
        // 1480 - Raigeki
        effectDatabase.Add("1480", Effect_Raigeki);

        // 1447 - Pot of Greed (Draw 2)
        effectDatabase.Add("1447", Effect_PotOfGreed);

        // 1318 - Mystical Space Typhoon (Destroy 1 Spell/Trap)
        effectDatabase.Add("1318", Effect_MST);

        // 1268 - Monster Reborn (Special Summon from GY)
        effectDatabase.Add("1268", Effect_MonsterReborn);

        // 1715 - Sparks (Damage 200)
        effectDatabase.Add("1715", (card) => Effect_DirectDamage(card, 200));
        
        // 1382 - Ookazi (Damage 800)
        effectDatabase.Add("1382", (card) => Effect_DirectDamage(card, 800));

        // --- ARMADILHAS (TRAPS) ---
        
        // 1503 - Red Medicine (Gain 500 LP)
        effectDatabase.Add("1503", (card) => Effect_GainLP(card, 500));
    }

    // Método principal chamado pelo GameManager/ChainManager
    public bool ExecuteCardEffect(CardDisplay card)
    {
        if (card == null || card.CurrentCardData == null) return false;

        string id = card.CurrentCardData.id;

        if (effectDatabase.ContainsKey(id))
        {
            Debug.Log($"Executando efeito da carta: {card.CurrentCardData.name} (ID: {id})");
            effectDatabase[id].Invoke(card);
            return true;
        }
        else
        {
            Debug.LogWarning($"Efeito não implementado para: {card.CurrentCardData.name} (ID: {id})");
            return false;
        }
    }

    // --- IMPLEMENTAÇÃO DOS EFEITOS ---

    void Effect_DarkHole(CardDisplay source)
    {
        // Destrói TODOS os monstros no campo
        DestroyAllMonsters(true, true);
    }

    void Effect_Raigeki(CardDisplay source)
    {
        // Destrói apenas monstros do oponente
        bool isPlayer = source.isPlayerCard;
        DestroyAllMonsters(!isPlayer, false); // Se sou player, destruo oponente.
    }

    void DestroyAllMonsters(bool targetOpponent, bool targetPlayer)
    {
        List<CardDisplay> toDestroy = new List<CardDisplay>();

        if (GameManager.Instance.duelFieldUI != null)
        {
            if (targetPlayer)
                CollectMonsters(GameManager.Instance.duelFieldUI.playerMonsterZones, toDestroy);
            if (targetOpponent)
                CollectMonsters(GameManager.Instance.duelFieldUI.opponentMonsterZones, toDestroy);
        }

        foreach (var monster in toDestroy)
        {
            if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDestruction(monster);
            GameManager.Instance.SendToGraveyard(monster.CurrentCardData, monster.isPlayerCard);
            Destroy(monster.gameObject);
        }
    }

    void CollectMonsters(Transform[] zones, List<CardDisplay> list)
    {
        foreach (var zone in zones)
        {
            if (zone.childCount > 0)
            {
                var cd = zone.GetChild(0).GetComponent<CardDisplay>();
                if (cd != null) list.Add(cd);
            }
        }
    }

    void Effect_PotOfGreed(CardDisplay source)
    {
        bool isPlayer = source.isPlayerCard;
        if (isPlayer)
        {
            GameManager.Instance.DrawCard(true); // Ignora limite de fase
            GameManager.Instance.DrawCard(true);
        }
        else
        {
            GameManager.Instance.DrawOpponentCard();
            GameManager.Instance.DrawOpponentCard();
        }
    }

    void Effect_DirectDamage(CardDisplay source, int amount)
    {
        bool isPlayer = source.isPlayerCard;
        // Se o player ativou, causa dano no oponente
        if (isPlayer)
            GameManager.Instance.DamageOpponent(amount);
        else
            GameManager.Instance.DamagePlayer(amount);
            
        if (DuelFXManager.Instance != null) DuelFXManager.Instance.PlayDamageEffect(Vector3.zero);
    }

    void Effect_GainLP(CardDisplay source, int amount)
    {
        bool isPlayer = source.isPlayerCard;
        // Lógica de ganho de vida (precisa implementar no GameManager se não tiver)
        Debug.Log($"{(isPlayer ? "Player" : "Opponent")} ganha {amount} LP.");
        // TODO: Implementar GameManager.HealPlayer(amount);
    }

    void Effect_MST(CardDisplay source)
    {
        // Lógica de seleção de alvo necessária.
        Debug.Log("MST ativado. Lógica de seleção de alvo pendente.");
    }

    void Effect_MonsterReborn(CardDisplay source)
    {
        // Requer seleção de alvo no cemitério.
        Debug.Log("Monster Reborn ativado. Lógica de seleção de cemitério pendente.");
    }
}