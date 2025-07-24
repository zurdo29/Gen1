using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Editor
{
    /// <summary>
    /// Interface for integrating with the game editor
    /// </summary>
    public interface IEditorIntegration
    {
        /// <summary>
        /// Registers editor commands for generation
        /// </summary>
        void RegisterEditorCommands();
        
        /// <summary>
        /// Shows the generation window in the editor
        /// </summary>
        void ShowGenerationWindow();
        
        /// <summary>
        /// Displays a generated level in the editor
        /// </summary>
        /// <param name="level">Level to display</param>
        void DisplayGeneratedLevel(Level level);
        
        /// <summary>
        /// Reports errors to the editor interface
        /// </summary>
        /// <param name="errors">List of error messages</param>
        void ReportErrors(List<string> errors);
        
        /// <summary>
        /// Reports warnings to the editor interface
        /// </summary>
        /// <param name="warnings">List of warning messages</param>
        void ReportWarnings(List<string> warnings);
        
        /// <summary>
        /// Reports validation results with both errors and warnings
        /// </summary>
        /// <param name="validationResult">Validation result to report</param>
        void ReportValidationResult(ValidationResult validationResult);
        
        /// <summary>
        /// Displays success messages with visual feedback
        /// </summary>
        /// <param name="message">Success message to display</param>
        void DisplaySuccessMessage(string message);
        
        /// <summary>
        /// Displays information messages
        /// </summary>
        /// <param name="message">Information message to display</param>
        void DisplayInfoMessage(string message);
        
        /// <summary>
        /// Shows a preview of the generated level
        /// </summary>
        /// <param name="level">Level to preview</param>
        void ShowLevelPreview(Level level);
        
        /// <summary>
        /// Allows user to select a configuration file
        /// </summary>
        /// <returns>Path to selected configuration file</returns>
        string SelectConfigurationFile();
    }
}