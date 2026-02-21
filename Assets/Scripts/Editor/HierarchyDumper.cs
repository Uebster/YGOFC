using UnityEngine;
using UnityEditor;
using System.Text;

public class HierarchyDumper : MonoBehaviour
{
    // Adiciona uma opção no menu de clique direito na Hierarquia
    [MenuItem("GameObject/Dump Hierarchy (Copiar Texto)", false, 0)]
    static void DumpHierarchy()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("Por favor, selecione um GameObject para mapear.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"ESTRUTURA DE: {go.name}");
        DumpRecursive(go, sb, "");
        
        // Copia para a área de transferência
        GUIUtility.systemCopyBuffer = sb.ToString();
        
        Debug.Log($"Hierarquia de '{go.name}' copiada para a área de transferência! (Pode colar no chat)");
    }

    static void DumpRecursive(GameObject go, StringBuilder sb, string indent)
    {
        sb.Append(indent + "- " + go.name);
        
        // Lista os componentes importantes para sabermos o que tem no objeto
        Component[] comps = go.GetComponents<Component>();
        if (comps.Length > 0)
        {
            sb.Append(" [");
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] != null)
                {
                    // Ignora Transform e CanvasRenderer para limpar a visualização
                    string typeName = comps[i].GetType().Name;
                    if (typeName != "Transform" && typeName != "RectTransform" && typeName != "CanvasRenderer")
                    {
                        sb.Append(typeName);
                        if (i < comps.Length - 1) sb.Append(", ");
                    }
                }
            }
            sb.Append("]");
        }
        sb.AppendLine();

        foreach (Transform child in go.transform)
        {
            DumpRecursive(child.gameObject, sb, indent + "  ");
        }
    }
}
