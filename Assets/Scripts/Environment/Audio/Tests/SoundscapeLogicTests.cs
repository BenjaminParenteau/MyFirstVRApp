using NUnit.Framework;

namespace HighStakes.Environment.Audio.Tests
{
    public class SoundscapeLogicTests
    {
        static readonly string[] Areas =
        {
            "MVP_Casino", "MVP_VIP", "MVP_SecurityHall", "MVP_SecurityOffice", "MVP_CameraRoom", "MVP_Vault",
        };

        [Test]
        public void EveryAreaHasAMusicBedAndARoomTone()
        {
            foreach (var area in Areas)
            {
                var beds = SoundscapeLogic.BedsFor(area);
                Assert.IsNotEmpty(beds.Music ?? "", $"{area} has no music bed");
                Assert.IsNotEmpty(beds.Ambience ?? "", $"{area} has no room tone");
                Assert.IsTrue(SoundscapeLogic.IsKnownArea(area), $"{area} is not listed as known");
            }
        }

        [Test]
        public void BedsUseSlotDsNamespaces()
        {
            foreach (var area in Areas)
            {
                var beds = SoundscapeLogic.BedsFor(area);
                Assert.AreEqual("D", SoundscapeLogic.OwnerOf(beds.Music), $"{area} music bed is not Slot D's");
                Assert.AreEqual("D", SoundscapeLogic.OwnerOf(beds.Ambience), $"{area} room tone is not Slot D's");
            }
        }

        [Test]
        public void TheBackOfHouseSoundsDifferentFromTheFloor()
        {
            // The premise the whole blackout mode rests on: you can tell where you are by ear.
            Assert.AreNotEqual(SoundscapeLogic.BedsFor("MVP_Casino").Ambience,
                SoundscapeLogic.BedsFor("MVP_SecurityOffice").Ambience);
            Assert.AreNotEqual(SoundscapeLogic.BedsFor("MVP_Casino").Music,
                SoundscapeLogic.BedsFor("MVP_Vault").Music);
            Assert.AreNotEqual(SoundscapeLogic.BedsFor("MVP_Vault").Ambience,
                SoundscapeLogic.BedsFor("MVP_SecurityHall").Ambience);
        }

        [Test]
        public void AnUnwrittenSceneFallsBackRatherThanGoingSilent()
        {
            var beds = SoundscapeLogic.BedsFor("MVP_SomeNewRoom");
            Assert.IsNotEmpty(beds.Music ?? "", "a new area must not be silent by accident");
            Assert.IsFalse(SoundscapeLogic.IsKnownArea("MVP_SomeNewRoom"), "but it must still be reported");
        }

        [Test]
        public void NoSceneNameAtAllIsSilence()
        {
            Assert.IsNull(SoundscapeLogic.BedsFor(null).Music);
            Assert.IsNull(SoundscapeLogic.BedsFor("").Ambience);
        }

        [Test]
        public void CrossfadeKeepsConstantPower()
        {
            // Linear gain on both beds at once dips in the middle and the player hears a hole walking through a door.
            for (float t = 0f; t <= 1.0001f; t += 0.1f)
            {
                SoundscapeLogic.Crossfade(t, 1f, out float a, out float b);
                Assert.AreEqual(1f, a * a + b * b, 1e-4f, $"power dips at t={t:0.0}");
            }
        }

        [Test]
        public void CrossfadeEndsFullyOnTheNewBed()
        {
            SoundscapeLogic.Crossfade(0f, 2f, out float a0, out float b0);
            Assert.AreEqual(1f, a0, 1e-4f);
            Assert.AreEqual(0f, b0, 1e-4f);

            SoundscapeLogic.Crossfade(2f, 2f, out float a1, out float b1);
            Assert.AreEqual(0f, a1, 1e-4f);
            Assert.AreEqual(1f, b1, 1e-4f);

            SoundscapeLogic.Crossfade(5f, 0f, out _, out float bInstant);
            Assert.AreEqual(1f, bInstant, 1e-4f, "a zero-length fade must land on the new bed, not divide by zero");
        }

        [Test]
        public void VariantNeverRepeatsTheSameClipTwiceRunning()
        {
            for (int count = 2; count <= 5; count++)
            {
                for (int last = 0; last < count; last++)
                {
                    for (float r = 0f; r <= 1.0001f; r += 0.05f)
                    {
                        int pick = SoundscapeLogic.PickVariant(count, last, r);
                        Assert.AreNotEqual(last, pick, $"repeated clip {last} of {count} at roll {r:0.00}");
                        Assert.GreaterOrEqual(pick, 0);
                        Assert.Less(pick, count);
                    }
                }
            }
        }

        [Test]
        public void VariantReachesEveryClip()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (float r = 0f; r <= 1.0001f; r += 0.01f)
                seen.Add(SoundscapeLogic.PickVariant(4, 2, r));
            Assert.AreEqual(3, seen.Count, "all clips except the last-played one should be reachable");
            Assert.IsFalse(seen.Contains(2));
        }

        [Test]
        public void VariantHandlesDegenerateCues()
        {
            Assert.AreEqual(0, SoundscapeLogic.PickVariant(1, -1, 0.5f), "one clip always picks it");
            Assert.AreEqual(0, SoundscapeLogic.PickVariant(1, 0, 0.5f), "one clip repeats, there is no choice");
            Assert.AreEqual(-1, SoundscapeLogic.PickVariant(0, -1, 0.5f), "no clips means nothing to play");
            Assert.AreEqual(1, SoundscapeLogic.PickVariant(2, 7, 0.9f), "an out-of-range 'last' must not crash");
        }

        [Test]
        public void PitchJitterIsSymmetricAndCentred()
        {
            Assert.AreEqual(1f, SoundscapeLogic.PitchFor(0f, 0.9f), 1e-5f, "no jitter means no shift");
            Assert.AreEqual(1f, SoundscapeLogic.PitchFor(2f, 0.5f), 1e-5f, "a centre roll means no shift");
            Assert.Less(SoundscapeLogic.PitchFor(2f, 0f), 1f);
            Assert.Greater(SoundscapeLogic.PitchFor(2f, 1f), 1f);
            // ±2 semitones is about 0.89..1.12, so nothing should ever be wildly off speed.
            Assert.AreEqual(1.122f, SoundscapeLogic.PitchFor(2f, 1f), 0.01f);
        }

        [Test]
        public void VoicePoolPrefersFreeThenStealsTheOldest()
        {
            Assert.AreEqual(1, SoundscapeLogic.PickVoice(
                new[] { true, false, true }, new[] { 1f, 2f, 3f }), "should take the free voice");

            Assert.AreEqual(2, SoundscapeLogic.PickVoice(
                new[] { true, true, true }, new[] { 9f, 8f, 1f }), "all busy: steal the one that started longest ago");

            Assert.AreEqual(-1, SoundscapeLogic.PickVoice(new bool[0], new float[0]), "empty pool");
            Assert.AreEqual(-1, SoundscapeLogic.PickVoice(null, null), "null pool must not crash");
        }

        [Test]
        public void EveryNamespaceMapsToExactlyOneSlot()
        {
            Assert.AreEqual("A", SoundscapeLogic.OwnerOf("table.chip.clink"));
            Assert.AreEqual("A", SoundscapeLogic.OwnerOf("cage.bell"));
            Assert.AreEqual("B", SoundscapeLogic.OwnerOf("sec.reader.ok"));
            Assert.AreEqual("B", SoundscapeLogic.OwnerOf("item.pickup"));
            Assert.AreEqual("C", SoundscapeLogic.OwnerOf("ui.press"));
            Assert.AreEqual("C", SoundscapeLogic.OwnerOf("vo.guide.first_contact"));
            Assert.AreEqual("D", SoundscapeLogic.OwnerOf("env.step.carpet"));
            Assert.AreEqual("D", SoundscapeLogic.OwnerOf("mus.floor"));
            Assert.AreEqual("D", SoundscapeLogic.OwnerOf("amb.vault"));
            Assert.AreEqual("D", SoundscapeLogic.OwnerOf("dark.sonar.ping"));
        }

        [Test]
        public void AnIdInNobodysNamespaceIsReported()
        {
            Assert.IsNull(SoundscapeLogic.OwnerOf("chip.clink"), "no prefix: must be flagged, not guessed");
            Assert.IsNull(SoundscapeLogic.OwnerOf("random.thing"));
            Assert.IsNull(SoundscapeLogic.OwnerOf(""));
            Assert.IsNull(SoundscapeLogic.OwnerOf(null));
        }

        [Test]
        public void InterfaceAndEarpieceAre2DButTheWorldIs3D()
        {
            Assert.IsFalse(SoundscapeLogic.ShouldBeSpatial("ui.press"), "UI belongs to the player, not the room");
            Assert.IsFalse(SoundscapeLogic.ShouldBeSpatial("vo.guide.tier_1"),
                "an earpiece must not pan when you turn your head");
            Assert.IsFalse(SoundscapeLogic.ShouldBeSpatial("mus.floor"), "music beds are not positioned");

            Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial("vo.pitboss.greeting"),
                "the Pit Boss has a body — you should hear which way he is");
            Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial("table.chip.bet"));
            Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial("sec.reader.ok"));
            Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial("amb.vault"));
            Assert.IsTrue(SoundscapeLogic.ShouldBeSpatial("dark.sonar.return.hard"));
        }
    }
}
