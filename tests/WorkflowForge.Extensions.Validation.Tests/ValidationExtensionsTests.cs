using FluentValidation;
using System;
using System.Reflection;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Validation;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class ValidationExtensionsTests : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly ISystemTimeProvider _timeProvider;

        public ValidationExtensionsTests()
        {
            _timeProvider = SystemTimeProvider.Instance;
            _foundry = WF.WorkflowForge.CreateFoundry("ValidationExtTest");
        }

        public void Dispose()
        {
            (_foundry as IDisposable)?.Dispose();
        }

        [Fact]
        public void UseValidation_WithFluentValidator_ShouldAddMiddleware()
        {
            var validator = new TestValidator();
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var result = _foundry.UseValidation(
                validator,
                f => new TestModel { Value = 10 },
                options);

            Assert.Same(_foundry, result);
        }

        [Fact]
        public void UseValidation_WithNullFoundry_ShouldThrowArgumentNullException()
        {
            var validator = new TestValidator();
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.UseValidation(validator, f => new TestModel(), null));
        }

        [Fact]
        public void UseValidation_WithNullValidator_ShouldThrowArgumentNullException()
        {
            IValidator<TestModel>? nullValidator = null;
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseValidation(nullValidator!, f => new TestModel(), null));
        }

        [Fact]
        public void UseValidation_WithNullDataExtractor_ShouldThrowArgumentNullException()
        {
            var validator = new TestValidator();

            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseValidation(validator, null!, null));
        }

        [Fact]
        public async Task ValidateAsync_WithValidData_ShouldStoreSuccessInProperties()
        {
            var validator = new TestValidator();
            var data = new TestModel { Value = 10 };

            var result = await _foundry.ValidateAsync(validator, data);

            Assert.True(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.IsValid"));
            Assert.Equal(true, _foundry.Properties["ValidationResult.IsValid"]);
        }

        [Fact]
        public async Task ValidateAsync_WithInvalidData_ShouldStoreFailureInProperties()
        {
            var validator = new TestValidator();
            var data = new TestModel { Value = -1 };

            var result = await _foundry.ValidateAsync(validator, data);

            Assert.False(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.IsValid"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.Errors"));
            Assert.Equal(false, _foundry.Properties["ValidationResult.IsValid"]);
        }

        [Fact]
        public async Task ValidateAsync_WithCustomPropertyKey_ShouldUseCustomKey()
        {
            var validator = new TestValidator();
            var data = new TestModel { Value = 10 };

            await _foundry.ValidateAsync(validator, data, "CustomValidation");

            Assert.True(_foundry.Properties.ContainsKey("CustomValidation"));
            Assert.True(_foundry.Properties.ContainsKey("CustomValidation.IsValid"));
        }

        [Fact]
        public void UseValidation_WithEnabledFalse_ShouldNotAddMiddleware()
        {
            var validator = new TestValidator();
            var options = new ValidationMiddlewareOptions { Enabled = false };
            
            // Create a new foundry to test with
            using var testFoundry = WF.WorkflowForge.CreateFoundry("Test");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(validator, f => new TestModel { Value = 10 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount, GetMiddlewareCount(testFoundry));
        }

        [Fact]
        public void UseValidation_WithEnabledTrue_ShouldAddMiddleware()
        {
            var validator = new TestValidator();
            var options = new ValidationMiddlewareOptions { Enabled = true };
            
            // Create a new foundry to test with
            using var testFoundry = WF.WorkflowForge.CreateFoundry("Test");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(validator, f => new TestModel { Value = 10 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount + 1, GetMiddlewareCount(testFoundry));
        }

        private static int GetMiddlewareCount(IWorkflowFoundry foundry)
        {
            // Use reflection to access MiddlewareCount since it's not on the interface
            var type = foundry.GetType();
            var property = type.GetProperty("MiddlewareCount");
            return property != null ? (int)property.GetValue(foundry)! : 0;
        }

        private class TestModel
        {
            public int Value { get; set; }
        }

        private class TestValidator : AbstractValidator<TestModel>
        {
            public TestValidator()
            {
                RuleFor(x => x.Value).GreaterThan(0).WithMessage("Value must be greater than 0");
            }
        }
    }
}