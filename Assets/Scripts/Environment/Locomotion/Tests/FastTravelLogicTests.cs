using NUnit.Framework;

namespace HighStakes.Environment.Tests
{
    public class FastTravelLogicTests
    {
        static TravelDestination Vault => FastTravelLogic.Destinations[FastTravelLogic.IndexOf("MVP_Vault")];
        static TravelDestination Casino => FastTravelLogic.Destinations[FastTravelLogic.IndexOf("MVP_Casino")];

        // ---------------------------------------------------------------- the unlock rule

        [Test]
        public void AnUnvisitedAreaIsUnknownEvenHoldingTheCard()
        {
            var state = FastTravelLogic.StateOf(Vault, visited: false, hasAccess: true, currentScene: "MVP_Casino");
            Assert.AreEqual(TravelState.Unknown, state);
            Assert.IsFalse(FastTravelLogic.CanTravel(state));
        }

        [Test]
        public void AVisitedAreaWithTheCardIsReady()
        {
            var state = FastTravelLogic.StateOf(Vault, visited: true, hasAccess: true, currentScene: "MVP_Casino");
            Assert.AreEqual(TravelState.Ready, state);
            Assert.IsTrue(FastTravelLogic.CanTravel(state));
        }

        [Test]
        public void AVisitedAreaWithoutTheCardIsLocked()
        {
            var state = FastTravelLogic.StateOf(Vault, visited: true, hasAccess: false, currentScene: "MVP_Casino");
            Assert.AreEqual(TravelState.Locked, state);
            Assert.IsFalse(FastTravelLogic.CanTravel(state));
        }

        [Test]
        public void TheAreaThePlayerIsStandingInIsHereNotReady()
        {
            var state = FastTravelLogic.StateOf(Vault, visited: true, hasAccess: true, currentScene: "MVP_Vault");
            Assert.AreEqual(TravelState.Here, state);
            Assert.IsFalse(FastTravelLogic.CanTravel(state));
        }

        // "Here" is decided before "visited", so the very first area reads sensibly on the watch during the frames
        // between the scene loading and SceneFlow recording the visit.
        [Test]
        public void TheCurrentAreaReadsHereEvenBeforeItIsRecordedAsVisited()
        {
            var state = FastTravelLogic.StateOf(Casino, visited: false, hasAccess: true, currentScene: "MVP_Casino");
            Assert.AreEqual(TravelState.Here, state);
        }

        [Test]
        public void AnAreaWithNoCardNeedsOnlyAVisit()
        {
            Assert.IsNull(Casino.RequiredAccess, "the casino floor is not behind a door");
            var state = FastTravelLogic.StateOf(Casino, visited: true, hasAccess: false, currentScene: "MVP_Vault");
            Assert.AreEqual(TravelState.Ready, state);
        }

        [Test]
        public void EveryRefusalSaysWhyAndATravelSaysNothing()
        {
            Assert.IsNotNull(FastTravelLogic.RefusalFor(TravelState.Unknown));
            Assert.IsNotNull(FastTravelLogic.RefusalFor(TravelState.Locked));
            Assert.IsNotNull(FastTravelLogic.RefusalFor(TravelState.Here));
            Assert.IsNull(FastTravelLogic.RefusalFor(TravelState.Ready));
        }

        // ---------------------------------------------------------------- the destination table

        [Test]
        public void EveryAreaSceneFlowCanLoadIsADestinationExactlyOnce()
        {
            string[] areas =
            {
                "MVP_Casino", "MVP_VIP", "MVP_SecurityHall", "MVP_SecurityOffice", "MVP_CameraRoom", "MVP_Vault",
            };
            Assert.AreEqual(areas.Length, FastTravelLogic.Destinations.Length);
            foreach (string area in areas)
                Assert.GreaterOrEqual(FastTravelLogic.IndexOf(area), 0, $"{area} is not a travel destination");
        }

        [Test]
        public void SomewhereThatIsNotAnAreaIsNotADestination()
        {
            Assert.AreEqual(-1, FastTravelLogic.IndexOf("MVP_Main"));
            Assert.AreEqual(-1, FastTravelLogic.IndexOf(null));
        }

        [Test]
        public void EveryDestinationHasASceneALabelAndAnArrivalDoor()
        {
            foreach (var destination in FastTravelLogic.Destinations)
            {
                Assert.IsFalse(string.IsNullOrEmpty(destination.Scene));
                Assert.IsFalse(string.IsNullOrEmpty(destination.Label));
                Assert.IsFalse(string.IsNullOrEmpty(destination.ArrivalDoor),
                    $"{destination.Scene} has nowhere to land");
                Assert.LessOrEqual(destination.Label.Length, 8, $"{destination.Label} will wrap on the watch");
            }
        }

        // The whole point of fast travel is skipping the corridor, and each wing's hallway and its room are separate
        // islands 40 m apart (HallwayBuilder). Landing at Hall*_Start would drop the player at the top of the hallway
        // and make them walk it anyway, so the back rooms must arrive at their in-room door.
        [Test]
        public void TheBackRoomsLandInsideTheRoomNotAtTheTopOfTheirHallway()
        {
            foreach (string scene in new[] { "MVP_SecurityOffice", "MVP_CameraRoom", "MVP_Vault" })
            {
                var destination = FastTravelLogic.Destinations[FastTravelLogic.IndexOf(scene)];
                StringAssert.EndsWith("_Door", destination.ArrivalDoor);
                StringAssert.StartsWith("Room", destination.ArrivalDoor);
            }
        }

        // Fast travel must never open a door walking would not: everything behind the STAFF ONLY door asks for the
        // staff card, the lounge asks for the VIP card, and only the open floor asks for nothing.
        [Test]
        public void EveryAreaBehindADoorKeepsItsCardRequirement()
        {
            Assert.AreEqual(FastTravelLogic.VipAccess, Destination("MVP_VIP").RequiredAccess);
            foreach (string scene in new[] { "MVP_SecurityHall", "MVP_SecurityOffice", "MVP_CameraRoom", "MVP_Vault" })
                Assert.AreEqual(FastTravelLogic.StaffAccess, Destination(scene).RequiredAccess,
                    $"{scene} is behind the STAFF ONLY door");
        }

        static TravelDestination Destination(string scene) =>
            FastTravelLogic.Destinations[FastTravelLogic.IndexOf(scene)];
    }
}
