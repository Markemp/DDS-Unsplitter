using DDSUnsplitter.Library;
using DDSUnsplitter.Library.Models;
using NUnit.Framework;
using static DDSUnsplitter.Library.Models.DdsConstants;

namespace DDSUnsplitter.Tests;

[TestFixture]
public class DDSFileCombinerTests
{
    private const string TEST_FILES_DIR = "TestFiles";
    private string _tempDir;
    private Dictionary<string, string> _copiedFiles = [];

    [OneTimeSetUp]
    public void SetUp()
    {
        // Create a temporary directory for test outputs
        _tempDir = Path.Combine(Path.GetTempPath(), "DDSUnsplitterTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        // Clean up temporary files after tests
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Test]
    public void Combine_SCStyle_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds");
        
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, true);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_SCStyleWithShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "defaultnouvs");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "defaultnouvs.dds*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file should have been rewritten with safe extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "defaultnouvs.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 296), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_BaseFileHasDDS0Extension_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "flat_normal_ddn.dds.0");
        
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "flat_normal_ddn*");
        Assert.That(allTestFiles.Contains(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName), "Header file should have been rewritten with safe extension");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_BaseFileHasDDS0ExtensionAndShortName_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "flat_normal_ddn");

        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);

        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(21976), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "flat_normal_ddn*");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".dds.0"), "Header file still has 0 extension");
        var headerFile = new FileInfo(Path.Combine(TEST_FILES_DIR, "flat_normal_ddn.dds.0"));
        Assert.That(headerFile.Length, Is.EqualTo(expected: 464), "Header file size is incorrect");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void Combine_CubeMap_CreatesCombinedFile()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "environmentprobeafternoon_cm.dds");
        string combinedFileName = DDSFileCombiner.Combine(baseFileName, false);
        FileInfo fileInfo = new(combinedFileName);
        Assert.That(File.Exists(combinedFileName), "Combined file was not created");
        Assert.That(fileInfo.Length, Is.EqualTo(174896), "Combined file size is incorrect");
        var allTestFiles = Directory.GetFiles(TEST_FILES_DIR, "environmentprobeafternoon_cm*");
        Assert.That(allTestFiles, Does.Contain(baseFileName), "Base file should be present");
        Assert.That(allTestFiles, Does.Contain(baseFileName + ".0"), "Header file should have been rewritten with safe extension");
        VerifyEndMarker(combinedFileName);
    }

    [Test]
    public void FindMatchingFiles_GlossFilesReturnsAllFiles()
    {
        string baseFileName = "gloss10_ddna";
        var actualFiles = DDSFileCombiner.FindMatchingFiles(TEST_FILES_DIR, baseFileName);
        Assert.That(actualFiles.Count, Is.EqualTo(8));
        Assert.That(actualFiles.Last, Is.EqualTo("TestFiles\\gloss10_ddna.dds.a"));
        Assert.That(actualFiles.First, Is.EqualTo("TestFiles\\gloss10_ddna.dds"));
    }

    [Test]
    public void FindMatchingFiles_SC_DDNAWithGloss_CombinesAllFiles()
    {
        string baseFileName = Path.Combine(TEST_FILES_DIR, "gloss10_ddna.dds");
        string directory = Path.GetDirectoryName(baseFileName)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);

        var matchingFiles = DDSFileCombiner.FindMatchingFiles(directory, fileNameWithoutExtension);

        Assert.That(matchingFiles, Has.Count.AtLeast(8), "Should find 4 files (base + 3 mipmaps)");

        Assert.That(matchingFiles, Contains.Item(baseFileName + ".a"), "At least one file should have .a extension");
        Assert.That(matchingFiles, Contains.Item(baseFileName), "Should include base file");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".1"), "Should include .1 mipmap");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".2"), "Should include .2 mipmap");
        Assert.That(matchingFiles, Contains.Item(baseFileName + ".3"), "Should include .3 mipmap");
    }

    private void VerifyEndMarker(string combinedFileName)
    {
        // Verify end marker
        var stream = File.OpenRead(combinedFileName);
        stream.Seek(-CRYENGINE_END_MARKER_SIZE, SeekOrigin.End);
        var endMarker = new byte[CRYENGINE_END_MARKER_SIZE];
        stream.Read(endMarker, 0, CRYENGINE_END_MARKER_SIZE);
        Assert.That(endMarker, Is.EqualTo(CRYENGINE_END_MARKER),
            "CryEngine end marker not found or incorrect");
    }
}
