using System.Net.Http.Headers;
using Aniruu.Database;
using Aniruu.Database.Entities;
using Aniruu.Response;
using Aniruu.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Aniruu.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public Task InvokeAsync(HttpContext ctx, AniruuContext db)
    {
        Endpoint? endpoint = ctx.GetEndpoint();
        AuthorizationAttribute? authAttr =
            endpoint?.Metadata.GetMetadata<AuthorizationAttribute>();
        if (authAttr is null)
        {
            return this._next(ctx);
        }

        string? authHeader = ctx.Request.Headers.Authorization;
        if (authHeader is null)
        {
            Error error = new(401, ErrorCode.Unauthorized);
            return ctx.Response.WriteAsJsonAsync(error);
        }

        AuthenticationHeaderValue authVal;
        try
        {
            authVal = AuthenticationHeaderValue.Parse(authHeader);
        }
        catch (FormatException)
        {
            Error error = new(401, ErrorCode.Unauthorized);
            ctx.Response.StatusCode = 501;
            return ctx.Response.WriteAsJsonAsync(error);
        }

        if (authVal.Scheme != "Bearer")
        {
            if (authVal.Scheme == "Bot")
            {
                Error error = new(501, ErrorCode.NotImplemented);
                ctx.Response.StatusCode = 501;
                return ctx.Response.WriteAsJsonAsync(error);
            }
            else
            {
                Error error = new(401, ErrorCode.Unauthorized);
                ctx.Response.StatusCode = 401;
                return ctx.Response.WriteAsJsonAsync(error);
            }
        }

        if (authVal.Parameter is null)
        {
            Error error = new(401, ErrorCode.Unauthorized);
            ctx.Response.StatusCode = 401;
            return ctx.Response.WriteAsJsonAsync(error);
        }

        if (!Guid.TryParse(authVal.Parameter, out Guid id))
        {
            Error error = new(401, ErrorCode.Unauthorized);
            ctx.Response.StatusCode = 401;
            return ctx.Response.WriteAsJsonAsync(error);
        }

        Session? session = db.Sessions
            .Include(s => s.User)
            .FirstOrDefault(s => s.Id == id);
        
        if (session is null)
        {
            Error error = new(401, ErrorCode.Unauthorized);
            ctx.Response.StatusCode = 401;
            return ctx.Response.WriteAsJsonAsync(error);
        }

        if ((session.User.Permission & authAttr.Permission) == 0)
        {
            Error error = new(403, ErrorCode.Forbidden);
            ctx.Response.StatusCode = 403;
            return ctx.Response.WriteAsJsonAsync(error);
        }

        ctx.Items["User"] = session.UserId;
        return this._next(ctx);
    }
}
