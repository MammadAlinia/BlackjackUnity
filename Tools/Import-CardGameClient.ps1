param([string]$EditorPath = 'E:/Programs/Unity/Hub/Editor/6000.6.0f1/Editor', [string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$solutionRoot = Join-Path (Split-Path $projectRoot -Parent) 'CardGameServer'
$destination = Join-Path $projectRoot 'Assets/Plugins/CardGame'
dotnet build (Join-Path $solutionRoot 'CardGame.Client/CardGame.Client.csproj') --configuration $Configuration --framework netstandard2.1 -p:CopyLocalLockFileAssemblies=true
if ($LASTEXITCODE -ne 0) { throw 'CardGame client build failed.' }
$referenceRoot = Join-Path $EditorPath 'Data/NetStandard/ref/2.1.0'
if (!(Test-Path -LiteralPath $referenceRoot)) { throw 'Pass -EditorPath pointing to the Unity Editor directory.' }
$provided = @{}
Get-ChildItem -LiteralPath $referenceRoot -Filter '*.dll' | ForEach-Object { $provided[$_.Name] = $true }
Get-ChildItem -LiteralPath (Join-Path $EditorPath 'Data/BCLExtensions/TargetingPacks/netstandard2.1/ref') -Filter '*.dll' |
    ForEach-Object { $provided[$_.Name] = $true }
$output = Join-Path $solutionRoot "CardGame.Client/bin/$Configuration/netstandard2.1"
$assets = Get-Content -LiteralPath (Join-Path $solutionRoot 'CardGame.Client/obj/project.assets.json') -Raw | ConvertFrom-Json
$required = @{ 'CardGame.Client.dll' = $true; 'CardGame.Contracts.dll' = $true }
foreach ($library in $assets.targets.'netstandard2.1'.PSObject.Properties) {
    foreach ($asset in $library.Value.runtime.PSObject.Properties) {
        $name = [IO.Path]::GetFileName($asset.Name)
        if ($name.EndsWith('.dll')) { $required[$name] = $true }
    }
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$imported = @()
Get-ChildItem -LiteralPath $output -Filter '*.dll' | ForEach-Object {
    if ($required.ContainsKey($_.Name) -and !$provided.ContainsKey($_.Name)) {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $destination $_.Name)
        $imported += $_.Name
    }
}
$inventory = Join-Path $destination 'imported-assemblies.json'
if (Test-Path -LiteralPath $inventory) {
    Get-Content -LiteralPath $inventory -Raw | ConvertFrom-Json | ForEach-Object {
        if ($_ -notin $imported -and [IO.Path]::GetFileName($_) -eq $_) {
            $obsolete = [IO.Path]::GetFullPath((Join-Path $destination $_))
            if (!$obsolete.StartsWith([IO.Path]::GetFullPath($destination) + [IO.Path]::DirectorySeparatorChar)) { throw 'Invalid inventory path.' }
            Remove-Item -LiteralPath $obsolete -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath ($obsolete + '.meta') -ErrorAction SilentlyContinue
        }
    }
}
ConvertTo-Json -InputObject @($imported | Sort-Object) | Set-Content -LiteralPath $inventory -Encoding utf8
Write-Output "Imported $($imported.Count) compatible assemblies into $destination"
