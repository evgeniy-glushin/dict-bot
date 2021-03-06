﻿using NUnit.Framework;
using System.Threading.Tasks;
using static Domain;
using static Bot;
using static Data;
using static Repository;
using System.Linq;
using System;
using MongoDB.Bson;

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
        public async Task ChangeLangCommand()
        {
            // Arrange
            var _ = await respondAsync(CreatePayload("/start")); // creates user
            var lang = "ru";
            BotPayload payload = CreatePayload($"/setLang {lang}");

            // Act
            var resp = await respondAsync(payload);

            // Assert
            Assert.AreEqual($"The language has been changed to {lang}", resp);
            var usr = findUser(payload.UserId).Value;
            Assert.AreEqual(lang, usr.Lang);
        }


        [Test]
        public async Task LearnCommand_updates_word_statistic()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("работа", "work"), ("здание", "building"), ("привет", "hello") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            // Act
            await respondAsync(payload);
            await respondAsync(CreatePayload("hello"));
            await respondAsync(CreatePayload("misspelled building"));

            // Assert
            var wordToTest1 = tryFindWord("привет", payload.UserId).Value;
            Assert.AreEqual(1, wordToTest1.Trained);
            Assert.AreEqual(1, wordToTest1.Succeeded);

            var wordToTest2 = tryFindWord("здание", payload.UserId).Value;
            Assert.AreEqual(1, wordToTest2.Trained);
            Assert.AreEqual(0, wordToTest2.Succeeded);
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
            var session = tryFindSession(payload.UserId).Value;

            Assert.AreNotEqual(session.CreateDate, session.ChangeDate);
            Assert.AreEqual(1, session.Idx);
        }

        [Test]
        public async Task LearnCommand_whole_flow_with_few_incorrect_attempts()
        {
            // Arrange
            var payload = CreatePayload("/learn");
            var words = (new[] { ("работа", "work"), ("здание", "building"), ("привет", "hello") })
                .Select(x => CreateWord(x.Item1, x.Item2));
            words.Select(insertNewWord).ToArray(); // insert

            var res1 = await respondAsync(payload);
            Assert.AreEqual("Translate following words in English. <br/> привет", res1);

            var res2 = await respondAsync(CreatePayload("hello"));
            Assert.AreEqual("Correct! Try the next one <br/> здание", res2);

            var res3 = await respondAsync(CreatePayload("cat"));
            Assert.AreEqual("Incorrect! Try the next one <br/> работа", res3);

            var res4 = await respondAsync(CreatePayload("work"));
            Assert.AreEqual("Correct! Try the next one <br/> здание", res4);

            var res5 = await respondAsync(CreatePayload("bulding"));
            Assert.AreEqual("Incorrect! Try the next one <br/> здание", res5);

            var res6 = await respondAsync(CreatePayload("building"));
            Assert.AreEqual("Correct! You are done.", res6);
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
            var session = tryFindSession(payload.UserId).Value;
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
            var hasSession = tryFindSession(payload.UserId).HasValue;

            Assert.False(hasSession);
            Assert.AreEqual("Not enough words.", result);
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
            var session = tryFindSession(payload.UserId).Value;
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
            Assert.AreEqual("Not enough words.", result);
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
        public async Task StartCommand_should_ask_the_language()
        {
            // Act
            var translation = await respondAsync(CreatePayload("/start"));

            // Assert
            Assert.AreEqual("Which language would you like to learn?<br/>Please write 'en' or 'ru'", translation);
        }

        [Test]
        public async Task StartCommand_user_should_show_up_in_UsersCollection()
        {
            // Act
            BotPayload payload = CreatePayload("/start");
            var translation = await respondAsync(payload);

            // Assert
            var usr = findUser(payload.UserId).Value;
            Assert.IsNotNull(usr);
            Assert.AreEqual(payload.UserName, usr.Name);
            Assert.AreEqual("en", usr.Lang);
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
            var savedWord = tryFindWord(word, payload.UserId).Value;

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
            new Dictionary(ObjectId.GenerateNewId(), "test_id", word, new[] { new Word(trans, 1.0) }, "en", "ru", 0, succeeded, DateTime.UtcNow, DateTime.UtcNow, "Tests", "none");
    }
}
