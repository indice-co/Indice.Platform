# download nuget.exe
If (!(Test-Path -Path .\nuget.exe )) {
    Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile .\nuget.exe
}
# run the pack command to create packages
& "$PSScriptRoot\pack.ps1"

# create local feed
.\nuget.exe init .\artifacts .\packages
