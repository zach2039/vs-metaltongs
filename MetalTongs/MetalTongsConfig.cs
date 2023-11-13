using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaltongs
{
    public class MetalTongsConfig
    {
        public static MetalTongsConfig Loaded { get; set; } = new MetalTongsConfig();

        public bool TongsUsageConsumesDurability { get; set; } = true;
    }
}