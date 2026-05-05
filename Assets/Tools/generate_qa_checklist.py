from flask import Flask, render_template_string, request, jsonify
import os
import json
import tkinter as tk
from tkinter import filedialog
import threading

app = Flask(__name__)

progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}
cancel_task = False
report_file_path = ""

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <link rel="icon" href="data:,">
    <title>QA Checklist Studio</title>
    <style>
        :root {
            --cyan: #00e5ff;
            --purple: #b537f2;
            --gold: #d4af37;
            --green: #39ff14;
            --red: #ff073a;
            --orange: #ff9d00;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 40px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--purple); padding: 35px; 
            border-radius: 20px; width: 800px; box-shadow: 0 0 40px rgba(181, 55, 242, 0.15);
            display: flex; flex-direction: column; max-height: 90vh;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--purple); text-shadow: 0 0 10px var(--purple), 0 0 20px rgba(181, 55, 242, 0.5);
        }
        label { font-size: 0.9em; color: #ccc; font-weight: bold; display: block; margin-bottom: 6px; }
        .form-group { margin-bottom: 15px; }
        
        input[type="text"] { 
            width: 100%; padding: 12px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 10px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input:focus { outline: none; border-color: var(--purple); box-shadow: 0 0 12px rgba(181, 55, 242, 0.4); }
        
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 12px; padding: 10px 15px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; display: flex; justify-content: center; align-items: center; text-align: center; white-space: nowrap;
        }
        .action-btn:hover { color: #000 !important; text-shadow: none; transform: translateY(-2px); }

        .purple-btn { color: var(--purple); border-color: var(--purple); box-shadow: inset 0 0 8px rgba(181,55,242,0.3); text-shadow: 0 0 5px var(--purple); }
        .purple-btn:hover { background: var(--purple); box-shadow: inset 0 0 20px var(--purple); }

        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan); }

        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold); }

        .red-btn { color: var(--red); border-color: var(--red); box-shadow: inset 0 0 8px rgba(255,7,58,0.3); text-shadow: 0 0 5px var(--red); }
        .red-btn:hover { background: var(--red); box-shadow: inset 0 0 20px var(--red); color: #fff !important; }
        
        .green-btn { color: var(--green); border-color: var(--green); box-shadow: inset 0 0 8px rgba(57,255,20,0.3); text-shadow: 0 0 5px var(--green); }
        .green-btn:hover { background: var(--green); box-shadow: inset 0 0 20px var(--green); color: #000 !important; }

        .orange-btn { color: var(--orange); border-color: var(--orange); box-shadow: inset 0 0 8px rgba(255,157,0,0.3); text-shadow: 0 0 5px var(--orange); }
        .orange-btn:hover { background: var(--orange); box-shadow: inset 0 0 20px var(--orange); color: #000 !important; }

        .row-flex { display: flex; gap: 15px; align-items: center; }

        .checkbox-grid { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 10px; font-size: 0.85em; color: #999; margin-bottom: 10px; padding: 15px; background: #050505; border: 1px solid #222; border-radius: 10px;}
        .checkbox-grid label { font-weight: normal; margin: 0; cursor: pointer; display: flex; align-items: center; gap: 8px; transition: color 0.2s;}
        .checkbox-grid label:hover { color: var(--purple); }
        input[type="checkbox"] { accent-color: var(--purple); width: 16px; height: 16px; cursor: pointer;}

        .progress-container { background: #050505; border: 1px solid var(--purple); height: 22px; border-radius: 11px; overflow: hidden; box-shadow: 0 0 10px rgba(181,55,242,0.2); margin-top: 15px; flex-shrink: 0;}
        .progress-bar { width: 0%; height: 100%; background: var(--purple); box-shadow: 0 0 15px var(--purple); transition: width 0.3s ease; }
        
        #info { text-align: center; color: #aaa; font-size: 0.9em; margin: 10px 0 0 0; font-style: italic;}
        
        #log { 
            margin-top: 15px; background: #050505; border: 1px solid #222; padding: 15px; 
            border-radius: 12px; overflow-y: auto; color: var(--gold); font-family: 'Consolas', monospace; 
            font-size: 0.85em; box-shadow: inset 0 0 20px rgba(0,0,0,1); flex-grow: 1;
        }
        #log div { margin-bottom: 4px; line-height: 1.4;}
        
        ::-webkit-scrollbar { width: 8px; }
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--purple); box-shadow: 0 0 10px var(--purple); }
        
        .modal { display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%; background-color: rgba(0,0,0,0.8); justify-content: center; align-items: center; }
        .modal-content { background: var(--panel-bg); padding: 30px; border-radius: 15px; border: 2px solid var(--purple); text-align: center; width: 500px; color: #fff; box-shadow: 0 0 40px rgba(0,0,0,1); }
        .modal-content h3 { margin-top: 0; margin-bottom: 10px;}
        .modal-buttons { display: flex; justify-content: center; gap: 20px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class="panel-container">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; flex-shrink: 0;">
            <h1 class="neon-title" style="margin: 0;">QA Checklist Studio</h1>
            <button id="btn_toggle_config" class="action-btn purple-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▼ Mostrar Config</button>
        </div>

        <div id="config_bar">
            <div class="form-group">
                <label>1. Arquivo cards.json</label>
                <div class="input-group">
                    <input type="text" id="json_path" placeholder="Caminho do arquivo cards.json...">
                    <button class="action-btn cyan-btn" onclick="selectFile()">Procurar</button>
                </div>
            </div>
            
            <div class="form-group">
                <label>2. Pasta de Scripts LUA (LuaScripts)</label>
                <div class="input-group">
                    <input type="text" id="lua_dir" placeholder="Pasta onde estão os arquivos .lua das cartas...">
                    <button class="action-btn cyan-btn" onclick="selectFolder('lua_dir')">Procurar</button>
                </div>
            </div>

            <div class="form-group">
                <label>3. Onde salvar o Markdown (.md)?</label>
                <div class="input-group">
                    <input type="text" id="out_dir" placeholder="Pasta de destino para o QA_Card_Checklist.md...">
                    <button class="action-btn cyan-btn" onclick="selectFolder('out_dir')">Procurar</button>
                </div>
            </div>

            <label style="margin-top: 15px;">Opções de Formatação do Checklist</label>
            <div class="checkbox-grid">
                <label><input type="checkbox" id="chk_split" checked> Separar por Tipo (Spell/Trap/Monst)</label>
                <label><input type="checkbox" id="chk_alpha" checked> Ordem Alfabética</label>
                <label><input type="checkbox" id="chk_box" checked> Checkbox Vazia (- [ ])</label>
                <label><input type="checkbox" id="chk_id" checked> Incluir ID</label>
                <label><input type="checkbox" id="chk_name" checked> Incluir Nome</label>
                <label><input type="checkbox" id="chk_status" checked> Mostrar Status LUA</label>
            </div>

            <div class="row-flex" style="margin-top: 20px;">
                <div style="flex-grow: 1;"></div>
                <button class="action-btn red-btn" onclick="cancelProcess()">Parar</button>
                <button class="action-btn gold-btn" onclick="preCheck()">🔍 ANALISAR & GERAR</button>
                <button class="action-btn green-btn" id="btn_open" style="display: none;" onclick="openReport()">📄 Abrir Checklist</button>
            </div>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info">Sistema Ocioso e Pronto.</p>
        
        <div id="log">
            <div style="color: var(--purple);">[ QA CHECKLIST WEB V2.0 ]</div>
            <div>- Preparado para auditoria e criação de checklist Markdown.</div>
        </div>
    </div>

    <!-- Modal de Confirmação -->
    <div id="confirmModal" class="modal">
        <div class="modal-content">
            <h3 id="modalTitle">Confirmação de Análise</h3>
            <p id="modalText"></p>
            <div id="missingScripts" style="max-height: 150px; overflow-y: auto; text-align: left; font-size: 0.85em; font-family: monospace; color: var(--red); margin-top: 10px; background: #0a0a0a; padding: 10px; border: 1px solid #333; border-radius: 5px;"></div>
            <div class="modal-buttons">
                <button class="action-btn red-btn" onclick="closeModal()">Cancelar</button>
                <button class="action-btn green-btn" onclick="confirmGenerate()">Gerar Checklist</button>
            </div>
        </div>
    </div>

    <script>
        let interval;

        function selectFile() {
            fetch('/select_file_src').then(r => r.json()).then(d => {
                if(d.path) document.getElementById('json_path').value = d.path;
            });
        }

        function selectFolder(inputId) {
            fetch('/select_folder').then(r => r.json()).then(d => {
                if(d.folder) document.getElementById(inputId).value = d.folder;
            });
        }

        function preCheck() {
            document.getElementById('btn_open').style.display = 'none';
            const data = {
                json_path: document.getElementById('json_path').value,
                lua_dir: document.getElementById('lua_dir').value,
                out_dir: document.getElementById('out_dir').value
            };
            
            if (!data.json_path || !data.lua_dir || !data.out_dir) {
                alert("ATENÇÃO: Você precisa selecionar o arquivo JSON, a pasta de Scripts LUA e a pasta de Destino para gerar o relatório!");
                return;
            }

            fetch('/precheck', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) })
            .then(r => r.json()).then(d => {
                if(d.error) {
                    alert("ERRO DE SISTEMA: " + d.error);
                } else {
                    showModal(d);
                }
            });
        }

        function showModal(data) {
            const modal = document.getElementById('confirmModal');
            const title = document.getElementById('modalTitle');
            const text = document.getElementById('modalText');
            const missingDiv = document.getElementById('missingScripts');
            
            const diff = data.expected - data.found;
            
            if (diff > 0) {
                title.style.color = "var(--orange)";
                title.innerText = "Atenção: Scripts Faltando!";
                text.innerHTML = `O banco de dados pede <b>${data.expected}</b> cartas com efeito, mas apenas <b>${data.found}</b> scripts LUA foram encontrados.<br><br>Deseja continuar e gerar o checklist marcando-os como ausentes?`;
                
                missingDiv.style.display = "block";
                missingDiv.innerHTML = "<b>[ALERTA] SCRIPTS LUA AUSENTES:</b><br>" + data.missing.map(m => `&bull; ${m}`).join("<br>");
            } else {
                title.style.color = "var(--green)";
                title.innerText = "Análise Perfeita!";
                text.innerHTML = `Foram encontradas <b>${data.expected}</b> cartas com efeito e exatos <b>${data.found}</b> scripts LUA compatíveis.<br><br>O ambiente está perfeitamente alinhado. Deseja gerar o arquivo .MD?`;
                missingDiv.style.display = "none";
            }
            
            modal.style.display = "flex";
        }

        function closeModal() {
            document.getElementById('confirmModal').style.display = "none";
        }

        function confirmGenerate() {
            closeModal();
            const data = {
                json_path: document.getElementById('json_path').value,
                lua_dir: document.getElementById('lua_dir').value,
                out_dir: document.getElementById('out_dir').value,
                options: {
                    split: document.getElementById('chk_split').checked,
                    alpha: document.getElementById('chk_alpha').checked,
                    box: document.getElementById('chk_box').checked,
                    id: document.getElementById('chk_id').checked,
                    name: document.getElementById('chk_name').checked,
                    status: document.getElementById('chk_status').checked
                }
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
                        document.getElementById('config_bar').style.display = 'none';
                        document.getElementById('btn_toggle_config').style.display = 'block';
                        document.getElementById('btn_toggle_config').innerText = '▼ Mostrar Config';
                        isConfigVisible = false;
                    }
                    if (interval) clearInterval(interval);
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

        let isConfigVisible = true;
        function toggleConfig() {
            const configBar = document.getElementById('config_bar');
            const btnToggle = document.getElementById('btn_toggle_config');
            isConfigVisible = !isConfigVisible;
            if(isConfigVisible) {
                configBar.style.display = 'block';
                btnToggle.innerText = '▲ Ocultar Config';
            } else {
                configBar.style.display = 'none';
                btnToggle.innerText = '▼ Mostrar Config';
            }
        }
    </script>
</body>
</html>
"""

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/select_file_src')
def select_file_src():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        p = filedialog.askopenfilename(title="Selecione cards.json", filetypes=[("JSON Files", "*.json")])
        root.destroy()
        return jsonify({"path": p})
    except: return jsonify({"path": ""})

@app.route('/select_folder')
def select_folder():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        p = filedialog.askdirectory(title="Selecione a pasta")
        root.destroy()
        return jsonify({"folder": p})
    except: return jsonify({"folder": ""})

@app.route('/precheck', methods=['POST'])
def precheck():
    data = request.json
    json_path = data.get('json_path')
    lua_dir = data.get('lua_dir')
    
    if not os.path.exists(json_path):
        return jsonify({"error": "O arquivo cards.json não foi encontrado neste caminho."})
    if not os.path.exists(lua_dir):
        return jsonify({"error": "A pasta LuaScripts informada não existe!"})
        
    try:
        with open(json_path, 'r', encoding='utf-8') as f:
            cards_db = json.load(f)
    except Exception as e:
        return jsonify({"error": f"Erro de leitura no JSON: {e}"})
        
    existing_lua_files = {f.lower() for f in os.listdir(lua_dir) if f.endswith('.lua')}
    
    expected = 0
    found = 0
    missing = []
    
    for card in cards_db:
        card_id = str(card.get("id", ""))
        c_type = card.get("type", "")
        c_name = card.get("name", "Unknown Name")
        
        is_spell = "Spell" in c_type or "Magic" in c_type
        is_trap = "Trap" in c_type
        is_effect_monster = "Monster" in c_type and ("Effect" in c_type or "Fusion" in c_type or "Ritual" in c_type)
        
        if not (is_spell or is_trap or is_effect_monster):
            continue
            
        expected += 1
        
        expected_name1 = f"c{card_id}.lua".lower()
        expected_name2 = f"{card_id}.lua".lower()
        
        try:
            numeric_id = str(int(card_id))
            expected_name3 = f"c{numeric_id}.lua".lower()
            expected_name4 = f"{numeric_id}.lua".lower()
        except ValueError:
            expected_name3 = ""
            expected_name4 = ""
            
        has_script = (expected_name1 in existing_lua_files or 
                      expected_name2 in existing_lua_files or 
                      (expected_name3 and expected_name3 in existing_lua_files) or 
                      (expected_name4 and expected_name4 in existing_lua_files))
                      
        if has_script: found += 1
        else: missing.append(f"[{card_id}] {c_name}")
            
    return jsonify({"expected": expected, "found": found, "missing": missing})

def generate_task(json_path, lua_dir, out_dir, options):
    global progresso, cancel_task, report_file_path
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Iniciando...", "card": "", "log": []}
    
    try:
        with open(json_path, 'r', encoding='utf-8') as f:
            cards_db = json.load(f)
            
        existing_lua_files = {f.lower() for f in os.listdir(lua_dir) if f.endswith('.lua')}
        
        spells, traps, effect_monsters, all_items = [], [], [], []
        
        total_cards = len(cards_db)
        progresso["total"] = total_cards
        
        for i, card in enumerate(cards_db, 1):
            if cancel_task: return
            
            progresso["atual"] = i
            c_name = card.get("name", "Unknown Name")
            progresso["card"] = c_name
            
            card_id = str(card.get("id", ""))
            c_type = card.get("type", "")
            
            is_spell = "Spell" in c_type or "Magic" in c_type
            is_trap = "Trap" in c_type
            is_effect_monster = "Monster" in c_type and ("Effect" in c_type or "Fusion" in c_type or "Ritual" in c_type)
            
            if not (is_spell or is_trap or is_effect_monster):
                continue
                
            expected_name1 = f"c{card_id}.lua".lower()
            expected_name2 = f"{card_id}.lua".lower()
            try:
                numeric_id = str(int(card_id))
                expected_name3 = f"c{numeric_id}.lua".lower()
                expected_name4 = f"{numeric_id}.lua".lower()
            except ValueError:
                expected_name3, expected_name4 = "", ""
                
            has_script = (expected_name1 in existing_lua_files or 
                          expected_name2 in existing_lua_files or 
                          (expected_name3 and expected_name3 in existing_lua_files) or 
                          (expected_name4 and expected_name4 in existing_lua_files))
                          
            parts = []
            if options.get('box'): parts.append("- [ ]")
            else: parts.append("-")
            
            if options.get('id'): parts.append(f"`{card_id}`")
            if options.get('name'): parts.append(f"**{c_name}**")
                
            if options.get('status'):
                status_str = "✅ `[LUA OK]`" if has_script else "❌ `[FALTA LUA]`"
                parts.append(status_str)
                
            item_str = " ".join(parts)
            
            if options.get('split'):
                if is_spell: spells.append((c_name, item_str))
                elif is_trap: traps.append((c_name, item_str))
                elif is_effect_monster: effect_monsters.append((c_name, item_str))
            else:
                all_items.append((c_name, item_str))
                
            if i % 100 == 0:
                progresso["log"].append(f"Varrendo e Formatando... ({i}/{total_cards})")
                while len(progresso["log"]) > 15: progresso["log"].pop(0)

        if options.get('alpha'):
            spells.sort(key=lambda x: x[0])
            traps.sort(key=lambda x: x[0])
            effect_monsters.sort(key=lambda x: x[0])
            all_items.sort(key=lambda x: x[0])
            
        output_path = os.path.join(out_dir, "QA_Card_Checklist.md")
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write("# 🧪 Checklist de Homologação de Efeitos LUA (QA)\n")
            f.write("> Utilize este documento para rastrear o progresso de testes dos scripts LUA das cartas.\n\n")
            
            if options.get('split'):
                if spells: f.write(f"## Magias (Spells) ({len(spells)})\n" + "\n".join([x[1] for x in spells]) + "\n\n")
                if traps: f.write(f"## Armadilhas (Traps) ({len(traps)})\n" + "\n".join([x[1] for x in traps]) + "\n\n")
                if effect_monsters: f.write(f"## Monstros de Efeito ({len(effect_monsters)})\n" + "\n".join([x[1] for x in effect_monsters]) + "\n\n")
            else:
                if all_items: f.write(f"## Todas as Cartas Relevantes ({len(all_items)})\n" + "\n".join([x[1] for x in all_items]) + "\n\n")
                
        progresso["log"].append(f"=== SUCESSO ABSOLUTO ===")
        progresso["log"].append(f"Checklist Markdown pronto em: {output_path}")
        report_file_path = output_path
        progresso["status"] = "Finalizado!"
        
    except Exception as e:
        progresso["log"].append(f"ERRO: {str(e)}")
        progresso["status"] = f"Erro: {str(e)}"

@app.route('/cancel', methods=['POST'])
def cancel():
    global cancel_task
    cancel_task = True
    return jsonify({"ok": True})

@app.route('/start', methods=['POST'])
def start():
    d = request.json
    threading.Thread(target=generate_task, args=(d['json_path'], d['lua_dir'], d['out_dir'], d['options'])).start()
    return jsonify({"ok": True})

@app.route('/open_report', methods=['POST'])
def open_report():
    global report_file_path
    if report_file_path and os.path.exists(report_file_path):
        try: os.startfile(report_file_path)
        except: pass
    return jsonify({"ok": True})

@app.route('/status')
def status(): return jsonify(progresso)

if __name__ == "__main__":
    # Usa a porta 5007 
    app.run(debug=True, port=5007)