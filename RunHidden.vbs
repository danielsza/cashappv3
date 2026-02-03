' Cash Drawer Server - Run Hidden in Background
' Automatically requests administrator rights

Set WshShell = CreateObject("WScript.Shell")
Set FSO = CreateObject("Scripting.FileSystemObject")

' Get the directory where this script is located
ScriptDir = FSO.GetParentFolderName(WScript.ScriptFullName)

' Check if running as admin, if not, re-launch with admin rights
If Not WScript.Arguments.Named.Exists("elevated") Then
    Set objShell = CreateObject("Shell.Application")
    objShell.ShellExecute "wscript.exe", """" & WScript.ScriptFullName & """ /elevated", "", "runas", 0
    WScript.Quit
End If

' Now running as administrator
' Start server completely hidden (0 = hidden, False = don't wait)
WshShell.Run """" & ScriptDir & "\CashDrawer.Server.exe""", 0, False

' Wait a moment for it to start
WScript.Sleep 2000

' Show brief notification that it started
WshShell.Popup "Cash Drawer Server started in background" & vbCrLf & vbCrLf & "Running as Administrator" & vbCrLf & "No window will be shown", 3, "Cash Server", 64
