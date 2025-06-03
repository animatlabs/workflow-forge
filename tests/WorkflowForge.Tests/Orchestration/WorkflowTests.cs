using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowForge;
using WorkflowForge.Abstractions;
using Xunit;
using Moq;

namespace WorkflowForge.Tests.Orchestration
{
    /// <summary>
    /// Comprehensive tests for the Workflow class covering construction, properties,
    /// and workflow lifecycle management.
    /// </summary>
    public class WorkflowTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesWorkflow()
        {
            // Arrange
            var name = "TestWorkflow";
            var description = "A test workflow";
            var version = "1.0.0";
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var properties = new Dictionary<string, object?> { { "key1", "value1" }, { "key2", 42 } };

            // Act
            var workflow = new Workflow(name, description, version, operations, properties);

            // Assert
            Assert.NotEqual(Guid.Empty, workflow.Id);
            Assert.Equal(name, workflow.Name);
            Assert.Equal(description, workflow.Description);
            Assert.Equal(version, workflow.Version);
            Assert.Equal(2, workflow.Operations.Count);
            Assert.Equal(operations[0], workflow.Operations[0]);
            Assert.Equal(operations[1], workflow.Operations[1]);
            Assert.Equal(2, workflow.Properties.Count);
            Assert.Equal("value1", workflow.Properties["key1"]);
            Assert.Equal(42, workflow.Properties["key2"]);
            Assert.True(workflow.CreatedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Workflow(null!, "description", "1.0.0", operations, properties));
        }

        [Fact]
        public void Constructor_WithNullVersion_ThrowsArgumentNullException()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Workflow("TestWorkflow", "description", null!, operations, properties));
        }

        [Fact]
        public void Constructor_WithNullOperations_ThrowsArgumentNullException()
        {
            // Arrange
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Workflow("TestWorkflow", "description", "1.0.0", null!, properties));
        }

        [Fact]
        public void Constructor_WithNullProperties_ThrowsArgumentNullException()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new Workflow("TestWorkflow", "description", "1.0.0", operations, null!));
        }

        [Fact]
        public void Constructor_WithNullDescription_AllowsNullValue()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow("TestWorkflow", null, "1.0.0", operations, properties);

            // Assert
            Assert.Null(workflow.Description);
        }

        [Fact]
        public void Constructor_WithEmptyOperations_CreatesWorkflowWithEmptyOperationsList()
        {
            // Arrange
            var operations = new List<IWorkflowOperation>();
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow("TestWorkflow", "description", "1.0.0", operations, properties);

            // Assert
            Assert.Empty(workflow.Operations);
        }

        [Fact]
        public void Constructor_WithEmptyProperties_CreatesWorkflowWithEmptyPropertiesDictionary()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow("TestWorkflow", "description", "1.0.0", operations, properties);

            // Assert
            Assert.Empty(workflow.Properties);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void SupportsRestore_WhenAllOperationsSupportsRestore_ReturnsTrue()
        {
            // Arrange
            var operations = new List<IWorkflowOperation>
            { 
                CreateMockOperation("Op1", supportsRestore: true).Object,
                CreateMockOperation("Op2", supportsRestore: true).Object
            };
            var workflow = CreateWorkflow(operations: operations);

            // Act & Assert
            Assert.True(workflow.SupportsRestore);
        }

        [Fact]
        public void SupportsRestore_WhenSomeOperationsDoNotSupportRestore_ReturnsFalse()
        {
            // Arrange
            var operations = new List<IWorkflowOperation>
            { 
                CreateMockOperation("Op1", supportsRestore: true).Object,
                CreateMockOperation("Op2", supportsRestore: false).Object
            };
            var workflow = CreateWorkflow(operations: operations);

            // Act & Assert
            Assert.False(workflow.SupportsRestore);
        }

        [Fact]
        public void SupportsRestore_WhenNoOperations_ReturnsTrue()
        {
            // Arrange
            var workflow = CreateWorkflow(operations: new List<IWorkflowOperation>());

            // Act & Assert
            Assert.True(workflow.SupportsRestore);
        }

        [Fact]
        public void Operations_ReturnsReadOnlyList()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act
            var operationsList = workflow.Operations;

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<IWorkflowOperation>>(operationsList);
            Assert.Equal(2, operationsList.Count);
        }

        [Fact]
        public void Properties_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var properties = new Dictionary<string, object?> { { "key1", "value1" }, { "key2", 42 } };
            var workflow = CreateWorkflow(properties: properties);

            // Act
            var propertiesDict = workflow.Properties;

            // Assert
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(propertiesDict);
            Assert.Equal(2, propertiesDict.Count);
            Assert.Equal("value1", propertiesDict["key1"]);
            Assert.Equal(42, propertiesDict["key2"]);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_DisposesAllOperations()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object, CreateMockOperation("Op2").Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act
            workflow.Dispose();

            // Assert
            Mock.Get(operations[0]).Verify(op => op.Dispose(), Times.Once);
            Mock.Get(operations[1]).Verify(op => op.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_HandledGracefully()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act
            workflow.Dispose();
            workflow.Dispose(); // Should not throw

            // Assert - The workflow doesn't track dispose state, so operation.Dispose() is called each time
            Mock.Get(operations[0]).Verify(op => op.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void Dispose_WithOperationThatThrowsOnDispose_PropagatesException()
        {
            // Arrange
            var throwingOp = CreateMockOperation("ThrowingOp");
            throwingOp.Setup(op => op.Dispose()).Throws<InvalidOperationException>();
            var normalOp = CreateMockOperation("NormalOp");
            
            var operations = new List<IWorkflowOperation> { throwingOp.Object, normalOp.Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act & Assert - The current implementation doesn't catch exceptions
            Assert.Throws<InvalidOperationException>(() => workflow.Dispose());
            
            throwingOp.Verify(op => op.Dispose(), Times.Once);
            // normalOp.Dispose() won't be called because the exception stops execution
        }

        [Fact]
        public void Dispose_WithNoOperations_CompletesSuccessfully()
        {
            // Arrange
            var workflow = CreateWorkflow(operations: new List<IWorkflowOperation>());

            // Act & Assert - Should not throw
            workflow.Dispose();
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Operations_ModifyingOriginalList_DoesNotAffectWorkflow()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var workflow = CreateWorkflow(operations: operations);
            var originalCount = workflow.Operations.Count;

            // Act
            operations.Add(CreateMockOperation("ModifiedOp").Object);

            // Assert
            Assert.Equal(originalCount, workflow.Operations.Count);
            Assert.Equal("Op1", workflow.Operations[0].Name);
        }

        [Fact]
        public void Properties_ModifyingOriginalDictionary_DoesNotAffectWorkflow()
        {
            // Arrange
            var properties = new Dictionary<string, object?> { { "key1", "value1" } };
            var workflow = CreateWorkflow(properties: properties);

            // Act
            properties["key1"] = "modified";
            properties["key2"] = "new";

            // Assert
            Assert.Single(workflow.Properties);
            Assert.Equal("value1", workflow.Properties["key1"]);
            Assert.False(workflow.Properties.ContainsKey("key2"));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Constructor_WithEmptyStringValues_AllowsEmptyStrings()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow("", "", "", operations, properties);

            // Assert
            Assert.Equal("", workflow.Name);
            Assert.Equal("", workflow.Description);
            Assert.Equal("", workflow.Version);
        }

        [Fact]
        public void Constructor_WithVeryLongStrings_HandlesLongStrings()
        {
            // Arrange
            var longString = new string('A', 10000);
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow(longString, longString, longString, operations, properties);

            // Assert
            Assert.Equal(longString, workflow.Name);
            Assert.Equal(longString, workflow.Description);
            Assert.Equal(longString, workflow.Version);
        }

        [Fact]
        public void Constructor_WithSpecialCharacters_HandlesSpecialCharacters()
        {
            // Arrange
            var specialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow(specialChars, specialChars, specialChars, operations, properties);

            // Assert
            Assert.Equal(specialChars, workflow.Name);
            Assert.Equal(specialChars, workflow.Description);
            Assert.Equal(specialChars, workflow.Version);
        }

        #endregion

        #region Helper Methods

        private Mock<IWorkflowOperation> CreateMockOperation(string name, bool supportsRestore = true)
        {
            var mock = new Mock<IWorkflowOperation>();
            mock.Setup(op => op.Id).Returns(Guid.NewGuid());
            mock.Setup(op => op.Name).Returns(name);
            mock.Setup(op => op.SupportsRestore).Returns(supportsRestore);
            return mock;
        }

        private Workflow CreateWorkflow(
            string name = "TestWorkflow", 
            string? description = "Test Description",
            string version = "1.0.0",
            List<IWorkflowOperation>? operations = null,
            Dictionary<string, object?>? properties = null)
        {
            return new Workflow(
                name,
                description,
                version,
                operations ?? new List<IWorkflowOperation> { CreateMockOperation("DefaultOp").Object },
                properties ?? new Dictionary<string, object?>()
            );
        }

        #endregion
    }
} 