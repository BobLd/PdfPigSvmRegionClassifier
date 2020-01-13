using System;

namespace PdfPigSvmRegionClassifier
{
    class Program
    {
        static readonly string trainingFolder = @"D:\Datasets\Document Layout Analysis\PubLayNet\extracted\train\";
        static readonly string validationFolder = @"D:\Datasets\Document Layout Analysis\PubLayNet\extracted\val\";

        static void Main(string[] args)
        {
            //GenerateData.GenerateCsv(trainingFolder, 3000);
            //GenerateData.GenerateCsv(validationFolder, 800);

            //Trainer.Train(trainingFolder);

            SvmZoneClassifier.Evaluate(trainingFolder, validationFolder);

            SvmZoneClassifier.TestClassification(trainingFolder, @"Samples\Random 2 Columns Lists Chart.pdf");

            Console.ReadKey();
        }
    }
}
