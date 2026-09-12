<#
.SYNOPSIS
    Builds, tests and packs the Shaker SQLite activity library. The .nupkg lands in .\artifacts.
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
$solution = Join-Path $root 'Shaker.SQLLiteDB.Activities.sln'

Write-Host '==> Restoring' -ForegroundColor Cyan
dotnet restore $solution

Write-Host "==> Building ($Configuration)" -ForegroundColor Cyan
dotnet build $solution -c $Configuration --no-restore

Write-Host '==> Testing' -ForegroundColor Cyan
dotnet test (Join-Path $root 'tests\Shaker.SQLLiteDB.Activities.Tests\Shaker.SQLLiteDB.Activities.Tests.csproj') -c $Configuration --no-build

Write-Host '==> Packing' -ForegroundColor Cyan
dotnet pack (Join-Path $root 'src\Shaker.SQLLiteDB.Activities\Shaker.SQLLiteDB.Activities.csproj') -c $Configuration --no-build

Write-Host '==> Done. Packages:' -ForegroundColor Green
Get-ChildItem (Join-Path $root 'artifacts') -Filter *.nupkg | Select-Object -ExpandProperty Name
