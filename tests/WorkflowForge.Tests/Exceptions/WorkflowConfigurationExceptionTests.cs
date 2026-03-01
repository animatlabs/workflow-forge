using System;
using System.Runtime.Serialization;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.Exceptions
{
    /// <summary>
    /// Tests for <see cref="WorkflowConfigurationException"/>.
    /// </summary>
    public class WorkflowConfigurationExceptionShould
    {
        #region Constructor Tests

        [Fact]
        public void SetMessageAndNullConfigurationKey_GivenMessage()
        {
            // Arrange
            const string message = "Invalid workflow configuration";

            // Act
            var exception = new WorkflowConfigurationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.ConfigurationKey);
        }

        [Fact]
        public void SetBothProperties_GivenMessageAndConfigurationKey()
        {
            // Arrange
            const string message = "Invalid workflow configuration";
            const string configurationKey = "workflow:timeout";

            // Act
            var exception = new WorkflowConfigurationException(message, configurationKey);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(configurationKey, exception.ConfigurationKey);
        }

        [Fact]
        public void SetEmptyString_GivenMessageAndEmptyConfigurationKey()
        {
            // Arrange
            const string message = "Invalid workflow configuration";

            // Act
            var exception = new WorkflowConfigurationException(message, string.Empty);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(string.Empty, exception.ConfigurationKey);
        }

        [Fact]
        public void SetInnerExceptionAndNullConfigurationKey_GivenMessageAndInnerException()
        {
            // Arrange
            const string message = "Invalid workflow configuration";
            var innerException = new ArgumentException("Invalid config value");

            // Act
            var exception = new WorkflowConfigurationException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Null(exception.ConfigurationKey);
        }

        [Fact]
        public void SetAllProperties_GivenMessageInnerExceptionAndConfigurationKey()
        {
            // Arrange
            const string message = "Invalid workflow configuration";
            var innerException = new ArgumentException("Invalid config value");
            const string configurationKey = "workflow:retryPolicy";

            // Act
            var exception = new WorkflowConfigurationException(message, innerException, configurationKey);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(configurationKey, exception.ConfigurationKey);
        }

        #endregion Constructor Tests

        #region Serialization Tests

#pragma warning disable SYSLIB0050, SYSLIB0051 // Serialization tests for .NET Framework compatibility
        [Fact]
        public void SerializeConfigurationKey_GivenGetObjectData()
        {
            // Arrange
            const string message = "Invalid workflow configuration";
            const string configurationKey = "serialized-config-key";
            var exception = new WorkflowConfigurationException(message, configurationKey);
            var info = new SerializationInfo(typeof(WorkflowConfigurationException), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            // Act
            exception.GetObjectData(info, context);

            // Assert
            var serializedConfigKey = info.GetString(nameof(WorkflowConfigurationException.ConfigurationKey));
            Assert.Equal(configurationKey, serializedConfigKey);
        }

        [Fact]
        public void SerializeNull_GivenNullConfigurationKey()
        {
            // Arrange
            const string message = "Invalid workflow configuration";
            var exception = new WorkflowConfigurationException(message);
            var info = new SerializationInfo(typeof(WorkflowConfigurationException), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            // Act
            exception.GetObjectData(info, context);

            // Assert
            var serializedConfigKey = info.GetString(nameof(WorkflowConfigurationException.ConfigurationKey));
            Assert.Null(serializedConfigKey);
        }
#pragma warning restore SYSLIB0050, SYSLIB0051

        #endregion Serialization Tests
    }
}
