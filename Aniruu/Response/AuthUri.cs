namespace Aniruu.Response;

/// <summary>
/// A class that holds info about authentication URI's
/// </summary>
public class AuthUri
{
    /// <summary>
    /// The service name
    /// </summary>
    public required string Service { get; set; }
    /// <summary>
    /// Uri for authentication
    /// </summary>
    public required string Uri { get; set; }
}
