$distFolders = @(
    "src\Indice.Features.Cases.App\dist",
    "src\Indice.Features.Identity.AdminUI.App\dist",
    "src\Indice.Features.Messages.App\dist",
    "src\Indice.Features.Risk.App\dist",
    "src\Indice.Features.Cases.App\node_modules",
    "src\Indice.Features.Identity.AdminUI.App\node_modules",
    "src\Indice.Features.Messages.App\node_modules",
    "src\Indice.Features.Risk.App\node_modules"
)

function DeleteFolder {
    param (
        [string]$folderPath
    )

    if (Test-Path $folderPath) {
        try {
            Remove-Item -Path $folderPath -Recurse -Force
            Write-Output "Folder '$folderPath' and all contents deleted successfully."
        } catch {
            Write-Error "Error deleting folder '$folderPath': $_"
        }
    } else {
        Write-Output "Folder '$folderPath' does not exist."
    }
}

foreach ($x in $distFolders) {
    DeleteFolder -folderPath $x
}