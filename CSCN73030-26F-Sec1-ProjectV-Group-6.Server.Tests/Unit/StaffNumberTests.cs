/**
 * @file StaffNumberTests.cs
 * @brief Unit tests for the staff number format (business rule 1).
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Staff numbers are STF- plus 7 zero-padded digits.</summary>
    public class StaffNumberTests
    {
        [Theory]
        [InlineData(1, "STF-0000001")]
        [InlineData(42, "STF-0000042")]
        [InlineData(1234567, "STF-1234567")]
        [InlineData(9999999, "STF-9999999")]
        public void Format_PadsSequenceValueToSevenDigits(long value, string expected)
        {
            Assert.Equal(expected, StaffNumber.Format(value));
        }

        [Fact]
        public void Format_AlwaysProducesElevenCharacters()
        {
            Assert.Equal(StaffNumber.Length, StaffNumber.Format(1).Length);
            Assert.Equal(StaffNumber.Length, StaffNumber.Format(9999999).Length);
        }

        [Theory]
        [InlineData("STF-0000001")]
        [InlineData("STF-9999999")]
        public void IsWellFormed_AcceptsTheStandardFormat(string value)
        {
            Assert.True(StaffNumber.IsWellFormed(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("STF-000001")]      // 6 digits
        [InlineData("STF-00000001")]    // 8 digits
        [InlineData("stf-0000001")]     // lower-case prefix
        [InlineData("EMS-0000001")]     // a department code is never part of the number
        [InlineData("STF0000001")]      // missing hyphen
        [InlineData("STF-00000A1")]     // a letter
        [InlineData(" STF-0000001")]    // whitespace
        public void IsWellFormed_RejectsAnythingElse(string? value)
        {
            Assert.False(StaffNumber.IsWellFormed(value));
        }
    }
}
