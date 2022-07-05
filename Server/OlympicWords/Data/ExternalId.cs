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
    }

    public enum ExternalIdType
    {
        Demo, //leave it even when disabled in production
        Facebook,
        Fbig,
        Huawei,
    }
}