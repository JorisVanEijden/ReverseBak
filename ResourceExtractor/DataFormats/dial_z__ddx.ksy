meta:
  id: dial_z__
  file-extension: ddx
  endian: le
doc: Dialog structures
seq:
  - id: num_entries
    type: u2
  - id: keys
    type: key
    repeat: expr
    repeat-expr: num_entries
  - id: dialog_entries
    type: dialog_entry
    repeat: eos

types:
  key:
    seq:
      - id: id
        type: u4
      - id: offset
        type: s4
  dialog_entry:
    seq:
      - id: display_style
        type: u1
      - id: actor
        type: u2
      - id: display_style_2
        type: u1
      - id: display_style_3
        type: u1
      - id: num_choices
        type: u1
      - id: num_actions
        type: u1
      - id: string_length
        type: u2
      - id: choices
        type: choice
        repeat: expr
        repeat-expr: num_choices
      - id: actions
        type: action
        repeat: expr
        repeat-expr: num_actions
      - id: text
        type: strz
        encoding: ascii
        size: string_length
  choice:
    seq:
      - id: state
        type: u2
      - id: choice_0
        type: u2
      - id: choice_1
        type: u2
      - id: offset
        type: u4
  action:
    seq:
      - id: action_type
        type: u2
      - id: action
        type:
          switch-on: action_type
          cases:
            1: set_text_variable
            2: give_item
            3: remove_item
            4: set_global_value
            6: resize_dialog
            8: apply_condition
        size: 8
  resize_dialog:
    seq:
      - id: x
        type: u2
      - id: y
        type: u2
      - id: width
        type: u2
      - id: height
        type: u2
  apply_condition:
    seq:
      - id: target
        type: u2
      - id: condition
        type: u2
      - id: minimum_amount
        type: s2
      - id: maximum_amount
        type: s2
  set_text_variable:
    seq:
      - id: slot
        type: u2
      - id: source
        type: u2
      - id: unknown
        type: u2
  give_item:
    seq:
      - id: object_id
        type: u1
      - id: actor
        type: u1
      - id: amount
        type: u2
  remove_item:
    seq:
      - id: object_id
        type: u2
      - id: amount
        type: u2
  set_global_value:
    seq:
      - id: key
        type: u2
      - id: mask
        type: u1
      - id: data
        type: u1
      - id: unused
        type: u2
      - id: value
        type: u2
