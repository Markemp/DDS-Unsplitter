using DDSUnsplitter.Library.Models;

namespace DDSUnsplitter.Library;

public static class DdsHeaderDeserializer
{
    public static DdsHeader Deserialize(byte[] headerContent)
    {
        using var ms = new MemoryStream(headerContent);
        using var reader = new BinaryReader(ms);

        // Skip the DDS magic number if present (first 4 bytes)
        if (new string(reader.ReadChars(4)) == "DDS ")
        {
            // Continue reading after magic number
        }
        else
        {
            // Reset position if no magic number
            ms.Position = 0;
        }

        return new DdsHeader
        {
            Size = reader.ReadInt32(),
            Flags = reader.ReadInt32(),
            Height = reader.ReadInt32(),
            Width = reader.ReadInt32(),
            PitchOrLinearSize = reader.ReadInt32(),
            Depth = reader.ReadInt32(),
            MipMapCount = reader.ReadInt32(),
            Reserved1 = Enumerable.Range(0, 11).Select(_ => reader.ReadInt32()).ToArray(),
            PixelFormat = ReadPixelFormat(reader),
            Caps = reader.ReadInt32(),
            Caps2 = reader.ReadInt32(),
            Caps3 = reader.ReadInt32(),
            Caps4 = reader.ReadInt32(),
            Reserved2 = reader.ReadInt32()
        };
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

// Usage example:
// byte[] headerContent = ...; // Your header bytes
// DdsHeader header = DdsHeaderDeserializer.Deserialize(headerContent);