using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WorkflowForge.Abstractions;
using WorkflowForge.Extensions.Validation.Options;
using WF = WorkflowForge;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class ValidationExtensionsShould : IDisposable
    {
        private readonly IWorkflowFoundry _foundry;
        private readonly ISystemTimeProvider _timeProvider;

        public ValidationExtensionsShould()
        {
            _timeProvider = SystemTimeProvider.Instance;
            _foundry = WF.WorkflowForge.CreateFoundry("ValidationExtTest");
        }

        public void Dispose()
        {
            (_foundry as IDisposable)?.Dispose();
        }

        [Fact]
        public void AddMiddleware_GivenDataAnnotationsValidation()
        {
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            var result = _foundry.UseValidation(
                f => new TestModel { Value = 10 },
                options);

            Assert.Same(_foundry, result);
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullFoundry()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.UseValidation(validator, f => new TestModel(), null));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullValidator()
        {
            IWorkflowValidator<TestModel>? nullValidator = null;
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseValidation(nullValidator!, f => new TestModel(), null));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenNullDataExtractor()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();

            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseValidation(validator, null!, null));
        }

        [Fact]
        public async Task StoreSuccessInProperties_GivenValidData()
        {
            var data = new TestModel { Value = 10 };

            var result = await _foundry.ValidateAsync(data);

            Assert.True(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.IsValid"));
            Assert.Equal(true, _foundry.Properties["ValidationResult.IsValid"]);
        }

        [Fact]
        public async Task StoreFailureInProperties_GivenInvalidData()
        {
            var data = new TestModel { Value = -1 };

            var result = await _foundry.ValidateAsync(data);

            Assert.False(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.IsValid"));
            Assert.True(_foundry.Properties.ContainsKey("ValidationResult.Errors"));
            Assert.Equal(false, _foundry.Properties["ValidationResult.IsValid"]);
        }

        [Fact]
        public async Task UseCustomKey_GivenCustomPropertyKey()
        {
            var data = new TestModel { Value = 10 };

            await _foundry.ValidateAsync(data, "CustomValidation");

            Assert.True(_foundry.Properties.ContainsKey("CustomValidation"));
            Assert.True(_foundry.Properties.ContainsKey("CustomValidation.IsValid"));
        }

        [Fact]
        public void NotAddMiddleware_GivenEnabledFalse()
        {
            var options = new ValidationMiddlewareOptions { Enabled = false };

            // Create a new foundry to test with
            using var testFoundry = WF.WorkflowForge.CreateFoundry("Test");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(f => new TestModel { Value = 10 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount, GetMiddlewareCount(testFoundry));
        }

        [Fact]
        public void AddMiddleware_GivenEnabledTrue()
        {
            var options = new ValidationMiddlewareOptions { Enabled = true };

            // Create a new foundry to test with
            using var testFoundry = WF.WorkflowForge.CreateFoundry("Test");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(f => new TestModel { Value = 10 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount + 1, GetMiddlewareCount(testFoundry));
        }

        [Fact]
        public void AddMiddleware_GivenCustomValidator()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();
            var options = new ValidationMiddlewareOptions { ThrowOnValidationError = true };

            using var testFoundry = WF.WorkflowForge.CreateFoundry("CustomValidatorTest");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(validator, f => new TestModel { Value = 5 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount + 1, GetMiddlewareCount(testFoundry));
        }

        [Fact]
        public void NotAddMiddleware_GivenCustomValidatorAndDisabledOptions()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();
            var options = new ValidationMiddlewareOptions { Enabled = false };

            using var testFoundry = WF.WorkflowForge.CreateFoundry("DisabledCustomValidatorTest");
            var initialCount = GetMiddlewareCount(testFoundry);

            var result = testFoundry.UseValidation(validator, f => new TestModel { Value = 5 }, options);

            Assert.Same(testFoundry, result);
            Assert.Equal(initialCount, GetMiddlewareCount(testFoundry));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenDataAnnotationsWithNullFoundry()
        {
            IWorkflowFoundry? nullFoundry = null;

            Assert.Throws<ArgumentNullException>(() =>
                nullFoundry!.UseValidation<TestModel>(f => new TestModel()));
        }

        [Fact]
        public void ThrowArgumentNullException_GivenDataAnnotationsWithNullDataExtractor()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _foundry.UseValidation<TestModel>(null!));
        }

        [Fact]
        public async Task StoreSuccessInProperties_GivenCustomValidatorAndValidData()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();
            var data = new TestModel { Value = 42 };

            var result = await _foundry.ValidateAsync(validator, data, "CustomResult");

            Assert.True(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("CustomResult"));
            Assert.True(_foundry.Properties.ContainsKey("CustomResult.IsValid"));
            Assert.Equal(true, _foundry.Properties["CustomResult.IsValid"]);
        }

        [Fact]
        public async Task StoreErrorsInProperties_GivenCustomValidatorAndInvalidData()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();
            var data = new TestModel { Value = -1 };

            var result = await _foundry.ValidateAsync(validator, data, "CustomResult");

            Assert.False(result.IsValid);
            Assert.True(_foundry.Properties.ContainsKey("CustomResult"));
            Assert.True(_foundry.Properties.ContainsKey("CustomResult.Errors"));
            Assert.Equal(false, _foundry.Properties["CustomResult.IsValid"]);
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenCustomValidatorAndNullFoundry()
        {
            IWorkflowFoundry? nullFoundry = null;
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                nullFoundry!.ValidateAsync(validator, new TestModel()));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenCustomValidatorAndNullValidator()
        {
            IWorkflowValidator<TestModel>? nullValidator = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _foundry.ValidateAsync(nullValidator!, new TestModel()));
        }

        [Fact]
        public async Task ThrowArgumentNullException_GivenValidateAsyncNullFoundry()
        {
            IWorkflowFoundry? nullFoundry = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                nullFoundry!.ValidateAsync(new TestModel()));
        }

        private static int GetMiddlewareCount(IWorkflowFoundry foundry)
        {
            var type = foundry.GetType();
            var property = type.GetProperty("MiddlewareCount");
            return property != null ? (int)property.GetValue(foundry)! : 0;
        }

        private class TestModel
        {
            [Range(1, int.MaxValue, ErrorMessage = "Value must be greater than 0")]
            public int Value { get; set; }
        }
    }
}