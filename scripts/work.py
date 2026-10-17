#!/usr/bin/env python3
"""Consistency checks and mechanical edits for the Creole work tracker.

Usage:
  python3 scripts/work.py check            Lint links, anchors, card fields and the README.
  python3 scripts/work.py brief            Short orientation (used by the SessionStart hook).
  python3 scripts/work.py stop-hook        Stop hook: exit 2 with errors on stderr if `check` fails.
  python3 scripts/work.py status           Summarize card statuses, in-progress and ready items.
  python3 scripts/work.py ready            List Not-started cards whose blockers are all Done.
  python3 scripts/work.py card <ID>        Print a card's path, status, blockers and dependents.
  python3 scripts/work.py move <path> <dest-dir> [--dry-run]
                                           Move a file or folder and rewrite every relative
                                           Markdown link that points into or out of it.

The rules this script enforces are described in CONTRIBUTING.md.
Exit code is 1 when `check` finds errors.
"""

from __future__ import annotations

import os
import re
import shutil
import sys
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"
CARD_DIRS = [WORK / "active", WORK / "complete", WORK / "backburner"]
STATUSES = ["Not started", "Researching", "Designing", "Building", "Done"]
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "artifacts", "TestResults"}

LINK_RE = re.compile(r"(?<!!)\[(?:[^\]\[]|\[[^\]]*\])*\]\(([^)\s]+)\)")
CARD_FILE_RE = re.compile(r"^([A-Z]+-\d+)-.+\.md$")
CARD_REF_RE = re.compile(r"\[([A-Z]+-\d+)\b")


# ---------------------------------------------------------------- markdown helpers

def md_files() -> list[Path]:
    out = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        out += [Path(dirpath) / f for f in filenames if f.endswith(".md")]
    return sorted(out)


def strip_code(text: str) -> str:
    """Blank out fenced code blocks and inline code so links inside them are ignored."""
    text = re.sub(r"```.*?```", lambda m: "\n" * m.group(0).count("\n"), text, flags=re.S)
    return re.sub(r"`[^`\n]*`", lambda m: " " * len(m.group(0)), text)


def slugify(heading: str) -> str:
    """GitHub-style heading anchor."""
    h = re.sub(r"<[^>]+>", "", heading).strip().lower()
    h = h.replace("`", "")
    h = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", h)
    h = re.sub(r"[^\w\- ]", "", h)
    return h.replace(" ", "-")


_anchor_cache: dict[Path, set[str]] = {}


def anchors(path: Path) -> set[str]:
    if path not in _anchor_cache:
        seen: dict[str, int] = {}
        result = set()
        for line in strip_code(path.read_text(encoding="utf-8")).splitlines():
            m = re.match(r"^#{1,6}\s+(.*?)\s*#*\s*$", line)
            if not m:
                continue
            slug = slugify(m.group(1))
            n = seen.get(slug, 0)
            seen[slug] = n + 1
            result.add(slug if n == 0 else f"{slug}-{n}")
        _anchor_cache[path] = result
    return _anchor_cache[path]


def is_relative(target: str) -> bool:
    return not re.match(r"^[a-z][a-z0-9+.-]*:", target, re.I) and not target.startswith("/")


def section(text: str, name: str) -> str:
    m = re.search(rf"^## {re.escape(name)}\s*$(.*?)(?=^## |\Z)", text, re.M | re.S)
    return m.group(1) if m else ""


# ---------------------------------------------------------------- cards

@dataclass
class Card:
    id: str
    path: Path
    title: str
    fields: dict[str, str]
    blocked_by: list[str]
    external: list[str]
    unblocks: list[str]
    criteria: list[bool]
    text: str = field(repr=False)

    @property
    def status(self) -> str:
        return self.fields.get("Status", "")

    @property
    def location(self) -> str:
        return self.path.relative_to(WORK).parts[0]


def load_cards() -> dict[str, Card]:
    cards: dict[str, Card] = {}
    for base in CARD_DIRS:
        if not base.exists():
            continue
        for p in sorted(base.rglob("*.md")):
            m = CARD_FILE_RE.match(p.name)
            if not m:
                continue
            text = p.read_text(encoding="utf-8")
            title_m = re.search(r"^# [A-Z]+-\d+: (.+)$", text, re.M)
            fields = dict(re.findall(r"^\| (\w[\w ]*?) \| (.+?) \|\s*$", text, re.M))
            crit = section(text, "Acceptance criteria")
            cards[m.group(1)] = Card(
                id=m.group(1),
                path=p,
                title=title_m.group(1) if title_m else "",
                fields=fields,
                blocked_by=CARD_REF_RE.findall(section(text, "Blocked by")),
                external=[b for b in re.findall(r"^- (.+)$", section(text, "Blocked by"), re.M)
                          if not CARD_REF_RE.search(b) and not b.startswith("~~")],
                unblocks=CARD_REF_RE.findall(section(text, "Unblocks")),
                criteria=[c.lower() == "x" for c in re.findall(r"^\s*- \[( |x|X)\]", crit, re.M)],
                text=text,
            )
    return cards


def ready(cards: dict[str, Card]) -> list[Card]:
    return [
        c for c in cards.values()
        if c.status == "Not started" and c.location == "active" and not c.external
        and all(b in cards and cards[b].status == "Done" for b in c.blocked_by)
    ]


def rel(p: Path) -> str:
    return str(p.relative_to(ROOT))


# ---------------------------------------------------------------- check

def collect() -> tuple[list[str], list[str], dict[str, Card]]:
    """Run every consistency check. Returns (errors, warnings, cards)."""
    errors: list[str] = []
    warnings: list[str] = []
    cards = load_cards()

    # Links and anchors in every Markdown file.
    for f in md_files():
        body = strip_code(f.read_text(encoding="utf-8"))
        for lineno, line in enumerate(body.splitlines(), 1):
            for target in LINK_RE.findall(line):
                if not is_relative(target):
                    continue
                path_part, _, anchor = target.partition("#")
                dest = (f.parent / path_part).resolve() if path_part else f
                where = f"{rel(f)}:{lineno}"
                if not dest.exists():
                    errors.append(f"{where}: broken link -> {target}")
                elif anchor and dest.suffix == ".md" and anchor not in anchors(dest):
                    errors.append(f"{where}: missing anchor #{anchor} in {rel(dest)}")

    # Card fields and dependency symmetry.
    for c in cards.values():
        where = rel(c.path)
        if c.status not in STATUSES:
            errors.append(f"{where}: Status '{c.status}' is not one of {STATUSES}")
        if not c.title:
            errors.append(f"{where}: missing '# {c.id}: Title' heading")
        for b in c.blocked_by:
            if b not in cards:
                errors.append(f"{where}: blocked by unknown card {b}")
            elif c.id not in cards[b].unblocks:
                errors.append(f"{where}: blocked by {b}, but {b} does not list {c.id} under Unblocks")
        for u in c.unblocks:
            if u not in cards:
                errors.append(f"{where}: unblocks unknown card {u}")
            elif c.id not in cards[u].blocked_by:
                errors.append(f"{where}: unblocks {u}, but {u} does not list {c.id} under Blocked by")
        if c.status == "Done" and not all(c.criteria):
            errors.append(f"{where}: Done but {c.criteria.count(False)} acceptance criteria unchecked")
        if c.status == "Done" and "## Outcome" not in c.text:
            warnings.append(f"{where}: Done but has no ## Outcome section")
        if c.status in ("Researching", "Designing", "Building"):
            open_blockers = [b for b in c.blocked_by if b in cards and cards[b].status != "Done"]
            if open_blockers:
                warnings.append(f"{where}: {c.status} while blockers are open: {', '.join(open_blockers)}")
        if c.location == "complete" and c.status != "Done":
            errors.append(f"{where}: in work/complete/ but Status is {c.status}")

    # README coverage, totals and the ready list.
    readme = WORK / "README.md"
    if readme.exists():
        rtext = readme.read_text(encoding="utf-8")
        for c in cards.values():
            if c.location != "backburner" and f"[{c.id}]" not in rtext:
                errors.append(f"work/README.md: card {c.id} is not listed")
        m = re.search(r"\*\*Totals:\*\* (\d+) epics, (\d+) work items", rtext)
        tracked = [c for c in cards.values() if c.location != "backburner"]
        epics = {c.path.parent for c in tracked}
        if m and (int(m.group(1)), int(m.group(2))) != (len(epics), len(tracked)):
            errors.append(f"work/README.md: Totals say {m.group(1)} epics, {m.group(2)} items; "
                          f"found {len(epics)} epics, {len(tracked)} items")
        listed = set(CARD_REF_RE.findall(section_any(rtext, "Can start now")))
        expected = {c.id for c in ready(cards)}
        if listed != expected:
            if expected - listed:
                errors.append(f"work/README.md: 'Can start now' is missing {sorted(expected - listed)}")
            if listed - expected:
                errors.append(f"work/README.md: 'Can start now' lists not-ready {sorted(listed - expected)}")
        listed_ip = set(CARD_REF_RE.findall(section_any(rtext, "In progress")))
        expected_ip = {c.id for c in cards.values() if c.status in ("Researching", "Designing", "Building")}
        if listed_ip != expected_ip:
            errors.append(f"work/README.md: 'In progress' lists {sorted(listed_ip)}, cards say {sorted(expected_ip)}")

    # Index READMEs for research and decisions list every document.
    for sub in ("research", "decisions", "archive"):
        d = WORK / "docs" / sub
        idx = d / "README.md"
        if not idx.exists():
            continue
        itext = idx.read_text(encoding="utf-8")
        for p in sorted(d.glob("*.md")):
            if p.name != "README.md" and f"({p.name}" not in itext:
                errors.append(f"{rel(idx)}: does not link {p.name}")

    return errors, warnings, cards


def cmd_check() -> int:
    errors, warnings, cards = collect()
    for w in warnings:
        print(f"warning: {w}")
    for e in errors:
        print(f"error: {e}")
    print(f"\n{len(cards)} cards, {len(errors)} errors, {len(warnings)} warnings")
    return 1 if errors else 0


def proposed_decisions() -> list[str]:
    d = WORK / "docs" / "decisions"
    return [rel(p) for p in sorted(d.glob("[0-9]*.md"))
            if re.search(r"^\| Status \| Proposed \|", p.read_text(encoding="utf-8"), re.M)] if d.exists() else []


def cmd_brief() -> int:
    """Compact orientation, printed into context by the SessionStart hook."""
    errors, _, cards = collect()
    active = [c for c in cards.values() if c.status in ("Researching", "Designing", "Building")]
    done = sum(c.status == "Done" for c in cards.values())
    health = "clean" if not errors else f"{len(errors)} errors (run `python3 scripts/work.py check`)"
    print(f"Creole tracker: {len(cards)} cards, {done} done, {len(active)} in progress, "
          f"{len(ready(cards))} ready to start. Tracker check: {health}.")
    for c in active:
        notes = re.findall(r"^- (\d{4}-\d{2}-\d{2}.*)$", section(c.text, "Progress notes"), re.M)
        print(f"- {c.id} [{c.status}] {c.title} ({rel(c.path)})" + (f"\n  last note: {notes[-1]}" if notes else ""))
    for d in proposed_decisions():
        print(f"- Decision awaiting the maintainer: {d}")
    return 0


def cmd_stop_hook() -> int:
    """Stop hook: block the end of a turn while the tracker is inconsistent (exit 2 feeds stderr to Claude)."""
    import json
    try:
        payload = json.load(sys.stdin)
    except Exception:
        payload = {}
    if payload.get("stop_hook_active"):
        return 0
    errors, _, _ = collect()
    if not errors:
        return 0
    shown = "\n".join(f"- {e}" for e in errors[:15])
    more = f"\n- … and {len(errors) - 15} more" if len(errors) > 15 else ""
    print("scripts/work.py check found tracker inconsistencies. Fix the ones your changes caused; "
          "if they predate this turn, tell the maintainer instead.\n" + shown + more, file=sys.stderr)
    return 2


def section_any(text: str, name: str) -> str:
    """Body of a heading at any level, up to the next heading of the same or higher level."""
    m = re.search(rf"^(#+) {re.escape(name)}\s*$", text, re.M)
    if not m:
        return ""
    level = len(m.group(1))
    rest = text[m.end():]
    end = re.search(rf"^#{{1,{level}}} ", rest, re.M)
    return rest[: end.start()] if end else rest


# ---------------------------------------------------------------- status / ready / card

def cmd_status() -> int:
    errors, warnings, cards = collect()
    print(f"Tracker check: {'clean' if not errors else f'{len(errors)} errors'}, {len(warnings)} warnings")
    counts = {s: 0 for s in STATUSES}
    for c in cards.values():
        counts[c.status] = counts.get(c.status, 0) + 1
    print("Status counts: " + ", ".join(f"{k}: {v}" for k, v in counts.items()))
    by_phase: dict[str, list[Card]] = {}
    for c in cards.values():
        by_phase.setdefault(c.fields.get("Phase", "?"), []).append(c)
    print("\nBy phase (done/total):")
    for phase, cs in by_phase.items():
        print(f"  {phase}: {sum(c.status == 'Done' for c in cs)}/{len(cs)}")
    active = [c for c in cards.values() if c.status in ("Researching", "Designing", "Building")]
    print("\nIn progress:")
    for c in active or []:
        print(f"  {c.id} [{c.status}] {c.title}  ({rel(c.path)})")
    if not active:
        print("  (none)")
    print("\nDecisions awaiting the maintainer:")
    for d in proposed_decisions() or ["(none)"]:
        print(f"  {d}")
    cmd_ready(cards)
    return 0


def cmd_ready(cards: dict[str, Card] | None = None) -> int:
    cards = cards or load_cards()
    print("\nReady to start (all blockers Done):")
    for c in ready(cards):
        print(f"  {c.id} {c.title}  ({rel(c.path)})")
    return 0


def cmd_card(card_id: str) -> int:
    cards = load_cards()
    c = cards.get(card_id.upper())
    if not c:
        print(f"No card {card_id}")
        return 1
    print(f"{c.id}: {c.title}\n  path: {rel(c.path)}\n  status: {c.status}")
    for k in ("Phase", "Gate"):
        if k in c.fields:
            print(f"  {k.lower()}: {c.fields[k]}")
    print(f"  criteria: {sum(c.criteria)}/{len(c.criteria)} checked")
    print("  blocked by:")
    for b in c.blocked_by:
        print(f"    {b} [{cards[b].status if b in cards else 'UNKNOWN'}]")
    for e in c.external:
        print(f"    external: {e}")
    print("  unblocks:")
    for u in c.unblocks:
        others = [b for b in cards[u].blocked_by if b != c.id and cards[b].status != "Done"] if u in cards else []
        tail = f" (also waiting on {', '.join(others)})" if others else ""
        print(f"    {u} [{cards[u].status if u in cards else 'UNKNOWN'}]{tail}")
    return 0


# ---------------------------------------------------------------- move

def cmd_move(src_arg: str, dest_arg: str, dry_run: bool) -> int:
    src = (ROOT / src_arg).resolve()
    dest_dir = (ROOT / dest_arg).resolve()
    if not src.exists():
        print(f"No such path: {src_arg}")
        return 1
    new_root = dest_dir / src.name
    if new_root.exists():
        print(f"Destination exists: {rel(new_root)}")
        return 1

    def moved(p: Path) -> Path:
        """Where a path will live after the move."""
        try:
            return new_root / p.relative_to(src)
        except ValueError:
            return p

    changed = 0
    for f in md_files():
        old_text = f.read_text(encoding="utf-8")
        new_loc = moved(f)

        def fix(m: re.Match) -> str:
            target = m.group(1)
            if not is_relative(target):
                return m.group(0)
            path_part, hash_, anchor = target.partition("#")
            if not path_part:
                return m.group(0)
            old_dest = (f.parent / path_part).resolve()
            new_dest = moved(old_dest)
            # A link needs rewriting only when exactly one of its ends moves.
            if (new_dest != old_dest) == (new_loc != f):
                return m.group(0)
            new_rel = os.path.relpath(new_dest, new_loc.parent)
            if path_part.endswith("/") and not new_rel.endswith("/"):
                new_rel += "/"
            if new_rel == path_part:
                return m.group(0)
            return m.group(0).replace(f"({target})", f"({new_rel}{hash_}{anchor})")

        new_text = LINK_RE.sub(fix, old_text)
        if new_text != old_text:
            changed += 1
            print(f"rewrite links: {rel(f)}")
            if not dry_run:
                f.write_text(new_text, encoding="utf-8")

    print(f"{'would move' if dry_run else 'move'}: {rel(src)} -> {rel(new_root)} ({changed} files relinked)")
    if not dry_run:
        dest_dir.mkdir(parents=True, exist_ok=True)
        shutil.move(str(src), str(new_root))
    return 0


def main(argv: list[str]) -> int:
    if not argv or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 0
    cmd, args = argv[0], argv[1:]
    if cmd == "check":
        return cmd_check()
    if cmd == "brief":
        return cmd_brief()
    if cmd == "stop-hook":
        return cmd_stop_hook()
    if cmd == "status":
        return cmd_status()
    if cmd == "ready":
        return cmd_ready()
    if cmd == "card" and len(args) == 1:
        return cmd_card(args[0])
    if cmd == "move" and len([a for a in args if a != "--dry-run"]) == 2:
        pos = [a for a in args if a != "--dry-run"]
        return cmd_move(pos[0], pos[1], "--dry-run" in args)
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
