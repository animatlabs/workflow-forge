using System;
using System.Collections.Generic;
using WorkflowForge.Abstractions;

namespace WorkflowForge.Tests.Orchestration
{
    public class WorkflowShould
    {
        #region Constructor Tests

        [Fact]
        public void CreateWorkflow_GivenValidParameters()
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
        public void ThrowArgumentNullException_GivenNullName()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Workflow(null!, "description", "1.0.0", operations, properties));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullVersion()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Workflow("TestWorkflow", "description", null!, operations, properties));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullOperations()
        {
            // Arrange
            var properties = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Workflow("TestWorkflow", "description", "1.0.0", null!, properties));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullProperties()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Workflow("TestWorkflow", "description", "1.0.0", operations, null!));
        }

        [Fact]
        public void AllowNullValue_GivenNullDescription()
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
        public void CreateWorkflowWithEmptyOperationsList_GivenEmptyOperations()
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
        public void CreateWorkflowWithEmptyPropertiesDictionary_GivenEmptyProperties()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var properties = new Dictionary<string, object?>();

            // Act
            var workflow = new Workflow("TestWorkflow", "description", "1.0.0", operations, properties);

            // Assert
            Assert.Empty(workflow.Properties);
        }

        #endregion Constructor Tests

        #region Property Tests

        [Fact]
        public void ReturnReadOnlyList_GivenOperationsAccessed()
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
        public void ReturnReadOnlyDictionary_GivenPropertiesAccessed()
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

        #endregion Property Tests

        #region Dispose Tests

        [Fact]
        public void DisposeAllOperations_GivenDisposeCalled()
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
        public void HandleGracefully_GivenMultipleDisposeCalls()
        {
            // Arrange
            var operations = new List<IWorkflowOperation> { CreateMockOperation("Op1").Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act
            workflow.Dispose();
            workflow.Dispose();

            // Assert
            Mock.Get(operations[0]).Verify(op => op.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public void PropagateException_GivenOperationThrowsOnDispose()
        {
            // Arrange
            var throwingOp = CreateMockOperation("ThrowingOp");
            throwingOp.Setup(op => op.Dispose()).Throws<InvalidOperationException>();
            var normalOp = CreateMockOperation("NormalOp");

            var operations = new List<IWorkflowOperation> { throwingOp.Object, normalOp.Object };
            var workflow = CreateWorkflow(operations: operations);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => workflow.Dispose());

            throwingOp.Verify(op => op.Dispose(), Times.Once);
        }

        [Fact]
        public void CompleteSuccessfully_GivenNoOperations()
        {
            // Arrange
            var workflow = CreateWorkflow(operations: new List<IWorkflowOperation>());

            // Act & Assert
            var ex = Record.Exception(() => workflow.Dispose());
            Assert.Null(ex);
        }

        #endregion Dispose Tests

        #region Immutability Tests

        [Fact]
        public void NotAffectWorkflow_GivenOriginalOperationsListModified()
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
        public void NotAffectWorkflow_GivenOriginalPropertiesDictionaryModified()
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

        #endregion Immutability Tests

        #region Edge Cases

        [Fact]
        public void AllowEmptyStrings_GivenEmptyStringValues()
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
        public void HandleLongStrings_GivenVeryLongStringValues()
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
        public void HandleSpecialCharacters_GivenSpecialCharacterValues()
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

        #endregion Edge Cases

        #region Helper Methods

        private Mock<IWorkflowOperation> CreateMockOperation(string name)
        {
            var mock = new Mock<IWorkflowOperation>();
            mock.Setup(op => op.Id).Returns(Guid.NewGuid());
            mock.Setup(op => op.Name).Returns(name);
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

        #endregion Helper Methods
    }
}
