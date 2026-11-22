#!/bin/bash
# Gera Release/DirectPackageInstaller-x86_64.AppImage (autocontido, sem precisar do .NET instalado)
#
# Uso: coloque esta pasta (build-appimage.sh + icones/) NA RAIZ do projeto, isto e, ao lado da pasta
# DirectPackageInstaller/ e da pasta Files/, e rode:
#   chmod +x build-appimage.sh && ./build-appimage.sh
set -e

APP=DirectPackageInstaller
RID=linux-x64
ROOT="$(cd "$(dirname "$0")" && pwd)"
DESKTOP_PROJ="$ROOT/DirectPackageInstaller/DirectPackageInstaller.Desktop/DirectPackageInstaller.Desktop.csproj"
ICON_PNG="$ROOT/Files/Icon.png"
ICON_ICO="$ROOT/DirectPackageInstaller/DirectPackageInstaller/Assets/avalonia-logo.ico"
OUT="$ROOT/Release/appimage"
APPDIR="$OUT/$APP.AppDir"

command -v dotnet >/dev/null || { echo ".NET 8 SDK nao encontrado"; exit 1; }
[ -f "$DESKTOP_PROJ" ] || { echo "Projeto nao encontrado em: $DESKTOP_PROJ"; echo "Coloque este script na raiz do projeto."; exit 1; }
[ -d "$ROOT/Files" ] || { echo "Pasta Files/ nao encontrada em $ROOT"; exit 1; }

# 0) Aplica os icones da pasta icones/ (ao lado deste script) dentro do projeto, a cada build
if [ -f "$ROOT/icones/Icon.png" ]; then cp -f "$ROOT/icones/Icon.png" "$ICON_PNG"; else echo "AVISO: icones/Icon.png nao encontrado; usando o que ja esta em Files/Icon.png"; fi
if [ -f "$ROOT/icones/avalonia-logo.ico" ]; then cp -f "$ROOT/icones/avalonia-logo.ico" "$ICON_ICO"; else echo "AVISO: icones/avalonia-logo.ico nao encontrado; usando o do projeto"; fi
echo "Icone do AppImage: $ICON_PNG"
file "$ICON_PNG"        # 512x512 = raposa | 1024x1024 = seta antiga

rm -rf "$OUT"; mkdir -p "$APPDIR/usr/bin"

# 1) Publica autocontido (inclui o runtime do .NET dentro do AppImage)
#    Antes, apaga builds antigos (bin/obj Release) para recompilar e embutir o icone das janelas de novo
rm -rf "$ROOT/DirectPackageInstaller/DirectPackageInstaller/"{bin,obj}/Release "$ROOT/DirectPackageInstaller/DirectPackageInstaller.Desktop/"{bin,obj}/Release
dotnet restore "$DESKTOP_PROJ" -r $RID
dotnet publish "$DESKTOP_PROJ" -c Release -r $RID --self-contained true \
  -p:PublishSingleFile=false -o "$APPDIR/usr/bin"
chmod +x "$APPDIR/usr/bin/DirectPackageInstaller.Desktop"

# 2) Icone e atalho (Terminal=false = abre sem terminal)
cp -f "$ICON_PNG" "$APPDIR/$APP.png"
cat > "$APPDIR/$APP.desktop" <<DESK
[Desktop Entry]
Type=Application
Name=DirectPackageInstaller
Exec=DirectPackageInstaller.Desktop
Icon=$APP
Categories=Utility;
Terminal=false
DESK

# 3) AppRun (sem argumentos o programa abre a interface grafica)
cat > "$APPDIR/AppRun" <<'RUN'
#!/bin/bash
HERE="$(dirname "$(readlink -f "$0")")"
export DOTNET_BUNDLE_EXTRACT_BASE_DIR="${XDG_CACHE_HOME:-$HOME/.cache}/DirectPackageInstaller"
exec "$HERE/usr/bin/DirectPackageInstaller.Desktop" "$@"
RUN
chmod +x "$APPDIR/AppRun"

# 4) appimagetool
TOOL="$OUT/appimagetool-x86_64.AppImage"
if [ ! -f "$TOOL" ]; then
  curl -L -o "$TOOL" https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
  chmod +x "$TOOL"
fi

# Extrai o appimagetool e roda o AppRun dele (funciona mesmo sem FUSE)
(cd "$OUT" && rm -rf squashfs-root && "$TOOL" --appimage-extract >/dev/null)
ARCH=x86_64 "$OUT/squashfs-root/AppRun" "$APPDIR" "$ROOT/Release/$APP-x86_64.AppImage"

echo
echo "Pronto: $ROOT/Release/$APP-x86_64.AppImage"
echo "Conferir o icone embutido:  cd /tmp && $ROOT/Release/$APP-x86_64.AppImage --appimage-extract .DirIcon >/dev/null && file /tmp/squashfs-root/.DirIcon"
