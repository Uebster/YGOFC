import os
import sys
import re
import json
import csv
import tkinter as tk
from tkinter import filedialog
from pathlib import Path
from collections import defaultdict

class LuaSymbolTable:
    """Stores and provides lookup for Lua functions and constants defined in C# and core Lua files."""
    def __init__(self):
        self.constants = set()
        self.functions = set()
        self.lua_api_methods = set() # Methods exposed from C# LuaAPI.cs

    def load_constants_from_file(self, file_path):
        """Parses a Lua file (like constant.lua) for global constant definitions."""
        if not file_path.exists():
            print(f"Warning: Constant file not found: {file_path}")
            return
        content = file_path.read_text(encoding='utf-8')
        # Match standard YGOPro global constants like: SET_ELEMENTAL_HERO = 0x3008
        # Adicionado \s* para aceitar Tabulações e Espaços antes da constante
        matches_ygopro = re.findall(r'^\s*([A-Z_0-9]+)\s*=', content, re.MULTILINE)
        self.constants.update(matches_ygopro)
        print(f"Loaded {len(matches_ygopro)} constants from {file_path.name}")

    def load_functions_from_file(self, file_path, prefix=""):
        """Parses a Lua file (like utility.lua) for function definitions."""
        if not file_path.exists():
            print(f"Warning: Function file not found: {file_path}")
            return
        content = file_path.read_text(encoding='utf-8')
        # Regex to find function definitions like function aux.FunctionName(...)
        matches = re.findall(r'function\s+(?:' + re.escape(prefix) + r'\.)?(?P<name>[a-zA-Z_0-9]+)\s*\(', content)
        self.functions.update(matches)
        print(f"Loaded {len(matches)} functions from {file_path.name}")

    def load_lua_api_from_cs(self, cs_file_path):
        """Parses LuaAPI.cs to extract exposed public methods."""
        if not cs_file_path.exists():
            print(f"Warning: LuaAPI.cs not found: {cs_file_path}")
            return
        content = cs_file_path.read_text(encoding='utf-8')
        
        # Regex to find public methods in LuaDuel, LuaCard, LuaEffect, LuaGroup
        # This is a simplified approach, a full C# parser would be more robust.
        # Captura qualquer método público no C#, extraindo os tipos de retorno dinamicamente
        matches = re.findall(r'public\s+(?:static\s+)?[a-zA-Z_0-9<>\[\]]+\s+(?P<name>[a-zA-Z_0-9]+)\s*\(', content)
        self.lua_api_methods.update([m for m in matches if m != 'operator']) # Remove o operador de soma nativo
        print(f"Loaded {len(matches)} LuaAPI methods from {cs_file_path.name}")

    def load_injected_globals_from_cs(self, cs_file_path):
        """Parses CardEffectManager.cs to find constants injected into Lua."""
        if not cs_file_path.exists():
            print(f"Warning: CardEffectManager.cs not found: {cs_file_path}")
            return
        content = cs_file_path.read_text(encoding='utf-8')
        matches = re.findall(r'luaEngine\.Globals\["([A-Z_0-9]+)"\]', content)
        self.constants.update(matches)
        print(f"Loaded {len(matches)} injected globals from {cs_file_path.name}")

    def is_constant_defined(self, name):
        return name in self.constants

    def is_function_defined(self, name):
        return name in self.functions or name in self.lua_api_methods

class LuaScriptAnalyzer:
    """Analyzes Lua scripts for common errors without executing"""
    
    def __init__(self, scripts_dir, cards_json_path, lua_api_cs_path, card_effect_manager_cs_path, constant_lua_path, utility_lua_path):
        self.scripts_dir = Path(scripts_dir)
        self.cards_json_path = Path(cards_json_path)
        self.lua_api_cs_path = Path(lua_api_cs_path)
        self.card_effect_manager_cs_path = Path(card_effect_manager_cs_path)
        self.constant_lua_path = Path(constant_lua_path)
        self.utility_lua_path = Path(utility_lua_path)
        self.errors = []
        self.valid_scripts = []
        self.symbol_table = LuaSymbolTable()
        self.card_database = {}
        self.expected_lua_scripts = set()

        # Pre-compile regex patterns for efficiency
        self.re_nil_access = re.compile(r'(\w+)\s*[\.:]\s*(\w+)') # var.field or var:method
        self.re_function_call = re.compile(r'(?<!function\s)\b(?P<name>[a-zA-Z_0-9]+)\s*\(') # Captura absolutamente TODAS as chamadas de funções
        self.re_global_constant = re.compile(r'\b(?P<name>[A-Z_][A-Z_0-9]*[A-Z][A-Z_0-9]*)\b') # ALL_CAPS_CONSTANT (Exige pelo menos uma letra para ignorar números)
        self.re_operator_on_nil = re.compile(r'[+\-*/%]\s*nil|\s*nil\s*[+\-*/%]')
        self.re_table_iter = re.compile(r'for\s+\w+\s+in\s+(\w+)\s+do') # for tc in eg do

    def load_game_data(self):
        """Loads cards.json and identifies cards expected to have Lua scripts."""
        if not self.cards_json_path.exists():
            print(f"Error: cards.json not found: {self.cards_json_path}")
            return
        with open(self.cards_json_path, 'r', encoding='utf-8') as f:
            self.card_database = {c["id"]: c for c in json.load(f)}
        
        for card_id, card_data in self.card_database.items():
            # Heuristic to determine if a card should have a Lua script
            # Assumes cards with "Effect", "Fusion", "Ritual", "Synchro", "Xyz", "Link" in type
            # or "Trap" or "Spell" (non-Normal) should have scripts.
            card_type = card_data.get("type", "")
            card_property = card_data.get("property", "") # For Spells/Traps
            
            if "Effect" in card_type or "Fusion" in card_type or "Ritual" in card_type or \
               "Synchro" in card_type or "Xyz" in card_type or "Link" in card_type or \
               "Trap" in card_type or "Spell" in card_type:
                self.expected_lua_scripts.add(card_id)
        print(f"Loaded {len(self.card_database)} cards. Expecting {len(self.expected_lua_scripts)} Lua scripts.")

    def initialize_symbol_table(self):
        """Initializes the symbol table from core Lua files and C# API."""
        self.symbol_table.load_constants_from_file(self.constant_lua_path)
        self.symbol_table.load_functions_from_file(self.utility_lua_path, prefix="aux")
        self.symbol_table.load_lua_api_from_cs(self.lua_api_cs_path)
        self.symbol_table.load_injected_globals_from_cs(self.card_effect_manager_cs_path)
        
        # Add common Lua globals/built-ins that are always available
        self.symbol_table.functions.update([
            'Duel', 'Effect', 'Card', 'Group', 'Fusion', 'Synchro', 'Spirit', 'Ritual', 'Xyz', 'aux',
            'ipairs', 'pairs', 'type', 'tostring', 'tonumber', 'print', 'math', 'string', 'table', 'bit32'
        ])
        self.symbol_table.constants.update([
            'LOCATION_DECK', 'LOCATION_HAND', 'LOCATION_MZONE', 'LOCATION_SZONE', 'LOCATION_GRAVE',
            'TYPE_MONSTER', 'TYPE_SPELL', 'TYPE_TRAP', 'EFFECT_TYPE_SINGLE', 'EFFECT_TYPE_IGNITION',
            'TRUE', 'FALSE' # These are defined in C# as closures
        ])

    def analyze_all(self):
        """Analyze all scripts"""
        print("=" * 50)
        print("FASE 27: COMPLETE SCRIPT ANALYSIS")
        print("=" * 50)
        print()
        
        self.load_game_data()
        self.initialize_symbol_table()

        lua_files_on_disk = {p.stem[1:] for p in self.scripts_dir.glob("c*.lua") if p.stem.startswith('c')}
        
        # Check for missing/extraneous scripts
        missing_scripts = self.expected_lua_scripts - lua_files_on_disk
        extraneous_scripts = lua_files_on_disk - self.expected_lua_scripts

        for card_id in missing_scripts:
            card_name = self.card_database.get(card_id, {}).get("name", f"Unknown Card {card_id}")
            self.errors.append((card_id, 'MissingScript', f"Expected Lua script c{card_id}.lua not found for {card_name}"))
        
        for card_id in extraneous_scripts:
            card_name = self.card_database.get(card_id, {}).get("name", f"Unknown Card {card_id}")
            self.errors.append((card_id, 'ExtraneousScript', f"Lua script c{card_id}.lua found but not expected for {card_name}"))

        # Analyze existing Lua files
        total_lua_files = len(lua_files_on_disk)
        processed_count = 0
        
        for lua_file_stem in lua_files_on_disk:
            processed_count += 1
            card_id = lua_file_stem
            lua_file_path = self.scripts_dir / f"c{card_id}.lua"
            
            try:
                content = lua_file_path.read_text(encoding='utf-8')
                errors_in_script = self.check_script(card_id, content)
                
                if errors_in_script:
                    for error_type, error_msg in errors_in_script:
                        self.errors.append((card_id, error_type, error_msg))
                else:
                    self.valid_scripts.append(card_id)
                    
            except Exception as e:
                self.errors.append((card_id, 'FileReadError', f"Failed to read {lua_file_path.name}: {str(e)}"))
            
            # Progress
            if processed_count % 100 == 0:
                print(f"Progress: {processed_count}/{total_lua_files}")
        
        print()
        print("=" * 50)
        print("ANALYSIS COMPLETE")
        print("=" * 50)
        print(f"Total Lua files on disk: {total_lua_files}")
        print(f"Expected Lua scripts: {len(self.expected_lua_scripts)}")
        print(f"Valid Scripts (no errors detected): {len(self.valid_scripts)}")
        print(f"Scripts with errors/issues: {len(self.errors)}")
        
        # Calculate pass rate based on scripts that were actually analyzed for content
        analyzed_scripts_count = total_lua_files - len(extraneous_scripts)
        if analyzed_scripts_count > 0:
            pass_rate = (len(self.valid_scripts) * 100.0 / total_lua_files)
            print(f"Pass Rate (of expected scripts): {pass_rate:.1f}%")
        else:
            print("No expected scripts to analyze.")
        print()
        
        error_categories = defaultdict(list)
        for card_id, error_type, error_msg in self.errors:
            error_categories[error_type].append({'card_id': card_id, 'message': error_msg})

        # Print failures by category
        if error_categories:
            print("=" * 50)
            print("FAILURE BREAKDOWN BY CATEGORY")
            print("=" * 50)
            
            for error_type, items in sorted(error_categories.items(),
                                           key=lambda x: len(x[1]), reverse=True): # Sort by count
                print(f"\n[{error_type}] - {len(items)} scripts:")
                for item in items[:10]:  # Show first 10
                    card_name = self.card_database.get(item['card_id'], {}).get("name", f"Unknown Card {item['card_id']}")
                    print(f"  c{item['card_id']}.lua ({card_name}): {item['message']}")
                if len(items) > 10:
                    print(f"  ... and {len(items) - 10} more")
        
        return len(self.errors), error_categories
    
    def check_script(self, card_id, content):
        """Check single script for known runtime errors"""
        errors = []
        lines = content.splitlines()

        # Identifica funções locais ou da própria carta (ex: function c123.condition ou local function target)
        local_funcs = set(re.findall(r'function\s+(?:[sc]\d*\.)?(?P<name>[a-zA-Z_0-9]+)\s*\(', content))
        local_funcs.update(re.findall(r'(?P<name>[a-zA-Z_0-9]+)\s*=\s*function\s*\(', content))

        for i, line in enumerate(lines):
            # Limpeza crucial: Remove comentários e strings da linha antes de analisar!
            clean_line = re.sub(r'--.*$', '', line) # Remove comentários de linha
            clean_line = re.sub(r'".*?"', '""', clean_line) # Remove strings em aspas duplas
            clean_line = re.sub(r"'.*?'", "''", clean_line) # Remove strings em aspas simples
            
            if not clean_line.strip():
                continue
                
            # Check for arithmetic operations on nil
            if self.re_operator_on_nil.search(clean_line):
                errors.append(('OperatorError', f"Line {i+1}: Possible arithmetic operation on nil value"))
                
            # Check for undefined global constants (ALL_CAPS)
            for match in self.re_global_constant.finditer(clean_line):
                const_name = match.group('name')
                if const_name not in ['true', 'false', 'nil', 'self', 'Duel', 'Effect', 'Card', 'Group', 'Fusion', 'Synchro', 'Spirit', 'Ritual', 'Xyz', 'aux'] and \
                   not self.symbol_table.is_constant_defined(const_name) and \
                   not self.symbol_table.is_function_defined(const_name): # Sometimes function names are all caps
                    errors.append(('UndefinedConstant', f"Line {i+1}: Constant '{const_name}' might be undefined"))

            # Check for function calls that might be missing in LuaAPI or core Lua
            for match in self.re_function_call.finditer(clean_line):
                func_name = match.group('name')
                if func_name not in ['function', 'local', 'if', 'then', 'elseif', 'else', 'end', 'return', 'for', 'in', 'do', 'while', 'break', 'and', 'or', 'not',
                                     'self', 'Duel', 'Effect', 'Card', 'Group', 'Fusion', 'Synchro', 'Spirit', 'Ritual', 'Xyz', 'aux',
                                     'ipairs', 'pairs', 'type', 'tostring', 'tonumber', 'print', 'math', 'string', 'table', 'bit32',
                                     'Stringid', 'GlobalCheck', 'HintSelection', 'AddProcMix', 'NecroValleyFilter', 'NegateActivation'] and \
                   func_name not in local_funcs and \
                   not self.symbol_table.is_function_defined(func_name):
                    errors.append(('MissingMethod', f"Line {i+1}: Function '{func_name}' is called but not defined in LuaAPI or core scripts"))
        
        # Check for common nil access patterns (e.g., trying to access a field of a potentially nil object)
        # This is heuristic and can have false positives.
        # (Desativado temporariamente para evitar poluição visual com falsos positivos em variáveis globais do YGOPro)
        # for i, line in enumerate(lines):
        #     for match in self.re_nil_access.finditer(line):
        #         var_name = match.group(1)
        #         field_name = match.group(2)
        #         # Exclui variáveis oficiais injetadas
        #         if var_name not in ['Duel', 'Effect', 'Card', 'Group', 'c', 'e', 'tp', 'eg', 'ep', 'ev', 're', 'r', 'rp', 'chk', 'chkc']:
        #             if f"local {var_name}" not in content and f"function {var_name}" not in content and f"({var_name}" not in content:
        #                 errors.append(('NilAccessHeuristic', f"Line {i+1}: Variable '{var_name}' accessed via '{field_name}' might be nil"))

        # Remove duplicate errors for the same card
        unique_errors = []
        seen_errors = set()
        for err_type, err_msg in errors:
            if (err_type, err_msg) not in seen_errors:
                unique_errors.append((err_type, err_msg))
                seen_errors.add((err_type, err_msg))
        
        return unique_errors
    
    def generate_reports(self):
        """Interactive prompt to save reports in multiple formats."""
        report_data = {
            'total_scripts_on_disk': len(list(self.scripts_dir.glob("c*.lua"))),
            'expected_scripts_from_cards_json': len(self.expected_lua_scripts),
            'valid_scripts_analyzed': len(self.valid_scripts),
            'scripts_with_errors_or_issues': len(self.errors),
            'pass_rate_of_expected_scripts': f"{(len(self.valid_scripts)*100/(len(self.expected_lua_scripts) if len(self.expected_lua_scripts) > 0 else 1)):.1f}%",
            'failed_cards': [
                {
                    'card_id': err[0],
                    'error_type': err[1],
                    'message': err[2]
                }
                for err in sorted(self.errors, key=lambda x: x[0])
            ]
        }

        print("\n" + "=" * 50)
        print("OPÇÕES DE EXPORTAÇÃO DE RELATÓRIO")
        print("=" * 50)
        
        choice = input("Como deseja salvar o relatório? (json / csv / txt / all / none) [all]: ").strip().lower()
        if choice == '' or choice == 'all':
            formats = ['json', 'csv', 'txt']
        elif choice == 'none':
            formats = []
        else:
            formats = [f.strip() for f in choice.split('/') if f.strip() in ['json', 'csv', 'txt']]
            if not formats: formats = [choice]

        if not formats:
            print("Exportação ignorada.")
            return

        print("\n-> Pressione ENTER para salvar na pasta padrão (Analysis_Reports)")
        print("-> Ou digite 'dir' para abrir a janela do Windows e escolher outra pasta.")
        dir_choice = input("Onde deseja salvar? [padrão]: ").strip().lower()

        if dir_choice == 'dir':
            root = tk.Tk()
            root.withdraw()
            root.attributes('-topmost', True)
            selected_dir = filedialog.askdirectory(title="Selecione a Pasta para salvar os Relatórios")
            root.destroy()
            
            out_dir = Path(selected_dir) if selected_dir else Path(__file__).resolve().parent / "Analysis_Reports"
        else:
            out_dir = Path(__file__).resolve().parent / "Analysis_Reports"

        out_dir.mkdir(parents=True, exist_ok=True)
        print(f"\n[*] Salvando relatórios em: {out_dir}")

        if 'json' in formats:
            json_path = out_dir / 'Lua_Analysis_Report.json'
            with open(json_path, 'w', encoding='utf-8') as f:
                json.dump(report_data, f, indent=2, ensure_ascii=False)
            print(f"[*] Salvo: {json_path}")

        if 'csv' in formats:
            csv_path = out_dir / 'Lua_Analysis_Report.csv'
            with open(csv_path, 'w', newline='', encoding='utf-8') as f:
                writer = csv.writer(f)
                writer.writerow(['ID da Carta', 'Nome da Carta', 'Tipo de Erro', 'Detalhes'])
                for err in sorted(self.errors, key=lambda x: x[0]):
                    card_name = self.card_database.get(err[0], {}).get("name", "Desconhecida")
                    writer.writerow([err[0], card_name, err[1], err[2]])
            print(f"[*] Salvo: {csv_path}")

        if 'txt' in formats:
            txt_path = out_dir / 'Lua_Analysis_Report.txt'
            with open(txt_path, 'w', encoding='utf-8') as f:
                f.write("==================================================\n")
                f.write("   RELATÓRIO DE ANÁLISE DE SCRIPTS LUA (OCGCore)  \n")
                f.write("==================================================\n\n")
                f.write(f"Total de Scripts no Disco: {report_data['total_scripts_on_disk']}\n")
                f.write(f"Scripts Esperados (cards.json): {report_data['expected_scripts_from_cards_json']}\n")
                f.write(f"Scripts Válidos: {report_data['valid_scripts_analyzed']}\n")
                f.write(f"Scripts com Alertas/Erros: {report_data['scripts_with_errors_or_issues']}\n")
                f.write(f"Taxa de Sucesso: {report_data['pass_rate_of_expected_scripts']}\n\n")
                
                error_categories = defaultdict(list)
                for err in self.errors:
                    error_categories[err[1]].append(err)
                    
                for err_type, items in sorted(error_categories.items(), key=lambda x: len(x[1]), reverse=True):
                    f.write(f"--- [ {err_type.upper()} ] ({len(items)} ocorrências) ---\n")
                    for item in items:
                        card_name = self.card_database.get(item[0], {}).get("name", "Desconhecida")
                        f.write(f"  c{item[0]}.lua ({card_name}): {item[2]}\n")
                    f.write("\n")
            print(f"[*] Salvo: {txt_path}")

def get_path_with_fallback(default_path, description, is_dir=False, filetypes=None):
    """Tries to find the path, or opens a file explorer dialog if missing."""
    if default_path.exists():
        return default_path
    
    print(f"\n[!] Caminho não encontrado: {default_path}")
    print(f"-> Abrindo janela de seleção para: {description}")
    
    root = tk.Tk()
    root.withdraw()
    root.attributes('-topmost', True)
    
    if is_dir:
        selected = filedialog.askdirectory(title=f"Selecione a Pasta: {description}")
    else:
        if filetypes is None:
            filetypes = [("Todos os Arquivos", "*.*")]
        selected = filedialog.askopenfilename(title=f"Selecione o Arquivo: {description}", filetypes=filetypes)
        
    root.destroy()
    
    if not selected:
        print(f"Erro: '{description}' é obrigatório para a análise. Operação cancelada.")
        sys.exit(1)
        
    return Path(selected)

def main():
    # Define paths relative to the script's location
    base_dir = Path(__file__).resolve().parent.parent.parent # YuGiOh_Forbidden_Chaos/Assets
    
    scripts_dir = get_path_with_fallback(base_dir / "Scripts" / "LuaScripts", "Pasta LuaScripts", is_dir=True)
    cards_json_path = get_path_with_fallback(base_dir / "Scripts" / "Cards" / "cards.json", "Arquivo cards.json", filetypes=[("JSON", "*.json")])
    lua_api_cs_path = get_path_with_fallback(base_dir / "Scripts" / "LuaAPI.cs", "Arquivo LuaAPI.cs", filetypes=[("C#", "*.cs")])
    card_effect_manager_cs_path = get_path_with_fallback(base_dir / "Scripts" / "CardEffectManager.cs", "Arquivo CardEffectManager.cs", filetypes=[("C#", "*.cs")])
    constant_lua_path = get_path_with_fallback(scripts_dir / "constant.lua", "Arquivo constant.lua", filetypes=[("Lua", "*.lua")])
    utility_lua_path = get_path_with_fallback(scripts_dir / "utility.lua", "Arquivo utility.lua", filetypes=[("Lua", "*.lua")])
    
    
    # Run analyzer
    analyzer = LuaScriptAnalyzer(scripts_dir, cards_json_path, lua_api_cs_path, card_effect_manager_cs_path, constant_lua_path, utility_lua_path)
    failed, categories = analyzer.analyze_all()
    
    # Interactive export
    analyzer.generate_reports()
    
    print()
    print(f"Found {failed} problematic scripts out of {len(list(analyzer.scripts_dir.glob('c*.lua')))} total Lua files on disk.")
    
    return 0 if failed < 50 else 1


if __name__ == '__main__':
    sys.exit(main())
