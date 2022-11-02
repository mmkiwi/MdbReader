namespace MMKiwi.MdbTools.Mutable;
public static partial class MdbBuilder
{
    internal class RealIndex
    {
        public MdbBuilder.RealIndexColumn[] Columns { get; } = new MdbBuilder.RealIndexColumn[10];
        public int UsedPages { get; set; }
        public int FirstDataPointer { get; set; }
        public byte Flags { get; set; }
        public int NumIndexRows { get; set; }
    }
}