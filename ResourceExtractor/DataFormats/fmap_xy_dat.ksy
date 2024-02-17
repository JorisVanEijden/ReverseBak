meta:
  id: fmap_xy
  file-extension: dat
  endian: le
seq:
  - id: zones
    type: zone
    repeat: eos
types:
  zone:
    seq:
      - id: num_tiles
        type: u2
      - id: positions
        type: tile_position
        repeat: expr
        repeat-expr: num_tiles
  tile_position:
    seq:
      - id: x
        type: s2
      - id: y
        type: s2
