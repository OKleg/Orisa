namespace NeuralNetwork1
{
	class BNKNet
	{
		public Layer[] layers;

		public BNKNet(int[] structure)
		{
			layers = new Layer[structure.Length - 1];
			for (int i = 0; i < structure.Length - 1; i++)
			{
				layers[i] = new Layer(structure[i], structure[i + 1]);
			}
		}

		public double[] Compute(double[] input)
		{
			for (int i = 0; i < layers.Length; i++)
			{
				input = layers[i].GetOutput(input);
			}

			return input;
		}
	}
}
