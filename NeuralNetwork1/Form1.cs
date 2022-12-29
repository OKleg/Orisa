using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot.Types;

namespace NeuralNetwork1
{

	public delegate void FormUpdater(double progress, double error, TimeSpan time);

    public delegate void UpdateTLGMessages(string msg);

    public delegate void UpdateImageData(Image bw, Image colored, string output);

    public partial class Form1 : Form
    {
        /// <summary>
        /// Чат-бот AIML
        /// </summary>
        AIMLBotik botik = new AIMLBotik();

        TLGBotik tlgBot;

        /// <summary>
        /// Генератор изображений (образов)
        /// </summary>
        //GenerateImage generator = new GenerateImage();
        Dataset dataset = new Dataset();
        public string RawDataPath = "../../data/raw/";
        public string PreprocessedDataPath = "../../data/preprocessed/";
        public string ReadyDataPath = "../../data/augmented/";
        public string NetworkPath = "../../network/";

        /// <summary>
        /// Обёртка для ActivationNetwork из Accord.Net
        /// </summary>
        AccordNet AccordNet = null;
        StudentNetwork StudentNetwork = null;

        /// <summary>
        /// Абстрактный базовый класс, псевдоним либо для CustomNet, либо для AccordNet
        /// </summary>
        BaseNetwork net = null;

        public Form1()
        {
            InitializeComponent();
            //dataset.Prepare(RawDataPath, PreprocessedDataPath, ReadyDataPath);
            //dataset.LoadAll(ReadyDataPath);
            tlgBot = new TLGBotik(net, new UpdateTLGMessages(UpdateTLGInfo), new UpdateImageData(UpdateImageData));
            netTypeBox.SelectedIndex = 0;
            Info.ClassesNum = (int)classCounter.Value;
            button3_Click(this, null);
            //pictureBox1.Image = Properties.Resources.Title;

        }

		public void UpdateLearningInfo(double progress, double error, TimeSpan elapsedTime)
		{
			if (progressBar1.InvokeRequired)
			{
				progressBar1.Invoke(new FormUpdater(UpdateLearningInfo),new Object[] {progress, error, elapsedTime});
				return;
			}
            StatusLabel.Text = "Точность: " + error.ToString();
            int prgs = (int)Math.Round(progress*100);
			prgs = Math.Min(100, Math.Max(0,prgs));
            elapsedTimeLabel.Text = "Затраченное время : " + elapsedTime.Duration().ToString(@"hh\:mm\:ss\:ff");
            progressBar1.Value = prgs;
		}

        public void UpdateTLGInfo(string message)
        {
            if (TLGUsersMessages.InvokeRequired)
            {
                TLGUsersMessages.Invoke(new UpdateTLGMessages(UpdateTLGInfo), new Object[] { message });
                return;
            }
            TLGUsersMessages.Text += message + Environment.NewLine;
        }

        public void UpdateImageData(Image bw, Image colored, string output)
        {
            if (pictureBox1.InvokeRequired)
            {
                pictureBox1.Invoke(new UpdateImageData(UpdateImageData), new Object[] { bw, colored, output });
                return;
            }
            if (pictureBox2.InvokeRequired)
            {
                pictureBox2.Invoke(new UpdateImageData(UpdateImageData), new Object[] { bw, colored, output });
                return;
            }
            if (label1.InvokeRequired)
            {
                label1.Invoke(new UpdateImageData(UpdateImageData), new Object[] { bw, colored, output });
                return;
            }
            pictureBox1.Image = bw;
            pictureBox2.Image = colored;
            label1.Text = output;
        }

        private void set_result(Sample figure, Bitmap picture)
        {
            label1.Text = figure.ToString();

            if (figure.Correct())
                label1.ForeColor = Color.Green;
            else
                label1.ForeColor = Color.Red;

            
            label1.Text = String.Format("{0} ({1:0.00}%)", Info.figureToStr[figure.recognizedClass], net.LastAccuracy * 100);

            //label8.Text = String.Join("\n", net.getOutput().Select(d => d.ToString()));
            pictureBox1.Image = picture;
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            //Sample fig = generator.GenerateFigure();
            var size = dataset.Size();
            int idx = new Random().Next(size);
            net.Predict(dataset.Data[idx]);

            set_result(dataset.Data[idx], dataset.Pictures[idx]);

            /*var rnd = new Random();
            var fname = "pic" + (rnd.Next() % 100).ToString() + ".jpg";
            pictureBox1.Image.Save(fname);*/

        }

        private async Task<double> train_networkAsync(int training_size, int epoches, double acceptable_error, bool parallel = true)
        {
            //  Выключаем всё ненужное
            label1.Text = "Выполняется обучение...";
            label1.ForeColor = Color.Red;
            groupBox1.Enabled = false;
            pictureBox1.Enabled = false;
            trainOneButton.Enabled = false;

            ////  Создаём новую обучающую выборку
            //SamplesSet samples = new SamplesSet();
            //dataset.Shuffle();
            var train_set = dataset.GetTrainData(training_size);

            //for (int i = 0; i < training_size; i++)
            //    samples.AddSample(generator.GenerateFigure());

            //  Обучение запускаем асинхронно, чтобы не блокировать форму
            double f = await Task.Run(() => net.TrainOnDataSet(train_set, epoches, acceptable_error, parallel));

            label1.Text = "Щелкните на картинку для теста нового образа";
            label1.ForeColor = Color.Green;
            groupBox1.Enabled = true;
            pictureBox1.Enabled = true;
            trainOneButton.Enabled = true;
            StatusLabel.Text = "Accuracy: " + f.ToString();
            StatusLabel.ForeColor = Color.Green;
            return f;

        }

        private void button1_Click(object sender, EventArgs e)
        {

            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            train_networkAsync( (int)TrainingSizeCounter.Value, (int)EpochesCounter.Value, (100 - AccuracyCounter.Value) / 100.0, parallelCheckBox.Checked);
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ////  Тут просто тестирование новой выборки
            ////  Создаём новую обучающую выборку
            //SamplesSet samples = new SamplesSet();

            //for (int i = 0; i < (int)TrainingSizeCounter.Value; i++)
            //    samples.AddSample(generator.GenerateFigure());
            var samples = dataset.GetTestData((int)TrainingSizeCounter.Value);

            double accuracy = net.TestOnDataSet(samples);
            
            StatusLabel.Text = string.Format("Точность на тестовой выборке : {0,5:F2}%", accuracy*100);
            if (accuracy*100 >= AccuracyCounter.Value)
                StatusLabel.ForeColor = Color.Green;
            else
                StatusLabel.ForeColor = Color.Red;

            this.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //  Проверяем корректность задания структуры сети
            int[] structure = netStructureBox.Text.Split(';').Select((c) => int.Parse(c)).ToArray();
            if (structure.Length < 2 || structure[structure.Length - 1] != Info.ClassesNum)
            {
                MessageBox.Show("А давайте вы структуру сети нормально запишите, ОК?", "Ошибка", MessageBoxButtons.OK);
                return;
            };

            if (netTypeBox.SelectedIndex == 0)
            {
                StudentNetwork = new StudentNetwork(structure);
                StudentNetwork.updateDelegate = UpdateLearningInfo;
                net = StudentNetwork;
                dataset.Shuffle();
            }
            else if (netTypeBox.SelectedIndex == 1)
            {
                AccordNet = new AccordNet(structure);
                AccordNet.updateDelegate = UpdateLearningInfo;
                net = AccordNet;
                dataset.Shuffle();
            }

            tlgBot.SetNet(net);

        }

        private void classCounter_ValueChanged(object sender, EventArgs e)
        {
            Info.ClassesNum = (int)classCounter.Value;
            var vals = netStructureBox.Text.Split(';');
            int outputNeurons;
            if (int.TryParse(vals.Last(),out outputNeurons))
            {
                vals[vals.Length - 1] = classCounter.Value.ToString();
                netStructureBox.Text = vals.Aggregate((partialPhrase, word) => $"{partialPhrase};{word}");
            }
        }

        private void btnTrainOne_Click(object sender, EventArgs e)
        {
            if (net == null) return;
            var size = dataset.Size();
            int idx = new Random().Next(size);
            Sample fig = dataset.Data[idx];
            pictureBox1.Image = dataset.Pictures[idx];
            pictureBox1.Invalidate();
            net.Train(fig, false);
            set_result(fig, dataset.Pictures[idx]);
        }

        private void netTrainButton_MouseEnter(object sender, EventArgs e)
        {
            infoStatusLabel.Text = "Обучить нейросеть с указанными параметрами";
        }

        private void testNetButton_MouseEnter(object sender, EventArgs e)
        {
            infoStatusLabel.Text = "Тестировать нейросеть на тестовой выборке такого же размера";
        }

        private void netTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (netTypeBox.SelectedIndex == 0)
            {
                net = StudentNetwork;
                button2.Enabled = true;
                button3.Enabled = true;
            }
            else if (netTypeBox.SelectedIndex == 1)
            {
                net = AccordNet;
                button2.Enabled = false;
                button3.Enabled = false;
            }
        }

        private void recreateNetButton_MouseEnter(object sender, EventArgs e)
        {
            infoStatusLabel.Text = "Заново пересоздаёт сеть с указанными параметрами";
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            var phrase = AIMLInput.Text;
            botik.Talk("default_user_id", "ОБНОВЛЕНИЕ ИМЕНИ TLGBot");
            botik.Talk("default_user_id", "ОБНОВЛЕНИЕ ФИГУРЫ Земля");
            if (phrase.Length > 0)
                AIMLOutput.Text = botik.Talk("default_user_id", phrase) + Environment.NewLine;
        }

        private void TLGBotOnButton_Click(object sender, EventArgs e)
        {
            tlgBot.Act();
            TLGBotOnButton.Enabled = false;
        }

        private void buttonSaveNet_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            StudentNetwork.Save(NetworkPath);
            Cursor.Current = Cursors.Default;
        }

        private void buttonLoadNet_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            StudentNetwork.Load(NetworkPath);
            Cursor.Current = Cursors.Default;
        }

        private void buttonLoadData_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            dataset.LoadAll(ReadyDataPath);
            Cursor.Current = Cursors.Default;
            this.label1.Text = $"Загружено данных: {dataset.Size()}";
        }

        private void buttonPrepareData_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            dataset.Prepare(RawDataPath, PreprocessedDataPath, ReadyDataPath);
            Cursor.Current = Cursors.Default;
        }

    }
}
