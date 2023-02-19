using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCut : MonoBehaviour
{
    public List<GameObject> Objects;
    void Start()
    {
        Objects.ForEach(o => AutoSlicer.Slice(o, 5f));
    }
}