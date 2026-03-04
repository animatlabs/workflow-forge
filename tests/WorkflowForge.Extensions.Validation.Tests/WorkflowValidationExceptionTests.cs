using System;
using System.Collections.Generic;
using WorkflowForge.Extensions.Validation;
using Xunit;

namespace WorkflowForge.Extensions.Validation.Tests
{
    public class WorkflowValidationExceptionShould
    {
        [Fact]
        public void SetMessageAndErrors_GivenConstructorWithMessageAndErrors()
        {
            var errors = new List<ValidationError>
            {
                new ValidationError("Name", "Name is required"),
                new ValidationError("Age", "Age must be positive")
            };

            var ex = new WorkflowValidationException("Validation failed", errors);

            Assert.Equal("Validation failed", ex.Message);
            Assert.Equal(2, ex.Errors.Count);
            Assert.Contains(ex.Errors, e => e.PropertyName == "Name");
            Assert.Contains(ex.Errors, e => e.PropertyName == "Age");
        }

        [Fact]
        public void SetMessageInnerExceptionAndErrors_GivenConstructorWithInnerException()
        {
            var inner = new InvalidOperationException("inner cause");
            var errors = new List<ValidationError>
            {
                new ValidationError("Field", "Field is invalid")
            };

            var ex = new WorkflowValidationException("Validation failed with inner", inner, errors);

            Assert.Equal("Validation failed with inner", ex.Message);
            Assert.Same(inner, ex.InnerException);
            Assert.Single(ex.Errors);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenConstructorWithNullErrors()
        {
            var ex = new WorkflowValidationException("Validation failed", (IEnumerable<ValidationError>)null!);

            Assert.NotNull(ex.Errors);
            Assert.Empty(ex.Errors);
        }

        [Fact]
        public void ReturnEmptyErrors_GivenConstructorWithInnerExceptionAndNullErrors()
        {
            var inner = new Exception("inner");

            var ex = new WorkflowValidationException("Validation failed", inner, null!);

            Assert.NotNull(ex.Errors);
            Assert.Empty(ex.Errors);
        }

        [Fact]
        public void BeException_GivenAnyConstructor()
        {
            var ex1 = new WorkflowValidationException("msg", new List<ValidationError>());
            var ex2 = new WorkflowValidationException("msg", new Exception("inner"), new List<ValidationError>());

            Assert.IsAssignableFrom<Exception>(ex1);
            Assert.IsAssignableFrom<Exception>(ex2);
        }
    }
}
