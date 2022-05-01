using Cysharp.Threading.Tasks;

public class FriendListChoiceButton : ChoiceButton
{
    public override void NextChoice()
    {
        base.NextChoice();
        FriendsView.I.ShowFriendList(CurrentChoice == 0)
            .Forget(e => throw e);
    }
}