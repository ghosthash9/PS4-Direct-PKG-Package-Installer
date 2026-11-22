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
