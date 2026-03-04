using WorkflowForge.Extensions.Audit.Options;
using Xunit;

namespace WorkflowForge.Extensions.Audit.Tests
{
    public class AuditMiddlewareOptionsShould
    {
        [Fact]
        public void HaveCorrectDefaults_GivenDefaultConstructor()
        {
            var options = new AuditMiddlewareOptions();

            Assert.Equal(AuditMiddlewareOptions.DefaultSectionName, options.SectionName);
            Assert.True(options.Enabled);
            Assert.Equal(AuditDetailLevel.Standard, options.DetailLevel);
            Assert.False(options.LogDataPayloads);
            Assert.True(options.IncludeTimestamps);
            Assert.True(options.IncludeUserContext);
        }

        [Fact]
        public void SetCustomSectionName_GivenCustomSectionNameConstructor()
        {
            var customSection = "MyApp:Audit";

            var options = new AuditMiddlewareOptions(customSection);

            Assert.Equal(customSection, options.SectionName);
        }

        [Fact]
        public void ReturnNoValidationErrors_GivenDefaultOptions()
        {
            var options = new AuditMiddlewareOptions();

            var errors = options.Validate();

            Assert.Empty(errors);
        }

        [Fact]
        public void ProduceIndependentCopy_GivenClone()
        {
            var original = new AuditMiddlewareOptions("Custom:Section")
            {
                Enabled = false,
                DetailLevel = AuditDetailLevel.Verbose,
                LogDataPayloads = true,
                IncludeTimestamps = false,
                IncludeUserContext = false
            };

            var clone = (AuditMiddlewareOptions)original.Clone();

            Assert.Equal("Custom:Section", clone.SectionName);
            Assert.False(clone.Enabled);
            Assert.Equal(AuditDetailLevel.Verbose, clone.DetailLevel);
            Assert.True(clone.LogDataPayloads);
            Assert.False(clone.IncludeTimestamps);
            Assert.False(clone.IncludeUserContext);

            // Verify independence
            clone.Enabled = true;
            Assert.False(original.Enabled);
        }

        [Fact]
        public void CloneDefaultOptions_GivenDefaultConstructor()
        {
            var original = new AuditMiddlewareOptions();

            var clone = (AuditMiddlewareOptions)original.Clone();

            Assert.Equal(original.SectionName, clone.SectionName);
            Assert.Equal(original.Enabled, clone.Enabled);
            Assert.Equal(original.DetailLevel, clone.DetailLevel);
            Assert.Equal(original.LogDataPayloads, clone.LogDataPayloads);
            Assert.Equal(original.IncludeTimestamps, clone.IncludeTimestamps);
            Assert.Equal(original.IncludeUserContext, clone.IncludeUserContext);
        }

        [Fact]
        public void SupportAllDetailLevels_GivenDetailLevelEnum()
        {
            var minimal = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Minimal };
            var standard = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Standard };
            var verbose = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Verbose };
            var complete = new AuditMiddlewareOptions { DetailLevel = AuditDetailLevel.Complete };

            Assert.Equal(AuditDetailLevel.Minimal, minimal.DetailLevel);
            Assert.Equal(AuditDetailLevel.Standard, standard.DetailLevel);
            Assert.Equal(AuditDetailLevel.Verbose, verbose.DetailLevel);
            Assert.Equal(AuditDetailLevel.Complete, complete.DetailLevel);
        }
    }
}
