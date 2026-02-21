using UnityEngine;
using System.Collections.Generic;

public enum DuelRank
{
    F,      // Derrota por Deck Out (Falta de cartas)
    D,      // Combate falho / Pontuação muito baixa
    C,      // Combate lento e pobre
    B,      // Combate muito lento
    BPlus,  // Combate lento
    A,      // Vitória padrão
    APlus,  // Vitória rápida
    S,      // Vitória técnica (Muitos pontos)
    SPlus   // Vitória por Deck Out (Oponente sem cartas)
}

[System.Serializable]
public class DuelScoreData
{
    public int spellsActivated;
    public int trapsActivated;
    public int fusionsPerformed;
    public int ritualsPerformed;
    public int summonsPerformed;
    public int tributesPerformed;
    public int cardsSentToGraveyard;
    public int enemyMonstersDestroyed;
    public int enemySpellsActivated;
    public int enemyTrapsActivated;
    public int enemySummonsPerformed;
    public int enemyFusionsPerformed;
    public int enemyRitualsPerformed;
    public int maxDamageDealt; // Maior dano causado em um ataque
    public int damageTaken;    // Total de dano recebido
    public int lpRemaining;    // LP sobrando no final
    public float duelDuration; // em segundos
    public bool isDeckOut;     // Se acabou por falta de cartas
    public bool playerWon;     // Se o jogador venceu
}

public class DuelScoreManager : MonoBehaviour
{
    public static DuelScoreManager Instance;

    [Header("Pontuação - Ações Técnicas")]
    public int pointsPerSpell = 100;
    public int pointsPerTrap = 100;
    public int pointsPerFusion = 400; // Alto valor para incentivar Tec
    public int pointsPerRitual = 400;
    public int pointsPerSummon = 50;
    public int pointsPerTribute = 150;
    public int pointsPerEnemyMonsterDestroyed = 300;
    
    [Header("Pontuação - Bônus Extras")]
    public int pointsPer1000MaxDamage = 100; // Recompensa ataques fortes
    public int pointsPer1000LPRemaining = 50; // Recompensa terminar saudável
    public int bonusNoDamage = 1000; // Vitória Perfeita
    
    [Header("Pontuação - Base")]
    public int baseWinPoints = 2500; // Garante pelo menos B/A se for rápido

    [Header("Pontuação - Deduções")]
    [Tooltip("Penaliza descarte excessivo ou destruição de suas próprias cartas.")]
    public int deductionPerCardInGY = 20; 
    [Tooltip("Penaliza a demora. 3 pontos por segundo = -180 pts/min.")]
    public int deductionPerSecond = 3; 
    public int deductionPerEnemySpell = 50;
    public int deductionPerEnemyTrap = 50;
    public int deductionPerEnemySummon = 0;
    public int deductionPerEnemyFusion = 0;
    public int deductionPerEnemyRitual = 0;
    public int deductionPer1000DamageTaken = 100; // Penaliza apanhar muito

    [Header("Limites de Rank (Thresholds)")]
    // S: Requer ~4000. Com base 2500, precisa de +1500 em ações (ex: 3 fusões + magias)
    public int sRankThreshold = 4000;
    // A+: Requer ~3000. Vitória rápida (pouca perda de tempo) + algumas ações.
    public int aPlusRankThreshold = 3000;
    // A: Requer ~2000. Vitória padrão.
    public int aRankThreshold = 2000;
    // B+: Requer ~1000. Vitória lenta (muita dedução de tempo).
    public int bPlusRankThreshold = 1000;
    // B: Requer ~500. Vitória muito lenta.
    public int bRankThreshold = 500;
    // C: Requer ~250.
    public int cRankThreshold = 250;
    // Abaixo de 250 é D

    private DuelScoreData currentScore;
    private float startTime;
    private bool isTracking;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartDuelTracking()
    {
        currentScore = new DuelScoreData();
        startTime = Time.time;
        isTracking = true;
        Debug.Log("DuelScoreManager: Rastreamento de pontuação iniciado.");
    }

    public void StopDuelTracking(bool playerWon, bool isDeckOut, int finalLP)
    {
        if (!isTracking) return;
        isTracking = false;
        currentScore.duelDuration = Time.time - startTime;
        currentScore.isDeckOut = isDeckOut;
        currentScore.playerWon = playerWon;
        currentScore.lpRemaining = finalLP;
        Debug.Log($"DuelScoreManager: Rastreamento finalizado. Duração: {currentScore.duelDuration:F2}s");
    }

    // --- Métodos de Registro de Ações ---
    // Chame estes métodos do GameManager ou das cartas quando a ação ocorrer
    public void RecordSpellActivation() { if(isTracking) currentScore.spellsActivated++; }
    public void RecordTrapActivation() { if(isTracking) currentScore.trapsActivated++; }
    public void RecordFusion() { if(isTracking) currentScore.fusionsPerformed++; }
    public void RecordRitual() { if(isTracking) currentScore.ritualsPerformed++; }
    public void RecordSummon() { if(isTracking) currentScore.summonsPerformed++; }
    public void RecordTribute() { if(isTracking) currentScore.tributesPerformed++; }
    public void RecordCardSentToGY() { if(isTracking) currentScore.cardsSentToGraveyard++; }
    public void RecordEnemyMonsterDestroyed() { if(isTracking) currentScore.enemyMonstersDestroyed++; }
    public void RecordEnemySpellActivation() { if(isTracking) currentScore.enemySpellsActivated++; }
    public void RecordEnemyTrapActivation() { if(isTracking) currentScore.enemyTrapsActivated++; }
    public void RecordEnemySummon() { if(isTracking) currentScore.enemySummonsPerformed++; }
    public void RecordEnemyFusion() { if(isTracking) currentScore.enemyFusionsPerformed++; }
    public void RecordEnemyRitual() { if(isTracking) currentScore.enemyRitualsPerformed++; }
    
    public void RecordDamageDealt(int amount) 
    { 
        if(isTracking && amount > currentScore.maxDamageDealt) 
            currentScore.maxDamageDealt = amount; 
    }
    
    public void RecordDamageTaken(int amount) 
    { 
        if(isTracking) currentScore.damageTaken += amount; 
    }

    public DuelRank CalculateFinalRank(out int totalScore)
    {
        // Regra Especial: Deck Out
        if (currentScore.isDeckOut)
        {
            totalScore = currentScore.playerWon ? 9999 : 0;
            return currentScore.playerWon ? DuelRank.SPlus : DuelRank.F;
        }

        // 1. Pontuação Base
        int score = baseWinPoints;

        // 2. Bônus por Ações (Tec)
        score += currentScore.spellsActivated * pointsPerSpell;
        score += currentScore.trapsActivated * pointsPerTrap;
        score += currentScore.fusionsPerformed * pointsPerFusion;
        score += currentScore.ritualsPerformed * pointsPerRitual;
        score += currentScore.summonsPerformed * pointsPerSummon;
        score += currentScore.tributesPerformed * pointsPerTribute;
        score += currentScore.enemyMonstersDestroyed * pointsPerEnemyMonsterDestroyed;
        
        // Bônus Extras
        score += (currentScore.maxDamageDealt / 1000) * pointsPer1000MaxDamage;
        score += (currentScore.lpRemaining / 1000) * pointsPer1000LPRemaining;
        if (currentScore.damageTaken == 0) score += bonusNoDamage;

        // 3. Deduções (Cemitério)
        score -= currentScore.cardsSentToGraveyard * deductionPerCardInGY;
        score -= currentScore.enemySpellsActivated * deductionPerEnemySpell;
        score -= currentScore.enemyTrapsActivated * deductionPerEnemyTrap;
        score -= currentScore.enemySummonsPerformed * deductionPerEnemySummon;
        score -= currentScore.enemyFusionsPerformed * deductionPerEnemyFusion;
        score -= currentScore.enemyRitualsPerformed * deductionPerEnemyRitual;
        score -= (currentScore.damageTaken / 1000) * deductionPer1000DamageTaken;
        
        // 4. Dedução por Tempo (Define Rápido vs Lento)
        // Ex: 5 min de duelo = 300s * 3 = -900 pontos.
        int timeDeduction = Mathf.FloorToInt(currentScore.duelDuration * deductionPerSecond);
        score -= timeDeduction;

        // Garante que não fique negativo
        if (score < 0) score = 0;

        // Se perdeu, reduz drasticamente a pontuação (mas não zera, para diferenciar D de F)
        if (!currentScore.playerWon) score /= 4;

        totalScore = score;

        // 5. Classificação
        if (score >= sRankThreshold) return DuelRank.S;
        if (score >= aPlusRankThreshold) return DuelRank.APlus;
        if (score >= aRankThreshold) return DuelRank.A;
        if (score >= bPlusRankThreshold) return DuelRank.BPlus;
        if (score >= bRankThreshold) return DuelRank.B;
        if (score >= cRankThreshold) return DuelRank.C;
        
        return DuelRank.D;
    }
    
    // Retorna um relatório formatado para a tela de vitória
    public string GetScoreReport()
    {
        if (currentScore == null) return "Nenhum duelo registrado.";
        
        int score;
        DuelRank rank = CalculateFinalRank(out score);
        string result = currentScore.playerWon ? "VITÓRIA" : "DERROTA";
        
        return $"RESULTADO: {result}\nRANK: <color=yellow>{rank}</color>\n" +
               $"Pontuação Total: {score}\n\n" +
               $"Tempo: {currentScore.duelDuration:F0}s (-{Mathf.FloorToInt(currentScore.duelDuration * deductionPerSecond)})\n" +
               $"Magias: {currentScore.spellsActivated} (+{currentScore.spellsActivated * pointsPerSpell})\n" +
               $"Armadilhas: {currentScore.trapsActivated} (+{currentScore.trapsActivated * pointsPerTrap})\n" +
               $"Fusões: {currentScore.fusionsPerformed} (+{currentScore.fusionsPerformed * pointsPerFusion})\n" +
               $"Cemitério: {currentScore.cardsSentToGraveyard} (-{currentScore.cardsSentToGraveyard * deductionPerCardInGY})\n" +
               $"Inimigos Destruídos: {currentScore.enemyMonstersDestroyed} (+{currentScore.enemyMonstersDestroyed * pointsPerEnemyMonsterDestroyed})\n" +
               $"Magias Inimigas: {currentScore.enemySpellsActivated} (-{currentScore.enemySpellsActivated * deductionPerEnemySpell})\n" +
               $"Armadilhas Inimigas: {currentScore.enemyTrapsActivated} (-{currentScore.enemyTrapsActivated * deductionPerEnemyTrap})\n" +
               $"Invocações Inimigas: {currentScore.enemySummonsPerformed} (-{currentScore.enemySummonsPerformed * deductionPerEnemySummon})\n" +
               $"Fusões Inimigas: {currentScore.enemyFusionsPerformed} (-{currentScore.enemyFusionsPerformed * deductionPerEnemyFusion})\n" +
               $"Rituais Inimigos: {currentScore.enemyRitualsPerformed} (-{currentScore.enemyRitualsPerformed * deductionPerEnemyRitual})\n" +
               $"Maior Dano: {currentScore.maxDamageDealt} (+{(currentScore.maxDamageDealt / 1000) * pointsPer1000MaxDamage})\n" +
               $"Dano Recebido: {currentScore.damageTaken} (-{(currentScore.damageTaken / 1000) * deductionPer1000DamageTaken})\n" +
               $"LP Restante: {currentScore.lpRemaining} (+{(currentScore.lpRemaining / 1000) * pointsPer1000LPRemaining})";
    }
}
