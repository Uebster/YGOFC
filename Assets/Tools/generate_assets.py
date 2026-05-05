from flask import Flask, render_template_string, request, jsonify
import json
import os
import re
import threading
import tkinter as tk
from tkinter import filedialog

app = Flask(__name__)

progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}
cancel_task = False
report_file_path = ""

# Configuração de Caminhos
BASE_DIR = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Correções de Nomes (Typos e Padronização)
NAME_FIXES = {
    "Enchanted Javeling": "Enchanted Javelin",
    "Lightning Conger-": "Lightning Conger",
    "CYBER-TECH ALLIGATOR": "Cyber-Tech Alligator",
    "Dark Spirit of the Slient": "Dark Spirit of the Silent",
    "Blue-eyes White Dragon": "Blue-Eyes White Dragon",
    "Blue-eyes Ultimate Dragon": "Blue-Eyes Ultimate Dragon",
    "Red-eyes B. Dragon": "Red-Eyes B. Dragon",
    "Hitotsu-me Giant": "Hitotsu-Me Giant",
    "Ryu-kishin": "Ryu-Kishin",
    "Ryu-kishin Powered": "Ryu-Kishin Powered",
    "Tri-horned Dragon": "Tri-Horned Dragon",
    "Serpent Night Dragon": "Serpent Night Dragon",
    "Dark-piercing Light": "Dark-Piercing Light",
    "Yamata Dragon Scroll": "Dragon Scroll"
}

# --- BANLIST MESTRA ---
BANLIST_NAMES = {
    "Raigeki": 0, "Dark Hole": 0, "Monster Reborn": 0, "Harpie's Feather Duster": 0,
    "Change of Heart": 0, "Imperial Order": 0, "Chaos Emperor Dragon - Envoy of the End": 0,
    "Yata-Garasu": 0, "Magical Scientist": 0, "Witch of the Black Forest": 0,
    "Cyber Jar": 0, "Fiber Jar": 0, "Makyura the Destructor": 0, "Painful Choice": 0,
    "The Forceful Sentry": 0, "Confiscation": 0, "Mirage of Nightmare": 0,
    
    "Black Luster Soldier - Envoy of the Beginning": 1, "Jinzo": 1, "Breaker the Magical Warrior": 1,
    "Tribe-Infecting Virus": 1, "Sinister Serpent": 1, "Exiled Force": 1, "D.D. Warrior Lady": 1,
    "Sangan": 1, "Morphing Jar": 1, "Dark Magician of Chaos": 1, "Pot of Greed": 1,
    "Graceful Charity": 1, "Delinquent Duo": 1, "Heavy Storm": 1, "Mystical Space Typhoon": 1,
    "Snatch Steal": 1, "Premature Burial": 1, "Swords of Revealing Light": 1, "Mirror Force": 1,
    "Call of the Haunted": 1, "Ring of Destruction": 1, "Torrential Tribute": 1,
    "Magic Cylinder": 1, "Exodia the Forbidden One": 1, "Right Arm of the Forbidden One": 1,
    "Left Arm of the Forbidden One": 1, "Right Leg of the Forbidden One": 1, "Left Leg of the Forbidden One": 1,

    "Upstart Goblin": 2, "Reinforcement of the Army": 2, "Creature Swap": 2,
    "Level Limit - Area B": 2, "Gravity Bind": 2, "Good Goblin Housekeeping": 2,
    "Magician of Faith": 2, "Apprentice Magician": 2, "Night Assailant": 2
}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <link rel="icon" href="data:,">
    <title>Asset Generator Studio</title>
    <style>
        :root {
            --cyan: #00e5ff;
            --purple: #b537f2;
            --gold: #d4af37;
            --green: #39ff14;
            --red: #ff073a;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 40px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--cyan); padding: 35px; 
            border-radius: 20px; width: 800px; box-shadow: 0 0 40px rgba(0, 229, 255, 0.15);
            display: flex; flex-direction: column; max-height: 90vh;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--cyan); text-shadow: 0 0 10px var(--cyan), 0 0 20px rgba(0, 229, 255, 0.5);
        }
        label { font-size: 0.9em; color: #ccc; font-weight: bold; display: block; margin-bottom: 6px; }
        .form-group { margin-bottom: 15px; }
        
        input[type="text"], select { 
            width: 100%; padding: 12px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 10px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input:focus, select:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 12px rgba(0, 229, 255, 0.4); }
        
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 12px; padding: 10px 15px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; display: flex; justify-content: center; align-items: center; text-align: center; white-space: nowrap;
        }
        .action-btn:hover { color: #000 !important; text-shadow: none; transform: translateY(-2px); }

        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan); }

        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold); }

        .red-btn { color: var(--red); border-color: var(--red); box-shadow: inset 0 0 8px rgba(255,7,58,0.3); text-shadow: 0 0 5px var(--red); }
        .red-btn:hover { background: var(--red); box-shadow: inset 0 0 20px var(--red); color: #fff !important; }
        
        .green-btn { color: var(--green); border-color: var(--green); box-shadow: inset 0 0 8px rgba(57,255,20,0.3); text-shadow: 0 0 5px var(--green); }
        .green-btn:hover { background: var(--green); box-shadow: inset 0 0 20px var(--green); color: #000 !important; }

        .row-flex { display: flex; gap: 15px; align-items: center; }

        .progress-container { background: #050505; border: 1px solid var(--cyan); height: 22px; border-radius: 11px; overflow: hidden; box-shadow: 0 0 10px rgba(0,229,255,0.2); margin-top: 15px; flex-shrink: 0;}
        .progress-bar { width: 0%; height: 100%; background: var(--cyan); box-shadow: 0 0 15px var(--cyan); transition: width 0.3s ease; }
        
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
        ::-webkit-scrollbar-thumb:hover { background: var(--cyan); box-shadow: 0 0 10px var(--cyan); }
    </style>
</head>
<body>
    <div class="panel-container">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; flex-shrink: 0;">
            <h1 class="neon-title" style="margin: 0;">Asset Generator Studio</h1>
            <button id="btn_toggle_config" class="action-btn cyan-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▼ Mostrar Config</button>
        </div>

        <div id="config_bar">
            <div class="form-group">
                <label>1. Lista de Cartas (TXT / MD / CSV)</label>
                <div class="input-group">
                    <input type="text" id="txt_path" placeholder="Caminho do arquivo com a lista...">
                    <button class="action-btn cyan-btn" onclick="selectFile()">Procurar</button>
                </div>
            </div>
            
            <div class="form-group">
                <label>2. Pasta de Imagens (Para extrair o prefixo exato)</label>
                <div class="input-group">
                    <input type="text" id="img_dir" placeholder="Pasta de destino das imagens (Opcional)...">
                    <button class="action-btn cyan-btn" onclick="selectFolder('img_dir')">Procurar</button>
                </div>
            </div>

            <div class="form-group">
                <label>3. Onde salvar o JSON?</label>
                <div class="input-group">
                    <input type="text" id="out_dir" placeholder="Pasta de destino do cards.json...">
                    <button class="action-btn cyan-btn" onclick="selectFolder('out_dir')">Procurar</button>
                </div>
            </div>

            <div class="row-flex" style="margin-top: 20px;">
                <select id="era_prefix" style="width: 150px;">
                    <option value="DM">Era DM</option>
                    <option value="GX">Era GX</option>
                    <option value="5D">Era 5D's</option>
                    <option value="ZX">Era ZEXAL</option>
                    <option value="AV">Era ARC-V</option>
                    <option value="VR">Era VRAINS</option>
                    <option value="">Geral/Custom</option>
                </select>
                <div style="flex-grow: 1;"></div>
                <button class="action-btn red-btn" onclick="cancelProcess()">Parar</button>
                <button class="action-btn gold-btn" onclick="startTask()">🚀 GERAR CARDS.JSON</button>
                <button class="action-btn green-btn" id="btn_open" style="display: none;" onclick="openReport()">📄 Abrir JSON</button>
            </div>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info">Sistema Ocioso e Pronto.</p>
        
        <div id="log">
            <div style="color: var(--cyan);">[ ASSET GENERATOR WEB V2.0 ]</div>
            <div>- Preparado para conversão de Listas em JSON OCGCore.</div>
        </div>
    </div>

    <script>
        let interval;

        function selectFile() {
            fetch('/select_file_src').then(r => r.json()).then(d => {
                if(d.path) document.getElementById('txt_path').value = d.path;
            });
        }

        function selectFolder(inputId) {
            fetch('/select_folder').then(r => r.json()).then(d => {
                if(d.folder) document.getElementById(inputId).value = d.folder;
            });
        }

        function startTask() {
            document.getElementById('btn_open').style.display = 'none';
            const data = {
                txt_path: document.getElementById('txt_path').value,
                img_dir: document.getElementById('img_dir').value,
                out_dir: document.getElementById('out_dir').value,
                era_prefix: document.getElementById('era_prefix').value
            };
            
            if (!data.txt_path || !data.out_dir) {
                alert("Selecione o arquivo de origem e a pasta de destino!");
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
                        document.getElementById('btn_open').style.display = 'block';
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

def clean_name(name):
    name = name.strip()
    return NAME_FIXES.get(name, name)

def sanitize_filename(name):
    """Remove caracteres inválidos para nomes de arquivo (ex: : ? " < > | *)."""
    return re.sub(r'[<>:"/\\|?*]', '', name)

def parse_poc_cards(filepath, game_name):
    """Parser para arquivos no formato Power of Chaos (Yugi/Kaiba/Joey)"""
    progresso["log"].append(f"-> Processando formato Power of Chaos: {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Regex ajustado para capturar qualquer total de cartas (ex: [123/155] ou [123/350])
    pattern = re.compile(r"--(.+?)--\s\[(\d+)/\d+\]\n(.+?)(?=\n--|\Z)", re.DOTALL)
    matches = pattern.findall(content)

    for name_raw, num, body in matches:
        name = clean_name(name_raw)
        
        card = {
            "id": int(num),
            "name": name,
            "description": ""
        }

        lines = body.strip().split('\n')
        for line in lines:
            if ':' in line:
                key, val = line.split(':', 1)
                key = key.strip().lower()
                val = val.strip()
                
                if key == "level": card["level"] = int(val)
                elif key == "attribute": card["attribute"] = val.upper()
                elif key == "type":
                    if val in ["Spell", "Trap", "Equip", "Continuous", "Quick-Play", "Counter", "Field", "Ritual", "Normal"]:
                        if val in ["Spell", "Trap"]:
                            card["type"] = val
                        else:
                            if "type" not in card: card["type"] = "Spell"
                            card["property"] = val
                    else:
                        card["type"] = "Monster"
                        card["race"] = val
                elif key == "attack": card["atk"] = int(val)
                elif key == "defense": card["def"] = int(val)
                elif key == "description": card["description"] = val
            else:
                if "description" in card: card["description"] += " " + line.strip()

        # Fallback para tipo se não detectado
        if "atk" not in card and "type" not in card:
             prop = card.get("property", "")
             if prop in ["Counter", "Continuous", "Normal"] and "Trap" in card.get("description", ""):
                 card["type"] = "Trap"
             else:
                 card["type"] = "Spell"

        cards[name.lower()] = card
    
    return cards

def parse_fm_cards(filepath, game_name):
    """Parser específico para o formato do Forbidden Memories (Tab separated com Starchips/Passwords)"""
    progresso["log"].append(f"-> Processando formato Forbidden Memories: {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    for line in lines:
        parts = line.strip().split('\t')
        if len(parts) < 4: continue
        
        try:
            fm_id = int(parts[0])
            name = clean_name(parts[1])
            raw_type = parts[2]
            
            card = {
                "id": fm_id,
                "name": name,
                "description": "No description available."
            }
            
            if raw_type == "Monster":
                card["type"] = "Monster"
                card["race"] = parts[3]
                card["level"] = int(parts[4])
                card["atk"] = int(parts[5])
                card["def"] = int(parts[6])
                if len(parts) > 7: card["password"] = parts[7]
                if len(parts) > 8: card["starchips"] = parts[8]
            else:
                # Mapeamento de tipos do FM
                if raw_type == "Magic": card["type"] = "Spell"; card["property"] = "Normal"
                elif raw_type == "Equip": card["type"] = "Spell"; card["property"] = "Equip"
                elif raw_type == "Ritual": card["type"] = "Spell"; card["property"] = "Ritual"
                elif raw_type == "Trap": card["type"] = "Trap"; card["property"] = "Normal"
                elif raw_type == "Field": card["type"] = "Spell"; card["property"] = "Field"
                
                if len(parts) > 3: card["password"] = parts[3]
                if len(parts) > 4: card["starchips"] = parts[4]

            cards[name.lower()] = card
        except ValueError:
            continue
            
    return cards

def parse_tsv_cards(filepath, game_name):
    """Parser Genérico para arquivos separados por TAB (comum em listas de GBA/Wiki)"""
    progresso["log"].append(f"-> Processando formato Genérico (TSV): {game_name}")
    cards = {}
    
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    for line in lines:
        parts = line.strip().split('\t')
        if len(parts) < 2: continue
        
        # Ignora cabeçalho se existir
        if parts[0] == "Nº": continue

        try:
            # Tenta usar a lógica do FM se tiver colunas suficientes (ID, Nome, Tipo...)
            if len(parts) >= 3:
                fm_id = parts[0]
                name = clean_name(parts[1])
                raw_type = parts[2]
                
                card = {
                    "id": str(fm_id),
                    "name": name,
                    "description": ""
                }
                
                # Detecta formato do download_cards.py (10 colunas ou mais)
                # 0:Num, 1:Name, 2:Type, 3:Attr, 4:Race/Prop, 5:Lvl, 6:Atk, 7:Def, 8:ID, 9:Desc
                if len(parts) >= 9 and ("Monster" in raw_type or "Spell" in raw_type or "Trap" in raw_type):
                    if "Monster" in raw_type:
                        card["type"] = raw_type
                        card["attribute"] = parts[3]
                        card["race"] = parts[4]
                        card["level"] = int(parts[5]) if parts[5].isdigit() else 0
                        card["atk"] = int(parts[6]) if parts[6].isdigit() else 0
                        card["def"] = int(parts[7]) if parts[7].isdigit() else 0
                        card["password"] = parts[8]
                        if len(parts) > 9: card["description"] = parts[9]
                    else:
                        # Magia ou Armadilha
                        # Define o Tipo Principal (Spell ou Trap)
                        card["type"] = "Spell" if "Spell" in raw_type else "Trap"
                        # Define a Propriedade (Normal, Continuous, Equip, etc.)
                        card["property"] = parts[4] if parts[4].strip() else "Normal"
                        card["password"] = parts[8]
                        if len(parts) > 9: card["description"] = parts[9]
                
                # Lógica antiga para outros formatos TSV (ex: GBA dumps)
                elif raw_type.startswith("Monster") and len(parts) >= 7:
                    card["type"] = raw_type
                    card["race"] = parts[3]
                    card["level"] = int(parts[4])
                    card["atk"] = int(parts[5])
                    card["def"] = int(parts[6])
                    if len(parts) > 7: card["password"] = parts[7]
                    if len(parts) > 8: card["description"] = parts[8] # Captura descrição
                elif any(x in raw_type for x in ["Magic", "Spell", "Trap", "Equip", "Ritual", "Field"]):
                    if "Trap" in raw_type: 
                        card["type"] = "Trap"
                        card["property"] = "Normal"
                    elif raw_type in ["Magic", "Spell"]: 
                        card["type"] = "Spell"
                        card["property"] = "Normal"
                    else: 
                        card["type"] = "Spell"
                        card["property"] = raw_type
                    if len(parts) > 3: card["password"] = parts[3]
                
                    # Suporte para formato estendido (com descrição e ID na coluna 7/8)
                    if len(parts) >= 9:
                        card["description"] = parts[8]
                        # Se a coluna 7 for numérica, é o ID/Password, não a coluna 3 (que seria Raça/Propriedade)
                        if len(parts) > 7 and parts[7].isdigit():
                            card["password"] = parts[7]
                    elif len(parts) >= 5:
                        card["description"] = parts[4]

                cards[name.lower()] = card
            else:
                # Fallback se tiver apenas ID e Nome
                cards[clean_name(parts[1]).lower()] = {
                    "id": str(parts[0]), 
                    "name": clean_name(parts[1]), 
                }
        except:
            continue
    
    return cards

def generate_task(txt_filepath, img_dir, out_dir, era_prefix):
    global progresso, cancel_task, report_file_path
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Processando...", "log": []}
    
    try:
        if not os.path.exists(txt_filepath):
            progresso["log"].append("ERRO: Arquivo de origem não encontrado.")
            progresso["status"] = "Erro"
            return
            
        if not os.path.exists(out_dir):
            progresso["log"].append("ERRO: Pasta de destino não encontrada.")
            progresso["status"] = "Erro"
            return

        images_subdir_name = os.path.basename(img_dir.rstrip('\\/')) if img_dir else "Images"
        
        master_db = {}
        total_processed = 0
        
        filename = os.path.basename(txt_filepath)
        game_name = os.path.splitext(filename)
        progresso["log"].append(f"Lendo arquivo: {filename}")
        
        format_type = "tsv"
        with open(txt_filepath, 'r', encoding='utf-8', errors='ignore') as f:
            header = f.read(1000)
            if "Nº\tNOME\tTIPO" in header: format_type = "tsv"
            elif "--" in header and "[" in header and "]" in header: format_type = "poc"
            elif "Monster (" in header: format_type = "tsv"
            elif "\t" in header and ("Monster" in header or "Magic" in header): format_type = "fm"
        
        if format_type == "poc": extracted = parse_poc_cards(txt_filepath, game_name)
        elif format_type == "fm": extracted = parse_fm_cards(txt_filepath, game_name)
        else: extracted = parse_tsv_cards(txt_filepath, game_name)
        
        if cancel_task: return
        
        count_new = 0
        for name_key, card_data in extracted.items():
            total_processed += 1
            master_db[name_key] = card_data
            count_new += 1
                
        progresso["log"].append(f"-> Total lido: {count_new} cartas.")

        final_list = list(master_db.values())
        final_list.sort(key=lambda x: x["name"])
        
        progresso["log"].append(f"-> Mapeando imagens com prefixo: {images_subdir_name}")
        progresso["total"] = len(final_list)
        
        for i, card in enumerate(final_list):
            if cancel_task: return
            progresso["atual"] = i + 1
            
            custom_id = str(card["id"])
            safe_name = sanitize_filename(card["name"])
            if img_dir:
                card["image_filename"] = f"{images_subdir_name}/{custom_id} - {safe_name}.jpg"
            else:
                card["image_filename"] = f"{custom_id} - {safe_name}.jpg"
                
            c_name = card.get("name", "")
            if c_name in BANLIST_NAMES:
                limit = BANLIST_NAMES[c_name]
                card["goat_banlist"] = "Banned" if limit == 0 else ("Limited" if limit == 1 else "Semi-Limited")
            else:
                card["goat_banlist"] = "Unlimited"

        if era_prefix == "MD" or era_prefix == "CUSTOM": era_prefix = ""
        base_name = f"cards{era_prefix}" if era_prefix else "cards"
        ext = ".json"
        final_path = os.path.join(out_dir, base_name + ext)
        
        counter = 1
        while os.path.exists(final_path):
            final_path = os.path.join(out_dir, f"{base_name}_{counter}{ext}")
            counter += 1

        with open(final_path, 'w', encoding='utf-8') as f:
            json.dump(final_list, f, indent=2, ensure_ascii=False)
            
        progresso["log"].append(f"=== Sucesso! ===")
        progresso["log"].append(f"Total de cartas processadas (bruto): {total_processed}")
        progresso["log"].append(f"Total de cartas únicas no JSON: {len(final_list)}")
        progresso["log"].append(f"Arquivo salvo em: {final_path}")
        
        report_file_path = final_path
        progresso["status"] = "Finalizado!"
        
    except Exception as e:
        progresso["log"].append(f"ERRO: {str(e)}")
        progresso["status"] = f"Erro: {str(e)}"

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/select_file_src')
def select_file_src():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        p = filedialog.askopenfilename(title="Selecione a Lista (TXT/MD/CSV)", filetypes=[("Text Files", "*.txt;*.md;*.csv"), ("Todos os Arquivos", "*.*")])
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

@app.route('/cancel', methods=['POST'])
def cancel():
    global cancel_task
    cancel_task = True
    return jsonify({"ok": True})

@app.route('/start', methods=['POST'])
def start():
    d = request.json
    threading.Thread(target=generate_task, args=(d['txt_path'], d['img_dir'], d['out_dir'], d['era_prefix'])).start()
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
    app.run(debug=True, port=5006)
