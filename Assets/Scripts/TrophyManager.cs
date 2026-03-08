using UnityEngine;
using System.Collections.Generic;

public class TrophyManager : MonoBehaviour
{
    public static TrophyManager Instance;

    [Tooltip("Se marcado, o sistema de troféus está ativo. Se desmarcado, nenhum progresso é registrado.")]
    public bool trophiesEnabled = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Chamado pelos outros sistemas para registrar progresso
    public void TrackStat(string statName, int amount)
    {
        if (!trophiesEnabled) return;

        if (SaveLoadSystem.Instance == null) return;
        
        var stats = SaveLoadSystem.Instance.GetStats();
        //bool checkTrophies = true; // Warning CS0219: The variable 'checkTrophies' is assigned but its value is never used

        switch (statName)
        {
            case "damage_dealt":
                stats.totalDamageDealt += amount;
                CheckDamageTrophies(stats);
                break;
            case "damage_taken":
                stats.totalDamageTaken += amount;
                CheckDamageTakenTrophies(stats);
                break;
            case "direct_damage":
                stats.totalDirectDamage += amount;
                if (stats.totalDirectDamage >= 50000) Unlock(44);
                break;
            case "reflect_damage":
                stats.totalReflectDamage += amount;
                if (stats.totalReflectDamage >= 50000) Unlock(43);
                break;
            case "effect_damage":
                stats.totalEffectDamage += amount;
                if (stats.totalEffectDamage >= 50000) Unlock(45);
                break;
            case "arena_win":
                stats.arenaWins += amount;
                CheckArenaWinTrophies(stats);
                break;
            case "arena_duel":
                stats.arenaDuels += amount;
                if (stats.arenaDuels >= 1) Unlock(11);
                break;
            case "spell_activated":
                stats.spellsActivated += amount;
                CheckSpellTrophies(stats);
                break;
            case "trap_activated":
                stats.trapsActivated += amount;
                CheckTrapTrophies(stats);
                break;
            case "monster_effect":
                stats.monsterEffectsActivated += amount;
                CheckEffectTrophies(stats);
                break;
            case "fusion_summon":
                stats.fusionSummons += amount;
                if (stats.fusionSummons >= 1) Unlock(60);
                if (stats.fusionSummons >= 100) Unlock(61);
                break;
            case "ritual_summon":
                stats.ritualSummons += amount;
                if (stats.ritualSummons >= 1) Unlock(62);
                if (stats.ritualSummons >= 100) Unlock(63);
                break;
            case "tribute_summon":
                stats.tributeSummons += amount;
                if (stats.tributeSummons >= 1) Unlock(64);
                break;
            case "special_summon":
                stats.specialSummons += amount;
                if (stats.specialSummons >= 1000) Unlock(65);
                break;
            case "max_damage_hit":
                if (amount > stats.highestDamageDealt) stats.highestDamageDealt = amount;
                if (amount >= 3000) Unlock(37);
                break;
            default:
                //checkTrophies = false;
                break;
        }

        // Salva periodicamente ou deixa o SaveLoadSystem salvar no final do duelo
    }

    // Métodos de Verificação Específicos
    void CheckDamageTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.totalDamageDealt >= 1) Unlock(36); // Primeiro Impacto
        if (stats.totalDamageDealt >= 100000) Unlock(40);
        if (stats.totalDamageDealt >= 1000000) Unlock(41);
        if (stats.totalDamageDealt >= 100000000) Unlock(42);
    }

    void CheckDamageTakenTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.totalDamageTaken >= 50000) Unlock(46);
        if (stats.totalDamageTaken >= 1000000) Unlock(47);
        if (stats.totalDamageTaken >= 10000000) Unlock(48);
    }

    void CheckArenaWinTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.arenaWins >= 1) Unlock(12);
        if (stats.arenaWins >= 10) Unlock(13);
        if (stats.arenaWins >= 50) Unlock(14);
        if (stats.arenaWins >= 100) Unlock(15);
        if (stats.arenaWins >= 500) Unlock(16);
        if (stats.arenaWins >= 1000) Unlock(17);
        if (stats.arenaWins >= 5000) Unlock(18);
        if (stats.arenaWins >= 10000) Unlock(19);
        if (stats.arenaWins >= 25000) Unlock(20);
    }

    void CheckSpellTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.spellsActivated >= 1) Unlock(51);
        if (stats.spellsActivated >= 1000) Unlock(52);
        if (stats.spellsActivated >= 10000) Unlock(53);
    }

    void CheckTrapTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.trapsActivated >= 1) Unlock(54);
        if (stats.trapsActivated >= 1000) Unlock(55);
        if (stats.trapsActivated >= 10000) Unlock(56);
    }

    void CheckEffectTrophies(SaveLoadSystem.PlayerStatistics stats)
    {
        if (stats.monsterEffectsActivated >= 1) Unlock(57);
        if (stats.monsterEffectsActivated >= 1000) Unlock(58);
        if (stats.monsterEffectsActivated >= 10000) Unlock(59);
    }

    // Método auxiliar para desbloquear
    public void Unlock(int id)
    {
        if (!trophiesEnabled) return;

        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.UnlockTrophy(id);
        }
    }
}
