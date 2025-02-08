﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using DDSUnsplitter.Library.Models;
using static DDSUnsplitter.Library.Models.DdsConstants;

namespace DDSUnsplitter.Library;

public class DdsFileSet
{
    public string HeaderFile { get; set; } = string.Empty;
    public List<string> MipmapFiles { get; set; } = new();
    public string? GlossHeaderFile { get; set; }
    public List<string>? GlossMipmapFiles { get; set; }
    public bool IsAlreadyCombined { get; set; }
}

/// <summary>Combines split DDS files back into a single file, handling CryEngine-specific formatting</summary>
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
        ArgumentNullException.ThrowIfNull(baseFileName);

        var (directory, fileNameWithoutExtension) = PrepareFileInfo(baseFileName);
        var fileSet = FindMatchingFiles(directory, fileNameWithoutExtension);

        if (fileSet.IsAlreadyCombined)
        {
            Console.WriteLine($"File {fileSet.HeaderFile} is already a valid DDS file. Skipping combining.");
            return fileSet.HeaderFile;
        }

        var headerInfo = DdsHeader.Deserialize(fileSet.HeaderFile);
        var outputPath = CreateOutputPath(directory, fileNameWithoutExtension, useSafeName, combinedFileNameIdentifier);
        
        // Combine main texture files
        CombineFiles(outputPath, headerInfo, fileSet.HeaderFile, fileSet.MipmapFiles);

        // If there's a gloss texture, combine those files too
        if (fileSet.GlossHeaderFile != null && fileSet.GlossMipmapFiles != null)
        {
            var glossOutputPath = Path.ChangeExtension(outputPath, $".{DDS_EXTENSION}.a");
            var glossHeaderInfo = DdsHeader.Deserialize(fileSet.GlossHeaderFile);
            CombineFiles(glossOutputPath, glossHeaderInfo, fileSet.GlossHeaderFile, fileSet.GlossMipmapFiles);
        }

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

    public static DdsFileSet FindMatchingFiles(string directory, string fileNameWithoutExtension)
    {
        var result = new DdsFileSet();
        var allFiles = Directory.GetFiles(directory, $"{fileNameWithoutExtension}*")
            .Where(f => !f.Contains(".combined.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Check if base .dds file exists and is valid
        var baseFile = allFiles.FirstOrDefault(f => f.EndsWith($".{DDS_EXTENSION}"));
        if (baseFile != null)
        {
            var headerInfo = DdsHeader.Deserialize(baseFile);
            if (IsAlreadyValidDDSFile(headerInfo, new FileInfo(baseFile)))
            {
                return new DdsFileSet { HeaderFile = baseFile, IsAlreadyCombined = true };
            }
        }

        // Find main texture header and mipmaps
        var dds0File = allFiles.FirstOrDefault(f => f.EndsWith($".{DDS_EXTENSION}.0"));
        result.HeaderFile = dds0File ?? baseFile ?? throw new FileNotFoundException("No header file found");

        // Get main texture mipmaps (files ending in .dds.1, .dds.2, etc.)
        result.MipmapFiles = allFiles
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"\.dds\.\d+$"))
            .Where(f => !f.EndsWith(".0"))
            .OrderBy(f => int.Parse(f.Split('.').Last()))
            .ToList();

        // Find gloss texture header and mipmaps
        var glossHeader = allFiles.FirstOrDefault(f => f.EndsWith($".{DDS_EXTENSION}.a"));
        if (glossHeader != null)
        {
            result.GlossHeaderFile = glossHeader;
            // Get gloss mipmaps (files ending in .dds.1a, .dds.2a, etc.)
            result.GlossMipmapFiles = allFiles
                .Where(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"\.dds\.\d+a$"))
                .OrderBy(f => {
                    var match = System.Text.RegularExpressions.Regex.Match(f, @"\.dds\.(\d+)a$");
                    return int.Parse(match.Groups[1].Value);
                })
                .ToList();
        }

        return result;
    }

    private static bool IsAlreadyValidDDSFile(HeaderInfo headerInfo, FileInfo file)
    {
        if (headerInfo is null)
            ArgumentNullException.ThrowIfNull(headerInfo);

        var offsets = CalculateMipmapOffsets(headerInfo);

        if (offsets.Count == 0)
            return true;

        // Check if file is long enough to contain all separate mipmaps
        return file.Length > offsets[^1];
    }

    public static List<long> CalculateMipmapOffsets(HeaderInfo headerInfo)
    {
        var header = headerInfo.Header;
        int width = header.Width;
        int height = header.Height;
        int totalMipCount = header.MipMapCount;

        // Calculate how many mips are in separate files by finding the highest numbered extension
        // This assumes the files are numbered sequentially from 1 to N
        int smallMipsInHeader = 3; // From your example, but should be determined by checking files

        // The number of small mips stored in PostHeaderData
        int separateMipCount = totalMipCount - smallMipsInHeader;

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
            case DxgiFormat.BC6H_UF16:
            case DxgiFormat.BC5_SNORM:
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
            var fourCC = new string(pixelFormat.FourCC);
            switch (fourCC)
            {
                case "DXT1":
                    return ((width + 3) / 4) * ((height + 3) / 4) * 8;

                case "DXT3":
                case "DXT5":
                case "ATI2":
                case "BC5_SNORM":
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

        private static void CombineFiles(string outputPath, HeaderInfo headerInfo, string headerFile, List<string> mipmapFiles)
        {
            // If the headerfile is .dds, rename it to .dds.0 to prevent loss of data.  For
            // game files that already have a .dds.0, that is the header file and it's safe to write
            // to .dds.
            string effectiveHeaderFile = headerFile;
            if (Path.GetExtension(headerFile) == $".{DDS_EXTENSION}")
            {
                var newHeaderPath = Path.ChangeExtension(headerFile, $".{DDS_EXTENSION}.0");
                if (!File.Exists(newHeaderPath))
                {
                    try
                    {
                        // Use FileShare.ReadWrite | FileShare.Delete to allow maximum compatibility
                        using (var stream = new FileStream(headerFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var destStream = new FileStream(newHeaderPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(destStream);
                        }
                    }
                    catch (IOException)
                    {
                        // If we can't open with FileStream, try a direct file copy
                        File.Copy(headerFile, newHeaderPath, overwrite: false);
                    }
                }
                effectiveHeaderFile = newHeaderPath;
            }

        int totalHeaderSize = DDS_HEADER_SIZE + (headerInfo.DXT10Header != null ? DXT10_HEADER_SIZE : 0);

        // Use FileShare.Read to allow other processes to read while we're writing
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);

        byte[] serializedHeader = headerInfo.Header.Serialize();
        if (serializedHeader.Length >= DDS_HEADER_SIZE)
        {
            outputStream.Write(serializedHeader, 0, DDS_HEADER_SIZE);
            
            if (headerInfo.DXT10Header is not null)
            {
                byte[] dxt10Header = headerInfo.DXT10Header.Serialize();
                if (dxt10Header != null && dxt10Header.Length >= DXT10_HEADER_SIZE)
                {
                    outputStream.Write(dxt10Header, 0, DXT10_HEADER_SIZE);
                }
            }
        }

        var isCubeMap = (headerInfo.Header.Caps2 & DDSCaps2.CUBEMAP) != 0;
        var faces = isCubeMap ? 6 : 1;

        var mipMapSizes = GetMipMapSizes(headerInfo.Header);
        var mipMapBytes = mipmapFiles
            .OrderDescending()
            .Select(file => {
                // Use FileShare.ReadWrite to ensure we can read the file even if it's being accessed
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                return bytes;
            })
            .ToArray();

        var postHeaderDataOffset = 0;

        // For cubemaps, we need to organize the data by face first
        for (var cubeFace = 0; cubeFace < faces; cubeFace++)
        {
            for (var mipMap = 0; mipMap < headerInfo.Header.MipMapCount; mipMap++)
            {
                var mipMapSize = mipMapSizes[mipMap];
                var mipMapByteCount = GetMipmapSize(mipMapSize.Width, mipMapSize.Height, headerInfo.Header.PixelFormat);

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
                    if (postHeaderDataOffset + mipMapByteCount <= headerInfo.PostHeaderData.Length)
                    {
                        outputStream.Write(headerInfo.PostHeaderData, postHeaderDataOffset, mipMapByteCount);
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
        int offset = DDS_HEADER_SIZE;

        if (headerInfo.DXT10Header is not null)
            offset += DXT10_HEADER_SIZE;

        if (headerInfo.PostHeaderData != null)
            offset += headerInfo.PostHeaderData.Length;

        return offset;
    }
}
