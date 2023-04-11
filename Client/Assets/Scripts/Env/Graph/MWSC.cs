using System.Linq;
using UnityEngine;

public abstract class GraphShootEnv : GraphEnv
{
}

public class MWSC : GraphShootEnv
{
    protected override void SetPlayersInitialPoz()
    {
        var x = .5f;
        var z = 1;
        foreach (var p in Players.Where(p => !p.GetComponent<PlayerController>()))
        {
            p.GetComponent<GWSPlayer>().offset = new Vector3(x, 0, -z);

            x *= -1;
            z++;
        }

        Players.Single(p => p.GetComponent<ShootController>()).GetComponent<ShootController>().Player.offset = Vector3.zero;
    }
}