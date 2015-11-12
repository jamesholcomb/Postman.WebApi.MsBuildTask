using System;
using System.Collections.Generic;

namespace Postman.WebApi.MsBuildTask.Models
{
	/// <summary>
	/// A Postman 3 collection representation
	/// </summary>
	/// <remarks>
	/// Doc - https://schema.getpostman.com/json/collection/latest/docs/index.html
	/// Raw - https://schema.getpostman.com/json/collection/v1.0.0/
	/// </remarks>
	public class PostmanCollection
	{
		/// <summary>
		/// Every collection is identified by the unique value of this field. The value of this field is 
		/// usually easiest to generate using a UID generator function. If you already have a collection, 
		/// it is recommended that you maintain the same id since changing the id 
		/// usually implies that this is a different collection than it was originally. 
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// A collection's friendly name is defined by this field. You would want to set this field to a value
		/// that would allow you to easily identify this collection among a bunch of other collections, 
		/// as such outlining its usage or content.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Represents the time the collection was last used
		/// </summary>
		public long Timestamp { get; set; }

		/// <summary>
		///     Requests associated with the collection
		/// </summary>
		public ICollection<PostmanRequest> Requests { get; set; }

		/// <summary>
		/// Folders are the way to go if you want to group your requests and to keep things organised.
		/// Folders can also be useful in sequentially requesting a part of the entire collection by
		/// using Postman Collection Runner or Newman on a particular folder.
		/// </summary>
		public ICollection<PostmanFolder> Folders { get; set; }

		/// <summary>
		/// Provide a long description of this collection using this field. 
		/// This field supports markdown syntax to better format the description.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The order array ensures that your requests and folders don't randomly get shuffled up.
		/// It holds a sequence of UUIDs corresponding to folders and requests.
		/// Note that if a folder ID or a request ID(if the request is not already part of a folder)
		/// is not included in the order array, the request or the folder will not show up in the collection.
		/// </summary>
		public ICollection<Guid> Order { get; set; }
	}
}
