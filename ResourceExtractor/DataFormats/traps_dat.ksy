meta:
  id: traps
  file-extension: dat
  endian: le
doc: Trap data
seq:
  - id: traps
    type: trap
    size: 62
    repeat: expr
    repeat-expr: 379
types:
  trap:
    seq:
      - id: num_elements
        type: u2
      - id: elements
        type: trap_element
        repeat: expr
        repeat-expr: num_elements - 1
  trap_element:
    seq:
      - id: type
        type: s2
        enum: element_type
      - id: x
        type: u1
      - id: y
        type: u1
    enums:
      element_type:
        -17: actor_0
        -16: actor_1
        -15: actor_2
        -13: cannon_south
        -12: cannon_north
        -11: cannon_east
        -10: cannon_west
        -6: exit
        0: unknown
        7: red_crystal
        8: green_crystal
        9: diamond_solid
        10: diamond_passthrough
