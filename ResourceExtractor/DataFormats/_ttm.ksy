meta:
  id: animator_script
  file-extension: ttm
  endian: le
doc: Animator scripts
seq:
  - id: version_tag
    type: version
  - id: pag_tag
    type: page
  - id: tt3_tag
    type: tt3
  - id: tti_tag
    type: tti
types:
  version:
    seq:
      - id: tag
        contents: 'VER:'
      - id: num_bytes
        type: u2
      - id: zero
        type: u2
      - id: version
        type: strz
        encoding: ascii
  page:
    seq:
      - id: tag
        contents: 'PAG:'
      - id: num_bytes
        type: u2
      - id: unknown
        type: u2
      - id: num_frames
        type: u2
  tt3:
    seq:
      - id: tag
        contents: 'TT3:'
      - id: num_bytes
        type: u2
      - id: unknown
        type: u2
      - id: compression_type
        type: u1
      - id: decompressed_size
        type: u4
      - id: compressed_data
        size: num_bytes - 5
        process: rle.decompress
  tti:
    seq:
      - id: tag
        contents: 'TTI:'
      - id: num_bytes
        type: u2
      - id: unknown
        type: u2
      - id: tag_tag
        type: tag
  tag:
    seq:
      - id: tag_tag
        contents: 'TAG:'
      - id: num_bytes2
        type: u2
      - id: unknown2
        type: u2
      - id: num_tags
        type: u2
      - id: commands
        type: command
        repeat: expr
        repeat-expr: num_tags
  command:
    seq:
      - id: number
        type: u2
      - id: command
        type: strz
        encoding: ascii