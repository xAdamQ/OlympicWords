#if !UNITY
using Microsoft.AspNetCore.Mvc;
using OlympicWords.Services;
using OlympicWords.Common;
#else
using System.Threading.Tasks;
#endif

namespace Shared.Controllers
{
    public interface IUserController : IController
    {
        Task<PersonalFullUserInfo> Personal();
        /// <summary>
        /// get public user data by his id
        /// </summary>
        Task<FullUserInfo> Public(string id);
        Task ToggleFollow(string targetId);
        Task SetOpenMatches(bool value);
        Task
#if !UNITY
            <IActionResult>
#endif
            LinkTo(
#if !UNITY
                [FromServices] SecurityManager securityManager,
#endif
                string originalToken, string originalProviderString, string newProviderStr,
                string newToken, bool overwriteNew);
    }

    public class LinkResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}