using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RitualManager : MonoBehaviour
{
    public static RitualManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Valida se os materiais de tributo são suficientes para o Monstro de Ritual.
    public bool ValidateRitual(CardData ritualSpell, CardData ritualMonster, List<CardData> selectedTributes)
    {
        if (ritualSpell == null || ritualMonster == null || !ritualMonster.type.Contains("Ritual") || selectedTributes == null)
        {
            return false;
        }

        // 1. Verifica se a Magia de Ritual corresponde ao Monstro de Ritual
        // Simplificação: A descrição da magia deve conter o nome do monstro.
        if (!ritualSpell.description.Contains(ritualMonster.name))
        {
            // Poderíamos ter uma exceção para rituais genéricos como "Contract with the Abyss"
            if (ritualSpell.id != "0325") // ID de Contract with the Abyss
            {
                 Debug.LogWarning($"A magia '{ritualSpell.name}' não é para '{ritualMonster.name}'.");
                 return false;
            }
        }

        // 2. Calcula o total de Níveis dos tributos
        int totalTributeLevels = 0;
        foreach (var tribute in selectedTributes)
        {
            totalTributeLevels += tribute.level;
        }

        // 3. Verifica se o Nível é suficiente
        if (totalTributeLevels < ritualMonster.level)
        {
            Debug.Log($"Nível de tributo insuficiente. Necessário: {ritualMonster.level}, Oferecido: {totalTributeLevels}");
            return false;
        }

        return true; // Todos os critérios foram atendidos
    }
}
