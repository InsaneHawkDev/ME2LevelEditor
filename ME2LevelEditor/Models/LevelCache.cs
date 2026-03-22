using System.Collections.Generic;

namespace ME2LevelEditor.Models
{
    public class LevelCache
    {
        public CacheHeader Header { get; set; }
        public List<IntensitySection> Sections { get; set; }
        public List<ObstacleDefinition> Obstacles { get; set; }

        public LevelCache()
        {
            Header = new CacheHeader();
            Sections = new List<IntensitySection>();
            Obstacles = new List<ObstacleDefinition>();
        }
    }
}
