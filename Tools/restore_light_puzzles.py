"""Restore legacy puzzle records in-place; preserve current art and newer content.

The source is the verified pre-gravity snapshot, never an old whole-scene builder.
Default invocation writes a plan. --apply writes the scene after reference checks.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / "Assets/Scenes/InGame.unity"
OUTPUT = ROOT / "Logs/LegacyPuzzleRestore"
SOURCE_HASH = "8308e2746ca3d75a2a57512f4ce030a8b27283fa3ab1155bd222f584379ea7b3"


def records(text):
    chunks = re.split(r"(?m)^(--- !u!(\d+) &(-?\d+)[^\n]*\n)", text)
    return chunks[0], {int(chunks[i + 2]): [chunks[i], int(chunks[i + 1]), chunks[i + 3]]
                       for i in range(1, len(chunks), 4)}


def fields(body):
    matches = list(re.finditer(r"(?m)^  (\w+):", body))
    return {m.group(1): body[m.start():matches[i + 1].start() if i + 1 < len(matches) else len(body)]
            for i, m in enumerate(matches)}


def set_field(record, name, block):
    old = fields(record[2]).get(name)
    if old is not None:
        record[2] = record[2].replace(old, block, 1)


def name(body):
    value = fields(body).get("m_Name", "").split(":", 1)[-1].strip()
    if value.startswith('"'):
        value = value[1:-1]
        return re.sub(r"\\(u[0-9A-Fa-f]{4}|x[0-9A-Fa-f]{2}|U[0-9A-Fa-f]{8})",
                      lambda m: chr(int(m.group(1)[1:], 16)), value)
    return value


def local_id(body, key):
    match = re.search(r"^  " + key + r": \{fileID: (-?\d+)\}", body, re.M)
    return int(match.group(1)) if match else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    source_path = OUTPUT / "InGame-before.unity"
    if not source_path.exists():
        with zipfile.ZipFile(ROOT / "Logs/GravityMigration.verified.zip") as archive:
            source_path.write_bytes(archive.read("InGame-before.unity"))
    if hashlib.sha256(source_path.read_bytes()).hexdigest() != SOURCE_HASH:
        raise ValueError("Unexpected pre-gravity source. Audit it before restoring.")
    original = SCENE.read_bytes()
    header, current = records(original.decode("utf-8-sig"))
    _, source = records(source_path.read_text(encoding="utf-8-sig"))
    script_paths = {}
    for meta in (ROOT / "Assets/_BookTower/Scripts").rglob("*.cs.meta"):
        match = re.search(r"(?m)^guid: (\w+)", meta.read_text(encoding="utf-8-sig"))
        if match:
            script_paths[match.group(1)] = str(meta.relative_to(ROOT)).replace("\\", "/")[:-5]

    def script(record):
        match = re.search(r"m_Script: .*guid: (\w+)", record[2])
        return script_paths.get(match.group(1), "") if match else ""

    puzzle_ids = {i for i, r in source.items() if r[1] == 114 and "/Scripts/Puzzle/" in script(r)}
    restored = []
    for object_id in sorted(puzzle_ids):
        if object_id in current:
            continue
        component = list(source[object_id])
        owner_id = local_id(component[2], "m_GameObject")
        if owner_id not in current:
            raise ValueError(f"Missing owner {owner_id} for puzzle component {object_id}")
        current[object_id] = component
        owner = current[owner_id]
        component_list = fields(owner[2])["m_Component"]
        owner[2] = owner[2].replace(component_list, component_list + f"  - component: {{fileID: {object_id}}}\n", 1)
        restored.append({"id": object_id, "type": script(component)})

    active, positions, rewired, notes, goals, gravity_roots = [], [], [], [], [], []
    for object_id, old in source.items():
        if object_id not in current:
            continue
        target = current[object_id]
        before, after = fields(old[2]), fields(target[2])
        if old[1] == 1 and before.get("m_IsActive") != after.get("m_IsActive"):
            # StructureCollisionFit deliberately removed duplicate blockout paths later.
            if not name(old[2]).startswith("주동선_"):
                set_field(target, "m_IsActive", before["m_IsActive"])
                active.append(object_id)
        if old[1] == 4 and before.get("m_LocalPosition") != after.get("m_LocalPosition"):
            # Only these three pre-existing ceilings were raised for the gravity boards.
            if object_id in (8372250, 1132636737, 2036353008):
                set_field(target, "m_LocalPosition", before["m_LocalPosition"])
                positions.append(object_id)
        if old[1] != 114:
            continue
        kind = script(old)
        if kind.endswith("/Rooms/Room.cs"):
            if before.get("goal") != after.get("goal"):
                set_field(target, "goal", before["goal"])
                goals.append(object_id)
        if kind.endswith("/UI/ReadableNote.cs"):
            for key in ("title", "pages"):
                if key in before and before[key] != after.get(key):
                    set_field(target, key, before[key])
                    notes.append({"id": object_id, "field": key})
        for key, value in before.items():
            if value == after.get(key) or key not in after:
                continue
            refs = {int(x) for x in re.findall(r"\{fileID: (-?\d+)\}", value)}
            if refs & puzzle_ids:
                set_field(target, key, value)
                rewired.append({"id": object_id, "field": key})
        if kind.endswith("/Story/OpeningStoryDirector.cs"):
            set_field(target, "primaryPuzzle", "  primaryPuzzle: {fileID: 0}\n")

    for object_id, record in current.items():
        if record[1] == 1 and name(record[2]) == "GravityPage_RebuiltRoom":
            if fields(record[2]).get("m_IsActive") != "  m_IsActive: 0\n":
                set_field(record, "m_IsActive", "  m_IsActive: 0\n")
                gravity_roots.append(object_id)

    # Original component identifiers are retained, so every restored link is auditable.
    for object_id in puzzle_ids:
        for reference in re.findall(r"\{fileID: (-?\d+)\}", current[object_id][2]):
            if int(reference) != 0 and int(reference) not in current:
                raise ValueError(f"Unresolved local reference {reference} on puzzle {object_id}")

    plan = dict(sourceSha256=SOURCE_HASH, originalSha256=hashlib.sha256(original).hexdigest(),
                restoredComponents=restored, restoredActive=active, restoredCeilings=positions,
                rewired=rewired, restoredNoteFields=notes, restoredGoals=goals,
                disabledGravityRoots=gravity_roots)
    changed = restored or active or positions or rewired or notes or goals or gravity_roots
    if args.apply and changed:
        backup = OUTPUT / "InGame-current.verified.zip"
        if not backup.exists():
            with zipfile.ZipFile(backup, "w", zipfile.ZIP_DEFLATED, compresslevel=1) as archive:
                archive.writestr("InGame.unity", original)
        with zipfile.ZipFile(backup) as archive:
            if archive.read("InGame.unity") != original:
                raise ValueError("Scene differs from existing safety backup; preserve the new state first.")
        result = header + "".join(r[0] + r[2] for r in current.values())
        SCENE.write_text(result, encoding="utf-8", newline="\n")
        plan["resultSha256"] = hashlib.sha256(SCENE.read_bytes()).hexdigest()
    (OUTPUT / ("applied.json" if args.apply else "plan.json")).write_text(
        json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
    print("APPLIED" if args.apply else "PLAN", {key: len(value) for key, value in plan.items() if isinstance(value, list)})


if __name__ == "__main__":
    main()
