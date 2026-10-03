"""Build the static Casino art preview from existing shared kit assets.

Run from the repository root. Does not modify any shared kit asset.
"""
from pathlib import Path
import math
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / 'Assets/Scenes/Casino_Test/Casino.unity'
KIT = ROOT / 'Assets/Content/StyleKit'
original = SCENE.read_text(encoding='utf-8')
# Keep the original camera, light and scene settings when rebuilding.
base = original.split('--- !u!1660057539')[0].split('--- !u!1 &3000000')[0]
base = base.replace('m_AmbientMode: 0', 'm_AmbientMode: 3')
base = re.sub(r'm_AmbientSkyColor: .*', 'm_AmbientSkyColor: {r: 0.38, g: 0.31, b: 0.24, a: 1}', base)
base = re.sub(r'm_SkyboxMaterial: .*', 'm_SkyboxMaterial: {fileID: 0}', base)
base = base.replace('m_ClearFlags: 1', 'm_ClearFlags: 2')
base = base.replace('m_LocalPosition: {x: 0, y: 1, z: -10}', 'm_LocalPosition: {x: 0, y: 1.7, z: -7.7}')
base = base.replace('field of view: 60', 'field of view: 72')
base = base.replace('m_Intensity: 1\n', 'm_Intensity: 0.85\n')
base = base.replace('m_Name: Directional Light', 'm_Name: Casino_Preview_WarmLight')
# This is a visual preview: one shadow-free realtime light, no bake required.
base = re.sub(r'(m_Shadows:\n\s+ m_Type:) \d+', r'\1 0', base)
parts = [base]
roots = [1146479766, 1764113453]
counter = 3000000

def vector(v):
    return '{' + ', '.join(f'{a}: {b:.8g}' for a, b in zip('xyz', v)) + '}'

def append_asset(text, name, pos, scale=(1, 1, 1), yaw=0):
    global counter
    text = text.split('%TAG !u! tag:unity3d.com,2011:\n')[-1]
    ids = re.findall(r'^--- !u!\d+ &(\d+)', text, re.M)
    mapping = {}
    for old in ids:
        mapping[old] = str(counter)
        counter += 1
    text = re.sub(r'&(\d+)', lambda m: '&' + mapping.get(m[1], m[1]), text)
    text = re.sub(r'fileID: (\d+)(?=})', lambda m: 'fileID: ' + mapping.get(m[1], m[1]), text)
    blocks = re.split(r'(?=^--- !u!)', text, flags=re.M)
    root_id = None
    root_go = None
    for i, block in enumerate(blocks):
        if block.startswith('--- !u!4 ') and 'm_Father: {fileID: 0}' in block:
            root_id = int(re.search(r'&(\d+)', block)[1])
            root_go = re.search(r'm_GameObject: {fileID: (\d+)}', block)[1]
            block = re.sub(r'm_LocalPosition: .*', 'm_LocalPosition: ' + vector(pos), block)
            block = re.sub(r'm_LocalScale: .*', 'm_LocalScale: ' + vector(scale), block)
            a = math.radians(yaw / 2)
            block = re.sub(r'm_LocalRotation: .*', f'm_LocalRotation: {{x: 0, y: {math.sin(a):.8g}, z: 0, w: {math.cos(a):.8g}}}', block)
            blocks[i] = block
    assert root_id is not None
    for i, block in enumerate(blocks):
        if block.startswith(f'--- !u!1 &{root_go}\n'):
            blocks[i] = re.sub(r'm_Name: .*', 'm_Name: ' + name, block)
    roots.append(root_id)
    parts.append(''.join(blocks))

def prefab(name, pos, yaw=0):
    append_asset((KIT / 'Prefabs' / f'{name}.prefab').read_text(), f'Casino_{name}', pos, yaw=yaw)

floor = (KIT / 'Prefabs/Kit_FloorTile_Warm.prefab').read_text()
# Extract the kit's cube mesh and components for bespoke architectural pieces.
cube = floor.split('--- !u!1 &7269106005861701223')[0]
cube = cube.replace('m_Father: {fileID: 3281809264075654052}', 'm_Father: {fileID: 0}')
cube = cube.replace('m_StaticEditorFlags: 0', 'm_StaticEditorFlags: 2147483647')
cube = cube.replace('m_CastShadows: 1', 'm_CastShadows: 0')

def shape(name, pos, size, mat='WoodDark', mesh=10202, yaw=0):
    guid = re.search(r'guid: (\w+)', (KIT / 'Materials' / f'M_{mat}.mat.meta').read_text())[1]
    text = cube.replace('d4999eb69fc254f43ba49b6e7da052f5', guid)
    text = text.replace('fileID: 10202,', f'fileID: {mesh},')
    # Unity's cylinder mesh is two units tall; the source cube collider is one.
    if mesh == 10206:
        text = text.replace('m_Size: {x: 1, y: 1, z: 1}', 'm_Size: {x: 1, y: 2, z: 1}')
    append_asset(text, 'Casino_' + name, pos, size, yaw)

# Room: open center aisle, burgundy floor, paneled walls and coffered ceiling.
shape('Carpet', (0, -.1, 2), (18, .2, 22), 'CarpetRed')
shape('Ceiling', (0, 4.2, 2), (18, .15, 22), 'WoodDark')
for x in (-9, 9):
    shape('SideWall', (x, 2, 2), (.2, 4.2, 22), 'PlasterCream')
    shape('Wainscot', (x * .987, .65, 2), (.16, 1.3, 22))
    shape('GoldChairRail', (x * .974, 1.32, 2), (.07, .06, 22), 'BrassGold')
    shape('CrownMolding', (x * .974, 3.85, 2), (.12, .18, 22), 'BrassGold')
    for z in (-6, -2, 2, 6, 10):
        shape('WallPilaster', (x * .962, 2, z), (.25, 4, .3), 'WoodDark')
        shape('PilasterGoldInset', (x * .945, 2.2, z), (.06, 2.6, .13), 'BrassGold')
shape('BackWall', (0, 2, 13), (18, 4.2, .2), 'PlasterCream')
shape('BackPaneling', (0, .65, 12.85), (18, 1.3, .15))
for x in (-7, -3.5, 0, 3.5, 7):
    shape('CeilingBeam', (x, 4.02, 2), (.13, .18, 22), 'BrassGold')
for z in (-5, 0, 5, 10):
    shape('CeilingCrossbeam', (0, 4.02, z), (18, .18, .13), 'BrassGold')
for x in (-1.4, 1.4):
    shape('AisleGoldBorder', (x, .012, 1), (.045, .015, 20), 'BrassGold')
for z in (-5, -1, 3, 7, 11):
    shape('CarpetDiamond', (0, .014, z), (.65, .015, .65), 'BrassGold', yaw=45)

# Tables retain the shared kit's real-world dimensions and green felt.
for x, z in [(-3.6, -1), (3.6, -1), (-3.6, 3.5), (3.6, 3.5), (3.6, 8)]:
    prefab('Kit_Table_Round', (x, 0, z))
    for angle in (45, 165, 285):
        rad = math.radians(angle)
        prefab('Kit_Chair', (x + 1.35 * math.sin(rad), 0, z + 1.35 * math.cos(rad)), angle + 180)
    for offset in (-.3, .3):
        shape('ChipStack', (x + offset, .79, z), (.08, .035, .08), 'ChipRed' if offset < 0 else 'ChipWhite', mesh=10206)
    shape('PlayingCard', (x, .761, z + .25), (.09, .005, .13), 'ChipWhite', yaw=15)

# Slot banks: dark cabinets, brass bezels, cyan screens and colored reels.
for side in (-1, 1):
    for z in (-4.5, -2.8, -1.1, .6, 2.3, 4):
        x = side * 7.5
        shape('SlotCabinet', (x, .95, z), (.72, 1.9, .7), 'ChipBlack')
        shape('SlotGoldBezel', (x - side * .375, 1.36, z), (.05, .86, .62), 'BrassGold')
        shape('SlotScreen', (x - side * .407, 1.36, z), (.025, .71, .49), 'ScreenGlow')
        for dz in (-.15, 0, .15):
            shape('SlotReel', (x - side * .425, 1.35, z + dz), (.015, .22, .10), 'ChipWhite' if dz else 'ChipRed')
        shape('SlotConsole', (x - side * .45, .87, z), (.28, .09, .68), 'BrassGold')
        shape('SlotMarquee', (x, 2.04, z), (.8, .2, .76), 'BrassGold')

# Rear left lounge/bar and cashier counter on the rear right.
shape('BarBase', (-5.8, .5, 9), (4.5, 1, .85))
shape('BarTop', (-5.8, 1.04, 9), (4.8, .12, 1.05), 'ChipBlack')
shape('BarGoldFace', (-5.8, .72, 8.55), (4.4, .06, .04), 'BrassGold')
for x in (-7.3, -6.3, -5.3, -4.3):
    shape('BarStoolStem', (x, .35, 7.9), (.12, .7, .12), 'BrassGold', mesh=10206)
    shape('BarStoolSeat', (x, .72, 7.9), (.45, .05, .45), 'FabricBurgundy', mesh=10206)
    shape('BarStoolFoot', (x, .04, 7.9), (.42, .04, .42), 'BrassGold', mesh=10206)
shape('BarBackDisplay', (-5.8, 2.1, 12.7), (4.9, 1.9, .18), 'WoodDark')
for y in (1.35, 2, 2.65):
    shape('BottleShelf', (-5.8, y, 12.3), (4.8, .08, .6), 'BrassGold')
    for i in range(7):
        shape('Bottle', (-7.6 + i * .6, y + .22, 12.3), (.13, .18, .13), 'FeltGreen' if i % 2 else 'ChipRed', mesh=10206)
shape('CashierCounter', (6.7, .53, 10), (3.2, 1.06, 1), 'WoodDark')
shape('CashierTop', (6.7, 1.1, 10), (3.45, .12, 1.15), 'BrassGold')
shape('CashierBackboard', (6.7, 2.2, 12.75), (3.6, 1.35, .15), 'CarpetRed')
for x in (5.1, 5.9, 6.7, 7.5, 8.3):
    shape('CashierBrassBars', (x, 1.95, 10), (.035, 1.55, .035), 'BrassGold')
shape('CashierHeader', (6.7, 2.75, 10), (3.4, .2, .15), 'BrassGold')

# VIP portal and a centered art-deco feature above the main aisle.
shape('VIPDoor', (-1.8, 1.2, 12.72), (1.9, 2.4, .12), 'WoodDark')
for x in (-2.83, -.77):
    shape('VIPGoldFrame', (x, 1.35, 12.58), (.12, 2.7, .22), 'BrassGold')
shape('VIPLintel', (-1.8, 2.65, 12.58), (2.2, .16, .22), 'BrassGold')
shape('VIPDoorInset', (-1.8, 1.4, 12.64), (1.5, 1.7, .035), 'FabricBurgundy')
shape('VIPHandle', (-1.1, 1.1, 12.55), (.05, .3, .05), 'BrassGold')
shape('VIPReader', (-.5, 1.3, 12.5), (.12, .2, .05), 'ScreenGlow')
shape('FeatureSignBacking', (0, 3.28, 12.65), (5, .85, .12), 'ChipBlack')
for x in (-2.6, 2.6):
    shape('SignGoldEdge', (x, 3.28, 12.55), (.08, 1, .08), 'BrassGold')
for x in (-1.6, -.8, 0, .8, 1.6):
    shape('ArtDecoDiamond', (x, 3.28, 12.53), (.37, .37, .06), 'BrassGold', yaw=45)

# Chandeliers: brass rings and warm-colored lamp globes, no extra lights.
for z in (-2, 4, 9):
    shape('ChandelierStem', (0, 3.8, z), (.065, .6, .065), 'BrassGold', mesh=10206)
    shape('ChandelierHub', (0, 3.48, z), (.32, .12, .32), 'BrassGold', mesh=10206)
    for angle in range(0, 360, 60):
        rad = math.radians(angle)
        x, dz = .8 * math.sin(rad), .8 * math.cos(rad)
        shape('ChandelierArm', (x / 2, 3.48, z + dz / 2), (.045, .045, .85), 'BrassGold', yaw=angle)
        shape('ChandelierLamp', (x, 3.48, z + dz), (.2, .3, .2), 'PlasterCream', mesh=10207)

parts.append('--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n' + ''.join(f'  - {{fileID: {i}}}\n' for i in roots))
SCENE.write_text(''.join(parts), encoding='utf-8', newline='\n')
subprocess.run([sys.executable, str(ROOT / 'tools/check_casino_collisions.py'), '--fix'], check=True)
print(f'Built {SCENE.relative_to(ROOT)} with {len(roots)-2} decorative roots.')
