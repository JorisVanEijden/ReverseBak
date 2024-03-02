meta:
  id: z__shp
  file-extension: dat
  imports:
    - creature_name
  endian: le
doc: Contains the zone's monsters for each chapter.
seq:
  - id: chapters
    type: creatures
    repeat: eos
types:
  creatures:
    seq:
      - id: slot1
        type: creature_name
      - id: slot2
        type: creature_name
      - id: slot3
        type: creature_name
      - id: slot4
        type: creature_name
