using System;

namespace NeuralNetwork1
{
	class Perceptron
	{
		private readonly double[] inputs;

		public double[] weights;

		public double bias;

		static Random r = new Random();

		public double w { get; set; }

		public int cntIn { get { return inputs.Length; } }

		public double Error { get; set; }

		public Perceptron(double[] input, double b)
		{

			inputs = input;
			weights = new double[cntIn];
			bias = b;
		}

		private double GetRandomNumber(Random r, double minimum, double maximum)
		{
			return r.NextDouble() * (maximum - minimum) + minimum;
		}

		public Perceptron(int input, double b)
		{
			inputs = new double[input];
			weights = new double[cntIn];

			for (int i = 0; i < weights.Length; i++)
				weights[i] = r.NextDouble();

			bias = b;
		}

		public Perceptron(int input, Random r)
		{
			inputs = new double[input];
			weights = new double[cntIn];
			for (int i = 0; i < weights.Length; i++)
				weights[i] = GetRandomNumber(r, -0.5, 0.5);
			bias = GetRandomNumber(r, -0.5, 0.5);
		}

		public double ActivationFunc()
		{
			return SigmoidF(w);
		}

		public double GetWSum(double[] inputs)
		{
			w = 0;
			for (int i = 0; i < cntIn; i++)
			{
				w += inputs[i] * weights[i];
			}
			w += bias;
			return ActivationFunc();
		}

		public double GetError()
		{
			return Error * revF(w);
		}

		public double SigmoidF(double w)
		{
			return 1.0 / (1 + Math.Exp(-w));
		}

		public double revF(double w)
		{
			double fval = SigmoidF(w);
			return fval * (1 - fval);
		}

	}
}
