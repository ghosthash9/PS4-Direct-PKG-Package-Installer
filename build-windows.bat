@echo off
REM Compila o DirectPackageInstaller para Windows (x64), autocontido (nao precisa instalar o .NET)
REM Uso: coloque esta pasta (build-windows.bat + icones\) NA RAIZ do projeto (ao lado de DirectPackageInstaller\ e Files\) e de dois cliques.
REM Requer: .NET 8 SDK  ->  https://dotnet.microsoft.com/download/dotnet/8.0

set PROJ=DirectPackageInstaller\DirectPackageInstaller.Desktop\DirectPackageInstaller.Desktop.csproj
set OUT=Release\Windows-X64

if not exist %PROJ% (
  echo Projeto nao encontrado. Coloque este .bat na raiz do projeto.
  pause
  exit /b 1
)

dotnet --list-sdks | find "8." >nul
if errorlevel 1 (
  echo .NET 8 SDK nao encontrado.
  pause
  exit /b 1
)

REM 0) Aplica os icones da pasta icones\ dentro do projeto
if exist icones\Icon.png copy /y icones\Icon.png Files\Icon.png >nul
if exist icones\avalonia-logo.ico copy /y icones\avalonia-logo.ico DirectPackageInstaller\DirectPackageInstaller\Assets\avalonia-logo.ico >nul

REM Define o icone do .exe no csproj (so se ainda nao existir)
powershell -NoProfile -Command "$p='%PROJ%'; $t=[IO.File]::ReadAllText($p); if($t -notmatch 'ApplicationIcon'){ $t=$t -replace '(<OutputType>WinExe</OutputType>)','$1<ApplicationIcon>../DirectPackageInstaller/Assets/avalonia-logo.ico</ApplicationIcon>'; [IO.File]::WriteAllText($p,$t); Write-Host 'ApplicationIcon adicionado ao csproj' }"

if exist Release\Windows-X64 rmdir /s /q Release\Windows-X64

REM Apaga builds antigos para recompilar e embutir os icones de novo
for %%D in (DirectPackageInstaller\DirectPackageInstaller DirectPackageInstaller\DirectPackageInstaller.Desktop) do (
  if exist %%D\bin\Release rmdir /s /q %%D\bin\Release
  if exist %%D\obj\Release rmdir /s /q %%D\obj\Release
)

dotnet restore %PROJ% -r win-x64
dotnet publish %PROJ% -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o %OUT%
if errorlevel 1 (
  echo Falhou a compilacao.
  pause
  exit /b 1
)

echo.
echo Pronto! Executavel: %OUT%\DirectPackageInstaller.Desktop.exe
pause
