using OlympicWords.Data;

namespace OlympicWords.Services
{
    public class ProviderLink
    {
        /// <summary>
        /// id to the external auth service like facebook or huawei
        /// </summary>
        public string Id { get; set; }

        public string UserId { get; set; }

        public int Type { get; set; }

        public virtual User User { get; set; }
        //I don't have 2FA, no roles, no intention to add them
        //much better to focus on real features
        //and to make sure the current flow is safe
        //anyway the game even could be played without authentication  
    }
}