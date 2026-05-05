from flask import Flask, render_template_string, request, jsonify
import json
import os
import csv
import tkinter as tk
from tkinter import filedialog
import math
import shutil
import datetime

app = Flask(__name__)

# Estado Global
global_cards = []
global_json_path = ""

# Template Base (Pode ser editado pelo usuário na UI)
DEFAULT_PREDEFINED = {
    "Pot of Greed": "MAX.1", "Raigeki": "MAX.1", "Dark Hole": "MAX.1", "Monster Reborn": "MAX.1",
    "Change of Heart": "MAX.1", "Harpie's Feather Duster": "MAX.1", "Graceful Charity": "MAX.1",
    "Delinquent Duo": "MAX.1", "The Forceful Sentry": "MAX.1", "Confiscation": "MAX.1",
    "Snatch Steal": "MAX.1", "Premature Burial": "MAX.1", "Imperial Order": "MAX.1",
    "Mirror Force": "MAX.1", "Call of the Haunted": "MAX.1", "Ring of Destruction": "MAX.1",
    "Jinzo": "MAX.1", "Magical Scientist": "MAX.1", "Cyber Jar": "MAX.1", "Fiber Jar": "MAX.1",
    "Thousand-Eyes Restrict": "MAX.1", "Barrel Dragon": "MAX.1",
    "Black Luster Soldier - Envoy of the Beginning": "MAX.MAX",
    "Chaos Emperor Dragon - Envoy of the End": "MAX.MAX",
    "Injection Fairy Lily": "4.5", "Spirit Reaper": "4.5", "Marshmallon": "4.5", "Sinister Serpent": "4.5",
    "Tribe-Infecting Virus": "4.5", "D.D. Warrior Lady": "4.5", "D.D. Assailant": "4.5", "Sangan": "4.5",
    "Witch of the Black Forest": "4.5", "Morphing Jar": "4.5", "Mask of Darkness": "4.1", "Exiled Force": "4.1",
    "Relinquished": "4.1", "Black Illusion Ritual": "4.1", "Legendary Jujitsu Master": "4.1", "Axe of Despair": "4.1",
    "United We Stand": "4.5", "Mage Power": "4.5", "Copycat": "3.5", "Penguin Soldier": "3.5",
    "Man-Eater Bug": "3.5", "Magician of Faith": "3.5", "Senju of the Thousand Hands": "3.5",
    "Sonic Bird": "3.5", "Wall of Illusion": "3.5", "Slate Warrior": "3.5", "Trap Hole": "3.1",
    "Bottomless Trap Hole": "3.5", "Kuriboh": "2.5"
}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <link rel="icon" href="data:,">
    <title>Pool & Tier Studio</title>
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
            display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 20px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--purple); padding: 30px; 
            border-radius: 20px; width: 1400px; box-shadow: 0 0 40px rgba(181, 55, 242, 0.15);
            display: flex; flex-direction: column; height: 95vh; box-sizing: border-box;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--cyan); text-shadow: 0 0 10px var(--cyan), 0 0 20px rgba(0, 229, 255, 0.5);
        }
        
        .top-bar { display: flex; flex-direction: column; gap: 15px; margin-bottom: 10px; background: #050505; padding: 15px; border-radius: 12px; border: 1px solid #333; }
        .row-flex { display: flex; gap: 15px; align-items: center; }
        
        input[type="text"], input[type="number"], select, textarea { 
            padding: 8px 12px; background: #1a1a1a; border: 1px solid #444; 
            color: #fff; border-radius: 6px; font-family: inherit; width: 100%; box-sizing: border-box;
        }
        input:focus, textarea:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 8px rgba(0, 229, 255, 0.3); }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 8px; padding: 10px 20px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; white-space: nowrap;
        }
        .action-btn:hover { transform: translateY(-2px); color: #000 !important; text-shadow: none !important;}
        
        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan); }
        
        .purple-btn { color: var(--purple); border-color: var(--purple); box-shadow: inset 0 0 8px rgba(181,55,242,0.3); text-shadow: 0 0 5px var(--purple); }
        .purple-btn:hover { background: var(--purple); box-shadow: inset 0 0 20px var(--purple); }
        
        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold); }
        
        .green-btn { color: var(--green); border-color: var(--green); box-shadow: inset 0 0 8px rgba(57,255,20,0.3); text-shadow: 0 0 5px var(--green); }
        .green-btn:hover { background: var(--green); box-shadow: inset 0 0 20px var(--green); }

        #predefined_editor_panel { display: none; margin-top: 10px; }
        #predefined_json { height: 150px; font-family: 'Consolas', monospace; font-size: 0.9em; resize: vertical; }

        .table-container { flex-grow: 1; overflow-y: auto; background: #050505; border: 1px solid #222; border-radius: 8px; margin-top: 10px; }
        table { width: 100%; border-collapse: collapse; text-align: left; font-size: 0.9em; }
        th { background: #111; color: var(--gold); padding: 12px; position: sticky; top: 0; border-bottom: 2px solid #333; z-index: 10; text-transform: uppercase; }
        td { padding: 10px 12px; border-bottom: 1px solid #222; vertical-align: middle; }
        tr:hover { background: rgba(0, 229, 255, 0.05); }
        
        .pool-input { width: 70px !important; text-align: center; font-weight: bold; color: var(--cyan) !important; border-color: var(--cyan) !important;}
        
        .pagination { display: flex; justify-content: center; align-items: center; gap: 15px; margin-top: 15px; padding: 10px; background: #050505; border-radius: 8px; border: 1px solid #222; flex-shrink: 0;}
        
        #notification { text-align: center; font-weight: bold; color: var(--gold); background: #222; padding: 10px; border-radius: 8px; margin-bottom: 10px; min-height: 20px; font-size: 1.1em; border: 1px solid #444;}

        ::-webkit-scrollbar { width: 8px; height: 8px;}
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #444; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--purple); }
    </style>
</head>
<body>
    <div class="panel-container">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; flex-shrink: 0;">
            <h1 class="neon-title" style="margin: 0;">Pool & Tier Studio</h1>
            <button id="btn_toggle_config" class="action-btn purple-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▲ Ocultar Config</button>
        </div>
        
        <div id="notification">Aguardando dados... (Selecione o arquivo JSON e clique em Carregar)</div>
        
        <div class="top-bar" id="config_bar">
            <div class="row-flex">
                <div style="flex: 1;">
                    <label>1. Arquivo cards.json</label>
                    <div style="display: flex; gap: 10px;">
                        <input type="text" id="json_path" placeholder="Caminho do cards.json...">
                        <button class="action-btn cyan-btn" style="padding: 8px 15px;" onclick="selectFile()">Procurar</button>
                    </div>
                </div>
                <div style="width: 150px;">
                    <label>Max Major Pools</label>
                    <input type="number" id="max_major" value="5" min="1" max="20">
                </div>
                <div style="width: 150px;">
                    <label>Max Sub Pools</label>
                    <input type="number" id="max_minor" value="5" min="1" max="20">
                </div>
                <div style="align-self: flex-end;">
                    <button class="action-btn gold-btn" onclick="loadAndCalculate()">🚀 CARREGAR & CALCULAR</button>
                </div>
            </div>
            
            <div class="row-flex">
                <button class="action-btn" style="padding: 6px 12px; font-size: 0.8em; color: #aaa; border-color: #555;" onclick="togglePredefined()">⚙️ Editar Regras Predefinidas (JSON)</button>
            </div>
            
            <div id="predefined_editor_panel">
                <label>Insira nomes exatos e Tiers blindados. Use "MAX.MAX" ou "MAX.1" para acompanhar a escala dinamicamente.</label>
                <textarea id="predefined_json"></textarea>
            </div>
        </div>

        <div class="row-flex" style="margin-bottom: 10px; flex-shrink: 0;">
            <input type="text" id="search_input" placeholder="🔍 Buscar por Nome ou ID..." oninput="applySearch()" style="max-width: 300px;">
            <div style="flex-grow: 1;"></div>
            <button class="action-btn" style="padding: 8px 15px; color: #fff; border-color: #555;" onclick="exportCSV()">📊 Exportar CSV</button>
            <button class="action-btn green-btn" style="padding: 8px 15px;" onclick="applyToJson()">💾 APLICAR NO CARDS.JSON</button>
        </div>

        <div class="table-container">
            <table id="cards_table">
                <thead>
                    <tr>
                        <th width="80">ID</th>
                        <th width="200">Nome</th>
                        <th width="150">Tipo</th>
                        <th width="100">Status</th>
                        <th>Descrição Curta</th>
                        <th width="100" style="text-align: center;">Sugestão</th>
                        <th width="100" style="text-align: center;">Pool Final</th>
                    </tr>
                </thead>
                <tbody id="table_body">
                    <!-- Rows will be populated here -->
                </tbody>
            </table>
        </div>
        
        <div class="pagination">
            <button class="action-btn" style="padding: 6px 12px; border-color: var(--cyan); color: var(--cyan);" onclick="firstPage()">« Primeira</button>
            <button class="action-btn purple-btn" style="padding: 6px 12px;" onclick="prevPage()">‹ Anterior</button>
            <span id="page_info">Página 1 de X</span>
            <button class="action-btn purple-btn" style="padding: 6px 12px;" onclick="nextPage()">Próxima ›</button>
            <button class="action-btn" style="padding: 6px 12px; border-color: var(--cyan); color: var(--cyan);" onclick="lastPage()">Última »</button>
        </div>
    </div>

    <script>
        let allCards = [];
        let filteredCards = [];
        let currentPage = 1;
        const rowsPerPage = 100;

        // Fetch initial defaults
        fetch('/get_defaults').then(r => r.json()).then(d => {
            document.getElementById('predefined_json').value = JSON.stringify(d, null, 2);
        });

        function selectFile() {
            fetch('/select_file').then(r => r.json()).then(d => {
                if(d.path) document.getElementById('json_path').value = d.path;
            });
        }

        function togglePredefined() {
            const p = document.getElementById('predefined_editor_panel');
            p.style.display = p.style.display === 'none' ? 'block' : 'none';
        }

        let isConfigVisible = true;
        function toggleConfig() {
            const configBar = document.getElementById('config_bar');
            const btnToggle = document.getElementById('btn_toggle_config');
            isConfigVisible = !isConfigVisible;
            if(isConfigVisible) {
                configBar.style.display = 'flex';
                btnToggle.innerText = '▲ Ocultar Config';
            } else {
                configBar.style.display = 'none';
                btnToggle.innerText = '▼ Mostrar Config';
            }
        }

        function notify(msg, isError=false) {
            const n = document.getElementById('notification');
            n.innerText = msg;
            n.style.color = isError ? "var(--red)" : "var(--cyan)";
            if (isError) alert(msg);
        }

        function loadAndCalculate() {
            let predefined = {};
            try {
                predefined = JSON.parse(document.getElementById('predefined_json').value);
            } catch (e) {
                alert("Erro de sintaxe no JSON das Regras Predefinidas!");
                return;
            }

            const data = {
                path: document.getElementById('json_path').value,
                max_major: parseInt(document.getElementById('max_major').value),
                max_minor: parseInt(document.getElementById('max_minor').value),
                predefined: predefined
            };

            notify("Calculando estimativas...");
            fetch('/calculate', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) })
            .then(r => r.json()).then(d => {
                if (d.error) { notify(d.error, true); }
                else {
                    allCards = d.cards;
                    applySearch(); // Filters and renders
                    notify(`Sucesso! ${allCards.length} cartas calculadas.`);
                    
                    document.getElementById('btn_toggle_config').style.display = 'block';
                    if(isConfigVisible) toggleConfig(); // Auto-hide
                }
            });
        }

        function applySearch() {
            const q = document.getElementById('search_input').value.toLowerCase();
            if (!q) {
                filteredCards = allCards;
            } else {
                filteredCards = allCards.filter(c => 
                    c.name.toLowerCase().includes(q) || 
                    String(c.id).toLowerCase().includes(q)
                );
            }
            currentPage = 1;
            renderTable();
        }

        function renderTable() {
            const tbody = document.getElementById('table_body');
            tbody.innerHTML = '';
            
            const totalPages = Math.ceil(filteredCards.length / rowsPerPage) || 1;
            if (currentPage > totalPages) currentPage = totalPages;
            document.getElementById('page_info').innerText = `Página ${currentPage} de ${totalPages} (Total: ${filteredCards.length})`;
            
            const start = (currentPage - 1) * rowsPerPage;
            const end = start + rowsPerPage;
            const pageCards = filteredCards.slice(start, end);
            
            pageCards.forEach(c => {
                let stats = "-";
                if(c.type.includes("Monster")) stats = `${c.atk} / ${c.def !== undefined ? c.def : '-'}`;
                
                let shortDesc = c.description ? c.description.substring(0, 80) + "..." : "";
                
                // Encontra o index real no array global para atualizar corretamente
                const globalIndex = allCards.findIndex(gc => gc.id === c.id);
                const finalVal = c.final_pool ? c.final_pool : "";
                
                tbody.innerHTML += `
                    <tr>
                        <td>${c.id}</td>
                        <td style="color: var(--gold); font-weight: bold;">${c.name}</td>
                        <td>${c.type}</td>
                        <td>${stats}</td>
                        <td style="color: #999; font-style: italic;">${shortDesc}</td>
                        <td style="text-align: center; color: #aaa;">${c.suggested_pool}</td>
                        <td style="text-align: center;">
                            <input type="text" class="pool-input" value="${finalVal}" placeholder="${c.suggested_pool}" onchange="updatePool(${globalIndex}, this.value)">
                        </td>
                    </tr>
                `;
            });
        }
        
        function updatePool(index, val) {
            allCards[index].final_pool = val;
        }

        function prevPage() { if (currentPage > 1) { currentPage--; renderTable(); } }
        function nextPage() { if (currentPage < Math.ceil(filteredCards.length / rowsPerPage)) { currentPage++; renderTable(); } }
        function firstPage() { if (currentPage > 1) { currentPage = 1; renderTable(); } }
        function lastPage() { const maxP = Math.ceil(filteredCards.length / rowsPerPage) || 1; if (currentPage < maxP) { currentPage = maxP; renderTable(); } }

        function exportCSV() {
            if(allCards.length === 0) return alert("Calcule as cartas primeiro!");
            fetch('/export_csv', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({cards: allCards, path: document.getElementById('json_path').value}) })
            .then(r => r.json()).then(d => {
                if(d.ok) notify(`CSV Exportado com sucesso: ${d.path}`);
                else notify("Erro ao exportar CSV: " + d.error, true);
            });
        }

        function applyToJson() {
            if(allCards.length === 0) return alert("Calcule as cartas primeiro!");
            if(!confirm("Atenção! Isso vai injetar a propriedade 'pool' diretamente no cards.json selecionado.\\nUm backup (.bak) será criado. Deseja continuar?")) return;
            
            fetch('/apply_json', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({cards: allCards, path: document.getElementById('json_path').value}) })
            .then(r => r.json()).then(d => {
                if(d.ok) {
                    notify(`SUCESSO! Cartas atualizadas. Backup: ${d.backup}`);
                    alert("cards.json atualizado com sucesso!");
                }
                else notify("Erro ao salvar JSON: " + d.error, true);
            });
        }
    </script>
</body>
</html>
"""

def scale_value(old_val, new_max):
    """Mapeia um valor da escala de 1 a 5 para a nova escala 1 a new_max"""
    if old_val <= 1: return 1
    return max(1, round(old_val * (new_max / 5.0)))

def do_estimation(card, predefined, max_maj, max_min):
    name = card.get("name", "")
    
    if not max_maj: max_maj = 5
    if not max_min: max_min = 5
    
    if name in predefined:
        val = predefined[name]
        if "MAX.MAX" in val: return f"{max_maj}.{max_min}"
        if "MAX.1" in val: return f"{max_maj}.1"
        # Se o usuário chumbou um número na UI, respeitamos, mas tentamos escalar
        try:
            parts = val.split('.')
            if len(parts) == 2:
                mj = scale_value(float(parts[0]), max_maj)
                mn = scale_value(float(parts[1]), max_min)
                return f"{mj}.{mn}"
        except: pass
        return val

    goat_status = card.get("goat_banlist", "Unlimited")
    if goat_status == "Banned": return f"{max_maj}.{max_min}"
    if goat_status == "Limited": return f"{max(1, max_maj - scale_value(1, max_maj))}.{max_min}"
    if goat_status == "Semi-Limited": return f"{max(1, max_maj - scale_value(2, max_maj))}.{max_min}"

    pool_major = 1
    pool_minor = 1
    
    desc = card.get("description", "").lower()
    card_type = card.get("type", "")

    if card_type.startswith("Monster"):
        level = card.get("level", 1)
        atk = card.get("atk", 0)
        defense = card.get("def", 0)
        max_stat = max(atk, defense)
        
        if max_stat < 500: pool_major = 1; pool_minor = 1
        elif max_stat < 800: pool_major = 1; pool_minor = 3
        elif max_stat < 1200: pool_major = 1; pool_minor = 5
        elif max_stat < 1400: pool_major = 2; pool_minor = 1
        elif max_stat < 1600: pool_major = 2; pool_minor = 3
        elif max_stat < 1800: pool_major = 2; pool_minor = 5
        elif max_stat < 1900: pool_major = 3; pool_minor = 1
        elif max_stat < 2100: pool_major = 3; pool_minor = 3
        elif max_stat < 2400: pool_major = 3; pool_minor = 5 
        elif max_stat < 2600: pool_major = 4; pool_minor = 1 
        elif max_stat < 3000: pool_major = 4; pool_minor = 3
        else: pool_major = 5; pool_minor = 1 

        is_extra = "Fusion" in card_type or "Ritual" in card_type or "Synchro" in card_type or "Xyz" in card_type or "Link" in card_type
        if not is_extra:
            if level <= 4 and atk >= 1900: pool_major += 1
            elif level in [5, 6] and max_stat < 2000: pool_major -= 1
            elif level >= 7 and max_stat < 2500: pool_major -= 1
        
        if "Effect" in card_type or "Flip" in card_type:
            pool_minor += 2
            if pool_minor > 5:
                pool_minor = pool_minor % 5
                if pool_minor == 0: pool_minor = 1
                pool_major += 1

        if is_extra: pool_major = max(pool_major, 3)
    else:
        pool_major = 2; pool_minor = 3
        strong_keywords = ["destroy all", "draw 2", "change of heart", "monster reborn", "control", "negate", "special summon", "equip"]
        weak_keywords = ["increase", "500 points", "def", "gain"]
        if any(k in desc for k in strong_keywords) or any(k in name.lower() for k in strong_keywords):
            pool_major = 3; pool_minor = 5
        elif any(k in desc for k in weak_keywords):
            pool_major = 1; pool_minor = 4

    if pool_major > 5: pool_major = 5
    if pool_major < 1: pool_major = 1
    if pool_minor > 5: pool_minor = 5
    if pool_minor < 1: pool_minor = 1
    
    if pool_major == 5 and pool_minor > 1: pool_minor = 1

    # Escala matemática final baseada na UI
    final_maj = scale_value(pool_major, max_maj)
    final_min = scale_value(pool_minor, max_min)
    
    # Tetos absolutos
    if final_maj > max_maj: final_maj = max_maj
    if final_min > max_min: final_min = max_min

    return f"{final_maj}.{final_min}"

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/get_defaults')
def get_defaults(): return jsonify(DEFAULT_PREDEFINED)

@app.route('/select_file')
def select_file():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        p = filedialog.askopenfilename(title="Selecione cards.json", filetypes=[("JSON Files", "*.json")])
        root.destroy()
        return jsonify({"path": p})
    except: return jsonify({"path": ""})

@app.route('/calculate', methods=['POST'])
def calculate():
    data = request.json
    path = data.get('path')
    if not path or not os.path.exists(path): return jsonify({"error": "Arquivo JSON não encontrado!"})
    
    try:
        with open(path, 'r', encoding='utf-8') as f:
            cards = json.load(f)
            
        for c in cards:
            # Preserva pool salvo se já existir, senão calcula novo
            c['final_pool'] = c.get('pool', "")
            c['suggested_pool'] = do_estimation(c, data['predefined'], data['max_major'], data['max_minor'])
            
        return jsonify({"cards": cards})
    except Exception as e:
        return jsonify({"error": str(e)})

@app.route('/export_csv', methods=['POST'])
def export_csv():
    data = request.json
    cards = data.get('cards', [])
    base_path = data.get('path', '')
    if not base_path: return jsonify({"error": "Caminho base inválido."})
    
    out_dir = os.path.dirname(base_path)
    out_csv = os.path.join(out_dir, f"card_pools_export.csv")
    
    try:
        with open(out_csv, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(["ID", "Name", "Type", "ATK", "DEF", "Description", "Suggested_Pool", "Final_Pool"])
            for c in cards:
                desc = c.get("description", "").replace("\n", " ")[:100]
                writer.writerow([c.get("id"), c.get("name"), c.get("type"), c.get("atk", ""), c.get("def", ""), desc, c.get("suggested_pool"), c.get("final_pool")])
        
        # Abre o arquivo no Excel/SO automaticamente
        try: os.startfile(out_csv)
        except: pass
        
        return jsonify({"ok": True, "path": out_csv})
    except Exception as e:
        return jsonify({"ok": False, "error": str(e)})

@app.route('/apply_json', methods=['POST'])
def apply_json():
    data = request.json
    cards = data.get('cards', [])
    path = data.get('path', '')
    if not path or not os.path.exists(path): return jsonify({"error": "Caminho base inválido."})
    
    try:
        # Cria Backup
        bak_path = f"{path}.bak_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}"
        shutil.copy2(path, bak_path)
        
        # Aplica a lógica
        for c in cards:
            final = c.get("final_pool", "").strip()
            if not final: final = c.get("suggested_pool", "1.1")
            
            # Formatação Básica Segura
            try: 
                parts = final.split('.')
                if len(parts) != 2: final = "1.1"
            except: final = "1.1"
            
            # Mantém apenas as chaves reais que a Engine espera (limpa as sujeiras geradas pro front-end)
            if 'suggested_pool' in c: del c['suggested_pool']
            if 'final_pool' in c: del c['final_pool']
            c['pool'] = final
            
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(cards, f, indent=2, ensure_ascii=False)
            
        return jsonify({"ok": True, "backup": os.path.basename(bak_path)})
    except Exception as e:
        return jsonify({"ok": False, "error": str(e)})

if __name__ == "__main__":
    # Usa porta 5004
    app.run(debug=True, port=5004)