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

if ($CommitSHA) {
    $newVersion = "$($newVersion)+$($CommitSHA.SubString(0, 7))"
}

$versionElement.InnerText = $newVersion
$xmlDoc.Save($csProjPath)

if (Test-Path $buildPath -PathType Container) {
    rm -rf $buildPath
}

$uid = sh -c 'id -u'
$gid = sh -c 'id -g'

docker run --rm -v "$($basePath):/var/src" mcr.microsoft.com/dotnet/core/sdk:3.1.401-alpine3.12 ash -c "dotnet publish -c Release /var/src/DnsChanger.Web/DnsChanger.Web.csproj -o /var/src/build && chown -R $($uid):$($gid) /var/src"

mv "$($csProjPath).original" $csProjPath

$nuspecPath = Join-Path $buildPath dnschanger.web.nuspec
$nupkgPath = Join-Path $buildPath "dnschanger.web.$($newVersion).nupkg"
cp dnschanger.web.nuspec $nuspecPath

[xml]$xmlDoc = Get-Content $nuspecPath
$xmlDoc['package']['metadata']['version'].InnerText = $newVersion
$xmlDoc.Save($nuspecPath)

Compress-Archive -Path "$($buildPath)/*" -DestinationPath $nupkgPath

Write-Host "Built!"
Write-Host "::set-env name=VERSION::$newVersion"
Write-Host "::set-env name=PKG_PATH::$nupkgPath"