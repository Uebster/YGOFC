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
    <title>YGOCore Code Search</title>
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

        .green-btn { color: var(--cyan); border-color: var(--cyan); }
        .green-btn:hover { background: var(--cyan); }

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
        <h1 class="neon-title">Code Search Engine</h1>

        <div class="form-group">
            <label>1. Pasta raiz do projeto (C#)</label>
            <div class="input-group">
                <input type="text" id="project_dir" placeholder="Ex: C:/MeuProjetoUnity">
                <button class="action-btn cyan-btn browse" onclick="selectFolder('project_dir')">Procurar</button>
            </div>
        </div>

        <div class="form-group">
            <label>2. Termo a buscar nos arquivos .cs</label>
            <div class="input-group">
                <input type="text" id="search_term" placeholder="Ex: LuaSupport, carta_especial, Effect_Modify" autocomplete="off">
            </div>
        </div>

        <div class="btn-grid">
            <button class="action-btn purple-btn" style="flex: 1;" onclick="startTask()">🔍 INICIAR BUSCA</button>
            <button class="action-btn red-btn" style="flex: 0 0 auto; padding: 0 25px;" onclick="cancelProcess()">Parar</button>
            <button class="action-btn green-btn" id="btn_open" style="flex: 0 0 auto; padding: 0 25px; display: none;" onclick="openReport()">📄 Abrir Report</button>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info">Sistema pronto para busca.</p>
        
        <div id="log">
            <div style="color: var(--purple);">[ CODE SEARCH ENGINE V1.0 ]</div>
            <div>- Busca em todos os arquivos .cs recursivamente</div>
            <div>- Detecta função/contexto próximo à ocorrência</div>
            <div>Aguardando seleção de pasta e termo...</div>
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
                search_term: document.getElementById('search_term').value
            };
            if (!data.project_dir || !data.search_term) {
                alert("Preencha a pasta raiz e o termo a buscar.");
                return;
            }
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
                    if (interval) clearInterval(interval);
                } else {
                    document.getElementById('info').innerText = d.card ? `Buscando em: ${d.card}` : d.status;
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

def find_function_name(lines, current_index):
    """
    Tenta encontrar o nome da função/método C# mais próximo acima da linha atual.
    Retorna uma string descritiva ou "Desconhecida".
    """
    patterns = [
        r'\b(public|private|protected|internal|static|async|override|virtual|sealed)\s+[\w<>\[\]]+\s+(\w+)\s*\(',
        r'^\s*(\w+)\s*\([^)]*\)\s*\{?',
        r'^\s*void\s+(\w+)\s*\(',
        r'^\s*(\w+)\s+(\w+)\s*\(',
        r'^\s*#region\s+(.+)$'
    ]
    # Varre até 40 linhas para trás
    start = max(0, current_index - 40)
    for i in range(current_index, start - 1, -1):
        line = lines[i]
        for pat in patterns:
            match = re.search(pat, line)
            if match:
                func_name = match.groups()[-1]
                func_name = func_name.strip().split('{')[0].strip()
                if func_name and len(func_name) > 0 and not func_name.startswith('if') and not func_name.startswith('for') and not func_name.startswith('while'):
                    return func_name
    # Se não encontrou, tenta capturar namespace/classe
    for i in range(current_index, max(0, current_index - 30), -1):
        line = lines[i]
        namespace_match = re.search(r'namespace\s+(\S+)', line)
        if namespace_match:
            return f"namespace {namespace_match.group(1)}"
        class_match = re.search(r'class\s+(\S+)', line)
        if class_match:
            return f"class {class_match.group(1)}"
    return "Desconhecida"

def search_executor(project_dir, search_term):
    global progresso, cancel_task, report_file_path
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Iniciando busca...", "card": "", "log": []}
    
    project_path = Path(project_dir)
    if not project_path.exists():
        progresso["status"] = f"Erro: Pasta '{project_dir}' não existe."
        return
    
    # Proteção Unity: Se estiver na raiz do projeto, foca apenas na pasta Assets para ignorar "Library" e "obj"
    search_base = project_path / "Assets" if (project_path / "Assets").exists() else project_path
    
    # Coleta todos os arquivos .cs recursivamente
    cs_files = list(search_base.rglob("*.cs"))
    total_files = len(cs_files)
    if total_files == 0:
        progresso["status"] = "Erro: Nenhum arquivo .cs encontrado na pasta."
        return
    
    progresso["total"] = total_files
    progresso["log"].append(f"=== BUSCA POR '{search_term}' ===")
    progresso["log"].append(f"Pasta base: {search_base}")
    progresso["log"].append(f"Total de arquivos .cs: {total_files}")
    progresso["log"].append("Iniciando varredura...")
    
    occurrences = []  # lista de dicionários
    term_lower = search_term.lower()
    
    for idx, cs_file in enumerate(cs_files, start=1):
        if cancel_task:
            progresso["status"] = "Cancelado"
            progresso["log"].append("Busca cancelada pelo usuário.")
            return
        
        progresso["atual"] = idx
        progresso["card"] = cs_file.name
        
        if idx % 50 == 0:
            progresso["log"].append(f"Processando {idx}/{total_files} - {cs_file.name}")
            if len(progresso["log"]) > 60:
                progresso["log"].pop(0)
        
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
                lines = f.readlines()
                
            for line_num, line in enumerate(lines, start=1):
                if term_lower in line.lower():
                    # Detecta função
                    func = find_function_name(lines, line_num - 1)
                    occurrences.append({
                        "arquivo": str(cs_file.relative_to(project_path)),
                        "funcao": func,
                        "linha": line_num,
                        "texto_linha": line.strip()
                    })
        except Exception as e:
            progresso["log"].append(f"Erro ao ler {cs_file.name}: {e}")
    
    # Gera relatório
    report_path = project_path / "Search_Report.md"
    report_file_path = str(report_path)
    with open(report_path, 'w', encoding='utf-8') as f:
        f.write(f"# 🔍 Relatório de Busca de Código\n\n")
        f.write(f"**Termo pesquisado:** `{search_term}`\n")
        f.write(f"**Pasta base:** `{search_base}`\n")
        f.write(f"**Total de ocorrências:** {len(occurrences)}\n\n")
        f.write("---\n\n")
        for occ in occurrences:
            f.write(f"### 📄 `{occ['arquivo']}`\n")
            f.write(f"- **Contexto/Função:** `{occ['funcao']}`\n")
            f.write(f"- **Linha {occ['linha']}:**\n")
            f.write(f"```csharp\n{occ['texto_linha']}\n```\n\n")
    
    progresso["log"].append(f"\nBusca concluída. {len(occurrences)} ocorrências encontradas.")
    progresso["log"].append(f"Relatório salvo em: {report_path}")
    progresso["status"] = "Finalizado!"

@app.route('/')
def home(): 
    return render_template_string(HTML_UI)

@app.route('/select_folder')
def select_folder():
    try:
        import tkinter as tk
        from tkinter import filedialog
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        folder = filedialog.askdirectory(title="Selecione a pasta raiz do projeto (C#)")
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
    threading.Thread(target=search_executor, args=(d['project_dir'], d['search_term'])).start()
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
def status(): 
    return jsonify(progresso)

if __name__ == '__main__':
    # Porta alterada para 5005
    app.run(debug=True, port=5005)