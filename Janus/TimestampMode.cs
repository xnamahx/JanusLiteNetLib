namespace Janus
{
	/// <summary>
	/// Options for how timestamps are treated: Absolute, Relative or None.  
	/// The default is Absolute
	/// </summary>
	public enum TimestampMode : byte
	{
		/// <summary>
		/// Timestamp values are absolute time
		/// </summary>
		Absolute = 0,
		/// <summary>
		/// Timestamp values are relative time
		/// </summary>
		Relative = 1,
		/// <summary>
		/// Timestamps are not used
		/// </summary>
		None = 2
	}
}