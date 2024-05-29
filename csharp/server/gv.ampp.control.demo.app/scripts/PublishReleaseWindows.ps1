# Check the PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 6) {
    Write-Host "PowerShell version is less than 6. Exiting..."
    exit 1  # Exit with a non-zero status code to indicate failure
}

# Your script continues here
Write-Host "PowerShell version is 6 or higher. Proceeding..."


push-Location ..

$currentDirectory = Get-Location

# Define the subfolder name

$subfolderName = "release\windows"

# Use Join-Path to combine the current directory and subfolder name
$packagePathWindows  = Join-Path -Path $currentDirectory -ChildPath $subfolderName

 # Check if the target folder exists and delete it if it does
if (Test-Path -Path $packagePathWindows -PathType Container) {
    Write-Host "Target folder $($packagePathWindows) exists. Deleting..."
    Remove-Item -Path $packagePathWindows -Recurse -Force
}


# Define the path to the JSON file
$jsonFilePath = "$($currentDirectory)\package\package-manifest.json"

# Check if the file exists
if (-not (Test-Path -Path $jsonFilePath -PathType Leaf)) {
    Write-Host "Package Manifest File not found: $jsonFilePath"
    return
}



# Read the JSON content
$jsonContent = Get-Content -Path $jsonFilePath -Raw | ConvertFrom-Json

# Extract the 'name' and 'version' fields
$packageName = $jsonContent.name
$version = $jsonContent.version
$title = $jsonContent.title
$execuable = $jsonContent.application.filePath

# Display the current 'name' and 'version'
#Write-Host "Current Name: $currentName"
#Write-Host "Current Version: $currentVersion"

# Ask the user if they want to update the 'name' and 'version'
$newName = Read-Host "Name: $($packageName)"
$newVersion = Read-Host "Version: ($version)"
$newTitle = Read-Host "Title: ($title)"
$newExePath = Read-Host "FilePath: ($execuable)"

# Update 'name' and 'version' if requested
if (-not [string]::IsNullOrWhiteSpace($newName)) {
    $jsonContent.name = $newName
    $packageName = $newName
}

if (-not [string]::IsNullOrWhiteSpace($newTitle)) {
    $jsonContent.title = $newTitle
}

if (-not [string]::IsNullOrWhiteSpace($newVersion)) {
        $validVersion = $version -match '^\d+\.\d+\.\d+(\.\d+)?$'
    if (-not $validVersion) {
            Write-Host "Invalid version format. Please use a format like 'X.Y.Z' or 'X.Y.Z.W'"
            exit 1
    }
    $jsonContent.version = $newVersion
    
    $version = $newVersion
}

if (-not [string]::IsNullOrWhiteSpace($newExePath)) {
    $jsonContent.application.filePath = $newExePath
}


$jsonContent.packageOS = "Windows"

# Recreate the target folder
New-Item -Path $packagePathWindows -ItemType Directory

$destinationPath  = ".\out\Packages\Windows\$($packageName)\$($version)"


 # Check if the target folder exists and delete it if it does
if (Test-Path -Path "./out" -PathType Container) {
    Write-Host "Deleting Ouput folder"
    Remove-Item -Path "./out" -Recurse -Force
}


# Create the target folder
New-Item -Path $destinationPath -ItemType Directory

$packageFilename = "$($packageName).$($version).gvpkg"

$destinationPakagePath  = "$($destinationPath)\$($packageFilename)"

Write-Host "Creating package: $($destinationPakagePath)" 



# Save the updated JSON content back to the file
$jsonContent | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonFilePath -NoNewline

 

Copy-Item $jsonFilePath -Destination $($packagePathWindows);
Copy-Item ./package/templates -Destination $($packagePathWindows) -Recurse;


Write-Host "Package Name $($packageName)"

# Build the App for Windows
dotnet publish -c Release -r win7-x64 -o $packagePathWindows\bin


# Build the ZIP
$compress = @{
    Path = "$($packagePathWindows)\bin", "$($packagePathWindows)\templates", "$($packagePathWindows)\package-manifest.json"
    CompressionLevel = "Fastest"
    DestinationPath = $destinationPakagePath
}
Compress-Archive @compress


& "./scripts/ValidateAMPPPackage.ps1" -PackagePath $destinationPakagePath

Copy-Item $jsonFilePath -Destination $($destinationPath);

 $zipName = ".\$($packageName)-$($version).gvzip"

compress-Archive -Path ".\out\Packages" -DestinationPath $zipName

pop-Location