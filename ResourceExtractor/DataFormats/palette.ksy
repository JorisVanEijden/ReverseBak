meta:
  id: palette
  file-extension: pal
  endian: le
doc: Color palette. Not entirely correct since this only takes VGA palettes into account.
seq:
  - id: main_tag
    contents: 'PAL:'
  - id: file_size
    type: u2
  - id: unknown1
    type: u2
  - id: vga_tag
    contents: 'VGA:'
  - id: chunk_size
    type: u4
  - id: colors
    type: color
    repeat: expr
    repeat-expr: 256
types:
  color:
    seq:
      - id: red
        type: u1
      - id: green
        type: u1
      - id: blue
        type: u1
