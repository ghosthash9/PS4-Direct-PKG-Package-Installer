# DirectPackageInstaller

A tool to preview and send PKG files to your PS4 or PS5 using direct download links.  
This tool supports three different methods for sending PKGs:

-   Remote Package Installer Homebrew
-   GoldHEN Payload Server    
-   etaHEN DPIv2 API
    

Features
----------

-   Preview PKG files
-   Standalone PS4 package downloads
-   PS5 etaHEN support
-   Automatic proxying to speed up PS4 downloads
-   Support for RAR/7z compressed files
-   Resumable downloads (only for uncompressed files)
-   Segmented downloads
-   Support for several file hosting services
-   Support for PSN update manifests (split PKGs)
-   Works with or without Remote Package Installer
-   Command Line Interface (CLI)
-   JDownloader Click'n Load support
-   Auto LAN Cable Connection (Windows Only)
-   Can Install PKGs from NFS source
-   **Server browser** (Server menu): lists the PKGs of your own file server *(this version, see the guide below)*
    

Supported File Hosting Services
----------

-   Any direct link (without authentication)
-   AllDebrid ([API key required](https://alldebrid.com/apikeys/))
-   RealDebrid ([API key required](https://real-debrid.com/apitoken))
-   DebridLink ([API key required](https://debrid-link.com/webapp/apikey))
-   Google Drive
-   Mediafire
-   PixelDrain
-   1Fichier (must wait 60 minutes after each download)

---

# Guia desta versão (PT-BR)

As seções abaixo documentam a instalação das dependências, a compilação (`.exe` e AppImage), as melhorias feitas, como adicionar os links e o cache em disco. O restante do README (em inglês) é o original do projeto.

**Índice**

1. [Instalar as dependências](#1-instalar-as-dependências)
2. [Compilar: .exe (Windows) e AppImage (Linux)](#2-compilar-exe-windows-e-appimage-linux)
3. [Melhorias desta versão](#3-melhorias-desta-versão)
4. [Como adicionar os links dos PKGs](#4-como-adicionar-os-links-dos-pkgs)
5. [Cache e espaço em disco](#5-cache-e-espaço-em-disco)
6. [Servidor de arquivos próprio (local e externo)](#6-servidor-de-arquivos-próprio-local-e-externo)
7. [Problemas comuns](#7-problemas-comuns)

---

## 1. Instalar as dependências

Só é preciso instalar algo para **compilar**. Os programas gerados pelos scripts (`.exe` e AppImage) são **autocontidos**: levam o runtime do .NET junto e rodam em máquinas sem o .NET instalado.

### Linux (Linux Mint / Ubuntu e derivados)

```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0 curl file
dotnet --list-sdks          # deve listar uma versão 8.0.x
```

| Pacote | Para quê |
|---|---|
| `dotnet-sdk-8.0` | compilar o projeto (obrigatório) |
| `curl` | o script baixa o `appimagetool` do GitHub na primeira vez |
| `file` | o script mostra qual ícone está sendo usado |
| `libfuse2` | **executar** AppImages (no Ubuntu/Mint 24.04+ o nome é `libfuse2t64`) |
| `libfontconfig1 libice6 libsm6` | só se o programa reclamar de biblioteca gráfica ao abrir |

```bash
sudo apt install -y libfuse2
```

### Windows 10/11 (x64)

Instale o **.NET 8 SDK**, por um destes caminhos:

- instalador do SDK x64 em <https://dotnet.microsoft.com/download/dotnet/8.0>
- ou, no Prompt de Comando: `winget install Microsoft.DotNet.SDK.8`

Confirme em um **novo** Prompt de Comando: `dotnet --list-sdks`. O PowerShell já vem com o Windows e é usado pelo `build-windows.bat`.

### Rede e firewall

- O primeiro build baixa os pacotes NuGet (precisa de internet). O script do AppImage também baixa o `appimagetool` de `github.com`.
- Nos modos **Proxy** e **Segmentado** o programa serve o PKG ao PS4 pela porta **9898** do PC: libere-a no firewall se necessário. Configure também o **PC IP** e o **PS IP** em *Options*.

### Opcional (servidor de arquivos próprio)

Python 3.8+ (para `servidor_games.py` e `gerar_manifest.py`) e o [Tailscale](https://tailscale.com/) (para acessar de fora de casa). Veja a [seção 6](#6-servidor-de-arquivos-próprio-local-e-externo).

---

## 2. Compilar: .exe (Windows) e AppImage (Linux)

### Estrutura de pastas esperada

Os scripts ficam na **raiz do projeto**, ao lado de `DirectPackageInstaller/` e `Files/`:

```
DirectPackage/
├── DirectPackageInstaller/      projeto principal (Desktop, library, ...)
├── DHCPServer/  HttpServerLite/  LibOrbisPkg/
├── Files/                       Icon.png (ícone do AppImage)
├── icones/                      Icon.png e avalonia-logo.ico (ícones aplicados a cada build)
├── build-appimage.sh            gera o AppImage (Linux)
├── build-windows.bat            gera o .exe (Windows)
├── clean-build.sh               limpa a compilação
└── Build.cmd                    script original (todas as plataformas)
```

Se um script não estiver na raiz certa, ele avisa e para.

### AppImage (Linux)

```bash
chmod +x build-appimage.sh
./build-appimage.sh
```

Resultado: `Release/DirectPackageInstaller-x86_64.AppImage`. Para abrir:

```bash
chmod +x Release/DirectPackageInstaller-x86_64.AppImage
```

Depois é só dar dois cliques (ou "Permitir execução do arquivo como programa" nas propriedades). Se não abrir, rode uma vez pelo terminal para ver o erro.

O script: aplica os ícones de `icones/`, apaga `bin/Release` e `obj/Release`, restaura os pacotes, publica autocontido (`linux-x64`), monta o AppDir (`Terminal=false`, abre sem terminal) e gera o AppImage com o `appimagetool`.

> O `appimagetool-x86_64.AppImage` é só a ferramenta que **monta** o AppImage. Rodá-lo sozinho mostra `ERROR: SOURCE is missing` — é normal. O programa é o arquivo gerado em `Release/`.

### .exe (Windows)

Dê dois cliques em `build-windows.bat` (ou rode no Prompt de Comando, dentro da pasta).

Resultado: `Release\Windows-X64\DirectPackageInstaller.Desktop.exe`. Para distribuir, copie a **pasta inteira** `Windows-X64` (as bibliotecas ficam ao lado do `.exe`).

O script: aplica os ícones, acrescenta o `ApplicationIcon` ao `.csproj` se ainda não existir, apaga builds antigos, restaura e publica autocontido (`win-x64`).

> Compilar o `.exe` a partir do Linux (`dotnet publish ... -r win-x64 --self-contained true`) deve funcionar em tese, mas **não foi testado**.

### Limpar a compilação

```bash
./clean-build.sh            # lista as pastas e pergunta antes de apagar
./clean-build.sh -y         # apaga sem perguntar
./clean-build.sh --cache    # também limpa miniaturas e cache de execução do AppImage
```

Apaga `Release/` e as pastas `bin/` e `obj/` (menos dentro de `.git`).

### Observações

- Compile sempre o projeto **`DirectPackageInstaller.Desktop`**, nunca a solução `.sln`: ela inclui Android/iOS e exige *workloads* que quebram o build.
- O primeiro build é mais demorado (baixa os pacotes).
- Para payload customizado, rode `make` na pasta do payload e substitua o `payload.bin` em `DirectPackageInstaller/DirectPackageInstaller/payload`.

---

## 3. Melhorias desta versão

- **Envio de vários PKGs sem reiniciar.** Corrigido o erro BGFT `0x80990086` que aparecia depois de ~3 pacotes e obrigava a desativar a internet do PC ou reiniciar o programa a cada envio (correção no payload, `Payload/main.c`).
- **Navegador de servidor (menu *Server*).** Lista os `.pkg` do seu próprio servidor de arquivos, com pastas e tamanhos, e carrega o escolhido no fluxo normal (preview → Install). Detalhes na [seção 4](#lista-de-pkgs-de-um-servidor-menu-server).
  - Entende `manifest.json` (`[{"p": caminho, "s": tamanho}]`), JSON genérico e listagens HTML (autoindex).
  - O endereço do servidor fica salvo no `Settings.ini` (chave `ServerURL`).
- **Ícone novo** (raposa) no AppImage, nas janelas e no `.exe`.
- **Scripts de build e limpeza** (`build-appimage.sh`, `build-windows.bat`, `clean-build.sh`) com builds autocontidos.
- **Scripts de servidor** (fora do projeto C#): `servidor_games.py` (serve a pasta com `index.html`, `manifest.json` automático e Range) e `gerar_manifest.py` (gera o `manifest.json`).

**Limitações conhecidas:** o *Install All* não funciona para listas de servidor (a fila automática ainda não foi implementada: escolha um pacote por vez); não há senha/token no servidor; a lista do servidor foi testada com servidores locais, não com todos os formatos possíveis.

---

## 4. Como adicionar os links dos PKGs

### Regras gerais

- O link precisa ser **direto** para o `.pkg`: sem login, captcha, contagem regressiva ou clique em anúncios.
- O servidor deve aceitar **Range** (`206 Partial Content`). Sem isso o preview e o download segmentado falham. Teste:

  ```bash
  curl -sI -H "Range: bytes=0-0" "LINK_DO_PKG"     # deve responder 206
  ```
- Espaços e colchetes costumam funcionar no campo URL. Se algum link falhar, troque por `%20`, `%5B` e `%5D`.
- Sites com captcha/espera (que abrem outras abas ou anúncios) **não** são lidos pelo programa. Use um serviço debrid, se ele suportar o site, ou o link final direto (se for estável).

### Qual formato usar

| Origem | Exemplo de link | Observações |
|---|---|---|
| **Arquivo local** | *(campo URL vazio)* → **Load** → escolher o `.pkg` | Também dá para arrastar o arquivo para a janela. Não usa cache. |
| **Servidor próprio na LAN** | `http://IP-DO-PC:8082/files/Pasta/arquivo.pkg` | Mais rápido. O PS4 pode baixar direto (modo Direct). |
| **Servidor próprio externo (Tailscale Funnel)** | `https://SEU-PC.SUA-TAILNET.ts.net:10000/Pasta/arquivo.pkg` | Quem baixa é o PC (Proxy/Segmentado). Funnel só aceita as portas 443, 8443 e 10000. |
| **Tailscale Serve (só dentro da tailnet)** | `https://SEU-PC.SUA-TAILNET.ts.net:PORTA/Pasta/arquivo.pkg` | O PS4 não está na tailnet: use Proxy ou Segmentado (o PC baixa). |
| **NFS** | `nfs://IP/Exportado/Pasta/arquivo.pkg` | Ver *NFS PKG Source* abaixo. |
| **Hospedagens suportadas** | Mediafire, Google Drive, PixelDrain, 1Fichier, debrid | Veja a lista no topo. |

### Passo a passo (link único)

1. Abra o **PS4 com o Remote Package Installer / payload do GoldHEN** ligado e configure o **PS IP** e o **PC IP** em *Options*.
2. Cole o link no campo **URL** e clique em **Load** (aguarde o preview: ícone, título, `CONTENT_ID`).
3. Clique em **Install**.

### Lista de PKGs de um servidor (menu *Server*)

1. **Server → Address**: cole o endereço do servidor (ex.: `https://SEU-PC.SUA-TAILNET.ts.net:10000/`). Fica salvo no `Settings.ini`.
2. **Server → Load Package List**: o programa lê o servidor e mostra o menu **Packages** com itens no formato `pasta/arquivo.pkg (tamanho)`. Se houver só um pacote, o link é colocado no campo URL.
3. Clique em um pacote (o preview carrega) e depois em **Install**.

O que o leitor entende:

| Formato do servidor | Como é lido |
|---|---|
| `manifest.json` ao lado da página (ou uma pasta acima) | `[{"p": "Pasta/arquivo.pkg", "s": 123}, ...]`; pastas terminam com `/`. Usado quando a página é montada por JavaScript. |
| JSON genérico | Lista de objetos/strings com `path`, `url`, `href`, `file`, `name`, `p`; tamanho em `size`, `bytes`, `length` ou `s`; pastas aninhadas (`files`, `children`, `items`). Caminhos relativos devem estar **sem** percent-encoding (o programa codifica). Use `url` absoluta se os downloads ficam em outro prefixo. |
| Listagem HTML (autoindex do nginx/Apache/Python/Tailscale) | Segue os links `<a href>` (mesmo servidor, até 4 níveis e 300 requisições) e lista os `.pkg`. |

### Modo de envio (qual usar)

O programa escolhe o modo pelas opções, nesta ordem de prioridade: **Segmented → Proxy → Direct**. Sem `Settings.ini`, o padrão no desktop é **Segmented e Proxy ligados** (vale o Segmented).

| Modo | Quem baixa | Cache em disco no PC | Quando usar |
|---|---|---|---|
| **Direct** (Segmented e Proxy desligados) | O **PS4** | Não | O PS4 alcança o link (ex.: `http://` na LAN). Dá para fechar o programa/desligar o PC depois de começar. |
| **Proxy** | O PC repassa os dados (stream) | **Não** | Links `https`, da tailnet ou que o PS4 não alcança. O programa precisa ficar aberto. |
| **Segmented** | O PC baixa em paralelo para o disco e envia | **Sim: o PKG inteiro** | Hospedagens lentas. O PS4 não mostra o progresso real. |
| **Disk cache** (hospedagens limitadas) | O PC baixa tudo antes | **Sim: o PKG inteiro** | Escolhido automaticamente; o Segmented é ignorado. |
| **RAR/7z** | O PC baixa e descompacta | **Sim** (≈ tamanho do PKG final) | Sempre via proxy. |
| **Arquivo local** | O PC serve do disco | Não | PKG já está no PC. |

Se o programa perguntar *"The given URL can't be direct downloaded. Do you want the DirectPackageInstaller to act as a server?"*, o link exige cabeçalhos/cookies: responda **Sim** (modo Proxy).

---

## 5. Cache e espaço em disco

- **Onde fica:** pasta `Temp/` dentro da **pasta de trabalho** do programa (a pasta de onde o AppImage/`.exe` é aberto, ou a definida na variável de ambiente `CD`; no macOS é a pasta `Application Support/DirectPackageInstaller`). O `Settings.ini` fica na mesma pasta.
- **Quanto é preciso:** o **tamanho do PKG inteiro** livre no disco dessa pasta, nos modos Segmented, Disk cache e RAR/7z. Ex.: um PKG de 84,3 GB exige 84,3 GB livres.
- **Aviso quando falta espaço:** `X GB in your internal storage is required, missing Y GB currently.` Isso se refere ao disco do **PC**, não ao do PS4.
- **Limpeza:** se o cache não está vazio e não há conexões abertas, o programa apaga os temporários e tenta de novo sozinho. Caso contrário, apague a pasta `Temp/` manualmente.
- **Sem cache em disco:** use **Proxy** (desligue o Segmented), **Direct** ou **abra o arquivo local**.
- **Espaço no PS4:** o programa **não** confere o espaço livre do PS4. Para instalar um PKG de 84 GB, o PS4 precisa de pelo menos esse tamanho livre.

Dica: se o servidor de arquivos está no mesmo PC onde o programa roda, abra o `.pkg` direto do disco (campo URL vazio → **Load**) em vez de baixar o link pela rede.

---

## 6. Servidor de arquivos próprio (local e externo)

### Opção A: o Tailscale serve a pasta (Funnel)

O Funnel pode servir um diretório direto. Se a pasta tiver um `index.html`, o servidor mostra a página em vez da listagem; por isso a página lê um `manifest.json`, que o `gerar_manifest.py` cria.

```bash
python3 gerar_manifest.py /caminho/da/pasta-de-jogos        # gera manifest.json
sudo tailscale funnel --bg --https=10000 /caminho/da/pasta-de-jogos
tailscale funnel status
```

Para desligar: `sudo tailscale funnel --https=10000 off` (não use `tailscale funnel reset` se houver outras portas em uso: ele apaga todas).

Atualização automática do manifest (a cada 5 minutos), em `crontab -e`:

```
*/5 * * * * /usr/bin/python3 /caminho/gerar_manifest.py /caminho/da/pasta-de-jogos
```

Endereço no programa: a **raiz** (`https://SEU-PC.SUA-TAILNET.ts.net:10000/`). Teste o Range (deve responder `206`); se responder `200`, use a opção B.

### Opção B: `servidor_games.py`

```bash
python3 servidor_games.py --root /caminho/da/pasta-de-jogos --port 8082
```

| Rota | O que entrega |
|---|---|
| `/files/` | o `index.html` da pasta |
| `/files/manifest.json` | gerado automaticamente (`[{"p","s"}]`, cache de 20 s) |
| `/files/<caminho>` | o arquivo, com Range/206 e HEAD |
| `/files/<pasta>/` | listagem HTML simples |

Local: `http://IP-DO-PC:8082/files/`. Externo: `sudo tailscale funnel --bg --https=10000 http://127.0.0.1:8082`.

### Segurança

- Com Funnel, **qualquer pessoa com a URL** acessa os arquivos. Não há senha.
- Tudo que está dentro da pasta servida fica acessível por URL, inclusive scripts (`.py`) e `payload.bin`, mesmo escondidos da lista. Mantenha-os fora da pasta publicada.

---

## 7. Problemas comuns

| Mensagem / sintoma | Causa | O que fazer |
|---|---|---|
| `X GB in your internal storage is required, missing Y GB` | Modo Segmented/Disk cache grava o PKG inteiro no disco do PC | Libere espaço, abra o programa em outro disco, ou desligue **Segmented** e use **Proxy** / arquivo local |
| `No .pkg file was found at this address` | O servidor respondeu, mas o formato não foi reconhecido (ex.: botões montados por JavaScript, sem `manifest.json`) | Gere o `manifest.json` (`gerar_manifest.py`) ou use `servidor_games.py` |
| `Failed to load the server list: ...` | Timeout, erro 404/500 ou certificado | Confira o endereço e abra-o no navegador |
| Lista aparece, mas o preview/download falha | O servidor não suporta Range | Teste com `curl -sI -H "Range: bytes=0-0" LINK`; use `servidor_games.py` |
| `PS IP not defined` / `PC IP not defined` | IPs não configurados | Preencha em *Options* |
| O PS4 não consegue baixar | Link inalcançável pelo PS4 (https, tailnet) ou porta 9898 bloqueada | Use Proxy/Segmented e libere a porta 9898 |
| `ERROR: SOURCE is missing` | Você abriu o `appimagetool` sozinho | Rode `./build-appimage.sh` |
| Ícone antigo no gerenciador de arquivos | Cache de miniaturas | `./clean-build.sh --cache` e recompile |
| Erro de *workload* (Android/iOS) no build | Compilou a `.sln` | Compile o projeto `DirectPackageInstaller.Desktop` (use os scripts) |
| Mensagem de falha no instalador/crash | — | Veja o `DPI-Crash.log` |

---

# Original documentation (English)

How to Install
----------

1.  Download a build compatible with your platform (see below).
2.  On Windows, Linux, and Android, install the [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) first.
    macOS builds are self-contained and do not require a separate .NET install.

    *Builds produced with the scripts of this version (`build-appimage.sh`, `build-windows.bat`) are self-contained too: no runtime install needed.*
    
Downloads:
----------

<details> <summary>Windows</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-X64.zip)    
-   [x86](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-X86.zip)    

</details> <details> <summary>Android</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-X64.zip)    
-   [x86](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-X86.zip)    

</details> <details> <summary>Linux</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-X64.zip)    

</details> <details> <summary>macOS</summary>

**CLI builds:**

-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-X64.zip)    

**App builds (zip):**

-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-ARM64-APP.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-X64-APP.zip)   

**App builds (DMG, drag-to-Applications):**

-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-ARM64-APP.dmg)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-X64-APP.dmg)

macOS builds are self-contained (.NET 8 runtime bundled) and the .app is ad-hoc signed so it launches on Apple Silicon without further steps. If Gatekeeper still blocks the app after extracting the zip, run:

```
xattr -dr com.apple.quarantine /Applications/DirectPackageInstaller.app
```

</details>


How to Use
----------

#### Direct Download Mode

1.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
2.  Open DirectPackageInstaller.    
3.  Paste a direct PKG URL into the "PKG URL" field.    
4.  Click "Load" and wait.    
5.  Click "Install" and wait again.    
6.  Done! You can close the application or shut down your PC.    

#### Proxied Download Mode (for PS4s without internet access)

1.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
2.  Open DirectPackageInstaller.    
3.  Go to `Options -> Proxy Downloads` and enable it.    
4.  Paste a direct PKG URL.    
5.  Click "Load", then "Install".    
6.  Wait for the installation to finish.
    

#### Segmented Download Mode (fastest)

1.  Make sure you have enough free disk space.
2.  Launch Remote Package Installer or enable GoldHEN Payload Server.
3.  Open DirectPackageInstaller.
4.  Go to `Options -> Segmented Downloads` and enable it.
5.  Paste a direct PKG URL.
6.  Click "Load", then "Install".
7.  The PKG will be downloaded and preloaded before installation.

> Note: In segmented mode, the PS4 cannot track download progress accurately.

#### Compressed File Mode (always proxied)
1.  Make sure you have enough free disk space.    
2.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
3.  Paste the direct URL to your compressed file.    
4.  Click "Load" and wait.    
5.  [Optional] Select the desired PKG from the file list.    
6.  Click "Install" and wait.

macOS CLI Usage
----------

Use the terminal to run the app:
```
./DirectPackageInstaller.Desktop
```
In some cases, you may need to run:
```
dotnet DirectPackageInstaller.Desktop.dll
```

NFS PKG Source
----------

To use an NFS server as a source for DPI, you just need to provide a URL in a format supported by DPI.
There are two supported NFS URL formats:

- `nfs://YOUR_NFS_IP_OR_HOSTNAME/ExportedPath/FilePath/FileName.pkg`

- `nfs://YOUR_NFS_IP_OR_HOSTNAME/FilePath/FileName.pkg`

In the first format, the path starts with the exact exported directory (as configured in the NFS server's /etc/exports).
DPI will mount that specific export and look for the file within it.

In the second format, no specific export path is provided. In this case, DPI will attempt to search across all available exports on the NFS server to locate the requested file.
Note that this may slow down the initial access if your server has many exports.

PS: The DPI is incompatible with the winnfsd due this bug [#88](https://github.com/winnfsd/winnfsd/issues/88), for windows, you can use [haneWIN NFS Server](https://www.hanewin.net/nfs-e.htm) instead.


How It Works
----------
DirectPackageInstaller is a simple tool that uses the Remote Package Installer's API to add PKG downloads directly to your PS4 or PS5. It helps you preview and send packages easily.

Additionally, the tool features an automatic proxy server to accelerate downloads. Since the PS4 has much better speeds over LAN than WAN, proxying through your PC can significantly improve performance.

It also works without the Remote Package Installer if you enable the binloader — the tool will automatically decide which method to use.

When using compressed files (RAR/7z), everything happens in parallel: download, extraction, and upload to your console. This saves time and disk space compared to traditional methods.

Segmented mode downloads PKGs in parallel threads to your PC, while sending them to the console. It's the fastest option, though it won't show progress on the PS4 screen.


Command Line Interface (CLI)
----------

An experimental CLI is available.

Note: RAR/7z and segmented downloads are not supported in CLI mode (yet).  
To view available options:

-   On Windows: `DirectPackageInstaller.Desktop.exe -help`    
-   On other systems: `DirectPackageInstaller.Desktop -help`
    



Dependencies
----------
Windows, Linux, and Android builds downloaded from the original project require the [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime).
macOS builds are self-contained and ship with the runtime bundled.

To **build** this project you need the .NET 8.x SDK. See [Instalar as dependências](#1-instalar-as-dependências) for the full list (Linux and Windows).


How to Build
----------

1.  Install the .NET 8.x SDK.    
2.  Run the build script for your platform from the project root:
    -   Linux (AppImage): `./build-appimage.sh` → `Release/DirectPackageInstaller-x86_64.AppImage`
    -   Windows (.exe): `build-windows.bat` → `Release\Windows-X64\DirectPackageInstaller.Desktop.exe`
    -   Clean the build: `./clean-build.sh`
    -   Or use the original `Build.cmd` script (works on both Windows and Unix-like systems), which creates a `Release` directory with the compiled binaries.

Details of each script: [Compilar](#2-compilar-exe-windows-e-appimage-linux).

For custom payloads, run `make` in the payload directory and replace the generated `payload.bin` inside `DirectPackageInstaller/DirectPackageInstaller/payload`.

Android builds:
----------
-   Targets API level 26    
-   Install Android SDK and NDK    
-   Accept SDK licenses    
-   Edit `Build.cmd` to set SDK/NDK paths    
-   Run `Build.cmd` as admin (first time only)

Alternatively, fork the project and use GitHub Actions for builds.

Q&A
----------

**My download is taking too long to start**  
You're probably downloading a multipart compressed file. The more volumes, the longer it takes to scan and validate. This isn’t a bug — just how compressed file access works. Please be patient.

**Download speed is slow, even in segmented mode**  
Segmented mode doesn't report accurate progress to the PS4. Use your PC's Task Manager to monitor real network usage.

**Segmented mode and only 2 Mbps?!**  
Some hosting sites (e.g., Zippyshare) are just slow. Try testing with Google Drive for better performance.

**Recommended file hostings?**

-   AllDebrid / RealDebrid    
-   Google Drive    
-   Mediafire
-   Everything else
-   1Fichier (slowest)
    

**I got an error during installation**  
Open an issue! Seriously, don’t worry — mistakes happen. If there's a crash, check the `DPI-Crash.log` for details.

Disclaimer
----------

This tool is for legitimate purposes only. It’s intended to help users install their personal backups. Piracy is not supported.  
You are responsible for how you use this software.

Warnings
----------

-   Avoid multiple parallel decompressions — process one file at a time.
-   Compressed file downloads create a temporary PKG cache, requiring free disk space equal to the final PKG size.
-   Segmented downloads (enabled by default on desktop) also cache the whole PKG on the PC: keep free space equal to the PKG size in the working directory drive. See [Cache e espaço em disco](#5-cache-e-espaço-em-disco).

Credits
----------

-   **LibOrbisPkg** by _maxton_
-   **HttpServerLite** by _jchristn_
-   Payload template by _sleirsgoevy_
-   PS4 OS internals help by _LM_
-   PS4 export definitions by _OpenOrbis SDK_
-   **DirectPackageInstaller** by _marcussacana_
