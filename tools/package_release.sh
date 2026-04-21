#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT_NAME="Sts2DpsPrototype"
DIST_DIR="$ROOT_DIR/dist"
STAGE_DIR="$DIST_DIR/$PROJECT_NAME"
BUILD_DLL="$ROOT_DIR/.godot/mono/temp/bin/Debug/${PROJECT_NAME}.dll"
VERSION="${STS2_VERSION:-$(python3 - <<'PY' "$ROOT_DIR/mod_manifest.json"
import json, pathlib, sys
print(json.loads(pathlib.Path(sys.argv[1]).read_text())["version"])
PY
)}"
ZIP_PATH="$DIST_DIR/${PROJECT_NAME}-${VERSION}-multiplatform-dll-only.zip"

sync_mod_version() {
  local version="$1"
  python3 - <<'PY' "$ROOT_DIR" "$version"
import json, pathlib, re, sys
root = pathlib.Path(sys.argv[1])
version = sys.argv[2]

main = root / 'MainFile.cs'
text = main.read_text()
text, count = re.subn(r'(internal const string Version = ")([^"]+)(";)', rf'\g<1>{version}\g<3>', text, count=1)
if count != 1:
    raise SystemExit('Failed to update MainFile.cs version')
main.write_text(text)

for name in ('Sts2DpsPrototype.json', 'mod_manifest.json'):
    path = root / name
    data = json.loads(path.read_text())
    data['version'] = version
    data['has_pck'] = False
    data['has_dll'] = True
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n')
PY
}

mkdir -p "$DIST_DIR"
sync_mod_version "$VERSION"

pushd "$ROOT_DIR" >/dev/null
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet build ${PROJECT_NAME}.csproj
popd >/dev/null

rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR"

cp "$BUILD_DLL" "$STAGE_DIR/${PROJECT_NAME}.dll"
cp "$ROOT_DIR/${PROJECT_NAME}.json" "$STAGE_DIR/${PROJECT_NAME}.json"
cp "$ROOT_DIR/README-install.md" "$STAGE_DIR/README-install.md"

rm -f "$ZIP_PATH"
(
  cd "$DIST_DIR"
  zip -r -q "$ZIP_PATH" "$PROJECT_NAME"
)

echo "Prepared DLL-only cross-platform package for version: $VERSION"
echo "Created: $ZIP_PATH"
echo "Contents:"
find "$STAGE_DIR" -maxdepth 1 -type f | sort
