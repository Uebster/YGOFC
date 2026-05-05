# 🛡️ OCGCore Diagnostic Report V7.0

**Arquivos LUA de Cartas Escaneados:** 1498
**Arquivos C# API Integrados:** 10
**Bibliotecas LUA Base Escaneadas:** 25

**Pasta C# Base:** `C:\Users\uebst\YuGiOh_Forbidden_Chaos\Assets`
**Pasta LUA Varredura:** `C:\Users\uebst\YuGiOh_Forbidden_Chaos\Assets\Scripts\DMLuaScripts`

**Pasta Libs Base (SupportLua):** `C:\Users\uebst\YuGiOh_Forbidden_Chaos\Assets\SupportLua`

---
### 📂 Arquivos C# Escaneados
<details><summary>Clique para expandir</summary>

- `LuaCard.cs`
- `LuaDuel_Actions.cs`
- `LuaDuel_Core.cs`
- `LuaDuel_Queries.cs`
- `LuaDuel_Stubs.cs`
- `LuaDuel_UI.cs`
- `LuaEffect.cs`
- `LuaEngineCore.cs`
- `LuaGroup.cs`
- `LuaProcsAndStubs.cs`

</details>

### 📚 Bibliotecas LUA Escaneadas
<details><summary>Clique para expandir</summary>

- `archetype_setcode_constants.lua`
- `card_counter_constants.lua`
- `cards_specific_functions.lua`
- `constant.lua`
- `debug_utility.lua`
- `deprecated_functions.lua`
- `proc_equip.lua`
- `proc_fusion.lua`
- `proc_fusion_spell.lua`
- `proc_gemini.lua`
- `proc_link.lua`
- `proc_maximum.lua`
- `proc_normal.lua`
- `proc_pendulum.lua`
- `proc_persistent.lua`
- `proc_ritual.lua`
- `proc_rush.lua`
- `proc_skill.lua`
- `proc_spirit.lua`
- `proc_synchro.lua`
- `proc_union.lua`
- `proc_unofficial.lua`
- `proc_workaround.lua`
- `proc_xyz.lua`
- `utility.lua`

</details>

---

> Este relatório cruza chamadas feitas pelas cartas com as declarações presentes nas bibliotecas LUA e nos scripts C# do simulador.

## ❌ Faltando em `Duel` (0)
*Tudo perfeito!*


## ❌ Faltando em `Card` (0)
*Tudo perfeito!*


## ❌ Faltando em `Group` (0)
*Tudo perfeito!*


## ❌ Faltando em `Effect` (0)
*Tudo perfeito!*


## ❌ Faltando em `aux` (0)
*Tudo perfeito!*


## ❌ Faltando em `Fusion` (6)
- [ ] `public void AddContactProc() { }`
- [ ] `public void AddProcMix() { }`
- [ ] `public void AddProcMixN() { }`
- [ ] `public void RegisterSummonEff() { }`
- [ ] `public void SummonEffOP() { }`
- [ ] `public void SummonEffTG() { }`

## ❌ Faltando em `Synchro` (0)
*Tudo perfeito!*


## ❌ Faltando em `Xyz` (0)
*Tudo perfeito!*


## ❌ Faltando em `Link` (0)
*Tudo perfeito!*


## ❌ Faltando em `Ritual` (2)
- [ ] `public void AddProcEqual() { }`
- [ ] `public void AddProcGreaterCode() { }`

## ❌ Faltando em `Spirit` (1)
- [ ] `public void AddProcedure() { }`

## ❌ Faltando em `Pendulum` (0)
*Tudo perfeito!*


## ⚠️ Constantes LUA Ausentes (1)
> As constantes abaixo foram usadas nas cartas, mas não foram definidas via `luaEngine.Globals` no C# nem encontradas no `constant.lua`.

- [ ] `SET_HORUS_BLACK_FLAME_DRAGON`

