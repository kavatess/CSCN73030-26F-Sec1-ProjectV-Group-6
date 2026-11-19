/**
 * @file CredentialStatusRulesTests.cs
 * @brief Unit tests for deriving credential status from the expiry date (business rule 5).
 */

using CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Domain;

namespace CSCN73030_26F_Sec1_ProjectV_Group_6.Server.Tests.Unit
{
    /// <summary>Expired if past, Expiring within 30 days, otherwise Valid; Suspended overrides.</summary>
    public class CredentialStatusRulesTests
    {
        private static readonly DateOnly Today = new(2026, 11, 5);

        [Theory]
        [InlineData(-365, CredentialStatus.Expired)]
        [InlineData(-1, CredentialStatus.Expired)]
        [InlineData(0, CredentialStatus.Expiring)]   // expires today: still usable today
        [InlineData(1, CredentialStatus.Expiring)]
        [InlineData(30, CredentialStatus.Expiring)]  // last day of the 30-day window
        [InlineData(31, CredentialStatus.Valid)]
        [InlineData(400, CredentialStatus.Valid)]
        public void Derive_UsesExpiryDateRelativeToToday(int expiresInDays, CredentialStatus expected)
        {
            var status = CredentialStatusRules.Derive(CredentialStatus.Valid, Today.AddDays(expiresInDays), Today);

            Assert.Equal(expected, status);
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(10)]
        [InlineData(400)]
        public void Derive_SuspendedOverridesTheDate(int expiresInDays)
        {
            var status = CredentialStatusRules.Derive(CredentialStatus.Suspended, Today.AddDays(expiresInDays), Today);

            Assert.Equal(CredentialStatus.Suspended, status);
        }

        [Theory]
        [InlineData(CredentialStatus.Valid)]
        [InlineData(CredentialStatus.Expiring)]
        [InlineData(CredentialStatus.Expired)]
        public void Derive_IgnoresAStaleStoredStatus(CredentialStatus stored)
        {
            // A credential stored as Valid two years ago must read as Expired once its date passes.
            Assert.Equal(CredentialStatus.Expired, CredentialStatusRules.Derive(stored, Today.AddDays(-1), Today));
            Assert.Equal(CredentialStatus.Valid, CredentialStatusRules.Derive(stored, Today.AddDays(100), Today));
        }

        [Fact]
        public void Derive_ForACredential_MatchesTheValueOverload()
        {
            var credential = new Credential { ExpiryDate = Today.AddDays(12), Status = CredentialStatus.Valid };

            Assert.Equal(CredentialStatus.Expiring, CredentialStatusRules.Derive(credential, Today));
        }

        /// <summary>The SQL filter must agree with Derive for every state and every date around the boundaries.</summary>
        [Fact]
        public void IsInState_AgreesWithDerive_ForEveryStateAndBoundary()
        {
            var offsets = new[] { -400, -31, -30, -1, 0, 1, 29, 30, 31, 32, 400 };
            var storedStatuses = Enum.GetValues<CredentialStatus>();

            foreach (var state in Enum.GetValues<CredentialStatus>())
            {
                var filter = CredentialStatusRules.IsInState(state, Today).Compile();
                foreach (var stored in storedStatuses)
                foreach (var offset in offsets)
                {
                    var credential = new Credential { Status = stored, ExpiryDate = Today.AddDays(offset) };
                    var expected = CredentialStatusRules.Derive(credential, Today) == state;
                    Assert.True(expected == filter(credential),
                        $"state {state}, stored {stored}, expires in {offset} days");
                }
            }
        }

        [Fact]
        public void Worst_PicksTheMostUrgentStatus()
        {
            Assert.Equal(CredentialStatus.Expired, CredentialStatusRules.Worst(
                [CredentialStatus.Valid, CredentialStatus.Suspended, CredentialStatus.Expiring, CredentialStatus.Expired]));
            Assert.Equal(CredentialStatus.Suspended, CredentialStatusRules.Worst(
                [CredentialStatus.Valid, CredentialStatus.Expiring, CredentialStatus.Suspended]));
            Assert.Equal(CredentialStatus.Expiring, CredentialStatusRules.Worst(
                [CredentialStatus.Valid, CredentialStatus.Expiring]));
            Assert.Equal(CredentialStatus.Valid, CredentialStatusRules.Worst([CredentialStatus.Valid]));
        }

        [Fact]
        public void Worst_ReturnsNullWhenNoCredentialsAreHeld()
        {
            Assert.Null(CredentialStatusRules.Worst([]));
        }
    }
}
