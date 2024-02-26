meta:
  id: world_items
  file-extension: wld
  endian: le
doc: Contains type, rotation and positions of the items in a tile.
  | The file names consist of Tzzxxyy.WLD where z is the zone number and x and y
  | are the tile coordinates
seq:
  - id: world_items
    type: world_item
    repeat: eos
types:
  world_item:
    seq:
      - id: type
        type: u2
      - id: rotation
        type: rotation_3d
      - id: position
        type: position_3d
  rotation_3d:
    seq:
      - id: x
        type: u2
      - id: y
        type: u2
      - id: z
        type: u2
  position_3d:
    seq:
      - id: x
        type: u4
      - id: y
        type: u4
      - id: z
        type: u4
