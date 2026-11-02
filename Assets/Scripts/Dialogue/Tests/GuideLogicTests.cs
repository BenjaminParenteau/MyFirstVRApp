using System.Linq;
using NUnit.Framework;

namespace HighStakes.Dialogue.Tests
{
    public class GuideLogicTests
    {
        [Test]
        public void Queue_PlaysLinesInOrderOneAtATime()
        {
            var queue = new GuideQueue();
            Assert.IsTrue(queue.Request("a"));
            Assert.IsTrue(queue.Request("b"));

            Assert.AreEqual("a", queue.StartNext());
            Assert.IsNull(queue.StartNext(), "b must wait until a has finished");
            Assert.AreEqual(1, queue.Waiting);

            queue.Finish();
            Assert.AreEqual("b", queue.StartNext());
            queue.Finish();
            Assert.IsNull(queue.StartNext());
            Assert.IsNull(queue.Current);
        }

        [Test]
        public void Queue_PlaysEachLineOncePerSession()
        {
            var queue = new GuideQueue();
            Assert.IsTrue(queue.Request("first_contact"));
            Assert.IsFalse(queue.Request("first_contact"), "asked again while waiting");
            queue.StartNext();
            queue.Finish();
            Assert.IsFalse(queue.Request("first_contact"), "asked again after it played");
            Assert.IsNull(queue.StartNext());
        }

        [TestCase(null)]
        [TestCase("")]
        public void Queue_IgnoresEmptyIds(string id)
        {
            var queue = new GuideQueue();
            Assert.IsFalse(queue.Request(id));
            Assert.IsNull(queue.StartNext());
        }

        [Test]
        public void LineSeconds_FollowsTheAudioWhenItIsLonger()
        {
            Assert.AreEqual(7.5f, GuideLogic.LineSeconds("Use it.", 7.5f), 1e-4f);
        }

        [Test]
        public void LineSeconds_LeavesTimeToReadALongSubtitle()
        {
            // 27 words at 2.7 words per second
            string subtitle = string.Join(" ", Enumerable.Repeat("word", 27));
            Assert.AreEqual(27, GuideLogic.CountWords(subtitle));
            Assert.AreEqual(10f, GuideLogic.LineSeconds(subtitle, 3f), 1e-4f);
        }

        [TestCase("", 0f)]
        [TestCase("Hi.", 0f)]
        [TestCase(null, 0.5f)]
        public void LineSeconds_NeverShorterThanTheMinimum(string subtitle, float clipSeconds)
        {
            Assert.AreEqual(GuideLogic.MinSeconds, GuideLogic.LineSeconds(subtitle, clipSeconds), 1e-4f);
        }

        [TestCase("Don't turn around.  Keep\tplaying.", 4)]
        [TestCase("   ", 0)]
        public void CountWords_SplitsOnAnyWhitespace(string text, int expected)
        {
            Assert.AreEqual(expected, GuideLogic.CountWords(text));
        }

        [Test]
        public void WaveHeight_StaysInRangeAndBarsDiffer()
        {
            for (float t = 0f; t < 5f; t += 0.037f)
            {
                for (int bar = 0; bar < GuideLogic.WaveBars; bar++)
                {
                    float h = GuideLogic.WaveHeight(bar, t);
                    Assert.That(h, Is.InRange(0.2f, 1f));
                }
            }
            Assert.AreNotEqual(GuideLogic.WaveHeight(0, 1f), GuideLogic.WaveHeight(1, 1f));
        }
    }
}
