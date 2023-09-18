namespace Aniruu;

/// <summary>
/// Limitation for lengths of kinds.
/// </summary>
public class Limits
{
    public uint UsernameLimit { get; init; }
    public uint TagNameLimit { get; init; }

    public Limits(uint usernameLimit, uint tagNameLimit)
    {
        this.UsernameLimit = usernameLimit;
        this.TagNameLimit = tagNameLimit;
    }

    public Limits(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Limits");
        this.UsernameLimit = uint.Parse(section["UsernameLimit"]!);
        this.TagNameLimit = uint.Parse(section["TagNameLimit"]!);
    }
}
