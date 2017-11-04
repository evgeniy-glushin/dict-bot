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
            Assert.AreEqual("здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_single_misspelled_word()
        {
            // Arrange 
            var misspelledWord = "buildng";

            // Act
            var translation = await respond(misspelledWord);

            // Assert
            Assert.AreEqual("building - здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_few_correct_words()
        {
            // Arrange 
            var correctWords = "Hello World";

            // Act
            var translation = await respond(correctWords);

            // Assert
            Assert.AreEqual("всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_misspelled_few_words()
        {
            // Arrange 
            var misspelledWords = "Hollo Worlda";

            // Act
            var translation = await respond(misspelledWords);

            // Assert
            Assert.AreEqual("hello world - всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_given_sentence_with_some_misspelled_words()
        {
            // Arrange 
            var sentence = "from simple sentences to compount and complex sentences";

            // Act
            var translation = await respond(sentence);

            // Assert
            Assert.AreEqual("from simple sentences to compound and complex sentences - от простых предложений для составных и сложных предложений", translation.ToLower());
        }
    }
}
