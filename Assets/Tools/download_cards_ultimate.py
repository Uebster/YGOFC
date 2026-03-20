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
        body { 
            background: #0d0d0d; color: #d4af37; font-family: 'Segoe UI', sans-serif;
            display: flex; flex-direction: column; align-items: center; padding: 40px;
        }
        .panel { 
            background: #1a1a1a; border: 3px solid #d4af37; padding: 25px; 
            border-radius: 10px; width: 500px; box-shadow: 0 0 30px rgba(212, 175, 55, 0.2);
        }
        h1 { text-align: center; text-transform: uppercase; letter-spacing: 3px; margin-bottom: 20px; }
        label { display: block; margin-top: 15px; font-size: 0.9em; color: #aaa; }
        input, select, button.browse { 
            width: 100%; padding: 10px; margin-top: 5px; background: #222; 
            border: 1px solid #444; color: #fff; border-radius: 4px; box-sizing: border-box;
        }
        .input-group { display: flex; gap: 10px; align-items: center; }
        button.browse { flex: 0 0 auto; width: 100px; background: #555; cursor: pointer; border: none; font-weight: bold; }
        button.browse:hover { background: #777; }
        .btn-group { display: flex; gap: 10px; margin-top: 25px; flex-wrap: wrap; }
        button { 
            flex: 1 1 45%; padding: 15px; background: #d4af37; 
            color: #000; border: none; font-weight: bold; cursor: pointer; text-transform: uppercase;
        }
        button.secondary { background: #3498db; color: #fff; }
        button.tertiary { background: #27ae60; color: #fff; }
        button.quaternary { background: #9b59b6; color: #fff; }
        button.danger { background: #e74c3c; color: #fff; }
        button:hover { opacity: 0.8; }
        .progress-container { background: #000; height: 20px; border-radius: 10px; margin-top: 25px; border: 1px solid #d4af37; overflow: hidden; }
        .progress-bar { width: 0%; height: 100%; background: #d4af37; transition: 0.2s; }
        #log { 
            margin-top: 20px; height: 150px; background: #000; border: 1px solid #333; 
            padding: 10px; font-size: 0.75em; overflow-y: scroll; color: #0f0; font-family: monospace;
        }
    </style>
</head>
<body>
    <div class="panel">
        <h1>Ultimate Extractor</h1>

        <label>Nome da Pasta</label>
        <div class="input-group">
            <input type="text" id="folder" placeholder="Ex: C:/YuGiOh_Assets">
            <button class="browse" onclick="selectFolder()">Procurar...</button>
        </div>

        <label>Data Inicial</label>
        <input type="date" id="start" value="1999-01-01">

        <label>Data Final</label>
        <input type="date" id="end" value="2008-12-31">

        <label>Região</label>
        <select id="region">
            <option value="ocg">OCG (Japão)</option>
            <option value="tcg">TCG (Ocidente)</option>
        </select>

        <div class="btn-group">
            <button class="secondary" onclick="startTask('txt')">1. Lista TXT</button>
            <button class="secondary" onclick="startTask('json')">2. JSON Master</button>
            <button onclick="startTask('img')">3. Imagens</button>
            <button class="tertiary" onclick="startTask('csv')">4. Lista CSV</button>
            <button class="quaternary" onclick="startTask('lua')">5. Scripts LUA</button>
            <button class="tertiary" onclick="startTask('audit')">6. Auditoria</button>
            <button class="secondary" onclick="startTask('zip')">7. Gerar ZIP</button>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <div style="margin-top:10px; text-align:center;">
            <button class="danger" style="width:200px; padding:10px;" onclick="cancelProcess()">Parar Execução</button>
        </div>
        <p id="info" style="text-align:center; font-size:0.8em; margin-top:10px;">Aguardando...</p>
        <div id="log"></div>
    </div>

    <script>
        let interval;

        function selectFolder() {
            fetch('/select_folder').then(r => r.json()).then(d => {
                if(d.folder) document.getElementById('folder').value = d.folder;
            });
        }

        function startTask(type) {
            const data = {
                type: type,
                folder: document.getElementById('folder').value || "Ultimate_Assets",
                start: document.getElementById('start').value,
                end: document.getElementById('end').value,
                region: document.getElementById('region').value
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

def task_executor(tipo, folder, start, end, region):
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
            progresso["status"] = "Gerando TXT..."
            path_txt = os.path.join(folder, "lista_cartas.txt")
            with open(path_txt, "w", encoding="utf-8") as f:
                f.write("Nº\tNOME\tTIPO\tATRIBUTO\tRAÇA/PROPRIEDADE\tLV\tATK\tDEF\tID\tDESC\n")
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

                    if "Monster" in tipo_api:
                        # Mantém a lógica de subtipo e adiciona o atributo
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
                        archetype = c.get('archetype', 'None')
                        linha = f"{num}\t{name}\t{tipo_final}\t{attribute}\t{race}\t{level}\t{atk}\t{defe}\t{cid}\t{desc}\n"
                    else:
                        # Adiciona a propriedade (subtipo) para Magias e Armadilhas
                        property = c.get('race', 'Normal')
                        linha = f"{num}\t{name}\t{tipo_api}\t-\t{property}\t-\t-\t-\t{cid}\t{desc}\n"
                    
                    f.write(linha)
            
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
    threading.Thread(target=task_executor, args=(d['type'], d['folder'], d['start'], d['end'], d['region'])).start()
    return jsonify({"ok": True})

@app.route('/status')
def status(): return jsonify(progresso)

if __name__ == '__main__':
    app.run(debug=True)