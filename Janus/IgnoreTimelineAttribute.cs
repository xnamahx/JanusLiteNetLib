using System;

namespace Janus
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class IgnoreTimelineAttribute : Attribute
	{
	}
}