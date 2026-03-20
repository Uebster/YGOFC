from flask import Flask, render_template_string, request, jsonify
import requests
import os
import threading
import time
import json as json_lib
import csv
import concurrent.futures
import zipfile

app = Flask(__name__)

# Controle de progresso global
progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}
cancel_task = False

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>Yu-Gi-Oh! Deck Extractor Pro</title>
    <style>
        :root {
            --gold: #d4af37;
            --cyan: #00e5ff;
            --purple: #b537f2;
            --green: #39ff14;
            --red: #ff073a;
            --yellow: #ffee00;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; padding: 40px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--yellow); padding: 35px; 
            border-radius: 20px; width: 1000px; box-shadow: 0 0 40px rgba(255, 238, 0, 0.15);
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 35px;
            color: var(--yellow); text-shadow: 0 0 10px var(--yellow), 0 0 20px rgba(255, 238, 0, 0.5);
        }
        .panel-columns { display: flex; gap: 40px; align-items: stretch; }
        .col { flex: 1; display: flex; flex-direction: column; gap: 18px; }
        
        label { font-size: 0.9em; color: #ccc; font-weight: bold; display: block; margin-bottom: 6px; }
        .form-group { margin-bottom: 5px; }
        
        input[type="text"], input[type="date"], select { 
            width: 100%; padding: 12px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 10px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input:focus, select:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 12px rgba(0, 229, 255, 0.4); }
        
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        /* Neon Buttons */
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 12px; padding: 14px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; display: flex; justify-content: center; align-items: center; text-align: center;
        }
        .action-btn:hover { color: #000 !important; text-shadow: none; transform: translateY(-2px); }

        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3), 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold), 0 0 20px var(--gold); }

        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3), 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan), 0 0 20px var(--cyan); }

        .green-btn { color: var(--green); border-color: var(--green); box-shadow: inset 0 0 8px rgba(57,255,20,0.3), 0 0 8px rgba(57,255,20,0.3); text-shadow: 0 0 5px var(--green); }
        .green-btn:hover { background: var(--green); box-shadow: inset 0 0 20px var(--green), 0 0 20px var(--green); }

        .purple-btn { color: var(--purple); border-color: var(--purple); box-shadow: inset 0 0 8px rgba(181,55,242,0.3), 0 0 8px rgba(181,55,242,0.3); text-shadow: 0 0 5px var(--purple); }
        .purple-btn:hover { background: var(--purple); box-shadow: inset 0 0 20px var(--purple), 0 0 20px var(--purple); }

        .red-btn { color: var(--red); border-color: var(--red); box-shadow: inset 0 0 8px rgba(255,7,58,0.3), 0 0 8px rgba(255,7,58,0.3); text-shadow: 0 0 5px var(--red); }
        .red-btn:hover { background: var(--red); box-shadow: inset 0 0 20px var(--red), 0 0 20px var(--red); color: #fff !important; }

        .browse { flex: 0 0 auto; width: auto; padding: 0 20px;}
        .btn-small { padding: 10px; font-size: 0.8em; border-width: 1px;}
        
        .btn-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin-bottom: 10px;}
        .btn-grid > :nth-child(7) { grid-column: span 2; } /* Botão Zip Ocupa tudo */

        /* TXT Options Block */
        #txt_options { background: #050505; padding: 20px; border-radius: 12px; border: 1px solid #222; margin-top: auto; box-shadow: inset 0 0 20px rgba(0,0,0,0.8); flex-grow: 1; display: flex; flex-direction: column;}
        .section-title { color: var(--cyan); text-shadow: 0 0 5px rgba(0,229,255,0.4); text-transform: uppercase; letter-spacing: 1px; margin-bottom: 15px;}
        .checkbox-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; font-size: 0.85em; color: #999; margin-bottom: auto; }
        .checkbox-grid label { font-weight: normal; margin: 0; cursor: pointer; display: flex; align-items: center; gap: 8px; transition: color 0.2s;}
        .checkbox-grid label:hover { color: var(--cyan); }
        .checkbox-actions { display: flex; gap: 10px; margin-top: 15px;}
        .checkbox-actions button { flex: 1; }
        input[type="checkbox"] { accent-color: var(--cyan); width: 16px; height: 16px; cursor: pointer;}

        /* Progress & Log */
        .progress-container { background: #050505; border: 1px solid var(--cyan); height: 22px; border-radius: 11px; overflow: hidden; box-shadow: 0 0 10px rgba(0,229,255,0.2); margin-top: 5px; flex-shrink: 0;}
        .progress-bar { width: 0%; height: 100%; background: var(--cyan); box-shadow: 0 0 15px var(--cyan); transition: width 0.3s ease; }
        
        #info { text-align: center; color: #aaa; font-size: 0.9em; margin: 5px 0 0 0; font-style: italic;}
        
        #log { 
            flex-grow: 1; background: #050505; border: 1px solid #222; padding: 15px; 
            border-radius: 12px; overflow-y: auto; color: var(--green); font-family: 'Consolas', 'Courier New', monospace; 
            font-size: 0.85em; box-shadow: inset 0 0 20px rgba(0,0,0,1); text-shadow: 0 0 2px rgba(57,255,20,0.3);
            height: 250px; /* Base height */
        }
        #log div { margin-bottom: 4px; line-height: 1.4;}
        
        /* Custom Scrollbar */
        ::-webkit-scrollbar { width: 8px; }
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #333; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--cyan); box-shadow: 0 0 10px var(--cyan); }
    </style>
</head>
<body>
    <div class="panel-container">
        <h1 class="neon-title">Ultimate Extractor</h1>

        <div class="panel-columns">
            
            <!-- Coluna Esquerda: Configurações -->
            <div class="col left-col">
                <div class="form-group">
                    <label>Nome da Pasta Destino</label>
                    <div class="input-group">
                        <input type="text" id="folder" placeholder="Ex: C:/YuGiOh_Assets">
                        <button class="action-btn cyan-btn browse" onclick="selectFolder()">Procurar</button>
                    </div>
                </div>

                <div class="form-group">
                    <label>Data Inicial</label>
                    <input type="date" id="start" value="1999-01-01">
                </div>

                <div class="form-group">
                    <label>Data Final</label>
                    <input type="date" id="end" value="2008-12-31">
                </div>

                <div class="form-group">
                    <label>Região da API</label>
                    <select id="region">
                        <option value="ocg">OCG (Japão)</option>
                        <option value="tcg">TCG (Ocidente)</option>
                    </select>
                </div>

                <div id="txt_options">
                    <label class="section-title">Colunas (Exportação TXT)</label>
                    <div class="checkbox-grid">
                        <label><input type="checkbox" id="col_id" checked> ID Custom</label>
                        <label><input type="checkbox" id="col_name" checked> Nome</label>
                        <label><input type="checkbox" id="col_type" checked> Tipo</label>
                        <label><input type="checkbox" id="col_attr" checked> Atributo</label>
                        <label><input type="checkbox" id="col_race" checked> Raça/Prop.</label>
                        <label><input type="checkbox" id="col_level" checked> Nível/Link</label>
                        <label><input type="checkbox" id="col_atk" checked> ATK</label>
                        <label><input type="checkbox" id="col_def" checked> DEF</label>
                        <label><input type="checkbox" id="col_pass" checked> Senha</label>
                        <label><input type="checkbox" id="col_desc" checked> Descrição</label>
                        <label><input type="checkbox" id="col_arch"> Arquétipo</label>
                        <label><input type="checkbox" id="col_scale"> Scale</label>
                        <label><input type="checkbox" id="col_goat"> Goat List</label>
                    </div>
                    <div class="checkbox-actions">
                        <button class="action-btn cyan-btn btn-small" onclick="toggleAllTxt(true)">Selecionar Todos</button>
                        <button class="action-btn cyan-btn btn-small" onclick="toggleAllTxt(false)">Limpar Seleção</button>
                    </div>
                </div>
            </div>

            <!-- Coluna Direita: Ações e Log -->
            <div class="col right-col">
                <div class="btn-grid">
                    <button class="action-btn cyan-btn" onclick="startTask('txt')">1. TXT Rápido</button>
                    <button class="action-btn cyan-btn" onclick="startTask('json')">2. JSON Master</button>
                    <button class="action-btn gold-btn" onclick="startTask('img')">3. Imagens (HD)</button>
                    <button class="action-btn green-btn" onclick="startTask('csv')">4. Tabela CSV</button>
                    <button class="action-btn purple-btn" onclick="startTask('lua')">5. Scripts LUA</button>
                    <button class="action-btn green-btn" onclick="startTask('audit')">6. Auditoria</button>
                    <button class="action-btn cyan-btn" onclick="startTask('zip')">7. Zipar Backup</button>
                </div>

                <button class="action-btn red-btn" onclick="cancelProcess()">Parar Operação Ativa</button>

                <div class="progress-container">
                    <div class="progress-bar" id="bar"></div>
                </div>
                <p id="info">Sistema Ocioso e Pronto.</p>
                
                <div id="log">
                    <div style="color: var(--cyan);">[ SISTEMA ULTIMATE EXTRACTOR V2.0 ]</div>
                    <div>- Módulo de Multi-Threading Ativo (x10)</div>
                    <div>- Proteção de Auto-Retry Habilitada</div>
                    <div>Aguardando comandos...</div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let interval;

        function selectFolder() {
            fetch('/select_folder').then(r => r.json()).then(d => {
                if(d.folder) document.getElementById('folder').value = d.folder;
            });
        }

        function toggleAllTxt(state) {
            const checkboxes = ['col_id', 'col_name', 'col_type', 'col_attr', 'col_race', 'col_level', 'col_atk', 'col_def', 'col_pass', 'col_desc', 'col_arch', 'col_scale', 'col_goat'];
            checkboxes.forEach(id => document.getElementById(id).checked = state);
        }

        function startTask(type) {
            const data = {
                type: type,
                folder: document.getElementById('folder').value || "Ultimate_Assets",
                start: document.getElementById('start').value,
                end: document.getElementById('end').value,
                region: document.getElementById('region').value,
                txt_cols: {
                    id: document.getElementById('col_id').checked,
                    name: document.getElementById('col_name').checked,
                    type: document.getElementById('col_type').checked,
                    attr: document.getElementById('col_attr').checked,
                    race: document.getElementById('col_race').checked,
                    level: document.getElementById('col_level').checked,
                    atk: document.getElementById('col_atk').checked,
                    def: document.getElementById('col_def').checked,
                    pass: document.getElementById('col_pass').checked,
                    desc: document.getElementById('col_desc').checked,
                    arch: document.getElementById('col_arch').checked,
                    scale: document.getElementById('col_scale').checked,
                    goat: document.getElementById('col_goat').checked
                }
            };
            fetch('/start', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) });
            if(interval) clearInterval(interval);
            interval = setInterval(update, 500);
        }

        function cancelProcess() {
            fetch('/cancel', { method: 'POST' });
        }

        function update() {
            fetch('/status').then(r => r.json()).then(d => {
                const perc = (d.atual / d.total) * 100 || 0;
                document.getElementById('bar').style.width = perc + '%';
                document.getElementById('info').innerText = d.card ? `Processando: ${d.card}` : d.status;
                if (d.log.length > 0) {
                    const logDiv = document.getElementById('log');
                    logDiv.innerHTML = d.log.map(line => `<div>${line}</div>`).join('');
                    logDiv.scrollTop = logDiv.scrollHeight;
                }
                if(d.status === "Finalizado!") clearInterval(interval);
            });
        }
    </script>
</body>
</html>
"""

def task_executor(tipo, folder, start, end, region, txt_cols=None):
    global progresso, cancel_task
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Iniciando...", "card": "", "log": []}
    errors_list = []
    
    if not os.path.exists(folder): os.makedirs(folder)
    headers = {'User-Agent': 'Mozilla/5.0'}

    # Repositórios oficiais do EDOPro para LUA
    LUA_URLS = [
        "https://raw.githubusercontent.com/ProjectIgnis/CardScripts/master/official/c{}.lua",
        "https://raw.githubusercontent.com/ProjectIgnis/CardScripts/master/pre-errata/c{}.lua",
        "https://raw.githubusercontent.com/Fluorohydride/ygopro-scripts/master/c{}.lua"
    ]

    url = f"https://db.ygoprodeck.com/api/v7/cardinfo.php?startdate={start}&enddate={end}&dateregion={region}"

    try:
        resp = requests.get(url, headers=headers).json()
        cards = resp.get('data', [])
        cards.sort(key=lambda x: x['name'])
        total = len(cards)
        progresso["total"] = total

        if tipo == 'txt':
            if not txt_cols:
                txt_cols = {'id': True, 'name': True, 'type': True, 'attr': True, 'race': True, 'level': True, 'atk': True, 'def': True, 'pass': True, 'desc': True, 'arch': False, 'scale': False, 'goat': False}
            progresso["status"] = "Gerando TXT..."
            path_txt = os.path.join(folder, "lista_cartas.txt")
            with open(path_txt, "w", encoding="utf-8") as f:
                headers = []
                if txt_cols.get('id'): headers.append("Nº")
                if txt_cols.get('name'): headers.append("NOME")
                if txt_cols.get('type'): headers.append("TIPO")
                if txt_cols.get('attr'): headers.append("ATRIBUTO")
                if txt_cols.get('race'): headers.append("RAÇA/PROPRIEDADE")
                if txt_cols.get('level'): headers.append("LV")
                if txt_cols.get('atk'): headers.append("ATK")
                if txt_cols.get('def'): headers.append("DEF")
                if txt_cols.get('pass'): headers.append("ID")
                if txt_cols.get('arch'): headers.append("ARCHETYPE")
                if txt_cols.get('scale'): headers.append("SCALE")
                if txt_cols.get('goat'): headers.append("GOAT")
                if txt_cols.get('desc'): headers.append("DESC")
                f.write("\t".join(headers) + "\n")
                
                for i, c in enumerate(cards, 1):
                    if cancel_task: break
                    if cancel_task: break
                    progresso["atual"] = i
                    progresso["card"] = c.get('name', 'Unknown')

                    num = f"{i:04d}"
                    name = c.get('name', 'Unknown')
                    tipo_api = c.get('type', '')
                    cid = c.get('id', '')
                    desc = c.get('desc', '').replace('\n', ' ').replace('\r', ' ')
                    archetype = c.get('archetype', 'None')
                    goat_status = c.get('banlist_info', {}).get('banlist_goat', 'Unlimited')
                    scale = c.get('scale', '-')

                    if "Monster" in tipo_api:
                        if "Fusion" in tipo_api: sub_tipo = "Fusion"
                        elif "Ritual" in tipo_api: sub_tipo = "Ritual"
                        elif "Normal" in tipo_api: sub_tipo = "Normal"
                        else: sub_tipo = "Effect"
                        tipo_final = f"Monster ({sub_tipo})"

                        attribute = c.get('attribute', '-').upper()
                        race = c.get('race', '-')
                        level = str(c.get('level', c.get('linkval', 0)))
                        atk = str(c.get('atk', 0))
                        defe = str(c.get('def', 0))
                    else:
                        tipo_final = tipo_api
                        attribute = "-"
                        race = c.get('race', 'Normal')
                        level = "-"
                        atk = "-"
                        defe = "-"
                        
                    row = []
                    if txt_cols.get('id'): row.append(num)
                    if txt_cols.get('name'): row.append(name)
                    if txt_cols.get('type'): row.append(tipo_final)
                    if txt_cols.get('attr'): row.append(attribute)
                    if txt_cols.get('race'): row.append(race)
                    if txt_cols.get('level'): row.append(level)
                    if txt_cols.get('atk'): row.append(atk)
                    if txt_cols.get('def'): row.append(defe)
                    if txt_cols.get('pass'): row.append(str(cid))
                    if txt_cols.get('arch'): row.append(archetype)
                    if txt_cols.get('scale'): row.append(str(scale))
                    if txt_cols.get('goat'): row.append(goat_status)
                    if txt_cols.get('desc'): row.append(desc)
                    f.write("\t".join(row) + "\n")
            
            progresso["log"].append(f"✓ Lista TXT gerada!")

        elif tipo == 'csv':
            progresso["status"] = "Gerando CSV..."
            path_csv = os.path.join(folder, "lista_cartas.csv")
            with open(path_csv, "w", encoding="utf-8", newline='') as f:
                writer = csv.writer(f)
                writer.writerow(["ID", "Name", "Type", "Attribute", "Race/Property", "Level/Link", "ATK", "DEF", "Password", "Archetype", "Scale", "Description"])
                
                for i, c in enumerate(cards, 1):
                    progresso["atual"] = i
                    progresso["card"] = c.get('name', 'Unknown')

                    custom_id = f"{i:04d}"
                    name_raw = c.get('name', 'Unknown')
                    tipo_api = c.get('type', '')
                    cid = str(c.get('id', ''))
                    desc = c.get('desc', '').replace('\n', ' ').replace('\r', ' ')
                    archetype = c.get('archetype', 'None')
                    goat_status = c.get('banlist_info', {}).get('banlist_goat', 'Unlimited')
                    scale = c.get('scale', '-')

                    if "Monster" in tipo_api:
                        if "Fusion" in tipo_api: sub_tipo = "Fusion"
                        elif "Ritual" in tipo_api: sub_tipo = "Ritual"
                        elif "Normal" in tipo_api: sub_tipo = "Normal"
                        else: sub_tipo = "Effect"
                        tipo_final = f"Monster ({sub_tipo})"

                        attribute = c.get('attribute', '-').upper()
                        race = c.get('race', '-')
                        level = c.get('level', c.get('linkval', 0))
                        atk = c.get('atk', 0)
                        defe = c.get('def', 0)
                        writer.writerow([custom_id, name_raw, tipo_final, attribute, race, level, atk, defe, cid, archetype, scale, f"[{goat_status}] {desc}"])
                    else:
                        property_val = c.get('race', 'Normal')
                        writer.writerow([custom_id, name_raw, tipo_api, "-", property_val, "-", "-", "-", cid, archetype, "-", desc])
            if not cancel_task: progresso["log"].append("✓ Lista CSV gerada!")

        elif tipo == 'json':
            progresso["status"] = "Gerando JSON Master..."
            final_db = []
            for i, c in enumerate(cards, 1):
                if cancel_task: break
                progresso["atual"] = i
                progresso["card"] = c.get('name', 'Unknown')

                custom_id = f"{i:04d}"
                name_raw = c.get('name', 'Unknown')
                tipo_api = c.get('type', '')
                desc = c.get('desc', '').replace('\r', '')

                card_obj = {
                    "id": custom_id,
                    "name": name_raw,
                    "password": str(c.get('id', '')),
                    "description": desc,
                    "archetype": c.get('archetype', 'None'),
                    "goat_banlist": c.get('banlist_info', {}).get('banlist_goat', 'Unlimited'),
                    "first_set": c.get('card_sets', [{}])[0].get("set_name", "Unknown Set") if c.get('card_sets') else "Unknown Set"
                }

                if "Monster" in tipo_api:
                    if "Fusion" in tipo_api: sub_tipo = "Fusion"
                    elif "Ritual" in tipo_api: sub_tipo = "Ritual"
                    elif "Normal" in tipo_api: sub_tipo = "Normal"
                    else: sub_tipo = "Effect"
                    
                    card_obj["type"] = f"Monster ({sub_tipo})"
                    card_obj["attribute"] = c.get('attribute', '-').upper()
                    card_obj["race"] = c.get('race', '-')
                    card_obj["level"] = c.get('level', 0)
                    card_obj["atk"] = c.get('atk', 0)
                    card_obj["def"] = c.get('def', 0)
                else:
                    card_obj["type"] = "Spell" if "Spell" in tipo_api else "Trap"
                    card_obj["property"] = c.get('race', 'Normal')

                if "scale" in c: card_obj["scale"] = c["scale"]
                if "linkval" in c: card_obj["linkval"] = c["linkval"]
                if "linkmarkers" in c: card_obj["linkmarkers"] = c["linkmarkers"]

                clean_name = "".join([char for char in name_raw if char not in r'<>:"/\\|?*'])
                card_obj["image_filename"] = f"{custom_id} - {clean_name}.jpg"

                final_db.append(card_obj)

            path_json = os.path.join(folder, "cards_ultimate.json")
            with open(path_json, "w", encoding="utf-8") as f:
                json_lib.dump(final_db, f, indent=2, ensure_ascii=False)
            if not cancel_task: progresso["log"].append("✓ JSON Completo gerado!")

        elif tipo == 'img':
            progresso["status"] = "Baixando Imagens (Multi-Thread)..."
            images_folder = os.path.join(folder, "Images")
            alt_folder = os.path.join(images_folder, "Alt_Arts")
            os.makedirs(images_folder, exist_ok=True)
            os.makedirs(alt_folder, exist_ok=True)

            def download_image(c):
                if cancel_task: return
                name_raw = c['name']
                clean_name = "".join([char for char in name_raw if char not in r'<>:"/\\|?*'])
                custom_id = f"{c['custom_id']:04d}"

                images = c.get('card_images', [])
                for idx, img_info in enumerate(images):
                    img_url = img_info.get('image_url', '')
                    if not img_url: continue

                    if idx == 0: path = os.path.join(images_folder, f"{custom_id} - {clean_name}.jpg")
                    else:
                        # Salva Artes Alternativas
                        path = os.path.join(alt_folder, f"{custom_id}_Alt{idx} - {clean_name}.jpg")

                    if not os.path.exists(path):
                        # Sistema de Retry Inteligente (Tenta 3 vezes antes de dar erro)
                        for attempt in range(3):
                            try:
                                img_data = requests.get(img_url, headers=headers, timeout=10).content
                                with open(path, 'wb') as f: f.write(img_data)
                                progresso["log"].append(f"✓ IMG: {custom_id}" + (" (Alt)" if idx > 0 else ""))
                                break
                            except Exception as e:
                                if attempt == 2:
                                    progresso["log"].append(f"✗ IMG FALHA: {custom_id}")
                                    errors_list.append(f"Falha de Rede ({name_raw}): {e}")
                                time.sleep(1)
                    else:
                        progresso["log"].append(f"✓ IMG Existe: {custom_id}" + (" (Alt)" if idx > 0 else ""))

            for i, c in enumerate(cards, 1): c['custom_id'] = i
            completed = 0
            with concurrent.futures.ThreadPoolExecutor(max_workers=10) as executor:
                futures = {executor.submit(download_image, c): c for c in cards}
                for future in concurrent.futures.as_completed(futures):
                    if cancel_task: break
                    completed += 1
                    progresso["atual"] = completed
                    progresso["card"] = futures[future]['name']
                    while len(progresso["log"]) > 15: progresso["log"].pop(0)

        elif tipo == 'lua':
            progresso["status"] = "Baixando Scripts Lua (Multi-Thread)..."
            lua_dir = os.path.join(folder, "LuaScripts")
            os.makedirs(lua_dir, exist_ok=True)

            def download_lua(c):
                if cancel_task: return
                name_raw = c.get('name', 'Unknown')
                custom_id = f"{c['custom_id']:04d}"
                official_id = str(c.get('id', ''))
                tipo_api = c.get('type', '')

                if "Normal Monster" in tipo_api and "Effect" not in tipo_api:
                    return

                lua_path = os.path.join(lua_dir, f"c{custom_id}.lua")
                if not os.path.exists(lua_path):
                    success = False
                    for base_url in LUA_URLS:
                        for attempt in range(2): # 2 Retries por repositório
                            try:
                                r = requests.get(base_url.format(official_id), timeout=5)
                                if r.status_code == 200:
                                    with open(lua_path, 'w', encoding='utf-8') as f: f.write(r.text)
                                    success = True
                                    break
                            except: time.sleep(0.5)
                        if success: break

                    if success: 
                        progresso["log"].append(f"✓ Lua: c{custom_id}.lua")
                    else:
                        progresso["log"].append(f"✗ Falha LUA: {custom_id}")
                        errors_list.append(f"Nenhum repo possui o LUA de: {name_raw} ({official_id})")
                else:
                    progresso["log"].append(f"✓ Lua Existe: c{custom_id}.lua")

            for i, c in enumerate(cards, 1): c['custom_id'] = i
            completed = 0
            with concurrent.futures.ThreadPoolExecutor(max_workers=10) as executor:
                futures = {executor.submit(download_lua, c): c for c in cards}
                for future in concurrent.futures.as_completed(futures):
                    if cancel_task: break
                    completed += 1
                    progresso["atual"] = completed
                    progresso["card"] = futures[future]['name']
                    while len(progresso["log"]) > 15: progresso["log"].pop(0)

        elif tipo == 'audit':
            progresso["status"] = "Auditoria de Arquivos Faltantes..."
            images_folder = os.path.join(folder, "Images")
            lua_dir = os.path.join(folder, "LuaScripts")
            missing_images, missing_luas = [], []

            for i, c in enumerate(cards, 1):
                if cancel_task: break
                custom_id = f"{i:04d}"
                clean_name = "".join([char for char in c['name'] if char not in r'<>:"/\\|?*'])
                
                if not os.path.exists(os.path.join(images_folder, f"{custom_id} - {clean_name}.jpg")):
                    missing_images.append(f"{custom_id} - {c['name']}")
                    
                tipo_api = c.get('type', '')
                if not ("Normal Monster" in tipo_api and "Effect" not in tipo_api):
                    if not os.path.exists(os.path.join(lua_dir, f"c{custom_id}.lua")):
                        missing_luas.append(f"{custom_id} - {c['name']}")
                
                progresso["atual"] = i
                progresso["card"] = c.get('name', 'Scanner')
            
            audit_file = os.path.join(folder, "audit_report.txt")
            with open(audit_file, "w", encoding="utf-8") as f:
                f.write(f"=== Auditoria (Imagens Faltando: {len(missing_images)}) ===\n")
                for item in missing_images: f.write(f"- {item}\n")
                f.write(f"\n=== Auditoria (Luas Faltando: {len(missing_luas)}) ===\n")
                for item in missing_luas: f.write(f"- {item}\n")
            progresso["log"].append(f"✓ Auditoria salva em audit_report.txt")

        elif tipo == 'zip':
            progresso["status"] = "Empacotando em arquivo ZIP..."
            zip_filename = os.path.join(folder, "YuGiOh_Database.zip")
            
            # Conta arquivos para a barra de progresso
            total_files = sum([len(f) for r, d, f in os.walk(folder) if "YuGiOh_Database.zip" not in f])
            progresso["total"] = total_files
            current_file = 0
            
            with zipfile.ZipFile(zip_filename, 'w', zipfile.ZIP_DEFLATED) as zipf:
                for root_dir, dirs, files in os.walk(folder):
                    for file in files:
                        if file == "YuGiOh_Database.zip": continue
                        if cancel_task: break
                        file_path = os.path.join(root_dir, file)
                        zipf.write(file_path, os.path.relpath(file_path, folder))
                        
                        current_file += 1
                        progresso["atual"] = current_file
                        progresso["card"] = file
                        progresso["log"].append(f"Zippando: {file[:20]}...")
                        while len(progresso["log"]) > 15: progresso["log"].pop(0)
            progresso["log"].append(f"✓ Pacote ZIP criado com sucesso!")

        if cancel_task:
            progresso["log"].append("⚠ TAREFA CANCELADA PELO USUÁRIO.")
            progresso["status"] = "Cancelado"
        elif errors_list:
            error_file = os.path.join(folder, "error_log.txt")
            with open(error_file, "a", encoding="utf-8") as ef:
                ef.write(f"\n=== Relatório de Erros - Tarefa: {tipo} ===\n")
                for err in errors_list:
                    ef.write(f"- {err}\n")
            progresso["log"].append("⚠ Alguns erros ocorreram. Veja error_log.txt")
            progresso["status"] = "Concluído com Erros"
        
        if not cancel_task and not errors_list:
            progresso["log"].append("✓ Tarefa concluída com sucesso!")
            progresso["status"] = "Finalizado!"
    except Exception as e:
        progresso["status"] = f"Erro: {str(e)}"

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
        folder = filedialog.askdirectory(title="Selecione a pasta para salvar os dados")
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
    threading.Thread(target=task_executor, args=(d['type'], d['folder'], d['start'], d['end'], d['region'], d.get('txt_cols', {}))).start()
    return jsonify({"ok": True})

@app.route('/status')
def status(): return jsonify(progresso)

if __name__ == '__main__':
    app.run(debug=True)