using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OlympicWords.Services
{
    public class ExternalId
    {
        /// <summary>
        /// id to the external auth service like facebook or huawei
        /// </summary>
        public string Id { get; set; }

        [ForeignKey("User")] public string MainId { get; set; }

        public int Type { get; set; }
        
        //I don't have 2FA, no roles, no intention to add them
        //much better to focus on real features
        //and to make sure the current flow is safe
        //anyway the game even could be played without authentication  
    }

    public enum ExternalIdType
    {
        Demo, //leave it even when disabled in production
        Facebook,
        Fbig,
        Huawei,
    }
}