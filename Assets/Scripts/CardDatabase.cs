// Assets/Scripts/CardDatabase.cs

using UnityEngine;
using System.IO; // Necessário para ler arquivos

public class CardDatabase : MonoBehaviour
{
    // Uma lista pública para que possamos ver as cartas carregadas no editor do Unity
    public System.Collections.Generic.List<CardData> cardDatabase = new System.Collections.Generic.List<CardData>();

    // Awake é chamado uma vez quando o jogo inicia, antes de qualquer outra coisa
    void Awake()
    {
        LoadCardDatabase();
    }

    void LoadCardDatabase()
    {
        // O caminho para o nosso arquivo JSON dentro da pasta especial StreamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, "cards.json");

        if (File.Exists(path))
        {
            // Lê todo o texto do arquivo
            string jsonText = File.ReadAllText(path);

            // O truque para o JsonUtility ler nosso arquivo:
            // Adicionamos um "invólucro" ao texto do JSON
            string wrappedJson = "{ \"items\": " + jsonText + "}";
            CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(wrappedJson);

            // Preenche nosso banco de dados com as cartas do wrapper
            cardDatabase = wrapper.items;

            // Envia uma mensagem para o console do Unity confirmando o sucesso
            Debug.Log($"SUCESSO: {cardDatabase.Count} cartas carregadas do JSON!");
        }
        else
        {
            // Envia uma mensagem de erro se o arquivo não for encontrado
            Debug.LogError("ERRO: Arquivo 'cards.json' não encontrado em Assets/StreamingAssets!");
        }
    }

    public CardData GetCardById(string id)
    {
        return cardDatabase.Find(x => x.id == id);
    }
}
