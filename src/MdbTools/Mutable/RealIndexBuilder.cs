namespace MMKiwi.MdbTools.Mutable;
public static partial class MdbBuilder
{
    internal class RealIndex
    {
        public RealIndexColumn[] Columns { get; } = new RealIndexColumn[10];
        public int UsedPages { get; set; }
        public int FirstDataPointer { get; set; }
        public byte Flags { get; set; }
        public int NumIndexRows { get; set; }
    }
}