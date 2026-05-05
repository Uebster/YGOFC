from flask import Flask, render_template_string, request, jsonify
import json
import os
import random
import re
import threading
import tkinter as tk
from tkinter import filedialog

app = Flask(__name__)

progresso = {"atual": 0, "total": 0, "status": "Pronto", "card": "", "log": []}
cancel_task = False
report_file_path = ""

# --- DEFAULT CONFIGURATION DICT (Transposed to UI) ---
DEFAULT_CONFIG = {
    "ACT_POOL_RANGES": {
        "1": [1.1, 1.5], "2": [1.3, 2.1], "3": [2.1, 2.5], "4": [2.4, 3.2],
        "5": [3.1, 3.5], "6": [3.3, 4.1], "7": [3.5, 4.3], "8": [4.1, 4.5],
        "9": [4.3, 5.2], "10": [4.5, 5.5]
    },
    "FORCED_CARDS": {
        "kaiba": ["Blue-Eyes White Dragon", "Blue-Eyes White Dragon", "Blue-Eyes White Dragon", "Master of Oz"],
        "yugi": ["Dark Magician"],
        "joey": ["Red-Eyes B. Dragon"],
        "pegasus": ["Relinquished", "Toon World"],
        "mai": ["Harpie Lady", "Harpie Lady", "Harpie Lady", "Harpie Lady Sisters"],
        "weevil": ["Great Moth", "Cocoon of Evolution", "Petit Moth"],
        "rex": ["Two-Headed King Rex"],
        "mako": ["The Legendary Fisherman"],
        "bandit": ["Barrel Dragon"],
        "keith": ["Barrel Dragon"],
        "marik": ["Lava Golem"],
        "strings": ["Revival Jam", "Jam Breeding Machine"],
        "bakura": ["Dark Necrofear"],
        "ishizu": ["Exchange of the Spirit"],
        "odion": ["Embodiment of Apophis"],
        "noah": ["Shinato, King of a Higher Plane"],
        "gozaburo": ["Exodia Necross"],
        "rare_hunter": ["Exodia the Forbidden One"],
        "arkana": ["Dark Magician Girl"],
        "shadi": ["Millennium Shield"],
        "heishin": ["Zera the Mant"],
        "isis": ["Senju of the Thousand Hands"]
    },
    "THEMES": {
        "mako": {"attribute": "Water", "race": "Aqua", "core": "Umi"},
        "ocean": {"attribute": "Water", "race": "Aqua", "core": "Umi"},
        "weevil": {"attribute": "Earth", "race": "Insect", "core": "Insect"},
        "forest": {"attribute": "Earth", "race": "Insect", "core": "Insect"},
        "rex": {"attribute": "Earth", "race": "Dinosaur", "core": "Dinosaur"},
        "mountain": {"attribute": "Wind", "race": "Dragon", "core": "Harpie"},
        "mai": {"attribute": "Wind", "race": "Winged Beast", "core": "Harpie"},
        "joey": {"attribute": "Earth", "race": "Warrior", "core": "Warrior"},
        "meadow": {"attribute": "Earth", "race": "Warrior", "core": "Warrior"},
        "yugi": {"attribute": "Dark", "race": "Spellcaster", "core": "Spellcaster"},
        "arkana": {"attribute": "Dark", "race": "Spellcaster", "core": "Spellcaster"},
        "bakura": {"attribute": "Dark", "race": "Fiend", "core": "DestinyBoard"},
        "marik": {"attribute": "Dark", "race": "Fiend", "core": "Burn"},
        "keith": {"attribute": "Dark", "race": "Machine", "core": "Machine"},
        "machine": {"attribute": "Dark", "race": "Machine", "core": "Machine"},
        "ishizu": {"attribute": "Earth", "race": "Fairy", "core": "Fairy"},
        "rare_hunter": {"attribute": "Dark", "race": "Spellcaster", "core": "Exodia"},
        "desert": {"attribute": "Earth", "race": "Zombie", "core": "Zombie"},
        "labyrinth": {"attribute": "Dark", "race": "Fiend", "core": "Fiend"},
        "pegasus": {"attribute": "Dark", "race": "Spellcaster", "core": "Toon"}
    },
    "CORES": {
        "Exodia": ["Exodia the Forbidden One", "Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One", "Contract with Exodia", "Exodia Necross"],
        "Umi": ["Umi", "Umi", "Umi", "A Legendary Ocean", "A Legendary Ocean", "Tornado Wall", "Tornado Wall", "Amphibian Bugroth MK-3", "Amphibian Bugroth MK-3", "The Legendary Fisherman", "Ocean Dragon Lord - Neo-Daedalus", "Levia-Dragon - Daedalus"], 
        "Burn": ["Solar Flare Dragon", "Solar Flare Dragon", "Just Desserts", "Just Desserts", "Bowganian", "Ojama Trio", "Des Koala", "Tremendous Fire", "Meteor of Destruction"], 
        "Insect": ["Insect Barrier", "Insect Barrier", "Insect Princess", "Pinch Hopper", "Pinch Hopper", "Insect Imitation", "Multiplication of Ants", "Flying Kamakiri #1", "Flying Kamakiri #1"], 
        "Dinosaur": ["Uraby", "Uraby", "Tyrant Dragon", "Two-Headed King Rex", "Two-Headed King Rex", "Enraged Battle Ox", "Mad Sword Beast", "Dark Driceratops"], 
        "Zombie": ["Blood Sucker", "Blood Sucker", "Vampire Lord", "Pyramid Turtle", "Pyramid Turtle", "Call of the Mummy", "Robbin' Zombie", "Mystic Tomato", "Despair from the Dark"], 
        "Warrior": ["Marauding Captain", "Marauding Captain", "Goblin Attack Force", "Goblin Attack Force", "Exiled Force", "Command Knight", "D.D. Warrior Lady", "Amazoness Swords Woman", "Reinforcement of the Army", "Reinforcement of the Army"], 
        "DestinyBoard": ["Destiny Board", "Destiny Board", "Spirit Message \"I\"", "Spirit Message \"N\"", "Spirit Message \"A\"", "Spirit Message \"L\"", "The Dark Door", "The Dark Door"], 
        "Toon": ["Toon World", "Toon World", "Toon World", "Toon Mermaid", "Toon Summoned Skull", "Toon Dark Magician Girl", "Toon Goblin Attack Force", "Toon Goblin Attack Force", "Toon Masked Sorcerer", "Blue-Eyes Toon Dragon"], 
        "Spellcaster": ["Magical Scientist", "Magical Scientist", "Magician of Faith", "Breaker the Magical Warrior", "Skilled Dark Magician", "Skilled Dark Magician", "Dark Magician Girl", "Pitch-Black Power Stone", "Apprentice Magician"], 
        "Machine": ["Limiter Removal", "Limiter Removal", "Heavy Mech Support Platform", "Jinzo", "Machine Duplication", "Cyber Dragon", "Reflect Bounder"], 
        "Harpie": ["Harpie Lady", "Harpie Lady", "Harpie Lady", "Elegant Egotist", "Elegant Egotist", "Harpies' Hunting Ground", "Harpies' Hunting Ground", "Harpie Lady Sisters", "Harpie's Brother", "Cyber Shield", "Cyber Shield"], 
        "Gravekeeper": ["Necrovalley", "Necrovalley", "Necrovalley", "Gravekeeper's Spy", "Gravekeeper's Spy", "Gravekeeper's Chief", "Gravekeeper's Spear Soldier", "Gravekeeper's Assailant", "Gravekeeper's Vassal"], 
        "Fiend": ["Dark Necrofear", "Giant Orc", "Skill Drain", "Archfiend Soldier", "Nightmare Wheel", "Dark Ruler Ha Des", "Wall of Illusion", "Wall of Illusion"], 
        "Fairy": ["The Sanctuary in the Sky", "The Sanctuary in the Sky", "The Sanctuary in the Sky", "Archlord Zerato", "The Agent of Judgment - Saturn", "Mudora", "Shining Angel", "Shining Angel", "Zolga"]
    },
    "STAPLES_TIER_1": ["Pot of Greed", "Graceful Charity", "Delinquent Duo", "Mystical Space Typhoon", "Snatch Steal", "Premature Burial"],
    "STAPLES_TIER_2": ["Heavy Storm", "Nobleman of Crossout", "Book of Moon", "Call of the Haunted", "Mirror Force", "Torrential Tribute"],
    "STAPLES_TIER_3": ["Dust Tornado", "Sakuretsu Armor", "Bottomless Trap Hole", "Smashing Ground", "Waboku", "Magic Cylinder"],
    "EXACT_DEPENDENCIES": {
        "Exodia the Forbidden One": ["Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One"], 
        "Exodia Necross": ["Exodia the Forbidden One", "Left Arm of the Forbidden One", "Left Leg of the Forbidden One", "Right Arm of the Forbidden One", "Right Leg of the Forbidden One", "Contract with Exodia"], 
        "Relinquished": ["Black Illusion Ritual"], 
        "Black Luster Soldier": ["Black Luster Ritual"], 
        "Magician of Black Chaos": ["Black Magic Ritual"], 
        "Blue-Eyes Toon Dragon": ["Toon World"], 
        "Toon Dark Magician Girl": ["Toon World"], 
        "Harpie Lady Sisters": ["Elegant Egotist"], 
        "Perfectly Ultimate Great Moth": ["Petit Moth", "Cocoon of Evolution"], 
        "Great Moth": ["Petit Moth", "Cocoon of Evolution"], 
        "The Masked Beast": ["Curse of the Masked Beast"], 
        "Blue-Eyes Shining Dragon": ["Blue-Eyes Ultimate Dragon", "Blue-Eyes White Dragon", "Polymerization"], 
        "Dark Sage": ["Dark Magician", "Time Wizard"]
    },
    "ALLOW_FORBIDDEN": True
}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <link rel="icon" href="data:,">
    <title>Deck & Rewards Generator Studio</title>
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
            background: var(--panel-bg); border: 2px solid var(--green); padding: 35px; 
            border-radius: 20px; width: 850px; box-shadow: 0 0 40px rgba(57, 255, 20, 0.15);
            display: flex; flex-direction: column; max-height: 90vh;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--green); text-shadow: 0 0 10px var(--green), 0 0 20px rgba(57, 255, 20, 0.5);
        }
        label { font-size: 0.9em; color: #ccc; font-weight: bold; display: block; margin-bottom: 6px; }
        .form-group { margin-bottom: 15px; }
        
        input[type="text"] { 
            width: 100%; padding: 12px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 10px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input:focus { outline: none; border-color: var(--green); box-shadow: 0 0 12px rgba(57, 255, 20, 0.4); }
        
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

        .row-flex { display: flex; gap: 15px; align-items: center; }

        .progress-container { background: #050505; border: 1px solid var(--green); height: 22px; border-radius: 11px; overflow: hidden; box-shadow: 0 0 10px rgba(57, 255, 20, 0.2); margin-top: 15px; flex-shrink: 0;}
        .progress-bar { width: 0%; height: 100%; background: var(--green); box-shadow: 0 0 15px var(--green); transition: width 0.3s ease; }
        
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
            <h1 class="neon-title" style="margin: 0;">Deck Generator Studio</h1>
            <button id="btn_toggle_config" class="action-btn green-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▼ Mostrar Config</button>
        </div>

        <div id="config_bar">
            <div class="form-group">
                <label>1. Base de Cartas (cards.json)</label>
                <div class="input-group">
                    <input type="text" id="cards_path" placeholder="Caminho do cards.json...">
                    <button class="action-btn cyan-btn" onclick="selectFile('cards_path')">Procurar</button>
                </div>
            </div>
            
            <div class="form-group">
                <label>2. Base de Personagens (characters.json com apenas os IDs gerados)</label>
                <div class="input-group">
                    <input type="text" id="chars_path" placeholder="Caminho do characters.json base...">
                    <button class="action-btn cyan-btn" onclick="selectFile('chars_path')">Procurar</button>
                </div>
            </div>

            <div class="form-group">
                <label>3. Onde Salvar o Resultado (Pasta de Destino)</label>
                <div class="input-group">
                    <input type="text" id="out_dir" placeholder="Será salvo como characters_generated.json...">
                    <button class="action-btn cyan-btn" onclick="selectFolder('out_dir')">Procurar</button>
                </div>
            </div>

            <label style="margin-top: 15px; color: var(--gold);">⚙️ Configurações Avançadas (Aceita Nomes OU IDs exatos)</label>
            <textarea id="advanced_config" style="height: 250px; font-family: 'Consolas', monospace; font-size: 12px; background: #050505; color: #0f0; border: 1px solid #333; padding: 10px; border-radius: 8px; width: 100%; box-sizing: border-box; resize: vertical;"></textarea>

            <div class="row-flex" style="margin-top: 20px;">
                <label style="color: #fff; margin:0;">Qtd. Decks por Personagem:</label>
                <input type="number" id="num_decks" value="3" min="1" max="5" style="width: 60px; padding: 6px;">
                <div style="flex-grow: 1;"></div>
                <button class="action-btn red-btn" onclick="cancelProcess()">Parar</button>
                <button class="action-btn cyan-btn" onclick="startTask()">🚀 GERAR DECKS</button>
                <button class="action-btn green-btn" id="btn_open" style="display: none;" onclick="openReport()">📄 Abrir Decks (JSON)</button>
            </div>
        </div>

        <div class="progress-container">
            <div class="progress-bar" id="bar"></div>
        </div>
        <p id="info">Sistema Ocioso e Pronto.</p>
        
        <div id="log">
            <div style="color: var(--green);">[ DECK GENERATOR WEB V2.0 ]</div>
            <div>- Motor procedural de Baralhos (A, B e C) e Lógica de Drops.</div>
        </div>
    </div>

    <script>
        let interval;
        
        // Busca os defaults do Python e injeta no textarea
        fetch('/get_defaults').then(r => r.json()).then(d => {
            document.getElementById('advanced_config').value = JSON.stringify(d, null, 4);
        });

        function selectFile(inputId) {
            fetch('/select_file_src').then(r => r.json()).then(d => {
                if(d.path) document.getElementById(inputId).value = d.path;
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
                cards_path: document.getElementById('cards_path').value,
                chars_path: document.getElementById('chars_path').value,
                out_dir: document.getElementById('out_dir').value,
                config: document.getElementById('advanced_config').value,
                num_decks: parseInt(document.getElementById('num_decks').value)
            };
            
            if (!data.cards_path || !data.out_dir || !data.chars_path) {
                alert("ATENÇÃO: Selecione os arquivos de origem (Cards e Characters) e a pasta de destino!");
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
 
def format_deck_list_custom(data_list, indent_level=4):
    if not data_list: return "[]"
    lines = []
    chunk_size = 10
    base_indent = " " * indent_level
    item_indent = " " * (indent_level + 2)
    for i in range(0, len(data_list), chunk_size):
        chunk = data_list[i:i + chunk_size]
        quoted_chunk = [f'"{x}"' for x in chunk]
        lines.append(f"{item_indent}{', '.join(quoted_chunk)}")
    content = ",\n".join(lines)
    return f"[\n{content}\n{base_indent}]"

def generate_task(cards_path, chars_path, out_dir, config_json, num_decks):
    global progresso, cancel_task, report_file_path
    cancel_task = False
    progresso = {"atual": 0, "total": 0, "status": "Processando...", "card": "", "log": []}
    
    try:
        if not os.path.exists(cards_path):
            progresso["log"].append("ERRO: Arquivo cards.json não encontrado.")
            progresso["status"] = "Erro"
            return
            
        if not os.path.exists(out_dir):
            progresso["log"].append("ERRO: Pasta de destino não encontrada.")
            progresso["status"] = "Erro"
            return

        try:
            config = json.loads(config_json)
        except Exception as e:
            progresso["log"].append(f"ERRO DE SINTAXE NO JSON DE CONFIGURAÇÃO: {e}")
            progresso["status"] = "Erro"
            return

        with open(cards_path, 'r', encoding='utf-8') as f: cards_data = json.load(f)
        
        has_pool = any("pool" in c for c in cards_data)
        if not has_pool:
            progresso["log"].append("ERRO: O arquivo cards.json não possui Tiers/Pools. Use o Pool Studio primeiro para criar a hierarquia das cartas!")
            progresso["status"] = "Erro"
            return
        
        all_cards_map = {c["id"]: c for c in cards_data}
        
        cards_by_pool = {}
        for c in cards_data:
            try: pool_val = float(c.get("pool", "1.1"))
            except: pool_val = 1.1
            if pool_val not in cards_by_pool: cards_by_pool[pool_val] = []
            cards_by_pool[pool_val].append(c["id"])

        NAME_TO_ID = {c["name"].lower(): c["id"] for c in cards_data}
        
        def resolve_ids(items_list):
            resolved = []
            for item in items_list:
                item_str = str(item)
                if item_str in all_cards_map: resolved.append(item_str)
                elif item_str.lower() in NAME_TO_ID: resolved.append(NAME_TO_ID[item_str.lower()])
                else:
                    try:
                        num_id = str(int(item_str))
                        if num_id in all_cards_map: resolved.append(num_id)
                    except: pass
            return resolved

        cfg_forced = config.get("FORCED_CARDS", {})
        FORCED_CARDS = {k: resolve_ids(v) for k, v in cfg_forced.items()}
        
        cfg_cores = config.get("CORES", {})
        CORES = {k: resolve_ids(v) for k, v in cfg_cores.items()}
        
        STAPLES_TIER_1 = resolve_ids(config.get("STAPLES_TIER_1", []))
        STAPLES_TIER_2 = resolve_ids(config.get("STAPLES_TIER_2", []))
        STAPLES_TIER_3 = resolve_ids(config.get("STAPLES_TIER_3", []))
        
        EXACT_DEPENDENCIES = {}
        for k, v in config.get("EXACT_DEPENDENCIES", {}).items():
            key_id = resolve_ids([k])
            if key_id:
                EXACT_DEPENDENCIES[key_id[0]] = resolve_ids(v)
                
        THEMES = config.get("THEMES", {})
        ALLOW_FORBIDDEN = config.get("ALLOW_FORBIDDEN", True)
        ACT_POOL_RANGES_RAW = config.get("ACT_POOL_RANGES", {})
        ACT_POOL_RANGES = {int(k): tuple(v) for k, v in ACT_POOL_RANGES_RAW.items()}

        def get_act_from_id(char_id):
            match = re.match(r"(\d+)_", char_id)
            if match:
                num = int(match.group(1))
                return max(1, min(10, (num - 1) // 10 + 1))
            return 1

        def get_pool_candidates(min_p, max_p, cards_by_pool, all_cards_map):
            candidates = []
            for pool_val, ids in cards_by_pool.items():
                if min_p <= pool_val <= max_p:
                    for cid in ids:
                        if cid in all_cards_map and "Token" not in all_cards_map[cid]["type"]:
                            candidates.append(cid)
            return candidates
            
        deck_letters = ['A', 'B', 'C', 'D', 'E'][:num_decks]

        def generate_deck(char_id, act, difficulty_modifier, forced_cards=None, unique_pool=None):
            main_deck = []
            extra_deck = []
            
            theme = None
            for k, v in THEMES.items():
                if k in char_id.lower():
                    theme = v
                    break

            def get_valid_main_count():
                count = 0
                for cid in main_deck:
                    c = all_cards_map.get(cid, {})
                    if c.get("goat_banlist", "Unlimited") != "Banned":
                        count += 1
                return count

            def add_card(cid, is_forced=False):
                if not cid or cid not in all_cards_map: return False
                c_data = all_cards_map[cid]
                
                limit = 3
                goat_status = c_data.get("goat_banlist", "Unlimited")
                is_forbidden = (goat_status == "Banned")
                if is_forbidden: limit = 0
                elif goat_status == "Limited" or goat_status == "1": limit = 1
                elif goat_status == "Semi-Limited": limit = 2
                
                if ALLOW_FORBIDDEN and is_forbidden: limit = 1
                
                if not is_forced and (main_deck.count(cid) + extra_deck.count(cid)) >= limit: 
                    return False
                    
                if "Fusion" in c_data["type"] or "Synchro" in c_data["type"] or "Xyz" in c_data["type"] or "Link" in c_data["type"]:
                    if len(extra_deck) < 15: 
                        extra_deck.append(cid)
                        poly_id = NAME_TO_ID.get("polymerization")
                        if poly_id and poly_id not in main_deck and get_valid_main_count() < 40:
                            main_deck.append(poly_id)
                        return True
                    return False
                else:
                    if get_valid_main_count() < 40 or is_forced:
                        main_deck.append(cid)
                        if cid in EXACT_DEPENDENCIES:
                            for dep_id in EXACT_DEPENDENCIES[cid]:
                                if main_deck.count(dep_id) == 0 and (get_valid_main_count() < 40 or is_forced):
                                    add_card(dep_id, True)
                        return True
                    return False

            if forced_cards:
                for cid in forced_cards:
                    add_card(cid, True)
                    if difficulty_modifier in ["B", "C"]: add_card(cid, True)
            if unique_pool:
                for cid in unique_pool:
                    add_card(cid, True)

            if theme and theme.get("core") in CORES:
                for cid in CORES[theme["core"]]:
                    add_card(cid)
                    if difficulty_modifier in ["B", "C"]: add_card(cid) 
                    
            staples = STAPLES_TIER_1
            if act >= 4: staples = STAPLES_TIER_2
            if act >= 8: staples = STAPLES_TIER_3
            for cid in random.sample(staples, min(len(staples), 5)): add_card(cid)
                
            min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
            
            def get_diff_offset(letter):
                if letter == 'B': return 0.2
                if letter == 'C': return 0.4
                if letter == 'D': return 0.6
                if letter == 'E': return 0.8
                return 0.0
                
            diff_offset = get_diff_offset(difficulty_modifier)
            min_p += diff_offset; max_p += diff_offset
            
            def fetch_cards(mn, mx):
                ml, mh, sp, tr, ex = [], [], [], [], []
                for pool_val, ids in cards_by_pool.items():
                    if mn <= pool_val <= mx:
                        for cid in ids:
                            if cid not in all_cards_map: continue
                            c = all_cards_map[cid]
                            t = c.get("type", "")
                            if "Fusion" in t or "Synchro" in t or "Xyz" in t or "Link" in t:
                                ex.append(cid)
                            elif "Monster" in t:
                                if c.get("level", 1) <= 4: ml.append(cid)
                                else: mh.append(cid)
                            elif "Spell" in t:
                                if c.get("property") != "Field": sp.append(cid)
                            elif "Trap" in t: tr.append(cid)
                return ml, mh, sp, tr, ex

            monsters_low, monsters_high, spells, traps, extras = fetch_cards(min_p, max_p)
            search_min = min_p
            while len(monsters_low) < 15 and search_min > 1.0:
                search_min -= 0.5
                ml, _, _, _, _ = fetch_cards(search_min, max_p)
                monsters_low = list(set(monsters_low + ml))
                    
            def weighted_choice(pool, count):
                if not pool: return []
                weights = []
                for cid in pool:
                    w = 1.0
                    if theme:
                        c = all_cards_map[cid]
                        if "Monster" in c["type"]:
                            if c.get("race") == theme["race"]: w += 5.0
                            if c.get("attribute") == theme["attribute"]: w += 3.0
                        elif "Spell" in c["type"] and c.get("property") == "Equip":
                            desc = c.get("description", "").lower()
                            if theme["race"].lower() in desc:
                                w += 4.0
                            else:
                                w = 0.1
                    weights.append(w)
                return random.choices(pool, weights=weights, k=count)

            monsters_in_deck = sum(1 for cid in main_deck if "Monster" in all_cards_map[cid]["type"])
            spells_in_deck = sum(1 for cid in main_deck if "Spell" in all_cards_map[cid]["type"])
            traps_in_deck = sum(1 for cid in main_deck if "Trap" in all_cards_map[cid]["type"])
            
            need_low = max(0, 16 - monsters_in_deck)
            need_high = max(0, 4)
            need_spell = max(0, 10 - spells_in_deck)
            need_trap = max(0, 10 - traps_in_deck)
            need_extra = max(0, 4 - len(extra_deck))
            
            # Variação Orgânica de Deck (Mínimo 40, Máximo Aleatório entre 40 a 46)
            target_deck_size = random.randint(40, 46)
            
            fillers = []
            fillers.extend(weighted_choice(monsters_low, need_low))
            fillers.extend(weighted_choice(monsters_high, need_high))
            fillers.extend(weighted_choice(spells, need_spell))
            fillers.extend(weighted_choice(traps, need_trap))
            fillers.extend(weighted_choice(extras, need_extra))
            
            if difficulty_modifier in ["B", "C", "D", "E"]: fillers.extend(STAPLES_TIER_3 + STAPLES_TIER_2)
            else: fillers.extend(STAPLES_TIER_1)
                
            for cid in fillers:
                if get_valid_main_count() >= target_deck_size: break
                add_card(cid)

            fallback_attempts = 0
            safe_pool = monsters_low if len(monsters_low) > 0 else STAPLES_TIER_1
            while get_valid_main_count() < 40 and fallback_attempts < 1000:
                add_card(random.choice(safe_pool))
                fallback_attempts += 1

            def sort_key(cid):
                c = all_cards_map.get(cid)
                if not c: return 999
                if "Monster" in c["type"]: return 1
                if "Spell" in c["type"]: return 2
                if "Trap" in c["type"]: return 3
                return 4
                
            main_deck.sort(key=sort_key)
            extra_deck.sort(key=sort_key)
            return main_deck, extra_deck

        if not os.path.exists(chars_path):
            progresso["log"].append("ERRO: Arquivo characters.json base não encontrado.")
            progresso["status"] = "Erro"
            return
            
        with open(chars_path, 'r', encoding='utf-8') as f: characters_data = json.load(f)
        
        progresso["log"].append("-> Distribuindo cartas exclusivas...")
        used_unique_ids = set()
        char_unique_map = {}
        char_signature_map = {}
        progresso["total"] = len(characters_data)
        
        for i, char in enumerate(characters_data):
            char_id = char["id"]
            act = get_act_from_id(char_id)
            char_unique_map[char_id] = []
            
            char_id_lower = char_id.lower()
            sig_id = None
            for key, sigs in FORCED_CARDS.items():
                if key in char_id_lower and sigs:
                    sig_id = sigs[0]
                    for s in sigs:
                        if s not in used_unique_ids and s in all_cards_map:
                            char_unique_map[char_id].append(s)
                            used_unique_ids.add(s)
                    break
            
            min_p, max_p = ACT_POOL_RANGES.get(act, (1.1, 1.5))
            
            if not sig_id:
                candidates = get_pool_candidates(min_p, max_p + 0.5, cards_by_pool, all_cards_map)
                candidates = [c for c in candidates if c not in used_unique_ids]
                if candidates:
                    candidates.sort(key=lambda x: float(all_cards_map[x].get("pool", "1.1")), reverse=True)
                    sig_id = candidates[0]
                    char_unique_map[char_id].append(sig_id)
                    used_unique_ids.add(sig_id)
                    
            char_signature_map[char_id] = sig_id
            
            candidates = get_pool_candidates(min_p, max_p + 0.5, cards_by_pool, all_cards_map)
            candidates = [c for c in candidates if c not in used_unique_ids]
            
            needed = 7 - len(char_unique_map[char_id])
            if needed > 0 and candidates:
                picked = random.sample(candidates, min(len(candidates), needed))
                char_unique_map[char_id].extend(picked)
                used_unique_ids.update(picked)

        progresso["log"].append(f"-> Gerando {num_decks} decks para {len(characters_data)} personagens...")
        for i, char in enumerate(characters_data):
            if cancel_task: return
            progresso["atual"] = i + 1
            progresso["card"] = char["name"]
            
            act = get_act_from_id(char["id"])
            
            forced = []
            for key, sigs in FORCED_CARDS.items():
                if key in char["id"].lower():
                    forced.extend(sigs)

            uniques = char_unique_map.get(char["id"], [])
            
            char["signature_card"] = char_signature_map.get(char["id"], "")
            char["unique_drops"] = uniques

            for letter in deck_letters:
                main_d, extra_d = generate_deck(char["id"], act, letter, forced, uniques)
                char[f"deck_{letter}"] = main_d
                char[f"extra_deck_{letter}"] = extra_d
            
            progresso["log"].append(f"[{char['id']}] Ato {act} - Decks gerados (Main A: {len(char['deck_A'])})")
            if len(progresso["log"]) > 15: progresso["log"].pop(0)
            
        final_path = os.path.join(out_dir, "characters_decks.json")

        progresso["log"].append(f"-> Salvando formatado...")
        with open(final_path, 'w', encoding='utf-8') as f:
            lines_global = []
            for i, char in enumerate(characters_data):
                lines = []
                lines.append(f'    "id": "{char["id"]}"')
                lines.append(f'    "name": "{char.get("name", "")}"')
                
                sig_str = char.get("signature_card", "")
                uniques_str = json.dumps(char.get("unique_drops", []))
                
                lines.append(f'    "signature_card": "{sig_str}"')
                lines.append(f'    "unique_drops": {uniques_str}')

                for letter in deck_letters:
                    if f"deck_{letter}" in char:
                        lines.append(f'    "deck_{letter}": {format_deck_list_custom(char[f"deck_{letter}"])}')
                    if f"extra_deck_{letter}" in char:
                        lines.append(f'    "extra_deck_{letter}": {format_deck_list_custom(char[f"extra_deck_{letter}"])}')
                
                lines.append(f'    "field": "{char.get("field", "Normal")}"')
                lines.append(f'    "difficulty": "{char.get("difficulty", "Easy")}"')
                lines.append(f'    "story_role": "{char.get("story_role", "Duelist")}"')
                
                char_str = "  {\n" + ",\n".join(lines) + "\n  }"
                lines_global.append(char_str)
                
            f.write("[\n" + ",\n".join(lines_global) + "\n]")

        progresso["log"].append(f"=== SUCESSO ABSOLUTO ===")
        progresso["log"].append(f"Arquivo salvo em: {final_path}")
        report_file_path = final_path
        progresso["status"] = "Finalizado!"
        
    except Exception as e:
        progresso["log"].append(f"ERRO: {str(e)}")
        progresso["status"] = f"Erro: {str(e)}"

@app.route('/get_defaults')
def get_defaults(): return jsonify(DEFAULT_CONFIG)

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

@app.route('/cancel', methods=['POST'])
def cancel():
    global cancel_task
    cancel_task = True
    return jsonify({"ok": True})

@app.route('/start', methods=['POST'])
def start():
    d = request.json
    threading.Thread(target=generate_task, args=(d['cards_path'], d['chars_path'], d['out_dir'], d['config'], d.get('num_decks', 3))).start()
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
    # Porta dedicada 5008
    app.run(debug=True, port=5008)