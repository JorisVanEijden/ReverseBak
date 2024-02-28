meta:
  id: chap_shp
  file-extension: dat
  endian: le
doc: Contains the starting characters for each chapter
seq:
  - id: party_members
    type: chapter
    repeat: eos
types:
  chapter:
    seq:
      - id: party_member_1
        type: s2
        enum: characters
      - id: party_member_2
        type: s2
        enum: characters
      - id: party_member_3
        type: s2
        enum: characters
    enums:
      characters:
        15: gorath
        16: owyn
        17: locklear
        45: pug
        47: patrus
        51: james
        