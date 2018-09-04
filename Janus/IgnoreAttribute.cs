using System;

namespace Janus
{
	/// <summary>
	/// Used by timeline manager to prevent some Timelines from being added
	/// </summary>

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class IgnoreAttribute : Attribute
	{
	}
}