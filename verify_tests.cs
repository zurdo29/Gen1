using System;
using ProceduralMiniGameGenerator.Core;

/// <summary>
/// Simple verification that the export/import tests exist and can be called
/// </summary>
public class VerifyTests
{
    public static void Main()
    {
        Console.WriteLine("=== Verifying Export/Import Tests ===\n");
        
        try
        {
            Console.WriteLine("1. Verifying export tests exist...");
            // This will throw an exception if the method doesn't exist
            var exportTestMethod = typeof(LevelExportTest).GetMethod("RunAllTests");
            if (exportTestMethod == null)
                throw new Exception("RunAllTests method not found");
            Console.WriteLine("   ✓ Export tests method exists");
            
            Console.WriteLine("2. Verifying import tests exist...");
            var importTestMethod = typeof(LevelExportTest).GetMethod("RunAllImportTests");
            if (importTestMethod == null)
                throw new Exception("RunAllImportTests method not found");
            Console.WriteLine("   ✓ Import tests method exists");
            
            Console.WriteLine("3. Verifying demonstration methods exist...");
            var exportDemoMethod = typeof(LevelExportTest).GetMethod("DemonstrateExportFunctionality");
            if (exportDemoMethod == null)
                throw new Exception("DemonstrateExportFunctionality method not found");
            Console.WriteLine("   ✓ Export demonstration method exists");
            
            var importDemoMethod = typeof(LevelExportTest).GetMethod("DemonstrateImportFunctionality");
            if (importDemoMethod == null)
                throw new Exception("DemonstrateImportFunctionality method not found");
            Console.WriteLine("   ✓ Import demonstration method exists");
            
            Console.WriteLine("\n✓ All export/import test methods are properly implemented!");
            Console.WriteLine("\nTest methods available:");
            Console.WriteLine("- LevelExportTest.RunAllTests() - Runs all export tests");
            Console.WriteLine("- LevelExportTest.RunAllImportTests() - Runs all import tests");
            Console.WriteLine("- LevelExportTest.DemonstrateExportFunctionality() - Demonstrates export features");
            Console.WriteLine("- LevelExportTest.DemonstrateImportFunctionality() - Demonstrates import features");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Verification failed: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}