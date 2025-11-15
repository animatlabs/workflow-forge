using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class FluentValidationAdapterTests
    {
        [Fact]
        public async Task ValidateAsync_WithValidData_ShouldReturnSuccess()
        {
            var validator = new TestValidator();
            var adapter = new FluentValidationAdapter<TestModel>(validator);

            var result = await adapter.ValidateAsync(new TestModel { Name = "Valid", Age = 25 }, CancellationToken.None);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithInvalidData_ShouldReturnFailure()
        {
            var validator = new TestValidator();
            var adapter = new FluentValidationAdapter<TestModel>(validator);

            var result = await adapter.ValidateAsync(new TestModel { Name = "", Age = -1 }, CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
            Assert.Contains(result.Errors, e => e.PropertyName == "Age");
        }

        [Fact]
        public async Task ValidateAsync_WithPartiallyInvalidData_ShouldReturnOnlyFailedRules()
        {
            var validator = new TestValidator();
            var adapter = new FluentValidationAdapter<TestModel>(validator);

            var result = await adapter.ValidateAsync(new TestModel { Name = "Valid", Age = -1 }, CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Age", result.Errors[0].PropertyName);
        }

        [Fact]
        public void Constructor_WithNullValidator_ShouldThrowArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new FluentValidationAdapter<TestModel>(null!));
        }

        private class TestModel
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        private class TestValidator : AbstractValidator<TestModel>
        {
            public TestValidator()
            {
                RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
                RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0");
            }
        }
    }
}

