using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Postman.WebApi.MsBuildTask.Models
{
	/// <summary>
	/// [Postman](http://getpostman.com) request object
	/// </summary>
	public class PostmanRequest
	{
		/// <summary>
		/// id of request
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// headers associated with the request
		/// </summary>
		public string Headers { get; set; }

		/// <summary>
		/// url of the request
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// path variables of the request
		/// </summary>
		public Dictionary<string, string> PathVariables { get; set; }

		/// <summary>
		/// method of request
		/// </summary>
		public string Method { get; set; }

		/// <summary>
		/// data to be sent with the request.</summary>
		/// <value>The data.</value>
		public string Data { get; set; }

		/// <summary>
		/// raw mode data to be sent with the request
		/// </summary>
		public string RawModeData { get; set; }

		/// <summary>
		/// data mode of reqeust
		/// </summary>
		public string DataMode { get; set; }

		/// <summary>
		/// name of request
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// request description
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// format of description
		/// </summary>
		public string DescriptionFormat { get; set; }

		/// <summary>
		/// time that this request object was generated
		/// </summary>
		public long Time { get; set; }

		/// <summary>
		/// version of the request endpoint
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// request response
		/// </summary>
		public ICollection<string> Responses { get; set; }

		/// <summary>
		/// the id of the collection that the request object belongs to
		/// </summary>
		public Guid CollectionId { get; set; }

		/// <summary>
		/// Folder the request belongs too
		/// </summary>
		public Guid Folder { get; set; }
	}
}