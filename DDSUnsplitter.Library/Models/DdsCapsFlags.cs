namespace DDSUnsplitter.Library.Models;

using System;

[Flags]
public enum DDSCaps : uint
{
    COMPLEX = 0x8,
    TEXTURE = 0x1000,
    MIPMAP = 0x400000,
    ALPHA = 0x1
}

[Flags]
public enum DDSCaps2: uint
{
    CUBEMAP = 0x200,
    CUBEMAP_POSITIVEX = 0x400,
    CUBEMAP_NEGATIVEX = 0x800,
    CUBEMAP_POSITIVEY = 0x1000,
    CUBEMAP_NEGATIVEY = 0x2000,
    CUBEMAP_POSITIVEZ = 0x4000,
    CUBEMAP_NEGATIVEZ = 0x8000,
    VOLUME = 0x200000,
    CUBEMAP_ALLFACES = CUBEMAP_POSITIVEX | CUBEMAP_NEGATIVEX |
                       CUBEMAP_POSITIVEY | CUBEMAP_NEGATIVEY |
                       CUBEMAP_POSITIVEZ | CUBEMAP_NEGATIVEZ
}

[Flags]
public enum DDSCaps3: uint
{
    // Reserved - no flags defined
}

[Flags]
public enum DDSCaps4: uint
{
    // Reserved - no flags defined
}