using NUnit.Framework;
using static Domain;
using static Core.Bot;
using System.Linq;
using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace Tests.Unit
{
    [TestFixture]
    class BotTests
    {
        SessionBuilder _sessionBuilder;

        [SetUp]
        public void BeforeEach()
        {
            _sessionBuilder = new SessionBuilder();
        }

        [Test]
        public void NewSessionState_should_return_next_index()
        {
            // Arrange
            var oldSession = _sessionBuilder.Default()
                .SetLastWordAsCurrent()
                .Create();

            // Act
            var newSession = newSessionState(oldSession, true, DateTime.UtcNow);

            // Assert
            Assert.AreEqual(0, newSession.Idx);
        }

        [Test]
        public void NewSessionState_attempts_increase()
        {
            // Arrange
            var succeeded = true;
            var oldSession = _sessionBuilder.Default()
                .Create();

            // Act
            var newSession = newSessionState(oldSession, succeeded, DateTime.UtcNow);

            // Assert
            var oldWords = oldSession.Words.ToList();
            var newWords = newSession.Words.ToList();
            var oldWord = oldWords[oldSession.Idx];
            var newWord = newWords[oldSession.Idx];

            Assert.AreEqual(succeeded, newWord.Succeeded);
            Assert.AreEqual(1, newSession.Idx);
            Assert.AreEqual(oldWord.Attempts + 1, newWord.Attempts);
        }


        [Test]
        public void NewSessionState_should_be_inactive_when_all_words_suceeded()
        {
            // Arrange
            var succeeded = true;
            var session = _sessionBuilder.Default()
                .OneWordLeftToFinish()
                .Create();

            // Act
            var newSession = newSessionState(session, succeeded, DateTime.UtcNow + TimeSpan.FromSeconds(5));

            // Assert
            Assert.False(newSession.IsActive);
            Assert.AreNotEqual(session.ChangeDate, newSession.ChangeDate);
        }       

        [Test]
        public void NewSessionState_changes_ChangeDate()
        {
            // Arrange

            // Act

            // Assert
        }

        [Test]
        public void NewSessionState_changes_CurrentIndex()
        {
            // Arrange

            // Act

            // Assert
        }

        class SessionBuilder
        {
            LearningSession _session;

            public SessionBuilder Default()
            {
                var words = new List<LearningWord>()
                {
                    new LearningWord(ObjectId.GenerateNewId(), "work", new List<Word>()
                    {
                        new Word("работа", 1)
                    }, false, 0),
                    new LearningWord(ObjectId.GenerateNewId(), "building", new List<Word>()
                    {
                        new Word("здание", 1)
                    }, false, 0),
                    new LearningWord(ObjectId.GenerateNewId(), "word1", new List<Word>()
                    {
                        new Word("word1", 1)
                    }, false, 0)
                };

                _session = new LearningSession(ObjectId.GenerateNewId(), "doesn't matter", 0, DateTime.UtcNow, DateTime.UtcNow, true, words);
                return this;
            }

            public SessionBuilder SetLastWordAsCurrent()
            {
                _session = new LearningSession(ObjectId.GenerateNewId(),"doesn't matter", _session.Words.Count() - 1, DateTime.UtcNow, DateTime.UtcNow, true, _session.Words);
                return this;
            }

            public SessionBuilder OneWordLeftToFinish()
            {
                int lastIdx = _session.Words.Count() - 1;
                var words = _session.Words.Select((w, idx) => idx == lastIdx ? w : MarkAsSucceeded(w));
                _session = new LearningSession(ObjectId.GenerateNewId(), "doesn't matter", lastIdx, DateTime.UtcNow, DateTime.UtcNow, true, words);
                return this;

                LearningWord MarkAsSucceeded(LearningWord w) =>
                    new LearningWord(ObjectId.GenerateNewId(), w.Word, w.Trans, true, w.Attempts + 1);
            }

            public LearningSession Create() => _session;
        }
    }
}
