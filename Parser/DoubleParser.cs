internal static class DoubleParser
{
    private const byte DecimalPoint = (byte)'.';
    private const byte NegativeSign = (byte)'-';
    private const byte Zero = (byte)'0';

    public static double ParseUtf8(ReadOnlySpan<byte> bytes)
    {
        var decimalIndex = bytes.IndexOf(DecimalPoint);
        var isNegative = bytes[0] == NegativeSign;

        if (isNegative)
        {
            if (decimalIndex == -1)
            {
                return bytes.Length switch
                {
                    3 => -1 * ((bytes[1] - Zero) * 10 + (bytes[2] - Zero)),
                    2 => -1 * (bytes[1] - Zero),
                    _ => double.Parse(bytes)
                };
            }

            return decimalIndex switch
            {
                3
                    => -1
                        * (
                            (bytes[decimalIndex - 2] - Zero) * 10
                            + (bytes[decimalIndex - 1] - Zero)
                            + (bytes[decimalIndex + 1] - Zero) * 0.1
                        ),
                2
                    => -1
                        * (
                            (bytes[decimalIndex - 1] - Zero)
                            + (bytes[decimalIndex + 1] - Zero) * 0.1
                        ),
                _ => double.Parse(bytes)
            };
        }

        if (decimalIndex == -1)
        {
            return bytes.Length switch
            {
                2 => (bytes[0] - Zero) * 10 + (bytes[1] - Zero),
                1 => bytes[0] - Zero,
                _ => double.Parse(bytes)
            };
        }

        return decimalIndex switch
        {
            2
                => (bytes[decimalIndex - 2] - Zero) * 10
                    + (bytes[decimalIndex - 1] - Zero)
                    + (bytes[decimalIndex + 1] - Zero) * 0.1,
            1 => (bytes[decimalIndex - 1] - Zero) + (bytes[decimalIndex + 1] - Zero) * 0.1,
            _ => double.Parse(bytes)
        };
    }
}
