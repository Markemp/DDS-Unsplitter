using System.Runtime.InteropServices;

namespace DDSUnsplitter.Library.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DdsPixelFormat
{
    public uint Size { get; init; }
    public DDSPixelFormatFlags Flags { get; init; }
    public CompressionMethods FourCC { get; init; }
    public int RGBBitCount { get; init; }
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


public enum CompressionMethods : uint
{
    // BitConverter.ToUInt32("DX10")
    DX10 = 0x30315844,
    
    // BitConverter.ToUInt32("DXT1")
    DXT1 = 0x31545844,
    
    // BitConverter.ToUInt32("DXT2")
    DXT2 = 0x32545844,
    
    // BitConverter.ToUInt32("DXT3")
    DXT3 = 0x33545844,
    
    // BitConverter.ToUInt32("DXT4")
    DXT4 = 0x34545844,
    
    // BitConverter.ToUInt32("DXT5")
    DXT5 = 0x35545844,
    
    // BitConverter.ToUInt32("BC4S")
    BC4S = 0x53344342,
    
    // BitConverter.ToUInt32("BC5S")
    BC5S = 0x53354342,
    
    // BitConverter.ToUInt32("ATI1")
    ATI1 = 0x31495441,
    
    // BitConverter.ToUInt32("ATI2")
    ATI2 = 0x32495441,
}