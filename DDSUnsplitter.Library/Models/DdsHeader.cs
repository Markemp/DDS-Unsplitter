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
    public DDSCaps Caps { get; init; }
    public DDSCaps2 Caps2 { get; init; }
    public DDSCaps3 Caps3 { get; init; }
    public DDSCaps4 Caps4 { get; init; }
    public int Reserved2 { get; init; }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Size);
        writer.Write(Flags);
        writer.Write(Height);
        writer.Write(Width);
        writer.Write(PitchOrLinearSize);
        writer.Write(Depth);
        writer.Write(MipMapCount);
        WriteReserved1(writer);
        WritePixelFormat(writer);
        writer.Write((int)Caps);
        writer.Write((int)Caps2);
        writer.Write((int)Caps3);
        writer.Write((int)Caps4);
        writer.Write(Reserved2);

        return stream.ToArray();
    }

    private void WriteReserved1(BinaryWriter writer)
    {
        foreach (var value in Reserved1)
        {
            writer.Write(value);
        }
    }

    private void WritePixelFormat(BinaryWriter writer)
    {
        writer.Write(PixelFormat.Size);
        writer.Write((uint)PixelFormat.Flags);
        writer.Write(PixelFormat.FourCC);
        writer.Write(PixelFormat.RGBBitCount);
        writer.Write(PixelFormat.RBitMask);
        writer.Write(PixelFormat.GBitMask);
        writer.Write(PixelFormat.BBitMask);
        writer.Write(PixelFormat.ABitMask);
    }

    private static DdsPixelFormat ReadPixelFormat(BinaryReader reader)
    {
        return new DdsPixelFormat
        {
            Size = reader.ReadUInt32(),
            Flags = (DDSPixelFormatFlags)reader.ReadUInt32(),
            FourCC = reader.ReadChars(4),
            RGBBitCount = reader.ReadUInt32(),
            RBitMask = reader.ReadUInt32(),
            GBitMask = reader.ReadUInt32(),
            BBitMask = reader.ReadUInt32(),
            ABitMask = reader.ReadUInt32()
        };
    }
}
