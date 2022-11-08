namespace MMKiwi.MdbTools.Mutable;
/// <summary>
/// A container class for mutable builders for the publicly visiable, immutable Mdb obejects
/// </summary>
internal static partial class MdbBuilder
{
    /// <summary>
    /// The mutable builder for a future MdbRealIndexColumn class.
    /// </summary>
    internal class RealIndexColumn
    {
        public ushort ColNum { get; set; }
        public byte ColOrder { get; set; }
    }
}