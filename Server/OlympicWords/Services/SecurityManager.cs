using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Web;
using Newtonsoft.Json.Linq;
using OlympicWords.Data;
using OlympicWords.Services.Helpers;
using OlympicWords.Services.Models;
using Shared;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OlympicWords.Services
{
    public class ProviderPublicUser
    {
        public string Id { get; set; }
        public ProviderType Provider { get; set; }
    }

    public class ProviderUser : ProviderPublicUser
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public List<ProviderPublicUser> Friends { get; set; }
        public string PictureUrl { get; set; }
    }

    public class FbDataResponse<T>
    {
        public List<T> Data { get; set; }
    }

    public class SecurityManager
    {
        private static readonly JsonSerializerOptions serializationOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        //I think the issuer/audience is important
        //it defines who can use the token and the token maker
        private readonly IOfflineRepo offlineRepo;
        private readonly ILogger<SecurityManager> logger;
        private readonly IScopeRepo scopeRepo;

        private readonly string
            figAppSecret,
            fbAppToken;

        public SecurityManager(IConfiguration configuration, IOfflineRepo offlineRepo,
            ILogger<SecurityManager> logger, IScopeRepo scopeRepo)
        {
            this.offlineRepo = offlineRepo;
            this.logger = logger;
            this.scopeRepo = scopeRepo;

            figAppSecret = configuration["Secrets:AppSecret"];
            fbAppToken = configuration["Secrets:FbAppToken"];
        }

        // public async Task<User> GetDiskUser(ProviderUser providerUser)
        // {
        //     if (providerUser.Id == null)
        //         throw new BadUserInputException();
        //
        //     var user = await offlineRepo.GetUserAsync(providerUser.Id, providerUser.Provider);
        //
        //     return user;
        // }

        public async Task SignInAsync(ProviderUser providerUser, User user)
        {
            logger.LogInformation("sign in attempt of {EId} -- named: {Name} -- isNull? {IsNull}",
                providerUser.Id, providerUser.Name, user == null);

            await SetProviderFriends(user, providerUser.Friends);

            await UpdateUserData(user, providerUser);

            //the last login resolution is 2 hours minimum
            if (DateTime.Now - user!.LastLogin > TimeSpan.FromHours(2))
                user!.LastLogin = DateTime.Now;

            await offlineRepo.SaveChangesAsync();
        }

        private async Task UpdateUserData(User user, ProviderUser providerUser)
        {
            if (DateTime.Now - user.LastLogin < TimeSpan.FromDays(2)) return;

            user.Name = providerUser.Name;
            user.PictureUrl = providerUser.PictureUrl;

            var imageBytes = await TryGetImage(providerUser.PictureUrl);
            if (imageBytes != null)
                offlineRepo.UpdateUserPicture(user.Id, imageBytes);

            await offlineRepo.SaveChangesAsync();
        }

        private async Task SetProviderFriends(User user, List<ProviderPublicUser> friends)
        {
            if (friends == null) return;

            var friendsIds = await offlineRepo.IdsByProviderIds(friends.Select(f => f.Id).ToList());

            var newFriends = friendsIds
                .Where(fi => user.Followers.All(current => current.Id != fi))
                .Select(fi => new User { Id = fi }).ToList();

            user.Followers.AddRange(newFriends);
            user.Followings.AddRange(newFriends);
        }

        public async Task<User> SignUpAsync(ProviderUser providerUser)
        {
            var user = await offlineRepo.CreateUserAsync(new User
            {
                Name = providerUser.Name,
                PictureUrl = providerUser.PictureUrl,
                Email = providerUser.Email,
                Money = 9999999,
                EnableOpenMatches = true,
                OwnedBackgroundIds = new List<int> { 0 },
                OwnedTitleIds = new List<int> { 0 },
                OwnedCardBackIds = new List<int> { 0 },
                LastLogin = DateTime.Now,
                Followers = new(),
                Followings = new(),
                AverageWpm = 10f,
                SelectedItemPlayer = new Dictionary<string, string>
                {
                    {
                        "GraphJumpCity", OfflineRepo.ItemPlayers.First().Value.Id
                    }
                },
                OwnedItemPlayers = new HashSet<string>
                {
                    OfflineRepo.ItemPlayers.First().Value.Id,
                },
            });

            // offlineRepo.MarkCurrentUserPropertyModified(u => u.SelectedItemPlayer);
            // offlineRepo.MarkCurrentUserPropertyModified(u => u.OwnedItemPlayers);

            scopeRepo.SetRealOwner(user.Id);

            await LinkUser(user.Id, providerUser);

            var imageBytes = await TryGetImage(providerUser.PictureUrl);
            if (imageBytes != null)
                await offlineRepo.SaveUserPicture(user.Id, imageBytes);

            await offlineRepo.ToggleFollow("999", user);
            await offlineRepo.ToggleFollow("9999", user);

            await offlineRepo.SaveChangesAsync();

            return user;
        }

        public async Task LinkUser(string userId, ProviderUser providerUser)
        {
            await offlineRepo.CreateProviderLink(new ProviderLink
            {
                UserId = userId,
                Type = (int)providerUser.Provider,
                Id = providerUser.Id,
            });
        }

        public async Task<ProviderUser> GetProfile(ProviderType provider, string accessToken)
        {
            switch (provider)
            {
                case ProviderType.Guest:
                    return new ProviderUser
                    {
                        Id = accessToken,
                        Name = accessToken[..8],
                        Provider = ProviderType.Guest,
                    };
                case ProviderType.Facebook:
                    return await GetFbProfile(accessToken);
                case ProviderType.Huawei:
                    // var token = await SecurityManager.GetTokenByHuaweiAuthCode(accessToken);
                    // var userData = await SecurityManager.GetHuaweiUserDataByToken(token);
                    // user = await securityManager.SignInAsync(userData);
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
            }
        }

        #region facebook
        private const string FB_BASE_ADDRESS = "https://graph.facebook.com/v15.0/";

        /// <exception cref="FbApiError"></exception>
        /// <exception cref="Exceptions.BadUserInputException"></exception>
        public async Task<bool> ValidateFbAccToken(string token)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams.Add("input_token", token);
            queryParams.Add("access_token", fbAppToken);

            const string address = FB_BASE_ADDRESS + "debug_token";

            var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

            using var client = new HttpClient();

            var response = await client.GetAsync(uri);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.error is not null)
                throw new FbApiError(obj.error.message.ToString());
            //this is my issue not client's

            var data = obj.data;

            if (data is null)
                throw new FbApiError("fb response is null: " + result);
            //this is an issue, could be me or client or facebook or something I didn't plan to

            if (data.error is not null)
                throw new Exceptions.BadUserInputException(data.error.message.ToString());

            return data.is_valid;
            //todo add check for timout
        }

        public static async Task<ProviderUser> GetFbProfile(string token)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams.Add("fields", "id,name,email");
            queryParams.Add("access_token", token);

            const string address = FB_BASE_ADDRESS + "me";

            var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

            using var client = new HttpClient();

            var response = await client.GetAsync(uri);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.error is not null)
                throw new Exceptions.BadUserInputException(obj.error.message.ToString());
            //this is client issue if the token is invalid, but this is impossible
            //because I validate it first

            var pictureUrl =
                "https://graph.facebook.com/me/picture?width=128&height=128&access_token=" + token;

            var friends = await GetFbFriends(token);

            return new ProviderUser
            {
                Id = obj.id,
                Name = obj.name,
                Email = obj.email,
                PictureUrl = pictureUrl,
                Provider = ProviderType.Facebook,
                Friends = friends,
            };
        }

        public static async Task<List<ProviderPublicUser>> GetFbFriends(string token)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams.Add("fields", "id");

            queryParams.Add("access_token", token);

            const string address = FB_BASE_ADDRESS + "me/friends";

            var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

            using var client = new HttpClient();

            var response = await client.GetAsync(uri);

            var result = response.Content.ReadAsStringAsync().Result;

            var friends = JsonSerializer
                .Deserialize<FbDataResponse<ProviderPublicUser>>(result, serializationOptions)?
                .Data;
            //for other providers with non matching names, map fields for serialization!

            friends?.ForEach(f => f.Provider = ProviderType.Facebook);

            return friends ?? new();
        }

        private async Task<byte[]> TryGetImage(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl)) return null;

            try
            {
                using var httpClient = new HttpClient();
                return await httpClient.GetByteArrayAsync(imageUrl);
            }
            catch (Exception e)
            {
                logger.LogInformation("failed to download player pic due to error: {Exc}", e);
                return null;
            }
        }

        public class FbApiError : Exception
        {
            public FbApiError(string message) : base(message)
            {
            }
        }
        #endregion

        #region huawei
        public static async Task<string> GetTokenByHuaweiAuthCode(string code)
        {
            var data = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "client_id", "104645983" },
                {
                    "client_secret",
                    "70fa010a05f4a3c9fc389d0046cd69cacc7b50f8d26b09e65940c9ef37abf416"
                },
                { "code", code },
                { "redirect_uri", "hms://redirect_uri" }
            });

            const string url = "https://oauth-login.cloud.huawei.com/oauth2/v3/token";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.access_token == null)
                throw new SecurityManager.HuaweiApiFailure(
                    "couldn't huawei token by the given code, with response: " + result);

            return obj.access_token;
        }

        public static async Task<ProviderUser> GetHuaweiUserDataByToken(string token)
        {
            var data = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "access_token", token },
                { "getNickName", "1" },
            });

            const string url = "https://account.cloud.huawei.com/rest.php?nsp_svc=GOpen.User.getInfo";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.openID == null)
                throw new HuaweiApiFailure("openID found null, with response: " +
                                           result);

            return new()
            {
                Id = obj.openID,
                Name = obj.displayName,
                PictureUrl = obj.headPictureURL,
                Provider = ProviderType.Huawei,
            };
        }

        [Serializable]
        public class HuaweiApiFailure : Exception
        {
            public HuaweiApiFailure(string message) : base(message)
            {
            }
        }
        #endregion


        #region fbig
        private bool VerifySignature(string[] token)
        {
            byte[] hash = null;
            using (var hmac = new HMACSHA256(Encoding.Default.GetBytes(figAppSecret)))
            {
                hash = hmac.ComputeHash(Encoding.Default.GetBytes(token[1]));
            }

            var hash64 = Base64UrlTextEncoder.Encode(hash);

            return hash64 == token[0];
        }

        private ConnectBody DeserializeConnectBody(string code)
        {
            var json = Encoding.Default.GetString(Base64UrlTextEncoder.Decode(code));
            return JsonConvert.DeserializeObject<ConnectBody>(json, Helper.SnakePropertyNaming);
        }

        private bool IsRecentConnection(int timestamp)
        {
            return true;
        }

        public bool ValidateToken(string token, out string playerId)
        {
            playerId = null;
            try
            {
                var tokenParts = token.Split('.');

                if (!VerifySignature(tokenParts))
                {
                    return false;
                }

                var connectBody = DeserializeConnectBody(tokenParts[1]);

                if (!IsRecentConnection(connectBody.IssuedAt))
                {
                    return false;
                }

                playerId = connectBody.PlayerId;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}