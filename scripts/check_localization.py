import os
import sys
import re
import json
from pathlib import Path

LOCALIZE_CALL = '.Localize()'
PREFIXES = {
    'injector:': 'USBHelperInjector/',
    'launcher:': 'USBHelperLauncher/'
}


def find_ids(file):
    data = file.read_text('utf-8')
    data = re.sub(r'\r?\n', '', data)

    return [tup[0] for tup in re.findall(r'"([^"]+)"{}'.format(LOCALIZE_CALL), data)]


def main():
    os.chdir(Path(__file__).absolute().parents[1])  # cd to repository root

    code_ids = set()
    for prefix, directory in PREFIXES.items():
        for path in Path(directory).rglob('*.cs'):
            code_ids |= {prefix + id for id in find_ids(path)}

    with open('USBHelperLauncher/locale/en-US.local.json', 'r', encoding='utf-8') as f:
        json_ids = set(json.load(f).keys())

    code_diff = code_ids - json_ids
    json_diff = json_ids - code_ids

    if len(code_diff) > 0:
        print('[-] IDs present in code but not in json file:')
        for id in code_diff:
            print('\t{}'.format(id))
    if len(json_diff) > 0:
        print('[-] IDs present in json file but not in code:')
        for id in json_diff:
            print('\t{}'.format(id))

    if len(code_diff) == 0 and len(json_diff) == 0:
        print('[+] JSON locale file matches C# code')
        sys.exit(1)


if __name__ == '__main__':
    main()
