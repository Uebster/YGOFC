from flask import Flask, render_template_string, request, jsonify, send_file
import json
import os
import sys
import re
import glob
import tkinter as tk
from tkinter import filedialog

app = Flask(__name__)

global_db = []
global_img_dir = ""
global_lua_dir = ""
image_map = {}

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>Yu-Gi-Oh! Web Card Viewer</title>
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
            border-radius: 20px; width: 1200px; box-shadow: 0 0 40px rgba(181, 55, 242, 0.15);
            display: flex; flex-direction: column; height: 95vh; box-sizing: border-box;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--purple); text-shadow: 0 0 10px var(--purple), 0 0 20px rgba(181, 55, 242, 0.5);
            flex-shrink: 0;
        }
        
        .config-bar { display: flex; flex-direction: column; gap: 10px; margin-bottom: 20px; flex-shrink: 0;}
        .input-group { display: flex; gap: 10px; align-items: stretch; }
        
        input[type="text"] { 
            flex-grow: 1; padding: 10px 15px; background: #050505; border: 1px solid #333; 
            color: #fff; border-radius: 8px; box-sizing: border-box; transition: 0.3s; font-family: inherit;
        }
        input[type="text"]:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 10px rgba(0, 229, 255, 0.3); }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 8px; padding: 10px 20px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; color: var(--cyan); border-color: var(--cyan); 
            box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan);
        }
        .action-btn:hover { background: var(--cyan); color: #000 !important; text-shadow: none; box-shadow: inset 0 0 20px var(--cyan); }
        
        .purple-btn { color: var(--purple); border-color: var(--purple); box-shadow: inset 0 0 8px rgba(181,55,242,0.3); text-shadow: 0 0 5px var(--purple); }
        .purple-btn:hover { background: var(--purple); box-shadow: inset 0 0 20px var(--purple); color: #fff !important; }

        .viewer-layout { display: flex; gap: 30px; flex-grow: 1; min-height: 0; }
        .left-col { flex: 0 0 350px; display: flex; flex-direction: column; }
        
        .card-image-container {
            width: 100%; padding-top: 145%; position: relative; border-radius: 12px; border: 2px solid var(--purple);
            box-shadow: 0 0 20px rgba(181, 55, 242, 0.2); overflow: hidden; background: #000; cursor: pointer;
        }
        .card-image-container img {
            position: absolute; top: 0; left: 0; width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s;
        }
        .alt-hint { position: absolute; bottom: 10px; right: 10px; background: rgba(0,0,0,0.8); color: var(--gold); padding: 4px 8px; border-radius: 5px; font-size: 0.8em; font-weight: bold; pointer-events: none;}
        
        .nav-controls {
            display: flex; justify-content: space-between; align-items: center; margin-top: 15px;
            background: #050505; padding: 10px; border-radius: 8px; border: 1px solid #333;
        }
        
        .right-col { flex-grow: 1; display: flex; flex-direction: column; min-width: 0; gap: 15px; }
        .card-header { border-bottom: 1px solid #333; padding-bottom: 10px; }
        .card-name { color: var(--gold); margin: 0; font-size: 1.8em; text-shadow: 0 0 10px rgba(212,175,55,0.4); white-space: nowrap; overflow: hidden; text-overflow: ellipsis;}
        
        .details-grid {
            display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; background: #050505; padding: 15px; 
            border-radius: 8px; border: 1px solid #222; font-size: 0.9em; flex-shrink: 0;
        }
        .detail-item { display: flex; flex-direction: column; }
        .detail-label { color: #888; font-size: 0.75em; text-transform: uppercase; font-weight: bold; }
        .detail-value { color: var(--cyan); font-weight: bold; }
        
        .description-box {
            background: rgba(0, 229, 255, 0.05); padding: 15px; border-left: 4px solid var(--cyan);
            border-radius: 0 8px 8px 0; font-style: italic; color: #eee; line-height: 1.5; font-size: 0.95em;
            overflow-y: auto; flex-shrink: 0; max-height: 120px;
        }
        
        .lua-box { flex-grow: 1; display: flex; flex-direction: column; min-height: 0; }
        .lua-header {
            display: flex; justify-content: space-between; align-items: center; padding: 8px 12px;
            background: #222; border-radius: 8px 8px 0 0; font-weight: bold; font-size: 0.9em;
        }
        textarea {
            width: 100%; flex-grow: 1; background: #050505; border: 1px solid #222; border-radius: 0 0 8px 8px;
            color: var(--green); font-family: 'Consolas', monospace; font-size: 0.85em; padding: 12px;
            box-sizing: border-box; resize: none; outline: none; white-space: pre;
        }
        
        ::-webkit-scrollbar { width: 8px; height: 8px;}
        ::-webkit-scrollbar-track { background: #111; border-radius: 4px; }
        ::-webkit-scrollbar-thumb { background: #444; border-radius: 4px; }
        ::-webkit-scrollbar-thumb:hover { background: var(--purple); }
    </style>
</head>
<body>
    <div class="panel-container">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; flex-shrink: 0;">
            <h1 class="neon-title" style="margin: 0;">Diagnostic Card Viewer</h1>
            <button id="btn_toggle_config" class="action-btn purple-btn" style="display: none; padding: 6px 12px; font-size: 0.8em;" onclick="toggleConfig()">▼ Mostrar Config</button>
        </div>
        
        <div class="config-bar" id="config_bar">
            <div class="input-group">
                <input type="text" id="json_path" placeholder="Caminho do cards.json (Ex: C:/YuGiOh/Assets/StreamingAssets/cards.json)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('file', 'json_path')">Procurar</button>
            </div>
            <div class="input-group">
                <input type="text" id="img_path" placeholder="Pasta de Imagens Brutas (Raiz do Download)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('folder', 'img_path')">Procurar</button>
            </div>
            <div class="input-group">
                <input type="text" id="lua_path" placeholder="Pasta de Scripts Lua (Ex: LuaScripts)">
                <button class="action-btn" style="flex: 0 0 auto;" onclick="selectPath('folder', 'lua_path')">Procurar</button>
                <button class="action-btn purple-btn" style="flex: 0 0 200px;" onclick="loadDB()">🚀 CARREGAR DB</button>
            </div>
        </div>

        <div id="viewer_section" class="viewer-layout" style="display: none;">
            <div class="left-col">
                <div class="card-image-container" onclick="cycleImage()">
                    <img id="card_image" src="" alt="Imagem da Carta">
                    <div id="alt_hint" class="alt-hint" style="display: none;">CLICK: ALT ART</div>
                </div>
                <div class="nav-controls">
                    <button class="action-btn" style="padding: 8px 15px;" onclick="prevCard()">&lt; Ant</button>
                    <span id="card_counter" style="color: #aaa; font-size: 0.9em; font-weight: bold;">0 / 0</span>
                    <button class="action-btn" style="padding: 8px 15px;" onclick="nextCard()">Próx &gt;</button>
                </div>
                <div style="margin-top: 10px;">
                     <input type="text" id="search_input" placeholder="🔍 ID, Nome ou Senha... (Pressione Enter)" onkeypress="handleSearch(event)">
                </div>
                <div style="margin-top: 15px; font-size: 0.8em; color: #666; text-align: center;">
                    Suporte Futuro: Integração com .md em desenvolvimento.
                </div>
            </div>

            <div class="right-col">
                <div class="card-header">
                    <h2 id="c_name" class="card-name">Nome da Carta</h2>
                </div>
                
                <div class="details-grid">
                    <div class="detail-item"><span class="detail-label">ID Custom</span><span class="detail-value" id="c_id">-</span></div>
                    <div class="detail-item"><span class="detail-label">Password API</span><span class="detail-value" id="c_pass">-</span></div>
                    <div class="detail-item"><span class="detail-label">Tipo Básico</span><span class="detail-value" id="c_type">-</span></div>
                    <div class="detail-item"><span class="detail-label">Typeline Crua</span><span class="detail-value" id="c_typeline">-</span></div>
                    <div class="detail-item"><span class="detail-label">Atributo</span><span class="detail-value" id="c_attr">-</span></div>
                    <div class="detail-item"><span class="detail-label">Raça / Prop.</span><span class="detail-value" id="c_race">-</span></div>
                    <div class="detail-item"><span class="detail-label">Nível / Link</span><span class="detail-value" id="c_level">-</span></div>
                    <div class="detail-item"><span class="detail-label">ATK / DEF</span><span class="detail-value" id="c_stats">-</span></div>
                    <div class="detail-item"><span class="detail-label">Arquétipo</span><span class="detail-value" id="c_arch">-</span></div>
                    <div class="detail-item"><span class="detail-label">Goat Banlist</span><span class="detail-value" id="c_goat">-</span></div>
                    <div class="detail-item"><span class="detail-label">Tier / Pool</span><span class="detail-value" id="c_pool">-</span></div>
                    <div class="detail-item"><span class="detail-label">First Set</span><span class="detail-value" id="c_set">-</span></div>
                </div>
                
                <div class="description-box" id="c_desc">Descrição</div>
                
                <div class="lua-box">
                    <div class="lua-header">
                        <span>📜 Script LUA Original</span>
                        <span id="c_lua_status" style="color: var(--cyan);">Buscando...</span>
                    </div>
                    <textarea id="c_lua_code" readonly></textarea>
                </div>
            </div>
        </div>
    </div>

    <script>
        let currentIndex = 0;
        let totalCards = 0;
        let currentImages = [];
        let currentImageIndex = 0;

        function selectPath(type, inputId) {
            const route = type === 'file' ? '/select_file' : '/select_folder';
            fetch(route).then(r => r.json()).then(d => {
                if(d.path) document.getElementById(inputId).value = d.path;
            });
        }

        function loadDB() {
            const data = {
                json_path: document.getElementById('json_path').value,
                img_path: document.getElementById('img_path').value,
                lua_path: document.getElementById('lua_path').value
            };
            fetch('/load', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(data) })
            .then(r => r.json()).then(d => {
                if (d.error) alert(d.error);
                else {
                    totalCards = d.count;
                    isConfigVisible = false;
                    document.getElementById('config_bar').style.display = 'none';
                    document.getElementById('btn_toggle_config').style.display = 'block';
                    document.getElementById('btn_toggle_config').innerText = '▼ Mostrar Config';
                    
                    document.getElementById('viewer_section').style.display = 'flex';
                    loadCard(0);
                }
            });
        }

        function loadCard(index) {
            if (totalCards === 0) return;
            if (index < 0) index = totalCards - 1;
            if (index >= totalCards) index = 0;
            currentIndex = index;
            
            document.getElementById('card_counter').innerText = `${currentIndex + 1} / ${totalCards}`;
            
            fetch(`/api/card/${index}`).then(r => r.json()).then(card => {
                document.getElementById('c_name').innerText = card.name || 'Unknown';
                document.getElementById('c_id').innerText = card.id || '-';
                document.getElementById('c_pass').innerText = card.password || '-';
                document.getElementById('c_type').innerText = card.type || '-';
                document.getElementById('c_typeline').innerText = card.typeline || '-';
                document.getElementById('c_attr').innerText = card.attribute || '-';
                document.getElementById('c_race').innerText = card.race || card.property || '-';
                document.getElementById('c_level').innerText = card.level || card.linkval || '-';
                document.getElementById('c_stats').innerText = card.atk !== undefined ? `${card.atk} / ${card.def !== undefined ? card.def : '-'}` : '-';
                document.getElementById('c_arch').innerText = card.archetype || '-';
                document.getElementById('c_goat').innerText = card.goat_banlist || '-';
                document.getElementById('c_pool').innerText = card.pool || '-';
                document.getElementById('c_set').innerText = card.first_set || '-';
                document.getElementById('c_desc').innerText = card.description || 'No description.';
                
                // Fetch Images
                fetch(`/api/images/${card.id}`).then(r => r.json()).then(data => {
                    currentImages = data.images;
                    currentImageIndex = 0;
                    updateImageDisplay();
                });
                
                // Fetch LUA
                document.getElementById('c_lua_code').value = "Carregando...";
                fetch(`/api/lua/${card.id}?type=${encodeURIComponent(card.type || '')}&desc=${encodeURIComponent(card.description || '')}`)
                .then(r => r.json()).then(data => {
                    const statusEl = document.getElementById('c_lua_status');
                    if (data.status === "ok") {
                        statusEl.innerText = `[ ${data.filename} ] Ativo`;
                        statusEl.style.color = "var(--green)";
                        document.getElementById('c_lua_code').value = data.content;
                    } else if (data.status === "not_required") {
                        statusEl.innerText = "Normal/Sem Efeito: LUA não exigido";
                        statusEl.style.color = "#888";
                        document.getElementById('c_lua_code').value = "-- Esta carta não possui efeitos complexos que exijam script.";
                    } else {
                        statusEl.innerText = "❌ FALTA SCRIPT LUA!";
                        statusEl.style.color = "var(--red)";
                        document.getElementById('c_lua_code').value = "ERRO: O script desta carta não foi encontrado na pasta selecionada!";
                    }
                });
            });
        }
        
        function updateImageDisplay() {
            const imgEl = document.getElementById('card_image');
            const hintEl = document.getElementById('alt_hint');
            if (currentImages.length > 0) {
                imgEl.src = `/api/image_raw?path=${encodeURIComponent(currentImages[currentImageIndex])}`;
                hintEl.style.display = currentImages.length > 1 ? 'block' : 'none';
                if (currentImages.length > 1) hintEl.innerText = `ALT ART (${currentImageIndex + 1}/${currentImages.length}) CLICK`;
            } else {
                imgEl.src = "";
                hintEl.style.display = 'none';
            }
        }
        
        function cycleImage() {
            if (currentImages.length > 1) {
                currentImageIndex = (currentImageIndex + 1) % currentImages.length;
                updateImageDisplay();
            }
        }

        function nextCard() { loadCard(currentIndex + 1); }
        function prevCard() { loadCard(currentIndex - 1); }
        
        function handleSearch(e) {
            if (e.key === 'Enter') {
                const q = document.getElementById('search_input').value;
                if (!q) return;
                fetch(`/api/search?q=${encodeURIComponent(q)}`).then(r => r.json()).then(d => {
                    if(d.index !== undefined) loadCard(d.index);
                    else alert("Carta não encontrada.");
                });
            }
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
        
        document.addEventListener('keydown', (e) => {
            if (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA') return;
            if (e.key === 'ArrowRight') nextCard();
            if (e.key === 'ArrowLeft') prevCard();
        });
    </script>
</body>
</html>
"""

@app.route('/')
def home():
    return render_template_string(HTML_UI)

@app.route('/select_file')
def select_file():
    root = tk.Tk()
    root.attributes("-topmost", True)
    root.withdraw()
    file_path = filedialog.askopenfilename(title="Selecione cards.json", filetypes=[("JSON Files", "*.json")])
    root.destroy()
    return jsonify({"path": file_path})

@app.route('/select_folder')
def select_folder():
    root = tk.Tk()
    root.attributes("-topmost", True)
    root.withdraw()
    folder = filedialog.askdirectory(title="Selecione a pasta")
    root.destroy()
    return jsonify({"path": folder})

@app.route('/load', methods=['POST'])
def load():
    global global_db, global_img_dir, global_lua_dir, image_map
    data = request.json
    json_path = data.get('json_path', '')
    global_img_dir = data.get('img_path', '')
    global_lua_dir = data.get('lua_path', '')
    
    if not os.path.exists(json_path):
        return jsonify({"error": "Arquivo JSON não encontrado!"})
        
    try:
        with open(json_path, 'r', encoding='utf-8') as f:
            global_db = json.load(f)
            
        # Varredura e Mapeamento Inteligente de Artes Alternativas
        image_map = {}
        if os.path.exists(global_img_dir):
            for root, _, files in os.walk(global_img_dir):
                for f in files:
                    if f.endswith('.jpg') or f.endswith('.png'):
                        parts = f.split(' - ')
                        if len(parts) >= 1:
                            cid = parts[0].split('_')[0] # Limpa perfeitamente sufixos de Alt Art
                            if cid not in image_map: image_map[cid] = []
                            image_map[cid].append(os.path.join(root, f))
            for cid in image_map:
                image_map[cid].sort(key=lambda x: 1 if "_Alt" in x else 0)
                
        return jsonify({"count": len(global_db)})
    except Exception as e:
        return jsonify({"error": str(e)})

@app.route('/api/card/<int:index>')
def get_card(index):
    if 0 <= index < len(global_db):
        return jsonify(global_db[index])
    return jsonify({"error": "Index out of bounds"}), 404

@app.route('/api/search')
def search_card():
    query = request.args.get('q', '').lower()
    for i, c in enumerate(global_db):
        if query == str(c.get('id', '')).lower() or query in c.get('name', '').lower() or query == str(c.get('password', '')):
            return jsonify({"index": i})
    return jsonify({"error": "Not found"}), 404

@app.route('/api/images/<card_id>')
def get_images(card_id):
    if card_id in image_map and len(image_map[card_id]) > 0:
        return jsonify({"images": image_map[card_id]})
    return jsonify({"images": []})

@app.route('/api/image_raw')
def image_raw():
    path = request.args.get('path')
    if path and os.path.exists(path):
        return send_file(path)
    return "Not found", 404

@app.route('/api/lua/<card_id>')
def get_lua(card_id):
    card_type = request.args.get('type', '')
    desc = request.args.get('desc', '')
    
    # Filtro inteligente para Monstros Normais
    is_normal = "Normal" in card_type or "Token" in card_type
    has_pendulum = "[ Pendulum Effect ]" in desc
    if is_normal and not has_pendulum:
        return jsonify({"status": "not_required"})
        
    import re
    num_match = re.search(r'\d', str(card_id))
    num_str = num_match.group() if num_match else card_id
    
    possible_names = [f"c{card_id}.lua", f"{card_id}.lua", f"c{num_str}.lua", f"{num_str}.lua"]
    
    for name in possible_names:
        p = os.path.join(global_lua_dir, name)
        if os.path.exists(p):
            with open(p, 'r', encoding='utf-8', errors='ignore') as f:
                return jsonify({"status": "ok", "content": f.read(), "filename": name})
                
    return jsonify({"status": "missing"})

if __name__ == "__main__":
    app.run(debug=True, port=5001)
