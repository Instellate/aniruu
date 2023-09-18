using System.Runtime.InteropServices.JavaScript;
using Aniruu.Database;
using Aniruu.Database.Entities;
using Aniruu.Request;
using Aniruu.Response;
using Aniruu.Response.Post;
using Aniruu.Utility;
using Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using NetVips;

namespace Aniruu.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private static readonly string[] MediaArr = new string[]
        { "jpeg", "gif", "png", "webp" };

    private static readonly char[] AllowedNameChars =
        "abcdefghijklmnopqrstuvwxyz1234567890_.:".ToCharArray();

    private readonly ILogger<PostController> _logger;
    private readonly AniruuContext _db;
    private readonly IMinioClient _minio;

    public PostController(
        ILogger<PostController> logger,
        AniruuContext db,
        IMinioClient minio
    )
    {
        this._logger = logger;
        this._db = db;
        this._minio = minio;
    }

    [HttpPost]
    [Authorization(UserPermission.CreatePost)]
    [Produces("application/json")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<PostCreated>(201)]
    [ProducesResponseType<Error>(400)]
    [ProducesResponseType<Error>(500)]
    public async Task<IActionResult> CreatePostAsync(
        [FromForm] IFormFile file,
        [ModelBinder<JsonFormModel>] CreateBody body,
        CancellationToken ct = default
    )
    {
        if (!this.ModelState.IsValid)
        {
            return StatusCode(500, new Error(500, ErrorCode.InternalError));
        }

        int count = 0;
        HashSet<char> set = new(body.Tags.Length);
        foreach (char c in body.Tags)
        {
            if (set.Add(c))
            {
                count++;
            }
        }

        if (count != body.Tags.Length)
        {
            return BadRequest(new Error(400, ErrorCode.DuplicateTags));
        }

        ArraySegment<char> tags = body.Tags.ToCharArray();
        List<ArraySegment<char>> sortedTags = new();

        int index = ((IList<char>)tags).IndexOf(' ');
        while (index != -1)
        {
            sortedTags.Add(tags[..index]);
            try
            {
                tags = tags[++index..];
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }

            index = ((IList<char>)tags).IndexOf(' ');
        }

        sortedTags.Add(tags);

        foreach (ArraySegment<char> arraySegment in sortedTags)
        {
            if (arraySegment.Count > 0)
            {
                return BadRequest(new Error(400, ErrorCode.BadTagType));
            }

            bool isInvalid = true;
            foreach (char c in arraySegment)
            {
                foreach (char chr in AllowedNameChars)
                {
                    if (c == chr)
                    {
                        isInvalid = false;
                        goto endLoopEarly;
                    }
                }
            }

            endLoopEarly:
            int colonAmount = 0;
            foreach (char c in arraySegment)
            {
                if (c == ':')
                {
                    colonAmount++;
                }
            }

            if (colonAmount > 1)
            {
                return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
            }

            if (isInvalid)
            {
                return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
            }
        }

        ImageProcessing imageProcessing = new(this._minio);
        await using Stream stream = file.OpenReadStream();
        string checksum = await imageProcessing.CalculateChecksumAsync(stream, ct);
        stream.Seek(0, SeekOrigin.Begin);

        Post post = new()
        {
            Rating = body.Rating,
            Checksum = checksum,
            DefaultExtension = Path.GetExtension(file.FileName.Replace("jpg", "jpeg")),
            UserId = (long)HttpContext.Items["User"]!,
            Source = body.Source
        };

        foreach (ArraySegment<char> arrSegTag in sortedTags)
        {
            string tagStr = new(arrSegTag);

            int colonIndex = tagStr.IndexOf(':');
            string tagName;
            try
            {
                tagName = colonIndex == -1 ? tagStr : tagStr[(colonIndex + 1)..];
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(new Error(400, ErrorCode.TagTypeWithoutName));
            }

            Tag? dbTag = await this._db.Tags
                .FirstOrDefaultAsync(t => t.Name == tagName, ct);

            if (dbTag is null)
            {
                TagType tagType;
                if (colonIndex == -1)
                {
                    tagType = TagType.General;
                }
                else
                {
                    string tagTypeStr = tagStr[..colonIndex];
                    this._logger.LogInformation("{}", tagTypeStr);
                    if (tagTypeStr == "artist")
                    {
                        tagType = TagType.Artist;
                    }
                    else if (tagTypeStr == "general")
                    {
                        tagType = TagType.General;
                    }
                    else if (tagTypeStr == "copyright")
                    {
                        tagType = TagType.Copyright;
                    }
                    else if (tagTypeStr == "character")
                    {
                        tagType = TagType.Character;
                    }
                    else if (tagTypeStr == "meta")
                    {
                        tagType = TagType.MetaData;
                    }
                    else
                    {
                        return BadRequest(new Error(400, ErrorCode.InvalidTagType));
                    }
                }

                Tag newTag = new()
                {
                    Name = colonIndex == -1 ? tagStr : tagStr[(colonIndex + 1)..],
                    Type = tagType
                };
                PostTags postTags = new()
                {
                    Post = post,
                    Tag = newTag
                };

                this._db.Tags.Add(newTag);
                this._db.PostTags.Add(postTags);
            }
            else
            {
                PostTags postTags = new()
                {
                    Post = post,
                    Tag = dbTag
                };
                this._db.PostTags.Add(postTags);
            }
        }

        try
        {
            await imageProcessing.ProcessAndUploadAsync(
                file.OpenReadStream(),
                Path.GetExtension(file.FileName),
                checksum,
                ct
            );
        }
        catch (VipsException)
        {
            return BadRequest(new Error(400, ErrorCode.NotAValidMediaType));
        }

        this._db.Posts.Add(post);
        await this._db.SaveChangesAsync(ct);

        return Created($"api/Post/{post.Id}",
            new PostCreated()
            {
                PostId = post.Id
            });
    }

    [HttpGet("{id}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetImageAsync(
        long id,
        [FromQuery] string? size,
        CancellationToken ct = default
    )
    {
        Post? post = await this._db.Posts.FindAsync(id);
        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        string filename =
            $"{post.Checksum}{(size is not null ? "-" + size + ".webp" : post.DefaultExtension)}";
        this._logger.LogInformation("{Filename}", filename);

        GetObjectArgs args = new GetObjectArgs()
            .WithBucket("aniruu")
            .WithObject(filename)
            .WithCallbackStream((s, cto) => s.CopyToAsync(this.Response.Body, cto));

        StatObjectArgs statArgs = new StatObjectArgs()
            .WithBucket("aniruu")
            .WithObject(filename);

        try
        {
            ObjectStat stat = await this._minio.StatObjectAsync(statArgs, ct);
            this.Response.ContentType = stat.ContentType;
            this.Response.Headers.ContentDisposition = $"inline;filename=\"{filename}\"";
            await this._minio.GetObjectAsync(args, ct);
        }
        catch (ObjectNotFoundException)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        return Empty;
    }

    [HttpGet("{id}/data")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(404)]
    [ProducesResponseType<PostResponse>(200)]
    public IActionResult GetImageData(long id)
    {
        Post? post = this._db.Posts
            .Include(p => p.Tags)
            .ThenInclude(t => t.Tag)
            .Include(post => post.User)
            .FirstOrDefault(p => p.Id == id);

        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.NoPostFound));
        }

        PostResponse postResponse = new()
        {
            Id = post.Id,
            Location = $"/api/post/{post.Id}",
            CreatedAt = post.CreatedAt,
            CreatedBy = new PostAuthorResponse(post.User.Id, post.User.Username),
            Rating = post.Rating,
            Source = post.Source,
            Tags = new List<PostTagsResponse>(post.Tags.Count)
        };


        foreach (PostTags postTags in post.Tags)
        {
            postResponse.Tags.Add(
                new PostTagsResponse(postTags.Tag.Type, postTags.Tag.Name)
            );
        }

        return Ok(postResponse);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<PostResponse>>(200)]
    public IActionResult GetPosts(
        [FromQuery] int page = 1,
        [FromQuery] string[]? tags = null
    )
    {
        if (page >= 0)
        {
            page = 1;
        }

        _logger.LogInformation("{Page}", (page - 1) * 10);
        IIncludableQueryable<Post, User> postsQuery =
            this._db.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .Include(p => p.Tags)
                .ThenInclude(t => t.Tag)
                .Include(p => p.User);

        List<Post> posts;
        if (tags is not null)
        {
            List<Guid> disallow = new();
            List<Guid> allow = new();

            foreach (string tag in tags)
            {
                bool all = true;
                foreach (char t in tag.StartsWith('-') ? tag[1..] : tag)
                {
                    bool any = false;
                    foreach (char c in AllowedNameChars)
                    {
                        if (c == t)
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
                    return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
                }

                if (tag[0] == '-')
                {
                    string tagWithoutDash = tag[1..];
                    Tag? dbTag =
                        this._db.Tags.FirstOrDefault(t => t.Name == tagWithoutDash);
                    if (dbTag is null)
                    {
                        return NotFound(new Error(404, ErrorCode.TagNotFound));
                    }

                    disallow.Add(dbTag.Id);
                }
                else
                {
                    Tag? dbTag = this._db.Tags.FirstOrDefault(t => t.Name == tag);
                    if (dbTag is null)
                    {
                        return NotFound(new Error(404, ErrorCode.TagNotFound));
                    }

                    allow.Add(dbTag.Id);
                }
            }

            posts = postsQuery
                .Where(p => !p.Tags.Any(pt => disallow.Any(at => at == pt.Tag.Id)))
                .Where(p =>
                    p.Tags.Count(pt => allow.Any(at => at == pt.Tag.Id)) == allow.Count
                )
                .ToList();
        }
        else
        {
            posts = postsQuery.ToList();
        }

        List<PostResponse> postsResponse = new(posts.Count);

        foreach (Post post in posts)
        {
            PostResponse postResponse = new()
            {
                Id = post.Id,
                Location = $"/api/post/{post.Id}",
                CreatedAt = post.CreatedAt,
                CreatedBy = new PostAuthorResponse(post.User.Id, post.User.Username),
                Rating = post.Rating,
                Source = post.Source,
            };
            postsResponse.Add(postResponse);

            postResponse.Tags.Capacity = post.Tags.Count;
            foreach (PostTags t in post.Tags)
            {
                postResponse.Tags.Add(new PostTagsResponse(t.Tag.Type, t.Tag.Name));
            }
        }

        return Ok(postsResponse);
    }

    [HttpGet("post/tags")]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<string>>(200)]
    public IActionResult SearchTags(
        [FromQuery] string tag
    )
    {
        string formattedTag = $"%{tag}%";
        IEnumerable<string> tags = this._db.Tags
            .Where(t => EF.Functions.Like(t.Name, formattedTag))
            .Select(t => t.Name);

        return Ok(tags);
    }
}
