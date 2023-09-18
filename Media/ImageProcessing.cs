using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualBasic;
using NetVips;
using Minio;
using static System.String;

namespace Media;

public class ImageProcessing
{
    public static int[] Sizes { get; }

    private readonly IMinioClient _client;

    static ImageProcessing()
    {
        Sizes = new[] { 320, 480, 720, 1080 };
        ModuleInitializer.Initialize();
        Console.WriteLine(!ModuleInitializer.VipsInitialized
            ? "Failed to initialize"
            : "Initialized VIPS");
    }

    public ImageProcessing(IMinioClient client)
    {
        this._client = client;
    }

    /// <summary>
    /// Processes the file and uploads it in multiple sizes
    /// </summary>
    /// <param name="stream">The stream for the file</param>
    /// <param name="fileExt">The extension for the file</param>
    /// <param name="checksum">A predefined checksum, generates one if null</param>
    /// <param name="ct">A cancellation token for early cancellation</param>
    /// <returns>The checksum for the file</returns>
    public async Task ProcessAndUploadAsync(
        Stream stream,
        string fileExt,
        string? checksum = null,
        CancellationToken ct = default
    )
    {
        using Image image = Image.NewFromStream(stream);

        List<Task> uploadTask = new(Sizes.Length + 1);

        if (checksum is null)
        {
            checksum = await CalculateChecksumAsync(stream, ct);
            stream.Seek(0, SeekOrigin.Begin);
        }

        foreach (int size in Sizes)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using Image resized = image.ThumbnailImage(
                size,
                size,
                Enums.Size.Down,
                false,
                Enums.Interesting.All
            );

            await using Stream resizedStream = new MemoryStream();
            resized.WriteToStream(resizedStream, ".webp");
            resizedStream.Seek(0, SeekOrigin.Begin);

            PutObjectArgs args = new PutObjectArgs()
                .WithStreamData(resizedStream)
                .WithObjectSize(resizedStream.Length)
                .WithBucket("aniruu")
                .WithObject($"{checksum}-{size}.webp")
                .WithContentType("image/webp");

            uploadTask.Add(this._client.PutObjectAsync(args, ct));
        }

        if (fileExt == ".jpg")
        {
            fileExt = ".jpeg";
        }

        fileExt = fileExt.Replace(".", Empty);

        string contentType = $"image/{fileExt}";

        stream.Seek(0, SeekOrigin.Begin);
        PutObjectArgs putArgs = new PutObjectArgs()
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithBucket("aniruu")
            .WithObject($"{checksum}.{fileExt}")
            .WithContentType(contentType);
        uploadTask.Add(this._client.PutObjectAsync(putArgs, ct));

        await Task.WhenAll(uploadTask);
    }

    public async Task<string> CalculateChecksumAsync(
        Stream stream,
        CancellationToken ct = default
    )
    {
        using SHA256 sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream, ct);
        string checksum = BitConverter.ToString(hash).Replace("-", Empty).ToLower();

        return checksum;
    }
}
