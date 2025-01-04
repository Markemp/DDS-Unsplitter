namespace DDSUnsplitter.Library.Models;

/// <summary>
/// Constants used throughout the DDS file processing
/// </summary>
public static class DdsConstants
{
    // File extensions and identifiers
    public const string DDS_EXTENSION = "dds";
    public const string DDS_MAGIC = "DDS ";
    public const string DX10_FOURCC = "DX10";
    public const int RESERVED1_SIZE = 11;

    // Header sizes
    public const int DDS_HEADER_SIZE = 128;
    public const int DXT10_HEADER_SIZE = 20;

    // CryEngine specific
    public const int CRYENGINE_END_MARKER_SIZE = 8;
    public static readonly byte[] CRYENGINE_END_MARKER = "CExtCEnd"u8.ToArray(); // "CExtCEnd"

    // Common texture types
    public const string DIFFUSE_SUFFIX = "_diff";
    public const string NORMAL_SUFFIX = "_ddna";
    public const string SPECULAR_SUFFIX = "_spec";
    public const string DISPLACEMENT_SUFFIX = "_displ";

    // File naming patterns
    public const string COMBINED_IDENTIFIER = "combined";
}