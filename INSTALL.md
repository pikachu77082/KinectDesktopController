# Installation & Requirements

To run or build the Kinect Desktop Controller on your own machine, you must meet the following hardware and software requirements.

## Hardware Requirements
1. **Xbox One Kinect Sensor** (or Kinect for Windows v2).
2. **Kinect PC Adapter** (to connect the Kinect to a USB 3.0 port on your PC).
3. **Dedicated USB 3.0 controller** (Kinect v2 is highly sensitive to USB bandwidth; ensure it is plugged into a true USB 3.0 port).

## Software Requirements
1. **Windows 10 or Windows 11** (64-bit).
2. **.NET 8.0 SDK** (The project is built specifically on `net8.0-windows`).
3. **Kinect for Windows SDK 2.0**.
   - Download and install the official SDK from Microsoft.
   - It will install to the default location (`C:\Program Files\Microsoft SDKs\Kinect\v2.0_1409\`). The project file (`.csproj`) relies on these hardcoded paths to locate the native libraries (such as `Kinect20.Face.dll` and the `NuiDatabase`).

## Setup Instructions
1. Install the Kinect for Windows SDK 2.0.
2. Connect your Kinect sensor to your PC and verify it is working using the "Kinect Studio" or "Kinect Configuration Verifier" tools included with the SDK.
3. Clone this repository.
4. Open a terminal in the project directory and run:
   ```bash
   dotnet build -c Release
   ```
5. The compiled executable will be located in `bin\Release\net8.0-windows\`.

## Troubleshooting Build Errors
If you get errors about missing assemblies (`Microsoft.Kinect.dll` or `Microsoft.Kinect.Face.dll`), ensure that the Kinect SDK is installed in the default `C:\Program Files\` location. The post-build events in the `.csproj` file copy necessary face-tracking databases directly from this folder.
