using System;

namespace Janus
{
	/// <summary>
	/// Used by timeline manager to mark certain timelines with string tags.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class TimelineTagsAttribute : Attribute
	{
		public string[] Tags
		{
			get { return _tags; }
		}

		string[] _tags;

		public TimelineTagsAttribute (params string[] tags)
		{
			_tags = tags;
		}
	}
}