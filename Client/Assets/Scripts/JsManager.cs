using System.Runtime.InteropServices;
using UnityEngine;

public class JsManager : MonoBehaviour
{
#if UNITY_WEBGL

    [DllImport("__Internal")]
    private static extern void Hello();

    [DllImport("__Internal")]
    private static extern void HelloString(string str);

    [DllImport("__Internal")]
    private static extern void PrintFloatArray(float[] array, int size);

    [DllImport("__Internal")]
    private static extern int AddNumbers(int x, int y);

    [DllImport("__Internal")]
    private static extern string StringReturnValueFunction();

    [DllImport("__Internal")]
    private static extern void BindWebGLTexture(int texture);


    [DllImport("__Internal")]
    public static extern string GetUserData();

    [DllImport("__Internal")]
    public static extern string GetFriends();

    [DllImport("__Internal")]
    public static extern void StartFbigGame();

    [DllImport("__Internal")]
    public static extern int IsFigSdkInit();

    [DllImport("__Internal")]
    public static extern string BackendAddress();

    [System.Obsolete]
    void Start()
    {
        Hello();

        HelloString("This is a string.");

        float[] myArray = new float[10];
        PrintFloatArray(myArray, myArray.Length);

        int result = AddNumbers(5, 7);
        Debug.Log(result);

        Debug.Log(StringReturnValueFunction());

        var texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
        BindWebGLTexture(texture.GetNativeTextureID());
    }
#endif

}