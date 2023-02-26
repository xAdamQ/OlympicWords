using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}