using System.Diagnostics;
using System.Globalization;
using System.Text;

var stopwatch = Stopwatch.StartNew();

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: 1brc_parser <input_file_path>");
    Environment.Exit(1);
}

var filePath = args[0];

using var parser = new FileParser(filePath);

var results = parser.Process();

stopwatch.Stop();

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine(results);
Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");