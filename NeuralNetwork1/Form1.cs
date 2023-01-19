﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace NeuralNetwork1
{

	public delegate void FormUpdater(double progress, double error, TimeSpan time);

    public delegate void UpdateTLGMessages(string msg);

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
        GenerateImage generator = new GenerateImage();
        
        /// <summary>
        /// Обёртка для ActivationNetwork из Accord.Net
        /// </summary>
        AccordNet AccordNet = null;

		/// <summary>
		/// Текущая выбранная через селектор нейросеть
		/// </summary>
		public BaseNetwork Net
		{
			get
			{
				var selectedItem = (string)netTypeBox.SelectedItem;
				if (!networksCache.ContainsKey(selectedItem))
					networksCache.Add(selectedItem, CreateNetwork(selectedItem));

				return networksCache[selectedItem];
			}
		}

		private readonly Dictionary<string, Func<int[], BaseNetwork>> networksFabric;
		private Dictionary<string, BaseNetwork> networksCache = new Dictionary<string, BaseNetwork>();

		public Form1()
        {
            InitializeComponent();
            tlgBot = new TLGBotik(Net, new UpdateTLGMessages(UpdateTLGInfo));
			netTypeBox.Items.AddRange(this.networksFabric.Keys.Select(s => (object)s).ToArray());
			netTypeBox.SelectedIndex = 0;
			generator.FigureCount = (int)classCounter.Value;
			button3_Click(this, null);
            pictureBox1.Image = Properties.Resources.Title;

        }

		public Form1(Dictionary<string, Func<int[], BaseNetwork>> networksFabric)
		{
			InitializeComponent();
			this.networksFabric = networksFabric;

			netTypeBox.Items.AddRange(this.networksFabric.Keys.Select(s => (object)s).ToArray());
			netTypeBox.SelectedIndex = 0;
			generator.FigureCount = (int)classCounter.Value;


			tlgBot = new TLGBotik(Net, new UpdateTLGMessages(UpdateTLGInfo));


			button3_Click(this, null);
			pictureBox1.Image = Properties.Resources.Title;

		}

		private BaseNetwork CreateNetwork(string networkName)
		{
			var network = networksFabric[networkName](CurrentNetworkStructure());
			network.TrainProgress += UpdateLearningInfo;
			return network;
		}

		private int[] CurrentNetworkStructure()
		{
			return netStructureBox.Text.Split(';').Select(int.Parse).ToArray();
		}

		public void UpdateLearningInfo(double progress, double error, TimeSpan elapsedTime)
		{
			if (progressBar1.InvokeRequired)
			{
				progressBar1.Invoke(new FormUpdater(UpdateLearningInfo),new Object[] {progress, error, elapsedTime});
				return;
			}
            StatusLabel.Text = "Accuracy: " + error.ToString();
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

		private void set_result(Sample figure)
		{
			label1.ForeColor = figure.Correct() ? Color.Green : Color.Red;

			label1.Text = "Распознано : " + figure.recognizedClass;

			label8.Text = string.Join("\n", figure.Output.Select(d => d.ToString(CultureInfo.InvariantCulture)));
			pictureBox1.Image = generator.GenBitmap();
			pictureBox1.Invalidate();
		}

		private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            Sample fig = generator.GenerateFigure();

			Net.Predict(fig);

            set_result(fig);

            /*var rnd = new Random();
            var fname = "pic" + (rnd.Next() % 100).ToString() + ".jpg";
            pictureBox1.Image.Save(fname);*/

        }

		private async Task<double> train_networkAsync(int training_size, int epoches, double acceptable_error,
			bool parallel = true)
		{
			//  Выключаем всё ненужное
			label1.Text = "Выполняется обучение...";
			label1.ForeColor = Color.Red;
			groupBox1.Enabled = false;
			pictureBox1.Enabled = false;
			trainOneButton.Enabled = false;

			//  Создаём новую обучающую выборку
			SamplesSet samples = new SamplesSet();

			for (int i = 0; i < training_size; i++)
				samples.AddSample(generator.GenerateFigure());
			try
			{
				//  Обучение запускаем асинхронно, чтобы не блокировать форму
				var curNet = Net;
				double f = await Task.Run(() => curNet.TrainOnDataSet(samples, epoches, acceptable_error, parallel));

				label1.Text = "Щелкните на картинку для теста нового образа";
				label1.ForeColor = Color.Green;
				groupBox1.Enabled = true;
				pictureBox1.Enabled = true;
				trainOneButton.Enabled = true;
				StatusLabel.Text = "Ошибка: " + f;
				StatusLabel.ForeColor = Color.Green;
				return f;
			}
			catch (Exception e)
			{
				label1.Text = $"Исключение: {e.Message}";
			}

			return 0;
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
            //  Тут просто тестирование новой выборки
            //  Создаём новую обучающую выборку
            SamplesSet samples = new SamplesSet();

            for (int i = 0; i < (int)TrainingSizeCounter.Value; i++)
                samples.AddSample(generator.GenerateFigure());

			double accuracy = samples.TestNeuralNetwork(Net);

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
			int[] structure = CurrentNetworkStructure();
			if (structure.Length < 2 || structure[0] != 400 ||
				structure[structure.Length - 1] != generator.FigureCount)
			{
				MessageBox.Show(
					$"В сети должно быть более двух слоёв, первый слой должен содержать 400 нейронов, последний - ${generator.FigureCount}",
					"Ошибка", MessageBoxButtons.OK);
				return;
			}

			// Чистим старые подписки сетей
			foreach (var network in networksCache.Values)
				network.TrainProgress -= UpdateLearningInfo;
			// Пересоздаём все сети с новой структурой
			networksCache = networksCache.ToDictionary(oldNet => oldNet.Key, oldNet => CreateNetwork(oldNet.Key));

			tlgBot.SetNet(Net);

        }

        private void classCounter_ValueChanged(object sender, EventArgs e)
        {
            generator.FigureCount = (int)classCounter.Value;
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
            if (Net == null) return;
			Sample fig = generator.GenerateFigure();
			pictureBox1.Image = generator.GenBitmap();
			pictureBox1.Invalidate();
			Net.Train(fig, 0.00005, parallelCheckBox.Checked);
			set_result(fig);
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
            
        }

        private void recreateNetButton_MouseEnter(object sender, EventArgs e)
        {
            infoStatusLabel.Text = "Заново пересоздаёт сеть с указанными параметрами";
        }

        private void button1_Click_1(object sender, EventArgs e)
        { 
            botik.Talk("default_user_id", "ОБНОВЛЕНИЕ ИМЕНИ TLGBot");
            var phrase = AIMLInput.Text;
            if (phrase.Length > 0)
                AIMLOutput.Text += botik.Talk("default_user_id",phrase) + Environment.NewLine;
        }

        private void TLGBotOnButton_Click(object sender, EventArgs e)
        {
            tlgBot.Act();
            TLGBotOnButton.Enabled = false;
        }
    }

  }
