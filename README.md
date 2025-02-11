# DDS-Unsplitter

A utility for combining split DDS (DirectDraw Surface) texture files, specifically designed for 
handling CryEngine texture formats.

## Overview

DDS-Unsplitter combines split DDS texture files back into their original form. It's particularly useful 
for working with CryEngine textures where a single texture might be split into multiple files 
(a header file and several mipmap files).

### Features

- Combines split DDS files into a single file
- Handles both standard DDS and DXT10 formats
- Preserves CryEngine-specific formatting
- Handles numbered header files (.dds.0) and regular headers (.dds)
- Maintains proper byte alignment
- Option for safe file handling (prevents overwriting originals)
- Properly handles DDS cubemaps and signed distance fields (SDF) files
- Converts DDNA (split normal and gloss textures) into a pair of files

## Installation

### Option 1: Download Release
1. Go to the [Releases](../../releases) page
2. Download the latest release's executable
3. Place it wherever you want to use it (no installation required).
4. Add the executable to your system PATH for easy access.

### Option 2: Build from Source
1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution
4. Find the executable in the bin directory

## Usage

Basic usage:
```bash
DDS-Unsplitter.exe <filename> [options]
```

### Parameters
- `filename`: The base name of the split DDS files to combine. Can be:
  - Full path: `C:\textures\file.dds`
  - Relative path: `.\file.dds`
  - Just filename: `file.dds` (will use current directory)
  - Extension is optional

### Options
- `-s, --safe`: Prevents overwriting original files by adding '.combined' before the extension

### Examples
```bash
# Basic usage
DDS-Unsplitter.exe texture.dds

# Use safe mode
DDS-Unsplitter.exe texture.dds --safe

# Without extension
DDS-Unsplitter.exe texture -s

# With full path
DDS-Unsplitter.exe C:\game\textures\texture.dds
```

### Note
- Split files should be in the same directory
- Files should be numbered sequentially (.0, .1, .2, etc.)
- By default, it will overwrite the .dds file with the combined version
- If a file has already been combined, it will be skipped

## File Format Support

The utility handles several DDS format variants:
- Standard DDS files
- DXT10 format files
- ATI2/BC5 normal maps

## Contributing

Interested in contributing? Please read our [Contributing Guidelines](CONTRIBUTING.md).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Thanks to the DDS file format documentation from Microsoft
- Thanks to the CryEngine community for format information