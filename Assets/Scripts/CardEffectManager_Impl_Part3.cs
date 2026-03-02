using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    // =========================================================================================
    // IMPLEMENTAÇÃO ESPECÍFICA (ID 1001 - 1100)
    // =========================================================================================

    void Effect_1001_KangarooChamp(CardDisplay source)
    {
        // Effect: If this card attacks or is attacked, change it to Defense Position at the end of the Damage Step.
        // Lógica no OnBattleEnd.
        Debug.Log("Kangaroo Champ: Vira defesa após batalha (Passivo).");
    }

    void Effect_1004_KarakuriSpider(CardDisplay source)
    {
        // Effect: If this card attacks a DARK monster, destroy that monster.
        // Lógica no OnDamageCalculation.
        Debug.Log("Karakuri Spider: Destrói DARK se atacar (Passivo).");
    }

    void Effect_1005_KarateMan(CardDisplay source)
    {
        // Effect: Once per turn, double original ATK. Destroy at End Phase.
        if (source.isOnField)
        {
            source.AddStatModifier(new StatModifier(StatModifier.StatType.ATK, StatModifier.ModifierType.Temporary, StatModifier.Operation.Set, source.originalAtk * 2, source));
            Debug.Log("Karate Man: ATK dobrado. Será destruído na End Phase.");
            // TODO: Agendar destruição na End Phase
        }
    }

    void Effect_1008_Kazejin(CardDisplay source)
    {
        // Effect: Once while face-up, make attacking monster's ATK 0 during damage calculation.
        // Lógica no OnDamageCalculation.
        Debug.Log("Kazejin: Zera ATK do atacante (Passivo/Ativável).");
    }

    void Effect_1009_Kelbek(CardDisplay source)
    {
        // Effect: If attacked, return attacking monster to hand.
        // Lógica no OnBattleEnd.
        Debug.Log("Kelbek: Retorna atacante para mão (Passivo).");
    }

    void Effect_1010_Keldo(CardDisplay source)
    {
        // Effect: When sent to GY by battle: Select 2 cards in opp GY, shuffle into Deck.
        // Lógica no OnCardSentToGraveyard.
        Debug.Log("Keldo: Recicla GY do oponente (Passivo).");
    }

    void Effect_1014_KingDragun(CardDisplay source)
    {
        // Effect: Your Dragons cannot be targeted by Spell/Trap/Effects. Once per turn: SS 1 Dragon from hand.
        if (source.isOnField)
        {
            List<CardData> hand = GameManager.Instance.GetPlayerHandData();
            List<CardData> dragons = hand.FindAll(c => c.race == "Dragon" && c.type.Contains("Monster"));
            
            if (dragons.Count > 0)
            {
                GameManager.Instance.OpenCardSelection(dragons, "Invocar Dragão", (selected) => {
                    GameManager.Instance.SpecialSummonFromData(selected, source.isPlayerCard);
                    GameManager.Instance.RemoveCardFromHand(selected, source.isPlayerCard);
                });
            }
        }
    }

    void Effect_1016_KingTigerWanghu(CardDisplay source)
    {
        // Effect: When monster with ATK <= 1400 is Summoned, destroy it.
        // Lógica no OnSummonImpl (Global Trigger).
        Debug.Log("King Tiger Wanghu: Destrói invocações fracas (Passivo).");
    }

    void Effect_1018_KingOfTheSkullServants(CardDisplay source)
    {
        // Effect: ATK = Skull Servants/King of Skull Servants in GY * 1000. Revive by banishing 1 Skull Servant.
        // Lógica de ATK no OnPhaseStart/UpdateStats.
        // Lógica de Revive no OnCardSentToGraveyard.
        Debug.Log("King of the Skull Servants: Stats dinâmicos e ressurreição.");
    }

    void Effect_1020_KingsKnight(CardDisplay source)
    {
        // Effect: If Queen's Knight on field when Normal Summoned: SS Jack's Knight from Deck.
        if (GameManager.Instance.IsCardActiveOnField("Queen's Knight") || GameManager.Instance.IsCardActiveOnField("1475")) // ID Queen's Knight
        {
            Effect_SearchDeck(source, "Jack's Knight", "Monster"); // Simplificado para busca/SS
        }
    }

    void Effect_1021_Kiryu(CardDisplay source)
    {
        // Effect: Union for Dark Blade. +900 ATK. Tribute to destroy face-up monster.
        Effect_Union(source, "Dark Blade", 900, 0);
    }

    void Effect_1022_Kiseitai(CardDisplay source)
    {
        // Effect: If attacked face-down, equip to attacker. Gain LP equal to half attacker's ATK each Standby.
        // Lógica no BattleManager (Equip on attack) e OnPhaseStart (Gain LP).
        Debug.Log("Kiseitai: Efeito de parasita configurado.");
    }

    void Effect_1023_KishidoSpirit(CardDisplay source)
    {
        // Effect: Monsters not destroyed by battle if ATK is equal.
        // Lógica no BattleManager (ResolveDamage).
        Debug.Log("Kishido Spirit: Proteção em empate de ATK.");
    }

    void Effect_1024_KnightsTitle(CardDisplay source)
    {
        // Effect: Tribute Dark Magician; SS Dark Magician Knight from hand/Deck/GY.
        if (SpellTrapManager.Instance != null)
        {
            SpellTrapManager.Instance.StartTargetSelection(
                (t) => t.isOnField && t.isPlayerCard && t.CurrentCardData.name == "Dark Magician",
                (t) => {
                    GameManager.Instance.TributeCard(t);
                    // Busca DMK (Simplificado: Tenta criar direto)
                    Debug.Log("Knight's Title: Invocando Dark Magician Knight.");
                    // GameManager.Instance.SpecialSummonById("0421", source.isPlayerCard);
                }
            );
        }
    }

    void Effect_1025_Koitsu(CardDisplay source)
    {
        // Effect: Union for Aitsu. +3000 ATK. Piercing.
        Effect_Union(source, "Aitsu", 3000, 0);
    }
}
