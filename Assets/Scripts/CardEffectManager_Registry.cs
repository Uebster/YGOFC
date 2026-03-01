using UnityEngine;
using System.Collections.Generic;

public partial class CardEffectManager
{
    void InitializeEffects()
    {
        effectDatabase = new Dictionary<string, System.Action<CardDisplay>>();
        InitializeEffects_Part1();
        InitializeEffects_Part2();
        InitializeEffects_Part3();
        InitializeEffects_Part4();
        InitializeEffects_Part5();
    }
}
