using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Base class for configuration objects with common validation and cloning functionality
    /// </summary>
    public abstract class BaseConfiguration
    {
        /// <summary>
        /// Validates the configuration using data annotations
        /// </summary>
        public virtual List<string> Validate()
        {
            var errors = new List<string>();
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(this, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            }

            // Allow derived classes to add custom validation
            var customErrors = ValidateCustomRules();
            errors.AddRange(customErrors);

            return errors;
        }

        /// <summary>
        /// Override this method to provide custom validation rules
        /// </summary>
        protected virtual List<string> ValidateCustomRules()
        {
            return new List<string>();
        }

        /// <summary>
        /// Creates a deep copy of this configuration
        /// Must be implemented by derived classes
        /// </summary>
        public abstract BaseConfiguration Clone();
    }
}