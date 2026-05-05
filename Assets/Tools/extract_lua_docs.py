import os
import re
import tkinter as tk
from tkinter import filedialog, messagebox

class ExtractorGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("OCGCore LUA Extractor V3")
        self.root.geometry("480x420")
        
        # Variables
        self.var_funcs = tk.BooleanVar(value=True)
        self.var_arrays = tk.BooleanVar(value=False)
        self.var_args_only = tk.BooleanVar(value=False)
        self.var_args_blanks = tk.BooleanVar(value=True)
        self.var_list_style = tk.StringVar(value="checkbox")
        
        # UI
        tk.Label(root, text="⚙️ O que extrair?", font=("Arial", 11, "bold")).pack(anchor="w", padx=10, pady=(10, 0))
        tk.Checkbutton(root, text="Funções (local / global)", variable=self.var_funcs).pack(anchor="w", padx=20)
        tk.Checkbutton(root, text="Arrays / Tabelas", variable=self.var_arrays).pack(anchor="w", padx=20)
        tk.Checkbutton(root, text="EXTRAIR APENAS ARGUMENTOS (Lista Única/Global limpa)", variable=self.var_args_only).pack(anchor="w", padx=20, pady=(5,0))
        
        tk.Label(root, text="📝 Estilo da Lista:", font=("Arial", 11, "bold")).pack(anchor="w", padx=10, pady=(15, 0))
        tk.Radiobutton(root, text="Checkboxes (- [ ])", variable=self.var_list_style, value="checkbox").pack(anchor="w", padx=20)
        tk.Radiobutton(root, text="Numerada (1., 2.)", variable=self.var_list_style, value="numbered").pack(anchor="w", padx=20)
        tk.Radiobutton(root, text="Marcadores (-)", variable=self.var_list_style, value="bullet").pack(anchor="w", padx=20)
        
        tk.Label(root, text="✨ Opções de Formatação:", font=("Arial", 11, "bold")).pack(anchor="w", padx=10, pady=(15, 0))
        tk.Checkbutton(root, text="Incluir espaço vazio para descrever os argumentos", variable=self.var_args_blanks).pack(anchor="w", padx=20)
        
        tk.Button(root, text="Selecionar Pasta e Extrair", command=self.run_extraction, bg="#4CAF50", fg="white", font=("Arial", 10, "bold")).pack(fill="x", padx=20, pady=25)

    def extract_from_file(self, filepath):
        funcs = []
        arrays = []
        
        func_pattern = re.compile(r'(?:local\s+)?function\s+([a-zA-Z0-9_.:]+)\s*\((.*?)\)')
        array_pattern = re.compile(r'(?:local\s+)?([a-zA-Z0-9_.:]+)\s*=\s*\{')
        
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                f_match = func_pattern.search(line)
                if f_match:
                    funcs.append((f_match.group(1), f_match.group(2)))
                    continue
                
                a_match = array_pattern.search(line)
                if a_match:
                    arrays.append(a_match.group(1))
                    
        return funcs, arrays

    def get_prefix(self, idx):
        style = self.var_list_style.get()
        if style == "checkbox": return "- [ ] "
        elif style == "numbered": return f"{idx}. "
        else: return "- "

    def run_extraction(self):
        input_folder = filedialog.askdirectory(title="Selecione a pasta SupportLua")
        if not input_folder: return
        
        out_lines = ["# 📖 Dicionário OCGCore Extracted\n\n"]
        all_unique_args = set()
        
        for filename in os.listdir(input_folder):
            if not filename.endswith(".lua"): continue
            
            filepath = os.path.join(input_folder, filename)
            funcs, arrays = self.extract_from_file(filepath)
            
            # Modo Argumentos Globais Únicos
            if self.var_args_only.get():
                for _, params in funcs:
                    if params.strip():
                        for p in params.split(','):
                            clean_p = p.strip()
                            if clean_p and clean_p != "...":
                                all_unique_args.add(clean_p)
                continue
            
            if not funcs and not arrays: continue
            
            out_lines.append(f"## 📄 `{filename}`\n\n")
            
            if self.var_arrays.get() and arrays:
                out_lines.append("### 🗃️ Arrays / Tabelas\n")
                for idx, arr in enumerate(arrays, 1): out_lines.append(f"{self.get_prefix(idx)}`{arr}`\n")
                out_lines.append("\n")
                
            if self.var_funcs.get() and funcs:
                out_lines.append("### ⚙️ Funções\n")
                for idx, (fname, params) in enumerate(funcs, 1):
                    param_list = [p.strip() for p in params.split(',')] if params.strip() else []
                    
                    if self.var_args_blanks.get() and param_list:
                        out_lines.append(f"{self.get_prefix(idx)}**`{fname}`**\n")
                        for p in param_list: out_lines.append(f"  - `{p}` : *[Descrição]*\n")
                    else:
                        p_str = ", ".join(param_list) if param_list else "(Sem parâmetros)"
                        out_lines.append(f"{self.get_prefix(idx)}**`{fname}`** (`{p_str}`)\n")
                out_lines.append("\n")
                
            out_lines.append("---\n")
            
        if self.var_args_only.get():
            out_lines.append("## 🧠 Argumentos e Filtros Únicos (Global)\n\n")
            for idx, arg in enumerate(sorted(list(all_unique_args)), 1):
                desc = " : *[Adicione a descrição aqui]*" if self.var_args_blanks.get() else ""
                out_lines.append(f"{self.get_prefix(idx)}`{arg}`{desc}\n")

        output_file = os.path.join(input_folder, "LUA_Extracted_Data.md")
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write("".join(out_lines))
            
        messagebox.showinfo("Sucesso!", f"Extração concluída!\nSalvo em:\n{output_file}")
        self.root.quit()

def main():
    root = tk.Tk()
    app = ExtractorGUI(root)
    root.mainloop()

if __name__ == "__main__":
    main()
