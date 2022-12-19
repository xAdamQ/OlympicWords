using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.Mvc;

namespace OlymbicWords.Controllers;

public class SseController : ControllerBase
{
    private readonly IServerSentEventsService serverSentEventsService;
    public SseController(IServerSentEventsService serverSentEventsService)
    {
        this.serverSentEventsService = serverSentEventsService;
    }
}