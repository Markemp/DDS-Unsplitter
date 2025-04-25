using System.Runtime.InteropServices;

namespace DDSUnsplitter.Library.Models;

public class HeaderInfo
{
    public DdsHeader Header { get; private init; }
    public DdsHeaderDXT10? DXT10Header { get; private init; }
    public byte[]? PostHeaderData { get; private init; }
    
    public static HeaderInfo Deserialize(Stream stream)
    {
        if (!stream.CanSeek) throw new ArgumentException("Stream must support seeking", nameof(stream));

        var before = stream.Position;
        //if we don't start with the magic, roll back to the start
        var magic = stream.ReadStruct<uint>();
        if (magic != BitConverter.ToUInt32(DdsConstants.DdsMagic))
            stream.Position = before;

        var header = stream.ReadStruct<DdsHeader>();

        DdsHeaderDXT10? dx10Header = null;
        if (header.PixelFormat.FourCC == CompressionMethods.DX10)
            dx10Header = stream.ReadStruct<DdsHeaderDXT10>();

        // if not at EOF, read the remaining to a byte array and store in HeaderInfo

        var remainingBytes = (int)(stream.Length - stream.Position);
        var postHeaderData = remainingBytes > 0 ? stream.ReadArray<byte>(remainingBytes) : null;
        
        return new HeaderInfo
        {
            Header = header,
            DXT10Header = dx10Header,
            PostHeaderData = postHeaderData
        };
    }

    public void Serialize(Stream stream)
    {
        stream.Write(DdsConstants.DdsMagic);
        stream.WriteStruct(Header);
        if (DXT10Header is {} dx10Header)
            stream.WriteStruct(dx10Header);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DdsHeader
{
    public int Size { get; init; }
    public int Flags { get; init; }
    public int Height { get; init; }
    public int Width { get; init; }
    public int PitchOrLinearSize { get; init; }
    public int Depth { get; init; }
    public int MipMapCount { get; init; }
    public DdsReserved1 Reserved1 { get; init; }
    public DdsPixelFormat PixelFormat { get; init; }
    public DDSCaps Caps { get; init; }
    public DDSCaps2 Caps2 { get; init; }
    public DDSCaps3 Caps3 { get; init; }
    public DDSCaps4 Caps4 { get; init; }
    public int Reserved2 { get; init; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DdsReserved1
{
    public readonly uint Reserved01;
    public readonly uint Reserved02;
    public readonly uint Reserved03;
    public readonly uint Reserved04;
    public readonly uint Reserved05;
    public readonly uint Reserved06;
    public readonly uint Reserved07;
    public readonly uint Reserved08;
    public readonly uint Reserved09;
    public readonly uint Reserved10;
    public readonly uint Reserved11;
}
