using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class ValidationMiddlewareTests : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly TestOperation _operation;
        private readonly ISystemTimeProvider _timeProvider;

        public ValidationMiddlewareTests()
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
        public async Task ExecuteAsync_WithValidData_ShouldCallNext()
        {
            var validator = new TestDataValidator();
            var adapter = new FluentValidationAdapter<TestData>(validator);
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = 10 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next() { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Success", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidData_ThrowOnFailureTrue_ShouldThrow()
        {
            var validator = new TestDataValidator();
            var adapter = new FluentValidationAdapter<TestData>(validator);
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = -1 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            Task<object?> Next() => Task.FromResult<object?>(null);

            await Assert.ThrowsAsync<WorkflowValidationException>(() =>
                middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None));

            Assert.Equal("Failed", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidData_ThrowOnFailureFalse_ShouldCallNext()
        {
            var validator = new TestDataValidator();
            var adapter = new FluentValidationAdapter<TestData>(validator);
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => new TestData { Value = -1 });
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = false };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next() { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
            Assert.Equal("Failed", _foundry.Properties["Validation.Status"]);
        }

        [Fact]
        public async Task ExecuteAsync_WithNullData_ShouldLogWarningAndCallNext()
        {
            var validator = new TestDataValidator();
            var adapter = new FluentValidationAdapter<TestData>(validator);
            var dataExtractor = new Func<IWorkflowFoundry, object?>(f => null);
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var middleware = new ValidationMiddleware(
                _foundry.Logger,
                new TestWorkflowValidator(adapter),
                dataExtractor,
                options);

            var nextCalled = false;
            Task<object?> Next() { nextCalled = true; return Task.FromResult<object?>(null); }

            await middleware.ExecuteAsync(_operation, _foundry, null, Next, CancellationToken.None);

            Assert.True(nextCalled);
        }

        private class TestData
        {
            public int Value { get; set; }
        }

        private class TestDataValidator : AbstractValidator<TestData>
        {
            public TestDataValidator()
            {
                RuleFor(x => x.Value).GreaterThan(0).WithMessage("Value must be greater than 0");
            }
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
            public bool SupportsRestore => false;

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