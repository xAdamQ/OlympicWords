using OlympicWords.Services;

namespace OlympicWords.Properties;

public  class OneTimeOperations
{
    private readonly IOfflineRepo offlineRepo;
    public OneTimeOperations(IOfflineRepo offlineRepo)
    {
        this.offlineRepo = offlineRepo;
    }
    
    /// <summary>
    /// when you add a new environment, and you want to update the whole database to set the default selected players
    /// </summary>
    public  void FillInMissingPlayers()
    {
        // offlineRepo.gerUser
    }
}