using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

[Rpc]
public class LevelUpPanel : MonoModule<LevelUpPanel>
{
    [SerializeField] private TMP_Text levelText, moneyRewardText;

    //money is added to the whole personal info object is updated on finalize 
    [Rpc]
    public static void LevelUp(int newLevel, int moneyReward)
    {
        UniTask.Create(async () =>
        {
            Instantiate(Coordinator.I.References.LevelUpView, Coordinator.I.canvas);

            I.levelText.text = newLevel.ToString();
            I.moneyRewardText.text = moneyReward.ToString();
        });
    }
}