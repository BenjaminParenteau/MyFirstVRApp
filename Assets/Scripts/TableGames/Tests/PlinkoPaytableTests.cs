using System;
using System.Collections.Generic;
using System.Linq;
using HighStakes.TableGames.Plinko;
using NUnit.Framework;

namespace HighStakes.TableGames.Tests
{
    public class PlinkoPaytableTests
    {
        static readonly PlinkoRisk[] Risks = (PlinkoRisk[])Enum.GetValues(typeof(PlinkoRisk));

        static IEnumerable<(int, PlinkoRisk, long[])> Grid(int rows, long[] buckets) =>
            Risks.Select(risk => (rows, risk, buckets));

        static long[] Flat(int rows, long multiplier) => Enumerable.Repeat(multiplier, rows + 1).ToArray();

        [Test]
        public void AFlatBoardReturnsItsMultiplier()
        {
            var table = new PlinkoPaytable(Grid(8, Flat(8, 100)));
            Assert.AreEqual(1.0, table.ExpectedReturn(8, PlinkoRisk.Low), 1e-12);
        }

        /// <summary>Only the two edge buckets pay, each 1 in 256 on 8 rows.</summary>
        [Test]
        public void ExpectedReturnWeighsEachBucketByItsBinomialChance()
        {
            var edges = new long[] { 12800, 0, 0, 0, 0, 0, 0, 0, 12800 };
            var table = new PlinkoPaytable(Grid(8, edges));
            Assert.AreEqual(2 * 128.0 / 256, table.ExpectedReturn(8, PlinkoRisk.High), 1e-12);
        }

        [Test]
        public void RowsOnOfferAreSortedAndDistinct()
        {
            var table = new PlinkoPaytable(Grid(16, Flat(16, 100)).Concat(Grid(8, Flat(8, 100))));
            CollectionAssert.AreEqual(new[] { 8, 16 }, table.Rows);
        }

        [Test]
        public void ABoardHandedOutCannotChangeTheTable()
        {
            var table = new PlinkoPaytable(Grid(8, Flat(8, 100)));
            table.For(8, PlinkoRisk.Low)[0] = 999999;
            Assert.AreEqual(100, table.For(8, PlinkoRisk.Low)[0]);
        }

        [Test]
        public void ABadBoardIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(8, Flat(7, 100))), "wrong bucket count");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(8, new long[] { 1, 2, 3, 4, 5, 4, 3, 2, 9 })), "not symmetric");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(8, new long[] { -1, 2, 3, 4, 5, 4, 3, 2, -1 })), "negative");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(7, Flat(7, 100))), "too few rows");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(17, Flat(17, 100))), "too many rows");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(8, null)), "no buckets");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Grid(8, Flat(8, 100)).Concat(Grid(8, Flat(8, 100)))), "defined twice");
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(Array.Empty<(int, PlinkoRisk, long[])>()), "empty");
        }

        [Test]
        public void EveryRowCountMustComeInEveryRisk()
        {
            var lowOnly = new[] { (8, PlinkoRisk.Low, Flat(8, 100)) };
            Assert.Throws<ArgumentException>(() => new PlinkoPaytable(lowOnly));
        }

        [Test]
        public void AskingForABoardThatIsNotOfferedThrows()
        {
            var table = new PlinkoPaytable(Grid(8, Flat(8, 100)));
            Assert.IsFalse(table.Offers(12, PlinkoRisk.Low));
            Assert.Throws<ArgumentOutOfRangeException>(() => table.For(12, PlinkoRisk.Low));
        }

        /// <summary>
        /// Computed exactly from each shipped board, which is the only way to prove the high risk ones: their rare top
        /// buckets swing too hard for a session to pin down closely.
        /// </summary>
        [Test]
        public void EveryShippedBoardReturnsJustUnderTheHouseEdge()
        {
            var paytable = PlinkoPaytable.Standard();
            CollectionAssert.AreEqual(new[] { 8, 12, 16 }, paytable.Rows);
            foreach (var rows in paytable.Rows)
            foreach (var risk in Risks)
                Assert.That(paytable.ExpectedReturn(rows, risk), Is.InRange(0.98, 0.99),
                    $"{rows} rows {risk}: a board must give the player 98 to 99 percent back");
        }
    }
}
