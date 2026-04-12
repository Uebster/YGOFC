using UnityEngine;
using UnityEditor;
using System.IO;

public class NotepadWindow : EditorWindow
{
    private string notes = "";
    private string filePath;
    private Vector2 scrollPos;

    [MenuItem("Yu-Gi-Oh!/Dev Tools/Bloco de Notas %g")]
    public static void ShowWindow()
    {
        GetWindow<NotepadWindow>("Notepad");
    }

    void OnEnable()
    {
        // Recupera o último arquivo aberto ou cria um padrão na pasta Support
        string defaultPath = Path.Combine(Application.dataPath, "Support", "Dev_Notes.md");
        filePath = EditorPrefs.GetString("YGO_Notepad_LastPath", defaultPath);
        LoadNotes();
    }

    void OnGUI()
    {
        DrawToolbar();
        
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        EditorGUI.BeginChangeCheck();
        
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.wordWrap = true;
        textAreaStyle.richText = false; // Mantém falso para o desenvolvedor conseguir ver as tags de cor/markdown
        
        notes = EditorGUILayout.TextArea(notes, textAreaStyle, GUILayout.ExpandHeight(true));
        
        if (EditorGUI.EndChangeCheck())
        {
            SaveNotes();
        }
        
        GUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Abrir / Salvar Como...", EditorStyles.toolbarButton))
        {
            ChooseFilePath();
        }
        
        if (GUILayout.Button("Forçar Salvar", EditorStyles.toolbarButton))
        {
            SaveNotes();
            ShowNotification(new GUIContent("Salvo!"));
        }

        GUILayout.Space(10);
        
        // Ferramentas de Formatação
        if (GUILayout.Button("<b>B</b>", EditorStyles.toolbarButton)) InsertTag("**", "**");
        if (GUILayout.Button("<i>I</i>", EditorStyles.toolbarButton)) InsertTag("*", "*");
        if (GUILayout.Button("H1", EditorStyles.toolbarButton)) InsertTag("\n# ", "");
        if (GUILayout.Button("- [ ]", EditorStyles.toolbarButton)) InsertTag("\n- [ ] ", "");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("<color=red>Red</color>", EditorStyles.toolbarButton)) InsertTag("<color=red>", "</color>");
        if (GUILayout.Button("<color=green>Green</color>", EditorStyles.toolbarButton)) InsertTag("<color=green>", "</color>");
        if (GUILayout.Button("<color=yellow>Yellow</color>", EditorStyles.toolbarButton)) InsertTag("<color=yellow>", "</color>");

        GUILayout.FlexibleSpace();
        
        string displayPath = string.IsNullOrEmpty(filePath) ? "Nenhum arquivo" : Path.GetFileName(filePath);
        GUILayout.Label(displayPath, EditorStyles.miniLabel);
        
        GUILayout.EndHorizontal();
    }

    private void InsertTag(string prefix, string suffix)
    {
        // Pega o controle interno de texto da Unity para inserir a tag exatamente onde o cursor está piscando!
        TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        if (te != null && !string.IsNullOrEmpty(te.text))
        {
            string sel = te.SelectedText;
            if (string.IsNullOrEmpty(sel)) sel = "texto";
            te.ReplaceSelection(prefix + sel + suffix);
            notes = te.text;
        }
        else
        {
            notes += $"\n{prefix}texto{suffix}";
        }
        SaveNotes();
        GUI.FocusControl(null); // Tira o foco para a TextArea recarregar visualmente
        Repaint();
    }

    private void ChooseFilePath()
    {
        string startDir = string.IsNullOrEmpty(filePath) ? Application.dataPath : Path.GetDirectoryName(filePath);
        string newPath = EditorUtility.SaveFilePanel("Salvar Anotações Como", startDir, "Dev_Notes", "md,txt,json");
        
        if (!string.IsNullOrEmpty(newPath))
        {
            filePath = newPath;
            EditorPrefs.SetString("YGO_Notepad_LastPath", filePath);
            
            if (File.Exists(filePath)) LoadNotes();
            else SaveNotes();
        }
    }

    private void LoadNotes()
    {
        if (File.Exists(filePath)) notes = File.ReadAllText(filePath);
        else notes = "# Anotações do Projeto\n\nUse os botões acima para formatar o seu texto.";
    }

    private void SaveNotes()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, notes);
    }
}