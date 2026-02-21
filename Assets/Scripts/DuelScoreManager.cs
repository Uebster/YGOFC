using UnityEngine;
using System.Collections.Generic;

public enum DuelRank
{
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
    public float duelDuration; // em segundos
    public bool isDeckOutVictory;
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
    
    [Header("Pontuação - Base")]
    public int baseWinPoints = 2500; // Garante pelo menos B/A se for rápido

    [Header("Pontuação - Deduções")]
    [Tooltip("Penaliza descarte excessivo ou destruição de suas próprias cartas.")]
    public int deductionPerCardInGY = 20; 
    [Tooltip("Penaliza a demora. 3 pontos por segundo = -180 pts/min.")]
    public int deductionPerSecond = 3; 
    public int deductionPerEnemySpell = 50;
    public int deductionPerEnemyTrap = 50;

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

    public void StopDuelTracking(bool isDeckOut)
    {
        if (!isTracking) return;
        isTracking = false;
        currentScore.duelDuration = Time.time - startTime;
        currentScore.isDeckOutVictory = isDeckOut;
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

    public DuelRank CalculateFinalRank(out int totalScore)
    {
        // Regra Especial: S+ apenas se o oponente perder por falta de cartas (Deck Out)
        if (currentScore.isDeckOutVictory)
        {
            totalScore = 9999; // Pontuação simbólica máxima
            return DuelRank.SPlus;
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

        // 3. Deduções (Cemitério)
        score -= currentScore.cardsSentToGraveyard * deductionPerCardInGY;
        score -= currentScore.enemySpellsActivated * deductionPerEnemySpell;
        score -= currentScore.enemyTrapsActivated * deductionPerEnemyTrap;
        
        // 4. Dedução por Tempo (Define Rápido vs Lento)
        // Ex: 5 min de duelo = 300s * 3 = -900 pontos.
        int timeDeduction = Mathf.FloorToInt(currentScore.duelDuration * deductionPerSecond);
        score -= timeDeduction;

        // Garante que não fique negativo
        if (score < 0) score = 0;

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
        
        return $"RANK: <color=yellow>{rank}</color>\n" +
               $"Pontuação Total: {score}\n\n" +
               $"Tempo: {currentScore.duelDuration:F0}s (-{Mathf.FloorToInt(currentScore.duelDuration * deductionPerSecond)})\n" +
               $"Magias: {currentScore.spellsActivated} (+{currentScore.spellsActivated * pointsPerSpell})\n" +
               $"Armadilhas: {currentScore.trapsActivated} (+{currentScore.trapsActivated * pointsPerTrap})\n" +
               $"Fusões: {currentScore.fusionsPerformed} (+{currentScore.fusionsPerformed * pointsPerFusion})\n" +
               $"Cemitério: {currentScore.cardsSentToGraveyard} (-{currentScore.cardsSentToGraveyard * deductionPerCardInGY})\n" +
               $"Inimigos Destruídos: {currentScore.enemyMonstersDestroyed} (+{currentScore.enemyMonstersDestroyed * pointsPerEnemyMonsterDestroyed})\n" +
               $"Magias Inimigas: {currentScore.enemySpellsActivated} (-{currentScore.enemySpellsActivated * deductionPerEnemySpell})\n" +
               $"Armadilhas Inimigas: {currentScore.enemyTrapsActivated} (-{currentScore.enemyTrapsActivated * deductionPerEnemyTrap})";
    }
}
