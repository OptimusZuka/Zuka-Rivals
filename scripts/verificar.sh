#!/usr/bin/env bash
# Verificacao rapida de padroes que costumam causar alertas de antivirus. Falha se encontrar algo grave.
cd "$(dirname "$0")/.."
fail=0
check() { # descricao, padrao, ficheiros
  out=$(grep -rnE "$2" --include='*.cs' --include='*.xaml' --include='*.bat' --include='*.vbs' --include='*.csproj' . 2>/dev/null | grep -v '^./scripts/' | grep -v '^./Strings.cs' || true)
  if [ -n "$out" ]; then echo "== $1"; echo "$out"; echo; fail=1; fi
}
echo "Zuka Rivals - verificacao de seguranca"
check "Rede (nao devia existir)"            'HttpClient|WebClient|System\.Net\.Sockets|TcpClient|UdpClient|WebRequest|DownloadFile|DownloadString'
check "Codigo dinamico"                     'Assembly\.Load|Reflection\.Emit|CSharpScript|Activator\.CreateInstanceFrom'
check "Hooks de teclado/rato"               'SetWindowsHookEx|GetAsyncKeyState|GetKeyboardState|keybd_event|SendInput'
check "PowerShell escondido"                'WindowStyle[ =]+Hidden|-w(indowstyle)? +hidden|-enc(odedcommand)?'
check "mshta/rundll32/regsvr32/certutil"    'mshta|rundll32|regsvr32|certutil|bitsadmin'
check "Tarefas agendadas/servicos"          'schtasks|sc\.exe +create|New-Service|ServiceController'
# DllImport: so as bibliotecas esperadas
bad=$(grep -rhoE 'DllImport\("[^"]+"' --include='*.cs' . | sort -u | grep -vE 'user32|dwmapi' || true)
# Sqlite usa a constante Lib; confirmar o valor
lib=$(grep -rhoE 'Lib *= *"[^"]+"' Sqlite.cs || true)
echo "DllImport inesperados: ${bad:-nenhum}"; echo "Biblioteca SQLite: $lib"
case "$lib" in *winsqlite3*) ;; *) echo "ATENCAO: biblioteca SQLite diferente de winsqlite3"; fail=1;; esac
# binarios no repositorio
bin=$(find . -type f \( -name '*.exe' -o -name '*.dll' -o -name '*.bin' -o -name '*.msi' \) -not -path './bin/*' -not -path './obj/*' -not -path './.git/*' || true)
if [ -n "$bin" ]; then echo "== Binarios no repositorio:"; echo "$bin"; fail=1; fi
# caminhos pessoais
check "Caminhos pessoais"                   'C:\\\\Users\\\\[A-Za-z]'
[ $fail -eq 0 ] && echo "OK: nada suspeito encontrado." || echo "Ha achados acima."
exit $fail
