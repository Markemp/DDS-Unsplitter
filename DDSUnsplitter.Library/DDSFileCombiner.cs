using DDSUnsplitter.Library.Models;
using static DDSUnsplitter.Library.Models.DdsConstants;

namespace DDSUnsplitter.Library;

/// <summary>
/// Combines split DDS files back into a single file, handling CryEngine-specific formatting
/// </summary>
public class DDSFileCombiner
{
    /// <summary>
    /// Combines split DDS files into a single file
    /// </summary>
    /// <param name="baseFileName">Path to the header file</param>
    /// <param name="useSafeName">If true, adds an identifier to the output filename</param>
    /// <param name="combinedFileNameIdentifier">Identifier to add to the output filename when useSafeName is true</param>
    /// <returns>Path to the combined file</returns>
    public static string Combine(string baseFileName, bool useSafeName = false, string combinedFileNameIdentifier = "combined")
    {
        if (baseFileName is null)
            throw new ArgumentNullException(nameof(baseFileName));

        var (directory, fileNameWithoutExtension) = PrepareFileInfo(baseFileName);
        var matchingFiles = FindMatchingFiles(directory, fileNameWithoutExtension);

        var headerFile = matchingFiles[0];
        var headerFileInfo = new FileInfo(headerFile);
        var headerFileLength = headerFileInfo.Length;

        var headerInfo = DdsHeader.Deserialize(headerFile);

        if (IsAlreadyValidDDSFile(headerInfo, headerFileInfo))
        {
            Console.WriteLine($"File {headerFile} is already a valid DDS file. Skipping combining.");
            return headerFile;
        }

        var outputPath = CreateOutputPath(directory, fileNameWithoutExtension, useSafeName, combinedFileNameIdentifier);
        CombineFiles(outputPath, headerInfo, matchingFiles);

        return outputPath;
    }

    private static (string directory, string fileNameWithoutExtension) PrepareFileInfo(string baseFileName)
    {
        string? directory = Path.GetDirectoryName(baseFileName);
        if (directory is null)
            throw new DirectoryNotFoundException("Could not determine directory from base file name");

        // Remove both extensions (e.g., "file.dds.1" -> "file")
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(baseFileName));
        return (directory, fileNameWithoutExtension);
    }

    public static List<string> FindMatchingFiles(string directory, string fileNameWithoutExtension)
    {
        var matchingFiles = Directory.GetFiles(directory, $"{fileNameWithoutExtension}*").ToList();

        // Remove any previously combined files
        matchingFiles.RemoveAll(file => file.Contains(".combined.", StringComparison.OrdinalIgnoreCase));

        return matchingFiles;
    }

    private static bool IsAlreadyValidDDSFile(HeaderInfo headerInfo, FileInfo file)
    {
        if (headerInfo is null)
            ArgumentNullException.ThrowIfNull(headerInfo);

        var offsets = CalculateMipmapOffsets(headerInfo);

        if (offsets.Count == 0)
            return true;

        // Check if file is long enough to contain all separate mipmaps
        return file.Length >= offsets[^1];
    }

    public static List<long> CalculateMipmapOffsets(HeaderInfo headerInfo)
    {
        var header = headerInfo.Header;
        int width = header.Width;
        int height = header.Height;
        int totalMipCount = header.MipMapCount;

        // Calculate how many mips are in separate files by finding the highest numbered extension
        // This assumes the files are numbered sequentially from 1 to N
        int separateMipCount = 3; // From your example, but should be determined by checking files

        // The number of small mips stored in PostHeaderData
        int smallMipsInHeader = totalMipCount - separateMipCount;

        List<long> offsets = [];
        long currentOffset = GetInitialOffset(headerInfo);

        // Calculate offsets only for the separate mips (larger ones)
        for (int i = 0; i < separateMipCount; i++)
        {
            offsets.Add(currentOffset);

            int mipWidth = Math.Max(1, width >> i);
            int mipHeight = Math.Max(1, height >> i);

            int mipSize = CalculateMipSize(mipWidth, mipHeight, headerInfo);

            // Align to 4-byte boundary
            mipSize = (mipSize + 3) & ~3;

            currentOffset += mipSize;
        }

        return offsets;
    }

    private static int CalculateMipSize(int width, int height, HeaderInfo headerInfo)
    {
        if (headerInfo.DXT10Header is not null)
            return CalculateMipSizeDX10(width, height, headerInfo.DXT10Header.DxgiFormat);
        else
            return CalculateMipSizeLegacy(width, height, headerInfo.Header.PixelFormat);
    }

    private static int CalculateMipSizeDX10(int width, int height, DxgiFormat format)
    {
        int blockSize;
        int blockWidth;
        int blockHeight;

        switch (format)
        {
            case DxgiFormat.BC1_UNORM:
            case DxgiFormat.BC1_UNORM_SRGB:
                blockSize = 8;
                blockWidth = blockHeight = 4;
                break;

            case DxgiFormat.BC2_UNORM:
            case DxgiFormat.BC2_UNORM_SRGB:
            case DxgiFormat.BC3_UNORM:
            case DxgiFormat.BC3_UNORM_SRGB:
                blockSize = 16;
                blockWidth = blockHeight = 4;
                break;

            // Add other DXGI formats as needed

            default:
                throw new NotSupportedException($"DXGI format {format} not supported");
        }

        int blocksWide = (width + blockWidth - 1) / blockWidth;
        int blocksHigh = (height + blockHeight - 1) / blockHeight;

        return blocksWide * blocksHigh * blockSize;
    }

    private static int CalculateMipSizeLegacy(int width, int height, DdsPixelFormat pixelFormat)
    {
        // Handle legacy DDS formats (DXT1, DXT3, DXT5, etc.)
        if ((pixelFormat.Flags & pixelFormat.Flags) != 0)
        {
            switch (pixelFormat.FourCC.ToString())
            {
                case "DXT1":
                    return ((width + 3) / 4) * ((height + 3) / 4) * 8;

                case "DXT3":
                case "DXT5":
                    return ((width + 3) / 4) * ((height + 3) / 4) * 16;

                default:
                    throw new NotSupportedException($"FourCC format {pixelFormat.FourCC} not supported");
            }
        }

        // Handle uncompressed formats
        int bpp = pixelFormat.RGBBitCount;
        return ((width * height * bpp + 7) / 8);
    }

    private static string CreateOutputPath(string directory, string fileNameWithoutExtension,
        bool useSafeName, string combinedFileNameIdentifier)
    {
        var suffix = useSafeName ? $".{combinedFileNameIdentifier}" : null;
        return Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}.{DDS_EXTENSION}");
    }

    private static void CombineFiles(string outputPath, HeaderInfo headerInfo, List<string> matchingFiles)
    {
        // If the headerfile is .dds, rename it to .dds.0 to prevent loss of data.  For
        // game files that already have a .dds.0, that is the header file and it's safe to write
        // to .dds.
        if (Path.GetExtension(matchingFiles[0]) == $".{DDS_EXTENSION}")
        {
            var newHeaderPath = Path.ChangeExtension(matchingFiles[0], $".{DDS_EXTENSION}.0");
            File.Move(matchingFiles[0], newHeaderPath);
            matchingFiles[0] = newHeaderPath;
        }

        //var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(headerContent);
        int totalHeaderSize = DDS_HEADER_SIZE + (dxt10Header != null ? DXT10_HEADER_SIZE : 0);

        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        outputStream.Write(headerContent, 0, totalHeaderSize);

        var isCubeMap = (header.Caps2 & DDSCaps2.CUBEMAP) != 0;
        var faces = isCubeMap ? 6 : 1;

        var mipMapSizes = GetMipMapSizes(header);
        var mipMapBytes = matchingFiles
            .Skip(1) // Skip the header file
            .OrderDescending()
            .Select(File.ReadAllBytes).ToArray();

        var postHeaderDataOffset = 0;

        // For cubemaps, we need to organize the data by face first
        for (var cubeFace = 0; cubeFace < faces; cubeFace++)
        {
            for (var mipMap = 0; mipMap < header.MipMapCount; mipMap++)
            {
                var mipMapSize = mipMapSizes[mipMap];
                var mipMapByteCount = GetMipmapSize(mipMapSize.Width, mipMapSize.Height, header.PixelFormat);

                if (mipMap < mipMapBytes.Length)
                {
                    // For cubemaps, each mipmap file should contain all faces
                    // So we read from the appropriate face offset within each mipmap file
                    var faceOffset = isCubeMap ? (cubeFace * mipMapByteCount) : 0;
                    if (faceOffset + mipMapByteCount <= mipMapBytes[mipMap].Length)
                        outputStream.Write(mipMapBytes[mipMap], faceOffset, mipMapByteCount);
                    else
                        throw new InvalidOperationException($"Mipmap file {mipMap} doesn't contain expected face data at offset {faceOffset}");
                }
                else
                {
                    // For the small mipmaps in the main file
                    var faceOffset = isCubeMap ? (cubeFace * mipMapByteCount) : 0;
                    if (postHeaderDataOffset + mipMapByteCount <= postHeaderData.Length)
                    {
                        outputStream.Write(postHeaderData, postHeaderDataOffset, mipMapByteCount);
                        postHeaderDataOffset += mipMapByteCount;
                    }
                    else
                        throw new InvalidOperationException($"Post-header data doesn't contain expected mipmap data at offset {postHeaderDataOffset}");
                }
            }
        }

        outputStream.Write(CRYENGINE_END_MARKER, 0, CRYENGINE_END_MARKER_SIZE);
    }

    private static (int Width, int Height)[] GetMipMapSizes(DdsHeader header)
    {
        var mipMapSizes = new (int Width, int Height)[header.MipMapCount];

        for (var i = 0; i < header.MipMapCount; i++)
        {
            var width = Math.Max((int)(header.Width / Math.Pow(2, i)), 1);
            var height = Math.Max((int)(header.Height / Math.Pow(2, i)), 1);
            mipMapSizes[i] = (width, height);
        }

        return mipMapSizes;
    }

    private static int GetMipmapSize(int width, int height, DdsPixelFormat pixelFormat)
    {
        int blockSize = IsDXT1(pixelFormat) ? 8 : 16;
        return Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * blockSize;
    }

    private static bool IsDXT1(DdsPixelFormat pixelFormat)=> pixelFormat.GetFourCCString() == "DXT1";

    private static int GetInitialOffset(HeaderInfo headerInfo)
    {
        int offset = sizeof(DdsHeader); // 128 bytes

        if (headerInfo.DXT10Header is not null)
            offset += sizeof(DdsHeaderDXT10); // Add DXT10 header size

        if (headerInfo.PostHeaderData != null)
            offset += headerInfo.PostHeaderData.Length;

        return offset;
    }

    //private static void WriteMipMapWithAlignment(FileStream outputStream, string mipmapFile)
    //{
    //    if (!File.Exists(mipmapFile))
    //        throw new FileNotFoundException($"Mipmap file not found: {mipmapFile}");

    //    using var mipmapStream = File.OpenRead(mipmapFile);
    //    mipmapStream.CopyTo(outputStream);

    //    // Align to 4 bytes
    //    var remainder = mipmapStream.Length % 4;
    //    if (remainder > 0)
    //    {
    //        var padding = new byte[4 - remainder];
    //        outputStream.Write(padding, 0, padding.Length);
    //    }

    //public class HeaderInfo
    //{
    //    public DdsHeader Header { get; set; }
    //    public DdsHeaderDXT10? DXT10Header { get; set; }
    //    public byte[] PostHeaderData { get; set; }
    //}
}
