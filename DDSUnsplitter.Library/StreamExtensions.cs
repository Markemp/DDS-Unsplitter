using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DDSUnsplitter.Library;

public static class StreamExtensions
{
    public static void WriteStruct<T>(this Stream stream, T data) where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
        
        MemoryMarshal.Write(buffer, data);
        
        stream.Write(buffer);
    }
    
    public static T ReadStruct<T>(this Stream stream) where T : unmanaged
    {
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
        
        stream.ReadExactly(buffer);
        
        return MemoryMarshal.Read<T>(buffer);
    }
    
    public static T[] ReadArray<T>(this Stream stream, int count) where T : unmanaged
    {
        var items = new T[count];
        
        var bytes = MemoryMarshal.Cast<T, byte>(items);
        
        stream.ReadExactly(bytes);
        
        return items;
    }
}