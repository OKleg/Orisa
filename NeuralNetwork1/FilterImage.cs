using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using AForge.Imaging;

namespace NeuralNetwork1
{
	public static class FilterImage
	{
		public static Bitmap Filter(System.Drawing.Image img, bool crop = false, int thres = 160)
		{
			System.Console.WriteLine(thres);
			System.Drawing.Bitmap bm = new System.Drawing.Bitmap(img);

			if (crop)
			{
				Random r = new Random();
				int newW = bm.Width * ((r.Next() % 20) + 75) / 100;
				int newH = bm.Height * ((r.Next() % 20) + 75) / 100;
				AForge.Imaging.Filters.Crop cropf = new AForge.Imaging.Filters.Crop(new Rectangle(r.Next() % 10, r.Next() % 10, newW, newH));
				bm = cropf.Apply(bm);

				bm.Save("crop.png");
			}

			//  Масштабируем aforge

			AForge.Imaging.Filters.Grayscale grayFilter = new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);

			bm = grayFilter.Apply(bm);

			bm.Save("temp2.png");

			AForge.Imaging.Filters.ResizeBilinear scaleFilter = new AForge.Imaging.Filters.ResizeBilinear(32, 32);
			bm = scaleFilter.Apply(bm);

			AForge.Imaging.Filters.Threshold threshold = new AForge.Imaging.Filters.Threshold(thres);
			bm = threshold.Apply(bm);

			bm.Save("temp.png");

			return bm;
		}
	}
}
