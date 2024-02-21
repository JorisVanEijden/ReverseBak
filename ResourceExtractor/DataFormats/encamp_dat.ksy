meta:
  id: encamp
  file-extension: dat
  endian: le
doc: Used by the encamp screen, this appears to be mainly used to plot the hours on the dial where you choose how long to rest.
seq:
  - id: unknown1
    type: u2
  - id: unknown2
    type: u2
  - id: unknown3
    type: u2
  - id: unknown4
    type: u2
  - id: num_a_entries
    type: u2
  - id: entries_a
    type: buffer
    repeat: expr
    repeat-expr: num_a_entries
  - id: num_b_entries
    type: u2
  - id: entries_b
    type: buffer
    repeat: expr
    repeat-expr: num_b_entries
types:
  buffer:
    seq:
      - id: x
        type: u2
      - id: y
        type: u2
