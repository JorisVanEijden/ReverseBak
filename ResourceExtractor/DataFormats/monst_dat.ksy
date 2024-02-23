meta:
  id: monst
  file-extension: dat
  endian: le
doc: Contains monster statistics for combat encounters. The number in the filename corresponds to the monster's ID.
  | each field is filled with a random number between the given min and max when a combat is loaded
seq:
  - id: health
    type: min_max
  - id: stamina
    type: min_max
  - id: speed
    type: min_max
  - id: strength
    type: min_max
  - id: accuracy_crossbow
    type: min_max
  - id: accuracy_melee
    type: min_max
  - id: accuracy_casting
    type: min_max
  - id: defense
    type: min_max
  - id: combat_data_field_f
    type: min_max
  - id: combat_data_field_10
    type: min_max
  - id: combat_data_field_11
    type: min_max
  - id: combat_data_field_e
    type: min_max
types:
  min_max:
    seq:
      - id: min
        type: u2
      - id: max
        type: u2
