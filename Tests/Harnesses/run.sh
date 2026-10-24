#!/bin/zsh
# Runs one entry point of a harness in the open Unity Editor (play mode) and prints its result.
#   ./Tests/Harnesses/run.sh <Harness> <Entry> [argument]
#   e.g. ./Tests/Harnesses/run.sh EventsTestHarness Play internal
set -e
if [ $# -lt 2 ]; then
  echo "usage: $0 <Harness> <Entry> [argument]" >&2
  exit 2
fi
project="$(cd "$(dirname "$0")/../.." && pwd)"
harness="$1"
entry="$2"
args=()
if [ -n "$3" ]; then args=(--args "[\"$3\"]"); fi
out=$(cd "$project" && unity command --project-path="$project" script run --file "Tests/Harnesses/$harness.cs" \
  --entry "$harness.$entry" "${args[@]}" --timeout 120 --result-only --no-banner 2>&1)
python3 -c '
import sys, json
s = sys.argv[1]
try:
    d = json.loads(s)
except Exception:
    print(s); sys.exit(1)
if d.get("success"):
    print(d.get("result"))
else:
    for diag in d.get("diagnostics") or []:
        print("line", diag.get("line"), ":", diag.get("message"))
    print(d.get("error") or d.get("errors") or d)
    sys.exit(1)' "$out"
