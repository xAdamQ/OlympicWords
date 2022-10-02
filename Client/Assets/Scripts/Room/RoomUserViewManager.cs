// using Zenject;
// using System.Collections;

// public interface IRoomUserViewManager
// {
//     void CreateUserViews();
// }

// public class RoomUserViewManager : IRoomUserViewManager
// {
//     [Inject] private readonly RoomUserView.Factory _roomUserViewFactory;

//     public void CreateUserViews()
//     {
//         _roomUserViewFactory.Create(0, _repository.PersonalFullInfo);

//         for (var i = 1; i < _EnvBase.Capacity; i++)
//         {
//             _roomUserViewFactory.Create(i, _EnvBase.OpposInfo[i - 1].FullUserInfo);
//         }
//     }
// }