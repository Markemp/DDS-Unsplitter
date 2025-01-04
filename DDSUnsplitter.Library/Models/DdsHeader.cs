namespace DDSUnsplitter.Library.Models;

public sealed record DdsHeader
{
    public int Size { get; init; }
    public int Flags { get; init; }
    public int Height { get; init; }
    public int Width { get; init; }
    public int PitchOrLinearSize { get; init; }
    public int Depth { get; init; }
    public int MipMapCount { get; init; }
    public required int[] Reserved1 { get; init; }
    public required DdsPixelFormat PixelFormat { get; init; }
    public int Caps { get; init; }
    public int Caps2 { get; init; }
    public int Caps3 { get; init; }
    public int Caps4 { get; init; }
    public int Reserved2 { get; init; }
}
