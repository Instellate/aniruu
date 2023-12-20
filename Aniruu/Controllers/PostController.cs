using System.Buffers;
using Aniruu.Database;
using Aniruu.Database.Entities;
using Aniruu.Request;
using Aniruu.Response;
using Aniruu.Response.Post;
using Aniruu.Utility;
using Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using NetVips;
using TagType = Aniruu.Database.Entities.TagType;

namespace Aniruu.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private static readonly SearchValues<char> AllowedNameChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyz1234567890_.:()!?$@~");


    private readonly ILogger<PostController> _logger;
    private readonly AniruuContext _db;
    private readonly IMinioClient _minio;
    private readonly IMemoryCache _cache;
    private readonly Limits _limits;

    public PostController(
        ILogger<PostController> logger,
        AniruuContext db,
        IMinioClient minio,
        IMemoryCache cache,
        Limits limits
    )
    {
        this._logger = logger;
        this._db = db;
        this._minio = minio;
        this._cache = cache;
        this._limits = limits;
    }

    [HttpPost]
    [Authorization(UserPermission.CreatePost)]
    [Produces("application/json")]
    [ProducesResponseType<PostCreated>(201)]
    [ProducesResponseType<Error>(400)]
    [ProducesResponseType<Error>(500)]
    public async Task<IActionResult> CreatePostAsync(
        [FromForm] IFormFile file,
        [ModelBinder<JsonFormModel>] CreateBody body,
        CancellationToken ct = default
    )
    {
        body.Tags = Regexes.ExcessSpacing().Replace(body.Tags, " ");

        if (!this.ModelState.IsValid)
        {
            return StatusCode(500, new Error(500, ErrorCode.InternalError));
        }

        string[] sortedTags = body.Tags
            .Split(' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Order()
            .ToArray();

        foreach (string str in sortedTags)
        {
            bool isInvalid =
                str.AsSpan().IndexOfAnyExcept(AllowedNameChars) >= 0;

            if (str.Length <= 1)
            {
                return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
            }

            if (str.Length > this._limits.TagNameLimit)
            {
                return BadRequest(new Error(400, ErrorCode.NameTooBig));
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
            UserId = ((User)this.HttpContext.Items["User"]!).Id,
            Source = body.Source
        };

        foreach (string arrTag in sortedTags)
        {
            string tagStr = new(arrTag);

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
                TagType? tagType;
                if (colonIndex == -1)
                {
                    tagType = TagType.General;
                }
                else
                {
                    string tagTypeStr = tagStr[..colonIndex];
                    tagType = tagTypeStr switch
                    {
                        "artist" => TagType.Artist,
                        "character" => TagType.Character,
                        "meta" => TagType.MetaData,
                        "general" => TagType.General,
                        "copyright" => TagType.Copyright,
                        _ => null
                    };

                    if (tagType is null)
                    {
                        return BadRequest(new Error(400, ErrorCode.BadTagType));
                    }
                }

                Tag newTag = new()
                {
                    Name = colonIndex == -1 ? tagStr : tagStr[(colonIndex + 1)..],
                    Type = tagType.Value
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
        // TODO: Make video like media a option as well, requires extraction of frame to make thumbnail and etc

        this._cache.Remove(body.Tags);
        this._cache.Remove("");

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
        Post? post = await this._db.Posts.FindAsync(id, ct);
        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        string filename =
            $"{post.Checksum}{(size is not null ? "-" + size + ".webp" : post.DefaultExtension)}";

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
    public IActionResult GetPost(long id)
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

        PostResponse postResponse = new(post);

        return Ok(postResponse);
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PostsPage>(200)]
    public IActionResult GetPosts(
        [FromQuery] int page = 1,
        [FromQuery] string[]? tags = null
    )
    {
        if (page <= 0)
        {
            page = 1;
        }

        IQueryable<Post> postsQuery =
            this._db.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * 10)
                .Take(10)
                .Include(p => p.Tags)
                .ThenInclude(t => t.Tag)
                .Include(p => p.User);
        long pageCount;

        List<Post> posts;

        if (tags is not null)
        {
            List<Tag> dbTags = new(tags.Length);
            List<Guid> allow = new();
            List<Guid> disallow = new();

            foreach (string tag in tags)
            {
                if (tag.Length == 1)
                {
                    if (tag[0] == 's')
                    {
                        postsQuery = postsQuery.Where(p => p.Rating == PostRating.Safe);
                        continue;
                    }
                    else if (tag[0] == 'q')
                    {
                        postsQuery =
                            postsQuery.Where(p => p.Rating == PostRating.Questionable);
                        continue;
                    }
                    else if (tag[0] == 'e')
                    {
                        postsQuery =
                            postsQuery.Where(p => p.Rating == PostRating.Explicit);
                        continue;
                    }

                    bool isInvalid =
                        (tag.StartsWith('-') ? tag[1..] : tag).AsSpan()
                        .IndexOfAnyExcept(AllowedNameChars) >= 0;
                    if (isInvalid)
                    {
                        return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
                    }
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

                    dbTags.Add(dbTag);

                    disallow.Add(dbTag.Id);
                }
                else
                {
                    Tag? dbTag = this._db.Tags.FirstOrDefault(t => t.Name == tag);
                    if (dbTag is null)
                    {
                        return NotFound(new Error(404, ErrorCode.TagNotFound));
                    }

                    dbTags.Add(dbTag);

                    allow.Add(dbTag.Id);
                }
            } // TODO: Refactor this into it's own util class

            posts = postsQuery
                .Where(p =>
                    !p.Tags.Any(pt => disallow.Any(at => at == pt.Tag.Id)))
                .Where(p =>
                    p.Tags.Count(pt => allow.Any(at => at == pt.Tag.Id)) == allow.Count
                )
                .ToList();

            string cacheKeyTags = string.Join(
                ' ',
                dbTags.Select(t =>
                    $"{t.Type.ToString().ToLower()}:{t.Name.ToLower()}"
                )
            );

            if (this._cache.TryGetValue(cacheKeyTags, out long count))
            {
                pageCount = count;
            }
            else
            {
                pageCount = postsQuery
                    .Where(p => !p.Tags.Any(pt => disallow.Any(at => at == pt.Tag.Id)))
                    .Where(p =>
                        p.Tags.Count(pt => allow.Any(at => at == pt.Tag.Id)) ==
                        allow.Count
                    )
                    .LongCount();

                MemoryCacheEntryOptions cacheEntryOptions = new()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                };
                this._cache.Set(cacheKeyTags, pageCount, cacheEntryOptions);
            }
        }
        else
        {
            posts = postsQuery.ToList();
            if (this._cache.TryGetValue(string.Empty, out long count))
            {
                pageCount = count;
            }
            else
            {
                pageCount = this._db.Posts.LongCount() / 10;
                MemoryCacheEntryOptions cacheOptions = new()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                };
                this._cache.Set(string.Empty, pageCount, cacheOptions);
            }
        }

        List<PostResponse> postsResponse = new(posts.Count);
        foreach (Post p in posts)
        {
            postsResponse.Add(new PostResponse(p));
        }

        PostsPage postPage = new()
        {
            Total = pageCount,
            Posts = postsResponse
        };
        return Ok(postPage);
    }

    [HttpGet("post/tags")]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<string>>(200)]
    public IActionResult SearchTags(
        [FromQuery] string tag
    )
    {
        IEnumerable<string> tags = this._db.Tags
            .Where(t => t.Name.Contains(tag.ToLower()))
            .Select(t => t.Name)
            .Take(50)
            .AsEnumerable();
        // TODO: Still do some optimisation

        return Ok(tags);
    }

    [Authorization]
    [HttpPatch("{id}")]
    [Produces("application/json")]
    public IActionResult EditPost(long id, EditPostBody body)
    {
        User? user = (User?)this.HttpContext.Items["User"];
        if (user is null)
        {
            return Unauthorized(new Error(401, ErrorCode.Unauthorized));
        }

        Post? post = this._db.Posts
            .Include(p => p.Tags)
            .ThenInclude(t => t.Tag)
            .FirstOrDefault(p => p.Id == id);

        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        if (post.UserId != user.Id && (user.Permission & UserPermission.EditPost) == 0)
        {
            return StatusCode(403, new Error(403, ErrorCode.Forbidden));
        }

        if (body.Source is not null)
        {
            post.Source = body.Source;
        }

        if (body.Tags is not null)
        {
            string[] sortedTags = body.Tags
                .Split(' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct()
                .Order()
                .ToArray();

            List<(TagType?, string)> tags = new(sortedTags.Length);
            foreach (string arrTag in sortedTags)
            {
                bool isInvalid =
                    arrTag.AsSpan().IndexOfAnyExcept(AllowedNameChars) >= 0;
                if (isInvalid)
                {
                    return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
                }

                if (arrTag.Length <= 1)
                {
                    return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
                }

                if (arrTag.Length > this._limits.TagNameLimit)
                {
                    return BadRequest(new Error(400, ErrorCode.NameTooBig));
                }

                string tagStr = new(arrTag);

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

                TagType? tagType;
                if (colonIndex == -1)
                {
                    tagType = null;
                }
                else
                {
                    string tagTypeStr = tagStr[..colonIndex];
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
                        return BadRequest(new Error(400, ErrorCode.BadTagType));
                    }

                    this._logger.LogInformation("Tag type is {Type}", tagType);
                }

                tags.Add((tagType, tagName));
            }

            List<(TagType? Type, string name)> tagsToAdd = new();
            Dictionary<string, PostTags> dict = new(post.Tags.Count);
            foreach (PostTags tag in post.Tags)
            {
                dict.Add(tag.Tag.Name, tag);
            }

            foreach ((TagType?, string) entry in tags)
            {
                if (!dict.ContainsKey(entry.Item2))
                {
                    tagsToAdd.Add(entry);
                }
                else
                {
                    if (dict[entry.Item2].Tag.Type != entry.Item1)
                    {
                        tagsToAdd.Add(entry);
                    }
                    else
                    {
                        dict.Remove(entry.Item2);
                    }
                }
            }

            foreach ((string _, PostTags tagToRemove) in dict)
            {
                this._db.PostTags.Remove(tagToRemove);
            }

            foreach ((TagType?, string) tagToAdd in tagsToAdd)
            {
                Tag? tag = this._db.Tags.FirstOrDefault(t => t.Name == tagToAdd.Item2);
                if (tag is null)
                {
                    Tag newTag = new()
                    {
                        Name = tagToAdd.Item2,
                        Type = tagToAdd.Item1 ?? TagType.General
                    };
                    PostTags postTags = new()
                    {
                        Tag = newTag,
                        Post = post,
                    };
                    this._db.Tags.Add(newTag);
                    this._db.PostTags.Add(postTags);
                }
                else
                {
                    PostTags postTags = new()
                    {
                        Tag = tag,
                        Post = post,
                    };
                    this._db.PostTags.Add(postTags);
                    this._logger.LogInformation(
                        "Tag type is {Type}, original type is {TypeOrg}",
                        tag.Type,
                        tagToAdd.Item1);
                    if (tagToAdd.Item1 is not null && tag.Type != tagToAdd.Item1)
                    {
                        _logger.LogInformation("Updating tag type");
                        tag.Type = tagToAdd.Item1.Value;
                    }
                }
            }
        }

        this._db.SaveChanges();
        return NoContent();
    }

    [Authorization]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePostAsync(
        long id,
        CancellationToken ct = default
    )
    {
        Post? post = await this._db.Posts
            .Include(p => p.Tags)
            .ThenInclude(t => t.Tag)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        User? user = (User?)HttpContext.Items["User"];
        if (user is null)
        {
            return StatusCode(401, new Error(401, ErrorCode.Unauthorized));
        }

        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        if (post.UserId != user.Id)
        {
            if ((user.Permission & UserPermission.DeletePost) == 0)
            {
                return StatusCode(403, new Error(403, ErrorCode.Forbidden));
            }
        }

        List<Task> deleteTasks = new(ImageProcessing.Sizes.Length + 1);
        foreach (int size in ImageProcessing.Sizes)
        {
            string filename = $"{post.Checksum}-{size}.webp";

            RemoveObjectArgs args = new RemoveObjectArgs()
                .WithBucket("aniruu")
                .WithObject(filename);
            deleteTasks.Add(this._minio.RemoveObjectAsync(args, ct));
        }

        string orgFilename = $"{post.Checksum}{post.DefaultExtension}";
        RemoveObjectArgs delArgs = new RemoveObjectArgs()
            .WithBucket("aniruu")
            .WithObject(orgFilename);
        deleteTasks.Add(this._minio.RemoveObjectAsync(delArgs, ct));

        try
        {
            await Task.WhenAll(deleteTasks);
        }
        catch (MinioException)
        {
            return StatusCode(500, new Error(500, ErrorCode.InternalError));
        }

        string tags = string.Join(
            ' ',
            post.Tags.Select(t =>
                $"{t.Tag.Type.ToString().ToLower()}:{t.Tag.Name.ToLower()}")
        );
        this._cache.Remove(tags);
        this._cache.Remove("");

        this._db.PostTags.RemoveRange(post.Tags);
        this._db.Comments.RemoveRange(post.Comments);
        this._db.Posts.Remove(post);
        await this._db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{id}/comments")]
    [ProducesResponseType<string>(201)]
    [Authorization(UserPermission.CreateComment)]
    public IActionResult CreateComment(long id, CommentBody body)
    {
        if (string.IsNullOrWhiteSpace(body.Content))
        {
            return BadRequest();
        }

        Post? post = this._db.Posts.Find(id);
        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        User user = (User)HttpContext.Items["User"]!;

        Comment comment = new()
        {
            Content = body.Content,
            PostId = post.Id,
            UserId = user.Id
        };

        this._db.Comments.Add(comment);
        this._db.SaveChanges();

        return Created($"{id}/comments", comment.Id);
    }

    [HttpGet("{id}/comments")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(404)]
    [ProducesResponseType<PostCommentPage>(200)]
    public IActionResult GetComments(long id, [FromQuery] int page)
    {
        if (page <= 0)
        {
            page = 1;
        }

        Post? post = this._db.Posts
            .Include(p =>
                p.Comments.Skip((page - 1) * 5).Take(5)
            )
            .ThenInclude(c => c.User)
            .FirstOrDefault(p => p.Id == id);

        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        IEnumerable<Comment> dbComments =
            post.Comments.OrderBy(c => c.CreatedAt);
        List<PostComment> comments = new(post.Comments.Count);
        foreach (Comment comment in dbComments)
        {
            comments.Add(new PostComment
            {
                Content = comment.Content,
                Author = new PostAuthorResponse(comment.User.Id, comment.User.Username),
                CreatedAt = ((DateTimeOffset)comment.CreatedAt).ToUnixTimeMilliseconds(),
                Id = comment.Id
            });
        }

        long totalComments = this._db.Comments.LongCount(c => c.PostId == id);

        return Ok(new PostCommentPage(comments, totalComments));
    }

    [Authorization]
    [HttpDelete("{postId}/comments/{commentId}")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(403)]
    [ProducesResponseType(200)]
    public IActionResult DeleteComment(long postId, Guid commentId)
    {
        Comment? comment = this._db.Comments.Find(commentId);
        if (comment is null)
        {
            return NotFound();
        }

        if (comment.PostId != postId)
        {
            return NotFound();
        }

        User user = (User)HttpContext.Items["User"]!;
        if (comment.UserId != user.Id)
        {
            if ((user.Permission & UserPermission.DeleteComment) == 0)
            {
                return StatusCode(403, new Error(403, ErrorCode.Forbidden));
            }
        }

        this._db.Comments.Remove(comment);
        this._db.SaveChanges();

        return Ok();
    }

    [Authorization]
    [HttpPut("{postId}/comments/{commentId}")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(403)]
    [ProducesResponseType(200)]
    public IActionResult EditComment(
        long postId,
        Guid commentId,
        [FromBody] CommentBody body
    )
    {
        Comment? comment = this._db.Comments.Find(commentId);
        if (comment is null)
        {
            return NotFound();
        }

        if (comment.PostId != postId)
        {
            return NotFound();
        }

        User user = (User)HttpContext.Items["User"]!;
        if (comment.UserId != user.Id)
        {
            return StatusCode(403, new Error(403, ErrorCode.Forbidden));
        }

        comment.Content = body.Content;
        this._db.SaveChanges();

        return Ok();
    }
}
