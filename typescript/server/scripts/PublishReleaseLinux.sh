#!/bin/bash

if ! [ -x "$(command -v jq)" ]; then
  echo "jq is not installed. Please install jq to use this script." >&2
  exit 1
fi

if ! [ -x "$(command -v dotnet)" ]; then
  echo "dotnet CLI is not installed!" >&2
  exit 1
fi

# Check if Node.js is installed
if [ -z "$(which node)" ]; then
  echo "Node.js is not installed. Please install Node.js and try again."
  exit 1  # Exit the script with a non-zero status code
fi

# Your script continues here...


# Save current directory 
PREV_DIR=$(pwd)

# Change to parent directory
cd ..

currentDirectory=$(pwd)

# Define the subfolder name
subfolderName="release/linux" 

# Use Join-Path to combine the current directory and subfolder name
packagePathLinux=$currentDirectory/$subfolderName

echo "Package Path For Linux: $packagePathLinux"

# Check if the target folder exists and delete it if it does  
if [ -d "$packagePathLinux" ]; then
    echo "Target folder $packagePathLinux exists. Deleting..."
    rm -rf "$packagePathLinux"
fi

# Define the path to the JSON file
jsonFilePath="$currentDirectory/package/package-manifest.json"

# Check if the file exists
if [ ! -f "$jsonFilePath" ]; then
    echo "Package Manifest File not found: $jsonFilePath"
    exit 1
fi

# Read the JSON content
jsonContent=$(cat "$jsonFilePath")

# Extract the 'name' and 'version' fields 
packageName=$(echo "$jsonContent" | jq -r '.name')
version=$(echo "$jsonContent" | jq -r '.version')
title=$(echo "$jsonContent" | jq -r '.title')

# Display the current 'name' and 'version'
echo "Current Name: $packageName"
echo "Current Version: $version"
echo "Current Title: $title"

# Ask the user if they want to update the 'name' and 'version'
read -p "Name ($packageName): " newName
read -p "Version ($version): " newVersion  
read -p "Title ($title): " newTitle

# Update 'name' and 'version' if requested
if [ ! -z "$newName" ]; then
    jsonContent=$(echo "$jsonContent" | jq --arg name "$newName" '.name = $name') 
    packageName=$newName
fi

if [ ! -z "$newTitle" ]; then
    jsonContent=$(echo "$jsonContent" | jq --arg title "$newTitle" '.title = $title')
fi   

if [ ! -z "$newVersion" ]; then
    if [[ ! "$newVersion" =~ ^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$ ]]; then
        echo "Invalid version format. Please use a format like 'X.Y.Z' or 'X.Y.Z.W'"
        exit 1
    fi
    
    jsonContent=$(echo "$jsonContent" | jq --arg version "$newVersion" '.version = $version')
    version=$newVersion
fi

jsonContent=$(echo "$jsonContent" | jq '.packageOS = "LinuxX64"')

# For linux, remove the .exe and replace \ with / in file path  
path=$(echo "$jsonContent" | jq -r '.application.filePath')
path="${path%.exe}"
path=${path//\\//}
jsonContent=$(echo "$jsonContent" | jq --arg path "$path" '.application.filePath = $path')

# Recreate the target folder
mkdir -p "$packagePathLinux"

destinationPath="./out/Packages/LinuxX64/$packageName/$version"

echo "Target Path: $destinationPath"

# Check if the target folder exists and delete it if it does
if [ -d "./out/Packages" ]; then
    echo "Deleting Output folder"
    rm -rf "./out/Packages" 
fi

# Create the target folder  
mkdir -p "$destinationPath"

packageFilename="$packageName.$version.gvpkg"
destinationPackagePath="$destinationPath/$packageFilename"

echo "Creating package: $destinationPackagePath"

# Save the updated JSON content back to the file
echo "$jsonContent" > "$jsonFilePath"


cp "$jsonFilePath" "$packagePathLinux/package-manifest.json"
cp -r ./package/templates "$packagePathLinux"

mkdir -p "$packagePathLinux/bin/Markdown" 
cp -r ./Markdown/* "$packagePathLinux/bin/Markdown"


echo "Package Name $packageName" 

pubPath="$packagePathLinux/bin"
echo "Publish Path $pubPath"


# Build the App for Linux
echo "Package Name $packageName"

# Create the JS file
npm run build

# Build the App for Windows
npm run build:linux


echo "************************************"

pathToExe="$packagePathLinux/$path"

echo "Granting exe permissions to: $pathToExe"

chmod +x "$pathToExe"

# Build the ZIP
#cd "$packagePathLinux"
#zip -r "../$destinationPackagePath" bin templates package-manifest.json
#tar -czvf $destinationPackagePath -C .. bin templates package-manifest.json
echo "PackageFileName: $packageFilename"

cd release/linux
zip -r $packageFilename package-manifest.json bin templates
cd ../..

# Validate package
#bash ./scripts/ValidateAMPPPackage.sh "$destinationPackagePath" 

cp "$jsonFilePath" "$destinationPath/package-manifest.json"
cp "./release/linux/$packageFilename" $destinationPath

zipName="$packageName.$version.gvzip"
cd out 
zip -r $zipName Packages

# Return to previous directory
cd "$PREV_DIR"
