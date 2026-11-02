using System.Collections.Generic;
using NUnit.Framework;

namespace HighStakes.UI.Tests
{
    public class CaseFileLogicTests
    {
        // ---------------------------------------------------------------- map

        [Test]
        public void Visit_MarksAMapAreaOnce()
        {
            var visited = new HashSet<string>();

            Assert.IsTrue(MapLogic.Visit(visited, "MVP_Casino"));
            Assert.IsFalse(MapLogic.Visit(visited, "MVP_Casino"), "already visited");
            Assert.AreEqual(1, visited.Count);
        }

        [TestCase("MVP_Main")]
        [TestCase("MVP_UI")]
        [TestCase("")]
        [TestCase(null)]
        public void Visit_IgnoresScenesThatArentOnTheMap(string scene)
        {
            var visited = new HashSet<string>();

            Assert.IsFalse(MapLogic.Visit(visited, scene));
            Assert.AreEqual(0, visited.Count);
        }

        [Test]
        public void LabelFor_ShowsTheNameOnlyOnceVisited()
        {
            var visited = new HashSet<string>();
            int vault = MapLogic.IndexOf("MVP_Vault");

            Assert.AreEqual(MapLogic.Unknown, MapLogic.LabelFor(vault, visited));
            MapLogic.Visit(visited, "MVP_Vault");
            Assert.AreEqual("VAULT", MapLogic.LabelFor(vault, visited));
        }

        [Test]
        public void ShowsPath_NeedsBothRoomsVisited()
        {
            var visited = new HashSet<string>();
            int casinoToVip = 0; // MapLogic.Paths[0]
            MapLogic.Visit(visited, "MVP_Casino");

            Assert.IsFalse(MapLogic.ShowsPath(casinoToVip, visited));
            MapLogic.Visit(visited, "MVP_VIP");
            Assert.IsTrue(MapLogic.ShowsPath(casinoToVip, visited));
        }

        [Test]
        public void EveryPathJoinsTwoDifferentAreas()
        {
            foreach (var path in MapLogic.Paths)
            {
                Assert.AreEqual(2, path.Length);
                Assert.That(path[0], Is.InRange(0, MapLogic.Areas.Length - 1));
                Assert.That(path[1], Is.InRange(0, MapLogic.Areas.Length - 1));
                Assert.AreNotEqual(path[0], path[1]);
            }
        }

        [Test]
        public void AreasFitOnThePageWithoutOverlapping()
        {
            const float halfWidth = 0.058f, halfHeight = 0.046f; // inside the map page's framed box
            var areas = MapLogic.Areas;
            for (int i = 0; i < areas.Length; i++)
            {
                Assert.LessOrEqual(System.Math.Abs(areas[i].U) + areas[i].Width / 2f, halfWidth, areas[i].Label);
                Assert.LessOrEqual(System.Math.Abs(areas[i].V) + areas[i].Height / 2f, halfHeight, areas[i].Label);
                for (int j = i + 1; j < areas.Length; j++)
                {
                    bool apart = System.Math.Abs(areas[i].U - areas[j].U) >= (areas[i].Width + areas[j].Width) / 2f
                              || System.Math.Abs(areas[i].V - areas[j].V) >= (areas[i].Height + areas[j].Height) / 2f;
                    Assert.IsTrue(apart, areas[i].Label + " overlaps " + areas[j].Label);
                }
            }
        }

        // ---------------------------------------------------------------- leads

        [Test]
        public void Add_IgnoresBlankAndRepeatedLeads()
        {
            var leads = new List<string>();

            Assert.IsTrue(LeadsLogic.Add(leads, "  Talk to the Pit Boss. "));
            Assert.IsFalse(LeadsLogic.Add(leads, "talk to the pit boss."), "same lead, different case");
            Assert.IsFalse(LeadsLogic.Add(leads, "   "));
            Assert.IsFalse(LeadsLogic.Add(leads, null));
            Assert.AreEqual(new[] { "Talk to the Pit Boss." }, leads);
        }

        [Test]
        public void NewestFirst_ListsTheCurrentLeadFirst()
        {
            Assert.AreEqual(new[] { 2, 1, 0 }, LeadsLogic.NewestFirst(3, LeadsLogic.Rows));
        }

        [Test]
        public void NewestFirst_DropsTheOldestWhenThePageIsFull()
        {
            Assert.AreEqual(new[] { 6, 5, 4 }, LeadsLogic.NewestFirst(7, 3));
            Assert.AreEqual(new int[0], LeadsLogic.NewestFirst(0, 3));
        }

        [Test]
        public void EveryAreaHasANoteForTheYouAreHereBox()
        {
            foreach (var area in MapLogic.Areas)
                Assert.IsFalse(string.IsNullOrWhiteSpace(area.Note), area.Label);
        }
    }
}
