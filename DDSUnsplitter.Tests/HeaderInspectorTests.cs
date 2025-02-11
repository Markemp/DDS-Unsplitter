//using DDSUnsplitter.Library;
//using DDSUnsplitter.Library.Models;
//using NUnit.Framework;

//namespace DDSUnsplitter.Tests;

//[TestFixture]
//public class HeaderInspectorTests
//{
//    HashSet<HeaderInfo> headerList = [];

//    [Test]
//    public void GetAllDSSHeadersForSC()
//    {
//        var basedir = @"d:\depot\sc3.24\data";

//        var allDdsFiles = Directory.GetFiles(basedir, "*.dds", SearchOption.AllDirectories)
//            .OrderBy(f => f)
//            .ToList();

//        Console.WriteLine($"Found {allDdsFiles.Count} DDS files");

//        foreach (var file in allDdsFiles)
//        {
//            HeaderInfo ddsHeader = DdsHeader.Deserialize(file);
//            try
//            {
//                var mipSize = DDSFileCombiner.CalculateMipSize(ddsHeader.Header.Width, ddsHeader.Header.Height, ddsHeader);
//            }
//            catch (Exception ex)
//            {
//                // Found a not supported format
//                headerList.Add(ddsHeader);
//            }

//        }
//    }

//    [Test]
//    public void GetAllDSSHeadersForAA()
//    {
//        var basedir = @"D:\depot\ArmoredWarfare";

//        var allDdsFiles = Directory.GetFiles(basedir, "*.dds", SearchOption.AllDirectories)
//            .OrderBy(f => f)
//            .ToList();

//        Console.WriteLine($"Found {allDdsFiles.Count} DDS files");

//        foreach (var file in allDdsFiles)
//        {
//            HeaderInfo ddsHeader = DdsHeader.Deserialize(file);
//            try
//            {
//                var mipSize = DDSFileCombiner.CalculateMipSize(ddsHeader.Header.Width, ddsHeader.Header.Height, ddsHeader);
//            }
//            catch (Exception ex)
//            {
//                // Found a not supported format
//                headerList.Add(ddsHeader);
//            }

//        }
//    }
//}
