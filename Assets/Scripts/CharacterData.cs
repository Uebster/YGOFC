// Assets/Scripts/CharacterData.cs
using System.Collections.Generic;

[System.Serializable]
public class CharacterData
{
    public string id;
    public string name;
    public List<string> deck_A;
    public List<string> deck_B;
    public List<string> deck_C;
    public List<string> rewards;
    public string field;
    public string difficulty;
    public string story_role;
}

[System.Serializable]
public class CharacterListWrapper
{
    public List<CharacterData> items;
}
