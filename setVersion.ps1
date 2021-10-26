param (
    [Parameter(Mandatory=$true)][int]$BuildNumber,
    [Parameter(Mandatory=$false)][string]$CommitSHA
)

$pwd = Pwd
$basePath = $pwd
$csProjPath = Join-Path $basePath DnsChanger.Web/DnsChanger.Web.csproj
$buildPath = Join-Path $basePath build

cp $csProjPath "$($csProjPath).original"

[xml]$xmlDoc = Get-Content $csProjPath
$versionElement = $xmlDoc['Project']['PropertyGroup']['Version']
$version = [version]$versionElement.InnerText
$newVersion = "$($version.Major).$($version.Minor).$($BuildNumber)"

$versionWithoutHash = $newVersion
if ($CommitSHA) {
    $newVersion = "$($newVersion)+$($CommitSHA.SubString(0, 7))"
}

$versionElement.InnerText = $newVersion
$xmlDoc.Save($csProjPath)

if ($env:GITHUB_ENV) {
    Write-Output "VERSION_WITHOUT_HASH=$versionWithoutHash" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    Write-Output "VERSION=$newVersion" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
    Write-Output "PKG_PATH=$nupkgPath" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
}
