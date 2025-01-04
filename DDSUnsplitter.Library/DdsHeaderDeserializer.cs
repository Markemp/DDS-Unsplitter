namespace DDSUnsplitter.Library;

using DDSUnsplitter.Library.Models;
using static DDSUnsplitter.Library.Models.DdsConstants;

public static class DdsHeaderDeserializer
{
    public static (DdsHeader Header, DXT10Header? DXT10) Deserialize(byte[] headerContent)
    {
        ValidateHeaderContent(headerContent);

        using var ms = new MemoryStream(headerContent);
        using var reader = new BinaryReader(ms);

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
            Caps = reader.ReadInt32(),
            Caps2 = reader.ReadInt32(),
            Caps3 = reader.ReadInt32(),
            Caps4 = reader.ReadInt32(),
            Reserved2 = reader.ReadInt32()
        };

        // Check if there's a DXT10 header and if we have enough data to read it
        DXT10Header? dxt10Header = null;
        if (IsDXT10Format(header) && ms.Position + DXT10_HEADER_SIZE <= ms.Length)
            dxt10Header = ReadDXT10Header(reader);

        return (header, dxt10Header);
    }

    private static bool IsDXT10Format(DdsHeader header) => new string(header.PixelFormat.FourCC) == "DX10";

    private static void SkipMagicNumberIfPresent(BinaryReader reader)
    {
        long originalPosition = reader.BaseStream.Position;
        string magic = new string(reader.ReadChars(DDS_MAGIC.Length));

        if (magic != DDS_MAGIC)
            reader.BaseStream.Position = originalPosition;
    }

    private static int[] ReadReserved1(BinaryReader reader)
    {
        return Enumerable.Range(0, RESERVED1_SIZE)
            .Select(_ => reader.ReadInt32())
            .ToArray();
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

    private static DXT10Header ReadDXT10Header(BinaryReader reader)
    {
        return new DXT10Header
        {
            DxgiFormat = (DXGI_FORMAT)reader.ReadUInt32(),
            ResourceDimension = (D3D10_RESOURCE_DIMENSION)reader.ReadUInt32(),
            MiscFlag = reader.ReadUInt32(),
            ArraySize = reader.ReadUInt32(),
            MiscFlags2 = (DDS_ALPHA_MODE)reader.ReadUInt32()
        };
    }

    private static void ValidateHeaderContent(byte[] headerContent)
    {
        if (headerContent == null)
        {
            throw new ArgumentNullException(nameof(headerContent));
        }

        if (headerContent.Length < DDS_HEADER_SIZE)
        {
            throw new ArgumentException(
                $"Header content must be at least {DDS_HEADER_SIZE} bytes long",
                nameof(headerContent));
        }
    }
}