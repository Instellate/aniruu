using Aniruu.Database;
using Aniruu.Database.Entities;
using Aniruu.Response;
using Aniruu.Utility;
using Microsoft.AspNetCore.Mvc;

namespace Aniruu.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly AniruuContext _db;

    public UserController(AniruuContext db)
    {
        _db = db;
    }

    [Authorization]
    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType<UserProfile>(200)]
    public IActionResult GetUserMe()
    {
        User user = (User)HttpContext.Items["User"]!;
        List<long> posts = this._db.Posts
            .Where(p => p.UserId == user.Id)
            .Select(p => p.Id)
            .ToList();

        return Ok(new UserProfile(user, posts, true));
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(404)]
    public IActionResult GetUser(long id)
    {
        return Ok();
    }
}
