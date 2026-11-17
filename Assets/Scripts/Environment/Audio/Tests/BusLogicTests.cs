using NUnit.Framework;

namespace HighStakes.Environment.Audio.Tests
{
    public class BusLogicTests
    {
        [Test]
        public void EveryBusHasItsOwnStableKeyAndName()
        {
            var keys = new System.Collections.Generic.HashSet<string>();
            foreach (SoundBus bus in System.Enum.GetValues(typeof(SoundBus)))
            {
                Assert.IsTrue(keys.Add(BusLogic.Key(bus)), $"{bus} shares a PlayerPrefs key with another bus");
                Assert.IsFalse(BusLogic.Name(bus).StartsWith("bus"), $"{bus} has no readable name");
            }
            Assert.AreEqual(BusLogic.BusCount, keys.Count, "BusCount must match the enum");
        }

        [Test]
        public void KeysAreNamedNotNumbered()
        {
            // The key must not contain the enum's integer, or reordering the enum would move players' saved
            // settings onto the wrong bus.
            Assert.AreEqual("hs.audio.vol.music", BusLogic.Key(SoundBus.Music));
            Assert.AreEqual("hs.audio.vol.voice", BusLogic.Key(SoundBus.Voice));
        }

        [Test]
        public void DefaultsAreAudibleAndInRange()
        {
            foreach (SoundBus bus in System.Enum.GetValues(typeof(SoundBus)))
            {
                float d = BusLogic.Default(bus);
                Assert.Greater(d, 0f, $"{bus} ships silent");
                Assert.LessOrEqual(d, 1f, $"{bus} ships above full scale");
            }
            Assert.Less(BusLogic.Default(SoundBus.Music), BusLogic.Default(SoundBus.Ambience),
                "music is meant to sit under the room tone");
        }

        [Test]
        public void GainMultipliesAndNeverExceedsFullScale()
        {
            Assert.AreEqual(0.5f, BusLogic.Gain(1f, 0.5f), 1e-5f);
            Assert.AreEqual(0.25f, BusLogic.Gain(0.5f, 0.5f), 1e-5f);
            Assert.AreEqual(0f, BusLogic.Gain(0f, 1f), 1e-5f);
            Assert.AreEqual(1f, BusLogic.Gain(1f, 4f), 1e-5f, "a mis-set cue must not clip the mix");
            Assert.AreEqual(0f, BusLogic.Gain(1f, -1f), 1e-5f, "a negative cue volume must not invert phase");
        }

        [Test]
        public void BlackoutDropsMusicAndPromotesTheRoomButNeverTouchesVoice()
        {
            Assert.Less(BusLogic.BlackoutDuck(SoundBus.Music), 0.5f, "dropping the music is most of the effect");
            Assert.Greater(BusLogic.BlackoutDuck(SoundBus.Ambience), 1f, "in the dark the room tone is the map");
            Assert.AreEqual(1f, BusLogic.BlackoutDuck(SoundBus.Voice), 1e-5f,
                "the Guide is the only interface left — never duck him");
        }

        [Test]
        public void BlendIsSmoothAndHitsBothEnds()
        {
            Assert.AreEqual(0.2f, BusLogic.Blend(0.2f, 0.9f, 0f), 1e-5f);
            Assert.AreEqual(0.9f, BusLogic.Blend(0.2f, 0.9f, 1f), 1e-5f);
            Assert.AreEqual(0.55f, BusLogic.Blend(0.2f, 0.9f, 0.5f), 1e-5f);
            Assert.AreEqual(0.2f, BusLogic.Blend(0.2f, 0.9f, -5f), 1e-5f, "t must clamp");
            Assert.AreEqual(0.9f, BusLogic.Blend(0.2f, 0.9f, 5f), 1e-5f, "t must clamp");
        }

        [Test]
        public void DecibelsAreReadableAndSilenceIsFloored()
        {
            Assert.AreEqual(0f, BusLogic.ToDecibels(1f), 0.01f);
            Assert.AreEqual(-6f, BusLogic.ToDecibels(0.5f), 0.1f);
            Assert.AreEqual(-80f, BusLogic.ToDecibels(0f), 0.01f, "must not return -infinity into a log line");
        }
    }
}
