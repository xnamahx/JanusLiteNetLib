namespace Janus
{
	/// <summary>
	/// Options for how network messages are delivered:  ReliableOrdered, ReliableUnordered or Unreliable.
	/// ReliableOrdered is used for establishing connections and creating and deleting timelines
	/// Unreliable is used for Timeline updates
	/// </summary>
	public enum DeliveryMode
	{
        /// <summary>
        /// Unreliable unordered message delivery
        /// </summary>		
        Unreliable,
        /// <summary>
        /// Reliable unordered message delivery
        /// </summary>		
        ReliableUnordered,
        /// <summary>
        /// Reliable ordered message delivery
        /// </summary>		
        Sequenced,
        /// <summary>
        /// Reliable ordered message delivery
        /// </summary>		
        ReliableOrdered,

	}
}