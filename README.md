# Jayo's Extended VMC Plugin for VNyan

A VNyan Plugin that allows you to send your vNyan Avatar's motion data over VMC Protocol! 
Including extended VNyan-specific functions and protocol extension (called NyaVMC) to provide improved sync on Triggers, Animation parameters, and more! 

# Table of contents
1. [Installation](#installation)
2. [Usage](#usage)
    1. [Connecting to VMC](#connecting-to-vmc)
    2. [VMC Receiver Behaviour](#vmc-receiver-behaviour)
    3. [Extended Features](#extended-features)
        1. [Variable Tracking Rate](#variable-tracking-rate)
        2. [Send Bone Scale](#send-bone-scale)
        3. [Send Triggers](#send-triggers)
        4. [Send Animation Parameters](#send-animation-parameters)
3. [Development](#development)
4. [Special Thanks](#special-thanks)

## Installation
1. Grab the ZIP file from the [latest release](https://github.com/jayo-exe/JayoVMCPlugin/releases/latest) of the plugin.
2. Extract the contents of the ZIP file _directly into your VNyan installation folder_.  This will add the plugin files to yor VNyan `Item\Assemblies` folders.
3. Start up VNyan. Once it loads, confirm that a button for the plugin now exists in your Plugins window!

**IMPORTANT:** If the plugin isn't listed in the Plugins panel, 'Open the VNyan Settings window, go to the "Misc" section, and ensure that **Allow 3rd Party Mods/Plugins** is enabled. This is required for this plugin  (or any plugin) to function correctly

## Usage
### Connecting to VMC
In order for the plugin to send motion data over VMC, you'll need to provide the address and port number (if these are different from the default). This is saved in the plugin's settings, so you won't need to do this every time!

If you're having trouble with a VMC receiver not getting your movement data, this may be because the reciever doesn't support bundling VMC message. Try checking off "Don't Bundle Packets" to see if ther is any improvement.

![VMC Plugin UI](https://github.com/user-attachments/assets/a513935c-8623-4498-8623-9b5dec13dcf7)

### VMC Receiver Behaviour
The JayoVMCPlugin.dll file also contains a "VMC Receiver Manager" behaviour that can be used to accept incoming VMC messages and apply them to a model.  This can be used in your model project in Unity to "preview" your model's movement' before exporting, or to create VNyan objects that contain VMC-Controllable Characters.  Using the Behaviour is pretty simple:
1. Create a new GameObject
2. Attach the VMCReceiverManager behaviour to the GameObject
3. Place a VMC-compatable model (VRM, vsfavatar, etc) into the scene as a _child_ of the GameObject
4. Set the Port number and target Model object for the VMCReceiverManager in the Inspector
5. Run the scene!
6. If you're creating a Custom Object to use in VNyan, select the GameObject you've created, and use the VNyan SDK to "Export Custom Object"

### Extended Features
This plugin's library also extends the functionality of the VMC protocol, allowing a greater range of model state information to be sent by the Sender plugin, and handled by the Receiver Behaviour.  

These features are denoted with message addresses starting with `/NyaVMC` rather then `/VMC`, so while these messages won't work with other VMC receivers, any receivers that are properly VMC-compliant will just ignore them.

![Extended Features seen on the UI](https://github.com/user-attachments/assets/80505158-900d-4ab2-98fa-09b865593bed)

#### Variable Tracking Rate
To reduce network load, NyaVMC provides a message in the format of `/NyaVMC/F <interval>` to communicate how many frames pass between tracking updates.

This plugin supports sending tracking updates at various rates (assuming VNyan is running at 60FPS) : 60, 30, 20, and 15 updates per second. if the "provide Send Rate Info" setting is enabled, these rate messages will be included in every tracking update bundle

The VMC Sender will send these packets at the end of each tracking update, as long as the "provide Send Rate Info" setting is enabled. 

Objects using the `VMCReceiverManager` behaviour will interpret these messages to understand the timing of inbound messages, and smoothly move between each update for the correct number of frames.  This way, the movement of the receiver's model remains smooth and accurate at lower tracking rates!

#### Send Bone Scale
As VMC's Bone Position message only supports location and rotation, there is a NyaVMC message to communicate changes in Bone Scale. This message takes the format `/NyaVMC/Ext/Bone/Scale <boneName> <xScale> <yScale> <zScale>`.

The VMC Sender will send these packets whenever a bone on your VNyan avatar changes scale, as long as the "Send Bone Scale Changes" setting is enabled. 

Objects using the `VMCReceiverManager` behaviour will interpret these messages and adjust bone scale on their bound model to match!

#### Send Triggers
To support VNyan's unique and powerful Trigger system, NyaVMC provides message to communicate VNyan Trigger activations, making it possible for VNyan Node Graphs running on two different machines to operate together! This message takes the format `/NyaVMC/Trigger <triggerName> <value1> <value2> <value3> <text1> <text2> <text3>`.

The VMC Sender will send these packets whenever a VNyan trigger starting with `_xjvt:` using the trigger name (without the prefix), as well as the values provided to the value sockets of the called trigger, as long as the "Send _xjvt: Triggers" setting is enabled. 

Objects using the `VMCReceiverManager` behaviour will interpret these messages and fire the appropriate trigger with the appropriate values in the VNyan instacne where it is running.  

For example, if the Sender fires a trigger called `_xjvt:some_trigger`, the Receiver would call a trigger called `some_trigger` with the provided values

![Diagram illustrating sender/receiver nodes](https://github.com/user-attachments/assets/edc3f7cd-b520-4c81-8e6c-09b85553a605)

#### Send Animation Parameters
Since VMC is platform-agnostic and Unity's Animator system is proprietary, VMC does not provide a way to communicate when an animation parameter on a model has changed.  To address this, NyaVMC provides messages for each Parameter type:

- `/NyaVMC/Ext/Anim/IntParam <objectPath> <parameterHash> <value>`
- `/NyaVMC/Ext/Anim/FloatParam <objectPath> <parameterHash> <value>`
- `/NyaVMC/Ext/Anim/BoolParam <objectPath> <parameterHash> <value>`
- `/NyaVMC/Ext/Anim/TriggerParam <objectPath> <parameterHash>`

The VMC Sender will send these packets whenever an animation Parameter on any animator on your avatar changes, as long as the "Send Animation Parameters" setting is enabled. 

Objects using the `VMCReceiverManager` behaviour will interpret these messages and set the parameters on the same animators on their bound model to match! 

`VMCReceiverManager` also disables any VNyan `AnimParamLink` components on the bound model to prevent conflicts and keep sync with the sender

## Development
(Almost) Everything you'll need to develop a fork of this plugin (or some other plugin based on this one)!  The main VS project contains all of the code for the plugin DLL, and the `dist` folder contains a `unitypackage` that can be dragged into a project to build and modify the UI and export the modified Custom Object.

It's worth noting that per VNyan's requirements, this plugin is built under **Unity 2020.3.40f1** , so you'll need to develop on this version to maintain compatability with VNyan.
You'll also need the [VNyan SDK](https://suvidriel.itch.io/vnyan) imported into your project for it to function properly.
Your Visual C# project will need to mave the paths to all dependencies updated to match their locations on your machine.  Most should point to Unity Engine libraries for the correct Engine version **2020.3.40f1**.

## Special Thanks
Suvidriel for building and maintaining VNyan (and answering my endless questions)

sh-akira and all of the contributors to the VMC protocol

Redd for collaboration in testing, QA, and debugging 
