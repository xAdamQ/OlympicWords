using System.Linq;
using UnityEngine;

public abstract class GraphShootEnv : GraphEnv
{
    protected override void SetPlayersInitialPoz()
    {
        var x = .5f;
        var z = 1;
        foreach (var p in Players.Where(p => !p.IsMine))
        {
            p.GetComponent<GWSPlayer>().offset = new Vector3(x, 0, -z);

            x *= -1;
            z++;
        }
    }
}

public class MWSC : GraphShootEnv
{
}