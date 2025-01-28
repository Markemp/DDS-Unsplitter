namespace DDSUnsplitter.Library.Models;

using static DDSUnsplitter.Library.Models.DdsConstants;

public class HeaderInfo
{
    public DdsHeader Header { get; set; }
    public DdsHeaderDXT10? DXT10Header { get; set; }
    public byte[]? PostHeaderData { get; set; }
}

public sealed record DdsHeader
{
    public int Size { get; init; }
    public int Flags { get; init; }
    public int Height { get; init; }
    public int Width { get; init; }
    public int PitchOrLinearSize { get; init; }
    public int Depth { get; init; }
    public int MipMapCount { get; init; }
    public int[] Reserved1 { get; init; }
    public DdsPixelFormat PixelFormat { get; init; }
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

    public byte[]? SerializeDXT10Header(HeaderInfo headerInfo)
    {
        if (headerInfo.DXT10Header is null)
            return null;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((uint)headerInfo.DXT10Header.DxgiFormat);
        writer.Write((uint)headerInfo.DXT10Header.ResourceDimension);
        writer.Write((uint)headerInfo.DXT10Header.MiscFlag);
        writer.Write(headerInfo.DXT10Header.ArraySize);
        writer.Write((uint)headerInfo.DXT10Header.MiscFlags2);

        return stream.ToArray();
    }

    public static HeaderInfo Deserialize(string headerFile)
    {
        byte[] fileContent = File.ReadAllBytes(headerFile);
        using var stream = new MemoryStream(fileContent);
        using var reader = new BinaryReader(stream);

        ValidateHeaderContent(fileContent);

        SkipMagicNumberIfPresent(reader);
        var header = new DdsHeader
        {
            Size = reader.ReadInt32(),
            Flags = reader.ReadInt32(),
            Height = reader.ReadInt32(),
            Width = reader.ReadInt32(),
            PitchOrLinearSize = reader.ReadInt32(),
            Depth = reader.ReadInt32(),
            MipMapCount = reader.ReadInt32(),
            Reserved1 = ReadReserved1(reader),
            PixelFormat = ReadPixelFormat(reader),
            Caps = (DDSCaps)reader.ReadInt32(),
            Caps2 = (DDSCaps2)reader.ReadInt32(),
            Caps3 = (DDSCaps3)reader.ReadInt32(),
            Caps4 = (DDSCaps4)reader.ReadInt32(),
            Reserved2 = reader.ReadInt32()
        };

        DdsHeaderDXT10? dxt10Header = null;
        if (IsDXT10Format(header) && stream.Position + DXT10_HEADER_SIZE <= stream.Length)
            dxt10Header = ReadDXT10Header(reader);

        // if not at EOF, read the remaining to a byte array and store in HeaderInfo
        int remainingBytes = (int)(stream.Length - stream.Position);
        byte[] postHeaderData = new byte[remainingBytes];
        stream.Read(postHeaderData, 0, remainingBytes);

        return new HeaderInfo()
        {
            Header = header,
            DXT10Header = dxt10Header,
            PostHeaderData = postHeaderData
        };
    }

    private void WriteReserved1(BinaryWriter writer)
    {
        foreach (var value in Reserved1)
        {
            writer.Write(value);
        }
    }

    private static int[] ReadReserved1(BinaryReader reader) =>
        Enumerable.Range(0, RESERVED1_SIZE)
            .Select(_ => reader.ReadInt32())
            .ToArray();

    private static void SkipMagicNumberIfPresent(BinaryReader reader)
    {
        long originalPosition = reader.BaseStream.Position;
        string magic = new string(reader.ReadChars(DDS_MAGIC.Length));

        if (magic != DDS_MAGIC)
            reader.BaseStream.Position = originalPosition;
    }

    private static bool IsDXT10Format(DdsHeader header) => new string(header.PixelFormat.FourCC) == "DX10";

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

    private static void ValidateHeaderContent(byte[] headerContent)
    {
        if (headerContent is null)
            throw new ArgumentNullException(nameof(headerContent));

        if (headerContent.Length < DDS_HEADER_SIZE)
        {
            throw new ArgumentException(
                $"Header content must be at least {DDS_HEADER_SIZE} bytes long",
                nameof(headerContent));
        }
    }

    private static DdsHeaderDXT10 ReadDXT10Header(BinaryReader reader) =>
        new()
        {
            DxgiFormat = (DXGI_FORMAT)reader.ReadUInt32(),
            ResourceDimension = (D3D10_RESOURCE_DIMENSION)reader.ReadUInt32(),
            MiscFlag = reader.ReadUInt32(),
            ArraySize = reader.ReadUInt32(),
            MiscFlags2 = (DDS_ALPHA_MODE)reader.ReadUInt32()
        };

    private static DdsPixelFormat ReadPixelFormat(BinaryReader reader) =>
        new()
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
