using UnityEngine;

public class CardLink : MonoBehaviour
{
    public CardDisplay source; // Carta que fornece o efeito
    public CardDisplay target; // Carta que recebe o efeito
    public LinkType type;

    public enum LinkType {
        Equipment, // Equip Spell: Se o monstro morrer, a spell é destruída.
        Continuous  // Call of the Haunted: Ambas dependem da existência uma da outra.
    }

    public void Initialize(CardDisplay source, CardDisplay target, LinkType type)
    {
        this.source = source;
        this.target = target;
        this.type = type;
    }
}
