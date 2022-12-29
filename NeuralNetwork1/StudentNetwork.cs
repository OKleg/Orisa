using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Forms;
using Accord.Statistics.Filters;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using Accord.Neuro;
using Accord.Math;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using Accord.Statistics.Distributions.Univariate;
using AForge.Imaging.Filters;
using static System.Collections.Specialized.BitVector32;
using Telegram.Bot.Types;
using Newtonsoft.Json;

namespace NeuralNetwork1
{
    public class NetworkData
    {
        public int[] structure;
        public double[][][] weights;
        public double[][][] velocities;
    }

    public class Matrix
    {
        public double[][] values;
        public Matrix() { }
        public Matrix(Matrix other_matrix) { values = other_matrix.values; }
        public Matrix(int rows_num, int cols_num)
        {
            values = new double[rows_num][];
            for(int i = 0; i < rows_num; ++i)
                values[i] = new double[cols_num];
        }
        public Matrix(int rows_num, int cols_num, double gaussian_std)
        {
            values = new double[rows_num][];
            var r = new Random(Guid.NewGuid().GetHashCode());
            int counter = 0;
            for (int i = 0; i < rows_num; ++i)
            {
                values[i] = new double[cols_num];
                for (int j = 0; j < cols_num; ++j)
                {
                    values[i][j] = gaussian_std * Math.Sqrt(-2.0 * Math.Log(1.0 - r.NextDouble())) * Math.Sin(2.0 * Math.PI * (1.0 - r.NextDouble()));
                    if (values[i][j] > 0)
                        ++counter;
                }
            }
        }

        public void Dot(Matrix other_matrix, Matrix result, bool thisIsTransposed = false, bool otherIsTransposed = false)
        {
            int mat1rows = thisIsTransposed ? values[0].Length : values.Length;
            int mat2cols = otherIsTransposed ? other_matrix.values.Length : other_matrix.values[0].Length;
            int mat1cols = thisIsTransposed ? values.Length : values[0].Length;
            Parallel.For(0, mat1rows, (i) =>
            {
                Parallel.For(0, mat2cols, (j) =>
                {
                    result.values[i][j] = 0;
                    if (!thisIsTransposed && !otherIsTransposed)
                        for (int k = 0; k < mat1cols; ++k)
                            result.values[i][j] += values[i][k] * other_matrix.values[k][j];
                    else if (!thisIsTransposed)
                        for (int k = 0; k < mat1cols; ++k)
                            result.values[i][j] += values[i][k] * other_matrix.values[j][k];
                    else if (!otherIsTransposed)
                        for (int k = 0; k < mat1cols; ++k)
                            result.values[i][j] += values[k][i] * other_matrix.values[k][j];
                    else
                        for (int k = 0; k < mat1cols; ++k)
                            result.values[i][j] += values[k][i] * other_matrix.values[j][k];

                });
            });
        }
    }

    public class StudentNetwork : BaseNetwork
    {
        public Matrix[] Weights;
        public Matrix[] Velocities;
        public Matrix[] ForwardResults;
        public Matrix[] dXResults;
        public Matrix[] dWResults;
        public Stopwatch stopWatch = new Stopwatch();
        private double slope = 0.35; //Leaky ReLU slope
        private double momentum = 0.5; //Nesterov Momentum
        private double lr = 0.01; // Learning rate
        private double lr_decay = 0.9999;
        private int batch_size = 64;
        private double acceptable_error = 0.0005;
        public double[] Output;
        public int[] Structure;

        public StudentNetwork(int[] structure)
        {
            Structure = structure;
            ReInit(structure, 0);
        }

        public override void ReInit(int[] structure, double initialLearningRate = 0.25)
        {
            Weights = new Matrix[structure.Length - 1];
            Velocities = new Matrix[structure.Length - 1];
            ForwardResults = new Matrix[structure.Length];
            dXResults = new Matrix[structure.Length - 1];
            dWResults = new Matrix[structure.Length - 1];
            for (int i = 0; i < structure.Length - 1; ++i)
            {
                int rows_num = structure[i] + 1;
                int cols_num = structure[i + 1] + 1;
                if (i == structure.Length - 2)
                    --cols_num;
                var weight = new Matrix(rows_num, cols_num, Math.Sqrt(2.0 / rows_num)); //He Weights Initialization
                var velocity = new Matrix(rows_num, cols_num);
                Weights[i] = weight;
                Velocities[i] = velocity;
                dWResults[i] = new Matrix(rows_num, cols_num);
            }
        }

        public override int Train(Sample sample, bool parallel)
        {
            int m = sample.input.Length;
            ForwardResults[0] = new Matrix(1, m + 1);
            for(int i = 0; i < sample.input.Length; ++i)
                ForwardResults[0].values[0][i] = sample.input[i];
            ForwardResults[0].values[0][m] = 1;
            for (int i = 0; i < Weights.Length; ++i)
            {
                ForwardResults[i + 1] = new Matrix(1, Weights[i].values[0].Length);
                dXResults[i] = new Matrix(1, Weights[i].values[0].Length);
            }
            double error = double.MaxValue;
            var labels = new int[1];
            labels[0] = (int)sample.actualClass;
            int iters = 0;
            while(error > acceptable_error)
            {
                Forward();
                error = CategoricalCrossEntropy(labels);
                dCategoricalCrossEntropy(labels);
                Backward();
                ++iters;
            }
            return iters;
        }

        public override double TrainOnDataSet(SamplesSet samplesSet, int epochsCount, double acceptableError, bool parallel)
        {
            acceptableError *= 0.85; 
            int n = samplesSet.Count;
            int m = samplesSet[0].input.Length;
            int batch_num = n / batch_size;
            Matrix[] batches = new Matrix[batch_num];
            int[][] labels = new int[batch_num][];

            for(int i = 0; i < batches.Length; ++i)
            {
                int start_idx = batch_size * i;
                int end_idx = batch_size * i + batch_size;
                batches[i] = new Matrix(batch_size, m + 1);
                labels[i] = new int[batch_size];
                for (int j = start_idx; j < end_idx; ++j)
                {
                    int relative_idx = j - start_idx;
                    for (int k = 0; k < m; ++k)
                        batches[i].values[relative_idx][k] = samplesSet[j].input[k];
                    batches[i].values[relative_idx][m] = 1;
                    labels[i][relative_idx] = (int)samplesSet[j].actualClass;
                }
            }
            ForwardResults[0] = new Matrix(batch_size, m + 1);
            for(int i = 0; i < Weights.Length; ++i)
            {
                ForwardResults[i + 1] = new Matrix(batch_size, Weights[i].values[0].Length);
                dXResults[i] = new Matrix(batch_size, Weights[i].values[0].Length);
            }

            double error = double.MaxValue;
            int epochs = 0;
            stopWatch.Restart();
            while (epochs < epochsCount && error > acceptableError)
            {
                //double sum_error = 0;
                for(int i = 0; i < batch_num; ++i)
                {
                    ForwardResults[0] = batches[i];
                    Forward();
                    error = CategoricalCrossEntropy(labels[i]);
                    dCategoricalCrossEntropy(labels[i]);
                    Backward();
                    if ((batch_size * i) % (batch_size * 10) == 0)
                        updateDelegate(epochs / (double)epochsCount, error, stopWatch.Elapsed);
                }
                //error = sum_error / batch_num;
                ++epochs;
                lr *= lr_decay;
            }
            stopWatch.Stop();
            return error;
        }

        protected double[] Compute(double[] input)
        {
            ForwardResults[0] = new Matrix(1, input.Length + 1);
            for (int j = 0; j < input.Length; ++j)
                ForwardResults[0].values[0][j] = input[j];
            ForwardResults[0].values[0][input.Length] = 1;
            for (int i = 0; i < Weights.Length; ++i)
                ForwardResults[i + 1] = new Matrix(1, Weights[i].values[0].Length);
            return Forward().values[0];
        }

        public override FigureType Predict(Sample sample)
        {
            sample.output = Compute(sample.input);
            Output = sample.output;
            sample.recognizedClass = ProcessOutput(Output);
            return sample.recognizedClass;
        }

        public FigureType ProcessOutput(double[] output)
        {
            double max_value = -1;
            int max_ind = -1;
            for(int i = 0; i < output.Length; ++i)
            {
                if (output[i] > max_value)
                {
                    max_value = output[i];
                    max_ind = i;
                }
            }
            LastAccuracy = max_value;
            return (FigureType)max_ind;
        }

        public override double TestOnDataSet(SamplesSet testSet)
        {
            int n = testSet.Count;
            int m = testSet[0].input.Length;
            ForwardResults[0] = new Matrix(n, m + 1);
            int[] labels = new int[testSet.Count];
            for(int i = 0; i < n; ++i)
            {
                for(int j = 0; j < m; ++j)
                    ForwardResults[0].values[i][j] = testSet[i].input[j];
                ForwardResults[0].values[i][m] = 1;
                labels[i] = (int)testSet[i].actualClass;
            }
            for (int i = 0; i < Weights.Length; ++i)
            {
                ForwardResults[i + 1] = new Matrix(n, Weights[i].values[0].Length);
                dXResults[i] = new Matrix(n, Weights[i].values[0].Length);
            }
            Matrix result = Forward();
            int sum = 0;
            for (int i = 0; i < n; ++i)
                sum += result.values[i].ArgMax() == labels[i] ? 1 : 0;
            return sum / (double)n;
        }

        public override double[] getOutput() { return Output; }

        public void Save(string path)
        {
            var data = new NetworkData();
            data.structure = Structure;
            data.weights = new double[Weights.Length][][];
            data.velocities = new double[Velocities.Length][][];
            for (int i = 0; i < Weights.Length; ++i)
            {
                data.weights[i] = Weights[i].values;
                data.velocities[i] = Velocities[i].values;
            }
            var result = JsonConvert.SerializeObject(data);
            System.IO.File.WriteAllText(path + "network.json", result);
        }

        public void Load(string path)
        {
            var str = System.IO.File.ReadAllText(path + "network.json");
            var data = JsonConvert.DeserializeObject<NetworkData>(str);
            Structure = data.structure;
            ReInit(Structure);
            for (int i = 0; i < Weights.Length; ++i)
            {
                Weights[i].values = data.weights[i];
                Velocities[i].values = data.velocities[i];
            }
        }

        private double Sigmoid(double x) { return 1 / (1 + Math.Exp(-x)); }

        private double LeakyReLU(double x) { return x > 0 ? x : slope * x; }

        private void Softmax(double[] elems, double shift=0.0)
        {
            double sum = 0;
            for(int i = 0; i < elems.Length; ++i)
            {
                elems[i] = Math.Exp(elems[i] - shift);
                sum += elems[i];
            }
            for (int i = 0; i < elems.Length; ++i)
                elems[i] /= sum;
        }

        private double CategoricalCrossEntropy(int[] labels)
        {
            double loss = 0;
            int output_idx = ForwardResults.Length - 1;
            for (int i = 0; i < labels.Length; ++i)
                loss -= Math.Log(ForwardResults[output_idx].values[i][labels[i]]);
            return loss / labels.Length;
        }

        private void dCategoricalCrossEntropy(int[] labels)
        {
            int idx = dXResults.Length - 1;
            dXResults[idx] = ForwardResults[idx + 1];
            Parallel.For(0, dXResults[idx].values.Length, (i) =>
            {
                dXResults[idx].values[i][labels[i]] -= 1;
                for (int j = 0; j < dXResults[idx].values[i].Length; ++j)
                    dXResults[idx].values[i][j] /= labels.Length;
            });
        }

        private Matrix Forward()
        {
            for(int i = 0; i < Weights.Length; ++i)
            {
                ForwardResults[i].Dot(Weights[i], ForwardResults[i + 1]);
                if (i < Weights.Length - 1)
                {
                    Parallel.For(0, ForwardResults[i + 1].values.Length, (j) =>
                    {
                        for (int k = 0; k < ForwardResults[i + 1].values[0].Length; ++k)
                            ForwardResults[i + 1].values[j][k] = LeakyReLU(ForwardResults[i + 1].values[j][k]);
                        ForwardResults[i + 1].values[j][ForwardResults[i + 1].values[0].Length - 1] = 1;
                    });
                }
                else
                {
                    Parallel.For(0, ForwardResults[i + 1].values.Length, (j) =>
                    {
                        var max_elem = ForwardResults[i + 1].values[j].Max();
                        Softmax(ForwardResults[i + 1].values[j], max_elem);
                    });
                }
            }
            return ForwardResults.Last();
        }

        private void Backward()
        {
            for(int i = Weights.Length - 1; i >= 0; --i)
            {
                if (i != Weights.Length - 1)
                {
                    Parallel.For(0, dXResults[i].values.Length, (j) =>
                    {
                        for (int l = 0; l < dXResults[i].values[0].Length; ++l)
                            if (ForwardResults[i + 1].values[j][l] < 0)
                                dXResults[i].values[j][l] *= slope;
                    });
                }
                ForwardResults[i].Dot(dXResults[i], dWResults[i], true, false);
                if (i != 0)
                    dXResults[i].Dot(Weights[i], dXResults[i - 1], false, true);
                Parallel.For(0, Weights[i].values.Length, (j) =>
                {
                    for (int l = 0; l < Weights[i].values[0].Length; ++l)
                    {
                        double new_velocity_elem = momentum * Velocities[i].values[j][l] - lr * dWResults[i].values[j][l];
                        Weights[i].values[j][l] += -momentum * Velocities[i].values[j][l] + (1 + momentum) * new_velocity_elem;
                        Velocities[i].values[j][l] = new_velocity_elem;
                    }
                });
            }
        }
    }


    static public class HelpFunctions
    {
        public static int[] Shuffle(int[] list)
        {
            int n = list.Length;
            while (n > 1)
            {
                n--;
                int k = new Random().Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        } 
    }
}