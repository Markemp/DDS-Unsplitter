using DDSUnsplitter.Library;

namespace DDSUnsplitter;

class Program
{
    static void Main(string[] args)
    {
        // Check if filename is passed as argument
        if (args.Length == 0)
        {
            DisplayUsage();
            return;
        }

        try
        {
            var useSafeName = false;
            if (args.Length > 1 && args[1] == "-s" || args[0] == "--safe")
                useSafeName = true;
            string combinedFile = DDSFileCombiner.Combine(args[0], useSafeName);
            Console.WriteLine($"Combined file created: {combinedFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void DisplayUsage()
    {
        Console.WriteLine("DDS-Unsplitter - A utility for combining split DDS texture files");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  DDS-Unsplitter.exe <filename>");
        Console.WriteLine("\nParameters:");
        Console.WriteLine("  filename    The base name of the split DDS files to combine. Can be without the extension.");
        Console.WriteLine("              For example: `defaultnouvs` instead of `defaultnouvs.dds.");
        Console.WriteLine("  --safe,-s <safeword>  Use this parameter to prevent overwriting the original .dds file.  ");
        Console.WriteLine("              The word '.combined' will be added before the file extension unless a safeword is specified.");
        Console.WriteLine("\nExample:");
        Console.WriteLine("  DDS-Unsplitter.exe texture.dds");
        Console.WriteLine("\nNote: Split files should be in the same directory and numbered sequentially (.0, .1, .2, etc.).");
        Console.WriteLine("        By default it will overwrite the .dds file with the combined one.  If the file has already been combined it'll skip processing it.");
    }
}