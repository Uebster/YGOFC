using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class LuaScriptLoader
{
    public static LuaCard EnsureCardScriptLoaded(CardDisplay card, Script luaEngine, Dictionary<CardDisplay, LuaCard> activeLuaCards)
    {
        if (card == null || card.CurrentCardData == null) return null;
        if (activeLuaCards.ContainsKey(card)) return activeLuaCards[card];
        
        string cardId = card.CurrentCardData.id;
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        
        if (!System.IO.File.Exists(scriptPath)) return null;

        try
        {
            DynValue selfTable = DynValue.NewTable(luaEngine);
            int numericId = 0;
            
            if (!string.IsNullOrEmpty(card.CurrentCardData.password))
                int.TryParse(card.CurrentCardData.password, out numericId);
            else
            {
                string digits = Regex.Replace(card.CurrentCardData.id, @"\D", "");
                if (!string.IsNullOrEmpty(digits)) int.TryParse(digits, out numericId);
            }
            
            DynValue cachedTable = luaEngine.Globals.Get("c" + numericId);
            bool alreadyLoaded = !cachedTable.IsNil() && cachedTable.Type == DataType.Table;

            if (!alreadyLoaded)
            {
                selfTable = DynValue.NewTable(luaEngine);
                luaEngine.Globals["self_table"] = selfTable;
                luaEngine.Globals["self_code"] = numericId;
                luaEngine.Globals["c" + numericId] = selfTable;
                luaEngine.Globals[cardId] = selfTable;

                string scriptCode = System.IO.File.ReadAllText(scriptPath);
                scriptCode = SanitizeOCGScript(scriptCode);
                luaEngine.DoString(scriptCode);
            }
            else
            {
                selfTable = cachedTable;
                luaEngine.Globals["self_table"] = selfTable;
                luaEngine.Globals["self_code"] = numericId;
            }

            DynValue initialEffect = selfTable.Table.Get("initial_effect");
            if (!initialEffect.IsNil())
            {
                LuaCard luaCard = new LuaCard(card);
                luaEngine.Call(initialEffect, luaCard);
                activeLuaCards[card] = luaCard;
                return luaCard;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA CRASH] Ocorreu uma falha no carregamento do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    public static LuaCard LoadScriptForData(CardData data, Script luaEngine)
    {
        if (data == null) return null;
        string cardId = data.id;
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        if (!System.IO.File.Exists(scriptPath)) return null;

        try
        {
            DynValue selfTable = DynValue.NewTable(luaEngine);
            int numericId = 0;
            
            if (!string.IsNullOrEmpty(data.password))
                int.TryParse(data.password, out numericId);
            else
            {
                string digits = Regex.Replace(data.id, @"\D", "");
                if (!string.IsNullOrEmpty(digits)) int.TryParse(digits, out numericId);
            }
            
            DynValue cachedTable = luaEngine.Globals.Get("c" + numericId);
            bool alreadyLoaded = !cachedTable.IsNil() && cachedTable.Type == DataType.Table;

            if (!alreadyLoaded)
            {
                selfTable = DynValue.NewTable(luaEngine);
                luaEngine.Globals["self_table"] = selfTable;
                luaEngine.Globals["self_code"] = numericId;
                luaEngine.Globals["c" + numericId] = selfTable;
                luaEngine.Globals[cardId] = selfTable;

                string scriptCode = System.IO.File.ReadAllText(scriptPath);
                scriptCode = SanitizeOCGScript(scriptCode);
                luaEngine.DoString(scriptCode);
            }
            else
            {
                selfTable = cachedTable;
                luaEngine.Globals["self_table"] = selfTable;
                luaEngine.Globals["self_code"] = numericId;
            }

            DynValue initialEffect = selfTable.Table.Get("initial_effect");
            if (!initialEffect.IsNil())
            {
                LuaCard luaCard = new LuaCard(data);
                luaEngine.Call(initialEffect, luaCard);
                return luaCard;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API LUA CRASH] Ocorreu uma falha no carregamento fantasma do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    private static string SanitizeOCGScript(string script)
    {
        // 0. Remove comentários para evitar falsos positivos
        script = Regex.Replace(script, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);
        script = Regex.Replace(script, @"--.*", "");

        // Padrão Avançado: Captura variáveis, arrays e métodos sem Catastrophic Backtracking
        string parens = @"\((?>[^()]+|\((?<DEPTH>)|\)(?<-DEPTH>))*(?(DEPTH)(?!))\)";
        string brackets = @"\[(?>[^\[\]]+|\[(?<DEPTH>)|\](?<-DEPTH>))*(?(DEPTH)(?!))\]";
        string curlies = @"\{(?>[^{}]+|\{(?<DEPTH>)|}(?<-DEPTH>))*(?(DEPTH)(?!))\}";
        string termPattern = $@"(?:[\w_]+|{parens}|{brackets}|{curlies})(?:\s*[\.\:]\s*[\w_]+|\s*{parens}|\s*{brackets}|\s*{curlies})*";

        // 1. Substitui o Bitwise NOT unário (~)
        script = Regex.Replace(script, $@"~(?!=)\s*({termPattern})", "bit32.bnot($1)");

        // 2. Divisão Inteira (//) para math.floor()
        string prev = "";
        while (script != prev) { prev = script; script = Regex.Replace(script, $@"({termPattern})\s*//\s*({termPattern})", "math.floor($1 / $2)"); }

        // 3. Bitwise Shifts (<< e >>)
        prev = "";
        while (script != prev) { prev = script; script = Regex.Replace(script, $@"({termPattern})\s*<<\s*({termPattern})", "bit32.lshift($1, $2)"); }
        prev = "";
        while (script != prev) { prev = script; script = Regex.Replace(script, $@"({termPattern})\s*>>\s*({termPattern})", "bit32.rshift($1, $2)"); }

        // 4. Bitwise AND (&)
        prev = "";
        while (script != prev) { prev = script; script = Regex.Replace(script, $@"({termPattern})\s*&\s*({termPattern})", "bit32.band($1, $2)"); }

        // 5. Bitwise OR (|)
        prev = "";
        while (script != prev) { prev = script; script = Regex.Replace(script, $@"({termPattern})\s*\|\s*({termPattern})", "bit32.bor($1, $2)"); }

        // 6. Operador de Comprimento (#) para userdata
        script = Regex.Replace(script, @"#\s*([a-zA-Z_][a-zA-Z0-9_]*)", "(type($1)=='userdata' and $1:GetCount() or #$1)");

        // 7. Loop Direto sobre Userdata (Iterador)
        script = Regex.Replace(script, @"for\s+([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_]+)\s+do", "for $1 in $2:Iter() do");

        // 8. OCGCore Compatibility: Mathematical Type Sums & Methods
        // O C# não entende a soma de bits (TYPE_SPELL + TYPE_TRAP). Redireciona para o LUA nativo inteligente.
        if (script.Contains("TYPE_SPELL") || script.Contains("TYPE_TRAP") || script.Contains("IsSpellTrap"))
        {
            // Padrão 1: Soma Tradicional (O que já tínhamos)
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*TYPE_SPELL\s*\+\s*TYPE_TRAP\s*\)", "Card.IsSpellTrap($1)");
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*TYPE_TRAP\s*\+\s*TYPE_SPELL\s*\)", "Card.IsSpellTrap($1)");
            
            // Padrão 2: Soma Bitwise (Usado em cartas complexas)
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*bit32\.bor\(\s*TYPE_SPELL\s*,\s*TYPE_TRAP\s*\)\s*\)", "Card.IsSpellTrap($1)");
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*bit32\.bor\(\s*TYPE_TRAP\s*,\s*TYPE_SPELL\s*\)\s*\)", "Card.IsSpellTrap($1)");
            
            // Padrão 3: Lógica OR separada (O sotaque que causou o bug de hoje)
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*TYPE_SPELL\s*\)\s*or\s*\1:IsType\(\s*TYPE_TRAP\s*\)", "Card.IsSpellTrap($1)");
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsType\(\s*TYPE_TRAP\s*\)\s*or\s*\1:IsType\(\s*TYPE_SPELL\s*\)", "Card.IsSpellTrap($1)");
            
            script = Regex.Replace(script, @"([a-zA-Z0-9_]+):IsSpellTrap\(\)", "Card.IsSpellTrap($1)");
        }

        return script;
    }
}