using Aniruu.Database;
using Aniruu.Database.Entities;
using Aniruu.Request;
using Aniruu.Response;
using Aniruu.Utility.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace Aniruu.Controllers;

/// <summary>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("default")]
public class AccountController : ControllerBase
{
    private static readonly IMemoryCache NewUserCache =
        new MemoryCache(new MemoryCacheOptions());

    private static readonly char[] AllowedNameChars =
        "abcdefghijklmnopqrstuvwxyz1234567890_.".ToCharArray();

    private readonly AniruuContext _db;
    private readonly Limits _limits;
    private readonly ILogger<AccountController> _logger;
    private readonly OAuth2 _oAuth2;

    public AccountController(
        ILogger<AccountController> logger,
        Limits limits,
        AniruuContext db,
        OAuth2 oAuth2
    )
    {
        this._logger = logger;
        this._limits = limits;
        this._db = db;
        this._oAuth2 = oAuth2;
    }

    /// <summary>
    /// OAuth2 Callback
    /// </summary>
    /// <para>
    /// A endpoint that is for callbacks from OAuth services
    /// </para>
    /// <param name="code">The OAuth2 code</param>
    /// <param name="state">The OAuth2 state</param>
    /// <param name="ct"></param>
    /// <returns>A redirection to the web app. </returns>
    // TODO: Remove the hard coding of web app URLs
    [HttpGet("redirect")]
    [Produces("application/json")]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)] // TODO: Proper errors
    [ProducesResponseType(409)]
    public async Task<IActionResult> RedirectUrlAsync(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct = default
    )
    {
        UserConnectionType type;
        {
            switch (state)
            {
                case "discord":
                    type = UserConnectionType.Discord;
                    break;
                case "github":
                    type = UserConnectionType.GitHub;
                    break;
                case "google":
                    type = UserConnectionType.Google;
                    break;
                default:
                    return BadRequest();
            }
        }

        if ((string?)Request.Headers.UserAgent is null)
        {
            return BadRequest();
        }

        OAuth2Info info;
        try
        {
            // TODO: Add support for multiple providers
            info = await this._oAuth2.GetInfoAsync(code, state, ct);
        }
        catch (OAuth2Exception e)
        {
            this._logger.LogError("Couldn't get info, exception: {Exception}", e);
            return BadRequest();
        }

        UserConnection? user =
            this._db.Connections.FirstOrDefault(u =>
                u.Email == info.Email);
        if (user is not null)
        {
            if (user.Type != type)
            {
                return Conflict();
            }

            Session session = new()
            {
                Id = Guid.NewGuid(),
                UserAgent = Request.Headers.UserAgent!,
                UserId = user.UserId,
                Type = UserConnectionType.Google,
            };
            this._db.Sessions.Add(session);
            await this._db.SaveChangesAsync(ct);
            return Redirect($"http://localhost:5173/signinCallback?token={session.Id}");
        }
        else
        {
            Guid id = Guid.NewGuid();
            NewUserCache.Set(id, info.Email, TimeSpan.FromMinutes(10));
            return Redirect(
                $"http://localhost:5173/signinCallback?newAccount=true&token={id}"
            );
        }
    }

    /// <summary>
    /// Get Authentication URI's
    /// </summary>
    /// <para>
    /// Returns all the URIs for authentication
    /// </para>
    /// <returns>All the URI's for authentication</returns>
    [HttpGet("authUris")]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<AuthUri>>(200)]
    public IActionResult GetAuthUri()
    {
        return Ok(new AuthUri[]
        {
            new()
            {
                Service = "Google",
                Uri = this._oAuth2.GoogleAuthUrl
            },
            new()
            {
                Service = "Discord",
                Uri = this._oAuth2.DiscordAuthUrl
            },
            new()
            {
                Service = "GitHub",
                Uri = "https://github.com"
            }
        });
    }

    /// <summary>
    /// Claim username
    /// </summary>
    /// <para>
    /// Claims a username for new accounts.
    /// </para>
    /// <param name="body"></param>
    /// <returns></returns>
    [HttpPost("claimUsername")]
    [Produces("application/json")]
    [ProducesResponseType<SessionCreated>(201)]
    [ProducesResponseType<Error>(400)]
    [ProducesResponseType<Error>(404)]
    [ProducesResponseType<Error>(409)]
    [ProducesResponseType<Error>(500)]
    public IActionResult ClaimUsername([FromBody] ClaimUsername body)
    {
        if (body.Name.Length > this._limits.UsernameLimit)
        {
            return BadRequest(new Error(400, ErrorCode.NameTooBig));
        }

        if ((string?)Request.Headers.UserAgent is null)
        {
            return BadRequest(new Error(400, ErrorCode.NoUserAgent));
        }

        bool all = true;
        foreach (char c in body.Name)
        {
            bool any = false;
            foreach (char ac in AllowedNameChars)
            {
                if (ac == c)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
            {
                all = false;
                break;
            }
        }

        if (!all)
        {
            return BadRequest();
        }

        foreach (char chr in body.Name)
        {
            bool invalidChar = true;
            foreach (char c in AllowedNameChars)
            {
                if (chr == c)
                {
                    invalidChar = false;
                    break;
                }
            }

            if (invalidChar)
            {
                return BadRequest();
            }
        }

        if (_db.Users.FirstOrDefault(u =>
                u.Username == body.Name) is not null)
        {
            return Conflict(new Error(409, ErrorCode.NameAlreadyInUsage));
        }

        if (!NewUserCache.TryGetValue(body.TemporaryToken, out string? email))
        {
            return NotFound(new Error(404, ErrorCode.NoTokenForClaimingName));
        }

        if (email is null) // Should hopefully never occur
        {
            return StatusCode(500, new Error(500, ErrorCode.InternalError));
        }

        User user = new()
        {
            PrimaryEmail = email,
            Username = body.Name
        };
        this._db.Users.Add(user);


        UserConnection connection = new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Type = UserConnectionType.Google,
            User = user,
        };
        this._db.Connections.Add(connection);

        Session session = new()
        {
            User = user,
            UserAgent = Request.Headers.UserAgent!,
            Type = UserConnectionType.Google
        };

        this._db.Sessions.Add(session);
        this._db.SaveChanges();

        return Created("api/Account/claimUsername",
            new SessionCreated()
            {
                SessionToken = session.Id
            }
        );
    }

    /// <summary>
    /// Delete session
    /// </summary>
    /// <param name="id">The id for the session</param>
    /// <returns></returns>
    [HttpDelete("session/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<Error>(400)]
    public IActionResult DeleteSession(Guid id)
    {
        Session? session = this._db.Sessions.Find(id);
        if (session is null)
        {
            return BadRequest(new Error(400, ErrorCode.NoSessionFound));
        }

        this._db.Remove(session);
        this._db.SaveChanges();

        return NoContent();
    }
}
