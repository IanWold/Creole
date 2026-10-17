#!/usr/bin/env python3
"""Claude Code PostToolUse hook: run `dotnet format whitespace` on a C# file after Edit or Write.

Reads the hook payload from stdin. Formats only the edited file, in folder mode, so it needs no
solution load and takes a few seconds. Analyzer and style fixes are left to the build, which
enforces code style. Never fails the tool call: formatting problems are reported, not fatal.
"""

import json
import os
import shutil
import subprocess
import sys


def find_dotnet():
    # Prefer a user-local SDK install (dotnet-install.sh) over the system one, which may lack .NET 11.
    local = os.path.expanduser("~/.dotnet/dotnet")
    if os.path.isfile(local):
        return local
    return shutil.which("dotnet")


def main():
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        return 0
    path = payload.get("tool_input", {}).get("file_path", "")
    if not path.endswith(".cs") or not os.path.isfile(path):
        return 0
    dotnet = find_dotnet()
    if dotnet is None:
        return 0
    root = os.environ.get("CLAUDE_PROJECT_DIR", os.getcwd())
    result = subprocess.run(
        [dotnet, "format", "whitespace", root, "--folder", "--include", path],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"dotnet format failed on {path}:\n{result.stdout}{result.stderr}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
