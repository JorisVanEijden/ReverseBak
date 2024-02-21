meta:
  id: gds_
  file-extension: dat
  endian: le
doc: Could be something to do with encounters.
seq:
  - id: size_in_bytes
    type: s2
  - id: gds
    type: gds_data
    size: size_in_bytes
types:
  gds_data:
    seq:
      - id: label
        type: str
        encoding: ascii
        size: 13
      - id: some_flag
        type: u2
      - id: field_f
        type: u2
      - id: song
        type: s2
      - id: field_13
        type: u2
      - id: field_15
        type: u2
      - id: field_17
        type: u2
      - id: field_19
        type: u2
      - id: num_something
        type: u2
      - id: unknown2
        size: 4
      - id: p_dialog_entry
        type: u4
      - id: p_container
        type: u4
      - id: array_something
        type: gds_struct36
        repeat: expr
        repeat-expr: num_something
  gds_struct36:
    seq:
      - id: x_position
        type: s2
      - id: y_position
        type: s2
      - id: width
        type: u2
      - id: height
        type: u2
      - id: flags
        type: u2
      - id: field_a
        type: s2
      - id: field_c
        type: u1
      - id: field_d
        type: u1
      - id: field_e
        type: u2
      - id: field_10
        type: u2
      - id: dialog_id_1
        type: u4
      - id: dialog_id_2
        type: u4
      - id: p_dialog_entry
        type: u4
      - id: global_key
        type: u2
      - id: min_global_value
        type: u2
      - id: max_global_value
        type: u2

      
        
      
