using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using static UglyToad.PdfPig.Core.PdfPath;

namespace PdfPigSvmRegionClassifier
{
    public static class FeatureHelper
    {
        public static readonly Dictionary<int, string> Categories = new Dictionary<int, string>()
        {
            { 0, "title" },     // 0    title
            { 1, "text" },      // 1    text
            { 2, "list" },      // 2    list
            { 3, "table" },     // 3    table
            { 4, "image" },     // 4    image
        };

        private static readonly char[] Bullets = new char[]
        {
            '•', 'o', '▪', '❖', '➢', '►', '✓', '➔', '⇨', '➪',
            '➨', '➫', '➬', '➭', '➮', '➯', '➱', '➲', '\u2023',
            '\u2043', '\u204C', '\u204D'
        };

        public static double[] GetFeatures(Page page, PdfRectangle bbox, IEnumerable<Letter> letters, IEnumerable<PdfPath> paths, IEnumerable<IPdfImage> images)
        {
            // Letters features
            double charsCount = 0;
            double pctNumericChars = 0;
            double pctAlphabeticalChars = 0;
            double pctSymbolicChars = 0;
            double pctBulletChars = 0;
            double deltaToHeight = -1;   // might be problematic

            if (letters != null && letters.Count() > 0)
            {
                var avgHeight = page.Letters.Select(l => l.GlyphRectangle.Height).Average();

                char[] chars = letters.SelectMany(l => l.Value).ToArray();

                charsCount = chars.Length;
                pctNumericChars = Math.Round(chars.Count(c => char.IsNumber(c)) / charsCount, 5);
                pctAlphabeticalChars = Math.Round(chars.Count(c => char.IsLetter(c)) / charsCount, 5);
                pctSymbolicChars = Math.Round(chars.Count(c => !char.IsLetterOrDigit(c)) / charsCount, 5);
                pctBulletChars = Math.Round(chars.Count(c => Bullets.Any(bullet => bullet == c)) / charsCount, 5);
                deltaToHeight = avgHeight != 0 ? Math.Round((double)(letters.Select(l => l.GlyphRectangle.Height).Average() / avgHeight), 5) : -1;
            }

            // Paths features
            double pathsCount = 0;
            double pctBezierPaths = 0;
            double pctHorPaths = 0;
            double pctVertPaths = 0;
            double pctOblPaths = 0;

            if (paths != null && paths.Count() > 0)
            {
                foreach (var path in paths)
                {
                    foreach (var command in path.Commands)
                    {
                        if (command is BezierCurve bezierCurve)
                        {
                            pathsCount++;
                            pctBezierPaths++;
                        }
                        else if (command is Line line)
                        {
                            pathsCount++;
                            if (line.From.X == line.To.X)
                            {
                                pctVertPaths++;
                            }
                            else if (line.From.Y == line.To.Y)
                            {
                                pctHorPaths++;
                            }
                            else
                            {
                                pctOblPaths++;
                            }
                        }
                    }
                }

                pctBezierPaths = Math.Round(pctBezierPaths / pathsCount, 5);
                pctHorPaths = Math.Round(pctHorPaths / pathsCount, 5);
                pctVertPaths = Math.Round(pctVertPaths / pathsCount, 5);
                pctOblPaths = Math.Round(pctOblPaths / pathsCount, 5);
            }

            // Images features
            double imagesCount = 0;
            double imageAvgProportion = 0;

            if (images != null && images.Count() > 0)
            {
                imagesCount = images.Count();
                imageAvgProportion = (double)(images.Average(i => i.Bounds.Area) / bbox.Area);
            }

            return new double[]
            {
                charsCount, pctNumericChars, pctAlphabeticalChars, pctSymbolicChars, pctBulletChars, deltaToHeight,
                pathsCount, pctBezierPaths, pctHorPaths, pctVertPaths, pctOblPaths,
                imagesCount, imageAvgProportion
            };
        }

        public static IEnumerable<Letter> GetLettersInside(PdfRectangle bound, IEnumerable<Letter> letters)
        {
            return letters.Where(l => l.GlyphRectangle.Left >= bound.Left &&
                                      l.GlyphRectangle.Right <= bound.Right &&
                                      l.GlyphRectangle.Bottom >= bound.Bottom &&
                                      l.GlyphRectangle.Top <= bound.Top);
        }

        public static IEnumerable<IPdfImage> GetImagesInside(PdfRectangle bound, IEnumerable<IPdfImage> images)
        {
            return images.Where(b => b.Bounds.Left >= bound.Left &&
                                     b.Bounds.Right <= bound.Right &&
                                     b.Bounds.Bottom >= bound.Bottom &&
                                     b.Bounds.Top <= bound.Top);
        }

        public static IEnumerable<PdfPath> GetPathsInside(PdfRectangle bound, IEnumerable<PdfPath> paths)
        {
            return paths.Where(b => b.GetBoundingRectangle().HasValue)
                        .Where(b => b.GetBoundingRectangle().Value.Left >= bound.Left &&
                                    b.GetBoundingRectangle().Value.Right <= bound.Right &&
                                    b.GetBoundingRectangle().Value.Bottom >= bound.Bottom &&
                                    b.GetBoundingRectangle().Value.Top <= bound.Top);
        }
    }
}
