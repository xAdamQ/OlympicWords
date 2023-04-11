using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TestCut : MonoBehaviour
{
    public GameObject pref;
    public Vector3 start;
    IEnumerator Start()
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(5);
            Instantiate(pref, start, Quaternion.identity);
            start += Vector3.right * 1.5f;
        }
    }


    // private char[][] rows =
    // {
    //     new[]
    //     {
    //         '`', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '=',
    //     },
    //     new[]
    //     {
    //         'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\\',
    //     },
    //     new[]
    //     {
    //         'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', '\'',
    //     },
    //     new[]
    //     {
    //         'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/'
    //     },
    // };

  



    [ContextMenu("test")]
    public void Test()
    {
      
    }

}