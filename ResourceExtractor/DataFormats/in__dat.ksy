meta:
  id: in_
  file-extension: dat
  endian: le
doc: this appear to be mostly for the editor, containing labels. It's also used by the game in in_save.dat. 
seq:
  - id: stringbuffer_len
    type: u2
  - id: stringbuffer
    size: stringbuffer_len
  - id: amount
    type: u2
  - id: entries
    type: entry
    repeat: expr
    repeat-expr: amount
types:
  entry:
    seq:
      - id: unknown_0
        type: u2
      - id: unknown_2
        type: u2
      - id: unknown_4
        type: u2
      - id: unknown_6
        type: u2
      - id: unknown_8
        type: u1
      - id: unknown_9
        type: u1
      - id: unknown_a
        type: u1
      - id: unknown_b
        type: u1
      - id: unknown_c
        type: u1
      - id: off_string
        type: u2
      - id: unknown_10
        type: u2
      - id: unknown_12
        type: u2
      - id: unknown_14
        type: u2
    instances:
      label:
        pos: off_string + 2
        type: strz
        encoding: ascii