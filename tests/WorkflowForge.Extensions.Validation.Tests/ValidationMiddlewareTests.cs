using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class ValidationMiddlewareShould : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly TestOperation _operation;
        private readonly ISystemTimeProvider _timeProvider;

        public ValidationMiddlewareShould()
        {
            _timeProvider = SystemTimeProvider.Instance;
            _foundry = WF.WorkflowForge.CreateFoundry("ValidationTest");
            _operation = new TestOperation();
        }

        public void Dispose()
        {
            (_foundry as IDisposable)?.Dispose();
        }

        [Fact]
        public async Task CallNext_GivenValidData()
        {
            var adapter = new DataAnnotationsWorkflowValidator<TestData>();
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = 10 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next(CancellationToken _)
            { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Success", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task Throw_GivenInvalidDataAndThrowOnFailureTrue()
        {
            var adapter = new DataAnnotationsWorkflowValidator<TestData>();
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = -1 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            Task<object?> Next(CancellationToken _) => Task.FromResult<object?>(null);

            await Assert.ThrowsAsync<WorkflowValidationException>(() =>
                middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

            Assert.Equal("Failed", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task CallNext_GivenInvalidDataAndThrowOnFailureFalse()
        {
            var adapter = new DataAnnotationsWorkflowValidator<TestData>();
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = -1 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = false };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next(CancellationToken _)
            { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Failed", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task LogWarningAndCallNext_GivenNullData()
        {
            var adapter = new DataAnnotationsWorkflowValidator<TestData>();
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => null);
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next(CancellationToken _)
            { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
        }

        private class TestData
        {
            [Range(1, int.MaxValue, ErrorMessage = "Value must be greater than 0")]
            public int Value { get; set; }
        }

        private class TestWorkflowValidator : IWorkflowValidator<object>
        {
            private readonly IWorkflowValidator<TestData> _inner;

            public TestWorkflowValidator(IWorkflowValidator<TestData> inner)
            {
                _inner = inner;
            }

            public async Task<ValidationResult> ValidateAsync(object data, CancellationToken cancellationToken = default)
            {
                if (data is TestData testData)
                    return await _inner.ValidateAsync(testData, cancellationToken);
                return ValidationResult.Failure("Invalid data type");
            }
        }

        private class TestOperation : IWorkflowOperation
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "TestOperation";

            public Task<object?> ForgeAsync(object? inputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<object?>(null);
            }

            public Task RestoreAsync(object? outputData, IWorkflowFoundry foundry, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            { }
        }
    }
}
