namespace DDSUnsplitter.Library.Models;

public class DDSDesc
{
    public string Name { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint Depth { get; set; }
    public uint Mips { get; set; }
    public uint MipsPersistent { get; set; }
    public uint Sides { get; set; }
    public uint BaseOffset { get; set; }
    public uint Flags { get; set; }
    //public TextureFormat Format { get; set; }
    //public TileMode TileMode { get; set; }
}