using System.IO;
using System.Linq;
using System.Text;
using ME2LevelEditor.Models;
using ME2LevelEditor.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ME2LevelEditor.Tests.Services
{
    [TestClass]
    public class CacheFileServiceTests
    {
        private CacheFileService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new CacheFileService();
        }

        [TestMethod]
        public void ParseLines_V081Header_ParsesAllFields()
        {
            var lines = new[]
            {
                "0.8.1",
                "7126;165.44;111;0",
                "H:138-34;N:450-53;H:868-23;",
                "23:Z;80:S;763:S-20;"
            };

            var result = _service.ParseLines(lines);

            Assert.AreEqual("0.8.1", result.Header.Version);
            Assert.AreEqual(7126, result.Header.TotalSamples);
            Assert.AreEqual(165.44, result.Header.DisplayBPM);
            Assert.AreEqual(111, result.Header.BPM);
            Assert.IsFalse(result.Header.LoudnessCalculated);
            Assert.IsNull(result.Header.LoudnessLUFS);
        }

        [TestMethod]
        public void ParseLines_V084Header_ParsesLUFS()
        {
            var lines = new[]
            {
                "0.8.4",
                "9796;227.46;167;0;-15.27",
                "N:66-34;L:560-81;",
                "100:S-86;205:Z;"
            };

            var result = _service.ParseLines(lines);

            Assert.AreEqual("0.8.4", result.Header.Version);
            Assert.AreEqual(9796, result.Header.TotalSamples);
            Assert.AreEqual(227.46, result.Header.DisplayBPM);
            Assert.AreEqual(167, result.Header.BPM);
            Assert.IsFalse(result.Header.LoudnessCalculated);
            Assert.AreEqual(-15.27, result.Header.LoudnessLUFS);
        }

        [TestMethod]
        public void ParseLines_V081WithLoudnessCalculated_MapsToTrue()
        {
            var lines = new[]
            {
                "0.8.1",
                "7126;165.44;111;1",
                "H:138-34;",
                "23:Z;"
            };

            var result = _service.ParseLines(lines);

            Assert.IsTrue(result.Header.LoudnessCalculated);
        }

        [TestMethod]
        public void ParseLines_SectionsWithAngelJump_ParsesFlag()
        {
            var lines = new[]
            {
                "0.8.1",
                "7126;165.44;111;0",
                "H:138-34-A;N:450-53;",
                "23:Z;"
            };

            var result = _service.ParseLines(lines);

            Assert.AreEqual(2, result.Sections.Count);
            Assert.IsTrue(result.Sections[0].IsAngelJump);
            Assert.AreEqual(IntensityLevel.High, result.Sections[0].Intensity);
            Assert.AreEqual(138, result.Sections[0].StartSample);
            Assert.AreEqual(34, result.Sections[0].Length);
            Assert.IsFalse(result.Sections[1].IsAngelJump);
        }

        [TestMethod]
        public void ParseLines_Obstacles_ParsesAllTypes()
        {
            var lines = new[]
            {
                "0.8.1",
                "7126;165.44;111;0",
                "H:138-34;",
                "23:Z;80:S;763:S-20;"
            };

            var result = _service.ParseLines(lines);

            Assert.AreEqual(3, result.Obstacles.Count);

            Assert.AreEqual(23, result.Obstacles[0].SampleId);
            Assert.AreEqual(ObstacleType.Zone, result.Obstacles[0].Type);
            Assert.IsNull(result.Obstacles[0].HeldDuration);

            Assert.AreEqual(80, result.Obstacles[1].SampleId);
            Assert.AreEqual(ObstacleType.Solid, result.Obstacles[1].Type);
            Assert.IsNull(result.Obstacles[1].HeldDuration);

            Assert.AreEqual(763, result.Obstacles[2].SampleId);
            Assert.AreEqual(ObstacleType.Solid, result.Obstacles[2].Type);
            Assert.AreEqual(20, result.Obstacles[2].HeldDuration);
        }

        [TestMethod]
        public void ParseLines_ExtremeSections_ParsesCorrectly()
        {
            var lines = new[]
            {
                "0.8.4",
                "9796;227.46;167;0;-15.27",
                "E:100-50;N:200-30;",
                "100:S;"
            };

            var result = _service.ParseLines(lines);

            Assert.AreEqual(IntensityLevel.Extreme, result.Sections[0].Intensity);
            Assert.AreEqual(100, result.Sections[0].StartSample);
            Assert.AreEqual(50, result.Sections[0].Length);
        }

        [TestMethod]
        public void RoundTrip_SerializeAndParse_PreservesAllValues()
        {
            var original = new LevelCache();
            original.Header.Version = "0.8.4";
            original.Header.TotalSamples = 9796;
            original.Header.DisplayBPM = 227.46;
            original.Header.BPM = 167;
            original.Header.LoudnessCalculated = true;
            original.Header.LoudnessLUFS = -15.27;

            original.Sections.Add(new IntensitySection
            {
                Intensity = IntensityLevel.High,
                StartSample = 138,
                Length = 34,
                IsAngelJump = true
            });
            original.Sections.Add(new IntensitySection
            {
                Intensity = IntensityLevel.Extreme,
                StartSample = 500,
                Length = 100,
                IsAngelJump = false
            });

            original.Obstacles.Add(new ObstacleDefinition
            {
                SampleId = 23,
                Type = ObstacleType.Zone
            });
            original.Obstacles.Add(new ObstacleDefinition
            {
                SampleId = 763,
                Type = ObstacleType.Solid,
                HeldDuration = 20
            });

            var serialized = _service.SerializeToLines(original);
            var parsed = _service.ParseLines(serialized);

            Assert.AreEqual(original.Header.Version, parsed.Header.Version);
            Assert.AreEqual(original.Header.TotalSamples, parsed.Header.TotalSamples);
            Assert.AreEqual(original.Header.DisplayBPM, parsed.Header.DisplayBPM);
            Assert.AreEqual(original.Header.BPM, parsed.Header.BPM);
            Assert.AreEqual(original.Header.LoudnessCalculated, parsed.Header.LoudnessCalculated);
            Assert.AreEqual(original.Header.LoudnessLUFS, parsed.Header.LoudnessLUFS);

            Assert.AreEqual(original.Sections.Count, parsed.Sections.Count);
            for (int i = 0; i < original.Sections.Count; i++)
            {
                Assert.AreEqual(original.Sections[i].Intensity, parsed.Sections[i].Intensity);
                Assert.AreEqual(original.Sections[i].StartSample, parsed.Sections[i].StartSample);
                Assert.AreEqual(original.Sections[i].Length, parsed.Sections[i].Length);
                Assert.AreEqual(original.Sections[i].IsAngelJump, parsed.Sections[i].IsAngelJump);
            }

            Assert.AreEqual(original.Obstacles.Count, parsed.Obstacles.Count);
            for (int i = 0; i < original.Obstacles.Count; i++)
            {
                Assert.AreEqual(original.Obstacles[i].SampleId, parsed.Obstacles[i].SampleId);
                Assert.AreEqual(original.Obstacles[i].Type, parsed.Obstacles[i].Type);
                Assert.AreEqual(original.Obstacles[i].HeldDuration, parsed.Obstacles[i].HeldDuration);
            }
        }

        [TestMethod]
        public void SerializeToLines_TrailingSemicolons_PresentOnLines3And4()
        {
            var cache = new LevelCache();
            cache.Header.Version = "0.8.1";
            cache.Header.TotalSamples = 100;
            cache.Header.DisplayBPM = 120.0;
            cache.Header.BPM = 120;

            cache.Sections.Add(new IntensitySection
            {
                Intensity = IntensityLevel.Normal,
                StartSample = 10,
                Length = 20
            });

            cache.Obstacles.Add(new ObstacleDefinition
            {
                SampleId = 5,
                Type = ObstacleType.Zone
            });

            var lines = _service.SerializeToLines(cache);

            Assert.IsTrue(lines[2].EndsWith(";"));
            Assert.IsTrue(lines[3].EndsWith(";"));
        }

        [TestMethod]
        public void SerializeToLines_V081_NoLUFSField()
        {
            var cache = new LevelCache();
            cache.Header.Version = "0.8.1";
            cache.Header.TotalSamples = 7126;
            cache.Header.DisplayBPM = 165.44;
            cache.Header.BPM = 111;
            cache.Header.LoudnessCalculated = false;

            cache.Sections.Add(new IntensitySection
            {
                Intensity = IntensityLevel.Normal,
                StartSample = 10,
                Length = 20
            });
            cache.Obstacles.Add(new ObstacleDefinition
            {
                SampleId = 5,
                Type = ObstacleType.Zone
            });

            var lines = _service.SerializeToLines(cache);

            Assert.AreEqual("7126;165.44;111;0", lines[1]);
            Assert.IsFalse(lines[1].Contains("-15"));
        }

        [TestMethod]
        public void CreateEmpty_ReturnsValidEmptyCache()
        {
            var cache = _service.CreateEmpty("0.8.4");

            Assert.IsNotNull(cache);
            Assert.IsNotNull(cache.Header);
            Assert.AreEqual("0.8.4", cache.Header.Version);
            Assert.IsNotNull(cache.Sections);
            Assert.AreEqual(0, cache.Sections.Count);
            Assert.IsNotNull(cache.Obstacles);
            Assert.AreEqual(0, cache.Obstacles.Count);
        }

        private static string FindLevelCacheDir()
        {
            var dir = Path.GetDirectoryName(typeof(CacheFileServiceTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "level-cache");
                if (Directory.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        [TestMethod]
        public void RoundTrip_RealFile_Situations_IdenticalOutput()
        {
            var cacheDir = FindLevelCacheDir();
            if (cacheDir == null) Assert.Inconclusive("level-cache directory not found");
            var path = Path.Combine(cacheDir, "Situations.mp3_1.txt");
            if (!File.Exists(path)) Assert.Inconclusive("Real cache file not found: " + path);

            var originalLines = File.ReadAllLines(path, Encoding.ASCII);
            var cache = _service.ParseLines(originalLines);
            var serializedLines = _service.SerializeToLines(cache);

            Assert.AreEqual(originalLines.Length, serializedLines.Length, "Line count mismatch");
            for (int i = 0; i < originalLines.Length; i++)
                Assert.AreEqual(originalLines[i], serializedLines[i], $"Mismatch on line {i}");
        }

        [TestMethod]
        public void RoundTrip_RealFile_AreYouReady_IdenticalOutput()
        {
            var cacheDir = FindLevelCacheDir();
            if (cacheDir == null) Assert.Inconclusive("level-cache directory not found");
            var path = Path.Combine(cacheDir, "Are You Ready!.wav_2.txt");
            if (!File.Exists(path)) Assert.Inconclusive("Real cache file not found: " + path);

            var originalLines = File.ReadAllLines(path, Encoding.ASCII);
            var cache = _service.ParseLines(originalLines);
            var serializedLines = _service.SerializeToLines(cache);

            Assert.AreEqual(originalLines.Length, serializedLines.Length, "Line count mismatch");
            for (int i = 0; i < originalLines.Length; i++)
                Assert.AreEqual(originalLines[i], serializedLines[i], $"Mismatch on line {i}");
        }
    }
}
