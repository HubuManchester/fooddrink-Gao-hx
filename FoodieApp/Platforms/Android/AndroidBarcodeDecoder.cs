using Android.Graphics;
using ZXing;
using ZXing.Common;

namespace FoodieApp.Platforms.Android;

/// <summary>
/// Android-specific barcode decoder.
/// Uses BitmapFactory to load the JPEG, converts pixels to the byte[] format
/// that ZXing.Net 0.16.x RGBLuminanceSource requires (RGB, 3 bytes per pixel),
/// then runs BarcodeReaderGeneric to find any supported barcode symbology.
/// </summary>
public static class AndroidBarcodeDecoder
{
    public static async Task<string?> DecodeAsync(Stream imageStream)
    {
        try
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Decode the JPEG/PNG into an Android Bitmap
            var bitmap = await Task.Run(() =>
                BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length));

            if (bitmap == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "AndroidBarcodeDecoder: BitmapFactory returned null");
                return null;
            }

            int width  = bitmap.Width;
            int height = bitmap.Height;

            // Get pixels as ARGB int array
            var argbPixels = new int[width * height];
            bitmap.GetPixels(argbPixels, 0, width, 0, 0, width, height);
            bitmap.Recycle();

            // Convert ARGB int[] to RGB byte[] (3 bytes per pixel)
            // RGBLuminanceSource(byte[], width, height) is the correct overload
            // for ZXing.Net 0.16.x
            var rgbBytes = new byte[width * height * 3];
            for (int i = 0; i < argbPixels.Length; i++)
            {
                int px = argbPixels[i];
                rgbBytes[i * 3]     = (byte)((px >> 16) & 0xFF); // R
                rgbBytes[i * 3 + 1] = (byte)((px >> 8)  & 0xFF); // G
                rgbBytes[i * 3 + 2] = (byte)( px        & 0xFF); // B
            }

            var luminance = new RGBLuminanceSource(rgbBytes, width, height);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder   = true,
                    PureBarcode = false,
                    PossibleFormats = new List<BarcodeFormat>
                    {
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.EAN_8,
                        BarcodeFormat.UPC_A,
                        BarcodeFormat.UPC_E,
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.QR_CODE,
                        BarcodeFormat.DATA_MATRIX,
                    }
                }
            };

            var result = await Task.Run(() => reader.Decode(luminance));
            return result?.Text;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"AndroidBarcodeDecoder error: {ex.Message}");
            return null;
        }
    }
}
