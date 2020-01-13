using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE;

namespace PdfPigSvmRegionClassifier
{
    public static class PageXmlConverter
    {
        static int regionCount = 0;

        /// <summary>
        /// Converts the single json file into several PAGE xml files.
        /// </summary>
        /// <param name="inputFilePath">The path to the json file.</param>
        /// <param name="outputFolderPath">The folder that will contain the PAGE xml files.</param>
        public static void Convert(string inputFilePath, string outputFolderPath)
        {
            using (FileStream s = File.Open(inputFilePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(s))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();
                var cocoFile = serializer.Deserialize<CocoFile>(reader);

                Dictionary<int, string> categories = cocoFile.categories.ToDictionary(k => k.id, k => k.name);

                int totalImageCount = cocoFile.images.Count;
                for (int i = 0; i < totalImageCount; i++)
                {
                    var image = cocoFile.images[i];
                    string outputFilePath = Path.ChangeExtension(Path.Combine(outputFolderPath, image.file_name), "xml");
                    if (File.Exists(outputFilePath)) continue;

                    var entry = new CocoEntry(image.id, image.file_name, image.height, image.width);

                    var annotations = cocoFile.annotations.Where(a => a.image_id == image.id);
                    foreach (var annotation in annotations)
                    {
                        entry.Annotations.Add(annotation);
                    }

                    var pageXml = Get(entry, categories);
                    File.WriteAllText(outputFilePath, pageXml);
                    Console.WriteLine("Done: \t" + Path.GetFileName(outputFilePath) + "\t" + i + @"/" + totalImageCount);
                }
            }
        }

        /// <summary>
        /// Get the PAGE-XML (XML) string of the pages layout.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="includePaths">Draw <see cref="PdfPath"/>s present in the page.</param>
        private static string Get(CocoEntry page, Dictionary<int, string> categories)
        {
            PageXmlDocument pageXmlDocument = new PageXmlDocument()
            {
                Metadata = new PageXmlDocument.PageXmlMetadata()
                {
                    Created = DateTime.UtcNow,
                    LastChange = DateTime.UtcNow,
                    Creator = "PublayNetConverter",
                    Comments = "PubLayNet dataset"
                },
                PcGtsId = "pc" + page.Id.ToString()
            };

            pageXmlDocument.Page = ToPageXmlPage(page, categories);

            return Serialize(pageXmlDocument);
        }

        private static string Serialize(PageXmlDocument pageXmlDocument)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PageXmlDocument));
            var settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "\t",
            };

            using (var memoryStream = new MemoryStream())
            using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                serializer.Serialize(xmlWriter, pageXmlDocument);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private static PageXmlDocument.PageXmlPage ToPageXmlPage(CocoEntry page, Dictionary<int, string> categories)
        {
            var pageXmlPage = new PageXmlDocument.PageXmlPage()
            {
                ImageFilename = page.FileName,
                ImageHeight = (int)Math.Round(page.Height),
                ImageWidth = (int)Math.Round(page.Width),
            };

            var regions = new List<PageXmlDocument.PageXmlRegion>();
            foreach (var annotation in page.Annotations)
            {
                var category = categories[annotation.category_id];
                var segmentations = annotation.GetSegmentationPoints()[0];

                switch (category)
                {
                    case "title":
                        regions.Add(ToPageXmlTextRegion(annotation.id, segmentations, PageXmlDocument.PageXmlTextSimpleType.Heading));
                        break;

                    case "text":
                        regions.Add(ToPageXmlTextRegion(annotation.id, segmentations, PageXmlDocument.PageXmlTextSimpleType.Paragraph));
                        break;

                    case "list":
                        regions.Add(ToPageXmlTextRegion(annotation.id, segmentations, PageXmlDocument.PageXmlTextSimpleType.LisLabel));
                        break;

                    case "figure":
                        regions.Add(ToPageXmlImageRegion(annotation.id, segmentations));
                        break;

                    case "table":
                        regions.Add(ToPageXmlTableRegion(annotation.id, segmentations));
                        break;

                    default:
                        regions.Add(ToPageXmlUnknownRegion(annotation.id, segmentations));
                        break;
                }
            }

            pageXmlPage.Items = regions.ToArray();

            return pageXmlPage;
        }


        private static PageXmlDocument.PageXmlImageRegion ToPageXmlImageRegion(int id, IEnumerable<PointF> segmentation)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(segmentation),
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlImageRegion ToPageXmlImageRegion(int id, RectangleF bbox)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlImageRegion()
            {
                Coords = ToCoords(bbox),
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(int id, IEnumerable<PointF> segmentation, PageXmlDocument.PageXmlTextSimpleType type)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(segmentation),
                Type = type,
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlTextRegion ToPageXmlTextRegion(int id, RectangleF bbox, PageXmlDocument.PageXmlTextSimpleType type)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTextRegion()
            {
                Coords = ToCoords(bbox),
                Type = type,
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlTableRegion ToPageXmlTableRegion(int id, IEnumerable<PointF> segmentation)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTableRegion()
            {
                Coords = ToCoords(segmentation),
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlTableRegion ToPageXmlTableRegion(int id, RectangleF bbox)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlTableRegion()
            {
                Coords = ToCoords(bbox),
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlUnknownRegion ToPageXmlUnknownRegion(int id, IEnumerable<PointF> segmentation)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlUnknownRegion()
            {
                Coords = ToCoords(segmentation),
                Id = "r" + id.ToString()
            };
        }

        private static PageXmlDocument.PageXmlUnknownRegion ToPageXmlUnknownRegion(int id, RectangleF bbox)
        {
            regionCount++;
            return new PageXmlDocument.PageXmlUnknownRegion()
            {
                Coords = ToCoords(bbox),
                Id = "r" + id.ToString()
            };
        }

        private static string PointToString(PointF point)
        {
            double x = Math.Round(point.X);
            double y = Math.Round(point.Y);
            return (x > 0 ? x : 0).ToString("0") + "," + (y > 0 ? y : 0).ToString("0");
        }

        private static string ToPoints(IEnumerable<PointF> points)
        {
            var pointsList = points.ToList();
            if (pointsList.Last().Equals(pointsList.First()))
            {
                pointsList.RemoveAt(pointsList.Count - 1);
            }
            return string.Join(" ", pointsList.Select(p => PointToString(p)));
        }

        private static string ToPoints(RectangleF pdfRectangle)
        {
            var TopLeft = pdfRectangle.Location;
            var BottomLeft = new PointF(pdfRectangle.Left, pdfRectangle.Bottom);
            var TopRight = new PointF(pdfRectangle.Right, pdfRectangle.Top);
            var BottomRight = new PointF(pdfRectangle.Right, pdfRectangle.Bottom);
            return ToPoints(new[] { BottomLeft, TopLeft, TopRight, BottomRight });
        }

        private static PageXmlDocument.PageXmlCoords ToCoords(RectangleF pdfRectangle)
        {
            return new PageXmlDocument.PageXmlCoords()
            {
                Points = ToPoints(pdfRectangle)
            };
        }

        private static PageXmlDocument.PageXmlCoords ToCoords(IEnumerable<PointF> points)
        {
            return new PageXmlDocument.PageXmlCoords()
            {
                Points = ToPoints(points)
            };
        }
    }

    public class CocoEntry
    {
        public CocoEntry(int id, string fileName, float height, float width)
        {
            Id = id;
            FileName = fileName;
            Height = height;
            Width = width;
            Annotations = new List<CocoAnnotation>();
        }

        public int Id { get; set; }

        public string FileName { get; set; }

        public List<CocoAnnotation> Annotations { get; set; }

        public float Height { get; set; }

        public float Width { get; set; }
    }


    public class CocoFile
    {
        public List<CocoImage> images { get; set; }
        public List<CocoAnnotation> annotations { get; set; }
        public List<CocoCategory> categories { get; set; }
    }

    public class CocoImage
    {
        public int id { get; set; }
        public string file_name { get; set; }
        public float height { get; set; }
        public float width { get; set; }
    }

    public class CocoCategory
    {
        public int id { get; set; }
        public string supercategory { get; set; }
        public string name { get; set; }
    }

    public class CocoAnnotation
    {
        public float[][] segmentation { get; set; }

        public PointF[][] GetSegmentationPoints()
        {
            PointF[][] pointsArray = new PointF[segmentation.Length][];
            for (int s = 0; s < segmentation.Length; s++)
            {
                List<PointF> points = new List<PointF>();
                for (int i = 0; i < segmentation[s].Length; i += 2)
                {
                    points.Add(new PointF(segmentation[s][i], segmentation[s][i + 1]));
                }
                pointsArray[s] = points.ToArray();
            }
            return pointsArray;
        }

        public RectangleF GetBBoxRectangle()
        {
            if (bbox == null || bbox.Count() != 4)
            {
                return new RectangleF();
            }
            return new RectangleF(bbox[0], bbox[1], bbox[2], bbox[3]);
        }

        /// <summary>
        /// 
        /// </summary>
        public double area { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int iscrowd { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int image_id { get; set; }

        /// <summary>
        /// [x,y,width,height]
        /// </summary>
        public float[] bbox { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int category_id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
    }
}
