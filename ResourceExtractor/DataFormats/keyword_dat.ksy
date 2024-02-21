meta:
  id: keywords
  file-extension: dat
  endian: le
doc: a simple list of keywords, indexed by a number
seq:
  - id: size
    type: u2
  - id: num_offsets
    type: u2
  - id: offsets
    type: u2
    repeat: expr
    repeat-expr: num_offsets
instances:
  keywords:
    type: keyword(_index)
    repeat: expr
    repeat-expr: num_offsets
types:
  keyword:
    params:
      - id: number
        type: u2
    instances:
      text:
        pos: _parent.offsets[number]
        type: strz
        encoding: ascii
