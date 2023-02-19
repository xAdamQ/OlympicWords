using System;
using BestHTTP.SignalRCore;

public class MyReconnectPolicy : IRetryPolicy
{
    static int MaxRetries = 0;
    int Retries;

    public TimeSpan? GetNextRetryDelay(RetryContext context)
    {
        Retries++;

        if (Retries > MaxRetries) return null;

        return TimeSpan.FromSeconds(2);
    }
}