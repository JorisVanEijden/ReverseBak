meta:
  id: teleport
  file-extension: dat
  endian: le
doc: Contains teleport destinations.
seq:
  - id: teleport_entries
    type: teleport_entry
    repeat: eos
types:
  teleport_entry:
    seq:
      - id: location
        type: location
      - id: gds_number
        type: u2
      - id: gds_letter
        type: u2
  location:
    seq:
      - id: zone_number
        type: u1
      - id: tile_x
        type: u1
      - id: tile_y
        type: u1
      - id: type_x_offset
        type: u1
      - id: type_y_offset
        type: u1
      - id: z_rotation
        type: u2
