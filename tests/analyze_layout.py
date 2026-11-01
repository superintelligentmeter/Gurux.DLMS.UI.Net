# superintelligentmeter contributors, 2026-10-30. Offline layout evidence. GPL-2.0-only.
import argparse, csv, json
from collections import defaultdict
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--policy', type=Path, required=True)
parser.add_argument('--baseline', type=Path)
args = parser.parse_args()
read = lambda p: json.loads(Path(p).read_text(encoding='utf-8-sig'))
policies = {r['Type']: r for r in read(args.policy)}
cases = read(args.output / 'layout-cases.json')
checks, appearance, captures_by_screen = {}, {}, defaultdict(list)
assemblies_by_screen = defaultdict(set)
compound = {'GXDLMSProfileGenericView', 'GXDLMSActivityCalendarView', 'GXDLMSGSMDiagnosticView', 'GXDLMSTariffPlanView'}

def check(case, cap, item, rule, passed, actual, expected):
    key = (case['Case'], item.get('Path', item.get('Name', '')), rule)
    row = dict(Case=case['Case'], Screen=case['Type'], State=case['State'], Monitor=case['Screen'],
               Dpi=cap.get('Dpi', 0), Control=item.get('Name', ''), Path=item.get('Path', ''), Rule=rule,
               Passed=bool(passed), Actual=actual, Expected=expected, Variant=cap.get('Variant', ''), Capture=cap.get('Path', ''))
    # Retain a failing observation instead of drowning the ledger in repeated resize/tab checks.
    if key not in checks or not passed:
        checks[key] = row

def appearance_key(m, lookup):
    owner = ''
    parent = lookup.get(m.get('ParentPath'))
    while parent:
        if parent['Type'] == 'GXValueField':
            owner = parent['Name']; break
        parent = lookup.get(parent.get('ParentPath'))
    return (m['Type'], m['Name'], owner)

def appearances(data):
    values = defaultdict(set)
    for cap in data.get('Captures', []):
        lookup = {m['Path']: m for m in cap.get('LayoutMetrics', [])}
        for m in lookup.values():
            if m['Name'] and m['Role'] not in ('Container', 'Region'):
                values[appearance_key(m, lookup)].add((m['FontFamily'], round(m['FontPoints'], 2), m['FontStyle'], m['ForeColor'], m['BackColor']))
    return values

baseline = {}
if args.baseline and (args.baseline / 'layout-cases.json').exists():
    for case in read(args.baseline / 'layout-cases.json'):
        if Path(case['Result']).exists(): baseline[case['Case']] = appearances(read(case['Result']))

for case in cases:
    if not Path(case['Result']).exists(): continue
    data = read(case['Result']); policy = policies[case['Type']]
    assemblies_by_screen[case['Type']].add(data.get('AssemblySha256', 'Unavailable'))
    short = case['Type'].rsplit('.', 1)[-1]
    reach = defaultdict(list)
    for cap in data.get('Captures', []):
        captures_by_screen[case['Type']].append(cap)
        metrics = cap.get('LayoutMetrics', []); lookup = {m['Path']: m for m in metrics}
        requested = cap.get('RequestedWidth') or cap.get('Width')
        for m in metrics:
            scale = m.get('Dpi', cap.get('Dpi', 96)) / 96
            if m['Type'] in ('ListBox', 'CheckedListBox', 'CheckBox', 'RadioButton') and m['Role'] != 'Internal':
                check(case, cap, m, 'native-list-choice-readable-height', m['Height'] + 2 >= m['FontHeight'], m['Height'], '>=font height ' + str(m['FontHeight']))
            if case['State'] == 'structured' and m['Type'] == 'GXValueField' and m['Role'] == 'Structured':
                active = next((x.get('FieldType', '') for x in cap.get('Controls', []) if x.get('Path') == m['Path']), '')
                budget = 120 if active in ('ListBox', 'CheckedListBox') else 160
                # Production rows reserve eight logical pixels for vertical editor margins.
                minimum = (budget - 8) * scale
                check(case, cap, m, 'structured-editor-height', m['Height'] + 2 >= minimum, round(m['Height'] / scale, 2), '>=' + str(budget - 8) + ' logical px usable editor (' + str(budget) + ' row budget)')
            if m['Role'] == 'Scalar':
                check(case, cap, m, 'scalar-width-cap', m['Width'] <= 640 * scale + 2, round(m['Width'] / scale, 2), '<=640 logical px')
                check(case, cap, m, 'readable-input-width', m['Width'] + 2 >= m['FontHeight'], m['Width'], '>=font height ' + str(m['FontHeight']))
                check(case, cap, m, 'readable-input-height', m['Height'] + 2 >= m['FontHeight'], m['Height'], '>=font height ' + str(m['FontHeight']))
                # Ascend through native hosts to the actual table cell before checking its alignment.
                cell = m
                while cell.get('ParentPath') in lookup and lookup[cell['ParentPath']]['Type'] != 'TableLayoutPanel': cell = lookup[cell['ParentPath']]
                region = lookup.get(cell.get('ParentPath'))
                if region and region['Type'] == 'TableLayoutPanel' and 0 <= cell.get('TableColumn', -1) < len(region.get('ColumnWidths', [])):
                    col = cell['TableColumn']
                    captions = [x for x in metrics if x.get('ParentPath') == region['Path'] and x['Role'] == 'Caption' and x.get('TableColumn') == 0 and x.get('TableRow') == cell.get('TableRow') and x.get('TableColumnSpan', 1) == 1]
                    if col == 1 and captions and len(region['ColumnWidths']) <= 3 and short not in compound:
                        expected = region['X'] + region.get('PaddingLeft', 0) + sum(region['ColumnWidths'][:col]) + cell.get('MarginLeft', 0)
                        check(case, cap, cell, 'field-cell-left-aligned', abs(cell['X'] - expected) <= 2, cell['X'], expected)
                        check(case, cap, cell, 'scalar-cell-width-cap', cell['Width'] <= 640 * scale + 2, round(cell['Width'] / scale, 2), '<=640 logical px')
            if m['Role'] == 'Caption' and m['Text'].strip():
                check(case, cap, m, 'wrapped-caption-fits', not m.get('CaptionClip'), f"{m['Width']}x{m['Height']}", 'Measured wrapped text fits without changing font')
            if m['Role'] in ('Scalar', 'Action', 'Choice'):
                reach[(m['Path'], requested)].append((cap, m))
            if m['Role'] == 'Region' and 2 <= len(m.get('ColumnWidths', [])) <= 3:
                rail = m['ColumnWidths'][0]
                captions = [x for x in metrics if x.get('ParentPath') == m['Path'] and x['Role'] == 'Caption' and x.get('TableColumn', 0) == 0 and x.get('TableColumnSpan', 1) == 1 and x['Width'] <= rail + 2]
                # A caption plus an inline flag inside the outer caption column is
                # part of that label rail, not an independent property section.
                caption_group = m.get('TableColumn') == 0 and m['Width'] <= 144 * scale + 2 and all(x['Role'] in ('Caption', 'Choice') for x in metrics if x.get('ParentPath') == m['Path'])
                if captions and short not in compound and policy['Category'] != 'CompactDialog' and not caption_group:
                    contains_tabs = any(x['Path'].startswith(m['Path'] + '/') and 'TabControl' in x['Type'] for x in metrics)
                    if not m.get('HasFlexibleContent') and not contains_tabs:
                        check(case, cap, m, 'property-section-width-cap', m['Width'] <= 800 * scale + 2, round(m['Width'] / scale, 2), '<=800 logical px')
                    # These source-declared ShareLabelColumn call sites follow an outer
                    # owner at narrow widths; do not infer ownership merely from equal rails.
                    rail_owner = m
                    parent = lookup.get(m.get('ParentPath'), {})
                    cipher_groups = {'GXCipheringSettings': {'groupBox2', 'groupBox3'}, 'GXDeviceCipheringSettings': {'groupBox1', 'groupBox2'}}
                    if parent.get('Type') == 'GroupBox':
                        if m.get('Name') == 'NativePropertyRows':
                            outer = lookup.get(parent.get('ParentPath'), {})
                            if outer.get('Name') == 'NativePropertyRows': rail_owner = outer
                        elif parent.get('Name') in cipher_groups.get(short, set()):
                            ancestor = parent
                            while ancestor.get('ParentPath') in lookup:
                                ancestor = lookup[ancestor['ParentPath']]
                                owner_parent = lookup.get(ancestor.get('ParentPath'), {})
                                if ancestor.get('Type') == 'TableLayoutPanel' and owner_parent.get('Name') == 'CipheringTab':
                                    rail_owner = ancestor
                                    break
                    expected = min(144 * scale, max(100 * scale, rail_owner['Width'] / 3))
                    check(case, cap, m, 'shared-label-column', abs(rail - expected) <= 2, round(rail / scale, 2),
                          str(round(expected / scale, 2)) + (' from shared owner ' + rail_owner['Path'] if rail_owner is not m else ''))
    for (_, requested), observations in reach.items():
        cap, m = max(observations, key=lambda x: x[1]['VisibleWidth'] * x[1]['VisibleHeight'])
        reachable = any(x['VisibleWidth'] + 4 >= x['Width'] and x['VisibleHeight'] + 4 >= x['Height'] for _, x in observations)
        check(case, cap, m, 'viewport-reachable-' + str(requested), reachable, f"visible {m['VisibleWidth']}x{m['VisibleHeight']} / {m['Width']}x{m['Height']}", 'Fully visible in at least one captured tab/scroll state')
    if case['Case'] in baseline:
        now = appearances(data)
        for key, expected in baseline[case['Case']].items():
            if key not in now: continue
            actual = now[key]
            exemplar = dict(Name=key[1], Path='appearance/' + '/'.join(key))
            check(case, {'Dpi': next(iter(data.get('Captures', [])), {}).get('Dpi', 0)}, exemplar, 'font-and-colors-unchanged', actual <= expected, str(sorted(actual)), str(sorted(expected)))

rows = list(checks.values()); failures = [r for r in rows if not r['Passed']]
def write_csv(path, items, fields=None):
    with path.open('w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=fields or list(items[0]) if items else fields or [])
        writer.writeheader(); writer.writerows(items)
write_csv(args.output / 'layout-checks.csv', rows)
write_csv(args.output / 'layout-findings.csv', failures, list(rows[0]) if rows else [])
ledger = []
for name, policy in policies.items():
    selected = [c for c in cases if c['Type'] == name]
    observed = sorted({c.get('Dpi', 0) for c in captures_by_screen[name]})
    failed = [r for r in failures if r['Screen'] == name]
    ledger.append(dict(Screen=name, Kind=policy['Kind'], Category=policy['Category'], Cases=len(selected), Captures=sum(c['Captures'] for c in selected),
                       Dpi=','.join(map(str, observed)), Dpi96='Observed' if 96 in observed else 'Unavailable: physical display not connected',
                       Errors=' | '.join(c['Error'] for c in selected if c['Error']), Findings=len(failed), Exceptions=policy['Exceptions'],
                       Status='Pass' if selected and not failed and all(not c['Error'] and c['ExitCode'] == 0 for c in selected) else 'Fail' if selected else 'Missing',
                       AssemblySha256=';'.join(sorted(assemblies_by_screen[name]))))
write_csv(args.output / 'screen-coverage-ledger.csv', ledger)
portable = [dict(Screen=r['Screen'], LayoutClass=r['Category'], NamedExceptions=r['Exceptions'], Result=r['Status'],
                 ActualDpi=r['Dpi'], Dpi96=r['Dpi96'], Cases=r['Cases'], Captures=r['Captures'], AssemblySha256=r['AssemblySha256']) for r in ledger]
write_csv(args.output / 'portable-screen-coverage.csv', portable)
summary = dict(Screens=len(policies), RenderedScreens=sum(bool(r['Cases']) and not r['Errors'] for r in ledger), PassedScreens=sum(r['Status'] == 'Pass' for r in ledger), Cases=len(cases), Captures=sum(c['Captures'] for c in cases),
               Checks=len(rows), Findings=len(failures), FailedCases=sum(bool(c['Error']) for c in cases),
               Rules={rule: sum(r['Rule'] == rule for r in failures) for rule in sorted({r['Rule'] for r in failures})},
               Dpi=sorted({c.get('Dpi', 0) for caps in captures_by_screen.values() for c in caps}))
(args.output / 'layout-summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(json.dumps(summary, indent=2))
