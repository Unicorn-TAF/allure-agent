﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.Visualization
{
    public static class VisualizerCircles
    {
        private static Random random = new Random();
        private static int margin = 30;
        private static List<Rect> rects = new List<Rect>();

        private static IPalette palette;

        public static void VisualizeAllData(AutomationData data, FilterType filterType, IPalette activePalette, Canvas canvas)
        {
            palette = activePalette;
            canvas.Background = palette.BackColor;
            rects.Clear();
            canvas.Children.Clear();

            var stats = GetStats(data, filterType);

            int max = stats.Values.Max();
            int featuresCount = stats.Values.Count;

            var items = from pair in stats
                        orderby pair.Value descending
                        select pair;

            int currentIndex = 0;

            foreach (KeyValuePair<string, int> pair in items)
            {
                int radius = CalculateRadius(pair.Value, max, featuresCount, (int)canvas.RenderSize.Width);
                DrawFeature(pair.Key, pair.Value, radius, currentIndex++, featuresCount, canvas);
            }
        }

        public static void VisualizeCoverage(AppSpecs specs, IPalette activePalette, Canvas canvas)
        {
            palette = activePalette;
            canvas.Background = palette.BackColor;
            rects.Clear();
            canvas.Children.Clear();

            var featuresStats = new Dictionary<string, int>();

            foreach (var module in specs.Modules)
            {
                var tests = from SuiteInfo s
                            in module.Suites
                            select s.TestsInfos;

                featuresStats.Add(module.Name, tests.Sum(t => t.Count));
            }

            int max = featuresStats.Values.Max();
            int featuresCount = featuresStats.Values.Count;

            var items = from pair in featuresStats
                        orderby pair.Value descending
                        select pair;

            int currentIndex = 0;

            foreach (KeyValuePair<string, int> pair in items)
            {
                int radius = CalculateRadius(pair.Value, max, featuresCount, (int)canvas.RenderSize.Width);
                DrawFeature(pair.Key, pair.Value, radius, currentIndex++, featuresCount, canvas);
            }
        }

        public static Dictionary<string, int> GetStats(AutomationData data, FilterType filterType)
        {
            var stats = new Dictionary<string, int>();

            switch (filterType)
            {
                case FilterType.Feature:
                    {
                        foreach (var feature in data.UniqueFeatures)
                        {
                            var suites = data.SuitesInfos.Where(s => s.Features.Contains(feature));
                            var tests = from SuiteInfo s
                                        in suites
                                        select s.TestsInfos;

                            stats.Add(feature, tests.Sum(t => t.Count));
                        }

                        return stats;
                    }

                case FilterType.Category:
                    {
                        foreach (var category in data.UniqueCategories)
                        {
                            var tests = from SuiteInfo s
                                        in data.SuitesInfos
                                        select s.TestsInfos.Where(ti => ti.Categories.Contains(category));

                            stats.Add(category, tests.Sum(t => t.Count()));
                        }

                        return stats;
                    }

                case FilterType.Author:
                    {
                        foreach (var author in data.UniqueAuthors)
                        {
                            var tests = from SuiteInfo s
                                        in data.SuitesInfos
                                        select s.TestsInfos.Where(ti => ti.Author.Equals(author));

                            stats.Add(author, tests.Sum(t => t.Count()));
                        }

                        return stats;
                    }
            }

            throw new ArgumentException("please check args");
        }

        private static void DrawFeature(string name, int tests, int radius, int index, int featuresCount, Canvas canvas)
        {
            int x = 0;
            int y = 0;
            Rect rect;

            do
            {
                x = random.Next(margin + radius, (int)canvas.RenderSize.Width - radius - margin);
                y = random.Next(margin + radius, (int)canvas.RenderSize.Height - radius - margin);

                rect = new Rect(x - radius - margin, y - radius - margin, (radius + margin) * 2, (radius + margin) * 2);
            }
            while (rects.Any(r => r.IntersectsWith(rect)));

            rects.Add(rect);

            double colorIndexStep = (double)palette.DataColors.Count / featuresCount;
            int currentColorIndex = (int)(((index + 1) * colorIndexStep) - 1);

            var ellipse = new Ellipse()
            {
                Fill = palette.DataColors[currentColorIndex],
                Width = radius * 2,
                Height = radius * 2,
                StrokeThickness = 0.1,
                Stroke = Brushes.Black,
                ToolTip = tests
            };

            Canvas.SetLeft(ellipse, x - radius);
            Canvas.SetTop(ellipse, y - radius);
            canvas.Children.Add(ellipse);

            var label = new TextBlock
            {
                Text = CamelCase($"{name}: {tests}"),
                TextAlignment = TextAlignment.Center,
                FontFamily = new FontFamily("Calibri"),
                FontSize = 15,
                Foreground = palette.DataFontColor
            };

            var formattedText = new FormattedText(
                label.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch),
                label.FontSize,
                Brushes.Black,
                new NumberSubstitution(), 
                TextFormattingMode.Display);

            canvas.Children.Add(label);
            Canvas.SetLeft(label, x - (formattedText.Width / 2));
            Canvas.SetTop(label, y - radius - formattedText.Height);

            ////var labelCount = new TextBlock
            ////{
            ////    Text = tests.ToString(),
            ////    TextAlignment = TextAlignment.Center,
            ////    FontFamily = new FontFamily("Calibri"),
            ////    FontSize = 15,
            ////    Foreground = palette.DataFontColor
            ////};

            ////canvas.Children.Add(labelCount);
            ////Canvas.SetLeft(labelCount, x - (formattedText.Width / 2));
            ////Canvas.SetTop(labelCount, y - (formattedText.Height / 2));
        }

        private static int CalculateRadius(int capacity, int max, int count, int canvasSize)
        {
            if (capacity == 0)
            {
                return 1;
            }

            double radius = (double)canvasSize / Math.Sqrt(count + margin);
            double ratio = (double)capacity / (double)max;
            return (int)(radius * ratio / 2);
        }

        private static string CamelCase(string s)
        {
            var x = s.Replace("_", "");
            if (x.Length == 0) return "Null";
            x = Regex.Replace(x, "([A-Z])([A-Z]+)",
                m => m.Groups[1].Value + m.Groups[2].Value.ToLower());
            return char.ToUpper(x[0]) + x.Substring(1);
        }
    }
}
