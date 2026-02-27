import os
import re

def remove_comments_and_empty_lines(content):
    # Regex to match single-line comments, multi-line comments, and strings
    pattern = r'//.*|/\*[\s\S]*?\*/|("(?:\\.|[^\\"])*")'
    
    def replacer(match):
        s = match.group(0)
        if s.startswith('/'):
            return "" # It's a comment
        else:
            return s # It's a string, keep it

    # Remove comments
    content = re.sub(pattern, replacer, content)
    
    # Remove empty lines and lines with only whitespace
    lines = content.splitlines()
    cleaned_lines = [line for line in lines if line.strip()]
    
    return "\n".join(cleaned_lines)

def process_directory(directory):
    for root, dirs, files in os.walk(directory):
        # Skip obj and bin directories
        if 'obj' in dirs:
            dirs.remove('obj')
        if 'bin' in dirs:
            dirs.remove('bin')
            
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                print(f"Processing: {file_path}")
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                cleaned_content = remove_comments_and_empty_lines(content)
                
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(cleaned_content)

if __name__ == "__main__":
    src_dir = r"d:\GitHub\DysonSphereMods\src"
    process_directory(src_dir)
    print("Cleanup complete.")
