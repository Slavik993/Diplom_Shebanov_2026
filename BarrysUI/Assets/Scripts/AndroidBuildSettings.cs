#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class AndroidBuildSettings
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetScenes();
        buildPlayerOptions.locationPathName = "Builds/Android/JetPackJoyride.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        ConfigureAndroidSettings();
        
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Android build succeeded: " + summary.totalSize + " bytes");
            Debug.Log("Build completed at: " + summary.buildEndedAt);
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Android build failed");
        }
    }
    
    private static string[] GetScenes()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
    
    private static void ConfigureAndroidSettings()
    {
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        
        PlayerSettings.companyName = "JetPack Games";
        PlayerSettings.productName = "JetPack Joyride";
        PlayerSettings.applicationIdentifier = "com.jetpackgames.jetpackjoyride";
        
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        PlayerSettings.Android.minifyRelease = true;
        PlayerSettings.Android.minifyDebug = false;
        
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.stripUnusedMeshComponents = true;
        // PlayerSettings.optimizeMeshData = true; // Deprecated in Unity 6000
        
        QualitySettings.SetQualityLevel(2);
        
        PlayerSettings.Android.useCustomKeystore = false;
        
        // PlayerSettings.Android.blendShapesNormal = true; // Deprecated
        // PlayerSettings.Android.graphicsAPI = new UnityEngine.Rendering.GraphicsDeviceType[] { 
        //     UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 
        // }; // Deprecated
        
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        
        // PlayerSettings.Android.splitAPKs = false; // Deprecated
        
        // PlayerSettings.Android.renderOutsideSafeArea = true; // Deprecated
        
        // PlayerSettings.Android.androidTVCompatibility = false; // Deprecated
        
        // PlayerSettings.Android.forceSDCardPermission = false; // Deprecated
        
        // PlayerSettings.Android.enableProfiler = false; // Deprecated
    }
    
    [MenuItem("Build/Configure for Techno Spark 30")]
    public static void ConfigureForTechnoSpark30()
    {
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 10f;
        QualitySettings.lodBias = 0.3f;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        
        // PlayerSettings.Android.targetFrameRate = 60; // Use Application.targetFrameRate instead
        Application.targetFrameRate = 60;
        
        // PlayerSettings.Android.use32BitDisplayBuffer = false; // Deprecated
        
        // PlayerSettings.Android.disableAudio = false; // Deprecated
        
        // PlayerSettings.Android.internetAccess = AndroidInternetAccess.Require; // Deprecated
        
        Debug.Log("Project configured for Techno Spark 30 with HiOS v14.5.0");
    }
    
    [MenuItem("Build/Optimize for Performance")]
    public static void OptimizeForPerformance()
    {
        Time.fixedDeltaTime = 1f / 60f;
        
        QualitySettings.particleRaycastBudget = 64;
        QualitySettings.asyncUploadTimeSlice = 2;
        QualitySettings.asyncUploadBufferSize = 4;
        
        PlayerSettings.MTRendering = true;
        
        PlayerSettings.Android.minifyRelease = true;
        PlayerSettings.Android.minifyDebug = false;
        
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.stripUnusedMeshComponents = true;
        // PlayerSettings.optimizeMeshData = true; // Deprecated in Unity 6000
        
        Debug.Log("Performance optimization applied");
    }
}
#endif
