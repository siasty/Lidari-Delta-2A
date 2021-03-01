using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace serialPortStd.Models
{
    class FramePoint
    {
        public int point { get; set; }
        public double anglePoint { get; set; }
        public double pointValue { get; set; }
    }

    class HexFrameModel
    {
        public string FrameHeader { get; set; }
        public UInt16 FrameLength { get; set; }
        public string ProtocolVersion { get; set; }
        public string FrameType { get; set; }
        public string CommandWord { get; set; }
        public int EffectiveDataLength { get; set; }
        public double RadarSpeed { get; set; }
        public string ZeroOffset { get; set; }
        public double StartingAngle { get; set; }
        public List<FramePoint> framePoint { get; set; }

        public static readonly HexFrameModel Empty = new HexFrameModel();

        public Boolean IsEmpty()
        {
            return Empty.Equals(this);
        }
    }
}
