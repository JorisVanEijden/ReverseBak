meta:
  id: z__map
  file-extension: dat
  endian: le
doc: These are the zone maps. Each file holds 50 rows of 64 bits of data.
  | The game takes the byte at Y*8 + X/8 and uses the X%8 bit to determine if the tile is part of this zone.
seq:
  - id: zone_maps
    size: 400
