import zipfile, os
for z in sorted(os.listdir('final')):
    if z.endswith('.zip'):
        names = zipfile.ZipFile(os.path.join('final', z)).namelist()
        has_shared = 'Shared.dll' in names
        print(f"{'FAIL' if has_shared else 'OK  '} {z}: {names}")
