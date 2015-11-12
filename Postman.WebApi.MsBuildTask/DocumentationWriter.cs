using Conditions;
using System.IO;
using System.Linq;
using System.Web.Http.Description;
using System;

namespace Postman.WebApi.MsBuildTask
{
	/// <summary>
	/// The Postman documentation writer
	/// </summary>
	/// <remarks>
	/// Builds markdown documentation for a Postman request
	/// </remarks>
	public class DocumentationWriter
	{
		public StringWriter Writer { get; set; } = new StringWriter();

		/// <summary>Writes the summary.</summary>
		/// <param name="text">The text.</param>
		public void WriteSummary(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				Writer.WriteLine("**Summary** {0}  ", text);
			}
		}

		/// <summary>Writes the remarks.</summary>
		/// <param name="text">The text.</param>
		public void WriteRemarks(string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				Writer.WriteLine("**Remarks** {0}  ", text);
			}
		}

		/// <summary>Writes the parameters.</summary>
		/// <param name="apiDescription">The API description.</param>
		public void WriteParameters(ApiDescription apiDescription)
		{
			Condition.Requires(apiDescription).IsNotNull();

			if (apiDescription.ParameterDescriptions.Any())
			{
				Writer.WriteLine("**Parameters**  ");
			}

			foreach (var pd in apiDescription.ParameterDescriptions)
			{
				if (pd.ParameterDescriptor != null)
				{
					Writer.Write("`{0}` {1} - ", pd.ParameterDescriptor.ParameterType.GetFriendlyTypeName(), pd.Name);

					if (!string.IsNullOrEmpty(pd.Documentation))
					{
						Writer.Write("_{0}_, ", pd.Documentation);
					}

					if (pd.ParameterDescriptor.DefaultValue != null)
					{
						Writer.Write("default [{0}], ", pd.ParameterDescriptor.DefaultValue);
					}

					Writer.WriteLine("{0}  ", pd.ParameterDescriptor.IsOptional ? "optional" : "required");
				}
			};
		}

		/// <summary>Writes the returns.</summary>
		/// <param name="text">The text.</param>
		/// <param name="responseType">Type of the response.</param>
		public void WriteReturns(string text, string responseType)
		{
			Writer.WriteLine("**Returns**  ");
			Writer.Write("`{0}`", responseType);

			if (!string.IsNullOrEmpty(text))
			{
				Writer.Write(" - _{0}_", text);
			}

			Writer.WriteLine("  ");
		}

		/// <summary>Writes the authorization.</summary>
		/// <param name="authorize">if set to <c>true</c> [authorize].</param>
		/// <param name="users">The users.</param>
		/// <param name="roles">The roles.</param>
		public void WriteAuthorization(bool authorize, string users, string roles)
		{
			if (authorize)
			{
				Writer.Write("**Authorization** Required");
				if (!string.IsNullOrEmpty(users))
				{
					Writer.Write(" _Users: [{0}]_", users);
				}

				if (!string.IsNullOrEmpty(roles))
				{
					Writer.Write(" _Roles: [{0}]_", roles);
				}

				Writer.WriteLine("  ");
			}
		}

		/// <summary>Writes the obsolete.</summary>
		/// <param name="obsolete">if set to <c>true</c> [obsolete].</param>
		/// <param name="text">The text.</param>
		public void WriteObsolete(bool obsolete, string text)
		{
			if (obsolete)
			{
				Writer.Write("**Obsolete**");
				if (!string.IsNullOrEmpty(text))
				{
					Writer.WriteLine(" - _{0}_", text);
				}
				else
				{
					Writer.WriteLine("");
				}
			}
		}

		public void WriteODataQuery(bool oDataQuery)
		{
			if (oDataQuery)
			{
				Writer.Write("_Supports ASP.NET WebApi OData query string syntax_");
			}

		}
	}
}
