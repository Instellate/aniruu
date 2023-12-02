namespace Aniruu.Utility.Ratelimit;

public sealed class RateLimitRule
{
    public TimeSpan ExpireTime { get; set; }
    public bool IsPerUniqueUri { get; set; }
    public int RequestLimit { get; set; }

    public RateLimitRule(IConfigurationSection section)
    {
        ReadOnlySpan<char> expireTimeSpan = section["ExpireTime"].AsSpan();
        if (expireTimeSpan.EndsWith("s"))
        {
            ReadOnlySpan<char> secondsSpan = expireTimeSpan[..^1];
            int seconds = int.Parse(secondsSpan);
            this.ExpireTime = TimeSpan.FromSeconds(seconds);
        }
        else if (expireTimeSpan.EndsWith("m"))
        {
            ReadOnlySpan<char> minutesSpan = expireTimeSpan[..^1];
            int minutes = int.Parse(minutesSpan);
            this.ExpireTime = TimeSpan.FromMinutes(minutes);
        }
        else if (expireTimeSpan.EndsWith("h"))
        {
            ReadOnlySpan<char> hoursSpan = expireTimeSpan[..^1];
            int hours = int.Parse(hoursSpan);
            this.ExpireTime = TimeSpan.FromHours(hours);
        }
        else
        {
            this.ExpireTime = TimeSpan.Zero;
        }

        this.IsPerUniqueUri = bool.Parse(section["IsPerUniqueUri"]!);
        this.RequestLimit = int.Parse(section["RequestLimit"]!);
    }
}
