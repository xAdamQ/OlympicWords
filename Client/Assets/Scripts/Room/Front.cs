using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Front : MonoBehaviour
{
    public int Index;

    public static async UniTask<Front> Create(int index, Transform parent)
    {
        var front = (await Addressables.InstantiateAsync("front", parent)).GetComponent<Front>();

        await Extensions.LoadAndReleaseAsset<Sprite>($"frontSprites[nums_{index}]",
            sprite => front.GetComponent<SpriteRenderer>().sprite = sprite);

        //init, no init method because this class can access private members
        front.Index = index;
        front.transform.localPosition = Vector3.forward * .01f;

        return front;
    }
}