# Seguranca e antivirus

## O que o Zuka Rivals faz (e nao faz)
- Le a **memoria partilhada do iRacing** (so leitura) e o YAML da sessao.
- Guarda historico, tags e notas num ficheiro SQLite local em `%AppData%\iRacingTracker`.
- **Nao tem rede.** Nao liga a nenhum servidor, nao envia nem descarrega nada.
- **Nao tem `.exe` nem `.dll` no repositorio.** E codigo-fonte; compila-se localmente (`instalar.bat`).
- **Nao usa PowerShell escondido, tarefas agendadas, servicos nem hooks de teclado.**
- Unica persistencia: uma entrada opcional em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, ligada pela caixa
  "Arrancar com o Windows" nas Definicoes. Desmarcar remove-a.
- Atalhos globais: apenas `RegisterHotKey` (mostrar/ocultar e click-through), configuraveis.

## Porque pode haver alertas de antivirus
Programas novos e sem reputacao podem receber um aviso do SmartScreen ou um falso positivo heuristico. Para reduzir:
1. Distribuimos so codigo-fonte e scripts de texto, sem executaveis soltos.
2. Cada release tem a soma **SHA-256** em `SHA256.txt`.
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
