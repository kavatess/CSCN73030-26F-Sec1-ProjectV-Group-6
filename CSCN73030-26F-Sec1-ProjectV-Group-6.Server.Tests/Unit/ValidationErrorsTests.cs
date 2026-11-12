/**
 * @file ValidationErrorsTests.cs
 * @brief Unit tests for the 422 field-error collector.
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Errors;
using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Application.Validation;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>ValidationErrors collects every field problem, then throws one 422.</summary>
    public class ValidationErrorsTests
    {
        [Fact]
        public void ThrowIfAny_DoesNothingWhenThereAreNoErrors()
        {
            var errors = new ValidationErrors();
            errors.Required("name", "Avery");

            Assert.False(errors.HasErrors);
            errors.ThrowIfAny();
        }

        [Fact]
        public void ThrowIfAny_ThrowsOne422WithEveryField()
        {
            var errors = new ValidationErrors();
            errors.Required("firstName", "");
            errors.Required("lastName", null);
            errors.Add("lastName", "A second message.");

            var ex = Assert.Throws<ValidationFailedException>(errors.ThrowIfAny);

            Assert.Equal(422, ex.StatusCode);
            Assert.Equal(["firstName", "lastName"], ex.Errors.Keys.Order());
            Assert.Equal(2, ex.Errors["lastName"].Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Required_RejectsMissingOrBlankText(string? value)
        {
            var errors = new ValidationErrors();
            errors.Required("field", value);
            Assert.True(errors.HasErrors);
        }

        [Fact]
        public void Required_RejectsAMissingValueType()
        {
            var errors = new ValidationErrors();
            errors.Required<int>("positions", null);
            Assert.True(errors.HasErrors);
        }

        [Fact]
        public void Required_ChecksMaxLength()
        {
            var errors = new ValidationErrors();
            errors.Required("code", "ABCDEFGHIJK", maxLength: 10);
            Assert.True(errors.HasErrors);
        }

        [Fact]
        public void MaxLength_IgnoresSurroundingWhitespaceAndNull()
        {
            var errors = new ValidationErrors();
            errors.MaxLength("code", "  ABC  ", 3);
            errors.MaxLength("code", null, 3);
            Assert.False(errors.HasErrors);
        }

        [Fact]
        public void NotBlankIfPresent_AllowsNullButNotBlank()
        {
            var errors = new ValidationErrors();
            errors.NotBlankIfPresent("firstName", null);
            Assert.False(errors.HasErrors);

            errors.NotBlankIfPresent("firstName", " ");
            Assert.True(errors.HasErrors);
        }

        [Theory]
        [InlineData("avery.quillfeather@cmes.example")]
        [InlineData("a@b")]
        public void Email_AcceptsAPlainAddress(string value)
        {
            var errors = new ValidationErrors();
            errors.Email("email", value);
            Assert.False(errors.HasErrors);
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("Avery <avery@cmes.example>")] // a display name is not a bare address
        [InlineData("two@at@signs.example")]
        public void Email_RejectsAnythingButOneAddress(string value)
        {
            var errors = new ValidationErrors();
            errors.Email("email", value);
            Assert.True(errors.HasErrors);
        }

        [Fact]
        public void AddIf_OnlyAddsWhenTheConditionHolds()
        {
            var errors = new ValidationErrors();
            errors.AddIf(false, "a", "never");
            Assert.False(errors.HasErrors);
            errors.AddIf(true, "a", "always");
            Assert.True(errors.HasErrors);
        }

        [Fact]
        public void Text_TrimToNullAndNormaliseEmail()
        {
            Assert.Null(Text.TrimToNull("   "));
            Assert.Equal("Ave", Text.TrimToNull("  Ave "));
            Assert.Equal("avery@cmes.example", Text.NormaliseEmail("  Avery@CMES.example "));
        }
    }
}
