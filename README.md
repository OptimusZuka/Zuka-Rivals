# Zuka Rivals

Overlay para iRacing (Windows 10/11) que mostra os pilotos da sessao ao vivo e guarda o historico de quem ja correste
contra: posicao, classe, iRating, licenca/SR, gap, melhor volta, pneu, tags e notas por piloto. 5 temas e cores
personalizaveis.

## Instalar
Descarrega `ZukaRivals-Setup-x.y.z.exe` em **Releases** e executa-o. Inclui o .NET, nao precisas de instalar mais nada.
Se o Windows avisar de "editor desconhecido", ver [`SECURITY.md`](SECURITY.md) (o instalador ainda nao esta assinado).

## Compilar
Precisas do .NET 10 SDK: `dotnet run` para testar, `dotnet publish -c Release -o publish` para gerar o `ZukaRivals.exe`.
O instalador (Inno Setup) e gerado pelo GitHub em `.github/workflows/instalador.yml`.

- Manual: [`LEIA-ME.txt`](LEIA-ME.txt)
- Revisao: `.github/workflows` (compilar, CodeQL, verificacao) e o agente `.claude/agents/revisor-zuka-rivals.md`

Sem rede, sem binarios no repositorio, historico so no teu computador.
