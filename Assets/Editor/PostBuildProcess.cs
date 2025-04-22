// Copyright 2022-2025 Niantic.
#if UNITY_IOS && UNITY_EDITOR_OSX
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor.iOS.Xcode;


public class PostBuildProcess : MonoBehaviour
{
    [PostProcessBuild(2)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            BuildForIos(path);
        }
    }

    private static void BuildForIos(string path)
    {
        var plistPath = path + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));
        var rootDict = plist.root;
        rootDict.SetString("UIFileSharingEnabled", "YES");
        rootDict.SetString("LSSupportsOpeningDocumentsInPlace", "YES");
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}


#endif