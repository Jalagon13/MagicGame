#!/usr/bin/env python3
"""
Script to wrap C# classes in ProjectWizard namespace
"""
import os
import re
from pathlib import Path

def has_namespace(content):
    """Check if file already has a namespace declaration"""
    return re.search(r'^\s*namespace\s+\w+', content, re.MULTILINE) is not None

def has_code_after_usings(content):
    """Check if there's actual code (classes, enums, etc.) after using statements"""
    # Remove comments
    content_no_comments = re.sub(r'//.*?$|/\*.*?\*/', '', content, flags=re.MULTILINE | re.DOTALL)
    
    # Check for class, interface, enum, struct, or delegate declarations
    return re.search(r'^\s*(public|internal|private|protected)?\s*(static|abstract|sealed)?\s*(class|interface|enum|struct|delegate)\s+\w+', 
                     content_no_comments, re.MULTILINE) is not None

def wrap_in_namespace(content, filepath):
    """Wrap C# code in ProjectWizard namespace"""
    
    # Skip if already has a namespace
    if has_namespace(content):
        print(f"  Skipping (already has namespace): {filepath}")
        return content
    
    # Skip if no actual code to wrap
    if not has_code_after_usings(content):
        print(f"  Skipping (no code to wrap): {filepath}")
        return content
    
    # Skip Editor scripts
    if '/_Editor/' in str(filepath) or 'Editor' in str(filepath):
        print(f"  Skipping (Editor script): {filepath}")
        return content
    
    lines = content.split('\n')
    
    # Find the last using statement
    last_using_index = -1
    for i, line in enumerate(lines):
        if line.strip().startswith('using '):
            last_using_index = i
    
    # Find where to insert namespace (after usings and blank lines)
    insert_index = last_using_index + 1
    while insert_index < len(lines) and lines[insert_index].strip() == '':
        insert_index += 1
    
    # Determine indentation level (count tabs/spaces in first code line)
    indent_char = '\t'  # Default to tabs (Unity standard)
    
    # Split content into before and after namespace
    before_namespace = lines[:insert_index]
    after_namespace = lines[insert_index:]
    
    # Add indentation to all code lines
    indented_code = []
    for line in after_namespace:
        if line.strip():  # Only indent non-empty lines
            indented_code.append(indent_char + line)
        else:
            indented_code.append(line)
    
    # Build the new content
    new_lines = before_namespace + [
        '',
        'namespace ProjectWizard',
        '{'
    ] + indented_code + [
        '}'
    ]
    
    return '\n'.join(new_lines)

def process_file(filepath):
    """Process a single C# file"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = wrap_in_namespace(content, filepath)
        
        if new_content != content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print(f"✓ Updated: {filepath}")
            return True
        return False
    except Exception as e:
        print(f"✗ Error processing {filepath}: {e}")
        return False

def main():
    scripts_dir = Path('Assets/_MagicGame/Scripts')
    
    if not scripts_dir.exists():
        print(f"Error: Directory {scripts_dir} not found")
        return
    
    cs_files = list(scripts_dir.rglob('*.cs'))
    print(f"Found {len(cs_files)} C# files\n")
    
    updated_count = 0
    for filepath in sorted(cs_files):
        if process_file(filepath):
            updated_count += 1
    
    print(f"\n{'='*60}")
    print(f"Processed {len(cs_files)} files")
    print(f"Updated {updated_count} files")
    print(f"{'='*60}")

if __name__ == '__main__':
    main()
