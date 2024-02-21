meta:
  id: fmap_twn
  file-extension: dat
  endian: le
doc: Used by the map screen, this appears to be a list of towns names and map coordinates.
seq:
  - id: unknown1
    type: u2
  - id: unknown2
    type: u2
  - id: unknown3
    type: u2
  - id: unknown4
    type: u2
  - id: num_town
    type: u2
  - id: town
    type: town
    repeat: expr
    repeat-expr: num_town
types:
  town:
    seq:
      - id: str_len
        type: u2
      - id: name
        type: str
        encoding: ascii
        size: str_len
      - id: x
        type: s2
      - id: y
        type: s2
