using System;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform camera;

    private void Awake()
    {
        camera = Camera.main!.transform!;
    }

    private void Update()
    {
        transform.LookAt(camera);
    }
}