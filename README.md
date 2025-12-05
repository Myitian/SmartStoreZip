# SmartStoreZip
For large, difficult-to-compress files, use store mode instead of deflate mode to balance archive size and decompression time.

This program will perform a virtual compression using the `CompressionLevel.Fastest` preset to check if the file is suitable for compression. While this avoids performing `CompressionLevel.SmallestSize` compression on large, difficult-to-compress files, it can still increase compression time to some extent in some cases.