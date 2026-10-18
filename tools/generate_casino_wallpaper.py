"""Generate the casino wallpaper colour map (Slot D).

Writes Assets/Content/Environment/Casino/Textures/T_Wallpaper_Albedo.jpg from Poly Haven's CC0 "Floral Jacquard"
(colormass, Rico Cilliers; https://polyhaven.com/a/floral_jacquard), the same asset as T_Wallpaper_Normal.jpg, at the
same 2048 x 3106 size so the colour pattern sits exactly on the embossed relief. The grey photo is recoloured into the
kit palette: cream field (M_PlasterCream) with a soft brass-gold damask (M_BrassGold), plus the photo's weave detail.
M_Wallpaper uses it with a white base colour (CasinoTextureBuilder). Needs Pillow, numpy and an internet connection.
Run from the repository root:

    python tools/generate_casino_wallpaper.py [output folder]
"""
from pathlib import Path
import io
import json
import sys
import urllib.request

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Content/Environment/Casino/Textures'

CREAM = np.array([0.804, 0.733, 0.596])     # M_PlasterCream
GOLD = np.array([0.722, 0.537, 0.227])      # M_BrassGold
FIELD = np.minimum(CREAM * 1.08, 1.0)       # wall colour between the motifs, a touch lighter than the kit cream
MOTIF = (CREAM * 0.25 + GOLD * 0.75) * 0.85  # damask: deep gold, a little cream so it reads as wallpaper, not foil
WEAVE = 0.2                                 # how much of the photo's fabric texture is kept


def fetch(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'HighStakes-texture-tool'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return r.read()


def main():
    out = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else OUT
    out.mkdir(parents=True, exist_ok=True)
    files = json.loads(fetch('https://api.polyhaven.com/files/floral_jacquard'))
    img = Image.open(io.BytesIO(fetch(files['Diffuse']['2k']['jpg']['url']))).convert('RGB')

    rgb = np.asarray(img, dtype=np.float32) / 255.0
    lum = rgb @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    # Motif mask: the damask is the lighter thread. Stretch between percentiles, then soften to a clean two-tone.
    lo, hi = np.percentile(lum, [35, 65])
    t = np.clip((lum - lo) / (hi - lo), 0.0, 1.0)
    mask = t * t * (3.0 - 2.0 * t)
    # Weave: fine brightness variation around the local average, kept faint.
    blur = np.asarray(Image.fromarray((lum * 255).astype(np.uint8)).resize((img.width // 8, img.height // 8), Image.BILINEAR)
                      .resize(img.size, Image.BILINEAR), dtype=np.float32) / 255.0
    weave = 1.0 + (lum - blur) * WEAVE * 4.0

    colour = FIELD[None, None, :] * (1.0 - mask[..., None]) + MOTIF[None, None, :] * mask[..., None]
    colour = np.clip(colour * weave[..., None], 0.0, 1.0)
    path = out / 'T_Wallpaper_Albedo.jpg'
    Image.fromarray((colour * 255.0 + 0.5).astype(np.uint8)).save(path, quality=92)
    print('wrote', path)


if __name__ == '__main__':
    main()
