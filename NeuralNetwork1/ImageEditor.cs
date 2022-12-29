using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using AForge.Imaging.Filters;
using AIMLbot.AIMLTagHandlers;

namespace NeuralNetwork1
{
    public static class ImageEditor
    {
        public static ColorMatrix GrayscaleMatrixPAL = new ColorMatrix(
        new float[][]
        {
            new float[] { 0.299f, 0.299f, 0.299f, 0, 0},
            new float[] { 0.587f, 0.587f, 0.587f, 0, 0},
            new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
        });
        public static ColorMatrix GrayscaleMatrixAVG = new ColorMatrix(
        new float[][]
        {
            new float[] { 0.333f, 0.333f, 0.333f, 0, 0},
            new float[] { 0.333f, 0.333f, 0.333f, 0, 0},
            new float[] { 0.333f, 0.333f, 0.333f, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
        });
        public static Random r = new Random();

        // Плавное изменение размера
        public static Bitmap SmoothResize(Image image, Size size)
        {
            var destRect = new Rectangle(0, 0, size.Width, size.Height);
            var destImage = new Bitmap(size.Width, size.Height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        //Резкое изменение размера
        public static Bitmap SharpResize(Image image, Size size)
        {
            var destImage = new Bitmap(size.Width, size.Height);
            var kernel_height = image.Height / size.Height;
            var kernel_width = image.Width / size.Width;
            double threshold = 0.0625;
            for (int i = 0; i < size.Height; i++)
            {
                for (int j = 0; j < size.Width; j++)
                {
                    double counter = 0;
                    for (int y = 0; y < kernel_height; y++)
                        for (int x = 0; x < kernel_width; x++)
                        {
                            if (((Bitmap)image).GetPixel(j * kernel_width + x, i * kernel_height + y).R == 0)
                                ++counter;
                        }
                    if (counter / (kernel_width * kernel_height) >= threshold)
                        destImage.SetPixel(j, i, Color.Black);
                    else
                        destImage.SetPixel(j, i, Color.White);
                }
            }
            return destImage;
        }
        //Обрезать до Квадратного
        public static Bitmap CropToSquare(Image image)
        {
            int left_x = 0, bottom_y = 0, length = image.Width;
            if (image.Height > image.Width)
            {
                length = image.Width;
                bottom_y = (image.Height - length) / 2;
            }
            else if (image.Height < image.Width)
            {
                length = image.Height;
                left_x = (image.Width - length) / 2;
            }
            var destImage = new Bitmap(length, length);
            using (var g = Graphics.FromImage(destImage))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    Rectangle section = new Rectangle(new Point(0, 0), new Size(length, length));
                    g.DrawImage(image, section, left_x, bottom_y, length, length, GraphicsUnit.Pixel, attributes);
                }
            }

            return destImage;
        }
        //Добавляет белый Гауссов шум
        public static Bitmap AddGaussianNoise(Bitmap image)
        {
            var result = new Bitmap(image.Height, image.Width);
            double std = System.Math.Sqrt(5);
            double mean = 0.0;
            for(int j = 0; j < image.Height; j++)
            {
                for(int i = 0; i < image.Width; ++i)
                {
                    var value = image.GetPixel(i, j);
                    var R_noise = mean + std * System.Math.Sqrt(-2.0 * System.Math.Log(1.0 - r.NextDouble())) * System.Math.Sin(2.0 * System.Math.PI * (1.0 - r.NextDouble()));
                    var G_noise = mean + std * System.Math.Sqrt(-2.0 * System.Math.Log(1.0 - r.NextDouble())) * System.Math.Sin(2.0 * System.Math.PI * (1.0 - r.NextDouble()));
                    var B_noise = mean + std * System.Math.Sqrt(-2.0 * System.Math.Log(1.0 - r.NextDouble())) * System.Math.Sin(2.0 * System.Math.PI * (1.0 - r.NextDouble()));
                    Color new_color = Color.FromArgb((byte)((double)value.R + R_noise), (byte)((double)value.G + G_noise), (byte)((double)value.B + B_noise));
                    result.SetPixel(i, j, new_color);

                }
            }
            return result;
        }

        public static Bitmap RGBtoBW(Bitmap rgb_pic)
        {
            var bitmap = new Bitmap(rgb_pic.Width, rgb_pic.Height);
            float threshold = 0.54f;
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(GrayscaleMatrixPAL);
                    attributes.SetThreshold(threshold);
                    g.DrawImage(rgb_pic, new Rectangle(0, 0, rgb_pic.Width, rgb_pic.Height), 0, 0, rgb_pic.Width, rgb_pic.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return bitmap;
        }

        public static Bitmap SharpenBW(Bitmap bw_pic, int threshold)
        {
            var result = new Bitmap(bw_pic.Width, bw_pic.Height);
            for (int j = 0; j < bw_pic.Height; ++j) {
                for (int i = 0; i < bw_pic.Width; ++i) {
                    var value = AvgColor(bw_pic.GetPixel(i, j));
                    if (value > threshold)
                        result.SetPixel(i, j, Color.White);
                    else
                        result.SetPixel(i, j, Color.Black);
                }
            }
            return result;
        }

        public static double[] GetArray(Bitmap pic)
        {
            var result = new double[pic.Height * pic.Width];
            for (int j = 0; j < pic.Height; ++j) {
                for (int i = 0; i < pic.Width; ++i) { 
                    result[j * pic.Width + i] = pic.GetPixel(i, j).R == 255 ? 0 : 1;
                }
            }
            return result;
        }

        private static int AvgColor(Color color) { return (int)((color.R + color.G + color.B) / 3.0); }
        
        
        //https://stackoverflow.com/questions/71309307/how-to-normalize-the-given-bounding-box-coordinates-and-also-normalize-them-for
        public static Bitmap NormalizeByBoundingBox(Bitmap input, Size size)
        {
            int top_y = 0, bottom_y = input.Height, left_x = input.Width, right_x = 0;
            for (int j = 0; j < input.Height; ++j)
            {
                for (int k = 0; k < input.Width; ++k)
                {
                    var value = (int)input.GetPixel(k, j).R;
                    if (value == 0)
                    {
                        if (j > top_y) top_y = j;
                        if (j < bottom_y) bottom_y = j;
                        if (k < left_x) left_x = k;
                        if (k > right_x) right_x = k;
                    }
                }
            }
            int new_width = right_x - left_x, new_height = top_y - bottom_y;
            if (new_width > new_height)
            {
                int divided_difference = (new_width - new_height) / 2;
                bottom_y -= divided_difference;
                top_y += divided_difference;
            }
            else if (new_height > new_width)
            {
                int divided_difference = (new_height - new_width) / 2;
                left_x -= divided_difference;
                right_x += divided_difference;
            }
            int local_shift = 20;
            var normalized_image = new Bitmap(right_x - left_x + local_shift, top_y - bottom_y + local_shift);
            using (var g = Graphics.FromImage(normalized_image))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    g.Clear(Color.White);
                    Rectangle section = new Rectangle(new Point(local_shift / 2, local_shift / 2), new Size(right_x - left_x, top_y - bottom_y));
                    g.DrawImage(input, section, left_x, bottom_y, right_x - left_x, top_y - bottom_y, GraphicsUnit.Pixel, attributes);
                }
            }
            Bitmap result = new Bitmap(normalized_image, size);
            return result;
        }
        //Нормализуем по Центру Масс
        public static Bitmap NormalizeByMassCenter(Bitmap input, Size size)
        {
            int sum_x = 0, sum_y = 0;
            List<KeyValuePair<int, int>> points = new List<System.Collections.Generic.KeyValuePair<int, int>>();
            for (int j = 0; j < input.Height; ++j) {
                for (int i = 0; i < input.Width; ++i) {
                    if ((int)input.GetPixel(i, j).R == 0)
                    {
                        points.Add(new KeyValuePair<int, int>(i, j));
                        sum_x += i;
                        sum_y += j;
                    }
                }
            }
            int avg_x = (int)(sum_x / (float)points.Count);
            int avg_y = (int)(sum_y / (float)points.Count);
            int sum_dist_x = 0, sum_dist_y = 0;
            for (int i = 0; i < points.Count; ++i) {
                sum_dist_x += Math.Abs(avg_x - points[i].Key);
                sum_dist_y += Math.Abs(avg_y - points[i].Value);
            }
            int new_width = (int)(2.8 * 2 * sum_dist_x / points.Count);
            int new_height = (int)(2.8 * 2 * sum_dist_y / points.Count);
            int new_length = new_width > new_height ? new_width : new_height;
            int local_shift = 5;
            var normalized_image = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(normalized_image))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.White);
                    Rectangle section = new Rectangle(new Point(local_shift, local_shift), new Size(size.Width - 2 * local_shift, size.Height - 2 * local_shift));
                    g.DrawImage(input, section, avg_x - new_length / 2, avg_y - new_length / 2, new_length, new_length, GraphicsUnit.Pixel, attributes);
                }
            }
            return normalized_image;
        }

        public static Bitmap Augment(Bitmap input)
        {
            var rotated_image = new Bitmap(input.Width, input.Width);
            int angle = r.Next(-35, 35);
            using (var g = Graphics.FromImage(rotated_image))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.White);
                    Rectangle section = new Rectangle(new Point(0, 0), new Size(input.Width, input.Height));
                    g.TranslateTransform(input.Width / 2, input.Height / 2);
                    g.RotateTransform(angle);
                    g.TranslateTransform(-input.Width / 2, -input.Height / 2);
                    g.DrawImage(input, section, 0, 0, input.Width, input.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            var augmented_image = rotated_image; 
            return augmented_image;
        }
    }
}
