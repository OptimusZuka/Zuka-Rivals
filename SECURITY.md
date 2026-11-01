# Security and antivirus

## What Zuka Rivals does (and does not do)
- Reads **iRacing's shared memory** (read-only) and the session YAML.
- Stores history, tags and notes in a local SQLite file in `%AppData%\iRacingTracker`.
- **Has no network code.** It never connects to a server and never uploads or downloads anything.
- **Has no `.exe` or `.dll` committed to the repository.** The `ZukaRivals.exe` and the installer are built by GitHub Actions from this source (`.github/workflows/instalador.yml`).
- **Does not use hidden PowerShell, scheduled tasks, services or keyboard hooks.** No `.bat`, `.vbs` or `.ps1` scripts ship with the product.
- Only persistence: an optional entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, enabled by the
  "Start with Windows" checkbox in Settings. Unchecking it removes the entry, and so does uninstalling.
- Global shortcuts use only `RegisterHotKey` (show/hide and click-through), and can be changed.

## Why an antivirus warning may appear
New programs without reputation can trigger SmartScreen or a heuristic false positive. To reduce this:
1. The executable and installer are built in public by GitHub from the source, without obfuscation.
2. Every release ships a **SHA-256** checksum in `SHA256.txt`, and the workflow runs a Windows Defender scan on the installer.
3. You can upload the installer to [VirusTotal](https://www.virustotal.com) and compare.
4. All the code is readable: nothing is obfuscated.

## Code signing (SmartScreen)
The installer is **not signed**. Without a signature, SmartScreen shows "Unknown publisher", and on PCs with Smart App
Control enabled Windows may block the `.exe` with no "run anyway" option. Options:
- **Microsoft Store (MSIX package):** Microsoft signs it, no warning; free registration for individuals, but the app goes
  through certification and store rules.
- **Code-signing certificate (OV):** roughly EUR 150-300 per year. Even signed, the warning only disappears as the
  publisher builds reputation. The workflow already supports signing: store `SIGN_PFX_BASE64` and `SIGN_PFX_PASSWORD` as repository secrets.
- **Azure Artifact Signing:** roughly USD 10/month, but individuals are only supported in the USA and Canada.

## False positives
If your antivirus flags the program, open an issue with the antivirus name and the detection. The maintainer will report it to the
vendor (Microsoft Defender: https://www.microsoft.com/wdsi/filesubmission).

## Automatic checks
`scripts/verificar.sh` looks for patterns that commonly trigger antivirus engines (network, dynamic code, hooks, hidden
PowerShell, binaries) and runs on every push. The agent in `.claude/agents/revisor-zuka-rivals.md` does a deeper review.

## Reporting a vulnerability
Use GitHub's private reporting (Security > Report a vulnerability) or contact the author.
