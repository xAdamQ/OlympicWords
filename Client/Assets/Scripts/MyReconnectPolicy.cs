using System;
using BestHTTP.SignalRCore;

public class MyReconnectPolicy : IRetryPolicy
{
    static int MaxRetries = 20;
    int Retries;

    public TimeSpan? GetNextRetryDelay(RetryContext context)
    {
        Retries++;

        if (Retries > MaxRetries)
            return null;
        else
            return TimeSpan.FromSeconds(2);
    }
}