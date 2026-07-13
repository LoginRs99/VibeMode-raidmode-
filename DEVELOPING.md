# VibeMode Development & Build Guide

If a future update to Outward is released, you will need to recompile the mod to ensure compatibility. This guide explains how to set up the development environment from a fresh `git clone`.

## 1. Setup Required Assemblies
Because the base game's Unity Engine and `Assembly-CSharp` binaries are copyrighted, they are **not** included in this GitHub repository. You must manually supply them.

1. Create the `RaidMode/bin/Debug/` folder structure if it doesn't already exist.
2. Navigate to your Outward Definitive Edition installation directory.
3. Copy the following DLL files from your game folder (and your BepInEx installation) into the `RaidMode/bin/Debug/` folder:
    - `0Harmony.dll` (from `BepInEx/core/`)
    - `BepInEx.dll` (from `BepInEx/core/`)
    - `Assembly-CSharp-firstpass-publicized.dll` (Requires a publicizer tool, or use the unpublicized `Assembly-CSharp-firstpass.dll` from `Outward_Defed_Data/Managed/` and update `RaidMode.csproj` accordingly)
    - `Assembly-CSharp-publicized.dll` (Requires a publicizer tool)
    - `Photon3Unity3D.dll`
    - `UnityEngine.dll`
    - `UnityEngine.CoreModule.dll`
    - `UnityEngine.AIModule.dll`
    - `UnityEngine.AnimationModule.dll`
    - `UnityEngine.InputLegacyModule.dll`
    - `UnityEngine.InputModule.dll`
    - `UnityEngine.UI.dll`

*Note: For the mod to compile and access private fields, you must publicize the base game's `Assembly-CSharp.dll` using a tool like BepInEx AssemblyPublicizer, or rewrite the codebase to use reflection/Harmony AccessTools for private variables.*

## 2. Compiling the Mod
Once all the dependency DLLs are present in the `RaidMode/bin/Debug/` directory, open a command line terminal at the root directory of this repository (where `RaidMode.sln` is located) and run:

```bash
dotnet build RaidMode.sln
```

This will automatically restore the `Microsoft.NETFramework.ReferenceAssemblies` targeting pack (so you don't need Visual Studio installed, just the modern `.NET SDK`) and compile the `.dll`.

## 3. Packaging
After a successful build:
1. The new `VibeMode.dll` will be generated in `RaidMode/bin/Debug/`.
2. Copy it into the `dist/` folder, replacing the old one.
3. Compress the contents of the `dist/` folder (including `manifest.json`, `icon.png`, and `README.md`) into a `VibeMode.zip` archive.
4. Upload to Thunderstore.
