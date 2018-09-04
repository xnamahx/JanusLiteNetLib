namespace Janus
{
	/// <summary>
	/// An entry in a Timeline.
	/// Includes the value, the time associated with that value and pointers to the previous and next entries
	/// </summary>
	public class TimelineEntry<T>
	{
		/// <summary>
		/// The time of an entry in a timeline
		/// </summary>
		public double Time
		{
			get { return _time; }
		}

		/// <summary>
		/// Whether or not this entry was cached.
		/// </summary>
		public bool IsCached
		{
			get { return _isCached; }
		}

		/// <summary>
		/// The opposite of IsCached.
		/// </summary>
		public bool IsNew
		{
			get { return !_isCached; }
		}

		/// <summary>
		/// The value of an entry in a timeline
		/// </summary>
		public T Value
		{
			get { return _value; }
		}

		/// <summary>
		/// The entry before this entry in a timeline
		/// </summary>
		public TimelineEntry<T> Prev
		{
			get { return _prev; }
		}

		/// <summary>
		/// The entry after this entry in a timeline
		/// </summary>
		public TimelineEntry<T> Next
		{
			get { return _next; }
		}

		internal TimelineEntry<T> _prev;
		internal TimelineEntry<T> _next;
		internal double _time;
		internal bool _isCached;
		internal bool _sent;
		internal T _value;

		/// <summary>
		/// An entry in a Timeline.
		/// Includes the value, the time associated with that value and pointers to the previous and next entries
		/// </summary>
		/// <param name="time">Time of the entry.</param>
		/// <param name="value">Value of the entry.</param>
		internal TimelineEntry(double time, T value)
		{
			_time = time;
			_value = value;
		}
	}
}