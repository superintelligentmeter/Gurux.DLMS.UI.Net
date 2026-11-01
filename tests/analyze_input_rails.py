# superintelligentmeter contributors, 2026-10-30. GPL-2.0-only.
"""Compare labeled scalar slots across containers in absolute view coordinates.

This consumes already captured layout evidence; it never opens a UI. Findings
are candidates until their screenshots/source establish that the sections are
related. Local table-column correctness alone cannot establish screen alignment.
"""
import argparse
import csv
import json
from collections import Counter, defaultdict
from itertools import combinations
from pathlib import Path


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def parents(item, lookup):
    current = lookup.get(item.get("ParentPath"))
    while current:
        yield current
        current = lookup.get(current.get("ParentPath"))


def csv_file(path, rows, fields):
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)


def extract(cap):
    metrics = cap.get("LayoutMetrics", [])
    lookup = {m["Path"]: m for m in metrics}
    records = []
    for field in metrics:
        if field["Role"] not in ("Scalar", "Structured", "Reference", "FlexibleData"):
            continue
        ancestors = list(parents(field, lookup))
        if any(p["Role"] in ("Scalar", "Structured", "Reference", "FlexibleData") for p in ancestors):
            continue
        scale = field.get("Dpi", cap.get("Dpi", 96)) / 96
        slot = lookup.get(field.get("ParentPath"), field)
        if slot["Type"] not in ("ModernFieldFrame", "NativeComboHost"):
            slot = field
        tab = next((p["Path"] for p in ancestors if p["Type"] == "TabPage"), "<root>")
        record = dict(Control=field["Name"], Path=field["Path"], FieldType=field["Type"],
                      Role=field["Role"], SlotType=slot["Type"], X=slot["X"], Right=slot["X"] + slot["Width"],
                      Width=slot["Width"], Height=slot["Height"], Dpi=field.get("Dpi", cap.get("Dpi", 96)),
                      Tab=tab, Scope="single-column", Section="", Caption="", CaptionX="", Classification="", Reason="")
        if field["Role"] != "Scalar":
            record.update(Classification="Flexible", Reason="Active structured/reference presentation; independent width budget")
            records.append(record)
            continue
        if any(p["Type"] == "FlowLayoutPanel" and any(m["Role"] == "Caption" and m.get("ParentPath") == p["Path"] for m in metrics) for p in ancestors):
            record.update(Classification="InlineCompound", Reason="Native editor inside a captioned inline action group (for example Association Version)")
            records.append(record)
            continue
        cell = slot
        while cell.get("ParentPath") in lookup and lookup[cell["ParentPath"]]["Type"] != "TableLayoutPanel":
            cell = lookup[cell["ParentPath"]]
        table = lookup.get(cell.get("ParentPath"))
        caption = None
        if table and table["Type"] == "TableLayoutPanel":
            column = cell.get("TableColumn", -1)
            row = cell.get("TableRow", -1)
            columns = len(table.get("ColumnWidths", []))
            siblings = [m for m in metrics if m.get("ParentPath") == table["Path"] and m.get("TableRow") == row]
            caption_column = column - 1 if columns >= 4 else 0
            caption = next((m for m in siblings if m["Role"] == "Caption" and m.get("TableColumn") == caption_column and m.get("TableColumnSpan", 1) == 1 and m.get("Text", "").strip()), None)
            if column > 1 and columns <= 3:
                record.update(Classification="InlineCompound", Reason="Secondary control in a property/action row", Section=table["Path"])
                records.append(record)
                continue
            if caption:
                record["Section"] = table["Path"]
                if columns >= 4:
                    record["Scope"] = table["Path"] + "::column" + str(column)
                    record["Reason"] = "Intentional paired/multiple columns; compare within this column only"
        if caption is None:
            # Legacy fixed-coordinate groups have no TableRow metadata. Pair only
            # nearby captions to the left in the same visible tab context.
            candidates = []
            for label in metrics:
                if label["Role"] != "Caption" or not label.get("Text", "").strip():
                    continue
                label_tab = next((p["Path"] for p in parents(label, lookup) if p["Type"] == "TabPage"), "<root>")
                vertical = min(label["Y"] + label["Height"], slot["Y"] + slot["Height"]) - max(label["Y"], slot["Y"])
                gap = slot["X"] - label["X"] - label["Width"]
                if label_tab == tab and vertical >= min(label["Height"], slot["Height"]) * .45 and -2 <= gap <= 160 * scale:
                    candidates.append((max(0, gap) + abs(label["Y"] - slot["Y"]), label))
            if candidates:
                caption = min(candidates, key=lambda x: x[0])[1]
                owner = next((p for p in ancestors if p["Type"] in ("GroupBox", "TableLayoutPanel", "TabPage")), None)
                record["Section"] = owner["Path"] if owner else "<root>"
                record["Reason"] = "Legacy geometric caption association; verify source when flagged"
        if caption:
            record.update(Caption=caption["Name"], CaptionX=caption["X"], Classification="Property")
        else:
            record.update(Classification="UnpairedScalar", Reason="No adjacent property caption; inspect as an inline, matrix or standalone editor")
        records.append(record)
    return records


def analyze(output, policy_path):
    cases = read(output / "layout-cases.json")
    policies = {p["Type"]: p for p in read(policy_path)}
    records, comparisons = {}, {}
    per_screen = defaultdict(lambda: dict(cases=0, captures=0, dpi=set(), errors=[], kinds=Counter(), controls=set(), sections=set(), tabs=set(), hashes=set()))
    for case in cases:
        screen = per_screen[case["Type"]]
        screen["cases"] += 1
        if case.get("Error") or case.get("ExitCode"):
            screen["errors"].append(case.get("Error") or "Worker exit " + str(case.get("ExitCode")))
        if not Path(case["Result"]).exists():
            continue
        data = read(case["Result"])
        screen["hashes"].add(data.get("AssemblySha256", "Unavailable"))
        for cap in data.get("Captures", []):
            screen["captures"] += 1
            screen["dpi"].add(cap.get("Dpi", 0))
            width = cap.get("RequestedWidth") or cap.get("Width")
            rows = extract(cap)
            for row in rows:
                key = (case["Case"], width, row["Path"])
                records.setdefault(key, dict(Screen=case["Type"], Case=case["Case"], RequestedWidth=width, Capture=cap.get("Path", ""), **row))
                screen["controls"].add(row["Path"])
                screen["sections"].add(row["Section"])
                screen["tabs"].add(row["Tab"])
            groups = defaultdict(list)
            for row in rows:
                if row["Classification"] == "Property":
                    groups[(row["Tab"], row["Scope"], row["Section"])].append(row)
            for (tab, scope, section), items in groups.items():
                # A representative edge for each actual property section; retain
                # all controls and all raw edge clusters in the inventory CSV.
                modes = Counter(r["X"] for r in items)
                edge = modes.most_common(1)[0][0]
                for row in items:
                    if row["X"] == edge:
                        row["Representative"] = True
                        break
            representatives = [(key, next(r for r in items if r.get("Representative"))) for key, items in groups.items()]
            for section_key, anchor in representatives:
                for row in groups[section_key]:
                    if row is anchor:
                        continue
                    key = (case["Case"], width, section_key, ("control", row["Path"]))
                    delta = abs(anchor["X"] - row["X"])
                    caption_delta = abs(anchor["CaptionX"] - row["CaptionX"])
                    comparison = dict(Screen=case["Type"], Case=case["Case"], RequestedWidth=width, Dpi=row["Dpi"], Tab=section_key[0],
                                      SectionA=anchor["Section"], SectionB=row["Section"], ControlA=anchor["Control"], ControlB=row["Control"],
                                      FieldXA=anchor["X"], FieldXB=row["X"], FieldDelta=delta, CaptionXA=anchor["CaptionX"], CaptionXB=row["CaptionX"],
                                      CaptionDelta=caption_delta, Passed=delta <= 2 and caption_delta <= 2, Capture=cap.get("Path", ""),
                                      Rule="single-section-explicit-input-and-caption-edges")
                    if key not in comparisons or not comparison["Passed"]:
                        comparisons[key] = comparison
            for (a_key, a), (b_key, b) in combinations(representatives, 2):
                if a_key[:2] != b_key[:2] or a_key[2] == b_key[2]:
                    continue
                scale = a["Dpi"] / 96
                overlap = min(a["Right"], b["Right"]) - max(a["X"], b["X"])
                # Different horizontal lanes are intentional columns, not
                # related nested groups. Nested 8/12/20px insets remain eligible.
                if abs(a["CaptionX"] - b["CaptionX"]) > 48 * scale or overlap < min(a["Width"], b["Width"]) * .5:
                    continue
                delta = abs(a["X"] - b["X"])
                caption_delta = abs(a["CaptionX"] - b["CaptionX"])
                key = (case["Case"], width, a_key, b_key)
                comparison = dict(Screen=case["Type"], Case=case["Case"], RequestedWidth=width, Dpi=a["Dpi"], Tab=a_key[0],
                                  SectionA=a["Section"], SectionB=b["Section"], ControlA=a["Control"], ControlB=b["Control"],
                                  FieldXA=a["X"], FieldXB=b["X"], FieldDelta=delta, CaptionXA=a["CaptionX"], CaptionXB=b["CaptionX"],
                                  CaptionDelta=caption_delta, Passed=delta <= 2 and caption_delta <= 2, Capture=cap.get("Path", ""),
                                  Rule="related-sections-share-absolute-input-and-caption-edges")
                if key not in comparisons or not comparison["Passed"]:
                    comparisons[key] = comparison
    all_rows = list(records.values())
    for row in all_rows:
        per_screen[row["Screen"]]["kinds"][row["Classification"]] += 1
    checks = list(comparisons.values())
    candidates = [c for c in checks if not c["Passed"]]
    csv_file(output / "absolute-input-rail-inventory.csv", all_rows, list(all_rows[0]) if all_rows else ["Screen"])
    csv_file(output / "cross-container-rail-checks.csv", checks, list(checks[0]) if checks else ["Screen"])
    csv_file(output / "cross-container-rail-candidates.csv", candidates, list(checks[0]) if checks else ["Screen"])
    inventory = read(output / "inventory" / "inventory.json") if (output / "inventory" / "inventory.json").exists() else None
    selected_kinds = {c["Kind"] for c in cases}
    names = [s["Type"] for s in inventory["Screens"] if s["Kind"] in selected_kinds] if inventory else sorted(per_screen)
    ledger = []
    for name in names:
        s = per_screen[name]
        own = [c for c in checks if c["Screen"] == name]
        failed = [c for c in own if not c["Passed"]]
        policy = policies.get(name, {})
        status = "Missing" if not s["cases"] else "Worker error" if s["errors"] else "Needs rendered/source review" if failed else "Compared: aligned" if own else "Single scalar rail; flexible surface exception" if s["kinds"]["Property"] else "Table/list screen; scalar-rail comparison not applicable"
        ledger.append(dict(Screen=name, LayoutClass=policy.get("Category", "Unclassified"), Cases=s["cases"], Captures=s["captures"], ActualDpi=",".join(map(str, sorted(s["dpi"]))),
                           Controls=len(s["controls"]), PropertySections=len(s["sections"] - {""}), TabContexts=len(s["tabs"]), Comparisons=len(own), CandidateComparisons=len(failed),
                           PropertyRecords=s["kinds"]["Property"], UnpairedScalarRecords=s["kinds"]["UnpairedScalar"], FlexibleRecords=s["kinds"]["Flexible"], InlineRecords=s["kinds"]["InlineCompound"],
                           NamedExceptions=policy.get("Exceptions", ""), Status=status, AssemblySha256=";".join(sorted(s["hashes"]))))
    csv_file(output / "cross-container-screen-ledger.csv", ledger, list(ledger[0]) if ledger else ["Screen"])
    summary = dict(Screens=len(names), RenderedScreens=sum(bool(s["captures"]) for s in per_screen.values()), Cases=len(cases), Captures=sum(s["captures"] for s in per_screen.values()),
                   InputRecords=len(all_rows), TotalRailComparisons=len(checks), CrossContainerComparisons=sum(c["Rule"].startswith("related-") for c in checks), CandidateComparisons=len(candidates), CandidateScreens=sorted({c["Screen"] for c in candidates}),
                   CoordinateSpace="Absolute view client coordinates (LayoutMetrics.X/Y), independent of local parent/table origins", PixelTolerance=2,
                   Scope="Related scalar sections in the same visible tab and overlapping horizontal lane; structured/reference fields and secondary inline controls inventoried separately",
                   Limitations="Candidates require rendered/source confirmation. A single-section screen is inventoried, not falsely reported as a successful cross-container comparison.")
    (output / "cross-container-summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument("--policy", type=Path, required=True)
    options = parser.parse_args()
    analyze(options.output, options.policy)
