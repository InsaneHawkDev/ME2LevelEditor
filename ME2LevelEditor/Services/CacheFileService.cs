using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ME2LevelEditor.Models;

namespace ME2LevelEditor.Services
{
    public class CacheFileService
    {
        public LevelCache LoadFromFile(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path, Encoding.ASCII);
                if (lines.Length < 4)
                    throw new InvalidDataException($"Cache file must have at least 4 lines, found {lines.Length}.");
                return ParseLines(lines);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException($"Failed to read cache file: {path}", ex);
            }
        }

        public void SaveToFile(string path, LevelCache cache)
        {
            try
            {
                var lines = SerializeToLines(cache);
                File.WriteAllText(path, string.Join("\r\n", lines), Encoding.ASCII);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException($"Failed to save cache file: {path}", ex);
            }
        }

        public LevelCache ParseLines(string[] lines)
        {
            var cache = new LevelCache();

            cache.Header.Version = lines[0].Trim();

            var headerParts = lines[1].Split(';');
            cache.Header.TotalSamples = int.Parse(headerParts[0], CultureInfo.InvariantCulture);
            cache.Header.DisplayBPM = double.Parse(headerParts[1], CultureInfo.InvariantCulture);
            cache.Header.BPM = int.Parse(headerParts[2], CultureInfo.InvariantCulture);
            cache.Header.LoudnessCalculated = int.Parse(headerParts[3], CultureInfo.InvariantCulture) != 0;

            if (headerParts.Length >= 5 && !string.IsNullOrEmpty(headerParts[4]))
            {
                cache.Header.LoudnessLUFS = double.Parse(headerParts[4], CultureInfo.InvariantCulture);
            }

            var sectionTokens = lines[2].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in sectionTokens)
            {
                var section = new IntensitySection();

                section.Intensity = ParseIntensityLetter(token[0]);

                var afterColon = token.Substring(2);
                var isAngel = afterColon.EndsWith("-A");
                if (isAngel)
                {
                    afterColon = afterColon.Substring(0, afterColon.Length - 2);
                }
                section.IsAngelJump = isAngel;

                var dashIndex = afterColon.IndexOf('-');
                section.StartSample = int.Parse(afterColon.Substring(0, dashIndex), CultureInfo.InvariantCulture);
                section.Length = int.Parse(afterColon.Substring(dashIndex + 1), CultureInfo.InvariantCulture);

                cache.Sections.Add(section);
            }

            var obstacleTokens = lines[3].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in obstacleTokens)
            {
                var obstacle = new ObstacleDefinition();

                var colonIndex = token.IndexOf(':');
                obstacle.SampleId = int.Parse(token.Substring(0, colonIndex), CultureInfo.InvariantCulture);

                var typePart = token.Substring(colonIndex + 1);
                if (typePart.StartsWith("Z"))
                {
                    obstacle.Type = ObstacleType.Zone;
                }
                else if (typePart.StartsWith("S"))
                {
                    obstacle.Type = ObstacleType.Solid;
                    var dashIndex = typePart.IndexOf('-');
                    if (dashIndex >= 0)
                    {
                        obstacle.HeldDuration = int.Parse(typePart.Substring(dashIndex + 1), CultureInfo.InvariantCulture);
                    }
                }

                cache.Obstacles.Add(obstacle);
            }

            return cache;
        }

        public string[] SerializeToLines(LevelCache cache)
        {
            var lines = new string[4];

            lines[0] = cache.Header.Version;

            var headerBuilder = new StringBuilder();
            headerBuilder.Append(cache.Header.TotalSamples.ToString(CultureInfo.InvariantCulture));
            headerBuilder.Append(';');
            headerBuilder.Append(cache.Header.DisplayBPM.ToString(CultureInfo.InvariantCulture));
            headerBuilder.Append(';');
            headerBuilder.Append(cache.Header.BPM.ToString(CultureInfo.InvariantCulture));
            headerBuilder.Append(';');
            headerBuilder.Append(cache.Header.LoudnessCalculated ? "1" : "0");
            if (cache.Header.LoudnessLUFS.HasValue)
            {
                headerBuilder.Append(';');
                headerBuilder.Append(cache.Header.LoudnessLUFS.Value.ToString(CultureInfo.InvariantCulture));
            }
            lines[1] = headerBuilder.ToString();

            var sectionsBuilder = new StringBuilder();
            foreach (var section in cache.Sections.OrderBy(s => s.StartSample))
            {
                sectionsBuilder.Append(IntensityToLetter(section.Intensity));
                sectionsBuilder.Append(':');
                sectionsBuilder.Append(section.StartSample.ToString(CultureInfo.InvariantCulture));
                sectionsBuilder.Append('-');
                sectionsBuilder.Append(section.Length.ToString(CultureInfo.InvariantCulture));
                if (section.IsAngelJump)
                {
                    sectionsBuilder.Append("-A");
                }
                sectionsBuilder.Append(';');
            }
            lines[2] = sectionsBuilder.ToString();

            var obstaclesBuilder = new StringBuilder();
            foreach (var obstacle in cache.Obstacles.OrderBy(o => o.SampleId))
            {
                obstaclesBuilder.Append(obstacle.SampleId.ToString(CultureInfo.InvariantCulture));
                obstaclesBuilder.Append(':');
                obstaclesBuilder.Append(obstacle.Type == ObstacleType.Zone ? "Z" : "S");
                if (obstacle.HeldDuration.HasValue)
                {
                    obstaclesBuilder.Append('-');
                    obstaclesBuilder.Append(obstacle.HeldDuration.Value.ToString(CultureInfo.InvariantCulture));
                }
                obstaclesBuilder.Append(';');
            }
            lines[3] = obstaclesBuilder.ToString();

            return lines;
        }

        public LevelCache CreateEmpty(string version)
        {
            var cache = new LevelCache();
            cache.Header.Version = version;
            return cache;
        }

        private static IntensityLevel ParseIntensityLetter(char letter)
        {
            switch (letter)
            {
                case 'L': return IntensityLevel.Low;
                case 'N': return IntensityLevel.Normal;
                case 'H': return IntensityLevel.High;
                case 'E': return IntensityLevel.Extreme;
                default: throw new ArgumentException($"Unknown intensity letter: {letter}");
            }
        }

        private static char IntensityToLetter(IntensityLevel level)
        {
            switch (level)
            {
                case IntensityLevel.Low: return 'L';
                case IntensityLevel.Normal: return 'N';
                case IntensityLevel.High: return 'H';
                case IntensityLevel.Extreme: return 'E';
                default: throw new ArgumentException($"Unknown intensity level: {level}");
            }
        }
    }
}
