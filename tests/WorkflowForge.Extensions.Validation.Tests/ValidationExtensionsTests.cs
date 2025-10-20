using FluentValidation;
using System;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
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
        public void AddValidation_WithFluentValidator_ShouldAddMiddleware()
        {
            var validator = new TestValidator();

            var result = _foundry.AddValidation(
                validator,
                f => new TestModel { Value = 10 },
                throwOnFailure: true);

            Assert.Same(_foundry, result);
        }

        [Fact]
        public void AddValidation_WithNullFoundry_ShouldThrowArgumentNullException()
        {
            var validator = new TestValidator();
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.AddValidation(validator, f => new TestModel(), true));
        }

        [Fact]
        public void AddValidation_WithNullValidator_ShouldThrowArgumentNullException()
        {
            IValidator<TestModel>? nullValidator = null;
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.AddValidation(nullValidator!, f => new TestModel(), true));
        }

        [Fact]
        public void AddValidation_WithNullDataExtractor_ShouldThrowArgumentNullException()
        {
            var validator = new TestValidator();

            Assert.Throws<ArgumentNullException>(() =>
                _foundry.AddValidation(validator, null!, true));
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