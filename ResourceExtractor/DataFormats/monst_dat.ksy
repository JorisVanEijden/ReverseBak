meta:
  id: monst
  file-extension: dat
  endian: le
doc: Contains monster data for combat encounters
seq:
  - id: number_0
    type: min_max
  - id: number_1
    type: min_max
  - id: number_2
    type: min_max
  - id: number_3
    type: min_max
  - id: number_5
    type: min_max
  - id: number_6
    type: min_max
  - id: number_7
    type: min_max
  - id: number_4
    type: min_max
  - id: negative_one
    type: min_max
    repeat: expr
    repeat-expr: 4
types:
  min_max:
    seq:
      - id: min
        type: u2
      - id: max
        type: u2
