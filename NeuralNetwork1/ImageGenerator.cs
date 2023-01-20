using System;
using System.IO;
using System.Drawing;

namespace NeuralNetwork1
{
	/// <summary>
	/// Тип фигуры
	/// </summary>
	public enum FigureType : byte { cpp = 0, cs, python, java, js, pascal, Ruby, php, go, haskell, Undef };

	public class GenerateImage
	{
		/// <summary>
		/// Бинарное представление образа
		/// </summary>
		public bool[,] img = new bool[32, 32];

		//  private int margin = 50;
		private Random rand = new Random();

		/// <summary>
		/// Текущая сгенерированная фигура
		/// </summary>
		public FigureType currentFigure = FigureType.Undef;

		/// <summary>
		/// Количество классов генерируемых фигур (10 - максимум)
		/// </summary>
		public int FigureCount { get; set; } = 10;


		/// <summary>
		/// Очистка образа
		/// </summary>
		public void ClearImage()
		{
			for (int i = 0; i < 32; ++i)
				for (int j = 0; j < 32; ++j)
					img[i, j] = false;
		}

		public Sample GenerateFigure()
		{
			generate_figure();
			double[] input = new double[64];
			for (int i = 0; i < 64; i++)
				input[i] = 0;

			FigureType type = currentFigure;

			for (int i = 0; i < 32; i++)
				for (int j = 0; j < 32; j++)
					if (img[i, j])
					{
						input[i] += 1;
						input[32 + j] += 1;
					}
			return new Sample(input, FigureCount, type);
		}

		public Sample GenerateFigure(Bitmap bm)
		{
			for (int i = 0; i < 32; ++i)
				for (int j = 0; j < 32; ++j)
				{
					img[i, j] = bm.GetPixel(i, j).R == 0;
				}


			double[] input = new double[64];
			for (int i = 0; i < 64; i++)
				input[i] = 0;

			FigureType type = currentFigure;

			for (int i = 0; i < 32; i++)
				for (int j = 0; j < 32; j++)
					if (img[i, j])
					{
						input[i] += 1;
						input[32 + j] += 1;
					}
			return new Sample(input, FigureCount, type);
		}

		public bool create_triangle()
		{
			currentFigure = FigureType.python;

			return true;
		}


		public Image GetRandomImage(string st)
		{
			Random r = new Random();
			string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
			DirectoryInfo di = new DirectoryInfo(projectDirectory + "\\dataset\\" + st);
			var files = di.GetFiles();
			return Image.FromFile(projectDirectory + "\\dataset\\" + st + "\\" + files[r.Next() % files.Length].Name);
		}

		public bool figCreate(FigureType type)
		{
			currentFigure = type;

			var bm = FilterImage.Filter(GetRandomImage(type.ToString()), true, rand.Next(100, 150));

			for (int i = 0; i < 32; ++i)
				for (int j = 0; j < 32; ++j)
				{
					img[i, j] = bm.GetPixel(i, j).R == 0;
				}
			return true;
		}

		public bool create_sin()
		{
			currentFigure = FigureType.cpp;

			return true;
		}


		public void generate_figure(FigureType type = FigureType.Undef)
		{

			if (type == FigureType.Undef || (int)type >= FigureCount)
				type = (FigureType)rand.Next(FigureCount);
			ClearImage();
			figCreate(type);
		}

		/// <summary>
		/// Возвращает битовое изображение для вывода образа
		/// </summary>
		/// <returns></returns>
		public Bitmap GenBitmap()
		{
			Bitmap drawArea = new Bitmap(32, 32);
			for (int i = 0; i < 32; ++i)
				for (int j = 0; j < 32; ++j)
					if (img[i, j])
						drawArea.SetPixel(i, j, Color.Black);
			return drawArea;
		}
	}

}
