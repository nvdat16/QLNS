#!/usr/bin/env python3
"""Kiểm tra link nội bộ và anchor trong toàn bộ tài liệu Markdown của repository.

Gate `MarkdownLinksAndMermaidAreValid` (docs/architecture.md §12) dựa trên script này.
Trả về exit code khác 0 nếu có bất kỳ link hoặc anchor nội bộ nào hỏng.
"""
from __future__ import annotations

import os
import re
import sys
from glob import glob

LINK = re.compile(r"\[([^\]]*)\]\(([^)]+)\)")
HEADING = re.compile(r"#{1,6}\s+(.*)")
EXCLUDED_DIRS = ("node_modules", ".git", "dist")


def slugify(heading: str) -> str:
    text = heading.strip().lower()
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text)
    text = re.sub(r"[`*_]", "", text)
    text = re.sub(r"[^\w\s\-À-ỹ]", "", text)
    return text.replace(" ", "-")


def anchors_of(path: str) -> set[str]:
    with open(path, encoding="utf-8") as handle:
        return {slugify(m.group(1)) for line in handle if (m := HEADING.match(line.rstrip()))}


def main() -> int:
    files = [
        f
        for f in glob("**/*.md", recursive=True)
        if not any(part in f.split(os.sep) for part in EXCLUDED_DIRS)
    ]
    anchor_cache: dict[str, set[str]] = {}
    problems: list[str] = []

    for file in sorted(files):
        directory = os.path.dirname(file)
        text = open(file, encoding="utf-8").read()
        for match in LINK.finditer(text):
            link = match.group(2).split(" ")[0]
            if link.startswith(("http://", "https://", "mailto:")):
                continue
            path, _, anchor = link.partition("#")
            line = text[: match.start()].count("\n") + 1
            target = file if not path else os.path.normpath(os.path.join(directory, path))
            if path and not os.path.exists(target):
                problems.append(f"{file}:{line}: target không tồn tại — {link}")
                continue
            if anchor and target.endswith(".md"):
                if target not in anchor_cache:
                    anchor_cache[target] = anchors_of(target)
                if anchor not in anchor_cache[target]:
                    problems.append(f"{file}:{line}: anchor không tồn tại — {link}")

    if problems:
        print(f"{len(problems)} link/anchor hỏng:")
        print("\n".join(problems))
        return 1

    print(f"OK — đã kiểm tra {len(files)} file Markdown, không có link nội bộ hỏng.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
