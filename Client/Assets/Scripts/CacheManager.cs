using System.Collections.Generic;
using UnityEngine;

public class CacheManager : MonoModule<CacheManager>
{
    //whether to make it mono or initialize it with game starting because it this spans multiple sessions
    // public static CacheManager I { get; } = new();

    //it's a dictionary, but it has a max size, so you delete the oldest ones

    private const int playerPicsSize = 10;
    private readonly Dictionary<string, Sprite> playerPics = new();
    private readonly Queue<string> playerPicsQueue = new();
    public readonly HashSet<string> pendingPics = new();

    public void AddPlayerPic(string id, Sprite sprite)
    {
        if (playerPics.ContainsKey(id))
        {
            playerPics[id] = sprite;
        }
        else
        {
            if (playerPics.Count >= playerPicsSize)
                playerPics.Remove(playerPicsQueue.Dequeue());
            //remove the oldest one

            playerPics.Add(id, sprite);
            playerPicsQueue.Enqueue(id);
        }
    }

    public bool TryGetPic(string id, out Sprite pic)
    {
        return playerPics.TryGetValue(id, out pic);
    }
}