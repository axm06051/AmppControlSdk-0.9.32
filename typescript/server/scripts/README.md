

# Building a Package for release

Although these samples can be run as standalone apps on any server, it is possible to package them up for deployment within AMPP
This would allow them to be installed and run under resource manager.

The scripts in this folder can be used to help build a gvzip package which can be deleivered to GV.
After some manual validation of the package it will be made available in the App Store

# Environment Variables

When an APP is run inside AMPP (under Node Agent) The Following environent variables will bet set: 

- GVCLUSTER_PLATFORMAPIKEY
- GVCLUSTER_PLATFORMURI
- GVCLUSTER_WORKLOADID
- GVCLUSTER_WORKLOADNAME



## Structure of GVZIP

The package manifest is a zip file and must contain the following structure:
```
- Packages/{OS}/{PackageName}/{Version}/

                              |- {packageName}.{Version}.gvpkg
                              |- package-manifest.json

``````

OS is either Windows or LinuxX64

# Structure of GVPKG file

The .gvpkg file must contain the following:

- package-manifest.json
- bin/
- templates/

The bin folder contains the binaries of your exexutable
the package-manifest.json contains the path to your executable.


```
"application": {
    "filePath": "bin/Sdk.DemoService",
    "arguments": "--port {{allocate_port}}",
    "workingFolder": "bin"
  },
```

An example package-manifest is included in the SDK sample app.

# Publishing for Windows

To publish for windows run the Powershell Script PublishReleaseWindows.ps1

**Note: Powershell 6+ required**

When you run this script it will prompt you for the:
- Name
- Version
- Title

Press enter to keep the same, or enter a value to update the package-manifest.json
This script will then build and package the executable into the gvzip.

# Publishing for Linux

To publish for linux run the Powershell Script PublishReleaseLinux.sh
This script should be run on a linux machine or WSL2 on Windows.

**Note: nodejs required**

The following packages are required
- pkg
- jq


When you run this script it will prompt you for the:
- Name
- Version
- Title

Press enter to keep the same, or enter a value to update the package-manifest.json
This script will then build and package the executable into the gvzip.

# Deployment in AMPP

Once you have built your package and verified it matches the structure designed above, please send to:

```colin.france@grassvalley.com```