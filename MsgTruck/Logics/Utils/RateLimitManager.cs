namespace MsgTruck
{
    /// <summary>
    /// Manages Rate Limit. Currently using <code>RateLimitManager.ResetAfter</code>, but might be changed later.
    /// This class exists for potentially complicated rate limit managing in the future.
    /// </summary>
    internal class RateLimitManager
    {
        //can be fixed to 250ms, let's see how this would work
        internal TimeSpan GetRateLimit()
        {
            var resetAfter = new Discord.Net.RateLimitInfo().ResetAfter;
            return (resetAfter == null)?TimeSpan.Zero:resetAfter.Value;
        }
    }
}
