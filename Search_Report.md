# 🔍 Relatório de Busca de Código

**Termo pesquisado:** `StartDirectSelection`
**Pasta base:** `C:\Users\uebst\YuGiOh_Forbidden_Chaos\Assets`
**Total de ocorrências:** 7

---

### 📄 `Assets\Scripts\GameManager_BoardActions.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 1324:**
```csharp
StartDirectSelection(validMaterials, 2, 5, fusionValidator, $"Selecione os Materiais para {targetFusion.name}", (selectedMaterials) => {
```

### 📄 `Assets\Scripts\GameManager_Selections.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 187:**
```csharp
StartDirectSelection(sourceList, min, max, null, title, onSelected, category, canCancel);
```

### 📄 `Assets\Scripts\GameManager_Selections.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 214:**
```csharp
private void StartDirectSelection(List<CardData> candidates, int min, int max, System.Func<List<CardData>, bool> validator, string title, System.Action<List<CardData>> callback, HighlightCategory category = HighlightCategory.GenericTarget, bool canCancel = true)
```

### 📄 `Assets\Scripts\GameManager_Summons.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 282:**
```csharp
StartDirectSelection(possibleTributes, tributes, tributes, null, $"Selecione {tributes} Tributo(s) para {cardData.name}", (selected) => {
```

### 📄 `Assets\Scripts\GameManager_Summons.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 624:**
```csharp
StartDirectSelection(possibleRituals, 1, 1, null, "Selecione o Monstro de Ritual", (rList) => {
```

### 📄 `Assets\Scripts\GameManager_Summons.cs`
- **Contexto/Função:** `StartDirectSelection`
- **Linha 683:**
```csharp
StartDirectSelection(validTributes, 1, 5, tributeValidator, $"Selecione os Tributos para {targetRitual.name}", (selectedTributes) => {
```

### 📄 `Assets\Scripts\LuaAPI\LuaGroup.cs`
- **Contexto/Função:** `OpenSumSelectionUI`
- **Linha 290:**
```csharp
GameManager.Instance.StartDirectSelection(validCards, minCount, maxCount, sumValidator, $"Selecione Tributos (Soma {(exactMath ? "exata de" : "mínima de")} {targetSum})", (selected) => {
```

