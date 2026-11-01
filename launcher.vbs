' Arranca o Zuka Rivals sem mostrar a janela preta.
Set fso = CreateObject("Scripting.FileSystemObject")
Set sh = CreateObject("WScript.Shell")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
sh.CurrentDirectory = dir & "\app"
sh.Run "dotnet """ & dir & "\app\IRacingTracker.dll""", 0, False
