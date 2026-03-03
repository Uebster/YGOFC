// Assets/Scripts/CardData.cs

[System.Serializable]
public class CardData
{
    public string id;
    public string name;
    public string type;
    public string description;
    public int atk;
    public int def;
    public int level;
    public string race;
    public string attribute;
    public string property;
    public string image_filename;
    public System.Collections.Generic.List<string> fusion_materials; // Lista de nomes dos materiais de fusão
}

// Esta classe auxiliar é necessária porque o leitor de JSON padrão do Unity
// não consegue ler um arquivo que começa com uma lista "[...]" diretamente.
// Nós "envelopamos" a lista com esta classe.
[System.Serializable]
public class CardListWrapper
{
    public System.Collections.Generic.List<CardData> items;
}
