using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;
using UnityEngine;

//minimap manager resides in room base
//so it appear before envbase, so it can't be dependent on env base
//

public class MiniMapManager : MonoBehaviour
{
    [SerializeField] private GameObject miniMapPrefab;
    public Sprite firstIcon, normalIcon, lastIcon;
    private readonly List<MiniMap> miniMaps = new();

    private void Start()
    {
        EnvBase.Initiated += OnEnvInitiated;
    }

    private void OnDestroy()
    {
        EnvBase.Initiated -= OnEnvInitiated;
    }

    private void OnEnvInitiated()
    {
        EnvBase.I.GamePrepared += () => StartCoroutine(Begin());
    }

    private IEnumerator Begin()
    {
        yield return new WaitForFixedUpdate();

        foreach (var playerBase in EnvBase.I.Players)
        {
            var miniMap = Instantiate(miniMapPrefab, transform).GetComponent<MiniMap>();
            miniMaps.Add(miniMap);

            miniMap.Init(playerBase);
            playerBase.MovedADigit += () => OnMovedADigit(miniMap, playerBase);
            OnMovedADigit(miniMap, playerBase);
            //set the initial values of each minimap
        }
    }

    private void OnMovedADigit(MiniMap miniMap, PlayerBase playerBase)
    {
        SetFill(miniMap, (float)playerBase.textPointer / (EnvBase.I.Text.Length - 1));
    }

    private void SetFill(MiniMap miniMap, float percent)
    {
        miniMap.SetFill(percent);
        SetIcons();
    }

    private void SetIcons()
    {
        var ordered = miniMaps.OrderByDescending(m => m.slider.value).ToList();

        ordered[0].slideIcon.sprite = firstIcon;
        for (var i = 1; i < ordered.Count - 1; i++)
            ordered[i].slideIcon.sprite = normalIcon;
        ordered[^1].slideIcon.sprite = lastIcon;
    }
}