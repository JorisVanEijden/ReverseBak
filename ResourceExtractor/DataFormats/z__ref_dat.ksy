meta:
  id: z__ref
  file-extension: dat
  endian: le
doc: Contains the coordinates of the tiles for this zone.
seq:
  - id: num_tiles
    type: u1
  - id: tiles
    type: tile
    repeat: expr
    repeat-expr: num_tiles
types:
  tile:
    seq:
      - id: x
        type: u1
      - id: y
        type: u1
