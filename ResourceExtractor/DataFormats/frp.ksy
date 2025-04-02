meta:
  id: sounds
  file-extension: sx
  endian: le

seq:
  - id: file_header
    type: file_header

  - id: inf_header
    type: inf_header

  - id: inf_block
    type: inf_block

  - id: sound_entries
    type: sound_entry
    repeat: expr
    repeat-expr: inf_block.nr_of_sounds

types:
  file_header:
    seq:
      - id: magic_snd
        contents: 'SND:'
      - id: file_size
        type: u4

  inf_header:
    seq:
      - id: magic_inf
        contents: 'INF:'
      - id: inf_block_size
        type: u4

  inf_block:
    seq:
      - id: version
        type: u2
      - id: nr_of_sounds
        type: u2
      - id: unknown_byte
        type: u1

  sound_entry:
    seq:
      - id: sound_id
        type: u2
      - id: offset
        type: u4
    instances:
      dat_block:
        type: dat_block
        pos: offset

  dat_block:
    seq:
      - id: magic_dat
        contents: 'DAT:'
      - id: size
        type: u4
      - id: sound_id
        type: u2
      - id: field_c
        type: u1
      - id: field_12
        type: u1
      - id: data
        type: resource
        size: size - 4

  resource:
    seq:
      - id: data_type_and_flags
        type: u1
      - id: uncompressed_size
        type: u4
      - id: unknown_byte_84
        type: u1
      - id: unknown_byte_var3
        type: u1
      - id: unknown_byte_var1
        type: u1
    instances:
      file_handler_type:
        value: data_type_and_flags & 0x1F
      flag_1:
        value: (data_type_and_flags >> 5) & 1
      flag_2:
        value: (data_type_and_flags >> 6) & 1
