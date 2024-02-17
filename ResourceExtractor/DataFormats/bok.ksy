meta:
  id: book
  title: Book data file
  file-extension: bok
  endian: le
doc: BOK files
seq:
  - id: size
    type: u4
    doc: total file size
  - id: nr_of_pages
    type: u2
    doc: Number of pages in the file
  - id: page_offsets
    type: s4
    repeat: expr
    repeat-expr: nr_of_pages
  - id: pages
    type: bok_page
    repeat: expr
    repeat-expr: "1"
types:
  bok_page:
    seq:
      - id: x_offset
        type: s2
      - id: y_offset
        type: s2
      - id: width
        type: u2
      - id: height
        type: u2
      - id: global_page_number
        type: s2
      - id: book_page_number
        type: s2
      - id: previous_page_number
        type: s2
      - id: next_page_number
        type: s2
      - id: next_page_number2
        type: s2
        doc: not exactly sure what the difference is with the other next_page_number
      - id: unknown_1
        type: s2
        doc: Always 0 in data, code seems to do something with how paragraphs are spread over the pages
      - id: number_of_images
        type: u2
      - id: number_of_reserved_areas
        type: u2
      - id: show_page_number
        type: u2
      - id: placeholder_for_pointer
        type: u4
      - id: current_paragraph
        type: paragraph_info
      - id: current_text_segment
        type: text_segment_info
      - id: reserved_areas
        type: reserved_area
        repeat: expr
        repeat-expr: number_of_reserved_areas
      - id: images
        type: image
        repeat: expr
        repeat-expr: number_of_images
      - id: p_marker
        type: u1
      - id: paragraphs
        type: paragraph
        repeat: expr
        repeat-expr: 2
  paragraph:
    seq:
      - id: info
        type: paragraph_info
      - id: text_segments
        type: text_segment_wrapper
  text_segment_wrapper:
    seq:
      - id: wrapped
        type: text_segment
        if: this_type == 0xF4
    instances:
      this_type:
        pos: _io.pos
        type: u1
  text_segment:
    seq:
      - id: marker
        type: u1
      - id: info
        type: text_segment_info
      - id: text
        type: u1
        repeat: until
        repeat-until: _ & 0xF0 == 0xF0
    instances:
      next:
        pos: _io.pos - 1
        type: u1
  paragraph_info:
    seq:
      - id: x_offset
        type: s2
      - id: width
        type: s2
      - id: line_spacing
        type: s2
      - id: word_spacing
        type: s2
      - id: start_indentation
        type: s2
      - id: unknown1
        type: s2
        doc: Always 0 in data. Code seems to indicate something with horizontal spacing but messing with it doesn't seem to change anything.
      - id: y_offset
        type: s2
      - id: alignment
        type: s2
        enum: text_alignment
    enums:
      text_alignment:
        1: left
        2: right
        3: justify
        4: center
  text_segment_info:
    seq:
      - id: font_number
        type: s2
      - id: y_offset
        type: s2
      - id: color_index
        type: u2
      - id: unknown
        type: s2
        doc: Always 0. Game uses a byte from this field, but it's not clear what for. Messing with it doesn't seem to change anything.
      - id: font_style
        type: u2
  reserved_area:
    seq:
      - id: x
        type: s2
      - id: y
        type: s2
      - id: width
        type: s2
      - id: height
        type: s2
  image:
    seq:
      - id: x
        type: s2
      - id: y
        type: s2
      - id: image_number
        type: s2
      - id: mirroring
        type: u2
        enum: mirroring
    enums:
      mirroring:
        0: none
        1: horizontal
        2: vertical
