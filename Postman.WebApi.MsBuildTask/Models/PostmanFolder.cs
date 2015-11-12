using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Postman.WebApi.MsBuildTask.Models
{
	public class PostmanFolder
	{
		/// <summary>
		///     id of the folder
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		///     folder name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     folder description
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		///     ordered list of ids of items in folder
		/// </summary>
		public ICollection<Guid> Order { get; set; }

		///// <summary>
		/////     Name of the collection
		///// </summary>
		//[JsonProperty(PropertyName = "collection_name")]
		//public string CollectionName { get; set; }

		/// <summary>
		///     id of the collection
		/// </summary>
		[JsonProperty(PropertyName = "collection_id")]
		public Guid CollectionId { get; set; }
	}
}
