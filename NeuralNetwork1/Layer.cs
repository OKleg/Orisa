using System;
using System.Linq;

namespace NeuralNetwork1
{
	class Layer
	{
		public Perceptron[] perceptrons;

		public double[] GetOutput(double[] input)
		{
			return perceptrons.Select(x => x.GetWSum(input)).ToArray(); ;
		}

		public Layer(int inputCount, int outputCount)
		{
			perceptrons = new Perceptron[outputCount];


			Random r = new Random();
			for (int i = 0; i < outputCount; i++)
			{
				perceptrons[i] = new Perceptron(inputCount, r);
			}


		}

	}
}
