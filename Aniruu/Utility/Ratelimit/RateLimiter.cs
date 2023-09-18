using System.Collections.Frozen;
using System.Text;
using System.Threading.RateLimiting;
using Aniruu.Response;
using Microsoft.AspNetCore.RateLimiting;

namespace Aniruu.Utility.Ratelimit;

public sealed class RateLimiter : IRateLimiterPolicy<string>
{
    private readonly FrozenDictionary<string, RateLimitRule> _rules;
    private readonly ILogger<RateLimiter> _logger;

    public RateLimiter(IConfiguration config, ILogger<RateLimiter> logger)
    {
        Dictionary<string, RateLimitRule> rules =
            config.GetRequiredSection("RateLimitRules")
                .GetChildren().ToDictionary(x => x.Key,
                    x => new RateLimitRule(x));
        this._rules = rules.ToFrozenDictionary();
        this._logger = logger;
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();
        if (endpoint is RouteEndpoint r)
        {
            if (this._rules.TryGetValue(r.RoutePattern.RawText ?? string.Empty,
                    out RateLimitRule? rule))
            {
                string? address = httpContext.Connection.RemoteIpAddress?.ToString() ??
                                  httpContext.Connection.LocalIpAddress?.ToString();
                if (address is null)
                {
                    // Instant rate limit connections that cannot get 
                    throw new NotImplementedException();
                }

                ReadOnlySpan<char> path = rule.IsPerUniqueUri
                    ? httpContext.Request.Path.Value!.AsSpan()
                    : r.RoutePattern.RawText!.AsSpan(); // TODO: Make this nullable

                StringBuilder sb = new(address.Length + path.Length);
                sb.Append(path);
                sb.Append(address.AsSpan());

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: sb.ToString(),
                    _ => new FixedWindowRateLimiterOptions()
                    {
                        AutoReplenishment = true,
                        PermitLimit = rule.RequestLimit,
                        QueueLimit = 0,
                        Window = rule.ExpireTime
                    });
            }
        }

        return RateLimitPartition.GetNoLimiter(string.Empty);
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected =>
        async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = 429;

            // TODO: Decide if this is a bad idea
            // context.HttpContext.Response.Headers.AccessControlAllowOrigin = "*";

            await context.HttpContext.Response.WriteAsJsonAsync(
                new Error(429, ErrorCode.TooManyRequests),
                ct
            );
        };
}
