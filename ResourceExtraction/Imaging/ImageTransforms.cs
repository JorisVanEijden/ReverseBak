namespace ResourceExtraction.Imaging;

using System;

public static class ImageTransforms {
    /// <summary>
    /// Reverse the row order of row-major 8-bits-per-pixel bitmap data — a vertical mirror.
    /// </summary>
    public static byte[] FlipVerticalRows(byte[] rowMajor, int width, int height) {
        if (rowMajor == null) throw new ArgumentNullException(nameof(rowMajor));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (rowMajor.Length < (long)width * height) {
            throw new ArgumentException($"Bitmap data ({rowMajor.Length}) smaller than width*height ({(long)width * height}).", nameof(rowMajor));
        }

        var flipped = new byte[rowMajor.Length];
        int stride = width;
        for (int row = 0; row < height; row++) {
            Array.Copy(rowMajor, row * stride, flipped, (height - 1 - row) * stride, stride);
        }

        return flipped;
    }
}
