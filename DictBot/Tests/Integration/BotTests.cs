using NUnit.Framework;
using System.Threading.Tasks;
using static Domain;
using static Core.Bot;
using static DataUtils;
using System.Linq;
using System;

namespace Tests.Integration
{
    [TestFixture]
    class BotTests
    {
        [SetUp]
        public void BeforeEach()
        {
            dropDatabase();
        }

        [Test]
        public async Task LearnCommand_should_start_over_if_misspelled_words()
        {
            // Arrange

            // Act

            // Assert
        }

        [Test]
        public async Task LearnCommand_should_change_words_pointer()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("работа", "work"), ("здание", "building"), ("привет", "hello") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            await respondAsync(payload);
            await respondAsync(CreatePayload("hello"));

            // Assert
            var session = tryFindSession(payload.UserId);

            Assert.AreNotEqual(session.CreateDate, session.ChangeDate);
            Assert.AreEqual(1, session.Ptr);
        }

        [Test]
        public async Task LearnCommand_whole_flow()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("работа", "work"), ("здание", "building"), ("привет", "hello") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            var res1 = await respondAsync(payload);
            Assert.AreEqual("Translate following words in English. <br/> привет", res1);

            var res2 = await respondAsync(CreatePayload("hello"));
            Assert.AreEqual("Correct! Try the next one <br/> здание", res2);

            var res3 = await respondAsync(CreatePayload("cat"));
            Assert.AreEqual("Incorrect! Try the next one <br/> работа", res3);

            var res4 = await respondAsync(CreatePayload("work"));
            Assert.AreEqual("Correct! Try the next one <br/> здание", res4);

            var res5 = await respondAsync(CreatePayload("building"));
            Assert.AreEqual("Correct! You are done.", res5);
        }

        [Test]
        public async Task LearnCommand_should_start_with_instructions()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("work", "работа"), ("building", "здание") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            var res = await respondAsync(payload);

            // Assert
            Assert.True(res.StartsWith("Translate following words in English"));
        }

        [Test]
        public async Task LearnCommand_less_than_5_words()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("work", "работа"),
                                 ("building", "здание"),
                                 ("word1", "word1"),
                                 ("word2", "word2"),
                                 ("word3", "word3")})
                .Select(x => CreateWord(x.Item1, x.Item2)); 
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            var result = await respondAsync(payload);

            // Assert
            var session = tryFindSession(payload.UserId);
            Assert.IsNotNull(session);
            Assert.AreEqual(4, session.Words.Count());
        }

        [Test]
        public async Task LearnCommand_takes_latest_words()
        {
            // TODO

            // Arrange
          
            // Act

            // Assert
        }

        [Test]
        public async Task LearnCommand_learned_words_are_not_in_the_list()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("work", "работа"), ("building", "здание") })
                .Select(x => CreateWord(x.Item1, x.Item2, 4)); // TODO: put 4 to constant
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            var result = await respondAsync(payload);

            // Assert
            var session = tryFindSession(payload.UserId);

            Assert.IsNull(session);
            Assert.AreEqual("Not enough words", result);
        }

        [Test]
        public async Task LearnCommand_some_words_available()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("work", "работа"), ("building", "здание") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            var _ = await respondAsync(payload);

            // Assert
            var session = tryFindSession(payload.UserId);
            Assert.IsNotNull(session);
            Assert.AreEqual(words.Count(), session.Words.Count());
            var allInSession = words.All(w => session.Words.Any(x => x.Word == w.Word));
            Assert.True(allInSession);
        }

        [Test]
        public async Task LearnCommand_not_enough_words()
        {
            // Arrange
            var payload = CreatePayload("/learn");

            // Act
            var result = await respondAsync(payload);

            // Assert
            Assert.AreEqual("Not enough words", result);
        }

        [Test]
        public async Task Respond_unknown_command()
        {
            // Act
            var translation = await respondAsync(CreatePayload("/unknown"));

            // Assert
            Assert.AreEqual("unknown command", translation);
        }

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
        public async Task Respond_should_not_add_word_to_dictionary_when_exists()
        {
            // Arrange
            var word = "building";
            var payload = CreatePayload(word);

            // Act
            var translation1 = await respondAsync(payload);
            var translation2 = await respondAsync(payload);

            // Assert
            Assert.AreEqual(translation1, translation2);

            var words = tryFindWords(word, payload.UserId);
            Assert.AreEqual(1, words.Count());
        }

        [Test]
        public async Task Respond_should_add_word_to_dictionary()
        {
            // Arrange
            var word = "building";
            var payload = CreatePayload(word);

            // Act
            var translation = await respondAsync(payload);

            // Assert
            var savedWord = tryFindWord(word, payload.UserId);

            Assert.IsNotNull(savedWord);
            Assert.AreEqual(word, savedWord.Word.ToLower());
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

        Dictionary CreateWord(string word, string trans, int succeeded = 0) =>
            new Dictionary("test_id", word, new[] { new Word(trans, 1.0) }, "en", "ru", 0, succeeded, DateTime.UtcNow, "Tests", "none");
    }
}
