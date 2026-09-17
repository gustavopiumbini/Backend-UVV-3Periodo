param([switch]$Demo)
Push-Location $PSScriptRoot
try {
    if ($Demo) { dotnet run --project DropMonAPI --launch-profile http -- --Demo:Seed=true }
    else { dotnet run --project DropMonAPI --launch-profile http }
}
finally { Pop-Location }
