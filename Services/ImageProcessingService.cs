using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cpu_net.Services
{
    /// <summary>
    /// 图片处理服务
    /// </summary>
    public static class ImageProcessingService
    {
        /// <summary>
        /// 从文件路径加载图片
        /// </summary>
        public static BitmapImage? LoadImage(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 创建带透明度的背景画刷
        /// </summary>
        public static ImageBrush? CreateBackgroundBrush(string? imagePath, double opacity)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return null;

            var bitmap = LoadImage(imagePath);
            if (bitmap == null)
                return null;

            var brush = new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = Math.Clamp(opacity, 0.0, 1.0)
            };
            return brush;
        }

        /// <summary>
        /// 裁剪并缩放图片为指定尺寸
        /// </summary>
        public static BitmapSource? CropAndResize(string path, int targetWidth, int targetHeight)
        {
            var bitmap = LoadImage(path);
            if (bitmap == null)
                return null;

            double scaleX = (double)targetWidth / bitmap.PixelWidth;
            double scaleY = (double)targetHeight / bitmap.PixelHeight;
            double scale = Math.Max(scaleX, scaleY);

            int newWidth = (int)(bitmap.PixelWidth * scale);
            int newHeight = (int)(bitmap.PixelHeight * scale);

            var scaledBitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));

            // 居中裁剪
            int offsetX = Math.Max(0, (newWidth - targetWidth) / 2);
            int offsetY = Math.Max(0, (newHeight - targetHeight) / 2);

            var croppedBitmap = new CroppedBitmap(scaledBitmap, new Int32Rect(offsetX, offsetY, targetWidth, targetHeight));
            croppedBitmap.Freeze();
            return croppedBitmap;
        }

        /// <summary>
        /// 保存BitmapSource到文件
        /// </summary>
        public static bool SaveToFile(BitmapSource source, string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                BitmapEncoder encoder = ext switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                encoder.Frames.Add(BitmapFrame.Create(source));
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                encoder.Save(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 复制图片到应用目录
        /// </summary>
        public static string? CopyToAppDirectory(string sourcePath, string fileName)
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string imagesDir = Path.Combine(appDir, "Images");
                Directory.CreateDirectory(imagesDir);

                string destPath = Path.Combine(imagesDir, fileName);
                File.Copy(sourcePath, destPath, true);
                return destPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
