meta:
  id: chap_shp
  file-extension: dat
  imports:
    - creature_name
  endian: le
doc: Contains the starting characters for each chapter
seq:
  - id: party_members
    type: characters
    repeat: eos
types:
  characters:
    seq:
      - id: party_member_1
        type: creature_name
      - id: party_member_2
        type: creature_name
      - id: party_member_3
        type: creature_name
