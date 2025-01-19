namespace DDSUnsplitter.Library.Models;

public sealed class ChunkInfo
{
    public string FileName { get; set; }
    public uint MipLevel { get; set; }
    public uint OffsetInFile { get; set; }
    public uint SizeInFile { get; set; }
    public uint SideDelta { get; set; }
}