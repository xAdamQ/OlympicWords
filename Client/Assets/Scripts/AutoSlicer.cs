using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AutoSlicer
{
    public static void Slice(GameObject c, float saberLength)
    {
        // var bounds = c.GetComponent<MeshRenderer>().bounds;

        // var enterY = Mathf.Lerp(bounds.min.y, bounds.max.y, Random.Range(.25f, .75f));
        // var enterBase = new Vector3(bounds.min.x, enterY, bounds.center.z);
        // var enterTip =  
        // var exitTip = 

        var pos = c.transform.position;
        var forward = c.transform.forward;
        var right = c.transform.right;
        var up = c.transform.up;

        var tip1Dir = Vector3.Lerp(forward, right, .5f) + Vector3.Lerp(-up, up, Random.Range(.25f, .75f));
        var tip2Dir = Vector3.Lerp(-forward, right, .5f) + Vector3.Lerp(-up, up, Random.Range(.25f, .75f));

        var saberBase = pos - saberLength * right / 2f;
        var saberTip = pos + saberLength * tip1Dir / 2f;
        var saberTipExit = pos + saberLength * tip2Dir / 2f;

        // GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = saberBase;
        // GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = saberTip;
        // GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = saberTipExit;

        Lighsaber.Slice(c, saberTipExit, saberBase, saberTip, forward * 4);
    }
}