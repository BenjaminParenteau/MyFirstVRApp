"""Generate the back-of-house (security wing) textures (Slot D).

Writes Assets/Content/Environment/Security/Textures/:
  T_SecurityWall_*     painted cinder block        <- Poly Haven "concrete_block_wall"    (2 m per tile)
  T_SecurityFloor_*    sealed concrete floor       <- Poly Haven "smooth_concrete_floor"  (2 m per tile)
  T_VaultFloor_*       diamond tread plate         <- Poly Haven "metal_plate"            (0.5 m per tile)
  T_SecuritySteel_*    riveted steel panels        <- Poly Haven "metal_plate_02"         (2 m per tile)
  T_SecurityCeiling_*  acoustic drop-ceiling grid  <- generated here                      (1 m per tile)

All Poly Haven sources are CC0. Each photo albedo is recoloured into the style-kit palette (StyleGuide §2): converted to
grey, its average brightness moved to the kit colour's and tinted with that colour, so the Material Variants can use a
white base colour and still read as ConcreteCool / TileCool / SteelCool. Normal maps (OpenGL, as Unity expects) are
copied unchanged. Output is 1024 px (the Android import maximum, StyleGuide §7).

Needs Pillow and numpy and an internet connection. Run from the repository root:

    python tools/generate_security_textures.py [output folder]
"""
from pathlib import Path
import io
import json
import sys
import urllib.request

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Content/Environment/Security/Textures'
SIZE = 1024

# Kit colours (Assets/Content/StyleKit/Materials, StyleGuide §2).
CONCRETE_COOL = (0x6E, 0x77, 0x80)
TILE_COOL = (0x4E, 0x5A, 0x63)
STEEL_COOL = (0x8F, 0xA3, 0xB0)

# (Poly Haven id, output name, kit colour, brightness of the average vs the kit colour, contrast)
PHOTO_SETS = [
    ('concrete_block_wall', 'T_SecurityWall', CONCRETE_COOL, 1.15, 1.0),
    ('smooth_concrete_floor', 'T_SecurityFloor', TILE_COOL, 1.0, 0.8),
    ('metal_plate', 'T_VaultFloor', STEEL_COOL, 0.75, 1.0),
    ('metal_plate_02', 'T_SecuritySteel', STEEL_COOL, 1.0, 0.7),
]


def fetch(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'HighStakes-texture-tool'})
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read()


def polyhaven_maps(asset):
    """Returns (albedo, normal) PIL images of the 1K JPG maps of a Poly Haven texture."""
    files = json.loads(fetch(f'https://api.polyhaven.com/files/{asset}'))
    colour = 'Diffuse' if 'Diffuse' in files else 'coll1'   # some fabrics ship colour variants instead
    albedo = Image.open(io.BytesIO(fetch(files[colour]['1k']['jpg']['url']))).convert('RGB')
    normal = Image.open(io.BytesIO(fetch(files['nor_gl']['1k']['jpg']['url']))).convert('RGB')
    return albedo.resize((SIZE, SIZE), Image.LANCZOS), normal.resize((SIZE, SIZE), Image.LANCZOS)


def recolour(img, kit, brightness, contrast):
    """Grey version of img whose average is the kit colour (times brightness), keeping the photo's detail."""
    rgb = np.asarray(img, dtype=np.float32) / 255.0
    lum = rgb @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    mean = lum.mean()
    detail = 1.0 + (lum / mean - 1.0) * contrast          # 1.0 = average brightness
    target = np.array(kit, dtype=np.float32) / 255.0 * brightness
    out = np.clip(detail[..., None] * target[None, None, :], 0.0, 1.0)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8))


# ------------------------------------------------------------------ drop ceiling

CEILING_TILES = 2          # tiles per texture side: 0.5 m tiles on a 1 m texture (4 x 4 on a 2 m kit ceiling tile)
BAR = 0.012                # half-width of the T-bar, in metres (24 mm bar)
PANEL = (214, 218, 222)    # cool off-white mineral fibre
GRID = (232, 234, 236)     # painted steel T-bar


def ceiling_maps(seed=7):
    rng = np.random.default_rng(seed)
    px = np.arange(SIZE, dtype=np.float32) / SIZE                      # 0..1 m
    cell = px * CEILING_TILES % 1.0                                    # 0..1 within one tile
    edge = np.minimum(cell, 1.0 - cell) / CEILING_TILES                # metres to the nearest grid line
    dx, dy = np.meshgrid(edge, edge)
    d = np.minimum(dx, dy)

    # Height: panels at 0, bars raised, a small bevel where the panel meets the bar, fissure pits in the panels.
    bar = np.clip((BAR - d) / 0.002 + 0.5, 0.0, 1.0)                  # 1 on the bar, soft 2 mm edge
    bevel = np.clip(1.0 - (d - BAR) / 0.008, 0.0, 1.0) * (1.0 - bar)  # 8 mm chamfer down into the panel

    def blur_noise(scale):
        small = rng.random((SIZE // scale, SIZE // scale)).astype(np.float32)
        img = Image.fromarray((small * 255).astype(np.uint8)).resize((SIZE, SIZE), Image.BICUBIC)
        return np.asarray(img, dtype=np.float32) / 255.0

    fine = rng.random((SIZE, SIZE)).astype(np.float32)
    pits = (fine > 0.985).astype(np.float32)                           # pinholes
    worms = (blur_noise(4) > 0.80).astype(np.float32) * 0.6            # short fissures
    texture = (pits + worms) * (1.0 - bar) * (1.0 - bevel)
    height = bar * 1.0 + bevel * 0.4 - texture * 0.15

    # Albedo: panel with soft blotches and darker pits, bright bars, a faint shadow in the bevel.
    blotch = (blur_noise(64) - 0.5) * 0.06 + (blur_noise(16) - 0.5) * 0.03
    panel = np.array(PANEL, np.float32)[None, None, :] / 255.0 * (1.0 + blotch[..., None] - texture[..., None] * 0.25)
    panel *= (1.0 - bevel[..., None] * 0.12)
    grid = np.array(GRID, np.float32)[None, None, :] / 255.0
    albedo = panel * (1.0 - bar[..., None]) + grid * bar[..., None]
    albedo_img = Image.fromarray((np.clip(albedo, 0, 1) * 255 + 0.5).astype(np.uint8))

    # Tangent-space normal (OpenGL: +Y up in the image) from the height map, wrapping at the edges so it tiles.
    strength = 6.0
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5 * strength
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5 * strength
    n = np.stack([-gx, gy, np.ones_like(height)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    normal_img = Image.fromarray(((n * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8))
    return albedo_img, normal_img


def save(img, name):
    path = OUT / f'{name}.jpg'
    img.save(path, quality=92)
    print('wrote', path)


def main():
    global OUT
    if len(sys.argv) > 1:
        OUT = Path(sys.argv[1]).resolve()
    OUT.mkdir(parents=True, exist_ok=True)
    for asset, name, kit, brightness, contrast in PHOTO_SETS:
        albedo, normal = polyhaven_maps(asset)
        save(recolour(albedo, kit, brightness, contrast), name + '_Albedo')
        save(normal, name + '_Normal')
    albedo, normal = ceiling_maps()
    save(albedo, 'T_SecurityCeiling_Albedo')
    save(normal, 'T_SecurityCeiling_Normal')


if __name__ == '__main__':
    main()
