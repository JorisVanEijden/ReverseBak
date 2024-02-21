meta:
  id: req
  file-extension: dat
  endian: le
doc: These are files for the UI of the game. They are used to define the UI elements and their properties.
seq:
  - id: ui_type
    type: u2
  - id: is_modal
    type: u2
  - id: color
    type: u2
  - id: x_position
    type: u2
  - id: y_position
    type: u2
  - id: width
    type: u2
  - id: height
    type: u2
  - id: placeholder_num_elements
    type: u2
  - id: placeholder_pointer_elements
    type: u2
  - id: offset_title
    type: s2
  - id: x_offset
    type: s2
  - id: y_offset
    type: s2
  - id: p_bitmap
    type: u4
  - id: num_ui_elements
    type: u2
  - id: ui_elements
    type: ui_element
    repeat: expr
    repeat-expr: num_ui_elements
  - id: len_string_buffer
    type: u2
  - id: string_buffer
    size: len_string_buffer
instances:
  string_buffer_offset:
    value: 32 + 33 * num_ui_elements
  title:
    pos: string_buffer_offset + offset_title
    type: strz
    encoding: ascii
    if: offset_title >= 0
types:
  ui_element:
    seq:
      - id: element_type
        type: u2
        enum: element_types
      - id: action_id
        type: s2
      - id: is_visible
        type: u1
      - id: colors
        type: u2
      - id: field_7
        type: u2
      - id: field_9
        type: u2
      - id: x_position
        type: u2
      - id: y_position
        type: u2
      - id: width
        type: u2
      - id: height
        type: u2
      - id: field_13
        type: s2
      - id: offset_label
        type: s2
      - id: teleport
        type: s2
      - id: icons
        type: s2
        doc: Half of this is the inactive icon, taken from bicons1, the other half is the active icon, taken from bicons2.
          So 8 = 4th icon from both. 7 = 3rd icon from bicons1, 4rd icon from bicons2. Except for icons over 50, which one less of bicons1 is taken
      - id: cursor
        type: s2
      - id: field_1d
        type: u2
      - id: sound
        type: s2
    instances:
      label:
        pos: _parent.string_buffer_offset + offset_label
        type: strz
        encoding: ascii
        if: offset_label >= 0
    enums:
      element_types:
        0: click_area
        1: input_field
        2: file_picker
        3: image_button
        4: toggle
        5: text_link
        6: text_button
        