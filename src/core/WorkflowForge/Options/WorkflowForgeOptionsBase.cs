using System;
using System.Collections.Generic;

namespace WorkflowForge.Options
{
    /// <summary>
    /// Base class for all WorkflowForge options classes, providing common functionality
    /// for configuration section management, validation, and cloning.
    /// </summary>
    public abstract class WorkflowForgeOptionsBase : ICloneable
    {
        /// <summary>
        /// Gets the configuration section name for this options class.
        /// </summary>
        public string SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowForgeOptionsBase"/> class.
        /// </summary>
        /// <param name="sectionName">The configuration section name. If null, uses the default section name.</param>
        /// <param name="defaultSectionName">The default configuration section name for this options type.</param>
        protected WorkflowForgeOptionsBase(string? sectionName, string defaultSectionName)
        {
            if (string.IsNullOrWhiteSpace(defaultSectionName))
                throw new ArgumentException("Default section name cannot be null or whitespace.", nameof(defaultSectionName));

            SectionName = sectionName ?? defaultSectionName;
        }

        /// <summary>
        /// Validates the options configuration and returns a list of validation errors.
        /// </summary>
        /// <returns>A list of validation error messages. Empty list indicates valid configuration.</returns>
        public abstract IList<string> Validate();

        /// <summary>
        /// Creates a deep copy of this options instance.
        /// </summary>
        /// <returns>A new instance with the same configuration values.</returns>
        public abstract object Clone();
    }
}

