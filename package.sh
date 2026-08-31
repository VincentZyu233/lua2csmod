#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
configuration="Release"
runtime="linux-x64"
artifact_root="$project_root/artifacts"
publish_dir="$artifact_root/publish"
stage_dir="$artifact_root/stage"
plugin_dir="$stage_dir/addons/counterstrikesharp/plugins/Lua2CS"
archive="$artifact_root/Lua2CS-preview-linux-x64.zip"

restore_args=()
if [[ "${1:-}" == "--no-restore" ]]; then
    restore_args+=(--no-restore)
fi

rm -rf "$publish_dir" "$stage_dir" "$archive"
dotnet publish "$project_root/src/Lua2CS/Lua2CS.csproj" \
    --configuration "$configuration" \
    --runtime "$runtime" \
    --self-contained false \
    --output "$publish_dir" \
    "${restore_args[@]}"

mkdir -p "$plugin_dir/scripts" "$plugin_dir/examples"
cp "$publish_dir/Lua2CS.dll" "$publish_dir/Lua2CS.deps.json" "$plugin_dir/"
cp "$publish_dir/NLua.dll" "$publish_dir/KeraLua.dll" "$plugin_dir/"
cp "$publish_dir/liblua54.so" "$plugin_dir/"
cp -R "$project_root"/examples/. "$plugin_dir/examples/"

mkdir -p "$artifact_root"
(
    cd "$stage_dir"
    zip -qr "$archive" addons
)

echo "$archive"
