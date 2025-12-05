using System.Collections.Concurrent;
using System.IO.Compression;

namespace SmartStoreZip;

sealed class Program
{
    static readonly EnumerationOptions options = new()
    {
        MatchType = MatchType.Simple,
        IgnoreInaccessible = true,
        RecurseSubdirectories = false
    };
    static readonly DateTime invalidDate = new(1980, 1, 1, 0, 0, 0);
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("""
                Usage:
                    SmartStoreZip <output> [inputs...]
                """);
            return;
        }
        string outputPath = args[0];
        using Stream output = outputPath is "-" ?
            Console.OpenStandardOutput() :
            File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using ZipArchive zip = new(output, ZipArchiveMode.Create);

        LengthCalculator lengthCalculator = new();
        foreach (string input in args
            .Skip(1)
            .Concat(ReadInput())
            .Distinct()
            .Select(Path.GetFullPath))
        {
            if (Path.GetDirectoryName(input) is not (string root and not ""))
                root = input;
            if (new FileInfo(input) is { Exists: true } fi)
                ProcessFile(zip, fi, root, lengthCalculator);
            else if (new DirectoryInfo(input) is { Exists: true } di)
            {
                foreach (FileSystemInfo fsi in EnumerateFileSystemLeavesIncludesSelf(di))
                {
                    if (fsi is FileInfo fiChild)
                        ProcessFile(zip, fiChild, root, lengthCalculator);
                    else if (fsi is DirectoryInfo)
                    {
                        string relativePath = $"{Path.GetRelativePath(root, fsi.FullName).Replace('\\', '/')}/";
                        DateTime lastWrite = fsi.LastWriteTime;
                        if (lastWrite.Year is < 1980 or > 2107)
                            lastWrite = invalidDate;
                        zip.CreateEntry(relativePath).LastWriteTime = lastWrite;
                    }
                }
            }
        }
    }
    static void ProcessFile(ZipArchive zip, FileInfo fi, string root, LengthCalculator? lengthCalculator = null)
    {
        string relativePath = Path.GetRelativePath(root, fi.FullName).Replace('\\', '/');
        CompressionLevel level = fi.Length < 1024 * 1024 || CalculateRatio(fi, lengthCalculator?.Reset()) < 0.9 ?
            CompressionLevel.SmallestSize :
            CompressionLevel.NoCompression;
        zip.CreateEntryFromFile(fi.FullName, relativePath, level);
    }
    static double CalculateRatio(FileInfo fi, LengthCalculator? lengthCalculator = null)
    {
        lengthCalculator ??= new();
        using (FileStream fs = fi.OpenRead())
        using (DeflateStream deflate = new(lengthCalculator, CompressionLevel.Fastest))
            fs.CopyTo(deflate);
        long fiLength = fi.Length;
        long deflateLength = lengthCalculator.Position;
        double ratio = (double)deflateLength / fiLength;
        Console.Error.WriteLine($"F {deflateLength}/{fiLength} = {ratio:F4} : {fi.FullName}");
        return ratio;
    }
    static IEnumerable<FileSystemInfo> EnumerateFileSystemLeavesIncludesSelf(DirectoryInfo dir)
    {
        int counter = 0;
        foreach (FileSystemInfo it in dir.EnumerateFileSystemInfos("*", options))
        {
            counter++;
            if (it is DirectoryInfo di)
                foreach (FileSystemInfo iit in EnumerateFileSystemLeavesIncludesSelf(di))
                    yield return iit;
            else
                yield return it;
        }
        if (counter == 0)
            yield return dir;
    }
    static IEnumerable<string> ReadInput()
    {
        BlockingCollection<string> queue = [];
        Console.CancelKeyPress += (sender, e) =>
        {
            if (queue.IsAddingCompleted)
                return; // hard exit
            e.Cancel = true;
            queue.CompleteAdding(); // soft exit
            Console.Error.WriteLine("Press Ctrl+C again to force exit.");
        };
        Task.Run(() =>
        {
            Console.Error.WriteLine("Input file/folders:");
            while (Console.In.ReadLine().AsSpan().Trim().Trim('"') is { IsEmpty: false } line && queue.TryAdd(new(line))) ;
            queue.CompleteAdding();
        });
        while (queue.TryTake(out string? item, Timeout.Infinite))
            yield return item;
    }
}