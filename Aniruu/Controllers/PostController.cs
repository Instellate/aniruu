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
using TagType = Aniruu.Database.Entities.TagType;

namespace Aniruu.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
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

        int count = 0;
        HashSet<ArraySegment<char>> set = new();
        foreach (ArraySegment<char> chars in sortedTags)
        {
            if (set.Add(chars))
            {
                count++;
            }
        }

        if (count != sortedTags.Count)
        {
            return BadRequest(new Error(400, ErrorCode.DuplicateTags));
        }

        foreach (ArraySegment<char> arraySegment in sortedTags)
        {
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
            UserId = ((User)this.HttpContext.Items["User"]!).Id,
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
        // TODO: Make video like media a option as well, requires extraction of frame to make thumbnail

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
        IEnumerable<string> tags = this._db.Tags
            .Where(t => t.Name.StartsWith(tag.ToLower())
                        || t.Name.EndsWith(tag.ToLower()))
            .Select(t => t.Name)
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
            ArraySegment<char> segTags = body.Tags.ToCharArray();
            List<ArraySegment<char>> sortedTags = new();

            int index = ((IList<char>)segTags).IndexOf(' ');
            while (index != -1)
            {
                sortedTags.Add(segTags[..index]);
                try
                {
                    segTags = segTags[++index..];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }

                index = ((IList<char>)segTags).IndexOf(' ');
            }

            sortedTags.Add(segTags);

            List<(TagType, string)> tags = new(sortedTags.Count);
            foreach (ArraySegment<char> arrSegTag in sortedTags)
            {
                foreach (char c in arrSegTag)
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
                        return BadRequest(new Error(400, ErrorCode.InvalidCharacters));
                    }
                }

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

                TagType tagType;
                if (colonIndex == -1)
                {
                    tagType = TagType.General;
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
                }

                tags.Add((tagType, tagName));
            }

            List<(TagType Type, string name)> tagsToAdd = new();
            Dictionary<string, PostTags> dict = new(post.Tags.Count);
            foreach (PostTags tag in post.Tags)
            {
                dict.Add(tag.Tag.Name, tag);
            }

            foreach ((TagType, string) entry in tags)
            {
                if (!dict.ContainsKey(entry.Item2))
                {
                    tagsToAdd.Add(entry);
                }
                else
                {
                    dict.Remove(entry.Item2);
                }
            }

            foreach ((string _, PostTags tagToRemove) in dict)
            {
                this._db.PostTags.Remove(tagToRemove);
            }

            foreach ((TagType, string) tagToAdd in tagsToAdd)
            {
                Tag? tag = this._db.Tags.FirstOrDefault(t => t.Name == tagToAdd.Item2);
                if (tag is null)
                {
                    Tag newTag = new()
                    {
                        Name = tagToAdd.Item2,
                        Type = tagToAdd.Item1
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
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

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

        this._db.PostTags.RemoveRange(post.Tags);
        this._db.Posts.Remove(post);
        await this._db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{id}/comments")]
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

        return Created($"{id}/comments", null);
    }

    [HttpGet("{id}/comments")]
    [Produces("application/json")]
    [ProducesResponseType<Error>(404)]
    [ProducesResponseType<IEnumerable<PostComment>>(200)]
    public IActionResult GetComments(long id, [FromQuery] int page = 1)
    {
        if (page >= 0)
        {
            page = 1;
        }

        Post? post = this._db.Posts
            .Include(p =>
                p.Comments.Skip((page - 1) * 10).Take(10)
            )
            .ThenInclude(c => c.User)
            .FirstOrDefault(p => p.Id == id);

        if (post is null)
        {
            return NotFound(new Error(404, ErrorCode.PostNotFound));
        }

        List<PostComment> comments = new(post.Comments.Count);
        foreach (Comment comment in post.Comments)
        {
            comments.Add(new PostComment
            {
                Content = comment.Content,
                Author = new PostAuthorResponse(comment.User.Id, comment.User.Username),
                CreatedAt = ((DateTimeOffset)comment.CreatedAt).ToUnixTimeMilliseconds(),
                Id = comment.Id
            });
        }

        return Ok(comments.OrderBy(c => c.CreatedAt));
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
