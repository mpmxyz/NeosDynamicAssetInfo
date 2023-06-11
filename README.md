# NeosDynamicAssetInfo

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that attaches Dynamic Variables to imported assets to enable LogiX to read information about it.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader) and [NeosAssetImportHook](https://github.com/mpmxyz/NeosAssetImportHook).
2. Download [NeosDynamicAssetInfo.dll](https://github.com/mpmxyz/NeosDynamicAssetInfo/releases/latest/download/NeosDynamicAssetInfo.dll).
3. Copy it into the `nml_mods` directory inside your Neos install.
4. Start the game. If you want to verify that the mod is working you can check your Neos logs.

## Usage

When importing assets the following dynamic variables are attached to the dynamic variable space "asset":

|Name       |Type       |Description|
|-----------|-----------|-----------|
|asset_dvs  |Slot       |Reference to child slot containing the dynamic variable components|
|asset_count|int        |Number of contained assets|
|grabbable  |Grabbable  |Reference to Grabbable of asset (only if it exists)|
|urlX       |Uri        |URL of the asset (only if asset is a static asset)|
|assetX     |IAssetProvider|Reference to an asset|
|assetX     |IAssetProvider&lt;A&gt; where A : IAsset|Reference to an asset (for each possible A)|

Note: X is an integer within range [0, asset_count-1], use a for loop to find the assets you are looking for. (no guaranteed order)
  
The dynamic variable space and its variables can be cleanly removed by deleting the slot asset_dvs - a DestroyProxy automatically deletes the DynamicVariableSpace component.
