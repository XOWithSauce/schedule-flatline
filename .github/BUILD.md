### Build Instructions
This document provides a step-by-step guide on how to build the Flatline mod from the source code.

### Prerequisites
Before you can build the project, you need to have the following software installed:

Visual Studio 2022 (or newer). **.NET desktop development workload* is required.

MelonLoader: This mod requires MelonLoader to be installed in your game. Follow the official MelonLoader installation guide for your specific game version.

### Getting the Source Code

Clone the Repository

```bash
git clone https://github.com/XOWithSauce/schedule-flatline.git
```

### Project Structure

- Mono and IL2Cpp folders have following files:
    1. **.csproj**: The main project file for the mod. Has the Build configurations MONO or IL2CPP + Debug/Release
    2. **.sln**: Preset Solution file with IL2CPP or MONO configuration ready.

- Source folder contains the shared source code between the 2 build types. In source code build differences are marked with conditional `#if MONO` expressions.

### Building the Mod

- First you need to get the required assembly files from the game installation:
1. **Mono**: Opt in to the Alternate or Alternate Beta branch for Schedule I in Steam and wait for it to finish installation.
    - Then you must navigate to C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Schedule I_Data\Managed
    - From here you will need to copy all the files specified in the **Flatline.csproj** file ItemGroup References to the libs-mono directory.
2. **IL2Cpp**: Opt in to the default (none) or beta branch for Schedule I in Steam and wait for it to finish installation.
    - Start your game once and let MelonLoader build the il2cpp assemblies. After this is done the game will start and then close the game.
    - Then you must navigate to the following directory: C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Il2CppAssemblies
    - From here you will need to copy all the files specified in the **Flatline-IL2Cpp.csproj** file ItemGroup References to the libs-il2cpp directory.
    - Additionally you will need the **Il2CppInteropRuntime.dll** from the C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\net6 directory. Copy it to the libs-il2cpp directory.
    - Additionally you will need the **Il2Cpp.dll** from the C:\Program Files (x86)\Steam\steamapps\common\Schedule I\MelonLoader\Dependencies\SupportModules directory. Copy it to the libs-il2cpp directory.



#### Set the Build Configuration:

Open the Project: Open the **.sln** solution file with Visual Studio.

In the Visual Studio toolbar, locate the "Solution Configurations" dropdown. By default, it's set to "Release."

For testing and development, use the Debug configuration. This build will include all debug logs and messages by default that would otherwise only be visible with console command `flatline enable logs`.