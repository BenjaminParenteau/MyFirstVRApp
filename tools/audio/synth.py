"""Shared audio synthesis helpers for High Stakes (Milestone 6).

Owned by Slot D (Zach) and **append-only**: if you need a primitive that isn't here, ask and it gets added,
rather than editing or reshaping what's here — three other generator scripts import from this file, so a changed
signature breaks someone else's sounds silently. Adding a new function is always safe.

Nobody on the team has recorded audio, so every cue is synthesised, the same way every texture in this repo was
made (`tools/generate_casino_carpet.py` and friends). Pure numpy, no audio libraries, 48 kHz 16-bit output.

Each slot has its own generator script that imports this one:

    tools/audio/generate_tables_audio.py        Slot A (Ben)     -> Assets/Content/Tables/Audio/
    tools/audio/generate_security_audio.py      Slot B (Ashton)  -> Assets/Content/Progression/Audio/
    tools/audio/generate_ui_audio.py            Slot C (Pak)     -> Assets/Content/UI/Audio/
    tools/audio/generate_environment_audio.py   Slot D (Zach)    -> Assets/Content/Environment/Audio/

Write your own; don't add your cues to anyone else's. See docs/milestone6/ for your brief.

Conventions these helpers follow, which are also the project's audio standards (StyleGuide §11):
  * **Mono.** A positioned 3D source is spatialised from mono anyway, so stereo doubles the cost for nothing.
    Only music and ambience beds are stereo (`save_wav(..., stereo=True)`), because they are not positioned.
  * **48 kHz, 16-bit.** Matches the Quest's audio path; no resampling on load.
  * **Peak-normalised to -3 dBFS** by `save_wav`, so you balance cues in the game (bus volumes, `SoundCue.volume`)
    rather than by re-rendering wavs.
  * **Loops are seamless.** Use `loopable()` on anything the game will loop, or you get a click every cycle.

Run your own generator from the repository root:

    python tools/audio/generate_<yours>_audio.py
"""
from __future__ import annotations

import math
import struct
import wave
from pathlib import Path

import numpy as np

SR = 48_000                 # sample rate, Hz. Don't change it per-cue; the importer assumes one rate.
PEAK = 0.708                # -3 dBFS headroom, so nothing clips once several cues fire at once
ROOT = Path(__file__).resolve().parents[2]


# ---------------------------------------------------------------------------
# basics
# ---------------------------------------------------------------------------

def seconds(n: float) -> int:
    """Length in samples of n seconds."""
    return max(1, int(round(n * SR)))


def silence(dur: float) -> np.ndarray:
    return np.zeros(seconds(dur), dtype=np.float64)


def t(dur: float) -> np.ndarray:
    """Time axis in seconds, for writing your own oscillators."""
    return np.arange(seconds(dur), dtype=np.float64) / SR


def rng(seed: int) -> np.random.Generator:
    """Seeded generator — always seed, so a regenerated cue is identical and git stays quiet."""
    return np.random.default_rng(seed)


# ---------------------------------------------------------------------------
# envelopes
# ---------------------------------------------------------------------------

def env_ad(sig: np.ndarray, attack: float = 0.002, decay: float = 0.08, curve: float = 2.0) -> np.ndarray:
    """Attack/decay envelope — the workhorse for one-shots (clicks, clinks, beeps).

    `curve` > 1 decays faster at the start, which is what most physical impacts do.
    """
    n = len(sig)
    a = min(seconds(attack), n)
    d = max(1, n - a)
    out = np.empty(n)
    out[:a] = np.linspace(0.0, 1.0, a, endpoint=False) if a else 1.0
    out[a:] = (1.0 - np.linspace(0.0, 1.0, d)) ** curve
    return sig * out


def env_adsr(sig: np.ndarray, attack=0.01, decay=0.05, sustain=0.6, release=0.2) -> np.ndarray:
    """Full ADSR, for anything held (alarms, tones, pads)."""
    n = len(sig)
    a, d, r = seconds(attack), seconds(decay), seconds(release)
    s = max(1, n - a - d - r)
    env = np.concatenate([
        np.linspace(0.0, 1.0, a, endpoint=False),
        np.linspace(1.0, sustain, d, endpoint=False),
        np.full(s, sustain),
        np.linspace(sustain, 0.0, r),
    ])
    env = np.resize(env, n)
    return sig * env


def fade(sig: np.ndarray, dur: float = 0.01) -> np.ndarray:
    """Short fade in and out, so a clip never starts or ends on a discontinuity (a click)."""
    n = min(seconds(dur), len(sig) // 2)
    if n <= 0:
        return sig
    out = sig.copy()
    ramp = np.linspace(0.0, 1.0, n)
    out[:n] *= ramp
    out[-n:] *= ramp[::-1]
    return out


# ---------------------------------------------------------------------------
# filters (one-pole, cheap and good enough for sound design)
# ---------------------------------------------------------------------------

def lowpass(sig: np.ndarray, cutoff: float) -> np.ndarray:
    """One-pole low-pass. Rounds off harshness; the main tool for making something sound distant or muffled."""
    if cutoff >= SR / 2:
        return sig
    a = math.exp(-2.0 * math.pi * cutoff / SR)
    out = np.empty_like(sig)
    prev = 0.0
    for i, x in enumerate(sig):
        prev = (1.0 - a) * x + a * prev
        out[i] = prev
    return out


def highpass(sig: np.ndarray, cutoff: float) -> np.ndarray:
    """One-pole high-pass. Removes mud; use it on anything that should sound small or crisp."""
    return sig - lowpass(sig, cutoff)


def bandpass(sig: np.ndarray, low: float, high: float) -> np.ndarray:
    return highpass(lowpass(sig, high), low)


def resonate(sig: np.ndarray, freq: float, q: float = 40.0, gain: float = 1.0) -> np.ndarray:
    """Ring a signal at one frequency — the difference between a thud and a *clink*.

    Stack two or three at non-harmonic ratios for metal, glass or a struck object.
    """
    w = 2.0 * math.pi * freq / SR
    r = math.exp(-w / (2.0 * q))
    b0 = (1.0 - r * r) * 0.5
    a1, a2 = -2.0 * r * math.cos(w), r * r
    out = np.zeros_like(sig)
    y1 = y2 = 0.0
    for i, x in enumerate(sig):
        y = b0 * x - a1 * y1 - a2 * y2
        out[i] = y
        y2, y1 = y1, y
    return sig + out * gain


# ---------------------------------------------------------------------------
# sources
# ---------------------------------------------------------------------------

def tone(freq: float, dur: float, kind: str = "sine", detune: float = 0.0) -> np.ndarray:
    """One oscillator. kind: sine | tri | square | saw. `detune` in cents, for thickening a stack."""
    f = freq * (2.0 ** (detune / 1200.0))
    ph = 2.0 * math.pi * f * t(dur)
    if kind == "sine":
        return np.sin(ph)
    if kind == "tri":
        return 2.0 / math.pi * np.arcsin(np.sin(ph))
    if kind == "square":
        return np.sign(np.sin(ph))
    if kind == "saw":
        return 2.0 * ((f * t(dur)) % 1.0) - 1.0
    raise ValueError(f"unknown kind {kind!r}")


def sweep(f0: float, f1: float, dur: float, kind: str = "sine") -> np.ndarray:
    """A tone gliding from f0 to f1 — rising alarms, falling power-downs, sonar pings."""
    tt = t(dur)
    k = (f1 / f0) ** (1.0 / max(dur, 1e-6))
    ph = 2.0 * math.pi * f0 * (k ** tt - 1.0) / math.log(max(k, 1.0000001))
    return np.sin(ph) if kind == "sine" else np.sign(np.sin(ph))


def noise(dur: float, seed: int = 0, colour: str = "white") -> np.ndarray:
    """white | pink | brown. Pink and brown are the useful ones for air, rumble and room tone."""
    g = rng(seed)
    n = g.standard_normal(seconds(dur))
    if colour == "white":
        return n
    if colour == "pink":
        out = np.zeros_like(n)
        b = [0.0] * 3
        for i, x in enumerate(n):
            b[0] = 0.99765 * b[0] + x * 0.0990460
            b[1] = 0.96300 * b[1] + x * 0.2965164
            b[2] = 0.57000 * b[2] + x * 1.0526913
            out[i] = b[0] + b[1] + b[2] + x * 0.1848
        return out / 3.0
    if colour == "brown":
        return np.cumsum(n) / math.sqrt(len(n))
    raise ValueError(f"unknown colour {colour!r}")


# ---------------------------------------------------------------------------
# ready-made shapes — the ones the briefs name
# ---------------------------------------------------------------------------

def click(dur: float = 0.03, freq: float = 1800.0, seed: int = 0, body: float = 0.4) -> np.ndarray:
    """A button press / small impact. `body` adds a woody thump under the tick (brass buttons want ~0.5)."""
    tick = env_ad(highpass(noise(dur, seed), 900.0), attack=0.0005, decay=dur, curve=3.0)
    thud = env_ad(tone(freq * 0.08, dur, "sine"), attack=0.001, decay=dur, curve=2.5)
    return tick + thud * body


def noise_burst(dur: float = 0.08, low: float = 200.0, high: float = 6000.0, seed: int = 0,
                curve: float = 2.5) -> np.ndarray:
    """Filtered noise with a decay — sweeps, rakes, card slides, bolts, footsteps."""
    return env_ad(bandpass(noise(dur, seed), low, high), attack=0.001, decay=dur, curve=curve)


def metallic(freq: float = 2400.0, dur: float = 0.25, seed: int = 0, partials=(1.0, 2.76, 5.40)) -> np.ndarray:
    """A struck metal / glass object: chips, coins, a keycard reader's housing, a bell.

    The default partial ratios are inharmonic on purpose — harmonic ratios sound like a musical note, not an object.
    """
    out = env_ad(noise(dur, seed) * 0.4, attack=0.0003, decay=dur, curve=4.0)
    for i, ratio in enumerate(partials):
        out = resonate(out, freq * ratio, q=60.0 + i * 40.0, gain=1.0 / (i + 1))
    return env_ad(out, attack=0.0005, decay=dur, curve=1.6)


def hum(freq: float = 50.0, dur: float = 4.0, harmonics: int = 4, wobble: float = 0.4) -> np.ndarray:
    """Electrical hum for loops — vault, CCTV wall, air handling. Follow with `loopable()`."""
    out = np.zeros(seconds(dur))
    for h in range(1, harmonics + 1):
        amp = 1.0 / (h ** 1.5)
        drift = 1.0 + wobble * 0.002 * np.sin(2.0 * math.pi * (0.07 * h) * t(dur))
        out += amp * np.sin(2.0 * math.pi * freq * h * t(dur) * drift)
    return out


def buzz(freq: float = 120.0, dur: float = 4.0, seed: int = 0) -> np.ndarray:
    """Fluorescent-strip buzz: a hard harmonic stack plus hiss. Follow with `loopable()`."""
    core = tone(freq, dur, "square") * 0.25 + tone(freq * 2, dur, "square") * 0.12
    return bandpass(core, 80.0, 4000.0) + lowpass(noise(dur, seed), 3000.0) * 0.08


def chime(freqs=(880.0, 1320.0), dur: float = 0.5, spacing: float = 0.07) -> np.ndarray:
    """Two or three notes for a UI confirmation / alert. Keep it under ~0.5 s or it grates."""
    out = np.zeros(seconds(dur + spacing * len(freqs)))
    for i, f in enumerate(freqs):
        note = env_ad(tone(f, dur, "sine") + tone(f * 2, dur, "sine") * 0.25, attack=0.004, decay=dur, curve=2.2)
        at = seconds(spacing * i)
        out[at:at + len(note)] += note
    return out


def paper(dur: float = 0.22, seed: int = 0) -> np.ndarray:
    """A page turn / rustle — band-limited noise with an irregular envelope."""
    g = rng(seed)
    body = bandpass(noise(dur, seed), 700.0, 7000.0)
    wobble = np.interp(np.linspace(0, 1, len(body)), np.linspace(0, 1, 12), g.random(12) * 0.8 + 0.3)
    return env_ad(body * wobble, attack=0.015, decay=dur, curve=1.4)


def thump(freq: float = 70.0, dur: float = 0.18) -> np.ndarray:
    """A low body hit — a bolt, a door closing, a heavy switch."""
    return env_ad(tone(freq, dur, "sine") + tone(freq * 1.5, dur, "sine") * 0.3,
                  attack=0.001, decay=dur, curve=2.0)


# ---------------------------------------------------------------------------
# shaping and output
# ---------------------------------------------------------------------------

def mix(*parts: np.ndarray, gains: tuple[float, ...] | None = None) -> np.ndarray:
    """Sum signals of different lengths, zero-padding to the longest."""
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for i, p in enumerate(parts):
        g = 1.0 if gains is None else gains[i]
        out[:len(p)] += p * g
    return out


def place(base: np.ndarray, part: np.ndarray, at: float, gain: float = 1.0) -> np.ndarray:
    """Drop `part` into `base` at `at` seconds (clipped to fit). For building sequences and loops."""
    i = seconds(at)
    end = min(len(base), i + len(part))
    if i < len(base):
        base[i:end] += part[:end - i] * gain
    return base


def loopable(sig: np.ndarray, blend: float = 0.25) -> np.ndarray:
    """Make a loop seamless by crossfading the tail over the head.

    Returns a clip `blend` seconds shorter. **Use this on every looping cue** — without it the game clicks once
    per cycle, which is the single most common mistake in game audio.
    """
    n = seconds(blend)
    if n <= 0 or len(sig) <= 2 * n:
        return fade(sig, 0.005)
    head, tail, mid = sig[:n], sig[-n:], sig[n:-n]
    ramp = np.linspace(0.0, 1.0, n)
    return np.concatenate([tail * (1.0 - ramp) + head * ramp, mid])


def pitch(sig: np.ndarray, semitones: float) -> np.ndarray:
    """Resample for a pitch shift (length changes, like a tape speed change)."""
    ratio = 2.0 ** (semitones / 12.0)
    n = max(1, int(len(sig) / ratio))
    return np.interp(np.linspace(0, len(sig) - 1, n), np.arange(len(sig)), sig)


def variants(make, count: int = 4, base_seed: int = 0, semitone_spread: float = 1.2) -> list[np.ndarray]:
    """Build N variants of a one-shot, for cues that fire often.

    `make(seed)` returns one clip. Anything the player hears more than a few times a session needs this — a cue
    with a single clip played fifty times sounds like a machine gun. `SoundCue.clips[]` takes them all and the game
    picks at random, on top of its own pitch jitter.
    """
    out = []
    g = rng(base_seed)
    for i in range(count):
        clip = make(base_seed + i * 101)
        out.append(pitch(clip, float(g.uniform(-semitone_spread, semitone_spread))))
    return out


def normalize(sig: np.ndarray, peak: float = PEAK) -> np.ndarray:
    m = float(np.max(np.abs(sig)))
    return sig if m < 1e-9 else sig * (peak / m)


def save_wav(path: str | Path, sig: np.ndarray, stereo: bool = False, width: float = 0.25,
             norm: bool = True) -> Path:
    """Write a 48 kHz 16-bit wav, creating parent folders. Mono unless `stereo` (beds and music only).

    `stereo=True` fakes width by delaying and tilting one channel — fine for a non-positioned bed, and never
    use it for a 3D cue (Unity spatialises from mono, so stereo there is double the memory for nothing).
    """
    out = normalize(np.asarray(sig, dtype=np.float64)) if norm else np.asarray(sig, dtype=np.float64)
    p = Path(path)
    if not p.is_absolute():
        p = ROOT / p
    p.parent.mkdir(parents=True, exist_ok=True)

    if stereo:
        d = seconds(width * 0.02)
        right = np.concatenate([np.zeros(d), out[:-d]]) if d and d < len(out) else out
        frames = np.stack([out, lowpass(right, 9000.0)], axis=1)
        frames = normalize(frames) if norm else frames
        channels = 2
    else:
        frames = out[:, None]
        channels = 1

    pcm = np.clip(frames, -1.0, 1.0)
    data = (pcm * 32767.0).astype('<i2').tobytes()
    with wave.open(str(p), 'wb') as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data)
    kib = len(data) / 1024.0
    try:
        shown = p.relative_to(ROOT)
    except ValueError:
        shown = p          # writing somewhere outside the repo (a temp dir, a scratch listen) is fine
    print(f"  {shown}  {len(frames) / SR:.2f}s  {channels}ch  {kib:.0f} KiB")
    return p


def report(paths: list[Path]) -> None:
    """Print a total, so you notice before the §7 audio budget does (8 MB resident across all four slots)."""
    total = sum(p.stat().st_size for p in paths) / 1024.0 / 1024.0
    print(f"\n{len(paths)} clips, {total:.2f} MB on disk (uncompressed; Unity imports these ADPCM/Vorbis)")
    if total > 6.0:
        print("  NOTE: that is a large source set — check Validate Cues for the resident total.")
