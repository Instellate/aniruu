namespace Aniruu.Utility;

public sealed class ArraySegCharComparer : IEqualityComparer<ArraySegment<char>>
{
    public bool Equals(ArraySegment<char> x, ArraySegment<char> y)
    {
        return x.AsSpan().SequenceEqual(y.AsSpan());
    }

    public int GetHashCode(ArraySegment<char> obj)
    {
        return string.GetHashCode(obj.AsSpan());
    }
}
