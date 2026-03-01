using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class DataAnnotationsWorkflowValidatorShould
    {
        [Fact]
        public async Task ReturnSuccess_GivenValidData()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();

            var result = await validator.ValidateAsync(new TestModel { Name = "Valid", Age = 25 }, CancellationToken.None);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ReturnFailure_GivenInvalidData()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();

            var result = await validator.ValidateAsync(new TestModel { Name = "", Age = -1 }, CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
            Assert.Contains(result.Errors, e => e.PropertyName == "Age");
        }

        [Fact]
        public async Task ReturnOnlyFailedRules_GivenPartiallyInvalidData()
        {
            var validator = new DataAnnotationsWorkflowValidator<TestModel>();

            var result = await validator.ValidateAsync(new TestModel { Name = "Valid", Age = -1 }, CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Age", result.Errors[0].PropertyName);
        }

        private class TestModel
        {
            [Required(ErrorMessage = "Name is required")]
            public string Name { get; set; } = string.Empty;

            [Range(1, int.MaxValue, ErrorMessage = "Age must be greater than 0")]
            public int Age { get; set; }
        }
    }
}