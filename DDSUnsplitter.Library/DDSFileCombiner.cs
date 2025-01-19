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
        ValidateInputParameters(baseFileName);

        var (directory, fileNameWithoutExtension) = PrepareFileInfo(baseFileName);
        var matchingFiles = FindMatchingFiles(directory, fileNameWithoutExtension);

        var headerFile = matchingFiles[0];
        var (headerContent, postHeaderData) = ProcessHeaderFile(headerFile);

        if (IsAlreadyValidDDSFile(headerFile))
        {
            Console.WriteLine($"File {headerFile} is already a valid DDS file. Skipping combining.");
            return headerFile;
        }

        var outputPath = CreateOutputPath(directory, fileNameWithoutExtension, useSafeName, combinedFileNameIdentifier);
        CombineFiles(outputPath, headerContent, postHeaderData, matchingFiles);

        return outputPath;
    }

    private static void ValidateInputParameters(string baseFileName)
    {
        if (baseFileName is null)
            throw new ArgumentNullException(nameof(baseFileName));
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

        // Remove .a variant files
        matchingFiles.RemoveAll(file =>
        {
            string fileName = file;

            // Handle .dds.a case
            if (fileName.EndsWith(".dds.a", StringComparison.OrdinalIgnoreCase))
                return true;

            // Handle .dds.#a case
            if (fileName.EndsWith("a", StringComparison.OrdinalIgnoreCase))
            {
                // Get the part before the 'a'
                string withoutA = fileName.Substring(0, fileName.Length - 1);
                string numberExt = Path.GetExtension(withoutA);

                // Check if it's a number
                if (!string.IsNullOrEmpty(numberExt) &&
                    numberExt.Length > 1 &&
                    numberExt[1..].All(char.IsDigit))
                {
                    return true;
                }
            }

            return false;
        });

        return matchingFiles;
    }

    private static (byte[] headerContent, byte[] postHeaderData) ProcessHeaderFile(string headerFile)
    {
        using var headerStream = File.OpenRead(headerFile);
        var headerContent = new byte[headerStream.Length];
        headerStream.Read(headerContent, 0, headerContent.Length);

        // Deserialize to check for DXT10
        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(headerContent);

        // Calculate where the actual post-header data starts
        int headerSize = DdsConstants.DDS_HEADER_SIZE +
                        (dxt10Header != null ? DdsConstants.DXT10_HEADER_SIZE : 0);

        // Calculate the size of the post-header data
        int postHeaderSize = headerContent.Length - headerSize;

        // Extract only the true post-header data (after both headers)
        var postHeaderData = new byte[postHeaderSize];
        Array.Copy(headerContent, headerSize, postHeaderData, 0, postHeaderSize);

        return (headerContent, postHeaderData);
    }

    private static bool IsAlreadyValidDDSFile(string headerFile)
    {
        using var stream = File.OpenRead(headerFile);
        if (stream.Length < CRYENGINE_END_MARKER_SIZE)
            return false;

        // Read and check headers with proper tuple deconstruction
        var headerData = new byte[DDS_HEADER_SIZE];
        stream.Read(headerData, 0, DDS_HEADER_SIZE);
        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(headerData);

        // Calculate minimum valid size based on headers present
        int minSize = DDS_HEADER_SIZE +
                     (dxt10Header != null ? DXT10_HEADER_SIZE : 0) +
                     CRYENGINE_END_MARKER_SIZE;

        if (stream.Length < minSize)
            return false;

        // Check end marker
        stream.Seek(-CRYENGINE_END_MARKER_SIZE, SeekOrigin.End);
        var endMarker = new byte[CRYENGINE_END_MARKER_SIZE];
        stream.Read(endMarker, 0, CRYENGINE_END_MARKER_SIZE);

        return endMarker.SequenceEqual(CRYENGINE_END_MARKER);
    }

    private static string CreateOutputPath(string directory, string fileNameWithoutExtension,
        bool useSafeName, string combinedFileNameIdentifier)
    {
        var suffix = useSafeName ? $".{combinedFileNameIdentifier}" : null;
        return Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}.{DDS_EXTENSION}");
    }

    private static void CombineFiles(string outputPath, byte[] headerContent, byte[] postHeaderData,
        List<string> matchingFiles)
    {
        var (header, dxt10Header) = DdsHeaderDeserializer.Deserialize(headerContent);
        int totalHeaderSize = DDS_HEADER_SIZE + (dxt10Header != null ? DXT10_HEADER_SIZE : 0);

        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        outputStream.Write(headerContent, 0, totalHeaderSize);

        var isCubeMap = (header.Caps2 & 0x200) != 0;
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
}
