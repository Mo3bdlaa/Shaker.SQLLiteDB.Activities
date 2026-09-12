#!/usr/bin/env bash
# Builds, tests and packs the activity library. The .nupkg lands in ./artifacts.
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> Restoring"
dotnet restore "$ROOT/Shaker.SQLLiteDB.Activities.sln"

echo "==> Building ($CONFIGURATION)"
dotnet build "$ROOT/Shaker.SQLLiteDB.Activities.sln" -c "$CONFIGURATION" --no-restore

echo "==> Testing"
dotnet test "$ROOT/tests/Shaker.SQLLiteDB.Activities.Tests/Shaker.SQLLiteDB.Activities.Tests.csproj" \
    -c "$CONFIGURATION" --no-build

echo "==> Packing"
dotnet pack "$ROOT/src/Shaker.SQLLiteDB.Activities/Shaker.SQLLiteDB.Activities.csproj" \
    -c "$CONFIGURATION" --no-build

echo "==> Done. Packages:"
ls -1 "$ROOT/artifacts"
