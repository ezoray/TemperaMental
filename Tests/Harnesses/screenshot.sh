#!/bin/zsh
# Saves the Game view as a PNG without importing anything into Assets/ (an import in play mode can trigger a
# recompile). Crop afterwards with e.g. `sips -c 140 940 --cropOffset 70 180 shot.png --out crop.png`.
#   ./Tests/Harnesses/screenshot.sh <out.png>
set -e
project="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$project"
unity command game_view --project-path="$project" --format json 2>/dev/null | python3 -c '
import sys, json, base64, re
s = sys.stdin.read()
m = re.search(r"\"(?:image|base64|data|png)[A-Za-z]*\"\s*:\s*\"([A-Za-z0-9+/=]{1000,})\"", s)
if not m:
    print("no image in output:", s[:300]); sys.exit(1)
open(sys.argv[1], "wb").write(base64.b64decode(m.group(1)))
print("saved", sys.argv[1])' "$1"
