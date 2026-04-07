using UnityEngine;
using UnityEditor;
using System.IO;

public class VFXBuilder : EditorWindow
{
    private Vector2 scrollPos;

    [Header("Basic Settings")]
    private string vfxName = "NovoEfeitoVFX";
    private AudioClip vfxSound;
    private Texture2D customTexture;
    private Material customMaterial;

    public enum RenderModeVFX { Particulas_3D, AnimacaoSprite_UI_2D }
    [Header("Engine de Renderização")]
    private RenderModeVFX renderMode = RenderModeVFX.Particulas_3D;
    
    public enum PresetType { Custom, Explosao_Fogo, Jato_Agua, Poeira_Terra, Fumaca_Escura, Aura_Luz, Deflect_Impacto }
    private PresetType currentPreset = PresetType.Custom;

    public enum AnimMode { Burst_Impacto, Continuo_Aura, Direcional_Tiro }
    [Header("Emission & Life")]
    private AnimMode animMode = AnimMode.Burst_Impacto;
    private float duration = 0.5f;
    private bool isLooping = false;
    private int burstCount = 30;
    private float emissionRate = 0f;

    public enum ShapeType { Sphere, Cone, Circle, Edge }
    [Header("Transform & Physics")]
    private ShapeType shapeType = ShapeType.Sphere;
    private float shapeRadius = 1f;
    private float shapeAngle = 25f;

    [Header("Particle Properties")]
    private float lifeMin = 0.4f;
    private float lifeMax = 0.8f;
    private float speedMin = 15f;
    private float speedMax = 30f;
    private float sizeMin = 0.5f;
    private float sizeMax = 1.5f;

    [Header("Color & Visuals")]
    private Color colorStart = new Color(1f, 0.8f, 0.2f, 1f);
    private Color colorEnd = new Color(1f, 0.4f, 0f, 1f);
    private bool fadeIn = true;
    private bool fadeOut = true;
    private bool useGravity = false;
    private float gravityModifier = 1f;

    [Header("Extras")]
    private bool enableTrails = false;
    private bool enableRotation = true;

    private float uiScale = 50f; // Previne o bug dos "3 pixels" em Canvas Full HD

    [MenuItem("Tools/VFX Builder (Gerador Avançado)")]
    public static void ShowWindow()
    {
        GetWindow<VFXBuilder>("VFX Builder Master");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Gerador Avançado de Partículas", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fogo/Explosão")) ApplyPreset(PresetType.Explosao_Fogo);
        if (GUILayout.Button("Jato de Água")) ApplyPreset(PresetType.Jato_Agua);
        if (GUILayout.Button("Poeira/Terra")) ApplyPreset(PresetType.Poeira_Terra);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fumaça Escura")) ApplyPreset(PresetType.Fumaca_Escura);
        if (GUILayout.Button("Aura de Luz")) ApplyPreset(PresetType.Aura_Luz);
        if (GUILayout.Button("Deflect/Estilhaço")) ApplyPreset(PresetType.Deflect_Impacto);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15);
        renderMode = (RenderModeVFX)EditorGUILayout.EnumPopup("Modo de Geração:", renderMode);
        GUILayout.Space(10);

        vfxName = EditorGUILayout.TextField("Nome do Prefab:", vfxName);
        vfxSound = (AudioClip)EditorGUILayout.ObjectField("Áudio (Opcional):", vfxSound, typeof(AudioClip), false);
        customTexture = (Texture2D)EditorGUILayout.ObjectField("Textura 2D (Opcional):", customTexture, typeof(Texture2D), false);
        customMaterial = (Material)EditorGUILayout.ObjectField("Material (Opcional):", customMaterial, typeof(Material), false);

        GUILayout.Space(10);
        GUILayout.Label("Comportamento e Âncora", EditorStyles.boldLabel);
        animMode = (AnimMode)EditorGUILayout.EnumPopup("Modo de Animação:", animMode);
        duration = EditorGUILayout.Slider("Duração (Segundos):", duration, 0.1f, 10f);
        isLooping = EditorGUILayout.Toggle("Fica em Loop?", isLooping);

        if (animMode == AnimMode.Burst_Impacto)
            burstCount = EditorGUILayout.IntSlider("Quantidade no Burst:", burstCount, 1, 200);
        else
            emissionRate = EditorGUILayout.Slider("Taxa de Emissão (por seg):", emissionRate, 1f, 100f);

        shapeType = (ShapeType)EditorGUILayout.EnumPopup("Formato de Espalhamento:", shapeType);
        shapeRadius = EditorGUILayout.Slider("Raio Inicial:", shapeRadius, 0.1f, 10f);
        if (shapeType == ShapeType.Cone)
            shapeAngle = EditorGUILayout.Slider("Ângulo do Cone:", shapeAngle, 0f, 90f);

        GUILayout.Space(10);
        GUILayout.Label("Física das Partículas", EditorStyles.boldLabel);
        EditorGUILayout.MinMaxSlider("Tamanho (Min/Max):", ref sizeMin, ref sizeMax, 0.1f, 10f);
        EditorGUILayout.MinMaxSlider("Velocidade (Min/Max):", ref speedMin, ref speedMax, 0f, 100f);
        EditorGUILayout.MinMaxSlider("Tempo de Vida (Min/Max):", ref lifeMin, ref lifeMax, 0.1f, 5f);
        
        useGravity = EditorGUILayout.Toggle("Usar Gravidade/Elevação?", useGravity);
        if (useGravity)
            gravityModifier = EditorGUILayout.Slider("Modificador (Negativo = Sobe):", gravityModifier, -3f, 3f);

        GUILayout.Space(10);
        GUILayout.Label("Visual e Cores", EditorStyles.boldLabel);
        colorStart = EditorGUILayout.ColorField("Cor 1 (Mistura):", colorStart);
        colorEnd = EditorGUILayout.ColorField("Cor 2 (Mistura):", colorEnd);
        
        EditorGUILayout.BeginHorizontal();
        fadeIn = EditorGUILayout.Toggle("Fade In (Nasce suave)", fadeIn);
        fadeOut = EditorGUILayout.Toggle("Fade Out (Morre suave)", fadeOut);
        EditorGUILayout.EndHorizontal();

        enableRotation = EditorGUILayout.Toggle("Girar Aleatoriamente?", enableRotation);
        enableTrails = EditorGUILayout.Toggle("Adicionar Rastro (Trails)?", enableTrails);

        GUILayout.Space(20);
        if (GUILayout.Button("CONSTRUIR PREFAB DE EFEITO", GUILayout.Height(50)))
        {
            if (renderMode == RenderModeVFX.Particulas_3D)
                GenerateVFX();
            else
                Generate2DUIEffect();
        }
        EditorGUILayout.EndScrollView();
    }

    private void ApplyPreset(PresetType preset)
    {
        currentPreset = preset;
        switch(preset)
        {
            case PresetType.Explosao_Fogo:
                animMode = AnimMode.Burst_Impacto; shapeType = ShapeType.Sphere;
                colorStart = new Color(1f, 0.5f, 0f, 1f); colorEnd = new Color(1f, 0.1f, 0f, 1f);
                sizeMin = 1f; sizeMax = 3f; speedMin = 10f; speedMax = 25f; lifeMin = 0.3f; lifeMax = 0.6f;
                burstCount = 50; emissionRate = 0; fadeIn = false; fadeOut = true; useGravity = false; enableTrails = false;
                break;
            case PresetType.Jato_Agua:
                animMode = AnimMode.Direcional_Tiro; shapeType = ShapeType.Cone; shapeAngle = 15f;
                colorStart = new Color(0f, 0.5f, 1f, 0.8f); colorEnd = new Color(0f, 0.8f, 1f, 0.5f);
                sizeMin = 0.5f; sizeMax = 1.5f; speedMin = 20f; speedMax = 40f; lifeMin = 0.4f; lifeMax = 0.8f;
                burstCount = 0; emissionRate = 60; fadeIn = true; fadeOut = true; useGravity = true; gravityModifier = 1.5f; enableTrails = true;
                break;
            case PresetType.Poeira_Terra:
                animMode = AnimMode.Burst_Impacto; shapeType = ShapeType.Circle;
                colorStart = new Color(0.6f, 0.4f, 0.2f, 0.8f); colorEnd = new Color(0.4f, 0.3f, 0.1f, 0.5f);
                sizeMin = 1f; sizeMax = 2.5f; speedMin = 5f; speedMax = 15f; lifeMin = 0.5f; lifeMax = 1.2f;
                burstCount = 30; emissionRate = 0; fadeIn = true; fadeOut = true; useGravity = false; enableTrails = false;
                break;
            case PresetType.Fumaca_Escura:
                animMode = AnimMode.Continuo_Aura; shapeType = ShapeType.Sphere;
                colorStart = new Color(0.2f, 0.2f, 0.2f, 0.9f); colorEnd = new Color(0f, 0f, 0f, 0.4f);
                sizeMin = 2f; sizeMax = 5f; speedMin = 2f; speedMax = 5f; lifeMin = 1.5f; lifeMax = 3f;
                burstCount = 0; emissionRate = 15; fadeIn = true; fadeOut = true; useGravity = true; gravityModifier = -0.2f; enableTrails = false;
                break;
            case PresetType.Aura_Luz:
                animMode = AnimMode.Continuo_Aura; shapeType = ShapeType.Circle;
                colorStart = new Color(1f, 1f, 0.8f, 0.8f); colorEnd = new Color(1f, 0.9f, 0.5f, 0f);
                sizeMin = 1f; sizeMax = 2f; speedMin = 2f; speedMax = 8f; lifeMin = 1f; lifeMax = 2f;
                burstCount = 0; emissionRate = 20; fadeIn = true; fadeOut = true; useGravity = true; gravityModifier = -0.5f; enableTrails = false;
                break;
            case PresetType.Deflect_Impacto:
                animMode = AnimMode.Burst_Impacto; shapeType = ShapeType.Sphere;
                colorStart = new Color(0.8f, 0.9f, 1f, 1f); colorEnd = new Color(0.4f, 0.6f, 1f, 1f);
                sizeMin = 0.2f; sizeMax = 0.8f; speedMin = 20f; speedMax = 50f; lifeMin = 0.2f; lifeMax = 0.5f;
                burstCount = 40; emissionRate = 0; fadeIn = false; fadeOut = true; useGravity = true; gravityModifier = 2f; enableTrails = true;
                break;
        }
    }

    private void GenerateVFX()
    {
        string folderPath = "Assets/Resources/GeneratedVFX";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        GameObject go = new GameObject(vfxName);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 100);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var sizeOverLife = ps.sizeOverLifetime;
        var colorOverLife = ps.colorOverLifetime;
        var trails = ps.trails;

        main.playOnAwake = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.duration = duration;
        main.loop = (animMode == AnimMode.Continuo_Aura) || isLooping;

        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin * uiScale, speedMax * uiScale);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin * uiScale, sizeMax * uiScale);
        
        if (enableRotation) main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        if (useGravity) main.gravityModifier = gravityModifier;

        main.startColor = new ParticleSystem.MinMaxGradient(colorStart, colorEnd);

        if (animMode == AnimMode.Burst_Impacto) {
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)(burstCount*0.8f), (short)burstCount) });
        } else {
            emission.rateOverTime = emissionRate;
            emission.SetBursts(new ParticleSystem.Burst[0]);
        }

        shape.enabled = true;
        switch(shapeType) {
            case ShapeType.Sphere: shape.shapeType = ParticleSystemShapeType.Sphere; break;
            case ShapeType.Cone: shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = shapeAngle; break;
            case ShapeType.Circle: shape.shapeType = ParticleSystemShapeType.Circle; break;
            case ShapeType.Edge: shape.shapeType = ParticleSystemShapeType.SingleSidedEdge; break;
        }
        shape.radius = shapeRadius * uiScale;

        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        if (fadeIn || fadeOut) {
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            float startA = fadeIn ? 0f : 1f;
            float endA = fadeOut ? 0f : 1f;
            grad.SetAlphaKeys(new GradientAlphaKey[] {
                new GradientAlphaKey(startA, 0f),
                new GradientAlphaKey(1f, fadeIn ? 0.2f : 0f),
                new GradientAlphaKey(1f, fadeOut ? 0.8f : 1f),
                new GradientAlphaKey(endA, 1f)
            });
            colorOverLife.color = grad;
        }

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 30000;

        if (enableTrails) {
            trails.enabled = true;
            trails.ratio = 0.5f;
            trails.lifetime = 0.2f;
            trails.minVertexDistance = 0.1f * uiScale;
            trails.dieWithParticles = true;
        }

        if (customMaterial != null) {
            renderer.material = customMaterial;
        } else if (customTexture != null) {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (pShader == null) pShader = Shader.Find("Particles/Standard Unlit");
            if (pShader == null) pShader = Shader.Find("Legacy Shaders/Particles/Additive");
            
            Material newMat = new Material(pShader);
            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", customTexture);
            else if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", customTexture);
            
            // Tenta forçar transparência nos Shaders URP
            if (newMat.HasProperty("_Surface")) newMat.SetFloat("_Surface", 1); // 1 = Transparent
            if (newMat.HasProperty("_Blend")) newMat.SetFloat("_Blend", 2); // 2 = Additive
            
            string matPath = $"{folderPath}/{vfxName}_Mat.mat";
            AssetDatabase.CreateAsset(newMat, matPath);
            renderer.material = newMat;
        } else {
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        }

        if (enableTrails) renderer.trailMaterial = renderer.material;

        if (vfxSound != null)
        {
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = vfxSound;
            src.playOnAwake = true;
            src.spatialBlend = 0f; 
        }

        string savePath = $"{folderPath}/{vfxName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, savePath);
        DestroyImmediate(go);

        Debug.Log($"[VFXBuilder] Prefab '{vfxName}' gerado com sucesso em '{savePath}'!");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(savePath));
    }

    private void Generate2DUIEffect()
    {
        string folderPath = "Assets/Resources/GeneratedVFX";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        GameObject go = new GameObject(vfxName);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);

        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.raycastTarget = false; // Não bloqueia os cliques

        UIAnimatedEffect anim = go.AddComponent<UIAnimatedEffect>();
        anim.framesPerSecond = 30f;
        anim.destroyOnEnd = !isLooping;
        anim.loop = isLooping;

        if (vfxSound != null)
        {
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = vfxSound;
            src.playOnAwake = true;
            src.spatialBlend = 0f; 
        }

        string savePath = $"{folderPath}/{vfxName}_2D.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, savePath);
        DestroyImmediate(go);

        Debug.Log($"[VFXBuilder] Prefab 2D '{vfxName}' gerado com sucesso! Arraste os frames para o script nele.");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(savePath));
    }
}