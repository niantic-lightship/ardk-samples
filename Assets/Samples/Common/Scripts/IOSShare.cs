using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_IOS
public class IOSShare
{
    [DllImport("__Internal")]
    private static extern void _ShareFile(string filePath, string message);

    public static void ShareFile(string filePath, string message = "")
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _ShareFile(filePath, message);
        }
        else
        {
            Debug.LogWarning("File sharing is only available on iOS.");
        }
    }
}
#endif