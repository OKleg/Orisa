using Accord.Statistics.Distributions.Univariate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuralNetwork1
{
    public static class Info
    {
        public static int Height = 50;
        public static int Width = 50;
        public static int InputSize = 2500;
        public static int ClassesNum;
        public static Dictionary<string, FigureType> strToFigure = new Dictionary<string, FigureType>
        {
            ["earth"] = FigureType.Earth,
            ["sun"] = FigureType.Sun,
            ["uranus"] = FigureType.Uranus,
            ["mars"] = FigureType.Mars,
            ["jupiter"] = FigureType.Jupiter,
            ["saturn"] = FigureType.Saturn,
            ["mercury"] = FigureType.Mercury,
            ["venus"] = FigureType.Venus,
            ["pluto"] = FigureType.Pluto,
            ["moon"] = FigureType.Moon,
            ["neptune"] = FigureType.Neptune
        };

        public static Dictionary<FigureType, string> figureToStr = new Dictionary<FigureType, string>
        {
            [FigureType.Earth] = "Земля",
            [FigureType.Sun] = "Солнце",
            [FigureType.Uranus] = "Уран",
            [FigureType.Mars] = "Марс",
            [FigureType.Jupiter] = "Юпитер",
            [FigureType.Saturn] = "Сатурн",
            [FigureType.Mercury] = "Меркурий",
            [FigureType.Venus] = "Венера",
            [FigureType.Pluto] = "Плутон",
            [FigureType.Moon] = "Луна",
            [FigureType.Neptune] = "Нептун",
            [FigureType.Undef] = "Мехмат ФИИТ"
        };
    }

    public class Dataset
    {
        public SamplesSet Data;
        public List<Bitmap> Pictures = new List<Bitmap>();
        public int[] Indices;
        public int TrainSize;
        public int TestSize;
        public Random r = new Random();

        public Dataset()
        {
            Data = new SamplesSet();
        }

        public int Size() { return Pictures.Count; }

        public void Prepare(string raw_path, string preprocessed_path, string ready_path)
        {
            var preprocessed_dir = new DirectoryInfo(preprocessed_path);
            var ready_dir = new DirectoryInfo(ready_path);
            foreach (var x in preprocessed_dir.GetFiles()) x.Delete();
            foreach (var x in ready_dir.GetFiles()) x.Delete();

            DirectoryInfo raw_dir = new DirectoryInfo(raw_path);
            FileInfo[] picture_files = raw_dir.GetFiles();
            for (int i = 0; i < picture_files.Length; ++i)
            {
                int idx = picture_files[i].Name.IndexOf("_");
                string old_name = picture_files[i].Name.Substring(0, idx);
                Bitmap original = (Bitmap)Image.FromFile(picture_files[i].FullName);
                for (int k = 0; k < 3; ++k)
                {
                    Bitmap original_noised = ImageEditor.AddGaussianNoise(original);
                    Bitmap original_bw = ImageEditor.RGBtoBW(original_noised);
                    Bitmap resized_smooth_bw = ImageEditor.SmoothResize(original_bw, new Size(Info.Width, Info.Height));
                    Bitmap sharp_bw = ImageEditor.SharpenBW(resized_smooth_bw, 220);
                    original_noised.Dispose();
                    original_bw.Dispose();
                    resized_smooth_bw.Dispose();
                    var normalized = ImageEditor.NormalizeByMassCenter(sharp_bw, new Size(Info.Width, Info.Height));
                    var sharpened_normalized = ImageEditor.SharpenBW(normalized, 210);
                    sharp_bw.Dispose();
                    normalized.Dispose();
                    sharpened_normalized.Save(preprocessed_path + $"{old_name}_{Guid.NewGuid()}.jpg");
                    for (int j = 0; j < 3; ++j)
                    {
                        var augmented = ImageEditor.SharpenBW(ImageEditor.Augment(sharpened_normalized), 130);
                        augmented.Save(ready_path + $"{old_name}_{Guid.NewGuid()}.jpg");
                        augmented.Dispose();
                    }
                    sharpened_normalized.Dispose();
                }
            }
        }

        public void LoadAll(string ready_path)
        {
            int classes_num = (int)FigureType.Undef;
            DirectoryInfo d = new DirectoryInfo(ready_path);
            FileInfo[] picture_files = d.GetFiles();
            for (int i = 0; i < picture_files.Length; ++i)
            {
                Bitmap picture = (Bitmap)Image.FromFile(picture_files[i].FullName);
                Pictures.Add(picture);
                var input = ImageEditor.GetArray(picture);
                int idx = picture_files[i].Name.IndexOf("_");
                Data.AddSample(new Sample(input, classes_num, Info.strToFigure[picture_files[i].Name.Substring(0, idx)]));
            }
        }

        public FigureType GetFigure(string name)
        {
            switch (name)
            {
                case "earth": return FigureType.Earth;
                case "sun": return FigureType.Sun;
                case "uranus": return FigureType.Uranus;
                case "mars": return FigureType.Mars;
                case "jupiter": return FigureType.Jupiter;
                case "saturn": return FigureType.Saturn;
                case "mercury": return FigureType.Mercury;
                case "venus": return FigureType.Venus;
                case "pluto": return FigureType.Pluto;
                case "moon": return FigureType.Moon;
                case "neptune": return FigureType.Neptune;
                default: return FigureType.Undef;
            }
        }

        public void Shuffle() { Indices = HelpFunctions.Shuffle(Enumerable.Range(0, Size()).ToArray()); }

        public SamplesSet GetTrainData(int training_size)
        {
            TrainSize = (int)((training_size / 100.0) * Size());
            var s = new SamplesSet();
            for (int i = 0; i < TrainSize; ++i)
                s.AddSample(Data[Indices[i]]);
            return s;
        }

        public SamplesSet GetTestData(int training_size)
        {
            int start = (int)((training_size / 100.0) * Size());
            TestSize = Size() - start;
            var s = new SamplesSet();
            s.samples = new List<Sample>();
            for (int i = start; i < Size(); ++i)
                s.AddSample(Data[Indices[i]]);
            return s;
        }
    }
}
