# VIVE CloudXR Unity Sample

Demonstrate how to program with NVIDIA CloudXR Client Unity Plugin for VIVE Focus 3 and VIVE XR Elite headset. You can start to develop your own CloudXR application based on this sample client. 

Below are the instructions to build from source. Alternatively you can find a pre-built APK in the [Releases]() section.

## Requirements
- HTC VIVE Focus 3 or VIVE XR Elite 
- Unity Minimum Version [2022.3.9f1 LTS](https://unity.com/releases/editor/archive) or later
    - Unity 2021.3.31f1 LTS has been tested and is functioning correctly.
- [VIVE OpenXR Plugin - Android 1.0.5](https://github.com/ViveSoftware/VIVE-OpenXR-AIO)
- [NVIDIA CloudXR Client Unity Plugin](https://developer.nvidia.com/nvidia-cloudxr-sdk)
- [XR Interaction Toolkit 2.5.4](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/manual/index.html)

## Settings & Build Setup
1. Switch to Android platform.
2. Load NVIDIA CloudXR Client Unity Plugin from `Window > Package Manager`.
	- the plugin is included labeled "sans-libc++"
3. Ensure **OpenXR** and **VIVE XR Support feature group** are checked from `Edit > Project Settings > XR Plug-in Management`.
4. Check and fix for any red or yellow flags from `Edit > Project Settings > XR Plug-in Management`.
5. Ensure **VIVE XR Support** and **CloudXR Tuned Pose Capture** are checked from `Edit > Project Settings > XR Plug-in Management > OpenXR`.
 
## Usage
1. Open Server and check server ip.
2. Modify the IP address in CloudXRLaunchOptions.txt.
3. Push CloudXRLaunchOptions.txt to /storage/emulated/0/Android/data/**PackageName**/files/CloudXRLaunchOptions.txt
4. Push cxrUnityConfig.json to /storage/emulated/0/Android/data/**PackageName**/files/cxrUnityConfig.json
    - PackageName from `Edit > Project Settings > Player > Other Settings > Package Name`
    - pre-built APK PackageName : `com.htc.vive.cloudxr.unitysample`
5. Launch the apk to start streaming.

## Notes
- If controller model position not fit, please follow these steps to fix it.
    1. Please open CxrUnityXRManager.cs from `Project > Packages > NVIDIA CloudXR Client for Unity > Runtime`
    2. Replace **/input/grip/pose** to  **/input/aim/pose**
- If you want to change the resolution, please follow these steps to fix it.
    1. Check the device's default resolution via XRSettings.eyeTextureHeight and XRSettings.eyeTextureWidth.
    2. Calculate the scale value between your desired resolution and the current device resolution, and set it in Assets/Settings/URP-Performant.asset under Quality > Render Scale