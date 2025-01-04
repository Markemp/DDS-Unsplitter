namespace DDSUnsplitter.Library.Models;


public sealed record DdsPixelFormat
{
    public uint Size { get; init; }

    public DDSPixelFormatFlags Flags { get; init; }

    public char[] FourCC { get; init; } = new char[4];

    public uint RGBBitCount { get; init; }
    public uint RBitMask { get; init; }
    public uint GBitMask { get; init; }
    public uint BBitMask { get; init; }
    public uint ABitMask { get; init; }
}

[Flags]
public enum DDSPixelFormatFlags : uint
{
    AlphaPixels = 0x1,
    Alpha = 0x2,
    FourCC = 0x4,
    RGB = 0x40,
    YUV = 0x200,
    Luminance = 0x20000
}