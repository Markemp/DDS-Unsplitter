using DDSUnsplitter.Library;

namespace DDSUnsplitter;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            DisplayUsage();
            return;
        }

        try
        {
            string filename = "";
            bool useSafeName = false;

            // Parse arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-s" || args[i] == "--safe")
                    useSafeName = true;
                else if (!args[i].StartsWith("-"))
                    filename = args[i];
            }

            if (string.IsNullOrEmpty(filename))
            {
                Console.WriteLine("Error: No filename specified");
                DisplayUsage();
                return;
            }

            // Ensure we have a proper path by combining with current directory if no path provided
            if (!Path.IsPathRooted(filename) && !filename.StartsWith("."))
                filename = Path.Combine(".", filename);

            string combinedFile = new DDSFileCombiner(new RealFileSystem()).Combine(filename, useSafeName);
            Console.WriteLine($"Combined file: {combinedFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void DisplayUsage()
    {
        Console.WriteLine("DDS-Unsplitter - A utility for combining split DDS texture files");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  DDS-Unsplitter.exe <filename> [options]");
        Console.WriteLine("\nParameters:");
        Console.WriteLine("  filename    The base name of the split DDS files to combine. Can be:");
        Console.WriteLine("              - Full path: C:\\textures\\file.dds");
        Console.WriteLine("              - Relative path: .\\file.dds");
        Console.WriteLine("              - Just filename: file.dds (will use current directory)");
        Console.WriteLine("              Extension is optional.");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -s, --safe  Use this flag to prevent overwriting the original .dds file.");
        Console.WriteLine("              The word '.combined' will be added before the file extension.");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  DDS-Unsplitter.exe .\\texture.dds");
        Console.WriteLine("  DDS-Unsplitter.exe texture.dds --safe");
        Console.WriteLine("  DDS-Unsplitter.exe texture -s");
        Console.WriteLine("\nNote: Split files should be in the same directory and numbered sequentially (.0, .1, .2, etc.).");
        Console.WriteLine("      By default it will overwrite the .dds file with the combined one.");
        Console.WriteLine("      If the file has already been combined it'll skip processing it.");
    }
}