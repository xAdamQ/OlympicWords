using OlympicWords.Data;

namespace OlympicWords.Services;

public class UserPicture
{
    public string UserId { get; set; }
    public virtual User User { get; set; }

    public byte[] Picture { get; set; }
    public int AvatarId { get; set; }
}