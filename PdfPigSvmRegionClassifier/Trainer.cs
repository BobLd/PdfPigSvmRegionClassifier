using Accord.IO;
using Accord.MachineLearning.Performance;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;
using System;
using System.IO;
using System.Linq;

namespace PdfPigSvmRegionClassifier
{
    public static class Trainer
    {
        /// <summary>
        /// Limitation linked to memory limit.
        /// </summary>
        static int traingRowsCount = 40_000;

        static int maxCrossValidateCount = 5_000;

        /// <summary>
        /// Read and format data from csv file.
        /// </summary>
        /// <param name="trainingFolder"></param>
        /// <param name="lim"></param>
        /// <returns></returns>
        static (double[][] inputs, int[] output) ReadData(string trainingFolder, int lim = 0)
        {
            double[][] data;
            using (var reader = new CsvReader(Path.Combine(trainingFolder, "features.csv"), false))
            {
                data = reader.ToJagged();
            }

            if (lim > 0 && data.Count() > lim)
            {
                data = data.Take(lim).ToArray();
            }

            double[][] inputs = data.GetColumns(Enumerable.Range(0, data.Columns() - 1).ToArray());
            int[] output = data.GetColumn(data.Columns() - 1).Select(o => (int)o).ToArray();

            return (inputs, output);
        }

        public static void Evaluate(MulticlassSupportVectorMachine<Gaussian> svm, string trainingFolder, int lim = 0)
        {
            Console.WriteLine("Evaluating SVM model...");

            (double[][] inputs, int[] output) = ReadData(trainingFolder, lim);

            int[] preds = svm.Decide(inputs);
            double[] score = svm.Score(inputs);

            int numCorrect = 0; int numWrong = 0;
            for (int i = 0; i < preds.Length; ++i)
            {
                if (preds[i] == output[i]) ++numCorrect;
                else ++numWrong;
            }

            double acc = (numCorrect * 100.0) / (numCorrect + numWrong);
            Console.WriteLine("Model accuracy = " + acc);
            Console.WriteLine();

            // Confusion matrix
            double[,] confusionMatrix = new double[5, 5];
            for (int i = 0; i < preds.Length; ++i)
            {
                int pred = preds[i];
                int gold = output[i];
                confusionMatrix[pred, gold]++;
            }

            Console.WriteLine(confusionMatrix.ToString("0   \t"));

            for (int i = 0; i < 5; i++)
            {
                double truePositive = confusionMatrix[i, i];
                double totalGoldLabel = confusionMatrix.GetColumn(i).Sum();
                double recall = truePositive / totalGoldLabel;

                double totalPredicted = confusionMatrix.GetRow(i).Sum();
                double precision = truePositive / totalPredicted;

                double F1 = 2 * (precision * recall) / (precision + recall);

                Console.WriteLine(FeatureHelper.Categories[i] + ":");
                Console.WriteLine("Precision: " + precision.ToString("0.000"));
                Console.WriteLine("Recall:    " + recall.ToString("0.000"));
                Console.WriteLine("F1 score:  " + F1.ToString("0.000"));
                Console.WriteLine();
            }
        }

        public static void Train(string trainingFolder)
        {
            Console.WriteLine("Training SVM model with Cross-Validation...");

            (double[][] inputs, int[] output) = ReadData(trainingFolder);

            int crossValidateCount = Math.Min(maxCrossValidateCount, inputs.Count());

            Accord.Math.Random.Generator.Seed = 0;

            Console.WriteLine("Grid-Search...");
            var gscv = GridSearch<double[], int>.CrossValidate(
                ranges: new
                {
                    Sigma = GridSearch.Range(fromInclusive: 0.00000001, toExclusive: 3),
                },
                
                learner: (p, ss) => new MulticlassSupportVectorLearning<Gaussian>
                {
                    Kernel = new Gaussian(p.Sigma)
                },
                
                fit: (teacher, x, y, w) => teacher.Learn(x, y, w),
                
                loss: (actual, expected, r) => new ZeroOneLoss(expected).Loss(actual),
                
                folds: 10);

            //gscv.ParallelOptions.MaxDegreeOfParallelism = 1;

            var result = gscv.Learn(inputs.Take(crossValidateCount).ToArray(), output.Take(crossValidateCount).ToArray());

            var crossValidation = result.BestModel;

            double bestError = result.BestModelError;
            double trainError = result.BestModel.Training.Mean;
            double trainErrorVar = result.BestModel.Training.Variance;
            double valError = result.BestModel.Validation.Mean;
            double valErrorVar = result.BestModel.Validation.Variance;

            double bestSigma = result.BestParameters.Sigma;
            Console.WriteLine("Grid-Search Done.");

            Console.WriteLine("Using Sigma=" + bestSigma);

            // train model with best parameter
            var bestTeacher = new MulticlassSupportVectorLearning<Gaussian>
            {
                Kernel = new Gaussian(bestSigma)
            };

            MulticlassSupportVectorMachine<Gaussian> svm = bestTeacher.Learn(
                                                                inputs.Take(traingRowsCount).ToArray(),
                                                                output.Take(traingRowsCount).ToArray());

            // save model
            svm.Save(Path.Combine(trainingFolder, "model"), SerializerCompression.GZip);
        }

        public static void Train(string trainingFolder, double sigma)
        {
            Console.WriteLine("Training SVM model with sigma value...");

            (double[][] inputs, int[] output) = ReadData(trainingFolder);
            int crossValidateCount = Math.Min(maxCrossValidateCount, inputs.Count());

            Accord.Math.Random.Generator.Seed = 0;

            var bestTeacher = new MulticlassSupportVectorLearning<Gaussian>
            {
                Kernel = new Gaussian(sigma)
            };

            MulticlassSupportVectorMachine<Gaussian> svm = bestTeacher.Learn(
                                                                inputs.Take(traingRowsCount).ToArray(),
                                                                output.Take(traingRowsCount).ToArray());

            // save model
            svm.Save(Path.Combine(trainingFolder, "model"), SerializerCompression.GZip);
        }
    }
}
