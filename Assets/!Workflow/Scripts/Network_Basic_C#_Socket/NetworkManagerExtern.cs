using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkManagerExtern : MonoBehaviour
{
    [DllImport("udp.dylib")]
    public static extern int SetServer();

    public void DoServer()
    {
        int result = SetServer();

        Debug.Log($"Server Result : {result}");
    }
}