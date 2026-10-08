using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class CountUpTests
    {
        [Test]
        public void StartsAtZeroAndLandsExactlyOnTheWin()
        {
            Assert.AreEqual(0, CountUp.Value(150, 0f, 1.2f));
            Assert.AreEqual(150, CountUp.Value(150, 1.2f, 1.2f));
            Assert.AreEqual(150, CountUp.Value(150, 5f, 1.2f));
        }

        [Test]
        public void ClimbsWithoutGoingBackOrPastTheWin()
        {
            long last = 0;
            for (var t = 0f; t <= 1.2f; t += 0.01f)
            {
                var v = CountUp.Value(1_000_000, t, 1.2f);
                Assert.GreaterOrEqual(v, last);
                Assert.LessOrEqual(v, 1_000_000);
                last = v;
            }
        }

        [Test]
        public void EasesOutSoMostOfTheWinShowsEarly()
        {
            Assert.Greater(CountUp.Value(100, 0.6f, 1.2f), 50);
        }
    }
}
