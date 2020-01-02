using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Geometry;
using static UglyToad.PdfPig.Export.PageXmlTextExporter;
using static UglyToad.PdfPig.Export.PageXmlTextExporter.PageXmlDocument;

namespace PdfPigSvmRegionClassifier
{
    static class GenerateData
    {
        /// <summary>
        /// Generate a csv file of features. You will need the pdf documents and the ground truths in PAGE xml format.
        /// </summary>
        /// <param name="trainingFolder">The path to the training folder. Should contain both the pdf files and their corresponding ground truth xml files.</param>
        /// <param name="numberOfPdfDocs">Number of documents to concider.</param>
        public static void GenerateCsv(string trainingFolder, int numberOfPdfDocs)
        {
            List<double[]> features = new List<double[]>();
            List<int> categories = new List<int>();

            int done = 0;

            DirectoryInfo d = new DirectoryInfo(trainingFolder);
            var pdfFileLinks = d.GetFiles("*.pdf");

            var indexesSelected = GenerateRandom(numberOfPdfDocs, 0, pdfFileLinks.Length);

            foreach (int index in indexesSelected)
            {
                var pdfFile = pdfFileLinks[index];
                string fileName = pdfFile.Name;
                string xmlFileNameTemplate = fileName.Replace(".pdf", "_*.xml");
                var pageXmlLinks = d.GetFiles(xmlFileNameTemplate);

                if (pageXmlLinks.Length == 0)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("No PageXml file found for document '" + fileName + "'");
                    Console.ResetColor();
                    continue;
                }

                try
                {
                    using (var doc = PdfDocument.Open(pdfFile.FullName))
                    {
                        foreach (var pageXmlLink in pageXmlLinks)
                        {
                            var pageXml = Deserialize(pageXmlLink.FullName);
                            int pageNo = ParseXmlFileName(pageXmlLink.Name);
                            var page = doc.GetPage(pageNo + 1);

                            var blocks = pageXml.Page.Items;

                            foreach (var block in blocks)
                            {
                                int category = -1;
                                PdfRectangle bbox = new PdfRectangle();

                                if (block is PageXmlTextRegion textBlock)
                                {
                                    bbox = ParsePageXmlCoord(textBlock.Coords.Points, (double)page.Height);
                                    switch (textBlock.Type)
                                    {
                                        case PageXmlTextSimpleType.Heading:
                                            category = 0;
                                            break;
                                        case PageXmlTextSimpleType.Paragraph:
                                            category = 1;
                                            break;
                                        case PageXmlTextSimpleType.LisLabel:
                                            category = 2;
                                            break;
                                        default:
                                            throw new ArgumentException("Unknown category");
                                    }
                                }
                                else if (block is PageXmlTableRegion tableBlock)
                                {
                                    bbox = ParsePageXmlCoord(tableBlock.Coords.Points, (double)page.Height);
                                    category = 3;
                                }
                                else if (block is PageXmlImageRegion imageBlock)
                                {
                                    bbox = ParsePageXmlCoord(imageBlock.Coords.Points, (double)page.Height);
                                    category = 4;
                                }
                                else
                                {
                                    throw new ArgumentException("Unknown region type");
                                }

                                var letters = FeatureHelper.GetLettersInside(bbox, page.Letters).ToList();
                                var paths = FeatureHelper.GetPathsInside(bbox, page.ExperimentalAccess.Paths).ToList();
                                var images = FeatureHelper.GetImagesInside(bbox, page.GetImages());
                                var f = FeatureHelper.GetFeatures(page, bbox, letters, paths, images);

                                if (category == -1)
                                {
                                    throw new ArgumentException("Unknown category number.");
                                }

                                if (f != null)
                                {
                                    features.Add(f);
                                    categories.Add(category);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error for document '" + fileName + "': " + ex.Message);
                    Console.ResetColor();
                }
                Console.WriteLine(done++);
            }

            if (features.Count != categories.Count)
            {
                throw new ArgumentException("features and categories don't have the same size");
            }

            string[] csv = features.Zip(categories, (f, c) => string.Join(',', f) + "," + c).ToArray();
            File.WriteAllLines(Path.Combine(trainingFolder, "features.csv"), csv);
        }

        static PageXmlDocument Deserialize(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));

            using (var reader = XmlReader.Create(xmlPath))
            {
                return (PageXmlDocument)serializer.Deserialize(reader);
            }
        }

        static PdfRectangle ParsePageXmlCoord(string points, double height)
        {
            string[] pointsStr = points.Split(' ');

            List<PdfPoint> pdfPoints = new List<PdfPoint>();

            foreach (var p in pointsStr)
            {
                string[] coord = p.Split(',');
                pdfPoints.Add(new PdfPoint(double.Parse(coord[0]), height - double.Parse(coord[1])));
            }

            return new PdfRectangle(pdfPoints.Min(p => p.X), pdfPoints.Min(p => p.Y), pdfPoints.Max(p => p.X), pdfPoints.Max(p => p.Y));
        }

        static int ParseXmlFileName(string xmlFileName)
        {
            string split = xmlFileName.Split("_")[1].Replace(".xml", "");
            if (int.TryParse(split, out int pageNo))
            {
                return pageNo;
            }

            throw new ArgumentException("Cannot parse page number");
        }

        /// <summary>
        /// https://codereview.stackexchange.com/questions/61338/generate-random-numbers-without-repetitions 
        /// </summary>
        static List<int> GenerateRandom(int count, int min, int max)
        {
            Random random = new Random(42);

            //  initialize set S to empty
            //  for J := N-M + 1 to N do
            //    T := RandInt(1, J)
            //    if T is not in S then
            //      insert T in S
            //    else
            //      insert J in S
            //
            // adapted for C# which does not have an inclusive Next(..)
            // and to make it from configurable range not just 1.

            if (max <= min || count < 0 ||
                    // max - min > 0 required to avoid overflow
                    (count > max - min && max - min > 0))
            {
                // need to use 64-bit to support big ranges (negative min, positive max)
                throw new ArgumentOutOfRangeException("Range " + min + " to " + max +
                        " (" + ((Int64)max - (Int64)min) + " values), or count " + count + " is illegal");
            }

            // generate count random values.
            HashSet<int> candidates = new HashSet<int>();

            // start count values before max, and end at max
            for (int top = max - count; top < max; top++)
            {
                // May strike a duplicate.
                // Need to add +1 to make inclusive generator
                // +1 is safe even for MaxVal max value because top < max
                if (!candidates.Add(random.Next(min, top + 1)))
                {
                    // collision, add inclusive max.
                    // which could not possibly have been added before.
                    candidates.Add(top);
                }
            }

            // load them in to a list, to sort
            List<int> result = candidates.ToList();

            // shuffle the results because HashSet has messed
            // with the order, and the algorithm does not produce
            // random-ordered results (e.g. max-1 will never be the first value)
            for (int i = result.Count - 1; i > 0; i--)
            {
                int k = random.Next(i + 1);
                int tmp = result[k];
                result[k] = result[i];
                result[i] = tmp;
            }
            return result;
        }
    }
}
