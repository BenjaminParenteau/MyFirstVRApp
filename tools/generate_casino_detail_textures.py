"""Generate the casino detail textures: ceiling, beams, furniture, tables, metalwork (Slot D).

Writes Assets/Content/Environment/Casino/Textures/:
  T_CeilingCoffer_*   carved dark-wood coffers   <- Poly Haven "dark_paneled_wood"       (1.4 m per tile)
  T_Rosewood_*        ceiling beams              <- Poly Haven "rosewood_veneer1"        (2.4 m per tile)
  T_DarkWood_*        pilasters, table wood, bar<- Poly Haven "lacquered_cherry_wood"   (1 m per tile)
  T_Felt_*            table felt                 <- Poly Haven "caban" (wool)            (1 m per tile)
  T_Leather_*         chair and stool seats      <- Poly Haven "leather_red_02"          (0.6 m per tile)
  T_BrushedMetal_*    brass, steel, slot cabinets  generated here, neutral grey, tinted by each material's colour

Felt, leather and metal also get soft large-scale mottling (wear, tarnish) so they still read at VR viewing distance,
where the fine fibre and brush detail blurs away in the mipmaps. Woods keep their photographed colour, brightened or darkened to the kit wood's brightness; felt and leather are
recoloured into the kit palette (M_FeltGreen, M_FabricBurgundy) like tools/generate_security_textures.py. All Poly Haven
sources are CC0. Needs Pillow, numpy and an internet connection. Run from the repository root:

    python tools/generate_casino_detail_textures.py [output folder]
"""
from pathlib import Path
import sys

import numpy as np
from PIL import Image

from generate_security_textures import SIZE, polyhaven_maps, recolour

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Content/Environment/Casino/Textures'

WOOD_DARK = (0x3B, 0x24, 0x16)        # M_WoodDark
FELT_GREEN = (0x0F, 0x5A, 0x3A)       # M_FeltGreen
BURGUNDY = (0x5A, 0x1A, 0x2B)         # M_FabricBurgundy
LUMA = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)

# (Poly Haven id, output name, mode, kit colour, brightness vs the kit colour, saturation (keep) or contrast (tint))
#   keep: photo colours, average brightness moved to the kit colour's; tint: grey photo tinted with the kit colour
PHOTO_SETS = [
    ('dark_paneled_wood', 'T_CeilingCoffer', 'keep', WOOD_DARK, 1.25, 0.6),    # a little lighter so the coffers read
    ('rosewood_veneer1', 'T_Rosewood', 'keep', WOOD_DARK, 1.7, 0.75),          # warm mid-brown beams
    ('lacquered_cherry_wood', 'T_DarkWood', 'keep', WOOD_DARK, 1.2, 1.0),
    ('caban', 'T_Felt', 'tint', FELT_GREEN, 1.15, 1.3),
    ('leather_red_02', 'T_Leather', 'tint', BURGUNDY, 1.2, 1.2),
]
# Large-scale mottling per output (strength, cells per tile).
MOTTLE = {'T_Felt': (0.16, 6), 'T_Leather': (0.12, 5)}


def keep_colour(img, kit, brightness, saturation):
    rgb = np.asarray(img, dtype=np.float32) / 255.0
    lum = rgb @ LUMA
    rgb = lum[..., None] + (rgb - lum[..., None]) * saturation
    target = (np.array(kit, dtype=np.float32) / 255.0 @ LUMA) * brightness
    out = np.clip(rgb * (target / lum.mean()), 0.0, 1.0)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8))


def low_frequency(cells, rng):
    """Smooth tileable noise in 0..1 with about `cells` blobs per side (a periodic grid upscaled, centre tile kept)."""
    grid = np.tile(rng.random((cells, cells)).astype(np.float32), (3, 3))
    big = Image.fromarray((grid * 255).astype(np.uint8)).resize((SIZE * 3, SIZE * 3), Image.BICUBIC)
    return np.asarray(big, dtype=np.float32)[SIZE:2 * SIZE, SIZE:2 * SIZE] / 255.0


def mottle(img, strength, cells, seed=5):
    rng = np.random.default_rng(seed)
    noise = low_frequency(cells, rng) * 0.65 + low_frequency(cells * 3, rng) * 0.35
    rgb = np.asarray(img, dtype=np.float32) / 255.0 * (1.0 + (noise[..., None] - 0.5) * 2.0 * strength)
    return Image.fromarray((np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8))


def brushed_metal(seed=11):
    """Neutral grey brushed metal (grain along U), albedo average ~0.8 so the material colour sets the tone."""
    rng = np.random.default_rng(seed)
    # Streaks: noise that is long along X and fine along Y, wrapped so it tiles.
    streak = rng.random((SIZE, SIZE // 16)).astype(np.float32)
    streak = np.asarray(Image.fromarray((streak * 255).astype(np.uint8)).resize((SIZE, SIZE), Image.BICUBIC), np.float32) / 255.0
    streak = np.repeat(rng.random((SIZE, 1)).astype(np.float32), SIZE, axis=1) * 0.6 + streak * 0.4
    fine = rng.random((SIZE, SIZE)).astype(np.float32)
    fine = (fine + np.roll(fine, 1, axis=1) + np.roll(fine, 2, axis=1) + np.roll(fine, 3, axis=1)) / 4.0
    height = streak * 0.7 + fine * 0.3
    tarnish = low_frequency(5, rng) * 0.6 + low_frequency(14, rng) * 0.4   # patchy wear, readable from a distance
    albedo = 0.78 + (height - height.mean()) * 0.4 + (tarnish - 0.5) * 0.22
    albedo_img = Image.fromarray((np.clip(np.stack([albedo] * 3, -1), 0, 1) * 255 + 0.5).astype(np.uint8))

    strength = 4.0
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5 * strength
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5 * strength * 0.2
    n = np.stack([-gx, gy, np.ones_like(height)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    normal_img = Image.fromarray(((n * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8))
    return albedo_img, normal_img



def main():
    out = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else OUT
    out.mkdir(parents=True, exist_ok=True)

    def save(img, name):
        path = out / f'{name}.jpg'
        img.save(path, quality=92)
        print('wrote', path)

    for asset, name, mode, kit, brightness, amount in PHOTO_SETS:
        albedo, normal = polyhaven_maps(asset)
        albedo = keep_colour(albedo, kit, brightness, amount) if mode == 'keep' else recolour(albedo, kit, brightness, amount)
        if name in MOTTLE:
            albedo = mottle(albedo, *MOTTLE[name])
        save(albedo, name + '_Albedo')
        save(normal, name + '_Normal')
    albedo, normal = brushed_metal()
    save(albedo, 'T_BrushedMetal_Albedo')
    save(normal, 'T_BrushedMetal_Normal')


if __name__ == '__main__':
    main()
