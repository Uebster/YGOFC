using UnityEngine;
using UnityEditor;

public class VFXOptimizer
{
    [MenuItem("GameObject/VFX: Ajustar para Impacto Rápido (Burst)", false, 22)]
    [MenuItem("Assets/VFX: Ajustar para Impacto Rápido (Burst)", false, 22)]
    public static void OptimizeForBurst()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("[VFX] Selecione um Particle System primeiro."); return; }
        
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null) { Debug.LogWarning($"[VFX] '{go.name}' não possui um ParticleSystem."); return; }

        Undo.RecordObject(ps, "Otimizar VFX (Burst)");
        var main = ps.main; main.duration = 0.5f; main.loop = false; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);

        var emission = ps.emission; emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30, 50) });

        Debug.Log($"[VFXOptimizer] Sucesso! '{go.name}' agora é um impacto concentrado no frame 0 (Burst)!");
    }

    [MenuItem("GameObject/VFX: Escalar para UI (Aumentar Tamanho 50x)", false, 23)]
    [MenuItem("Assets/VFX: Escalar para UI (Aumentar Tamanho 50x)", false, 23)]
    public static void OptimizeForUI()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("[VFX] Selecione um Particle System primeiro."); return; }
        
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null) { Debug.LogWarning($"[VFX] '{go.name}' não possui um ParticleSystem."); return; }

        Undo.RecordObject(ps, "Otimizar Escala VFX (UI)");
        
        // Aumenta os tamanhos base para deixarem de ter "3 pixels" num Canvas Full HD
        var main = ps.main; main.startSizeMultiplier *= 50f; main.startSpeedMultiplier *= 50f;
        
        // Expande o raio/caixa de emissão caso exista
        var shape = ps.shape;
        if (shape.enabled) { shape.radius *= 50f; shape.scale *= 50f; }

        Debug.Log($"[VFXOptimizer] Sucesso! Escala de '{go.name}' aumentada 50x. Agora será visível na UI!");
    }
}