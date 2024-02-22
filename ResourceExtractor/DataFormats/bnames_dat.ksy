meta:
  id: bnames
  file-extension: dat
  endian: le
doc: Bitmap filenames and color sets for creatures.
seq:
  - id: num_creatures
    type: u2
  - id: creatures
    type: creature
    repeat: expr
    repeat-expr: num_creatures
types:
  creature:
    seq:
      - id: offset
        type: u2
    instances:
      entry:
        pos: offset + _parent.num_creatures * 2 + 4
        type: instance
  instance:
    seq:
      - id: bmx_base_file_name
        type: strz
        encoding: ascii
      - id: bmx_file_suffix_1
        type: u1
      - id: bmx_file_suffix_2
        type: u1
      - id: bmx_file_suffix_3
        type: u1
      - id: color_set
        type: s1
        doc: CSx.DAT color set file to apply to the bitmaps
