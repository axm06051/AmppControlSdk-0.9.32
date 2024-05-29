param(
    [string]$PackagePath = $(throw "Package path must be provided")
)

function ValidateVersion {
    param (
        [string]$Version = $(throw "Version must be provided.")
    );

    if ($Version -notmatch "^[0-9]{1,8}\.[0-9]{1,8}\.[0-9]{1,8}((\.[0-9]{1,8})?|(-beta-|-alpha-)[0-9]{1,8})$") {
        throw "Version is an invalid format"
    }
}

function ValidateManifest {
    param(
        [string]$PackageFileName = $(throw "Package file name must be provided"),
        [string]$ManifestFilePath = $(throw "Manifest path must be provided")
    );

    if ((Test-Path $ManifestFilePath) -eq $false) {
        throw "Package does not contain a package-manifest.json file"
    }

    [string]$ManifestContentJson = Get-Content $ManifestFilePath
    $ManifestContent = ConvertFrom-Json $ManifestContentJson

    if ($ManifestContent.Name -eq "" -or $ManifestContent.Name -eq $null) {
        throw "Name must be provided in the package-manifest.json file"
    }
    elseif ($ManifestContent.Version -eq "" -or $ManifestContent.Version -eq $null) {
        throw "Version must be provided in the package-manifest.json file"
    }
    
    ValidateVersion -Version $ManifestContent.Version

    if (!$PackageFileName.StartsWith("$($ManifestContent.Name).$($ManifestContent.Version)", "CurrentCultureIgnoreCase")){
        throw "Package file name and manifest name and version do not match. Package file name: $PackageFileName $($ManifestContent.Name).$($ManifestContent.Version)"
    }
    elseif ($ManifestContent.PackageType -ne "System" -and $ManifestContent.VRU -lt 0) {
        throw "Package VRU is missing or invalid"
    }
    elseif ($ManifestContent.Title -eq "" -or $ManifestContent.Title -eq $null -or $ManifestContent.Title.Length -gt 64) {
        throw "Package title is missing or longer than 64 characters"
    }
    elseif ($ManifestContent.Summary.Length -gt 256) {
        throw "Package summary is longer than 256 characters"
    }
    elseif ($ManifestContent.Description.Length -gt 1024) {
        throw "Package description is longer than 1024 characters"
    }
    elseif ($ManifestContent.ClientDetails -ne $null -and 
        ($ManifestContent.ClientDetails.GrantType -eq $null -or $ManifestContent.ClientDetails.GrantType -eq "")) {
            throw "GrantType of ClientDetails is invalid or missing"
    }
     elseif ($ManifestContent.PackageOS -ne "Windows" -and $ManifestContent.PackageOS -ne "LinuxX64")  {
            throw "Invalid PackageOS"
    }
}

$ErrorActionPreference = "STOP"

if ((Test-Path $PackagePath) -eq $False) {
    throw "Package could not be found"
}

$TmpPath = [System.IO.Path]::GetTempPath();
$TmpFolder = [System.Guid]::NewGuid()
$FileInfo = Get-Item $PackagePath
$UnzipPath = (Join-Path $TmpPath $TmpFolder)

try {    
    new-item -Path $UnzipPath -ItemType Directory
    Copy-Item $PackagePath "$UnzipPath/$($FileInfo.Name)"
    $ZipName = "$($FileInfo.Name.Substring(0, $FileInfo.Name.Length - $FileInfo.Extension.Length)).zip"
    Rename-Item -Path "$UnzipPath\$($FileInfo.Name)" -NewName $ZipName
    Expand-Archive -Path "$UnzipPath\$ZipName" -DestinationPath $UnzipPath

    # Validate the manifest file
    ValidateManifest -PackageFileName $FileInfo.Name -ManifestFilePath "$UnzipPath/package-manifest.json"
}
finally {
    Remove-Item $UnzipPath -Recurse
}