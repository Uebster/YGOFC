using UnityEngine;
using UnityEngine.UI;
using System;

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
        if (atkDefPanel != null) atkDefPanel.SetActive(false);
        if (attributePanel != null) attributePanel.SetActive(false);
        if (racePanel != null) racePanel.SetActive(false);
        gameObject.SetActive(false);
        onSelectionMade?.Invoke(value);
    }
}
