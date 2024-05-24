# Jayo's VMC Plugin for VNyan

A VNyan Plugin that allows you to send your vNyan Avatar's motion data over VMC Protocol

# Table of contents
1. [Installation](#installation)
2. [Usage](#usage)
    1. [Connecting to OBS](#connecting-to-obs)
3. [Development](#development)

## Installation
1. Grab the ZIP file from the [latest release](https://github.com/jayo-exe/JayoVMCPlugin/releases/latest) of the plugin.
2. Extract the contents of the ZIP file _directly into your VNyan installation folder_.  This will add the plugin files to yor VNyan `Item\Assemblies` folders.
3. Start up VNyan. Once it loads, confirm that a button for the plugin now exists in your Plugins window!

If the plugin isn't listen in the Plugins panel, 'Open the VNyan Settings window, go to the "Misc" section, and ensure that **Allow 3rd Party Mods/Plugins** is enabled. This is required for this plugin  (or any plugin) to function correctly

## Usage
### Connecting to VMC
In order for the plugin to send motion data over VMC, you'll need to provide the address and port number (if these are different from the default). This is saved in the plugin's settings, so you won't need to do this every time!

If you're having trouble with a VMC receiver not getting your movement data, this may be because the reciever doesn't support bundling VMC message. Try checking off "Don't Bundle Packets" to see if ther is any improvement.

### VMC Receiver Behaviour
The JayoVMCPlugin.dll file also contains a "VMC Receiver Manager" behaviour that can be used to accept incoming VMC messages and apply them to a model.  This can be used in your model project in Unity to "preview" your model's movement' before exporting, or to create VNyan objects that contain VMC-Controllable Characters.  Using the Behaviour is pretty simple:
1. Create a new GameObject
2. Attach the VMCReceiverManager behaviour to the GameObject
3. Place a VMC-compatable model (VRM, vsfavatar, etc) into the scene as a _child_ of the GameObject
4. Set the Port number and target Model object for the VMCReceiverManager in the Inspector
5. Run the scene!
6. If you're creating a Custom Object to use in VNyan, select the GameObject you've created, and use the VNyan SDK to "Export Custom Object"

## Development
(Almost) Everything you'll need to develop a fork of this plugin (or some other plugin based on this one)!  The main VS project contains all of the code for the plugin DLL, and the `dist` folder contains a `unitypackage` that can be dragged into a project to build and modify the UI and export the modified Custom Object.

It's worth noting that per VNyan's requirements, this plugni in built under **Unity 2020.3.40f1** , so you'll need to develop on this version to maintain compatability with VNyan.
You'll also need the [VNyan SDK](https://suvidriel.itch.io/vnyan) imported into your project for it to function properly.
Your Visual C# project will need to mave the paths to all dependencies updated to match their locations on your machine.  Most should point to Unity Engine libraries for the correct Engine version **2020.3.40f1**.
