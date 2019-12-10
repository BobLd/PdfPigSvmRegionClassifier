using System;

namespace PdfPigSvmRegionClassifier
{
    class Program
    {
        static readonly string trainingFolder = @"D:\Datasets\Document Layout Analysis\PubLayNet\extracted\train\";

        static void Main(string[] args)
        {
            //GenerateData.GenerateCsv(trainingFolder, 2000);

            //Trainer.Train(trainingFolder);

            SvmZoneClassifier.Evaluate(trainingFolder);

            SvmZoneClassifier.TestClassification(trainingFolder, @"Samples\Random 2 Columns Lists Chart.pdf");

            Console.ReadKey();
        }
    }
}
