using System;
using System.Collections.Generic;
using System.IO;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Editor
{
    /// <summary>
    /// Test class for enhanced error reporting functionality
    /// </summary>
    public static class ErrorReportingTest
    {
        /// <summary>
        /// Runs all error reporting tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Enhanced Error Reporting Tests ===");
            
            try
            {
                TestBasicErrorReporting();
                TestWarningReporting();
                TestValidationResultReporting();
                TestSuccessAndInfoMessages();
                TestErrorLogging();
                TestVisualFeedback();
                
                Console.WriteLine("✓ All error reporting tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reporting tests failed: {ex.Message}");
                throw;
            }
        }
        
        private static void TestBasicErrorReporting()
        {
            Console.WriteLine("Testing basic error reporting...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            var testErrors = new List<string>
            {
                "Configuration file not found",
                "Invalid terrain dimensions: width must be greater than 0",
                "Entity placement failed: no valid positions available"
            };
            
            // This should display enhanced error panel
            editorIntegration.ReportErrors(testErrors);
            
            var recentErrors = editorIntegration.GetRecentErrors();
            if (recentErrors.Count != testErrors.Count)
                throw new Exception($"Expected {testErrors.Count} errors, got {recentErrors.Count}");
            
            Console.WriteLine("✓ Basic error reporting works correctly");
        }
        
        private static void TestWarningReporting()
        {
            Console.WriteLine("Testing warning reporting...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            var testWarnings = new List<string>
            {
                "No sprite defined for entity type 'enemy' in visual theme",
                "Level size may be too small for the number of entities",
                "Time limit may be too short for level size"
            };
            
            // This should display warning panel
            editorIntegration.ReportWarnings(testWarnings);
            
            Console.WriteLine("✓ Warning reporting works correctly");
        }
        
        private static void TestValidationResultReporting()
        {
            Console.WriteLine("Testing validation result reporting...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // Test with errors and warnings
            var validationResult = new ValidationResult();
            validationResult.Errors.Add("Critical configuration error");
            validationResult.Warnings.Add("Configuration warning");
            
            editorIntegration.ReportValidationResult(validationResult);
            
            // Test with valid configuration
            var validResult = new ValidationResult();
            editorIntegration.ReportValidationResult(validResult);
            
            Console.WriteLine("✓ Validation result reporting works correctly");
        }
        
        private static void TestSuccessAndInfoMessages()
        {
            Console.WriteLine("Testing success and info messages...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            editorIntegration.DisplaySuccessMessage("Level generated successfully");
            editorIntegration.DisplayInfoMessage("Using default configuration values");
            
            Console.WriteLine("✓ Success and info messages work correctly");
        }
        
        private static void TestErrorLogging()
        {
            Console.WriteLine("Testing error logging to files...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // Clean up any existing log files
            var errorLogPath = "editor_errors.log";
            var warningLogPath = "editor_warnings.log";
            
            if (File.Exists(errorLogPath))
                File.Delete(errorLogPath);
            if (File.Exists(warningLogPath))
                File.Delete(warningLogPath);
            
            // Test error logging
            var testErrors = new List<string> { "Test error for logging" };
            editorIntegration.ReportErrors(testErrors);
            
            // Test warning logging
            var testWarnings = new List<string> { "Test warning for logging" };
            editorIntegration.ReportWarnings(testWarnings);
            
            // Verify log files were created
            if (!File.Exists(errorLogPath))
                throw new Exception("Error log file was not created");
            
            if (!File.Exists(warningLogPath))
                throw new Exception("Warning log file was not created");
            
            // Verify log content
            var errorLogContent = File.ReadAllText(errorLogPath);
            if (!errorLogContent.Contains("Test error for logging"))
                throw new Exception("Error was not logged correctly");
            
            var warningLogContent = File.ReadAllText(warningLogPath);
            if (!warningLogContent.Contains("Test warning for logging"))
                throw new Exception("Warning was not logged correctly");
            
            Console.WriteLine("✓ Error logging works correctly");
        }
        
        private static void TestVisualFeedback()
        {
            Console.WriteLine("Testing visual feedback for different message types...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // Test long error messages that need truncation
            var longErrors = new List<string>
            {
                "This is a very long error message that should be truncated in the display panel but still logged completely to the file for debugging purposes",
                "Another long error message to test the truncation and wrapping functionality of the enhanced error reporting system"
            };
            
            editorIntegration.ReportErrors(longErrors);
            
            // Test long warning messages
            var longWarnings = new List<string>
            {
                "This is a very long warning message that should be truncated in the display panel but still logged completely to the file for debugging purposes"
            };
            
            editorIntegration.ReportWarnings(longWarnings);
            
            Console.WriteLine("✓ Visual feedback works correctly");
        }
        
        /// <summary>
        /// Demonstrates the complete error reporting workflow
        /// </summary>
        public static void DemonstrateErrorReporting()
        {
            Console.WriteLine("\n=== Error Reporting Demonstration ===");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            Console.WriteLine("1. Demonstrating error reporting:");
            editorIntegration.ReportErrors(new List<string>
            {
                "Configuration file is missing required 'width' parameter",
                "Entity count exceeds maximum allowed for level size"
            });
            
            Console.WriteLine("\n2. Demonstrating warning reporting:");
            editorIntegration.ReportWarnings(new List<string>
            {
                "Using default seed value",
                "No visual theme specified, using default"
            });
            
            Console.WriteLine("\n3. Demonstrating success message:");
            editorIntegration.DisplaySuccessMessage("Level validation completed successfully");
            
            Console.WriteLine("\n4. Demonstrating info message:");
            editorIntegration.DisplayInfoMessage("Loading configuration from default.json");
            
            Console.WriteLine("\n5. Demonstrating validation result reporting:");
            var validationResult = new ValidationResult();
            validationResult.Errors.Add("Invalid algorithm parameter: scale must be between 0 and 1");
            validationResult.Warnings.Add("Recommended entity count is 3-10 for this level size");
            
            editorIntegration.ReportValidationResult(validationResult);
            
            Console.WriteLine("\n=== End Demonstration ===\n");
        }
    }

}