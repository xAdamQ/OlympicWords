using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Alachisoft.NCache.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using OlympicWords.Common;
using Microsoft.EntityFrameworkCore;
using OlympicWords.Data;
using Shared;
using Mapper = OlympicWords.Services.Helpers.Mapper;

namespace OlympicWords.Services
{
    public interface IOfflineRepo
    {
        Task<User> CreateUserAsync(User user);
        Task<User> GetUserAsync(string providerId, ProviderType providerType);
        Task<List<User>> GetUsersAsync(List<string> ids);
        Task<User> GetCurrentUserAsync();
        void SetCurrentUser(User user);
        void DeleteUserAsync(string id);

        Task<PersonalFullUserInfo> GetPersonalInfo(string providerId, ProviderType providerType);
        Task<PersonalFullUserInfo> GetPersonalInfo();
        Task<FullUserInfo> GetFullUserInfoAsync(string id);

        Task ToggleFollow(string target);
        Task<FriendShip> GetFriendship(string userId, string targetId);

        Task CreateProviderLink(ProviderLink providerLink);
        Task<List<string>> IdsByProviderIds(List<string> providerIds);

        Task<bool> SaveChangesAsync();

        Task<byte[]> GetUserPicture(string userId);
        Task SaveUserPicture(string userId, byte[] picture);
        void UpdateUserPicture(string userId, byte[] picture);

        string ChooseText(int category);
        string[] SmallFillers { get; }
        string[] MediumFillers { get; }
        string[] LargeFillers { get; }
    }

    /// <summary>
    /// hide queries, reuse queries
    /// </summary>
    public class OfflineRepo : IOfflineRepo
    {
        private readonly MasterContext context;
        private readonly IScopeRepo scopeRepo;
        private readonly ILogger<OfflineRepo> logger;

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
        public void UpdateUserPicture(string userId, byte[] picture)
        {
            context.UserPictures.Update(new UserPicture
            {
                UserId = userId,
                Picture = picture,
            });
        }

        public OfflineRepo(MasterContext context, IScopeRepo scopeRepo,
            ILogger<OfflineRepo> logger)
        {
            this.context = context;
            this.scopeRepo = scopeRepo;
            this.logger = logger;
        }

        private readonly string[] testSentences =
        {
            // "i do well in school",
            "i do well in school and people think i am smart because of it but its not true in fact three years ago i struggled in school however two years ago i decided to get serious about school and made a few changes",
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
            "first i decided i would become interested in whatever was being taught regardless of what other people thought i also decided i would work hard every day and never give up on any assignment",
            "i decided to never never fall behind finally i decided to make school a priority over friends and fun after implementing these changes i became an active participant in classroom discussions",
            "then my test scores began to rise i still remember the first time that someone made fun of me because i was smart how exciting it seems to me that being smart is simply a matter of working hard and being interested",
            "after all learning a new video game is hard work even when you are interested unfortunately learning a new video game does not help you get into college or get a good job",
            "oceans and lakes have much in common but they are also quite different both are bodies of water but oceans are very large bodies of salt water while lakes are much smaller bodies of fresh water",
            "lakes are usually surrounded by land while oceans are what surround continents both have plants and animals living in them the ocean is home to the largest animals on the planet whereas lakes support much smaller forms of life",
            "the blue whales just played their first baseball game of the new season i believe there is much to be excited about although they lost it was against an excellent team that had won the championship last year",
        };

        public string[] SmallFillers { get; } =
        {
            "actually", "just", "really", "totally", "simply", "somehow",
        };
        public string[] MediumFillers { get; } =
        {
            "kind of", "by the way", "basically"
        };
        public string[] LargeFillers { get; } =
        {
            "at the end of the day", "you know what i mean", "you know like i said",
            "something like that",
        };

        public string ChooseText(int category)
        {
            return testSentences[StaticRandom.GetRandom(testSentences.Length)];
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() >= 0;
        }

        private static readonly MapperConfiguration mapperConfig = new(cfg =>
        {
            cfg.CreateProjection<User, PersonalFullUserInfo>();
            cfg.CreateProjection<User, FullUserInfo>();
            cfg.CreateProjection<User, MinUserInfo>();
        });
        private static readonly CachingOptions cacheOptions = new()
        {
            StoreAs = StoreAs.SeperateEntities,
        };

        private User currentUser;
        public async Task<User> GetCurrentUserAsync()
        {
            if (currentUser != null)
                return currentUser;

            currentUser = await context.Users.FirstAsync(u => u.Id == scopeRepo.UserId);

            return currentUser;
        }

        public async Task<User> GetCurrentFullUserAsync()
        {
            if (currentUser is { Followers: not null, Followings: not null }) return currentUser;

            currentUser = await context.Users
                .Include(u => u.Followings)
                .Include(u => u.Followers)
                .FirstAsync(u => u.Id == scopeRepo.UserId);

            return currentUser;
        }


        public void SetCurrentUser(User user)
        {
            currentUser = user;
            scopeRepo.SetRealOwner(user.Id);
        }

        public async Task CreateProviderLink(ProviderLink providerLink)
        {
            await context.AddAsync(providerLink);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await context.AddAsync(user);
            //await _context.Users.AddAsync(user);
            return user;
        }
        public void DeleteUserAsync(string id)
        {
            context.Users.Remove(new User { Id = id });
        }

        public async Task<PersonalFullUserInfo> GetPersonalInfo(string providerId, ProviderType providerType)
        {
            var res = await context.Users
                .Where(u => u.Providers
                    .Any(provider => provider.Id == providerId && provider.Type == (int)providerType))
                .ProjectTo<PersonalFullUserInfo>(mapperConfig)
                .AsSplitQuery()
                .FromCacheAsync(cacheOptions);

            return res.Single();
        }
        public async Task<PersonalFullUserInfo> GetPersonalInfo()
        {
            // return await context.Users
            //     .Where(u => u.Id == scopeRepo.UserId)
            //     .ProjectTo<PersonalFullUserInfo>(mapperConfig)
            //     .AsSplitQuery()
            //     .SingleAsync();

            var a = (await context.Users
                    .Where(u => u.Id == scopeRepo.UserId)
                    // .AsSplitQuery()
                    .FromCacheAsync(cacheOptions))
                .Single();

            return Mapper.UserToClientUserFunc(a);

            var q = await context.Users
                .Where(u => u.Id == scopeRepo.UserId)
                .ProjectTo<PersonalFullUserInfo>(mapperConfig)
                .AsSplitQuery()
                .FromCacheAsync(cacheOptions);

            //I split the query because the projection was causing a join and making cartesian explosion

            // var q2 = context.Users
            //     .Where(u => u.Id == scopeRepo.UserId)
            //     .Select(Mapper.UserToClientUserProjection);
            //
            // logger.LogInformation("==========q1==========");
            // logger.LogInformation("==========q2==========");
            // logger.LogInformation(q2.ToQueryString());
            // //both queries are almost identical, I want to know why both automapper and microsoft are complaining

            return q.Single();
        }

        public async Task<User> GetUserAsync(string providerId, ProviderType providerType)
        {
            return await context.Users
                .Where(u => u.Providers
                    .Any(provider => provider.Id == providerId && provider.Type == (int)providerType))
                .SingleOrDefaultAsync();
        }
        public async Task<List<User>> GetUsersAsync(List<string> ids)
        {
            return await context.Users.Where(u => ids.Contains(u.Id)).Take(ids.Count)
                .ToListAsync();
        }

        public async Task<FullUserInfo> GetFullUserInfoAsync(string id)
        {
            return await context.Users.Where(u => u.Id == id)
                .ProjectTo<FullUserInfo>(mapperConfig)
                .SingleAsync();
        }

        public async Task<List<string>> IdsByProviderIds(List<string> providerIds)
        {
            return await context.ExternalIds
                .Where(i => providerIds.Any(given => given == i.Id))
                .Select(i => i.UserId)
                .ToListAsync();
        }

        #region user relation
        public async Task ToggleFollow(string targetId)
        {
            await GetCurrentFullUserAsync();

            currentUser.Followings ??= new List<User>();

            var target = new User { Id = targetId };
            context.Attach(target);

            var targetFollowing = currentUser.Followings.FirstOrDefault(u => u.Id == targetId);

            if (targetFollowing != null)
                currentUser.Followings.Remove(targetFollowing);
            else
                currentUser.Followings.Add(target);
        }

        private async Task<(bool IFollow, bool HeFollow)> GetFollowSate(string userId, string targetId)
        {
            var res = await context.Users.Where(u => u.Id == userId)
                .Select(me => new
                {
                    iFollow = me.Followings.Any(f => f.Id == targetId),
                    heFollow = me.Followers.Any(f => f.Id == targetId),
                }).SingleAsync();

            return (res.iFollow, res.heFollow);
        }

        public async Task<FriendShip> GetFriendship(string userId, string targetId)
        {
            var (iFollow, heFollow) = await GetFollowSate(userId, targetId);

            if (iFollow && heFollow)
                return FriendShip.Friend;
            if (heFollow)
                return FriendShip.Follower;
            if (iFollow)
                return FriendShip.Following;

            return FriendShip.None;
        }
        #endregion
    }
}