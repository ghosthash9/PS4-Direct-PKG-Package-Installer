#!/bin/bash
# Limpa os arquivos de compilacao do DirectPackageInstaller.
# Uso (na raiz do projeto, ao lado da pasta DirectPackageInstaller/):
#   chmod +x clean-build.sh
#   ./clean-build.sh          -> apaga Release/ e as pastas bin/ e obj/ (pede confirmacao)
#   ./clean-build.sh -y       -> igual, sem perguntar
#   ./clean-build.sh --cache  -> alem disso, limpa miniaturas e cache de execucao do AppImage
set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
ASSUME_YES=0; CLEAN_CACHE=0
for a in "$@"; do
  case "$a" in
    -y|--yes)  ASSUME_YES=1 ;;
    --cache)   CLEAN_CACHE=1 ;;
    -h|--help) sed -n 2,7p "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Opcao desconhecida: $a (use --help)"; exit 1 ;;
  esac
done

# Seguranca: so roda dentro da raiz do projeto
if [ ! -d "$ROOT/DirectPackageInstaller" ] || [ ! -d "$ROOT/Files" ]; then
  echo "Esta nao parece ser a raiz do projeto (faltam DirectPackageInstaller/ e Files/)."
  echo "Coloque o script ao lado dessas pastas e rode de novo."
  exit 1
fi

# Lista o que sera apagado (nao entra em .git)
mapfile -t TARGETS < <(
  { [ -d "$ROOT/Release" ] && echo "$ROOT/Release"; 
    find "$ROOT" -path '*/.git' -prune -o -type d \( -name bin -o -name obj \) -prune -print; } | sort -u
)

if [ ${#TARGETS[@]} -eq 0 ]; then
  echo "Nada para limpar."
else
  echo "Pastas que serao apagadas:"
  for t in "${TARGETS[@]}"; do printf '  %8s  %s\n' "$(du -sh "$t" 2>/dev/null | cut -f1)" "${t#$ROOT/}"; done
  echo

  if [ $ASSUME_YES -ne 1 ]; then
    read -r -p "Apagar tudo isso? [s/N] " resp
    case "$resp" in s|S|y|Y) ;; *) echo "Cancelado."; exit 0 ;; esac
  fi

  for t in "${TARGETS[@]}"; do rm -rf -- "$t"; done
  echo "Compilacao limpa (${#TARGETS[@]} pastas apagadas)."
fi

if [ $CLEAN_CACHE -eq 1 ]; then
  rm -rf "$HOME/.cache/thumbnails" "$HOME/.cache/DirectPackageInstaller"
  echo "Cache de miniaturas e de execucao limpos."
fi

echo "Para compilar de novo: ./build-appimage.sh"
