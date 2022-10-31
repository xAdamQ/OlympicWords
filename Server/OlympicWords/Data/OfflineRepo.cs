using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using OlympicWords.Common;
using Microsoft.EntityFrameworkCore;
using OlympicWords.Data;
using OlympicWords.Services.Helpers;

namespace OlympicWords.Services
{
    public interface IOfflineRepo
    {
        Task<User> CreateUserAsync(User user);

        Task<string> GetNameOfUserAsync(string id);

        // bool GetUserActiveState(string id);
        Task<User> GetUserByEIdAsync(string eId, int eIdType);

        Task<User> GetUserByIdAsyc(string id, bool track = true,
            bool withFollowings = false, bool withFollowers = false);
        Task<User> GetCurrentUser();

        // void MarkAllUsersNotActive();
        Task<bool> SaveChangesAsync();
        // List<DisplayUser> GetRoomDisplayUsersAsync(Room room);

        //
        // // Task<Room> GetPendingRoomWithSpecs(int genre, int playerCount);
        // // Task<Room> CreatePendingRoom(int genre, int playerCount);
        // PendingRoom GetPendingRoomWithSpecs(int genre, int playerCount);
        // PendingRoom MakeRoom(int genre, int userCount);
        // RoomUser GetRoomUserWithId(string id);
        // void DeleteRoom(Room room);
        // RoomUser AddRoomUser(string id, string connId, PendingRoom pRoom);
        // void RemovePendingRoom(PendingRoom pendingRoom);
        //
        // List<DisplayUser> GetRoomDisplayUsersAsync(PendingRoom pendingRoom);
        // void StartRoomUser(RoomUser roomUser, int turnId, string roomId);
        Task<List<User>> GetUsersByIdsAsync(List<string> ids);
        Task<FullUserInfo> GetFullUserInfoAsync(string id);
        Task<List<FullUserInfo>> GetFullUserInfoListAsync(IEnumerable<string> ids);

        Task<List<MinUserInfo>> GetFollowingsAsync(User user);
        Task<List<MinUserInfo>> GetFollowersAsync(User user);
        Task ToggleFollow(User user, User target);
        Task<FriendShip> GetFriendship(string userId, string targetId);
        Task CreateExternalId(ExternalId externalId);

        string GetRandomRoomWords(int category);
        Task<byte[]> GetUserPicture(string userId);
        Task SaveUserPicture(string userId, byte[] picture);
        Task UpdateUserPicture(string userId, byte[] picture);
        Task<List<string>> IdsByProviderIds(List<string> providerIds);
    }

    /// <summary>
    /// hide queries, reuse queries
    /// </summary>
    public class OfflineRepo : IOfflineRepo
    {
        private readonly MasterContext context;
        private readonly IScopeRepo scopeRepo;
        private readonly ILogger<IOfflineRepo> logger;

        public async Task<User> GetCurrentUser()
        {
            if (user != null)
                return user;

            user = await GetUserByIdAsyc(scopeRepo.ActiveUser.Id);

            return user;
        }

        private User user;

        private async Task<byte[]> GetAvatar(int id)
        {
            var absPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", id + ".jpg");
            return await File.ReadAllBytesAsync(absPath);
        }

        public async Task<byte[]> GetUserPicture(string userId)
        {
            var picRecord = await context.UserPictures.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
            //it is possible that some users have no pics, no using facebook provider for sure

            if (picRecord == null)
                return await GetAvatar(1);
            //you can have some keyboard related default avatar

            if (picRecord.AvatarId != 0)
                return await GetAvatar(picRecord.AvatarId);

            return picRecord.Picture;
        }

        public async Task SaveUserPicture(string userId, byte[] picture)
        {
            await context.UserPictures.AddAsync(new()
            {
                UserId = userId,
                Picture = picture,
            });
        }

        public async Task UpdateUserPicture(string userId, byte[] picture)
        {
            context.UserPictures.Update(new UserPicture
            {
                UserId = userId,
                Picture = picture,
            });
        }


        public OfflineRepo(MasterContext context, IScopeRepo scopeRepo,
            ILogger<IOfflineRepo> logger)
        {
            this.context = context;
            this.scopeRepo = scopeRepo;
            this.logger = logger;
        }

        // private string[] testSentences =
        // {
        //     "He swung back the fishing pole and cast the line which ell 25 feet away into the river. The lure landed in the perfect spot and he was sure he would soon get a bite. He never expected that the bite would come from behind in the form of a bear.",
        //     "There was nothing to indicate Nancy was going to change the world. She looked like an average girl going to an average high school. It was the fact that everything about her seemed average that would end up becoming her superpower.",
        //     "Twenty-five stars were neatly placed on the piece of paper. There was room for five more stars but they would be difficult ones to earn. It had taken years to earn the first twenty-five, and they were considered the easy ones.",
        //     "The lone lamp post of the one-street town flickered, not quite dead but definitely on its way out. Suitcase by her side, she paid no heed to the light, the street or the town. A car was coming down the street and with her arm outstretched and thumb in the air, she had a plan.",
        //     "Bryan h ad made peace with himself and felt comfortable with the choices he made. This had made all the difference in the world. Being alone no longer bothered him and this was essential since there was a good chance he might spend the rest of his life alone in a cell.",
        // };   
        //
        private string[] testSentences =
        {
            "he swung back the fishing pole and cast the line which ell feet away into the river the lure landed in the perfect spot and he was sure he would soon get a bite he never expected that the bite would come from behind in the form of a bear",
            "there was nothing to indicate nancy was going to change the world she looked like an average girl going to an average high school it was the fact that everything about her seemed average that would end up becoming her superpower",
            "twenty five stars were neatly placed on the piece of paper there was room for five more stars but they would be difficult ones to earn it had taken years to earn the first twenty five and they were considered the easy ones",
            "the lone lamp post of the one street town flickered not quite dead but definitely on its way out suitcase by her side she paid no heed to the light the street or the town",
            "bryan had made peace with himself and felt comfortable with the choices he made this had made all the difference in the world being alone no longer bothered him and this was essential",
            "last week we installed a kitty door so that our cat could come and go as she pleases unfortunately we ran into a problem our cat was afraid to use the kitty door we tried pushing her through and that caused her to be even more afraid",
            "the kitty door was dark and she could not see what was on the other side the first step we took in solving this problem was taping the kitty door open after a couple of days she was confidently coming and going through the open door",
            "however when we removed the tape and closed the door once again she would not go through they say you catch more bees with honey so we decided to use food as bait",
            "we would sit next to the kitty door with a can of wet food and click the top of the can when kitty came through the closed door we would open the can and feed her",
            "it took five days of doing this to make her unafraid of using the kitty door now we have just one last problem our kitty controls our lives",
            "people often install a kitty door only to discover that they have a problem the problem is their cat will not use the kitty door there are several common reasons why cats wo not use kitty doors",
            "i do well in school and people think i am smart because of it but its not true in fact three years ago i struggled in school however two years ago i decided to get serious about school and made a few changes",
            "first i decided i would become interested in whatever was being taught regardless of what other people thought i also decided i would work hard every day and never give up on any assignment",
            "i decided to never never fall behind finally i decided to make school a priority over friends and fun after implementing these changes i became an active participant in classroom discussions",
            "then my test scores began to rise i still remember the first time that someone made fun of me because i was smart how exciting it seems to me that being smart is simply a matter of working hard and being interested",
            "after all learning a new video game is hard work even when you are interested unfortunately learning a new video game does not help you get into college or get a good job",
            "oceans and lakes have much in common but they are also quite different both are bodies of water but oceans are very large bodies of salt water while lakes are much smaller bodies of fresh water",
            "lakes are usually surrounded by land while oceans are what surround continents both have plants and animals living in them the ocean is home to the largest animals on the planet whereas lakes support much smaller forms of life",
            "the blue whales just played their first baseball game of the new season i believe there is much to be excited about although they lost it was against an excellent team that had won the championship last year",
        };

        // private string[] testSentences =
        // {
        //     "the kitty door was dark and she could not see what was on the other side the first step we took in solving this problem was taping the kitty door open after a couple of days she was confidently coming and going through the open door",
        // };

        // private string[] testSentences =
        // {
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river",
        //     // "There was nothing to indicate Nancy was going to change the world",
        //     // "Twenty five stars were neatly placed on the piece of paper",
        //     "The lone lamp post of the one street town flickered",
        //     // "Bryan had made peace with himself and felt comfortable with the choices he made Bryan had made peace with himself and felt comfortable with the choices he made Bryan had made peace with himself and felt comfortable with the choices he made ",
        //     //////////-------------------------
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river There was nothing to indicate Nancy was going to change the world " +
        //     // "Twenty five stars were neatly placed on the piece of paper The lone lamp post of the one street town flickered Bryan had made peace with himself and felt comfortable with the choices he made" +
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river There was nothing to indicate Nancy was going to change the world " +
        //     // "Twenty five stars were neatly placed on the piece of paper The lone lamp post of the one street town flickered Bryan had made peace with himself and felt comfortable with the choices he made" +
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river There was nothing to indicate Nancy was going to change the world " +
        //     // "Twenty five stars were neatly placed on the piece of paper The lone lamp post of the one street town flickered Bryan had made peace with himself and felt comfortable with the choices he made" +
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river There was nothing to indicate Nancy was going to change the world " +
        //     // "Twenty five stars were neatly placed on the piece of paper The lone lamp post of the one street town flickered Bryan had made peace with himself and felt comfortable with the choices he made" +
        //     // "He swung back the fishing pole and cast the line which ell feet away into the river There was nothing to indicate Nancy was going to change the world " +
        //     // "Twenty five stars were neatly placed on the piece of paper The lone lamp post of the one street town flickered Bryan had made peace with himself and felt comfortable with the choices he made",
        // };


        public string GetRandomRoomWords(int category)
        {
            return testSentences[StaticRandom.GetRandom(testSentences.Length)];
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() >= 0;
        }

        #region user

        public async Task CreateExternalId(ExternalId externalId)
        {
            await context.AddAsync(externalId);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await context.AddAsync(user);
            //await _context.Users.AddAsync(user);
            return user;
        }

        public async Task<User> GetUserByEIdAsync(string eId, int eIdType)
        {
            var externalId = await context.ExternalIds
                .Include(i => i.User)
                .ThenInclude(u => u.Followers)
                .ThenInclude(u => u.Followings)
                .FirstOrDefaultAsync(id => id.Id == eId);

            return externalId?.User;

            return await context.Users.Join(
                context.ExternalIds.Where(id => id.Type == eIdType && id.Id == eId),
                u => u.Id, id => id.UserId,
                (u, _) => u).FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByIdAsyc(string id, bool track = true,
            bool withFollowings = false, bool withFollowers = false)
        {
            var userQ = context.Users;

            if (!track)
                userQ.AsNoTracking();

            if (withFollowings)
                userQ.Include(u => u.Followings);

            if (withFollowers)
                userQ.Include(u => u.Followers);

            return await userQ.FirstAsync(u => u.Id == id);
        }

        public async Task<List<User>> GetUsersByIdsAsync(List<string> ids)
        {
            return await context.Users.Where(u => ids.Contains(u.Id)).Take(ids.Count)
                .ToListAsync();
        }

        public async Task<string> GetNameOfUserAsync(string id)
        {
            var q = context.Users.Where(u => u.Id == id).Select(u => u.Name);

            return await context.Users.Where(u => u.Id == id).Select(u => u.Name).FirstAsync();
        }

        public async Task<FullUserInfo> GetFullUserInfoAsync(string id)
        {
            return await context.Users.Where(_ => _.Id == id)
                .Select(Mapper.UserToFullUserInfoProjection).FirstAsync();
        }

        public async Task<List<FullUserInfo>> GetFullUserInfoListAsync(IEnumerable<string> ids)
        {
            return await context.Users.Where(u => ids.Contains(u.Id)).Take(ids.Count())
                .Select(Mapper.UserToFullUserInfoProjection)
                .ToListAsync();
        }

        public async Task<List<string>> IdsByProviderIds(List<string> providerIds)
        {
            return await context.ExternalIds
                .Where(i => providerIds.Any(given => given == i.Id))
                .Select(i => i.UserId)
                .ToListAsync();
        }

        #endregion

        #region user relation

        public async Task ToggleFollow(User user, User target)
        {
            user.Followings ??= new List<User>();

            if (user.Followings.Contains(target))
                user.Followings.Remove(target);
            else
                user.Followings.Add(target);
        }

        /// <summary>
        /// Am I follower
        /// </summary>
        public async Task<bool> IsFollowing(string userId, string targetId)
        {
            return await context.Users
                .AnyAsync(u => u.Id == userId && u.Followings.Any(f => f.Id == targetId));

            //you have some follower for a, some following for b
            // var q = context.Users.Where(u =>
            //     u.Id == userId && u.Followers.Any(f => f.Id == followerId));
            //
            // logger.LogInformation(q.ToQueryString());
            //
            // return await q.AnyAsync();

            // return context.Users.First(u => u.Id == userId).Followers.Any(u => u.Id == targetId);
        }

        // /// <summary>
        // /// Is HE following me
        // /// </summary>
        // public bool IsFollowing(string userId, string targetId)
        // {
        //     return context.UserRelations.Any(r =>
        //         r.FollowingId == userId && r.FollowerId == targetId);
        // }

        public async Task<FriendShip> GetFriendship(string userId, string targetId)
        {
            var isFollowing = await IsFollowing(userId, targetId);
            var isFollower = await IsFollowing(targetId, userId);

            if (isFollower && isFollowing)
                return FriendShip.Friend;
            if (isFollower)
                return FriendShip.Follower;
            if (isFollowing)
                return FriendShip.Following;

            return FriendShip.None;
        }

        public async Task<List<MinUserInfo>> GetFollowingsAsync(User user)
        {
            return user.Followings
                .Select(Mapper.UserToMinUserInfoFunc)
                .ToList();

            // var relationsWhereIFolllow = context.UserRelations.Where(u => u.FollowerId == userId);
            //
            // var myFollowingInfo = relationsWhereIFolllow.Join(context.Users,
            //     relation => relation.FollowingId, u => u.Id,
            //     (_, u) => Mapper.UserToMinUserInfoFunc(u));
            //
            // return await myFollowingInfo.ToListAsync();
        }

        public async Task<List<MinUserInfo>> GetFollowersAsync(User user)
        {
            return user.Followers
                .Select(Mapper.UserToMinUserInfoFunc)
                .ToList();

            // var relationsWhereIFolllow = context.UserRelations.Where(u => u.FollowingId == userId);
            //
            // var myFollowingInfo = relationsWhereIFolllow.Join(context.Users,
            //     relation => relation.FollowerId, u => u.Id,
            //     (_, u) => Mapper.UserToMinUserInfoFunc(u));
            //
            // return await myFollowingInfo.ToListAsync();
        }

        #endregion
    }
}