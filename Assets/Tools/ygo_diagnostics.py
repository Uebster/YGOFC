from flask import Flask, render_template_string, request, jsonify
import os
import re
from pathlib import Path
import threading
import time

app = Flask(__name__)

# Controle de progresso global
progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}
cancel_task = False
report_file_path = ""

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>YGOCore Diagnostic & Mapper Web</title>
    <style>
        :root {
            --cyan: #00e5ff;
            --purple: #b537f2;
            --red: #ff073a;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; padding: 40px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--purple); padding: 35px; 
            border-radius: 20px; width: 800px; box-shadow: 0 0 40px rgba(181, 55, 242, 0.15);
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 35px;
            color: var(--purple); text-shadow: 0 0 10px var(--purple), 0 0 20px rgba(181, 55, 242, 0.5);
        }
        label { font-size: 0.9em; color: #ccc; font-weight: bold; display: block; margin-bottom: 6px; }
        .form-group { margin-bottom: 20px; }
        
        input[type="text"] { 
            width: 100%; padding: 12px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 10px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input:focus { outline: none; border-color: var(--purple); box-shadow: 0 0 12px rgba(181, 55, 242, 0.4); }
        
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 12px; padding: 14px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; display: flex; justify-content: center; align-items: center; text-align: center;
        }
        .action-btn:hover { color: #000 !important; text-shadow: none; transform: translateY(-2px); }

        .purple-btn { color: var(--purple); border-color: var(--purple); box-shadow: inset 0 0 8px rgba(181,55,242,0.3); text-shadow: 0 0 5px var(--purple); }
        .purple-btn:hover { background: var(--purple); box-shadow: inset 0 0 20px var(--purple); }

        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan); }

        .red-btn { color: var(--red); border-color: var(--red); box-shadow: inset 0 0 8px rgba(255,7,58,0.3); text-shadow: 0 0 5px var(--red); }
        .red-btn:hover { background: var(--red); box-shadow: inset 0 0 20px var(--red); color: #fff !important; }

        .browse { flex: 0 0 auto; width: auto; padding: 0 20px;}
        .btn-grid { display: flex; gap: 15px; margin-top: 25px;}

        .progress-container { background: #050505; border: 1px solid var(--purple); height: 22px; border-radius: 11px; overflow: hidden; box-shadow: 0 0 10px rgba(181,55,242,0.2); margin-top: 20px; }
        .progress-bar { width: 0%; height: 100%; background: var(--purple); box-shadow: 0 0 15px var(--purple); transition: width 0.3s ease; }
        
        #info { text-align: center; color: #aaa; font-size: 0.9em; margin: 10px 0 0 0; font-style: italic;}
        
        #log { 
            margin-top: 15px; background: #050505; border: 1px solid #222; padding: 15px; 
            border-radius: 12px; overflow-y: auto; color: var(--cyan); font-family: 'Consolas', monospace; 
            font-size: 0.85em; box-shadow: inset 0 0 20px rgba(0,0,0,1); height: 250px;
        }
        #log div { margin-bottom: 4px; line-height: 1.4;}
        
        ::-webkit-scrollbar { width: 8px; }
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--purple); box-shadow: 0 0 10px var(--purple); }
    </style>
</head>
<body>
    <div class="panel-container">
        <h1 class="neon-title">Diagnostic & Mapper</h1>

        <div class="form-group">
            <label>1. Projeto Unity (Pasta raiz do C#)</label>
            <div class="input-group">
                <input type="text" id="project_dir" placeholder="Ex: C:/YuGiOh_Forbidden_Chaos">
                <button class="action-btn cyan-btn browse" onclick="selectFolder('project_dir')">Procurar</button>
            </div>
        </div>

        <div class="form-group">
            <label>2. Scripts LUA (Pasta contendo as cartas .lua)</label>
            <div class="input-group">
                <input type="text" id="lua_dir" placeholder="Ex: C:/YuGiOh_Forbidden_Chaos/Assets/Scripts/LuaScripts">
                <button class="action-btn cyan-btn browse" onclick="selectFolder('lua_dir')">Procurar</button>
            </div>
        </div>

        <div class="form-group">
            <label>3. Bibliotecas LUA (Pasta SupportLua) - Opcional</label>
            <div class="input-group">
                <input type="text" id="support_dir" placeholder="Ex: C:/YuGiOh_Forbidden_Chaos/Assets/StreamingAssets/SupportLua">
                <button class="action-btn cyan-btn browse" onclick="selectFolder('support_dir')">Procurar</button>
            </div>
        </div>

        <div class="btn-grid">
            <button class="action-btn purple-btn" style="flex: 1;" onclick="startTask()">🚀 INICIAR AUDITORIA</button>
            <button class="action-btn red-btn" style="flex: 0 0 auto; padding: 0 25px;" onclick="cancelProcess()">Parar</button>
            <button class="action-btn green-btn" id="btn_open" style="flex: 0 0 auto; padding: 0 25px; display: none;" onclick="openReport()">📄 Abrir Report</button>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info">Sistema Ocioso e Pronto.</p>
        
        <div id="log">
            <div style="color: var(--purple);">[ OCGCORE DIAGNOSTIC WEB (TRUE GLOBAL LINTER - PATCHED) ]</div>
            <div>- Interface Web Ativada</div>
            <div>- Motor de Análise Linha a Linha Pronto</div>
            <div>- Verificador de Dependências (LoadScript) Ativado</div>
            <div>Aguardando seleção de pastas...</div>
        </div>
    </div>

    <script>
        let interval;

        function selectFolder(inputId) {
            fetch('/select_folder').then(r => r.json()).then(d => {
                if(d.folder) document.getElementById(inputId).value = d.folder;
            });
        }

        function startTask() {
            document.getElementById('btn_open').style.display = 'none';
            const data = {
                project_dir: document.getElementById('project_dir').value,
                lua_dir: document.getElementById('lua_dir').value,
                support_dir: document.getElementById('support_dir').value
            };
            fetch('/start', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) });
            if(interval) clearInterval(interval);
            interval = setInterval(update, 500);
        }

        function cancelProcess() {
            fetch('/cancel', { method: 'POST' });
        }

        function openReport() {
            fetch('/open_report', { method: 'POST' });
        }

        function update() {
            fetch('/status').then(r => r.json()).then(d => {
                const perc = (d.atual / d.total) * 100 || 0;
                document.getElementById('bar').style.width = perc + '%';
                
                let isTerminal = d.status === "Finalizado!" || d.status === "Cancelado" || d.status.startsWith("Erro:");
                if (isTerminal) {
                    document.getElementById('info').innerText = d.status;
                    if (d.status === "Finalizado!") {
                        document.getElementById('btn_open').style.display = 'flex';
                    }
                    clearInterval(interval);
                } else {
                    document.getElementById('info').innerText = d.card ? `Processando: ${d.card}` : d.status;
                }

                if (d.log.length > 0) {
                    const logDiv = document.getElementById('log');
                    logDiv.innerHTML = d.log.map(line => `<div>${line}</div>`).join('');
                    logDiv.scrollTop = logDiv.scrollHeight;
                }
            });
        }
    </script>
</body>
</html>
"""

def audit_executor(project_dir, lua_dir, support_dir):
    global progresso, cancel_task, report_file_path
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Iniciando Auditoria...", "card": "", "log": []}
    
    if not project_dir or not lua_dir:
        progresso["status"] = "Erro: Selecione ambas as pastas."
        return
        
    assets_dir = Path(project_dir) / "Assets"
    lua_cards_dir = Path(lua_dir)
    
    if not assets_dir.exists():
        if Path(project_dir).name == "Assets":
            assets_dir = Path(project_dir)
        else:
            progresso["status"] = f"Erro: Pasta Assets não encontrada em {project_dir}."
            return

    support_lua_dir = Path(support_dir) if support_dir else None

    progresso["log"].append("=== INICIANDO AUDITORIA PROFUNDA ===")
    progresso["log"].append(f"-> Escaneando Engine C# em: {assets_dir}")
    progresso["log"].append(f"-> Escaneando Cartas LUA em: {lua_cards_dir}")
    if support_lua_dir and support_lua_dir.exists():
        progresso["log"].append(f"-> Escaneando Libs OCGCore em: {support_lua_dir}")
    progresso["log"].append("")
    
    cs_methods = {"Duel": set(), "Card": set(), "Group": set(), "Effect": set(), "aux": set()}
    lua_calls = {"Duel": set(), "Card": set(), "Group": set(), "Effect": set(), "aux": set()}
    
    defined_constants = set()
    used_constants = set()
    linter_errors = []
    
    available_lua_files = set()
    load_script_requests = {}
    library_exported_globals = set()
    missing_libs = {}

    scanned_cs_files = set()
    scanned_lib_files = set()

    # Regex mais inteligente e permissivo para capturar métodos C#
    method_pattern = re.compile(r'public\s+(?:static\s+|virtual\s+|override\s+|new\s+)*[\w<>\[\]]+\s+([A-Z][a-zA-Z0-9_]*)\s*\(')
    
    # Dicionário de classes extras que o Lua usa nativamente
    extra_lua_classes = ["Fusion", "Synchro", "Xyz", "Link", "Ritual", "Spirit", "Pendulum"]
    for cls in extra_lua_classes:
        cs_methods[cls] = set()
        lua_calls[cls] = set()

    for cs_file in assets_dir.rglob("*.cs"):
        if cancel_task: return
        try:
            used_file = False
            fname = cs_file.name
            cat = None
            
            # Identificação por Nome de Arquivo (Blindado)
            if fname.startswith("LuaDuel"): cat = "Duel"
            elif fname.startswith("LuaCard"): cat = "Card"
            elif fname.startswith("LuaGroup"): cat = "Group"
            elif fname.startswith("LuaEffect"): cat = "Effect"
            
            if cat:
                used_file = True
                progresso["log"].append(f"[C#] Lendo API: {cs_file.name} -> {cat}")
                if len(progresso["log"]) > 60: progresso["log"].pop(0)
                
                content = cs_file.read_text(encoding='utf-8', errors='ignore')
                
                for match in re.finditer(r'(?:Duel|luaDuel)\.LoadScript\(\s*["\']([^"\']+\.lua)["\']\s*\)', content):
                    req_file = match.group(1)
                    if req_file not in load_script_requests: load_script_requests[req_file] = set()
                    load_script_requests[req_file].add(cs_file.name)
                
                for match in method_pattern.finditer(content):
                    cs_methods[cat].add(match.group(1))
                    
            elif fname in ["LuaEngineCore.cs", "LuaUtilityBridge.cs", "LuaProcsAndStubs.cs"]:
                used_file = True
                progresso["log"].append(f"[C#] Lendo Core/Stubs: {fname}")
                content = cs_file.read_text(encoding='utf-8', errors='ignore')
                
                # Captura registros LUA via string no C# e funções estáticas de Procs
                for match in re.finditer(r'auxTable\.Table\.Set\(\s*"([a-zA-Z0-9_]+)"', content):
                    cs_methods["aux"].add(match.group(1))
                    
                for match in re.finditer(r'Globals\["([A-Z0-9_]+)"\]', content):
                    defined_constants.add(match.group(1))
                    
                for match in re.finditer(r'Globals\["([A-Z][a-zA-Z0-9_]*)"\]\s*=\s*.*?new\s+PythonAnalyzerStubs', content):
                    cls_name = match.group(1)
                    if cls_name in cs_methods:
                        for m in method_pattern.finditer(content): cs_methods[cls_name].add(m.group(1))
            
            if used_file:
                scanned_cs_files.add(fname)
        except Exception as e:
            progresso["log"].append(f"[ERRO] Falha ao ler {cs_file.name}: {e}")

    if support_lua_dir and support_lua_dir.exists():
        progresso["log"].append("\n-> Escaneando Bibliotecas Base (SupportLua)...")
        for lua_file in support_lua_dir.rglob("*.lua"):
            if cancel_task: return
            try:
                scanned_lib_files.add(lua_file.name)
                available_lua_files.add(lua_file.name)
                content = lua_file.read_text(encoding='utf-8', errors='ignore')
                
                # Captura tabelas globais exportadas e namespaces (Ex: Fusion = {} ou function Ritual.AddProc)
                for m in re.finditer(r'^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*\{', content, re.MULTILINE):
                    library_exported_globals.add(m.group(1))
                for m in re.finditer(r'function\s+([a-zA-Z_][a-zA-Z0-9_]*)\.', content):
                    library_exported_globals.add(m.group(1))
                
                for match in re.finditer(r'Duel\.LoadScript\(\s*["\']([^"\']+\.lua)["\']\s*\)', content):
                    req_file = match.group(1)
                    if req_file not in load_script_requests: load_script_requests[req_file] = set()
                    load_script_requests[req_file].add(lua_file.name)
                
                # Padrão amplo para capturar: function aux.nome() E aux.nome = function()
                aux_funcs = set(re.findall(r'function\s+aux\.([a-zA-Z0-9_]+)', content))
                aux_funcs.update(re.findall(r'aux\.([a-zA-Z0-9_]+)\s*=\s*(?:function|{)', content))
                
                if aux_funcs:
                    progresso["log"].append(f"[LUA-LIB] {lua_file.name} forneceu {len(aux_funcs)} funções aux.")
                    cs_methods["aux"].update(aux_funcs)
                
                const_matches = re.findall(r'^\s*([A-Z][A-Z0-9_]{2,})\s*=', content, re.MULTILINE)
                defined_constants.update(const_matches)

                # NOVO: Escaneia constantes USADAS dentro das bibliotecas para não deixar passar NADA!
                for m in re.finditer(r'\b([A-Z_][A-Z0-9_]{2,})\b', content):
                    used_constants.add(m.group(1))
            except: pass
    else:
        progresso["log"].append("\n-> Nenhuma pasta de Bibliotecas selecionada. Buscando libs espalhadas...")
        for lua_file in assets_dir.rglob("*.lua"):
            if cancel_task: return
            if lua_file.name.startswith("c") and len(lua_file.name) > 2 and lua_file.name[1].isdigit(): continue 
            try:
                scanned_lib_files.add(lua_file.name)
                available_lua_files.add(lua_file.name)
                content = lua_file.read_text(encoding='utf-8', errors='ignore')
                aux_funcs = set(re.findall(r'function\s+aux\.([a-zA-Z0-9_]+)', content))
                aux_funcs.update(re.findall(r'aux\.([a-zA-Z0-9_]+)\s*=\s*(?:function|{)', content))
                if aux_funcs: cs_methods["aux"].update(aux_funcs)
                const_matches = re.findall(r'^\s*([A-Z][A-Z0-9_]{2,})\s*=', content, re.MULTILINE)
                defined_constants.update(const_matches)

                # NOVO: Escaneia constantes USADAS dentro das bibliotecas para não deixar passar NADA!
                for m in re.finditer(r'\b([A-Z_][A-Z0-9_]{2,})\b', content):
                    used_constants.add(m.group(1))
                    
                for match in re.finditer(r'Duel\.LoadScript\(\s*["\']([^"\']+\.lua)["\']\s*\)', content):
                    req_file = match.group(1)
                    if req_file not in load_script_requests: load_script_requests[req_file] = set()
                    load_script_requests[req_file].add(lua_file.name)
            except: pass

    progresso["log"].append(f"\n-> Analisando scripts de cartas em: {lua_cards_dir}...")
    
    lua_files = list(lua_cards_dir.rglob("*.lua"))
    target_files = [f for f in lua_files if f.name.startswith("c") or f.name.startswith("DM")]
    total_files = len(target_files)
    progresso["total"] = total_files

    current_file = 0
    for lua_file in target_files:
        if cancel_task: return
        current_file += 1
        progresso["atual"] = current_file
        progresso["card"] = lua_file.name
        
        if current_file % 150 == 0:
            progresso["log"].append(f"[LUA-CARDS] Analisados {current_file}/{total_files} arquivos...")
            if len(progresso["log"]) > 60: progresso["log"].pop(0)
            
        try:
            content = lua_file.read_text(encoding='utf-8', errors='ignore')
            available_lua_files.add(lua_file.name)
            
            for match in re.finditer(r'Duel\.LoadScript\(\s*["\']([^"\']+\.lua)["\']\s*\)', content):
                req_file = match.group(1)
                if req_file not in load_script_requests: load_script_requests[req_file] = set()
                load_script_requests[req_file].add(lua_file.name)
            
            # Identifica constantes locais criadas na própria carta (Ex: ATTRIBUTE_EARTH_WATER_FIRE)
            local_consts = set(re.findall(r'^\s*(?:local\s+)?([A-Z_][A-Z0-9_]+)\s*=', content, re.MULTILINE))
            
            # TRUE GLOBAL LINTER: Analisador Linha-a-Linha de Escopo Completo
            lines = content.split('\n')
            lua_keywords = {'and', 'break', 'do', 'else', 'elseif', 'end', 'false', 'for', 'function', 'if', 'in', 'local', 'nil', 'not', 'or', 'repeat', 'return', 'then', 'true', 'until', 'while', 'goto'}
            safe_globals = {'Duel', 'Card', 'Effect', 'Group', 'Auxiliary', 'aux', 'bit', 'bit32', 'math', 'string', 'table', 'coroutine', 'os', 'Debug', 'GetID', 'Cost', 'Core', 'Log', 'ipairs', 'pairs', 'type', 'tonumber', 'tostring', 'next', 'print', 'error', 'assert', 'select', 'pcall', 'xpcall', 'unpack', 'require', 'setmetatable', 'getmetatable', 'rawget', 'rawset'}
            local_vars = set(['c', 'e', 'tp', 'eg', 'ep', 'ev', 're', 'r', 'rp', 'chk', 'chkc', 's', 'id', 'tc', 'g', 'sg', 'mg', 'mat', 'p', 'd', 'val', 'code', 'loc', 'seq', 'pos', 'reason', 'self'])
            
            # Extrai parâmetros de funções do script (Incluindo Anônimas)
            for m in re.finditer(r'function\s*(?:[\w_.:]+\s*)?\((.*?)\)', content):
                for p in m.group(1).split(','): local_vars.add(p.strip())
                
            # Extrai variáveis locais declaradas e loops for globalmente no arquivo
            for m in re.finditer(r'\blocal\s+([\w, ]+)', content):
                val = m.group(1).strip()
                if not val.startswith('function'):
                    for v in val.split(','): 
                        if v.strip(): local_vars.add(v.strip())
            
            for m in re.finditer(r'\blocal\s+function\s+([a-zA-Z0-9_]+)', content):
                local_vars.add(m.group(1).strip())
                
            for m in re.finditer(r'\bfor\s+([\w, ]+)\s+in\b', content):
                for v in m.group(1).split(','): local_vars.add(v.strip())
                
            for line_num, raw_line in enumerate(lines, 1):
                clean_line = raw_line.split('--')[0].strip()
                if not clean_line: continue
                
                # Remove strings ("exemplo") para não validar texto livre
                clean_line = re.sub(r'(["\'])(?:(?=(\\?))\2.)*?\1', '""', clean_line)
                
                # Pega qualquer palavra isolada que não seja precedida de '.' ou ':'
                words = re.findall(r'(?<![:.])\b([a-zA-Z_][a-zA-Z0-9_]*)\b', clean_line)
                for w in words:
                    if (w not in lua_keywords and w not in safe_globals and w not in local_vars and 
                        w not in defined_constants and w not in library_exported_globals and 
                        w not in cs_methods.get("aux", set()) and w not in local_consts):
                        linter_errors.append(f"[{lua_file.name} : Linha {line_num}] Global Desconhecida '{w}'. Risco de 'nil value'!")
            
            content = re.sub(r'--\[\[.*?\]\]', '', content, flags=re.DOTALL)
            content = re.sub(r'--.*', '', content)
            
            for m in re.finditer(r'\bDuel\.([A-Z][a-zA-Z0-9_]+)\s*\(', content): lua_calls["Duel"].add(m.group(1))
            for m in re.finditer(r'\bCard\.([A-Z][a-zA-Z0-9_]+)\s*\(', content): lua_calls["Card"].add(m.group(1))
            for m in re.finditer(r'\bEffect\.([A-Z][a-zA-Z0-9_]+)\s*\(', content): lua_calls["Effect"].add(m.group(1))
            for m in re.finditer(r'\bGroup\.([A-Z][a-zA-Z0-9_]+)\s*\(', content): lua_calls["Group"].add(m.group(1))
            for m in re.finditer(r'\baux\.([a-zA-Z0-9_]+)\s*\(', content): lua_calls["aux"].add(m.group(1))
            
            for cls in extra_lua_classes:
                for m in re.finditer(rf'\b{cls}\.([a-zA-Z0-9_]+)\s*\(', content): lua_calls[cls].add(m.group(1))
            
            for m in re.finditer(r'\b([a-zA-Z0-9_]+):([A-Z][a-zA-Z0-9_]+)\s*\(', content):
                var_name = m.group(1)
                method = m.group(2)
                
                if var_name in ["e", "te", "re", "eff", "effect"] or method.startswith("Set") or method.startswith("GetCountLimit") or method in ["Clone", "Reset", "GetCondition", "GetCost", "GetTarget", "GetOperation", "GetValue", "GetLabel", "GetLabelObject", "GetHandler", "GetOwner"]:
                    lua_calls["Effect"].add(method)
                elif var_name in ["g", "sg", "mg", "tg", "mat", "group", "rg"] or method in ["AddCard", "RemoveCard", "GetFirst", "GetNext", "Filter", "Match", "IsExists", "Select", "GetSum", "GetCount", "KeepAlive", "Delete", "Merge", "Sub", "FilterCount", "FilterSelect", "RandomSelect", "GetClassCount"]:
                    lua_calls["Group"].add(method)
                else:
                    lua_calls["Card"].add(method) 

            for m in re.finditer(r'\b([A-Z_][A-Z0-9_]{2,})\b', content):
                if m.group(1) not in local_consts: # Ignora as que a carta acabou de criar
                    used_constants.add(m.group(1))
        except: pass

    progresso["log"].append(f"[LUA-CARDS] Concluído. {total_files} cartas escaneadas.\n")
    progresso["log"].append("-> Cruzando chamadas LUA com Engine C#...")

    missing_methods = {k: [] for k in cs_methods.keys()}
    missing_constants = []
    
    for cat in missing_methods.keys():
        for method in lua_calls[cat]:
            if method in ["Format", "Clone", "Clear", "Add", "Sub", "Merge", "Stringid"]: continue
            
            if method not in cs_methods[cat]:
                found_anywhere = False
                for c in cs_methods.values():
                    if method in c:
                        found_anywhere = True
                        break
                
                if not found_anywhere:
                    missing_methods[cat].append(method)

    for const in used_constants:
        if const not in defined_constants and const not in ["TRUE", "FALSE", "MIN_ID", "AND", "NOT"]:
            missing_constants.append(const)
            
    for req_lib, requesters in load_script_requests.items():
        if req_lib not in available_lua_files:
            missing_libs[req_lib] = requesters
            for r in requesters:
                linter_errors.append(f"[ERRO CRÍTICO] Biblioteca '{req_lib}' ausente (Requisitada por: {r})")

    report_path = Path(project_dir) / "Diagnostic_Report.md"
    report_file_path = str(report_path)
    
    if not cancel_task:
        with open(report_path, "w", encoding="utf-8") as f:
            f.write(f"# 🛡️ OCGCore Diagnostic Report V7.0\n\n")
            f.write(f"**Arquivos LUA de Cartas Escaneados:** {total_files}\n")
            f.write(f"**Arquivos C# API Integrados:** {len(scanned_cs_files)}\n")
            f.write(f"**Bibliotecas LUA Base Escaneadas:** {len(scanned_lib_files)}\n\n")
            f.write(f"**Pasta C# Base:** `{assets_dir}`\n")
            f.write(f"**Pasta LUA Varredura:** `{lua_cards_dir}`\n\n")
            if support_lua_dir:
                f.write(f"**Pasta Libs Base (SupportLua):** `{support_lua_dir}`\n")

            f.write(f"\n---\n")
            f.write(f"### 📂 Arquivos C# Escaneados\n")
            f.write("<details><summary>Clique para expandir</summary>\n\n")
            for name in sorted(scanned_cs_files):
                f.write(f"- `{name}`\n")
            f.write("\n</details>\n\n")

            f.write(f"### 📚 Bibliotecas LUA Escaneadas\n")
            f.write("<details><summary>Clique para expandir</summary>\n\n")
            for name in sorted(scanned_lib_files):
                f.write(f"- `{name}`\n")
            f.write("\n</details>\n\n")
            f.write("---\n\n")

            f.write("> Este relatório cruza chamadas feitas pelas cartas com as declarações presentes nas bibliotecas LUA e nos scripts C# do simulador.\n\n")
            
            for cat, missing in missing_methods.items():
                f.write(f"## ❌ Faltando em `{cat}` ({len(missing)})\n")
                if not missing:
                    f.write("*Tudo perfeito!*\n\n")
                else:
                    for m in sorted(missing):
                        if cat == "aux":
                            f.write(f"- [ ] `function aux.{m}(...)`\n")
                        else:
                            f.write(f"- [ ] `public void {m}() {{ }}`\n")
                f.write("\n")

            f.write(f"## ⚠️ Constantes LUA Ausentes ({len(missing_constants)})\n")
            f.write("> As constantes abaixo foram usadas nas cartas, mas não foram definidas via `luaEngine.Globals` no C# nem encontradas nas bibliotecas LUA (SupportLua).\n\n")
            if not missing_constants:
                f.write("*Todas as constantes estão declaradas!*\n\n")
            else:
                for const in sorted(missing_constants):
                    f.write(f"- [ ] `{const}`\n")
            f.write("\n")

            f.write(f"## ☠️ Erros Críticos de Sintaxe / Valores Nulos (Deep Linter) ({len(linter_errors)})\n")
            f.write("> O analisador linha-a-linha identificou chamadas perigosas e variáveis não declaradas que resultarão em `attempt to index a nil value` ou crashes na Unity.\n\n")
            if missing_libs:
                f.write("### 🚨 Dependências de Biblioteca Faltantes\n")
                for lib, reqs in sorted(missing_libs.items()):
                    f.write(f"- 🔴 **`{lib}`** não foi encontrado! (Exigido por: `{', '.join(reqs)}`)\n")
                f.write("\n")
            if not linter_errors:
                f.write("*Nenhum erro letal de indexação encontrado!*\n\n")
            else:
                for err in linter_errors:
                    f.write(f"- 🔴 `{err}`\n")
            f.write("\n")

        progresso["log"].append(f"=== AUDITORIA FINALIZADA ===")
        progresso["log"].append(f"Relatório gerado em: {report_path}")
        progresso["status"] = "Finalizado!"

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/select_folder')
def select_folder():
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        folder = filedialog.askdirectory(title="Selecione a pasta")
        root.destroy()
        return jsonify({"folder": folder})
    except:
        return jsonify({"folder": ""})

@app.route('/cancel', methods=['POST'])
def cancel():
    global cancel_task
    cancel_task = True
    return jsonify({"ok": True})

@app.route('/start', methods=['POST'])
def start():
    d = request.json
    threading.Thread(target=audit_executor, args=(d['project_dir'], d['lua_dir'], d.get('support_dir'))).start()
    return jsonify({"ok": True})

@app.route('/open_report', methods=['POST'])
def open_report():
    global report_file_path
    if report_file_path and os.path.exists(report_file_path):
        try:
            os.startfile(report_file_path)
        except AttributeError:
            import subprocess, sys
            if sys.platform.startswith('darwin'):
                subprocess.call(('open', report_file_path))
            elif os.name == 'posix':
                subprocess.call(('xdg-open', report_file_path))
    return jsonify({"ok": True})

@app.route('/status')
def status(): return jsonify(progresso)

if __name__ == '__main__':
    app.run(debug=True, port=5009)
