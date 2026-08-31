#!/usr/bin/env bash
set -euo pipefail

lua_version="5.4.8"
lua_sha256="4f18ddae154e793e46eeab727c59ef1c0c0c2b744e7b94219710d76f530629ae"
output_path="${1:?usage: build-lua54-linux.sh OUTPUT_PATH}"
cache_root="${LUA2CS_NATIVE_CACHE:-${TMPDIR:-/tmp}/lua2cs-native}"
archive_path="$cache_root/lua-$lua_version.tar.gz"
source_root="$cache_root/lua-$lua_version"
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

mkdir -p "$cache_root" "$(dirname "$output_path")"

if [[ ! -f "$archive_path" ]]; then
    curl --fail --location --retry 3 --output "$archive_path" \
        "https://www.lua.org/ftp/lua-$lua_version.tar.gz"
fi

actual_sha256="$(sha256sum "$archive_path" | awk '{print $1}')"
if [[ "$actual_sha256" != "$lua_sha256" ]]; then
    echo "Lua source checksum mismatch: expected $lua_sha256, got $actual_sha256" >&2
    exit 1
fi

if [[ ! -d "$source_root/src" ]]; then
    tar -xzf "$archive_path" -C "$cache_root"
fi

sources=()
for source_file in "$source_root"/src/*.c; do
    case "$(basename "$source_file")" in
        lua.c|luac.c) continue ;;
    esac
    sources+=("$source_file")
done

"${CC:-cc}" \
    -std=gnu17 -O2 -fPIC -DLUA_COMPAT_5_3 -DLUA_USE_LINUX \
    -shared -Wl,-Bsymbolic -Wl,--wrap=fmod -Wl,-soname,liblua54.so \
    -o "$output_path" "${sources[@]}" "$project_root/native/glibc-fmod-compat.c" -lm -ldl

if command -v readelf >/dev/null 2>&1 \
    && ! readelf -d "$output_path" | grep -q 'SYMBOLIC'; then
    echo "The private Lua library is missing the ELF SYMBOLIC flag." >&2
    exit 1
fi

if command -v objdump >/dev/null 2>&1; then
    while read -r required_version; do
        [[ -z "$required_version" ]] && continue
        newest="$(printf '%s\n%s\n' "$required_version" "2.35" | sort -V | tail -n 1)"
        if [[ "$newest" != "2.35" ]]; then
            echo "The private Lua library requires unsupported GLIBC_$required_version (maximum: GLIBC_2.35)." >&2
            exit 1
        fi
    done < <(objdump -T "$output_path" | grep -oE 'GLIBC_[0-9.]+' | cut -d_ -f2 | sort -Vu)
fi
