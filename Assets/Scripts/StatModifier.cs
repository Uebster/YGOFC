using UnityEngine;

[System.Serializable]
public class StatModifier
{
    public enum StatType { ATK, DEF }
    public enum ModifierType { Base, Original, Equipment, Field, Temporary, Continuous }
    public enum Operation { Add, Set, Multiply }

    public string id;
    public StatType statType;
    public ModifierType type;
    public Operation operation;
    public int value;        // Para Add ou Set
    public float multiplier; // Para Multiply (ex: 2.0 para dobrar)
    public CardDisplay source; // A carta que gerou o efeito (para remover se ela sair de campo)
    public bool removeAtEndPhase; // Se true, expira no fim do turno

    // Construtor para Adição/Subtração ou Set
    public StatModifier(StatType stat, ModifierType type, Operation op, int val, CardDisplay src = null)
    {
        this.id = System.Guid.NewGuid().ToString();
        this.statType = stat;
        this.type = type;
        this.operation = op;
        this.value = val;
        this.source = src;
        this.multiplier = 1f;
        this.removeAtEndPhase = (type == ModifierType.Temporary);
    }

    // Construtor para Multiplicação
    public StatModifier(StatType stat, ModifierType type, float mult, CardDisplay src = null)
    {
        this.id = System.Guid.NewGuid().ToString();
        this.statType = stat;
        this.type = type;
        this.operation = Operation.Multiply;
        this.multiplier = mult;
        this.source = src;
        this.value = 0;
        this.removeAtEndPhase = (type == ModifierType.Temporary);
    }
}
