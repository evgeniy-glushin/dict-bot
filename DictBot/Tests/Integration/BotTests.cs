using NUnit.Framework;
using System.Threading.Tasks;
using static Core.Bot;

namespace Tests.Integration
{
    [TestFixture]
    class BotTests
    {
        [Test]
        public async Task Respond_single_correct_word()
        {
            // Act
            var translation = await respond("building");

            // Assert
            Assert.AreEqual("здание", translation);
        }

        [Test]
        public async Task Respond_single_misspelled_word()
        {
            // Arrange 
            var misspelledWord = "buildng";

            // Act
            var translation = await respond(misspelledWord);

            // Assert
            Assert.AreEqual("building - здание", translation);
        }
    }
}
