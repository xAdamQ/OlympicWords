using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Web;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using OlympicWords.Services.Exceptions;
using OlympicWords.Services.Helpers;
using OlympicWords.Services.Models;

//using Basra.Server.Identity;

namespace OlympicWords.Services
{
    public class SecurityManager
    {
        //I think the issuer/audiance is important
        //it defines who can use the token and the token maker
        private readonly IConfiguration configuration;
        private readonly IOfflineRepo offlineRepo;
        private ILogger<SecurityManager> logger;

        private readonly string
            figAppSecret,
            fbAppToken;

        public SecurityManager(IConfiguration configuration, IOfflineRepo offlineRepo, ILogger<SecurityManager> logger)
        {
            this.configuration = configuration;
            this.offlineRepo = offlineRepo;
            this.logger = logger;

            figAppSecret = this.configuration["Secrets:AppSecret"];
            fbAppToken = this.configuration["Secrets:FbAppToken"];
        }

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

        private ConnectBody DeserialzeConnectBody(string code)
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

                var connectBody = DeserialzeConnectBody(tokenParts[1]);

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

        /// <summary>
        /// checks if the user exist and make a new one if not
        /// the last 2 args are not used if you won't sign up and would be usually null
        /// </summary>
        public async Task<User> SignInAsync(string eId, int eIdType, string name, string
            pictureUrl)
        {
            if (eId == null)
                throw new BadUserInputException();

            var user = await offlineRepo.GetUserByEIdAsync(eId, eIdType);

            logger.LogInformation($"sign in attempt of {eId} -- named: {name} -- isNull? {user == null}");

            if (user == null)
                return await SignUpAsync(eId, eIdType, name, pictureUrl);

            return user;
        }

        private async Task<User> SignUpAsync(string eId, int eIdType, string name,
            string pictureUrl)
        {
            var user = await offlineRepo.CreateUserAsync(new User
            {
                Name = name,
                PictureUrl = pictureUrl,
                Money = 100000,
                EnableOpenMatches = true,
                OwnedBackgroundIds = new List<int> { 0 },
                OwnedTitleIds = new List<int> { 0 },
                OwnedCardBackIds = new List<int> { 0 },
            });

            offlineRepo.ToggleFollow(user.Id, "999");
            offlineRepo.ToggleFollow(user.Id, "9999");

            await offlineRepo.CreateExternalId(new ExternalId
            {
                Id = eId,
                Type = eIdType,
                MainId = user.Id,
            });

            await offlineRepo.SaveChangesAsync();
            return user;
        }

        private const string FbBaseAddress = "https://graph.facebook.com/v12.0/";

        /// <exception cref="FbApiError"></exception>
        /// <exception cref="Exceptions.BadUserInputException"></exception>
        public async Task<bool> ValidateFbAccToken(string token)
        {
            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams.Add("input_token", token);
            queryParams.Add("access_token", fbAppToken);

            var address = FbBaseAddress + "debug_token";

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
                throw new FbApiError("fb response is not supported: " +
                                     result);
            //this is an issue, could be me or client or facebook or something I didn't plan to

            if (data.error is not null)
                throw new Exceptions.BadUserInputException(data.error.message.ToString());

            return data.is_valid;
            //todo add check for timout
        }

        public async Task<(string id, string name, string picUrl)> GetFbProfile(string token)
        {
            var queryParams = HttpUtility.ParseQueryString(String.Empty);
            queryParams.Add("fields", "id,name,email,picture");
            queryParams.Add("access_token", token);

            var address = FbBaseAddress + "me";

            var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

            using var client = new HttpClient();

            var response = await client.GetAsync(uri);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.error is not null)
                throw new Exceptions.BadUserInputException(obj.error.message.ToString());
            //this is client issue if the token is invalid, but this is impossible
            //because I validate it first

            return (obj.id, obj.name, obj.picture.data.url);
        }

        public class FbApiError : Exception
        {
            public FbApiError(string message) : base(message)
            {
            }
        }
    }
}