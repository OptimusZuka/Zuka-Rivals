# Seguranca e antivirus

## O que o Zuka Rivals faz (e nao faz)
- Le a **memoria partilhada do iRacing** (so leitura) e o YAML da sessao.
- Guarda historico, tags e notas num ficheiro SQLite local em `%AppData%\iRacingTracker`.
- **Nao tem rede.** Nao liga a nenhum servidor, nao envia nem descarrega nada.
- **Nao tem `.exe` nem `.dll` no repositorio.** E codigo-fonte; o `ZukaRivals.exe` e o instalador sao compilados pelo GitHub Actions a partir deste codigo (`.github/workflows/instalador.yml`).
- **Nao usa PowerShell escondido, tarefas agendadas, servicos nem hooks de teclado.**
- Unica persistencia: uma entrada opcional em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, ligada pela caixa
  "Arrancar com o Windows" nas Definicoes. Desmarcar remove-a.
- Atalhos globais: apenas `RegisterHotKey` (mostrar/ocultar e click-through), configuraveis.

## Porque pode haver alertas de antivirus
Programas novos e sem reputacao podem receber um aviso do SmartScreen ou um falso positivo heuristico. Para reduzir:
1. O executavel e o instalador sao gerados em publico pelo GitHub a partir do codigo-fonte, sem ofuscacao, sem scripts `.bat`, PowerShell nem VBS.
2. Cada release tem a soma **SHA-256** em `SHA256.txt`, e o fluxo corre uma analise do Windows Defender ao instalador.
3. Antes de descarregar, podes enviar o ZIP ao [VirusTotal](https://www.virustotal.com) e comparar.
4. Podes ler todo o codigo: nada esta ofuscado.

## Falso positivo
Se o teu antivirus marcar o programa, abre uma issue com o nome do antivirus e da detecao. O autor reporta-o ao
fornecedor (Microsoft Defender: https://www.microsoft.com/wdsi/filesubmission).

## Verificacao automatica
`scripts/verificar.sh` procura padroes que costumam disparar antivirus (rede, codigo dinamico, hooks, PowerShell
escondido, binarios) e corre em cada push. O agente `.claude/agents/revisor-zuka-rivals.md` faz uma revisao mais funda.

## Reportar uma vulnerabilidade
Abre uma issue privada (Security > Report a vulnerability) ou contacta o autor.

## Assinatura digital (SmartScreen)
O instalador **nao esta assinado**. Sem assinatura, o SmartScreen mostra "editor desconhecido" e, em PCs com o Controlo
Inteligente de Aplicacoes ligado, o Windows pode bloquear o `.exe` sem opcao de executar mesmo assim. Opcoes:
- **Microsoft Store (pacote MSIX):** a Microsoft assina e nao ha alerta; registo gratuito para particulares, mas o
  programa passa por certificacao e regras da loja.
- **Certificado de assinatura (OV):** cerca de 150-300 EUR por ano. Mesmo assinado, o aviso so desaparece quando o
  editor ganha reputacao. O fluxo ja suporta assinar: guarda `SIGN_PFX_BASE64` e `SIGN_PFX_PASSWORD` como segredos.
- **Azure Artifact Signing:** cerca de 10 USD/mes, mas so para particulares nos EUA e Canada.
