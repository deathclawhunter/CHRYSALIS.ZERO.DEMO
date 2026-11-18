Unity terrain texture pack
==========================

Four equal-sized RGB PNG textures, each 625 x 625 pixels:
- base_cracked_stone.png
- mossy_stone.png
- rocky_gravel.png
- worn_concrete.png

Each texture includes a 3x3 repeat preview.
corrected_2x2_atlas.png is a convenience atlas.
four_texture_repeat_overview.png shows all four repeat previews.

Correction method:
Opposing 64-pixel edge bands were paired and blended toward a shared edge
appearance, with the blend fading toward the interior. Opposite border pixels
were then made exactly equal.

Verification:
The JSON report contains before/after edge pixel mismatch and edge-gradient
mismatch. A zero edge-pixel MAE means the opposite border pixels match exactly.
Gradient mismatch is also reported because exact color matching alone does not
guarantee perfect derivative continuity or eliminate every perceptual seam.

Unity import suggestions:
- Texture Type: Default
- Wrap Mode: Repeat
- Compression: None for initial visual testing
- Generate Mip Maps: On if used on 3D terrain
- Filter Mode: Bilinear or Trilinear
