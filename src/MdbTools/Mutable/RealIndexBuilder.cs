namespace MMKiwi.MdbTools.Mutable;/// <summary>
/// A container class for mutable builders for the publicly visiable, immutable Mdb obejects
/// </summary>
internal static partial class MdbBuilder
{
    /// <summary>
    /// The mutable builder for a future MdbRealIndex class.
    /// </summary>
    internal class RealIndex
    {
        public RealIndexColumn[] Columns { get; } = new RealIndexColumn[10];
        public int UsedPages { get; set; }
        public int FirstDataPointer { get; set; }
        public byte Flags { get; set; }
        public int NumIndexRows { get; set; }
    }
}