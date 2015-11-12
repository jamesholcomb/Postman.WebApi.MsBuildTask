using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;

namespace Postman.WebApi.MsBuildTask
{
	public static class Extensions
	{
		/// <summary>Converts byte-array to hex.</summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns></returns>
		public static string ToStringBase16(this byte[] buffer)
		{
			return buffer.Aggregate(string.Empty, (result, item) => (result += item.ToString("X2")));
		}

		/// <summary>Converts to unix milliseconds.</summary>
		/// <param name="dt">The datetime.</param>
		/// <returns></returns>
		public static long ToUnixMilliseconds(this DateTime dt)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt64((dt.ToUniversalTime() - epoch).TotalMilliseconds);
		}

		/// <summary>
		/// Sets the documentation provider.
		/// </summary>
		/// <param name="config">The <see cref="HttpConfiguration"/>.</param>
		/// <param name="documentationProvider">The documentation provider.</param>
		public static void SetDocumentationProvider(this HttpConfiguration config, IDocumentationProvider documentationProvider)
		{
			config.Services.Replace(typeof(IDocumentationProvider), documentationProvider);
		}

		/// <summary>Generates the formatted string of the generic type.</summary>
		/// <param name="t">The t.</param>
		/// <returns></returns>
		public static string GetFriendlyTypeName(this Type type)
		{
			if (type == null)
				return "void";
			if (type == typeof(int))
				return "int";
			else if (type == typeof(short))
				return "short";
			else if (type == typeof(byte))
				return "byte";
			else if (type == typeof(bool))
				return "bool";
			else if (type == typeof(long))
				return "long";
			else if (type == typeof(float))
				return "float";
			else if (type == typeof(double))
				return "double";
			else if (type == typeof(decimal))
				return "decimal";
			else if (type == typeof(string))
				return "string";
			else if (type.IsGenericType)
				return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => GetFriendlyTypeName(x)).ToArray()) + ">";
			else
				return type.Name;
		}
	}
}