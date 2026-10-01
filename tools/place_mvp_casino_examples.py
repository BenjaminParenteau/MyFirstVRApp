"""Place linked casino prefab examples in the existing MVP template."""
from pathlib import Path
import math
import re

root = Path(__file__).resolve().parents[1]
path = root / 'Assets/Scenes/MVP/MVP_Template.unity'
scene = path.read_text()
assert 'm_Name: CasinoExamples' not in scene, 'Examples already placed; use Unity to edit them.'
solo_go = re.search(r'--- !u!1 &(\d+)\nGameObject:\n(?:(?!^---).)*?m_Name: _SoloTest\n', scene, re.M | re.S)[0]
solo = re.search(r'component: {fileID: (\d+)}', solo_go)[1]
group_go, group_t = 8000000000, 8000000001
placements = [('Kit_CasinoTableSet',(2.5,0,1.5),0),('Kit_CashierCounter',(-3.5,0,1.5),0)]
placements += [('Kit_SlotMachine',(7.5,0,-1+i*2),180) for i in range(3)]
placements += [('Kit_BarCounter',(2.5,0,-4),0)]
placements += [('Kit_BarStool',(1.5+i,0,-3),0) for i in range(3)]
placements += [('Kit_Chandelier',(2.5,2.38,1.5),0)]
placements += [('Kit_CasinoWallPanel',(-4+i*2,0,3.5),0) for i in range(3)]
children=[]
output=[]
for index,(name,pos,yaw) in enumerate(placements):
    prefab=root/'Assets/Content/StyleKit/Prefabs'/f'{name}.prefab'
    text=prefab.read_text()
    guid=re.search(r'guid: (\w+)',Path(str(prefab)+'.meta').read_text())[1]
    transform=re.search(r'--- !u!4 &(\d+)\nTransform:\n(?:(?!^---).)*?m_Father: {fileID: 0}',text,re.M|re.S)
    source_t=transform[1]
    instance=8000000100+index*2
    child=instance+1
    children.append(child)
    values=dict(zip(('m_LocalPosition.x','m_LocalPosition.y','m_LocalPosition.z'),pos))
    values.update({'m_LocalRotation.x':0,'m_LocalRotation.y':math.sin(math.radians(yaw/2)), 'm_LocalRotation.z':0,'m_LocalRotation.w':math.cos(math.radians(yaw/2))})
    mods=''.join(f'    - target: {{fileID: {source_t}, guid: {guid}, type: 3}}\n      propertyPath: {prop}\n      value: {value:.8g}\n      objectReference: {{fileID: 0}}\n' for prop,value in values.items())
    output.append(f'''--- !u!1001 &{instance}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: {group_t}}}
    m_Modifications:
{mods}    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}
--- !u!4 &{child} stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {source_t}, guid: {guid}, type: 3}}
  m_PrefabInstance: {{fileID: {instance}}}
  m_PrefabAsset: {{fileID: 0}}
''')
template=re.search(r'--- !u!1 &545395218\n.*?(?=^--- !u!1001)',scene,re.M|re.S)[0]
template=template.replace('545395218',str(group_go)).replace('545395219',str(group_t))
template=re.sub(r'm_Name: .*','m_Name: CasinoExamples',template)
template=template.replace('m_Father: {fileID: 0}',f'm_Father: {{fileID: {solo}}}')
template=template.replace('m_Children: []','m_Children:\n'+'\n'.join(f'  - {{fileID: {c}}}' for c in children))
solo_block=re.search(r'--- !u!4 &'+solo+r'\n.*?(?=^---)',scene,re.M|re.S)[0]
updated=solo_block.replace('m_Children:\n',f'm_Children:\n  - {{fileID: {group_t}}}\n')
assert updated != solo_block
scene=scene.replace(solo_block,updated)
scene=scene.replace('--- !u!1660057539',template+''.join(output)+'--- !u!1660057539')
path.write_text(scene,encoding='utf-8',newline='\n')
print(f'Placed {len(placements)} linked prefabs under _SoloTest/CasinoExamples.')
