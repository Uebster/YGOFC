// Assets/Scripts/CharacterDatabase.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class CharacterDatabase : MonoBehaviour
{
    public List<CharacterData> characterDatabase = new List<CharacterData>();

    void Awake()
    {
        LoadCharacterDatabase();
    }

    void LoadCharacterDatabase()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "characters.json");
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            // O JSON é uma lista, então usamos o wrapper igual fizemos com as cartas
            string wrappedJson = "{ \"items\": " + jsonText + "}";
            CharacterListWrapper wrapper = JsonUtility.FromJson<CharacterListWrapper>(wrappedJson);
            characterDatabase = wrapper.items;
            Debug.Log($"SUCESSO: {characterDatabase.Count} personagens carregados do JSON!");
        }
        else
        {
            Debug.LogError("ERRO: Arquivo 'characters.json' não encontrado!");
        }
    }

    public CharacterData GetCharacterById(string id)
    {
        return characterDatabase.Find(c => c.id == id);
    }
}
