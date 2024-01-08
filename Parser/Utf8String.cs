using System.Text;

// https://github.com/dotnet/runtime/blob/main/src/coreclr/tools/Common/Internal/Text/Utf8String.cs
internal readonly unsafe struct Utf8String(byte* pointer, int length) : IEquatable<Utf8String>
{
    private readonly byte* _pointer = pointer;
    private readonly int _length = length;

    public override bool Equals(object? obj) => obj is Utf8String other && Equals(other);

    public override string ToString() => Encoding.UTF8.GetString(new ReadOnlySpan<byte>(_pointer, _length));

    public bool Equals(Utf8String other)
    {
        var length = other._length;

        if (_length != length)
        {
            return false;
        }

        var a = _pointer;
        var b = other._pointer;

        while (length >= 4)
        {
            if (*(int*)a != *(int*)b)
            {
                return false;
            }

            a += 4;
            b += 4;
            length -= 4;
        }

        if (length >= 2)
        {
            if (*(short*)a != *(short*)b)
            {
                return false;
            }

            a += 2;
            b += 2;
            length -= 2;
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

        var hash1 = (5381 << 16) + 5381;
        var hash2 = hash1;

        for (var i = 0; i < _length; ++i)
        {
            var c = *ptr++;
            hash1 = unchecked((hash1 << 5) + hash1) ^ c;

            if (++i >= _length)
            {
                break;
            }

            c = *ptr++;
            hash2 = unchecked((hash2 << 5) + hash2) ^ c;
        }

        return unchecked(hash1 + (hash2 * 1566083941));
    }
}
