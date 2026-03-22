namespace ME2LevelEditor.Models
{
    public class IntensitySection
    {
        public IntensityLevel Intensity { get; set; }
        public int StartSample { get; set; }
        public int Length { get; set; }
        public bool IsAngelJump { get; set; }
    }
}
