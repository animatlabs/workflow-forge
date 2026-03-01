using System;
using System.Runtime.Serialization;
using WorkflowForge.Exceptions;

namespace WorkflowForge.Tests.Exceptions
{
    /// <summary>
    /// Tests for <see cref="FoundryDataException"/>.
    /// </summary>
    public class FoundryDataExceptionShould
    {
        #region Constructor Tests

        [Fact]
        public void SetMessageAndNullDataKey_GivenMessage()
        {
            // Arrange
            const string message = "Data operation failed";

            // Act
            var exception = new FoundryDataException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.DataKey);
        }

        [Fact]
        public void SetBothProperties_GivenMessageAndDataKey()
        {
            // Arrange
            const string message = "Data operation failed";
            const string dataKey = "user:123";

            // Act
            var exception = new FoundryDataException(message, dataKey);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(dataKey, exception.DataKey);
        }

        [Fact]
        public void SetEmptyString_GivenMessageAndEmptyDataKey()
        {
            // Arrange
            const string message = "Data operation failed";

            // Act
            var exception = new FoundryDataException(message, string.Empty);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(string.Empty, exception.DataKey);
        }

        [Fact]
        public void SetInnerExceptionAndNullDataKey_GivenMessageAndInnerException()
        {
            // Arrange
            const string message = "Data operation failed";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new FoundryDataException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Null(exception.DataKey);
        }

        [Fact]
        public void SetAllProperties_GivenMessageInnerExceptionAndDataKey()
        {
            // Arrange
            const string message = "Data operation failed";
            var innerException = new InvalidOperationException("Inner error");
            const string dataKey = "config:retry";

            // Act
            var exception = new FoundryDataException(message, innerException, dataKey);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(dataKey, exception.DataKey);
        }

        #endregion Constructor Tests

        #region Serialization Tests

#pragma warning disable SYSLIB0050, SYSLIB0051 // Serialization tests for .NET Framework compatibility
        [Fact]
        public void SerializeDataKey_GivenGetObjectData()
        {
            // Arrange
            const string message = "Data operation failed";
            const string dataKey = "serialized-key";
            var exception = new FoundryDataException(message, dataKey);
            var info = new SerializationInfo(typeof(FoundryDataException), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            // Act - base.GetObjectData populates info with Exception data
            exception.GetObjectData(info, context);

            // Assert
            var serializedDataKey = info.GetString(nameof(FoundryDataException.DataKey));
            Assert.Equal(dataKey, serializedDataKey);
        }

        [Fact]
        public void SerializeNull_GivenNullDataKey()
        {
            // Arrange
            const string message = "Data operation failed";
            var exception = new FoundryDataException(message);
            var info = new SerializationInfo(typeof(FoundryDataException), new FormatterConverter());
            var context = new StreamingContext(StreamingContextStates.All);

            // Act
            exception.GetObjectData(info, context);

            // Assert
            var serializedDataKey = info.GetString(nameof(FoundryDataException.DataKey));
            Assert.Null(serializedDataKey);
        }
#pragma warning restore SYSLIB0050, SYSLIB0051

        #endregion Serialization Tests
    }
}
