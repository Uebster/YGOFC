#!/usr/bin/env python3
"""
FASE 27: Card Script Analyzer
Analyzes all 1,498 Lua scripts to identify the 40 with errors
"""

import os
import sys
import re
import json
from pathlib import Path
from collections import defaultdict

class LuaScriptAnalyzer:
    """Analyzes Lua scripts for common errors without executing"""
    
    def __init__(self, scripts_dir):
        self.scripts_dir = Path(scripts_dir)
        self.errors = []
        self.valid_scripts = []
        
    def analyze_all(self):
        """Analyze all scripts"""
        print("=" * 50)
        print("FASE 27: COMPLETE SCRIPT ANALYSIS")
        print("=" * 50)
        print()
        
        # Get all Lua files
        lua_files = sorted(self.scripts_dir.glob("cDM*.lua"))
        total = len(lua_files)
        
        print(f"Found {total} Lua scripts")
        print()
        
        error_categories = defaultdict(list)
        checked = 0
        
        for lua_file in lua_files:
            checked += 1
            card_id = int(lua_file.stem.replace("cDM", ""))
            
            try:
                content = lua_file.read_text(encoding='utf-8')
                errors = self.check_script(card_id, content)
                
                if errors:
                    for error_type, error_msg in errors:
                        error_categories[error_type].append({
                            'card_id': card_id,
                            'file': lua_file.name,
                            'message': error_msg
                        })
                        self.errors.append((card_id, error_type, error_msg))
                else:
                    self.valid_scripts.append(card_id)
                    
            except Exception as e:
                error_categories['FileError'].append({
                    'card_id': card_id,
                    'file': lua_file.name,
                    'message': str(e)
                })
                self.errors.append((card_id, 'FileError', str(e)))
            
            # Progress
            if checked % 100 == 0:
                print(f"Progress: {checked}/{total}")
        
        print()
        print("=" * 50)
        print("ANALYSIS COMPLETE")
        print("=" * 50)
        print(f"Total Checked: {total}")
        print(f"Valid Scripts: {len(self.valid_scripts)}")
        print(f"Failed Scripts: {len(self.errors)}")
        print(f"Pass Rate: {(len(self.valid_scripts)*100/total):.1f}%")
        print()
        
        # Print failures by category
        if error_categories:
            print("=" * 50)
            print("FAILURE BREAKDOWN BY CATEGORY")
            print("=" * 50)
            
            for error_type, items in sorted(error_categories.items(), 
                                           key=lambda x: len(x[1]), reverse=True):
                print(f"\n[{error_type}] - {len(items)} scripts:")
                for item in items[:10]:  # Show first 10
                    print(f"  cDM{item['card_id']:04d}: {item['message']}")
                if len(items) > 10:
                    print(f"  ... and {len(items) - 10} more")
        
        return len(self.errors), error_categories
    
    def check_script(self, card_id, content):
        """Check single script for known runtime errors"""
        errors = []
        
        # 1. Check for nil access patterns (returning without assignment)
        nil_patterns = [
            (r'local\s+\w+\s*=\s*\w+\.\w+\(\)', 'Possible nil return from method call'),
            (r'\.attribute\s*[=!~<>]+', 'Accessing undefined properties'),
            (r'if\s+\w+\.has_count\b', 'Using undefined method has_count'),
        ]
        for pattern, msg in nil_patterns:
            if re.search(pattern, content):
                errors.append(('NilIndexing', msg))
                break
        
        # 2. Check for undefined Fusion methods
        if 'Fusion.AddProcMix' in content:
            if 'function.*AddProcMix' not in content:
                errors.append(('MissingMethod', 'Fusion.AddProcMix() method does notexist'))
        
        # 3. Check for Counter setup issues
        if 'COUNTER' in content or 'counter' in content.lower():
            if 'SetCounterLimit' not in content and 'addCounter' not in content:
                pass  # Might be okay, could be reading counters
        
        # 4. Check for specific undefined constants/methods in Lua registry
        undefined_methods = [
            'IsCanBeSpecialSummoned',
            'IsEnvironment',
            'HasCount',
            'Spirit.AddProcedure',
            'IsCanSummon',
        ]
        
        for method in undefined_methods:
            if method in content and not re.search(rf'function.*{method}', content):
                # Check if it's a method that should exist in MoonSharp registry
                errors.append(('MissingMethod', f'{method}() is called but not found'))
        
        # 5. Check for set-specific constants that might be missing
        set_constants = [
            'CARD_AMAZONESS',
            'CARD_ARCHFIEND',
            'SET_RESETS_STANDARD_EXC_GRAVE',
            'RESETS_STANDARD_EXCEPT_GRAVE',
        ]
        
        for const in set_constants:
            if const in content:
                errors.append(('UndefinedConstant', f'Constant {const} may not be defined'))
                break
        
        # 6. Check for operator misuse
        if re.search(r'[+\-*/%]\s*nil', content):
            errors.append(('OperatorError', 'Arithmetic operation on nil'))
        
        # Remove duplicates
        error_dict = {}
        for err_type, msg in errors:
            if err_type not in error_dict:
                error_dict[err_type] = msg
        
        return [(k, v) for k, v in error_dict.items()]
    
    def has_nil_access_error(self, content):
        """Check for potential nil access patterns"""
        return False  # Skip this check for now
    
    def has_counter_setup(self, content):
        """Check if card counter is properly set up"""
        return 'SetCounterLimit' in content or 'addCounter' in content
    
    def check_undefined_constants(self, content):
        """Find potentially undefined constants"""
        return []  # Skip this check for now
    
    def save_report(self, filename='FASE27_SCRIPT_ANALYSIS.json'):
        """Save analysis report"""
        report = {
            'total_scripts': len(self.valid_scripts) + len(self.errors),
            'valid_scripts': len(self.valid_scripts),
            'failed_scripts': len(self.errors),
            'pass_rate': f"{(len(self.valid_scripts)*100/(len(self.valid_scripts)+len(self.errors))):.1f}%",
            'failed_cards': [
                {
                    'card_id': err[0],
                    'error_type': err[1],
                    'message': err[2]
                }
                for err in sorted(self.errors, key=lambda x: x[0])
            ]
        }
        
        with open(filename, 'w') as f:
            json.dump(report, f, indent=2)
        
        print(f"\nReport saved to: {filename}")
        return report


def main():
    # Get script directory
    script_dir = Path("Assets/Scripts/LuaScripts")
    
    if not script_dir.exists():
        print(f"Error: Directory not found: {script_dir}")
        sys.exit(1)
    
    # Run analyzer
    analyzer = LuaScriptAnalyzer(script_dir)
    failed, categories = analyzer.analyze_all()
    
    # Save report
    report = analyzer.save_report()
    
    print()
    print(f"Found {failed} problematic scripts out of {len(analyzer.valid_scripts) + len(analyzer.errors)} total")
    
    return 0 if failed < 50 else 1


if __name__ == '__main__':
    sys.exit(main())
