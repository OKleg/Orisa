using System;

namespace NeuralNetwork1
{
	public class StudentNetwork : BaseNetwork
	{
		private BNKNet network;

		public StudentNetwork(int[] structure)
		{
			network = new BNKNet(structure);
		}


		public override int Train(Sample sample, double acceptableError, bool parallel)
		{
			int cnt = 1;

			var lossRes = Loss(network.Compute(sample.input), sample.targetValues);
			while (cnt <= 100  && lossRes >= acceptableError)
			{
				BackProg(sample);
				lossRes = Loss(network.Compute(sample.input), sample.targetValues);
				cnt++;
			}

			return cnt;
		}


		private double Loss(double[] aRes, double[] tRes)
		{
			double res = 0;

			for (int i = 0; i < tRes.Length; i++)
			{
				res += Math.Pow(tRes[i] - aRes[i], 2);
			}

			return res / 2;
		}

		//Обратное распространение
		public void BackProg(Sample sample, double learningRate = 0.3)
		{
			var targets = sample.targetValues;

			//Обход всех слоев
			for (int i = network.layers.Length - 1; i >= 1; i--)
			{
				for (int j = 0; j < network.layers[i].perceptrons.Length; j++)
				{
					Perceptron perceptron = network.layers[i].perceptrons[j];

					if (i == network.layers.Length - 1)
					{
						perceptron.Error = targets[j] - perceptron.ActivationFunc();
					}

					double error = perceptron.GetError();
					perceptron.bias += learningRate * error * perceptron.bias;
					for (int k = 0; k < perceptron.weights.Length; k++)
					{
						network.layers[i - 1].perceptrons[k].Error += error * perceptron.weights[k];
						perceptron.weights[k] += learningRate * error * network.layers[i - 1].perceptrons[k].ActivationFunc();
					}
					perceptron.Error = 0;
				}
			}
		}

		public override double TrainOnDataSet(SamplesSet samplesSet, int countOfAges, double acceptEr, bool parallel)
		{
			int procSampCnt = 0;
			double sumError = 0;
			double mn;


			DateTime stTIME = DateTime.Now;
			int samplesCount = countOfAges * samplesSet.Count;
			

			for (int age = 0; age < countOfAges; age++)
			{
				for (int i = 0; i < samplesSet.samples.Count; i++)
				{
					var sample = samplesSet.samples[i];
					sumError += Train(sample);

					procSampCnt++;
					OnTrainProgress(1.0 * procSampCnt / samplesCount,sumError / (age * samplesSet.Count + i + 1), DateTime.Now - stTIME);
				}

				mn = sumError / ((age + 1) * samplesSet.Count + 1);

				if (mn <= acceptEr)
				{
					OnTrainProgress(1.0, mn, DateTime.Now - stTIME);
					return mn;
				}
			}

			mn = sumError / (countOfAges * samplesSet.Count + 1);
			OnTrainProgress(1.0, mn, DateTime.Now - stTIME);
			return sumError / (countOfAges * samplesSet.Count);
		}

		protected override double[] Compute(double[] input)
		{
			return network.Compute(input);
		}

		private double Train(Sample sample)
		{
			var loss = Loss(network.Compute(sample.input), sample.targetValues);
			BackProg(sample);
			return loss;
		}
	}
}