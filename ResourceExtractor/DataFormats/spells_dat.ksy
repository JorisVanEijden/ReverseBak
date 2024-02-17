meta:
  id: spell_data
  file-extension: dat
  endian: le
seq:
  - id: num_spells
    type: u2
  - id: spells
    type: spell
    repeat: expr
    repeat-expr: num_spells
types:
  spell:
    seq:
      - id: spell_name_offset
        type: s2
      - id: minimum_cost
        type: s2
      - id: maximum_cost
        type: s2
      - id: is_martial
        type: s2
      - id: targeting_type
        type: s2
      - id: color
        type: s2
        doc: Color of any related effect sprites, e.g. sparks of flamecast, mind melt color
      - id: animation_effect_type
        type: s2
      - id: object_id
        type: s2
        doc: object required to cast spell
      - id: calculation
        type: u2
        enum: spell_calculation
      - id: damage
        type: s2
      - id: duration
        type: s2
    enums:
      spell_calculation:
        0: non_cost_related
        1: fixed_amount
        2: cost_times_damage
        3: cost_times_duration
        4: special_1
        5: special_2
    instances:
      name:
        pos: spell_name_offset + 4 + _parent.num_spells * 22
        type: strz
        encoding: ASCII
