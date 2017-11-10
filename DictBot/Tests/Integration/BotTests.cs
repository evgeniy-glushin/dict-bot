using NUnit.Framework;
using System.Threading.Tasks;
using static Domain;
using static Core.Bot;

namespace Tests.Integration
{
    [TestFixture]
    class BotTests
    {
        [Test]
        public async Task Respond_start_command()
        {
            // Act
            var translation = await respondAsync(CreatePayload("/start"));

            // Assert
            Assert.AreEqual("Welcome!", translation);
        }

        [Test]
        public async Task Respond_help_command()
        {
            // Act
            var translation = await respondAsync(CreatePayload("/help"));

            // Assert
            Assert.AreEqual("Help", translation);
        }

        [Test]
        public async Task Respond_single_correct_word()
        {
            // Act
            var translation = await respondAsync(CreatePayload("building"));

            // Assert
            Assert.AreEqual("здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_single_misspelled_word()
        {
            // Arrange 
            var misspelledWord = "buildng";

            // Act
            var translation = await respondAsync(CreatePayload(misspelledWord));

            // Assert
            Assert.AreEqual("building - здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_few_correct_words()
        {
            // Arrange 
            var correctWords = "Hello World";

            // Act
            var translation = await respondAsync(CreatePayload(correctWords));

            // Assert
            Assert.AreEqual("всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_misspelled_few_words()
        {
            // Arrange 
            var misspelledWords = "Hollo Worlda";

            // Act
            var translation = await respondAsync(CreatePayload(misspelledWords));

            // Assert
            Assert.AreEqual("hello world - всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_given_sentence_with_some_misspelled_words()
        {
            // Arrange 
            var sentence = "from simple sentences to compount and complex sentences";

            // Act
            var translation = await respondAsync(CreatePayload(sentence));

            // Assert
            Assert.AreEqual("from simple sentences to compound and complex sentences - от простых предложений для составных и сложных предложений", translation.ToLower());
        }

        [Test]
        public async Task Respond_translate_ru_to_en()
        {
            // Arrange 
            var sentence = "привет мир";

            // Act
            var translation = await respondAsync(CreatePayload(sentence));

            // Assert
            Assert.AreEqual("hello world", translation.ToLower());
        }

        BotPayload CreatePayload(string str) =>
            new BotPayload("test_id", "test_name", str);
    }
}
