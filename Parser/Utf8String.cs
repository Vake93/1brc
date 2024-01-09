using System.Text;

// https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/Common/Internal/Text/Utf8String.cs
internal readonly unsafe struct Utf8String(byte* pointer, int length) : IEquatable<Utf8String>
{
    private readonly byte* _pointer = pointer;
    private readonly int _length = length;

    public override bool Equals(object? obj) => obj is Utf8String other && Equals(other);

    public override string ToString() =>
        Encoding.UTF8.GetString(new ReadOnlySpan<byte>(_pointer, _length));

    public bool Equals(Utf8String other)
    {
        var length = other._length;

        if (_length != length)
        {
            return false;
        }

        var a = _pointer;
        var b = other._pointer;

        while (length >= sizeof(int))
        {
            if (*(int*)a != *(int*)b)
            {
                return false;
            }
            a += sizeof(int);
            b += sizeof(int);
            length -= sizeof(int);
        }

        if (length >= sizeof(short))
        {
            if (*(short*)a != *(short*)b)
            {
                return false;
            }
            a += sizeof(short);
            b += sizeof(short);
            length -= sizeof(short);
        }

        if (length > 0)
        {
            if (*a != *b)
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var ptr = _pointer;
        var hash = (uint)0;

        if (_length > 0)
            hash = unchecked((hash * 31) + *ptr++);

        if (_length > 1)
            hash = unchecked((hash * 31) + *ptr++);

        if (_length > 2)
            hash = unchecked((hash * 31) + *ptr++);

        if (_length > 3)
            hash = unchecked((hash * 31) + *ptr++);

        return (int)hash;
    }
}
