# C++ Ampp Control Access Sample application

## What this sample application does:

1) Request a bearer token through a REST API call using the provided API_KEY.
2) Request a list of all applications currently registered to Ampp Control.
3) For a specific application, request the list of all its workload IDs (instances), running or not.
4) Using the bearer token, open a secure websocket.
5) Using a specific workload ID, subscribe to the appropriate notification and status topics.
6) Request notifications for any state changes (.getstate command) for this workload.
7) Send a command to change the state of the application (here, request a slider change on an audiomixer).

Please refer to the top of  "AmppControlSample.cpp" for additional informations.

## Building the sample application on Linux

This procedure has been tested on a freshly installed Ubuntu 20.04 virtual machine on VirtualBox

### Cloning the project

#### Install git

`sudo apt install git`

#### Goto https://dev.azure.com/grassvalley-ampp-partners/_git/AmppControlSDK

Select "Clone" and copy the HTTPS link.

Also, below the HTTPS link, click the "Generate Git Credentials", you will need the password for the next command.

#### Clone the project

`git clone https://grassvalley-ampp-partners@dev.azure.com/grassvalley-ampp-partners/AmppControlSDK/_git/AmppControlSDK`

When asked for a password, cut&paste the password from the Git Credentials.

*Hint: when pasting the password, it won't appear on the screen although the cut&paste was successful. Just press Enter.*

### Building the app

#### Install the compiler and libraires

`sudo apt install build-essential cmake libwebsocketpp-dev libasio-dev nlohmann-json3-dev libcurl4-openssl-dev`

#### Goto the AmppControlSDK directory where the sources are

`cd AmppControlSDK/cpp/AmppControlSample`

#### Create build directory and compile

```
mkdir build
cd build
cmake ..
make
```



## Building the sample application on Windows

- Open the .sln file in the subdirectory AmppControlSample with Visual Studio.

- If it is the first time, do a "nuget restore" to get all the necessary libraries.

- Then simply "Build" the project.

### Important notes

- If the project is build as "Debug", the executable may not run because of the missing MSVCR110D.dll. When in "Release", the executable will look for MSVCR110.dll (without the "D") and this dll is usually already installed.

- To run the sample app from Visual Studio, don't forget to add the command line parameters in "properties->Debugging->Command Arguments".