namespace ME2LevelEditor.Models
{
    public class CacheHeader
    {
        public string Version { get; set; }
        public int TotalSamples { get; set; }
        public double DisplayBPM { get; set; }
        public int BPM { get; set; }
        public bool LoudnessCalculated { get; set; }
        public double? LoudnessLUFS { get; set; }
    }
}
