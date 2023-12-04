using System.Buffers;

namespace Aniruu.Utility;

public sealed class TagParsing
{
    private static readonly SearchValues<char> AllowedNameChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyz1234567890_.:()!?$@~+-");

    public List<string> Allowed { get; } = new();
    public List<string> Disallowed { get; } = new();

    public void ProcessString(string value)
    {
        ReadOnlySpan<char> span = value.AsSpan();
        int spaceAmount = 0;
        foreach (char chr in span)
        {
            if (chr == ' ')
            {
                spaceAmount++;
            }
        }

        Span<Range> ranges = stackalloc Range[spaceAmount];
        int amount = span.Split(ranges, ' ');

        for (int i = 0; i < amount; i++)
        {
            ReadOnlySpan<char> tag = span[ranges[i]];
            if (tag.IsEmpty)
            {
                continue;
            }

            ReadOnlySpan<char> part = tag[0] == '-' ? tag[1..] : tag;
            bool containsValidChars = part.IndexOfAnyExcept(AllowedNameChars) < 0;
            if (!containsValidChars)
            {
                throw new InvalidTagParsingException()
                {
                    InvalidChars = true
                };
            }

            if (tag[0] == '-')
            {
                this.Disallowed.Add(tag.ToString());
            }
            else
            {
                this.Allowed.Add(tag.ToString());
            }
        }
    } // TODO: Complete
}
