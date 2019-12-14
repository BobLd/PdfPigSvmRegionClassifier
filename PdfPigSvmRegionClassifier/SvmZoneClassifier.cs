using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using System;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace PdfPigSvmRegionClassifier
{
    class SvmZoneClassifier
    {
        public static void Evaluate(string trainingFolder)
        {
            var svm = Serializer.Load<MulticlassSupportVectorMachine<Gaussian>>(Path.Combine(trainingFolder, "model.gz"), SerializerCompression.GZip);
            Trainer.Evaluate(svm, trainingFolder);
        }

        public static void TestClassification(string trainingFolder, string pdfPath)
        {
            var svm = Serializer.Load<MulticlassSupportVectorMachine<Gaussian>>(Path.Combine(trainingFolder, "model.gz"), SerializerCompression.GZip);

            using (var document = PdfDocument.Open(pdfPath))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    var words = page.GetWords();
                    if (words.Count() == 0) continue;

                    var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                    foreach (var block in blocks)
                    {
                        var letters = block.TextLines.SelectMany(li => li.Words).SelectMany(w => w.Letters);
                        var paths = FeatureHelper.GetPathsInside(block.BoundingBox, page.ExperimentalAccess.Paths);
                        var images = FeatureHelper.GetImagesInside(block.BoundingBox, page.GetImages());
                        var features = FeatureHelper.GetFeatures(page, block.BoundingBox, letters, paths, images);

                        var category = svm.Decide(features);

                        Console.WriteLine(FeatureHelper.Categories[category]);
                        Console.WriteLine(block.Text);
                        Console.WriteLine();
                    }
                    
                    Console.ReadKey();
                }
            }
        }
    }
}
