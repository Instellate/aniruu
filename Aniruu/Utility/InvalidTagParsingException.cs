namespace Aniruu.Utility;

public sealed class InvalidTagParsingException : Exception
{
    public bool DuplicateTags { get; init; } = false;
    public bool InvalidChars { get; init; } = false;
}
