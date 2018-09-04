namespace Janus
{
	/// <summary>
	/// Used for interpolation and extrapolation methods
	/// The values before and after the interpolation (or extrapolation) time are stored in the TimelineContext
	/// and then the interpolate and extrapolate functions use the context to return a timeline value
	/// </summary>
	public class TimelineContext<T>
	{
		/// <summary>
		/// Time to which the context applies
		/// </summary>
		public double Time
		{
			get { return _time; }
		}
		/// <summary>
		/// The value before Time
		/// </summary>
		public TimelineEntry<T> Prev
		{
			get { return _prev; }
		}

		/// <summary>
		/// The value before the value before Time
		/// </summary>
		public TimelineEntry<T> PrevPrev
		{
			get { return _prevPrev; }
		}

		/// <summary>
		/// The value after Time
		/// </summary>
		public TimelineEntry<T> Next
		{
			get { return _next; }
		}

		internal double _time;
		internal TimelineEntry<T> _prev;
		internal TimelineEntry<T> _prevPrev;
		internal TimelineEntry<T> _next;

		/// <summary>
		/// The ith value in a timeline
		/// </summary>
		public TimelineEntry<T> this[int i]
		{
			get
			{
				TimelineEntry<T> target = null;

				for (int j = 0; j < i; j++)
				{
					if (target == null)
						target = _next;
					else target = target._next;

					if (target == null)
						return null;
				}

				for (int j = 0; j > i; j--)
				{
					if (target == null)
						target = _prev;
					else target = target._prev;

					if (target == null)
						return null;
				}

				return target;
			}
		}
	}
}