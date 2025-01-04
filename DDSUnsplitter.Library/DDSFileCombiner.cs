namespace DDSUnsplitter.Library;

/// <summary>
/// Combines split DDS files back into a single file, handling CryEngine-specific formatting
/// </summary>
public class DDSFileCombiner
{
    private const int DDS_HEADER_SIZE = 128;
    private const int CRYENGINE_END_MARKER_SIZE = 8;
    private static readonly byte[] CRYENGINE_END_MARKER = new byte[] { 0x43, 0x45, 0x78, 0x74, 0x43, 0x45, 0x6E, 0x64 }; // "CExtCEnd"
    private const string DDS_EXTENSION = "dds";

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

        if (IsAlreadyValidDDSFile(headerFile, headerContent))
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

    private static List<string> FindMatchingFiles(string directory, string fileNameWithoutExtension)
    {
        var matchingFiles = Directory.GetFiles(directory, $"{fileNameWithoutExtension}*").ToList();
        matchingFiles.RemoveAll(file => file.Contains("combined", StringComparison.OrdinalIgnoreCase));
        return matchingFiles;
    }

    private static (byte[] headerContent, byte[] postHeaderData) ProcessHeaderFile(string headerFile)
    {
        using var headerStream = File.OpenRead(headerFile);
        var headerContent = new byte[headerStream.Length];
        headerStream.Read(headerContent, 0, headerContent.Length);

        var postHeaderData = new byte[headerContent.Length - DDS_HEADER_SIZE];
        Array.Copy(headerContent, DDS_HEADER_SIZE, postHeaderData, 0, postHeaderData.Length);

        return (headerContent, postHeaderData);
    }

    private static bool IsAlreadyValidDDSFile(string headerFile, byte[] headerContent)
    {
        var header = DdsHeaderDeserializer.Deserialize(headerContent);
        var estimatedMinSize = headerContent.Length + (header.Width * header.Height * header.PixelFormat.Size / 8);
        var fileSize = new FileInfo(headerFile).Length;

        return fileSize > estimatedMinSize;
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
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        // Write header
        outputStream.Write(headerContent, 0, DDS_HEADER_SIZE);

        // Write mipmaps in reverse order
        for (int i = matchingFiles.Count - 1; i > 0; i--)
        {
            WriteMipMapWithAlignment(outputStream, matchingFiles[i]);
        }

        // Write footer
        outputStream.Write(postHeaderData, 0, postHeaderData.Length);
        outputStream.Write(CRYENGINE_END_MARKER, 0, CRYENGINE_END_MARKER_SIZE);
    }

    private static void WriteMipMapWithAlignment(FileStream outputStream, string mipmapFile)
    {
        if (!File.Exists(mipmapFile))
            throw new FileNotFoundException($"Mipmap file not found: {mipmapFile}");

        using var mipmapStream = File.OpenRead(mipmapFile);
        mipmapStream.CopyTo(outputStream);

        // Align to 4 bytes
        var remainder = mipmapStream.Length % 4;
        if (remainder > 0)
        {
            var padding = new byte[4 - remainder];
            outputStream.Write(padding, 0, padding.Length);
        }
    }
}