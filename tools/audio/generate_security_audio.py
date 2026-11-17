"""Render Slot B's mono 48 kHz security cues. Install numpy before running.

Install eSpeak NG for speech regeneration, or pass --voice-dir with mono PCM
files named 0.wav through 9.wav and radio.wav. Without either, existing spoken
cues are retained and only the synthesized cues are regenerated.
"""

import argparse
import shutil
import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np

from synth import (
    SR, bandpass, buzz, chime, click, env_ad, fade, hum, loopable, metallic,
    mix, noise_burst, paper, place, save_wav, silence, sweep, thump, tone,
)

FOLDER = "Assets/Content/Progression/Audio"
# C-major pentatonic from C5 to A6. Zero is the top note, after 1–9.
DIGIT_NOTES = (93, 72, 74, 76, 79, 81, 84, 86, 88, 91)
DIGIT_WORDS = "zero one two three four five six seven eight nine".split()


def beep(freq, duration=0.12, brightness=0.14):
    return env_ad(tone(freq, duration) + brightness * tone(2 * freq, duration), 0.008, duration - 0.008)


def interval(first, second, gap=0.13):
    return place(place(silence(gap + len(second) / SR), first, 0), second, gap)


def mechanism(seed, freq, duration):
    return mix(thump(freq, duration), noise_burst(duration, 250, 4200, seed),
               metallic(freq * 8, duration, seed), gains=(0.8, 0.34, 0.27))


def speech(path):
    with wave.open(str(path), "rb") as source:
        if source.getnchannels() != 1 or source.getsampwidth() != 2:
            raise ValueError(f"{path} must be mono 16-bit PCM")
        raw = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(float) / 32768
        rate = source.getframerate()
    voiced = np.flatnonzero(np.abs(raw) > 0.008)
    raw = raw[max(0, voiced[0] - rate // 100):voiced[-1] + rate // 50]
    frames = np.arange(round(len(raw) * SR / rate)) * rate / SR
    return fade(np.interp(frames, np.arange(len(raw)), raw))


def generate_voice(executable, folder):
    lines = {str(digit): word for digit, word in enumerate(DIGIT_WORDS)}
    lines["radio"] = "Control, two. West corridor clear."
    for key, line in lines.items():
        subprocess.run(
            [executable, "-v", "en-us", "-s", "170", "-w", str(folder / f"{key}.wav"), line],
            check=True,
        )


def render(voice_folder):
    reject = interval(beep(420), beep(294, 0.18))
    latch = mechanism(7, 120, 0.13)
    motor = loopable(mix(hum(92, 2.25, 4, 0.26), buzz(184, 2.25, 14), gains=(0.8, 0.12)))
    breaker = mix(
        mechanism(19, 95, 0.21),
        place(silence(1.1), env_ad(sweep(430, 44, 0.93), 0.035, 0.895), 0.14),
        place(silence(1.1), noise_burst(0.31, 240, 2100, 11), 0.10, 0.55),
        gains=(1, 0.73, 0.48),
    )
    alarm_pulse = env_ad(sweep(470, 820, 0.42), 0.10, 0.32)
    alarm = place(place(silence(1.85), alarm_pulse, 0.20), alarm_pulse, 0.85, 0.8)
    counter = silence(2.25)
    for i in range(10):
        counter = place(counter, click(0.06, 720 + i * 18, 40 + i), 0.32 + i * 0.072)

    cues = {
        "sec.reader.ok": interval(beep(1480, 0.10), beep(1976, 0.16), 0.10),
        "sec.reader.deny": reject,
        "sec.reader.idle": place(silence(3.6), beep(1245, 0.075), 0.35),
        "sec.bolt.unlock": interval(latch, mechanism(8, 170, 0.12), 0.085),
        "sec.bolt.locked": interval(mechanism(3, 78, 0.14), mechanism(4, 71, 0.15), 0.17),
        "sec.door.heavy": mix(mechanism(6, 53, 0.44), env_ad(sweep(137, 60, 0.62), 0.07, 0.55)),
        "sec.drawer.open": interval(latch, noise_burst(0.30, 320, 2600, 22), 0.08),
        "item.pickup": mix(paper(0.20, 16), interval(beep(784, 0.08), beep(1047, 0.12), 0.07),
                           gains=(0.48, 0.60)),
        "item.pickup.key": mix(click(0.045, 2600, 9), chime((784, 1175, 1568), 0.42, 0.075),
                               gains=(0.42, 0.70)),
        "sec.keypad.ok": chime((523, 659, 784, 1047), 0.62, 0.095),
        "sec.keypad.bad": interval(beep(349, 0.15), beep(233, 0.21), 0.15),
        "sec.keypad.lockout": chime((392, 311, 220), 0.54, 0.11),
        "sec.wire.seat": interval(click(0.035, 2300, 4), beep(988, 0.09), 0.04),
        "sec.wire.wrong": interval(click(0.045, 1800, 6), beep(262, 0.13), 0.035),
        "sec.wire.solved": chime((587, 784, 1175), 0.55, 0.075),
        "sec.breaker.throw": breaker,
        "sec.tape.insert": interval(mechanism(8, 110, 0.15), mechanism(15, 176, 0.13), 0.14),
        "sec.tape.play": motor,
        "sec.printer": interval(place(silence(0.58), counter[int(0.31 * SR):int(0.89 * SR)], 0),
                                paper(0.30, 29), 0.55),
        "sec.cctv.hum": loopable(hum(60, 2.25, 5, 0.42)),
        "sec.cctv.static": interval(noise_burst(0.21, 700, 6900, 35), beep(280, 0.13), 0.17),
        "sec.fluorescent": loopable(buzz(120, 2.25, 39)),
        "sec.vault.door": mix(mechanism(12, 47, 0.56), env_ad(sweep(156, 65, 0.76), 0.11, 0.65)),
        "sec.vault.hum": loopable(hum(50, 3.25, 6, 0.34)),
        "sec.alarm.lockdown": alarm,
        "sec.counting": counter,
    }
    for digit, note in enumerate(DIGIT_NOTES):
        freq = 440 * 2 ** ((note - 69) / 12)
        key = beep(freq)
        softer = beep(freq, brightness=0.06)
        cues[f"sec.keypad.key.{digit}"] = key
        cues[f"sec.keypad.key.{digit}_2"] = softer
        if voice_folder:
            spoken = bandpass(speech(voice_folder / f"{digit}.wav"), 340, 4100)
            cues[f"sec.keypad.spoken.{digit}"] = interval(key, spoken * 1.5, 0.125)
            cues[f"sec.keypad.spoken.{digit}_2"] = interval(softer, spoken * 1.5, 0.125)
    if voice_folder:
        radio = bandpass(speech(voice_folder / "radio.wav"), 480, 2900)
        radio = mix(radio, noise_burst(len(radio) / SR, 650, 3500, 41), gains=(0.85, 0.23))
        chatter = place(silence(9.5), radio, 1.4)
        chatter = place(chatter, beep(1380, 0.07), 1.27, 0.16)
        cues["sec.radio.chatter"] = place(chatter, click(0.065, 2100, 56), 1.42 + len(radio) / SR)
    for cue, samples in cues.items():
        save_wav(f"{FOLDER}/{cue}.wav", samples)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--voice-dir", type=Path)
    options = parser.parse_args()
    espeak = shutil.which("espeak-ng")
    if options.voice_dir or not espeak:
        render(options.voice_dir)
    else:
        with tempfile.TemporaryDirectory(prefix="highstakes-security-voice-") as directory:
            voice_folder = Path(directory)
            generate_voice(espeak, voice_folder)
            render(voice_folder)
