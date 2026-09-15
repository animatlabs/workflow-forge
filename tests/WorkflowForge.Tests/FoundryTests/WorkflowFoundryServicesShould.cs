using System;

namespace WorkflowForge.Tests.FoundryTests
{
    public class WorkflowFoundryServicesShould
    {
        [Fact]
        public void NotReplaceExisting_WhenTryAdd_GivenKeyAlreadyPresent()
        {
            using var foundry = WorkflowForge.CreateFoundry("ServicesTryAdd");
            var first = new object();
            var second = new object();

            Assert.True(foundry.Services.TryAdd("slot", first));
            Assert.False(foundry.Services.TryAdd("slot", second));
            Assert.True(foundry.Services.TryGet<object>("slot", out var stored));
            Assert.Same(first, stored);
        }

        [Fact]
        public void ExposeServices_GivenCreateFoundry()
        {
            using var foundry = WorkflowForge.CreateFoundry("ServicesExposure");
            Assert.NotNull(foundry.Services);
        }
    }
}
