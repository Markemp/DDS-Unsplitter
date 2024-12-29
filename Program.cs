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
            string combinedFile = DDSFileCombiner.Combine(args[0]);
            Console.WriteLine($"Combined file created: {combinedFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static void DisplayUsage()
    {
        Console.WriteLine("DDSUnsplitter - A utility for combining split DDS texture files");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  DDSUnsplitter.exe <filename>");
        Console.WriteLine("\nParameters:");
        Console.WriteLine("  filename    The base name of the split DDS files to combine");
        Console.WriteLine("\nExample:");
        Console.WriteLine("  DDSUnsplitter.exe texture.dds");
        Console.WriteLine("\nNote: Split files should be in the same directory and numbered sequentially (.0, .1, .2, etc.)");
    }
}