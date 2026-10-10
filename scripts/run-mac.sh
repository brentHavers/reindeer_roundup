#!/usr/bin/env bash
# Run the Mac DesktopGL build of ReindeerRoundup.
# Optional: first args can be -c Release (or pass through to dotnet run after --).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REINDEER_ROUNDUP_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REINDEER_ROUNDUP_ROOT/ReindeerRoundup.Mac/ReindeerRoundup.Mac.csproj"

dotnet run --project "$PROJECT" "$@"
