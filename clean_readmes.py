import os
import glob
import re

def clean_readmes():
    paths = glob.glob("zip/*_README.md") + glob.glob("src/*/README.md")
    
    version_pattern = re.compile(r'##\s*Version History[\s\S]*?(?=\n\s*---|\n\s*##\s|\Z)', re.IGNORECASE)

    for path in paths:
        try:
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()

            new_content = version_pattern.sub('', content)
            
            # Clean up dangling multiple blank lines
            new_content = re.sub(r'\n{3,}', '\n\n', new_content).strip() + '\n'

            if content != new_content:
                with open(path, "w", encoding="utf-8") as f:
                    f.write(new_content)
                print(f"Removed Version History from: {path}")

        except Exception as e:
            print(f"Error processing {path}: {e}")

if __name__ == "__main__":
    clean_readmes()
    print("Clean operation finished!")
