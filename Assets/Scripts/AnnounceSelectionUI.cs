using UnityEngine;
using UnityEngine.UI;
using System;

// ==============================================================================
// VALORES PARA O INSPECTOR DA UNITY (Botões OnClick -> SelectValue)
//
// ATK/DEF: 704 = ATK | 705 = DEF
// ATRIBUTOS: 1=EARTH, 2=WATER, 4=FIRE, 8=WIND, 16=DARK, 32=LIGHT, 64=DIVINE
// RAÇAS:
// 1=Warrior, 2=Spellcaster, 4=Fairy, 8=Fiend, 16=Zombie, 32=Machine, 64=Aqua
// 128=Pyro, 256=Rock, 512=Winged Beast, 1024=Plant, 2048=Insect, 4096=Thunder
// 8192=Dragon, 16384=Beast, 32768=Beast-Warrior, 65536=Dinosaur, 131072=Fish
// 262144=Sea Serpent, 524288=Reptile, 1048576=Psychic, 2097152=Divine
// ==============================================================================

public class AnnounceSelectionUI : MonoBehaviour
{
    public static AnnounceSelectionUI Instance;

    [Header("Painéis")]
    public GameObject atkDefPanel;    // Painel de Escolher ATK/DEF
    public GameObject attributePanel; // Painel de Atributos
    public GameObject racePanel;      // Painel de Raças

    private Action<int> onSelectionMade;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void ShowAtkDefSelection(Action<int> callback)
    {
        onSelectionMade = callback;
        if (attributePanel != null) attributePanel.SetActive(false);
        if (racePanel != null) racePanel.SetActive(false);
        if (atkDefPanel != null) atkDefPanel.SetActive(true);
        gameObject.SetActive(true);
    }

    public void ShowAttributeSelection(Action<int> callback)
    {
        onSelectionMade = callback;
        if (atkDefPanel != null) atkDefPanel.SetActive(false);
        if (racePanel != null) racePanel.SetActive(false);
        if (attributePanel != null) attributePanel.SetActive(true);
        gameObject.SetActive(true);
    }

    public void ShowRaceSelection(Action<int> callback)
    {
        onSelectionMade = callback;
        if (atkDefPanel != null) atkDefPanel.SetActive(false);
        if (attributePanel != null) attributePanel.SetActive(false);
        if (racePanel != null) racePanel.SetActive(true);
        gameObject.SetActive(true);
    }

    // Você vai colocar essa função no OnClick() de CADA BOTÃO dos painéis lá no Inspector
    // E no campo que aparecer, você digita o valor correspondente (tabela abaixo)
    public void SelectValue(int value)
    {
        Debug.Log($"<color=yellow>[UI] Seleção Visual (Announce) clicada! Valor enviado: {value}</color>");

        if (atkDefPanel != null) atkDefPanel.SetActive(false);
        if (attributePanel != null) attributePanel.SetActive(false);
        if (racePanel != null) racePanel.SetActive(false);
        gameObject.SetActive(false);
        onSelectionMade?.Invoke(value);
    }
}
