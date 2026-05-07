using UnityEngine;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

// ==============================================================================
// CLASSE SCRIPT LOADER (O Compilador e Sanitizador)
// Onde a Mágica Acontece: Lê os arquivos .lua do disco e os converte para a Unity.
// Tratativas Críticas & Dependências: 
// - Regex: O OCGCore usa Bitwise Operators do Lua 5.3 (>>, <<, ~, |). O MoonSharp (Lua 5.2) não suporta.
//   O Loader sanitiza e converte tudo on-the-fly usando bit32 e math.
// - Cache RAM: Mantém um dicionário de scripts já carregados (activeLuaCards) para performance.
// ==============================================================================
public static class LuaScriptLoader
{
    public static LuaCard EnsureCardScriptLoaded(CardDisplay card, Script luaEngine, Dictionary<CardDisplay, LuaCard> activeLuaCards)
    {
        if (card == null || card.CurrentCardData == null) return null;
        if (activeLuaCards.ContainsKey(card)) return activeLuaCards[card];
        
        string cardId = card.CurrentCardData.id;
        
        // 1. Extrai a Era dinamicamente do ID (ex: "DM" de "DM0001")
        string prefix = new string(cardId.TakeWhile(char.IsLetter).ToArray());
        if (string.IsNullOrEmpty(prefix)) prefix = "DM";
        
        string folderName = $"{prefix}LuaScripts";
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", folderName, $"c{cardId}.lua");

        // 2. Fallback de Segurança 1: Pasta Fixa DM
        if (!System.IO.File.Exists(scriptPath))
            scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "DMLuaScripts", $"c{cardId}.lua");

        // 3. Fallback de Segurança 2: Pasta Antiga Clássica
        if (!System.IO.File.Exists(scriptPath))
            scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
        
        if (!System.IO.File.Exists(scriptPath)) 
        {
            // FIX: Cacheia Monstros Normais e Tokens na RAM para que eles possam reter Efeitos Temporários!
            LuaCard emptyCard = new LuaCard(card);
            activeLuaCards[card] = emptyCard;
            return emptyCard;
        }

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
                luaEngine.Globals["c" + cardId] = selfTable; // FIX: Cria a tabela OCGCore Exata com Letras (ex: cDM1410)

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
                activeLuaCards[card] = luaCard; // FIX: Cacheia antes de invocar o initial_effect para prevenir Stack Overflow (Loop Infinito)!
                luaEngine.Call(initialEffect, luaCard);
                return luaCard;
            }
        }
        catch (InterpreterException ex)
        {
            Debug.LogWarning($"[API LUA CRASH] Falha interna no script c{cardId}.lua:\n{ex.DecoratedMessage}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[API LUA CRASH] Ocorreu uma falha no carregamento do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    public static LuaCard LoadScriptForData(CardData data, Script luaEngine)
    {
        if (data == null) return null;
        string cardId = data.id;
        
        // 1. Extrai a Era dinamicamente do ID
        string prefix = new string(cardId.TakeWhile(char.IsLetter).ToArray());
        if (string.IsNullOrEmpty(prefix)) prefix = "DM";
        
        string folderName = $"{prefix}LuaScripts";
        string scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", folderName, $"c{cardId}.lua");

        // 2. Fallback de Segurança 1: Pasta Fixa DM
        if (!System.IO.File.Exists(scriptPath))
            scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "DMLuaScripts", $"c{cardId}.lua");

        // 3. Fallback de Segurança 2: Pasta Antiga Clássica
        if (!System.IO.File.Exists(scriptPath))
            scriptPath = System.IO.Path.Combine(Application.dataPath, "Scripts", "LuaScripts", $"c{cardId}.lua");
            
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
                luaEngine.Globals["c" + cardId] = selfTable; // FIX: Cria a tabela OCGCore Exata com Letras (ex: cDM1410)

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
        catch (InterpreterException ex)
        {
            Debug.LogWarning($"[API LUA CRASH] Falha interna no script c{cardId}.lua:\n{ex.DecoratedMessage}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[API LUA CRASH] Ocorreu uma falha no carregamento fantasma do arquivo c{cardId}.lua:\n{ex.Message}");
        }
        
        return null;
    }

    public static string SanitizeOCGScript(string script)
    {
        // 0. Remove comentários para evitar falsos positivos
        script = Regex.Replace(script, @"--\[\[.*?\]\]", "", RegexOptions.Singleline);
        script = Regex.Replace(script, @"--.*", "");

        // 0.5. Protege as strings literais para que o conversor não altere símbolos dentro de textos!
        List<string> stringLiterals = new List<string>();
        script = Regex.Replace(script, @"""(?:\\.|[^""])*""|'(?:\\.|[^'])*'|\[\[[\s\S]*?\]\]", match => {
            stringLiterals.Add(match.Value);
            return $"__STR_LITERAL_{stringLiterals.Count - 1}__";
        });

        // 1. OCGCore Compatibility: Mathematical Type Sums & Methods
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

        // Otimização extrema de Regex: Evita a Catastrophic Backtracking substituindo o laço "while" pesado.
        // O MoonSharp processa apenas o que sobrou.
        if (script.Contains("<<") || script.Contains(">>") || script.Contains("~") || script.Contains("&") || script.Contains("|") || script.Contains("//"))
        {
            // Expressão Regular Poderosa para capturar variáveis, números, parênteses balanceados e cadeias de métodos OCGCore (ex: e:GetHandler():GetCode())
            string balancedParens = @"\((?>[^()]+|\((?<DEPTH>)|\)(?<-DEPTH>))*(?(DEPTH)(?!))\)";
            string termBase = $@"(?:[\w_]+|{balancedParens})";
            string termModifier = $@"(?:[\.:][\w_]+|\s*{balancedParens})";
            string term = $@"(?:{termBase}{termModifier}*)";

            script = Regex.Replace(script, $@"(?<![\w_\]\)]\s*)-\s*(\d+)\s*(?=(?:<<|>>|&|\||~))", "(-$1)");

            // Aplica múltiplas passadas priorizadas para resolver encadeamentos como A | (B & ~C) perfeitamente
            string prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*~(?!=)\s*({term})", "bit32.bxor($1, $2)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"~(?!=)\s*({term})", "bit32.bnot($1)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*//\s*({term})", "math.floor($1 / $2)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*<<\s*({term})", "bit32.lshift($1, $2)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*>>\s*({term})", "bit32.rshift($1, $2)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*&\s*({term})", "bit32.band($1, $2)"); }
            prev = "";
            while (script != prev) { prev = script; script = Regex.Replace(script, $@"({term})\s*\|\s*({term})", "bit32.bor($1, $2)"); }
        }

        // Loop Direto
        if (script.Contains("for ") && script.Contains(" in "))
        {
            script = Regex.Replace(script, @"for\s+([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_]+)\s+do", "for $1 in $2:Iter() do");
        }

        // Fim: Restaura as strings literais protegidas intactas
        for (int i = 0; i < stringLiterals.Count; i++) {
            script = script.Replace($"__STR_LITERAL_{i}__", stringLiterals[i]);
        }

        return script;
    }
}