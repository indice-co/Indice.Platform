# download nuget.exe
If (!(Test-Path -Path ".\.nuget\nuget.exe" )) {
    If (!(Test-Path -Path ".\.nuget" )) {
        New-Item -ItemType "directory" -Path ".\.nuget"
    }
    Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile ".\.nuget\nuget.exe"
}
# run the pack command to create packages
& "$PSScriptRoot\pack.ps1"

# create local feed
.\.nuget\nuget.exe init .\artifacts .\.nuget\packages
