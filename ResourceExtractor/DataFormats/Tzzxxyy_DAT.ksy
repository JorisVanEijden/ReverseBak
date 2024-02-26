meta:
  id: tiles
  file-extension: dat
  endian: le
doc: Tile encounter data, not sure about lot of the fields and there is unknown data that the game doesn't 
  | seem to use after the known data in the 192 byte blocks.
  | The file names consist of Tzzxxyy.DAT where z is the zone number and x and y
  | are the tile coordinates
seq:
  - id: chapter_tiles
    type: tile
    size: 192
    repeat: eos
types:
  tile:
    seq:
      - id: num_defs
        type: u2
      - id: file_defs
        type: file_def
        repeat: expr
        repeat-expr: num_defs
  file_def:
    seq:
      - id: type
        type: u2
        enum: def_files
      - id: x_start
        type: u1
      - id: y_end
        type: u1
      - id: x_end
        type: u1
      - id: y_start
        type: u1
      - id: entry_number
        type: u4
      - id: unknown_a
        type: u1
      - id: global_key_1
        type: u2
      - id: global_key_2
        type: u2
      - id: global_key_3
        type: u2
      - id: unknown_11
        type: u2
    enums:
      def_files:
        0: bkgr
        1: comb
        2: comm
        3: dial
        4: heal
        5: soun
        6: town
        7: trap
        8: zone
        9: disa
        10: enab
        11: bloc
