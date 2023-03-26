using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signapse.Services;
using Signapse.Test;

namespace Signapse.Data.Tests
{
    [TestClass]
    public class DatabaseEntryExtensionsTests
    {
        [DataTestMethod]
        [DynamicData(nameof(MatchingUserPolicies))]
        public void Matching_Policies_Succeed(TestEntry original, IAuthResults authResults, TestEntry expected)
        {
            var expectedJson = expected.Serialize();
            var securedPolicy = original.ApplyPolicyAccess(authResults).Serialize();

            Assert.AreEqual(expectedJson, securedPolicy);
        }

        public static IEnumerable<object[]> MatchingUserPolicies
        {
            get
            {
                // Unauthenticated User
                yield return new object[] {
                    new TestEntry(),
                    new TestAuthResults(false),
                    new TestEntry() {
                        UserProperty = null,
                        AdminProperty = null,
                        AuthenticatedUserProperty = null,
                        UsersAdminProperty = null,
                        AffiliatesAdminProperty = null,
                    },
                };

                // Normal User
                yield return new object[] {
                    new TestEntry(),
                    new TestAuthResults(true),
                    new TestEntry() {
                        AdminProperty = null,
                        UsersAdminProperty = null,
                        AffiliatesAdminProperty = null,
                    },
                };

                // User Administrator
                yield return new object[] {
                    new TestEntry(),
                    new TestAuthResults(true, AdministratorFlag.User),
                    new TestEntry()
                    {
                        AffiliatesAdminProperty = null
                    },
                };

                // Affiliate Administrator
                yield return new object[] {
                    new TestEntry(),
                    new TestAuthResults(true, AdministratorFlag.Affiliate),
                    new TestEntry()
                    {
                        UsersAdminProperty = null
                    },
                };

                // Full Administrator
                yield return new object[] {
                    new TestEntry(),
                    new TestAuthResults(true, AdministratorFlag.Full),
                    new TestEntry(),
                };
            }
        }

        public class TestEntry : IDatabaseEntry
        {
            public Guid ID { get; set; } = Guid.Empty;

            public string? PublicProperty { get; set; } = "Public Property";

            [PolicyAccess]
            public string? AuthenticatedUserProperty { get; set; } = "Auth User Property";

            [PolicyAccess(Policies.User)]
            public string? UserProperty { get; set; } = "User Property";

            [PolicyAccess(Policies.Administrator)]
            public string? AdminProperty { get; set; } = "Admin Property";

            [PolicyAccess(Policies.UsersAdministrator)]
            public string? UsersAdminProperty { get; set; } = "Users Admin Property";

            [PolicyAccess(Policies.AffiliatesAdministrator)]
            public string? AffiliatesAdminProperty { get; set; } = "Affiliates Admin Property";
        }
    }
}