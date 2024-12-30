using System.Collections.Generic;

namespace DDSUnsplitter.Library;

public class DDSFileCombiner
{
    private const int DDS_HEADER_SIZE = 128;
    private const int CRYENGINE_METADATA_SIZE = 168;
    private const int CRYENGINE_END_MARKER_SIZE = 8;
    private readonly byte[] CRYENGINE_END_MARKER = new byte[] { 0x43, 0x45, 0x78, 0x74, 0x43, 0x45, 0x6E, 0x64 }; // "CExtCEnd"
    private const string DDS_EXTENSION = "dds";

    public static string Combine(string baseFileName, string combinedFileNameIdentifier = "combined")
    {
        if (baseFileName is null)
            return string.Empty;
        
        string? directory = Path.GetDirectoryName(baseFileName);
        
        if (directory is null)
            return baseFileName;

        // If directory doesn't exist, inform the user and exit the program
        if (!Directory.Exists(directory))
            throw new Exception("Directory does not exist.");

        string fileNameWithExtension = Path.GetFileName(baseFileName);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);
        // Check to see if fileNameWithoutExtension still has an extension (AW files).  If so, remove that extension too.
        fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithoutExtension);

        // Get all files that start with the base file name and have a numeric extension
        List<string> filesToCombine = new() { Path.Combine(directory, fileNameWithExtension) };

        var matchingFiles = Directory.GetFiles(directory, $"{fileNameWithoutExtension}*").ToList();
        matchingFiles.RemoveAll(file => file.Contains("combined", StringComparison.OrdinalIgnoreCase));

        // If no files to combine, inform the user and exit the program
        if (!matchingFiles.Any())
            throw new Exception("No matching part files found.");

        // Create a new combined file
        string combinedFileName = Path.Combine(directory, $"{fileNameWithoutExtension}.{combinedFileNameIdentifier}.{DDS_EXTENSION}");

        // If the first file in the list is already a valid DDS file, skip combining


        try
        {
            using FileStream combinedFileStream = new(combinedFileName, FileMode.Create);

            // Iterate over each file and append it to the combined file
            foreach (string filePath in filesToCombine)
            {
                using FileStream partFileStream = new(filePath, FileMode.Open);
                partFileStream.CopyTo(combinedFileStream);
                // Align the file to a 4-byte boundary
                // Pad to 4-byte boundary if necessary
                var padding = (int)(partFileStream.Length % 4);
                if (padding > 0)
                {
                    var paddingBytes = new byte[4 - padding];
                    partFileStream.Write(paddingBytes);
                }
            }

            return combinedFileName;
        }
        catch (Exception ex)
        {
            throw new Exception($"An error occurred: {ex.Message}");
        }
    }
}
