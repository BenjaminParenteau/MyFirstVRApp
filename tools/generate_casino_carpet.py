"""Generate the casino floor carpet texture (Slot D).

Writes Assets/Content/Environment/Casino/Textures/T_CasinoCarpet_Albedo.png: a seamless 1024 x 1024 tile covering
1 m of floor (CasinoTextureBuilder.CarpetRepeat). Classic casino carpet in the kit palette: burgundy field
(M_CarpetRed), brass-gold diamond lattice with quatrefoil medallions (M_BrassGold) and felt-green rosettes
(M_FeltGreen), plus a little fibre noise. Pure Python (no Pillow). Run from the repository root:

    python tools/generate_casino_carpet.py
"""
from pathlib import Path
import math
import random
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Content/Environment/Casino/Textures/T_CasinoCarpet_Albedo.png'
SIZE = 1024

FIELD = (112, 22, 36)       # burgundy, a little brighter than M_CarpetRed so the pattern reads under casino light
FIELD_DARK = (78, 14, 26)   # inner diamond
GOLD = (196, 148, 64)       # M_BrassGold
GREEN = (18, 96, 62)        # M_FeltGreen
REPEATS = 2                 # lattice cells per tile side (so each diamond is ~0.35 m)
PX = REPEATS / SIZE         # one pixel, in lattice units


def wrap(d):
    """Distance to the nearest integer, so every shape tiles seamlessly."""
    d = d % 1.0
    return min(d, 1.0 - d)


def cover(dist, edge):
    """1 inside (dist < edge), 0 outside, with a one-pixel soft edge."""
    return max(0.0, min(1.0, (edge - dist) / PX + 0.5))


def mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def point_dist(u, v, cu, cv):
    return math.hypot(wrap(u - cu), wrap(v - cv))


def pixel(u, v, rng):
    u, v = u * REPEATS, v * REPEATS
    # Diamond lattice: lines u + v = k and u - v = k (diamonds centred at (0.5, 0) and (0, 0.5)).
    line = min(wrap(u + v), wrap(u - v)) / math.sqrt(2.0)
    c = FIELD_DARK if line > 0.06 else FIELD
    c = mix(c, GOLD, cover(line, 0.012))                                   # main lattice line
    c = mix(c, GOLD, cover(abs(line - 0.04), 0.005))                       # thin companion line

    # Gold quatrefoil (four petals and a green heart) at (0.5, 0).
    for du, dv in ((0.06, 0), (-0.06, 0), (0, 0.06), (0, -0.06)):
        c = mix(c, GOLD, cover(point_dist(u, v, 0.5 + du, dv), 0.055))
    c = mix(c, GREEN, cover(point_dist(u, v, 0.5, 0.0), 0.04))

    # Green rosette with a gold ring and eight gold beads at (0, 0.5).
    d = point_dist(u, v, 0.0, 0.5)
    c = mix(c, GREEN, cover(d, 0.1))
    c = mix(c, GOLD, cover(abs(d - 0.105), 0.012))
    for k in range(8):
        a = k * math.pi / 4
        c = mix(c, GOLD, cover(point_dist(u, v, 0.06 * math.cos(a), 0.5 + 0.06 * math.sin(a)), 0.017))

    # Gold studs where the lattice lines cross.
    c = mix(c, GOLD, cover(min(point_dist(u, v, 0, 0), point_dist(u, v, 0.5, 0.5)), 0.03))

    # Fibre noise.
    n = rng.uniform(-7, 7)
    return bytes(max(0, min(255, int(ch + n))) for ch in c)


def write_png(path, rows):
    raw = b''.join(b'\x00' + r for r in rows)

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)

    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', SIZE, SIZE, 8, 2, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    path.write_bytes(png)


def main():
    rng = random.Random(7)
    rows = []
    for y in range(SIZE):
        v = (y + 0.5) / SIZE
        rows.append(b''.join(pixel((x + 0.5) / SIZE, v, rng) for x in range(SIZE)))
    write_png(OUT, rows)
    print('Wrote', OUT.relative_to(ROOT))


if __name__ == '__main__':
    main()
