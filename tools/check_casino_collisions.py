"""Audit solid Casino geometry; --fix aligns cylinder box colliders to their mesh."""
from pathlib import Path
import re
import sys

scene = Path(__file__).resolve().parents[1] / 'Assets/Scenes/Casino_Test/Casino.unity'
text = scene.read_text(encoding='utf-8')
blocks = re.split(r'(?=^--- !u!)', text, flags=re.M)
objects, meshes, colliders = {}, {}, {}
for index, block in enumerate(blocks):
    header = re.match(r'--- !u!(\d+) &(-?\d+)', block)
    if not header:
        continue
    kind, identifier = map(int, header.groups())
    if kind == 1 and 'stripped' not in block.splitlines()[0]:
        objects[identifier] = re.search(r'  m_Name: (.*)', block)[1]
    owner = re.search(r'm_GameObject: \{fileID: (\d+)\}', block)
    if not owner:
        continue
    owner = int(owner[1])
    if kind == 33:
        meshes[owner] = int(re.search(r'm_Mesh: \{fileID: (\d+)', block)[1])
    if kind in (64, 65, 135, 136):
        colliders.setdefault(owner, []).append((index, kind))

errors = []
fixed = 0
next_id = max(int(value) for value in re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M)
              if int(value) != 9223372036854775807) + 1
mesh_template = next(block for block in blocks if block.startswith('--- !u!64 '))
for owner, mesh in meshes.items():
    name = objects.get(owner, str(owner))
    solid = [(index, kind) for index, kind in colliders.get(owner, [])
             if 'm_IsTrigger: 0' in blocks[index] and 'm_Enabled: 1' in blocks[index]]
    if not solid:
        if '--fix' in sys.argv and not colliders.get(owner):
            collider = re.sub(r'^--- !u!64 &-?\d+', f'--- !u!64 &{next_id}', mesh_template)
            collider = re.sub(r'm_GameObject: \{fileID: \d+\}', f'm_GameObject: {{fileID: {owner}}}', collider)
            collider = re.sub(r'm_Mesh: \{fileID: \d+,', f'm_Mesh: {{fileID: {mesh},', collider)
            for index, block in enumerate(blocks):
                if block.startswith(f'--- !u!1 &{owner}\n'):
                    blocks[index] = block.replace('  m_Layer:', f'  - component: {{fileID: {next_id}}}\n  m_Layer:', 1)
                    break
            blocks.append(collider)
            next_id += 1
            fixed += 1
        else:
            errors.append(f'{name}: missing enabled solid collider')
    for index, kind in solid:
        if mesh == 10206 and kind == 65:
            expected = 'm_Size: {x: 1, y: 2, z: 1}'
            if expected not in blocks[index]:
                if '--fix' in sys.argv and 'm_Size: {x: 1, y: 1, z: 1}' in blocks[index]:
                    blocks[index] = blocks[index].replace(
                        'm_Size: {x: 1, y: 1, z: 1}', expected)
                    fixed += 1
                else:
                    errors.append(f'{name}: cylinder collider does not cover full mesh height')

if fixed:
    scene.write_text(''.join(blocks), encoding='utf-8', newline='\n')
print(f'Checked {len(meshes)} mesh objects; corrected {fixed} colliders.')
if errors:
    print('\n'.join(errors))
    sys.exit(1)
print('All Casino meshes have enabled, non-trigger colliders with full cylinder height.')
