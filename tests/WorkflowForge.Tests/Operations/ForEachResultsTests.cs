using System;
using WorkflowForge.Operations;
using Xunit;

namespace WorkflowForge.Tests.Operations
{
    public class ForEachResultsShould
    {
        [Fact]
        public void HaveCorrectDefaults_GivenDefaultConstructor()
        {
            var results = new ForEachResults();

            Assert.NotNull(results.Results);
            Assert.Empty(results.Results);
            Assert.Equal(0, results.TotalResults);
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public void ReturnCorrectCount_GivenResultsArray()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "a", "b", "c" },
                TotalResults = 3
            };

            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void ReturnResultAtIndex_GivenGetResultWithValidIndex()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "first", 42, null }
            };

            Assert.Equal("first", results.GetResult(0));
            Assert.Equal(42, results.GetResult(1));
            Assert.Null(results.GetResult(2));
        }

        [Fact]
        public void ReturnNull_GivenGetResultWithNegativeIndex()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "value" }
            };

            Assert.Null(results.GetResult(-1));
        }

        [Fact]
        public void ReturnNull_GivenGetResultWithIndexOutOfRange()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "value" }
            };

            Assert.Null(results.GetResult(5));
        }

        [Fact]
        public void ReturnTypedResult_GivenGetResultGenericWithValidIndex()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "hello", 99, true }
            };

            Assert.Equal("hello", results.GetResult<string>(0));
            Assert.Equal(99, results.GetResult<int>(1));
            Assert.True(results.GetResult<bool>(2));
        }

        [Fact]
        public void ReturnDefault_GivenGetResultGenericWithWrongType()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "not-an-int" }
            };

            var value = results.GetResult<int>(0);

            Assert.Equal(default(int), value);
        }

        [Fact]
        public void ReturnDefault_GivenGetResultGenericWithOutOfRangeIndex()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "value" }
            };

            var value = results.GetResult<string>(10);

            Assert.Null(value);
        }

        [Fact]
        public void ReturnDefault_GivenGetResultGenericWithNegativeIndex()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { "value" }
            };

            var value = results.GetResult<string>(-1);

            Assert.Null(value);
        }

        [Fact]
        public void StoreTimestamp_GivenTimestampAssignment()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var results = new ForEachResults { Timestamp = timestamp };

            Assert.Equal(timestamp, results.Timestamp);
        }

        [Fact]
        public void ReturnNullForNullResultAtIndex_GivenGetResultGenericWithNullEntry()
        {
            var results = new ForEachResults
            {
                Results = new object?[] { null }
            };

            var value = results.GetResult<string>(0);

            Assert.Null(value);
        }
    }
}
