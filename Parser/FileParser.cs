using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

internal sealed class FileParser : IDisposable
{
    private const int DefaultCapacity = 500;
    private const byte NullCharacter = (byte)'\0';
    private const byte LineEnding = (byte)'\n';
    private const byte CarriageReturn = (byte)'\r';
    private const byte ValueDelimiter = (byte)';';

    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _memoryMappedViewAccessor;
    private readonly MemoryMappedFilePointer _memoryMappedFilePointer;

    public FileParser(string filePath)
    {
        _memoryMappedFile = MemoryMappedFile.CreateFromFile(
            filePath,
            FileMode.Open,
            null,
            0,
            MemoryMappedFileAccess.Read
        );

        _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor(
            0,
            0,
            MemoryMappedFileAccess.Read
        );

        unsafe
        {
            var ptr = (byte*)0;
            _memoryMappedViewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            _memoryMappedFilePointer = new MemoryMappedFilePointer(ptr);
        }
    }

    public string Process()
    {
        var chunks = SplitFile();

        var result = ProcessChunks(chunks);

        return BuildSummary(result);
    }

    public void Dispose()
    {
        _memoryMappedViewAccessor.Dispose();
        _memoryMappedFile.Dispose();
    }

    private Chunk[] SplitFile()
    {
        var chunkCount = Environment.ProcessorCount;
        var chunks = new Chunk[chunkCount];

        using var stream = _memoryMappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        var fileSize = stream.Length;
        var chunkSize = (int)Math.Ceiling(fileSize / (double)chunkCount);

        for (var i = 0; i < chunkCount; i++)
        {
            var start = stream.Position;
            var length = chunkSize;

            if (start + length < fileSize)
            {
                stream.Seek(length, SeekOrigin.Current);

                // Ensure that the chunk does not end in the middle of a line
                while (stream.ReadByte() != LineEnding && start + length < fileSize)
                {
                    length++;
                }
            }
            else
            {
                length = (int)(fileSize - start - 1);
            }

            chunks[i] = new Chunk(start, length + 1);
        }

        return chunks;
    }

    private SortedList<string, string> ProcessChunks(Chunk[] chunks)
    {
        var processResults = new Dictionary<Utf8String, Result>[chunks.Length];

        Parallel.For(
            0,
            chunks.Length,
            new ParallelOptions { MaxDegreeOfParallelism = chunks.Length, },
            index => processResults[index] = ProcessChunk(chunks[index])
        );

        return AggregatedResults(processResults);
    }

    private unsafe Dictionary<Utf8String, Result> ProcessChunk(Chunk chunk)
    {
        var results = new Dictionary<Utf8String, Result>(DefaultCapacity);

        var fileSpan = _memoryMappedFilePointer.GetFileSpan(chunk);
        var position = 0L;

        while (fileSpan.Length > 0 && fileSpan[0] != NullCharacter)
        {
            var delimiterIndex = fileSpan.IndexOf(ValueDelimiter);
            var lineEndIndex = fileSpan.IndexOf(LineEnding);
            var hasCarriageReturn = fileSpan[lineEndIndex - 1] == CarriageReturn;

            var strPtr = _memoryMappedFilePointer.GetPointer(chunk, position);
            var station = new Utf8String(strPtr, delimiterIndex);

            var measurement = DoubleParser.ParseUtf8(
                hasCarriageReturn
                    ? fileSpan[(delimiterIndex + 1)..(lineEndIndex - 1)]
                    : fileSpan[(delimiterIndex + 1)..lineEndIndex]
            );

            ref var result = ref CollectionsMarshal.GetValueRefOrAddDefault(
                results,
                station,
                out bool _
            );

            result.Aggregate(measurement);

            position += lineEndIndex + 1;
            fileSpan = fileSpan[(lineEndIndex + 1)..];
        }

        return results;
    }

    private SortedList<string, string> AggregatedResults(
        Dictionary<Utf8String, Result>[] processResults
    )
    {
        var aggregatedResults = new SortedList<string, string>(DefaultCapacity);
        var finalResult = processResults[0];
        var otherResults = processResults[1..];

        foreach (var dictionary in otherResults)
        {
            foreach (var kv in dictionary)
            {
                ref var result = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    finalResult,
                    kv.Key,
                    out bool _
                );

                result.Combine(kv.Value);
            }
        }

        // Convert to SortedList with string keys and formatted values
        foreach (var kv in finalResult)
        {
            aggregatedResults.Add(kv.Key.ToString(), kv.Value.ToString());
        }

        return aggregatedResults;
    }

    private static string BuildSummary(SortedList<string, string> result)
    {
        var count = result.Count;
        var index = 0;

        var stringBuilder = new StringBuilder("{");

        foreach (var pair in result)
        {
            stringBuilder.Append(pair.Key).Append(" = ").Append(pair.Value);

            if (++index < count)
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.Append('}');

        return stringBuilder.ToString();
    }

    private sealed record Chunk(long Start, int Length);

    private sealed unsafe class MemoryMappedFilePointer(byte* pointer)
    {
        public ReadOnlySpan<byte> GetFileSpan(Chunk chunk) =>
            new(pointer + chunk.Start, chunk.Length);

        public byte* GetPointer(Chunk chunk, long offset) => pointer + chunk.Start + offset;
    }
}
