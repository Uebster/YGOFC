using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectorDumper
{
    // Adiciona a opção no menu de clique direito na Hierarchy (GameObjects)
    [MenuItem("GameObject/Dump Inspector (Copiar Texto)", false, 20)]
    // Adiciona a opção no menu de clique direito no Project (Prefabs, Materiais, ScriptableObjects)
    [MenuItem("Assets/Dump Inspector (Copiar Texto)", false, 20)]
    public static void DumpSelected()
    {
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("[InspectorDumper] Selecione um objeto para copiar o Inspector.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== DUMP DO INSPECTOR: {selected.name} ===");
        sb.AppendLine($"Tipo Principal: {selected.GetType().Name}");
        sb.AppendLine("--------------------------------------------------");

        // Se for um GameObject, precisamos ler todos os componentes atrelados a ele
        if (selected is GameObject go)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                DumpObject(comp, sb);
            }
        }
        else
        {
            // Se for um Asset (ex: Material, ScriptableObject, Script C#), lemos direto
            DumpObject(selected, sb);
        }

        // Copia para a área de transferência do Windows/Mac
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"[InspectorDumper] O Inspector de '{selected.name}' foi copiado para a área de transferência! (Ctrl+V para colar)");
    }

    private static void DumpObject(Object obj, StringBuilder sb)
    {
        sb.AppendLine($"\n[ Componente: {obj.GetType().Name} ]");
        
        // Cria um objeto serializado que simula o que a Unity desenha no Inspector
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.GetIterator();

        bool enterChildren = true;
        
        // Itera por todas as variáveis visíveis
        while (prop.NextVisible(enterChildren))
        {
            // Entra nos filhos se for um Array, List, Struct ou Class serializada
            enterChildren = prop.hasVisibleChildren; 

            // Usa a profundidade para indentar o texto e deixar igual à árvore da Unity
            string indent = new string(' ', prop.depth * 4);
            string propName = prop.displayName;
            string propValue = GetPropertyValueAsString(prop);

            sb.AppendLine($"{indent}- {propName} ({prop.type}): {propValue}");
        }
    }

    private static string GetPropertyValueAsString(SerializedProperty prop)
    {
        try 
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue ? "True" : "False";
                case SerializedPropertyType.Float: return prop.floatValue.ToString("F3");
                case SerializedPropertyType.String: return $"\"{prop.stringValue}\"";
                case SerializedPropertyType.Color: return prop.colorValue.ToString();
                case SerializedPropertyType.ObjectReference: return prop.objectReferenceValue != null ? $"{prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})" : "None/Null";
                case SerializedPropertyType.Enum: return prop.enumDisplayNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0 ? prop.enumDisplayNames[prop.enumValueIndex] : prop.enumValueIndex.ToString();
                case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return prop.vector4Value.ToString();
                case SerializedPropertyType.Rect: return prop.rectValue.ToString();
                case SerializedPropertyType.AnimationCurve: return "[ AnimationCurve ]";
                case SerializedPropertyType.Bounds: return prop.boundsValue.ToString();
                case SerializedPropertyType.Quaternion: return prop.quaternionValue.eulerAngles.ToString() + " (Euler Angles)";
                case SerializedPropertyType.Generic: return prop.isArray ? $"Array/List (Tamanho: {prop.arraySize})" : "[ Struct/Classe ]";
                default: return prop.propertyType.ToString();
            }
        }
        catch { return "[ Erro ao ler valor ]"; }
    }
}