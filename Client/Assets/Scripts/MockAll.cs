using System;
using UnityEngine;
using Moq;
using Microsoft.QualityTools.Testing.Fakes;
public class MockAll:MonoBehaviour
{
    [SerializeField] private bool Enable;

    private void Awake()
    {
        NetManager.I = new Mock<NetManager>().Object;
    }
}