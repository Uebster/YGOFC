#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlayModeStateSaver : EditorWindow
{
    [MenuItem("Tools/DuelFX/Salvar Configurações (Durante o Play Mode)")]
    public static void SaveDuelFX()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Esta ferramenta só deve ser usada enquanto o jogo estiver rodando (Play Mode) para capturar suas edições temporárias.");
            return;
        }
        
        DuelFXManager fx = Object.FindFirstObjectByType<DuelFXManager>();
        if (fx != null)
        {
            string json = EditorJsonUtility.ToJson(fx);
            EditorPrefs.SetString("SavedDuelFXSettings", json);
            Debug.Log("<color=green>✅ DuelFXManager: Configurações Salvas na Memória! Saia do Play Mode e use a ferramenta de Aplicar.</color>");
        }
    }

    [MenuItem("Tools/DuelFX/Aplicar Configurações Salvas (Edit Mode)")]
    public static void LoadDuelFX()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Saia do Play Mode primeiro para poder gravar as configurações de forma permanente no seu Prefab ou Cena.");
            return;
        }

        string json = EditorPrefs.GetString("SavedDuelFXSettings", "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Nenhuma configuração salva encontrada na memória. Use a opção Salvar durante o Play Mode primeiro.");
            return;
        }

        DuelFXManager fx = Object.FindFirstObjectByType<DuelFXManager>();
        if (fx != null)
        {
            Undo.RecordObject(fx, "Aplicar Configurações DuelFX");
            EditorJsonUtility.FromJsonOverwrite(json, fx);
            EditorUtility.SetDirty(fx);
            Debug.Log("<color=cyan>✨ DuelFXManager: Configurações Aplicadas com Sucesso! Lembre-se de salvar a cena (Ctrl+S).</color>");
        }
    }
}
#endif