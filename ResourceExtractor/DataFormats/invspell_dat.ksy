meta:
  id: invspell
  file-extension: dat
  endian: le
doc: This contains the "grouping" of the spells.
seq:
  - id: spell_groups
    type: spell_group
    repeat: expr
    repeat-expr: 6
types:
  spell_group:
    seq:
      - id: group_icon
        type: u2
      - id: num_in_group
        type: u2
      - id: spells
        type: spell
        repeat: expr
        repeat-expr: num_in_group
  spell:
    seq:
      - id: name
        type: str
        encoding: ascii
        size: 0x18
      - id: spell_nr
        type: u2
