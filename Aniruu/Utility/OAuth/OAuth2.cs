using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Aniruu.Database.Entities;

namespace Aniruu.Utility.OAuth;

public class OAuth2
{
    private readonly string _googleSecret;
    private readonly string _googleId;
    private readonly string _googleRedirectUri;

    private readonly string _discordId;
    private readonly string _discordSecret;
    private readonly string _discordRedirectUri;

    public string GoogleAuthUrl { get; }
    public string DiscordAuthUrl { get; }

    public OAuth2(IConfiguration config)
    {
        IConfigurationSection oAuth2Config = config.GetSection("OAuth2");

        this._googleSecret = config["GOOGLE_CLIENT_SECRET"]!;
        this._googleId = config["GOOGLE_CLIENT_ID"]!;
        this._googleRedirectUri = oAuth2Config["GoogleRedirectUri"]!;

        NameValueCollection googleQueryBuilder =
            HttpUtility.ParseQueryString(string.Empty);
        googleQueryBuilder.Add("prompt", "login");
        googleQueryBuilder.Add("response_type", "code");
        googleQueryBuilder.Add("access_type", "offline");
        googleQueryBuilder.Add("state", "google");
        googleQueryBuilder.Add("scope", "https://www.googleapis.com/auth/userinfo.email");
        googleQueryBuilder.Add("client_id", config["GOOGLE_CLIENT_ID"]!);
        googleQueryBuilder.Add("redirect_uri", this._googleRedirectUri);

        this.GoogleAuthUrl =
            $"https://accounts.google.com/o/oauth2/auth?login_hint&{googleQueryBuilder}";

        this._discordId = config["DISCORD_CLIENT_ID"]!;
        this._discordSecret = config["DISCORD_CLIENT_SECRET"]!;
        this._discordRedirectUri = oAuth2Config["DiscordRedirectUri"]!;

        NameValueCollection discordQueryBuilder =
            HttpUtility.ParseQueryString(string.Empty);
        discordQueryBuilder.Add("response_type", "code");
        discordQueryBuilder.Add("scope", "email identify");
        discordQueryBuilder.Add("client_id", this._discordId);
        discordQueryBuilder.Add("redirect_uri", this._discordRedirectUri);
        discordQueryBuilder.Add("state", "discord");

        this.DiscordAuthUrl =
            $"https://discord.com/oauth2/authorize?{discordQueryBuilder}";
    }

    public Task<OAuth2Info> GetInfoAsync(
        string code,
        string service,
        CancellationToken ct = default)
    {
        switch (service)
        {
            case "google":
                return GetInfoFromGoogleAsync(code, ct);

            case "discord":
                return GetInfoFromDiscordAsync(code, ct);

            case "github":
                throw new NotImplementedException();

            default:
                throw new OAuth2Exception();
        }
    }

    public async Task<OAuth2Info> GetInfoFromGoogleAsync(
        string code,
        CancellationToken ct = default
    )
    {
        using HttpClient client = new();
        NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("code", code);
        queryString.Add("client_id", this._googleId);
        queryString.Add("client_secret", this._googleSecret);
        queryString.Add("redirect_uri", this._googleRedirectUri);
        queryString.Add("grant_type", "authorization_code");

        try
        {
            using HttpRequestMessage httpRequest = new();
            httpRequest.RequestUri =
                new Uri($"https://oauth2.googleapis.com/token?{queryString}");
            httpRequest.Method = HttpMethod.Post;

            using HttpResponseMessage response =
                await client.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new OAuth2Exception();
            }

            OAuth2GoogleResponse? json =
                await response.Content.ReadFromJsonAsync<OAuth2GoogleResponse>(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    },
                    ct);
            if (json is null)
            {
                throw new OAuth2Exception();
            }

            DateTimeOffset expireDate = DateTimeOffset.Now.AddSeconds(json.ExpiresIn);

            using HttpRequestMessage request = new();
            request.Headers.Add("Authorization", $"Bearer {json.AccessToken}");
            request.RequestUri =
                new Uri("https://www.googleapis.com/oauth2/v3/userinfo?access_token");
            request.Method = HttpMethod.Get;

            using HttpResponseMessage emailResponseMessage =
                await client.SendAsync(request, ct);
            if (!emailResponseMessage.IsSuccessStatusCode)
            {
                throw new OAuth2Exception();
            }

            OAuth2GoogleEmailResponse? emailJson = await emailResponseMessage.Content
                .ReadFromJsonAsync<OAuth2GoogleEmailResponse>(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    },
                    ct);

            if (emailJson is null)
            {
                throw new OAuth2Exception();
            }

            return new OAuth2Info()
            {
                Email = emailJson.Email,
                AccessToken = json.AccessToken,
                AccessTokenExpiration = expireDate,
                Type = UserConnectionType.Google,
                RefreshToken = json.RefreshToken,
                RefreshTokenExpiration = DateTimeOffset.MaxValue
            };
        }
        catch (Exception)
        {
            throw new OAuth2Exception();
        }
    }

    public async Task<OAuth2Info> GetInfoFromDiscordAsync(
        string code,
        CancellationToken ct = default
    )
    {
        try
        {
            using HttpClient client = new();

            Dictionary<string, string> queryBuilder = new()
            {
                { "grant_type", "authorization_code" },
                { "client_id", this._discordId },
                { "client_secret", this._discordSecret },
                { "code", code },
                { "redirect_uri", this._discordRedirectUri }
            };

            using HttpRequestMessage tokenRequest = new();
            tokenRequest.Method = HttpMethod.Post;
            tokenRequest.RequestUri =
                new Uri("https://discord.com/api/v10/oauth2/token");
            FormUrlEncodedContent content = new(queryBuilder);
            tokenRequest.Content = content;

            using HttpResponseMessage
                tokenResponse = await client.SendAsync(tokenRequest, ct);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new OAuth2Exception(
                    $"Token response did not return success status code: {await tokenResponse.Content.ReadAsStringAsync(ct)}");
            }

            OAuth2DiscordResponse? response =
                await tokenResponse.Content.ReadFromJsonAsync<OAuth2DiscordResponse>(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    },
                    ct);
            if (response is null)
            {
                throw new OAuth2Exception("Token response is null.");
            }

            DateTimeOffset expiresIn =
                DateTimeOffset.Now + TimeSpan.FromSeconds(response.ExpiresIn);

            using HttpRequestMessage userRequest = new();
            userRequest.Method = HttpMethod.Get;
            userRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", response.AccessToken);
            userRequest.RequestUri = new Uri("https://discord.com/api/v10/users/@me");

            using HttpResponseMessage userResponse =
                await client.SendAsync(userRequest, ct);
            if (!userResponse.IsSuccessStatusCode)
            {
                throw new OAuth2Exception(
                    $"User did not return successful status code: {await userResponse.Content.ReadAsStringAsync(ct)}");
            }

            DiscordUser? user =
                await userResponse.Content.ReadFromJsonAsync<DiscordUser>(
                    new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    },
                    ct);
            if (user is null)
            {
                throw new OAuth2Exception("User is null");
            }

            return new OAuth2Info()
            {
                AccessToken = response.AccessToken,
                AccessTokenExpiration = expiresIn,
                Email = user.Email,
                Type = UserConnectionType.Discord,
                RefreshToken = response.RefreshToken,
                RefreshTokenExpiration = DateTimeOffset.MaxValue
            };
        }
        catch (Exception e) when (e is not OAuth2Exception)
        {
            Console.WriteLine(e);
            throw new OAuth2Exception(e);
        }
    }
}
