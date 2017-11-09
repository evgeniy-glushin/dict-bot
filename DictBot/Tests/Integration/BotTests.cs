using NUnit.Framework;
using System.Threading.Tasks;
using static BotModels;
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
            var translation = await respond(CreatePayload("building"));

            // Assert
            Assert.AreEqual("здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_single_misspelled_word()
        {
            // Arrange 
            var misspelledWord = "buildng";

            // Act
            var translation = await respond(CreatePayload(misspelledWord));

            // Assert
            Assert.AreEqual("building - здание", translation.ToLower());
        }

        [Test]
        public async Task Respond_few_correct_words()
        {
            // Arrange 
            var correctWords = "Hello World";

            // Act
            var translation = await respond(CreatePayload(correctWords));

            // Assert
            Assert.AreEqual("всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_misspelled_few_words()
        {
            // Arrange 
            var misspelledWords = "Hollo Worlda";

            // Act
            var translation = await respond(CreatePayload(misspelledWords));

            // Assert
            Assert.AreEqual("hello world - всем привет", translation.ToLower());
        }

        [Test]
        public async Task Respond_given_sentence_with_some_misspelled_words()
        {
            // Arrange 
            var sentence = "from simple sentences to compount and complex sentences";

            // Act
            var translation = await respond(CreatePayload(sentence));

            // Assert
            Assert.AreEqual("from simple sentences to compound and complex sentences - от простых предложений для составных и сложных предложений", translation.ToLower());
        }

        [Test]
        public async Task Respond_translate_ru_to_en()
        {
            // Arrange 
            var sentence = "привет мир";

            // Act
            var translation = await respond(CreatePayload(sentence));

            // Assert
            Assert.AreEqual("hello world", translation.ToLower());
        }

        TranslatePayload CreatePayload(string str) =>
            new TranslatePayload("test_id", "test_name", str);
    }
}
