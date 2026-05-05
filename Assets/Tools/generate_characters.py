from flask import Flask, render_template_string, request, jsonify
import json
import os
import tkinter as tk
from tkinter import filedialog

app = Flask(__name__)

HTML_UI = """
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <title>Character & Roster Studio</title>
    <style>
        :root {
            --cyan: #00e5ff;
            --purple: #b537f2;
            --gold: #d4af37;
            --green: #39ff14;
            --red: #ff073a;
            --yellow: #ffee00;
            --bg-dark: #080808;
            --panel-bg: #111111;
        }
        body { 
            background: var(--bg-dark); color: #fff; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex; justify-content: center; min-height: 100vh; margin: 0; padding: 20px;
        }
        .panel-container { 
            background: var(--panel-bg); border: 2px solid var(--purple); padding: 30px; 
            border-radius: 20px; width: 100%; max-width: 1400px; box-shadow: 0 0 40px rgba(181, 55, 242, 0.15);
            display: flex; flex-direction: column;
        }
        .neon-title { 
            text-align: center; text-transform: uppercase; letter-spacing: 5px; margin-top: 0; margin-bottom: 20px;
            color: var(--purple); text-shadow: 0 0 10px var(--purple), 0 0 20px rgba(181, 55, 242, 0.5);
        }
        
        .top-bar { display: flex; gap: 15px; margin-bottom: 20px; background: #050505; padding: 15px; border-radius: 12px; border: 1px solid #333; }
        
        input[type="text"], select { 
            padding: 8px 12px; background: #1a1a1a; border: 1px solid #444; 
            color: #fff; border-radius: 6px; font-family: inherit; width: 100%; box-sizing: border-box;
        }
        input[type="text"]:focus, select:focus { outline: none; border-color: var(--cyan); box-shadow: 0 0 8px rgba(0, 229, 255, 0.3); }
        
        .action-btn {
            background: transparent; font-weight: bold; text-transform: uppercase; letter-spacing: 1px;
            border-radius: 8px; padding: 10px 20px; cursor: pointer; transition: all 0.3s ease;
            border: 2px solid; white-space: nowrap;
        }
        .action-btn:hover { transform: translateY(-2px); color: #000 !important; text-shadow: none !important;}
        
        .cyan-btn { color: var(--cyan); border-color: var(--cyan); box-shadow: inset 0 0 8px rgba(0,229,255,0.3); text-shadow: 0 0 5px var(--cyan); }
        .cyan-btn:hover { background: var(--cyan); box-shadow: inset 0 0 20px var(--cyan); }
        
        .yellow-btn { color: var(--yellow); border-color: var(--yellow); box-shadow: inset 0 0 8px rgba(255,238,0,0.3); text-shadow: 0 0 5px var(--yellow); }
        .yellow-btn:hover { background: var(--yellow); box-shadow: inset 0 0 20px var(--yellow); }
        
        .gold-btn { color: var(--gold); border-color: var(--gold); box-shadow: inset 0 0 8px rgba(212,175,55,0.3); text-shadow: 0 0 5px var(--gold); }
        .gold-btn:hover { background: var(--gold); box-shadow: inset 0 0 20px var(--gold); }
        
        .red-btn { color: var(--red); border-color: var(--red); box-shadow: inset 0 0 8px rgba(255,7,58,0.3); text-shadow: 0 0 5px var(--red); }
        .red-btn:hover { background: var(--red); box-shadow: inset 0 0 20px var(--red); }

        .green-btn { color: var(--green); border-color: var(--green); box-shadow: inset 0 0 8px rgba(57,255,20,0.3); text-shadow: 0 0 5px var(--green); }
        .green-btn:hover { background: var(--green); box-shadow: inset 0 0 20px var(--green); }

        .chapter-box { background: #050505; border: 1px solid #333; border-radius: 12px; padding: 20px; margin-bottom: 20px; }
        .chapter-header { display: flex; align-items: center; gap: 15px; margin-bottom: 15px; border-bottom: 1px solid #222; padding-bottom: 10px;}
        .chapter-title-input { font-size: 1.2em; font-weight: bold; color: var(--gold); background: transparent; border: none; border-bottom: 1px dashed #555; width: 300px; padding: 5px; }
        .chapter-title-input:focus { outline: none; border-color: var(--cyan); }

        .char-grid-header { display: grid; grid-template-columns: 40px 100px 1fr 150px 120px 120px 1fr 50px; gap: 10px; color: #888; font-size: 0.8em; font-weight: bold; text-transform: uppercase; margin-bottom: 10px; padding: 0 10px;}
        .char-row { display: grid; grid-template-columns: 40px 100px 1fr 150px 120px 120px 1fr 50px; gap: 10px; align-items: center; background: #111; padding: 10px; border-radius: 8px; margin-bottom: 5px; border: 1px solid #222; transition: border-color 0.2s;}
        .char-row:hover { border-color: var(--cyan); }
        .char-index { text-align: center; color: #555; font-weight: bold; }
        
        .delete-btn { background: transparent; border: none; color: #666; cursor: pointer; font-size: 1.2em; transition: color 0.2s; }
        .delete-btn:hover { color: var(--red); }

        #notification { text-align: center; font-style: italic; color: #aaa; margin-top: 15px; min-height: 20px;}
    </style>
</head>
<body>
    <div class="panel-container">
        <h1 class="neon-title">Character & Roster Studio</h1>
        
        <div class="top-bar">
            <button class="action-btn yellow-btn" onclick="importList()">📥 Importar Lista (TXT/MD)</button>
            <button class="action-btn cyan-btn" onclick="scanAvatars()">🖼️ Mapear Pasta de Avatares</button>
            <div style="flex-grow: 1;"></div>
            <select id="era_prefix" style="width: 140px; flex: 0 0 auto;">
                <option value="DM">Era DM</option>
                <option value="GX">Era GX</option>
                <option value="5D">Era 5D's</option>
                <option value="ZX">Era ZEXAL</option>
                <option value="AV">Era ARC-V</option>
                <option value="VR">Era VRAINS</option>
                <option value="">Geral/Custom</option>
            </select>
            <button class="action-btn green-btn" onclick="exportData()">💾 Exportar</button>
        </div>

        <div id="chapters_container"></div>

        <button class="action-btn gold-btn" style="align-self: center; margin-top: 10px;" onclick="addChapter()">➕ Adicionar Novo Capítulo</button>
        
        <div id="notification"></div>

        <!-- Datalists for Suggestions -->
        <datalist id="roleList">
            <option value="Treinamento"></option>
            <option value="Torneio Local"></option>
            <option value="Torneio Principal"></option>
            <option value="Guardião"></option>
            <option value="Big Five"></option>
            <option value="Desafio"></option>
            <option value="Vilão"></option>
            <option value="Servo"></option>
            <option value="High Mage"></option>
            <option value="Boss Final"></option>
            <option value="Extra"></option>
            <option value="Amigo"></option>
            <option value="Colega"></option>
            <option value="Rival"></option>
            <option value="Mentor"></option>
            <option value="Bandido"></option>
            <option value="Herói"></option>
            <option value="Espectador"></option>
        </datalist>
        <datalist id="diffList">
            <option value="Very Easy"></option>
            <option value="Easy"></option>
            <option value="Medium"></option>
            <option value="Hard"></option>
            <option value="Very Hard"></option>
            <option value="Extreme"></option>
        </datalist>
        <datalist id="fieldList">
            <option value="Normal"></option>
            <option value="Yami"></option>
            <option value="Forest"></option>
            <option value="Wasteland"></option>
            <option value="Mountain"></option>
            <option value="Sogen"></option>
            <option value="Umi"></option>
            <option value="Toon World"></option>
            <option value="A Legendary Ocean"></option>
            <option value="The Sanctuary in the Sky"></option>
            <option value="Necrovalley"></option>
            <option value="Skyscraper"></option>
            <option value="Jurassic World"></option>
            <option value="Zombie World"></option>
            <option value="Neo Space"></option>
            <option value="Geartown"></option>
            <option value="The Seal of Orichalcos"></option>
            <option value="Dark Sanctuary"></option>
            <option value="Luminous Spark"></option>
            <option value="Mystic Plasma Zone"></option>
            <option value="Molten Destruction"></option>
            <option value="Umiiruka"></option>
            <option value="Gaia Power"></option>
            <option value="Rising Air Current"></option>
        </datalist>
    </div>

    <script>
        let state = {
            chapters: [
                {
                    name: "Capítulo 1: O Início",
                    chars: Array.from({length: 10}, (_, i) => createEmptyChar(i + 1))
                }
            ],
            avatars: [],
            globalIndexCounter: 10
        };

        function createEmptyChar(idx, name = "") {
            const idStr = String(idx).padStart(3, '0') + "_" + (name ? name.toLowerCase().replace(/[^a-z0-9]/g, '') : "char");
            return { id: idStr, name: name, role: "Duelista", difficulty: "Medium", field: "Normal", avatar: "" };
        }

        function render() {
            const container = document.getElementById('chapters_container');
            container.innerHTML = '';
            
            let globalListIndex = 1;
            
            state.chapters.forEach((chap, cIdx) => {
                let charRows = '';
                chap.chars.forEach((c, charIdx) => {
                    let avatarOptions = '<option value="">Sem Avatar</option>';
                    state.avatars.forEach(av => {
                        const selected = c.avatar === av ? 'selected' : '';
                        avatarOptions += `<option value="${av}" ${selected}>${av}</option>`;
                    });

                    charRows += `
                        <div class="char-row">
                            <div class="char-index">${globalListIndex++}</div>
                            <input type="text" id="id_${cIdx}_${charIdx}" value="${c.id}" onchange="updateChar(${cIdx}, ${charIdx}, 'id', this.value)" placeholder="ID">
                            <input type="text" value="${c.name}" oninput="updateName(${cIdx}, ${charIdx}, this.value)" placeholder="Nome">
                            <input type="text" list="roleList" value="${c.role}" onfocus="this.dataset.oldValue=this.value; this.value='';" onblur="if(this.value==='') this.value=this.dataset.oldValue; updateChar(${cIdx}, ${charIdx}, 'role', this.value)" placeholder="Papel" autocomplete="off">
                            <input type="text" list="diffList" value="${c.difficulty}" onfocus="this.dataset.oldValue=this.value; this.value='';" onblur="if(this.value==='') this.value=this.dataset.oldValue; updateChar(${cIdx}, ${charIdx}, 'difficulty', this.value)" placeholder="Dificuldade" autocomplete="off">
                            <input type="text" list="fieldList" value="${c.field}" onfocus="this.dataset.oldValue=this.value; this.value='';" onblur="if(this.value==='') this.value=this.dataset.oldValue; updateChar(${cIdx}, ${charIdx}, 'field', this.value)" placeholder="Arena" autocomplete="off">
                            <select onchange="updateChar(${cIdx}, ${charIdx}, 'avatar', this.value)">
                                ${avatarOptions}
                            </select>
                            <button class="delete-btn" onclick="removeChar(${cIdx}, ${charIdx})" title="Remover Personagem">🗑️</button>
                        </div>
                    `;
                });

                container.innerHTML += `
                    <div class="chapter-box">
                        <div class="chapter-header">
                            <input type="text" class="chapter-title-input" value="${chap.name}" onchange="updateChapterName(${cIdx}, this.value)">
                            <button class="action-btn red-btn" style="padding: 6px 12px; font-size: 0.8em;" onclick="removeChapter(${cIdx})">Remover Ato</button>
                        </div>
                        <div class="char-grid-header">
                            <div>Nº</div><div>ID (Único)</div><div>Nome</div><div>Role / Papel</div><div>Dificuldade</div><div>Arena</div><div>Avatar Associado</div><div>Ação</div>
                        </div>
                        ${charRows}
                        <button class="action-btn cyan-btn" style="margin-top: 10px; padding: 6px 15px;" onclick="addChar(${cIdx})">+ Novo Personagem</button>
                    </div>
                `;
            });
        }

        function updateChar(cIdx, charIdx, key, val) { state.chapters[cIdx].chars[charIdx][key] = val; }
        
        function updateName(cIdx, charIdx, val) {
            let c = state.chapters[cIdx].chars[charIdx];
            c.name = val;
            let numMatch = c.id.match(/^(\d+)_/);
            let numStr = numMatch ? numMatch[1] : "000";
            c.id = numStr + "_" + (val ? val.toLowerCase().replace(/[^a-z0-9]/g, '') : "char");
            document.getElementById(`id_${cIdx}_${charIdx}`).value = c.id;
        }
        
        function updateChapterName(cIdx, val) { state.chapters[cIdx].name = val; }
        
        function addChar(cIdx) {
            state.globalIndexCounter++;
            state.chapters[cIdx].chars.push(createEmptyChar(state.globalIndexCounter));
            render();
        }
        
        function removeChar(cIdx, charIdx) {
            state.chapters[cIdx].chars.splice(charIdx, 1);
            render();
        }

        function addChapter() {
            state.chapters.push({ name: `Ato ${state.chapters.length + 1}`, chars: [] });
            render();
        }
        
        function removeChapter(cIdx) {
            if(confirm("Tem certeza que deseja apagar este capítulo inteiro?")) {
                state.chapters.splice(cIdx, 1);
                render();
            }
        }

        function notify(msg, isError=false) {
            const n = document.getElementById('notification');
            n.innerText = msg;
            n.style.color = isError ? "var(--red)" : "var(--cyan)";
        }

        function importList() {
            fetch('/import_txt').then(r => r.json()).then(d => {
                if(d.names && d.names.length > 0) {
                    const newChap = { name: "Ato Importado", chars: [] };
                    d.names.forEach(name => {
                        state.globalIndexCounter++;
                        newChap.chars.push(createEmptyChar(state.globalIndexCounter, name));
                    });
                    state.chapters.push(newChap);
                    render();
                    notify(`Importados ${d.names.length} personagens com sucesso.`);
                } else if(d.error) notify(d.error, true);
            });
        }

        function scanAvatars() {
            fetch('/scan_avatars').then(r => r.json()).then(d => {
                if(d.files) {
                    state.avatars = d.files;
                    // Auto-Assign Magic
                    state.chapters.forEach(chap => {
                        chap.chars.forEach(c => {
                            if (!c.avatar) {
                                const match = state.avatars.find(av => av.includes(c.id));
                                if(match) c.avatar = match;
                            }
                        });
                    });
                    render();
                    notify(`Mapeadas ${d.files.length} imagens. Avatares com IDs correspondentes foram associados automaticamente!`);
                } else if(d.error) notify(d.error, true);
            });
        }

        function exportData() {
            state.era_prefix = document.getElementById('era_prefix').value;
            fetch('/export_json', { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify(state) })
            .then(r => r.json()).then(d => {
                if(d.ok) {
                    notify(`SUCESSO! Arquivo salvo de forma segura em: ${d.filepath}`);
                    alert(`Personagens Exportados!\nSalvo em:\n${d.filepath}`);
                } else notify("Erro ao exportar: " + d.error, true);
            });
        }

        window.onload = render;
    </script>
</body>
</html>
"""

@app.route('/')
def home(): return render_template_string(HTML_UI)

@app.route('/import_txt')
def import_txt():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        path = filedialog.askopenfilename(title="Selecione um TXT ou MD com a lista de nomes", filetypes=[("Text Files", "*.txt;*.md"), ("All", "*.*")])
        root.destroy()
        
        if not path: return jsonify({"names": []})
        
        names = []
        with open(path, 'r', encoding='utf-8') as f:
            for line in f:
                clean = line.strip()
                # Remove bullet points if copied from markdown
                if clean.startswith("- "): clean = clean[2:]
                elif clean.startswith("* "): clean = clean[2:]
                if clean: names.append(clean)
        return jsonify({"names": names})
    except Exception as e:
        return jsonify({"error": str(e)})

@app.route('/scan_avatars')
def scan_avatars():
    try:
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        folder = filedialog.askdirectory(title="Selecione a pasta de imagens dos Avatares")
        root.destroy()
        
        if not folder: return jsonify({"files": []})
        
        valid_exts = ['.png', '.jpg', '.jpeg']
        files = [f for f in os.listdir(folder) if any(f.lower().endswith(ext) for ext in valid_exts)]
        return jsonify({"files": sorted(files)})
    except Exception as e:
        return jsonify({"error": str(e)})

@app.route('/export_json', methods=['POST'])
def export_json():
    try:
        state = request.json
        
        root = tk.Tk()
        root.attributes("-topmost", True)
        root.withdraw()
        save_dir = filedialog.askdirectory(title="Escolha ONDE salvar o characters.json (Ex: StreamingAssets)")
        root.destroy()
        
        if not save_dir: return jsonify({"ok": False, "error": "Operação cancelada pelo usuário."})

        # Proteção de Sobrescrita (Safe Save)
        era_prefix = state.get('era_prefix', '')
        base_name = f"characters{era_prefix}" if era_prefix else "characters"        
        ext = ".json"
        final_path = os.path.join(save_dir, base_name + ext)
        counter = 1
        while os.path.exists(final_path):
            final_path = os.path.join(save_dir, f"{base_name}_{counter}{ext}")
            counter += 1
            
        final_characters = []
        for chap in state.get('chapters', []):
            for c in chap.get('chars', []):
                char_entry = {
                    "id": c["id"],
                    "name": c["name"],
                    "deck_A": [],
                    "extra_deck_A": [],
                    "deck_B": [],
                    "extra_deck_B": [],
                    "deck_C": [],
                    "extra_deck_C": [],
                    "unique_drops": [],
                    "rewards": {"s_plus": "", "s": [], "b": [], "c": [], "d": []},
                    "field": c["field"],
                    "difficulty": c["difficulty"],
                    "story_role": c["role"]
                }
                # Adicionamos o avatar caso exista
                if c.get("avatar"):
                    char_entry["avatar_path"] = c["avatar"]
                    
                final_characters.append(char_entry)
                
        with open(final_path, 'w', encoding='utf-8') as f:
            json.dump(final_characters, f, indent=2)
            
        return jsonify({"ok": True, "filepath": final_path})
    except Exception as e:
        return jsonify({"ok": False, "error": str(e)})

if __name__ == "__main__":
    # Usa a porta 5003 para não colidir
    app.run(debug=True, port=5003)
