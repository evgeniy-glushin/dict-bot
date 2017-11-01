using LanguageServices;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Tests.Integration
{
    [TestFixture]
    class LanguageServicesTests
    {
        [Test]
        public async Task Translate_ensure_result_is_not_empty()
        {
            // Act
            var response = await LangProvider.Translate("Kid", "ru");

            // Assert
            Assert.IsNotNull(response);
        }

        [Test]
        public async Task CheckSpelling_ensure_result_is_not_empty()
        {
            // Act
            var response = await LangProvider.CheckSpelling("sircumtances", "en");

            // Assert
            Assert.IsNotNull(response);
        }
    }
}
