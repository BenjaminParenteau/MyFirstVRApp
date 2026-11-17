using HighStakes.Environment.Audio;
using NUnit.Framework;

namespace HighStakes.Environment.Blackout.Tests
{
    public class BlackoutLogicTests
    {
        // ---------------------------------------------------------------- stage machine

        [Test]
        public void TheLightsGoOutAndComeBackInOrder()
        {
            var s = BlackoutStage.Off;
            s = BlackoutLogic.Advance(s, true, 0f);
            Assert.AreEqual(BlackoutStage.PowerDown, s);

            s = BlackoutLogic.Advance(s, true, BlackoutLogic.PowerDownSeconds);
            Assert.AreEqual(BlackoutStage.Dark, s);

            s = BlackoutLogic.Advance(s, false, 0f);
            Assert.AreEqual(BlackoutStage.PowerUp, s);

            s = BlackoutLogic.Advance(s, false, BlackoutLogic.PowerUpSeconds);
            Assert.AreEqual(BlackoutStage.Off, s);
        }

        [Test]
        public void StagesDoNotEndEarly()
        {
            Assert.AreEqual(BlackoutStage.PowerDown,
                BlackoutLogic.Advance(BlackoutStage.PowerDown, true, BlackoutLogic.PowerDownSeconds - 0.01f));
            Assert.AreEqual(BlackoutStage.PowerUp,
                BlackoutLogic.Advance(BlackoutStage.PowerUp, false, BlackoutLogic.PowerUpSeconds - 0.01f));
        }

        [Test]
        public void DarkHoldsAsLongAsSomethingWantsIt()
        {
            Assert.AreEqual(BlackoutStage.Dark, BlackoutLogic.Advance(BlackoutStage.Dark, true, 999f),
                "the dark must not time out on its own — something has to release it");
        }

        [Test]
        public void CancellingMidFadeRecoversRatherThanSticking()
        {
            // A player who leaves the area (or a bug) must never be able to strand the game in a half-dark state.
            Assert.AreEqual(BlackoutStage.PowerUp, BlackoutLogic.Advance(BlackoutStage.PowerDown, false, 0.3f));
            Assert.AreEqual(BlackoutStage.PowerDown, BlackoutLogic.Advance(BlackoutStage.PowerUp, true, 0.3f));
        }

        [Test]
        public void PowerComesBackSlowerThanItLeaves()
        {
            // Going dark is a breaker being thrown; coming back must not startle someone wearing a headset.
            Assert.Greater(BlackoutLogic.PowerUpSeconds, BlackoutLogic.PowerDownSeconds);
        }

        // ---------------------------------------------------------------- darkness ramp

        [Test]
        public void DarknessRunsZeroToOneAndBack()
        {
            Assert.AreEqual(0f, BlackoutLogic.Darkness(BlackoutStage.Off, 0f), 1e-4f);
            Assert.AreEqual(1f, BlackoutLogic.Darkness(BlackoutStage.Dark, 0f), 1e-4f);

            Assert.AreEqual(0f, BlackoutLogic.Darkness(BlackoutStage.PowerDown, 0f), 1e-3f);
            Assert.AreEqual(1f, BlackoutLogic.Darkness(BlackoutStage.PowerDown, BlackoutLogic.PowerDownSeconds), 1e-3f);

            Assert.AreEqual(1f, BlackoutLogic.Darkness(BlackoutStage.PowerUp, 0f), 1e-3f);
            Assert.AreEqual(0f, BlackoutLogic.Darkness(BlackoutStage.PowerUp, BlackoutLogic.PowerUpSeconds), 1e-3f);
        }

        [Test]
        public void DarknessIsMonotonicAndNeverLeavesRange()
        {
            float prev = -1f;
            for (float t = 0f; t <= BlackoutLogic.PowerDownSeconds + 0.5f; t += 0.05f)
            {
                float d = BlackoutLogic.Darkness(BlackoutStage.PowerDown, t);
                Assert.GreaterOrEqual(d, 0f);
                Assert.LessOrEqual(d, 1f);
                Assert.GreaterOrEqual(d, prev - 1e-5f, $"darkness went backwards at t={t:0.00}");
                prev = d;
            }
        }

        [Test]
        public void TheDarkMixAppliesWhileTheLightsAreStillDying()
        {
            // The mix has to lead the picture slightly, or the player hears the casino music over a black screen.
            Assert.IsTrue(BlackoutLogic.IsDarkEnoughToNeedSound(BlackoutStage.PowerDown));
            Assert.IsTrue(BlackoutLogic.IsDarkEnoughToNeedSound(BlackoutStage.Dark));
            Assert.IsFalse(BlackoutLogic.IsDarkEnoughToNeedSound(BlackoutStage.Off));
            Assert.IsFalse(BlackoutLogic.IsDarkEnoughToNeedSound(BlackoutStage.PowerUp));
        }

        // ---------------------------------------------------------------- echolocation

        [Test]
        public void EchoDelayUsesTheRealSpeedOfSoundOutAndBack()
        {
            // 17.15 m out and back at 343 m/s is 0.1 s.
            Assert.AreEqual(0.1f, BlackoutLogic.ReturnDelay(17.15f), 1e-3f);
            Assert.AreEqual(0.2f, BlackoutLogic.ReturnDelay(34.3f), 1e-3f);
        }

        [Test]
        public void AVeryCloseEchoIsStillSeparateFromTheClick()
        {
            // Physically a 0.2 m return is ~1 ms, which would be inaudible as a distinct echo.
            Assert.GreaterOrEqual(BlackoutLogic.ReturnDelay(0.2f), 0.04f);
            Assert.GreaterOrEqual(BlackoutLogic.ReturnDelay(0f), 0.04f);
            Assert.GreaterOrEqual(BlackoutLogic.ReturnDelay(-5f), 0.04f, "negative distance must not invert time");
        }

        [Test]
        public void EchoDelayGrowsWithDistance()
        {
            float prev = 0f;
            for (float d = 0f; d < 30f; d += 1f)
            {
                float delay = BlackoutLogic.ReturnDelay(d);
                Assert.GreaterOrEqual(delay, prev - 1e-6f);
                prev = delay;
            }
        }

        [Test]
        public void HardSurfacesComeBackLouderThanSoftOnes()
        {
            float hard = BlackoutLogic.ReturnVolume(5f, 20f, SurfaceHardness.Hard);
            float medium = BlackoutLogic.ReturnVolume(5f, 20f, SurfaceHardness.Medium);
            float soft = BlackoutLogic.ReturnVolume(5f, 20f, SurfaceHardness.Soft);
            Assert.Greater(hard, medium);
            Assert.Greater(medium, soft);
            Assert.Greater(soft, 0f, "even carpet must answer, or a carpeted room reads as a void");
        }

        [Test]
        public void EchoesGoSilentBeyondRange()
        {
            Assert.AreEqual(0f, BlackoutLogic.ReturnVolume(20f, 20f, SurfaceHardness.Hard), 1e-5f);
            Assert.AreEqual(0f, BlackoutLogic.ReturnVolume(50f, 20f, SurfaceHardness.Hard), 1e-5f);
            Assert.AreEqual(0f, BlackoutLogic.ReturnVolume(1f, 0f, SurfaceHardness.Hard), 1e-5f, "zero range");
        }

        [Test]
        public void EchoVolumeAndPitchBothFallWithDistance()
        {
            Assert.Greater(BlackoutLogic.ReturnVolume(2f, 20f, SurfaceHardness.Hard),
                           BlackoutLogic.ReturnVolume(10f, 20f, SurfaceHardness.Hard));
            Assert.Greater(BlackoutLogic.ReturnPitch(1f, 20f), BlackoutLogic.ReturnPitch(18f, 20f));
            // Keep pitch sane: no chipmunks, no sub-bass.
            for (float d = 0f; d <= 20f; d += 1f)
            {
                float p = BlackoutLogic.ReturnPitch(d, 20f);
                Assert.Greater(p, 0.5f);
                Assert.Less(p, 2f);
            }
        }

        // ---------------------------------------------------------------- proximity

        [Test]
        public void ProximityRisesAsYouCloseIn()
        {
            Assert.AreEqual(1f, BlackoutLogic.ProximityVolume(0.2f, 0.4f, 1.5f), 1e-4f, "at or inside near = full");
            Assert.AreEqual(1f, BlackoutLogic.ProximityVolume(0.4f, 0.4f, 1.5f), 1e-4f);
            Assert.AreEqual(0f, BlackoutLogic.ProximityVolume(1.5f, 0.4f, 1.5f), 1e-4f, "at far = silent");
            Assert.AreEqual(0f, BlackoutLogic.ProximityVolume(9f, 0.4f, 1.5f), 1e-4f);

            float mid = BlackoutLogic.ProximityVolume(0.95f, 0.4f, 1.5f);
            Assert.Greater(mid, 0.2f);
            Assert.Less(mid, 0.8f);
        }

        [Test]
        public void ProximityHandlesADegenerateRange()
        {
            Assert.AreEqual(0f, BlackoutLogic.ProximityVolume(1f, 1.5f, 1.5f), 1e-5f);
            Assert.AreEqual(0f, BlackoutLogic.ProximityVolume(1f, 2f, 1f), 1e-5f, "far below near must not invert");
        }

        [Test]
        public void ProximityPitchStaysInformationalNotAlarming()
        {
            for (float d = 0f; d <= 2f; d += 0.1f)
            {
                float p = BlackoutLogic.ProximityPitch(d, 0.4f, 1.5f);
                Assert.GreaterOrEqual(p, 0.8f);
                Assert.LessOrEqual(p, 1.5f);
            }
            Assert.Greater(BlackoutLogic.ProximityPitch(0.4f, 0.4f, 1.5f),
                           BlackoutLogic.ProximityPitch(1.4f, 0.4f, 1.5f));
        }

        // ---------------------------------------------------------------- orientation

        [Test]
        public void BearingPansTheRightWayAndSaturates()
        {
            Assert.AreEqual(0f, BlackoutLogic.BearingPan(0f), 1e-4f, "dead ahead is centred");
            Assert.Less(BlackoutLogic.BearingPan(-45f), 0f, "left of forward pans left");
            Assert.Greater(BlackoutLogic.BearingPan(45f), 0f, "right of forward pans right");
            Assert.AreEqual(-1f, BlackoutLogic.BearingPan(-90f), 1e-4f);
            Assert.AreEqual(1f, BlackoutLogic.BearingPan(90f), 1e-4f);
            Assert.AreEqual(1f, BlackoutLogic.BearingPan(140f), 1e-4f, "must saturate, not wrap back");
            Assert.AreEqual(-1f, BlackoutLogic.BearingPan(-140f), 1e-4f);
        }

        [Test]
        public void BearingPanIsUnaffectedByHowTheAngleWasMeasured()
        {
            Assert.AreEqual(BlackoutLogic.BearingPan(45f), BlackoutLogic.BearingPan(405f), 1e-4f);
            Assert.AreEqual(BlackoutLogic.BearingPan(-45f), BlackoutLogic.BearingPan(315f), 1e-4f);
        }

        [Test]
        public void TheBeaconTicksFasterAsYouTurnTowardsIt()
        {
            float facing = BlackoutLogic.BearingTickRate(0f);
            float side = BlackoutLogic.BearingTickRate(90f);
            float behind = BlackoutLogic.BearingTickRate(180f);
            Assert.Greater(facing, side);
            Assert.Greater(side, behind);
            Assert.Greater(behind, 0f, "it must keep ticking even facing away, or the player thinks it stopped");
        }

        [Test]
        public void FacingToleranceIsSymmetric()
        {
            Assert.IsTrue(BlackoutLogic.IsFacing(0f));
            Assert.IsTrue(BlackoutLogic.IsFacing(20f));
            Assert.IsTrue(BlackoutLogic.IsFacing(-20f));
            Assert.IsFalse(BlackoutLogic.IsFacing(40f));
            Assert.IsFalse(BlackoutLogic.IsFacing(-40f));
            Assert.IsFalse(BlackoutLogic.IsFacing(180f));
        }

        // ---------------------------------------------------------------- the run

        [Test]
        public void TheGuideWaitsALongTimeBeforeTheFirstNudgeThenEscalates()
        {
            // Being lost IS the mode. Rescuing the player at 5 seconds would delete the whole experience.
            Assert.GreaterOrEqual(BlackoutLogic.NudgeAfterSeconds(0), 25f);
            Assert.Greater(BlackoutLogic.NudgeAfterSeconds(0), BlackoutLogic.NudgeAfterSeconds(1));
            Assert.Greater(BlackoutLogic.NudgeAfterSeconds(1), BlackoutLogic.NudgeAfterSeconds(2));
            Assert.Greater(BlackoutLogic.NudgeAfterSeconds(9), 0f, "must never reach zero and spam");
        }

        [Test]
        public void ScoreRewardsSpeedAndPenalisesBumpsAndPingSpam()
        {
            int fast = BlackoutLogic.Score(40f, 0, 3);
            int slow = BlackoutLogic.Score(120f, 0, 3);
            int bumpy = BlackoutLogic.Score(40f, 5, 3);
            int spammy = BlackoutLogic.Score(40f, 0, 40);
            Assert.Greater(fast, slow);
            Assert.Greater(fast, bumpy);
            Assert.Greater(fast, spammy, "spamming the ping into a solid picture must not be optimal");
        }

        [Test]
        public void ScoreNeverGoesNegative()
        {
            Assert.AreEqual(0, BlackoutLogic.Score(9999f, 99, 99));
            Assert.GreaterOrEqual(BlackoutLogic.Score(0f, 0, 0), 0);
        }

        [Test]
        public void RanksCoverEveryScore()
        {
            Assert.AreEqual("GHOST", BlackoutLogic.Rank(900));
            Assert.AreEqual("PROFESSIONAL", BlackoutLogic.Rank(700));
            Assert.AreEqual("LUCKY", BlackoutLogic.Rank(500));
            Assert.AreEqual("HEARD", BlackoutLogic.Rank(100));
            Assert.AreEqual("CAUGHT", BlackoutLogic.Rank(0));
        }

        // ---------------------------------------------------------------- shared surface logic

        [Test]
        public void SurfacesInThisProjectAreRecognisedByName()
        {
            // Real names from the builders in this repo.
            Assert.AreEqual(SurfaceKind.Carpet, SurfaceLogic.KindOf("Casino_Carpet"));
            Assert.AreEqual(SurfaceKind.Carpet, SurfaceLogic.KindOf("M_FeltGreen"));
            Assert.AreEqual(SurfaceKind.Carpet, SurfaceLogic.KindOf("T_Leather"));
            Assert.AreEqual(SurfaceKind.Wood, SurfaceLogic.KindOf("Casino_SideWall_WoodPanel"));
            Assert.AreEqual(SurfaceKind.Metal, SurfaceLogic.KindOf("Vault_TreadPlate"));
            Assert.AreEqual(SurfaceKind.Metal, SurfaceLogic.KindOf("M_BrassGold"));
            Assert.AreEqual(SurfaceKind.Tile, SurfaceLogic.KindOf("Env_CasinoFloor_Marble"));
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceLogic.KindOf("Security_CinderBlock"));
        }

        [Test]
        public void AnUnknownSurfaceStillMakesAFootstep()
        {
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceLogic.KindOf("Thing_42"));
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceLogic.KindOf(""));
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceLogic.KindOf(null));
            Assert.IsNotEmpty(SurfaceLogic.FootstepCue(SurfaceLogic.KindOf(null)));
        }

        [Test]
        public void EveryFootstepCueIsSlotDsAndExists()
        {
            foreach (SurfaceKind kind in System.Enum.GetValues(typeof(SurfaceKind)))
            {
                string cue = SurfaceLogic.FootstepCue(kind);
                Assert.AreEqual("D", SoundscapeLogic.OwnerOf(cue), $"{kind} footstep cue is not Slot D's");
                Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial(cue), $"{kind} footstep must be positioned");
            }
        }

        [Test]
        public void CarpetSwallowsAPingAndConcreteRings()
        {
            Assert.AreEqual(SurfaceHardness.Soft, SurfaceLogic.HardnessOf(SurfaceKind.Carpet));
            Assert.AreEqual(SurfaceHardness.Medium, SurfaceLogic.HardnessOf(SurfaceKind.Wood));
            Assert.AreEqual(SurfaceHardness.Hard, SurfaceLogic.HardnessOf(SurfaceKind.Concrete));
            Assert.AreEqual(SurfaceHardness.Hard, SurfaceLogic.HardnessOf(SurfaceKind.Metal));

            Assert.AreEqual("dark.sonar.return.soft", SurfaceLogic.SonarReturnCueFor("Casino_Carpet"));
            Assert.AreEqual("dark.sonar.return.hard", SurfaceLogic.SonarReturnCueFor("Security_Concrete"));
            Assert.AreEqual("D", SoundscapeLogic.OwnerOf(SurfaceLogic.SonarReturnCueFor("anything")));
        }
    }
}
