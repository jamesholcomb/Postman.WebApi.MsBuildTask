using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.XPath;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Web.Http.OData;

namespace Postman.WebApi.MsBuildTask
{
	/// <summary>
	/// A custom <see cref="IDocumentationProvider"/> that reads the API documentation from an XML documentation file.
	/// </summary>
	/// <remarks>
	/// Derived from https://goo.gl/lQohpj Apache License Version 2 2014 - Copyright 2015 Ove Andersen
	/// </remarks>
	public class XmlDocumentationProvider : IDocumentationProvider
	{
		private readonly XPathNavigator _documentNavigator;
		private const string TypeExpression = "/doc/members/member[@name='T:{0}']";
		private const string MethodExpression = "/doc/members/member[@name='M:{0}']";
		private const string PropertyExpression = "/doc/members/member[@name='P:{0}']";
		private const string FieldExpression = "/doc/members/member[@name='F:{0}']";
		private const string ParameterExpression = "param[@name='{0}']";

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlDocumentationProvider"/> class.
		/// </summary>
		/// <param name="documentPath">The physical path to XML document.</param>
		public XmlDocumentationProvider(string documentPath)
		{
			if (documentPath == null)
			{
				throw new ArgumentNullException("documentPath");
			}

			XPathDocument xpath = new XPathDocument(documentPath);

			_documentNavigator = xpath.CreateNavigator();
		}

		/// <summary>
		/// Gets the documentation based on <see cref="T:System.Web.Http.Controllers.HttpControllerDescriptor" />.
		/// </summary>
		/// <param name="controllerDescriptor">The controller descriptor.</param>
		/// <returns>The documentation for the controller.</returns>
		public virtual string GetDocumentation(HttpControllerDescriptor controllerDescriptor)
		{
			var typeNode = this.GetTypeNode(controllerDescriptor.ControllerType);

			dynamic doc = new JObject();

			doc.summary = GetTagValue(typeNode, "summary");
			doc.remarks = GetTagValue(typeNode, "remarks");
			doc.authorize = false;

			// Add message if controller requires authorization
			var authorizationAttr = controllerDescriptor.GetCustomAttributes<AuthorizeAttribute>().FirstOrDefault();
			if (authorizationAttr != null)
			{
				doc.authorize = true;
				doc.authorizeUsers = authorizationAttr.Users;
				doc.authorizeRoles = authorizationAttr.Roles;
			}

			return doc.ToString();
		}

		/// <summary>
		/// Gets the documentation based on <see cref="T:System.Web.Http.Controllers.HttpActionDescriptor" />.
		/// </summary>
		/// <param name="actionDescriptor">The action descriptor.</param>
		/// <returns>The documentation for the action.</returns>
		public virtual string GetDocumentation(HttpActionDescriptor actionDescriptor)
		{
			var methodNode = this.GetMethodNode(actionDescriptor);

			dynamic doc = new JObject();

			doc.summary = GetTagValue(methodNode, "summary");
			doc.remarks = GetTagValue(methodNode, "remarks");
			doc.returns = GetTagValue(methodNode, "returns");
			doc.authorize = false;
			doc.obsolete = false;
			doc.oDataQuery = false;

			// Add message if controller requires authorization
			var authorizationAttr = actionDescriptor.GetCustomAttributes<AuthorizeAttribute>().FirstOrDefault();
			if (authorizationAttr != null)
			{
				doc.authorize = true;
				doc.authorizeUsers = authorizationAttr.Users;
				doc.authorizeRoles = authorizationAttr.Roles;
			}

			// Add message if action is marked as Obsolete
			var obsoleteAttr = actionDescriptor.GetCustomAttributes<ObsoleteAttribute>().FirstOrDefault();
			if (obsoleteAttr != null)
			{
				doc.obsolete = true;
				doc.obsoleteMessage = obsoleteAttr.Message;
			}

			// Add message if action is OData Query enabled
			var queryAttr = actionDescriptor.GetCustomAttributes<EnableQueryAttribute>().FirstOrDefault();
			if (queryAttr != null)
			{
				doc.oDataQuery = true;
			}

			return doc.ToString();
		}

		/// <summary>
		/// Gets the documentation based on <see cref="T:System.Web.Http.Controllers.HttpParameterDescriptor" />.
		/// </summary>
		/// <param name="parameterDescriptor">The parameter descriptor.</param>
		/// <returns>The documentation for the parameter.</returns>
		public virtual string GetDocumentation(HttpParameterDescriptor parameterDescriptor)
		{
			var reflectedParameterDescriptor = parameterDescriptor as ReflectedHttpParameterDescriptor;

			if (reflectedParameterDescriptor != null)
			{
				var methodNode = this.GetMethodNode(reflectedParameterDescriptor.ActionDescriptor);

				if (methodNode != null)
				{
					var parameterName = reflectedParameterDescriptor.ParameterInfo.Name;
					var parameterNode = methodNode.SelectSingleNode(String.Format(CultureInfo.InvariantCulture, ParameterExpression, parameterName));

					if (parameterNode != null)
					{
						return parameterNode.Value.Trim();
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the documentation based on <see cref="T:MemberInfo"/>.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <returns>The documentation for the member.</returns>
		public string GetDocumentation(MemberInfo member)
		{
			var memberName = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", GetTypeName(member.DeclaringType), member.Name);
			var expression = member.MemberType == MemberTypes.Field ? FieldExpression : PropertyExpression;
			var selectExpression = String.Format(CultureInfo.InvariantCulture, expression, memberName);

			var n = _documentNavigator.SelectSingleNode(selectExpression);

			if (n != null)
			{
				var s = new[]
					{
							GetTagValue(n, "returns")
						};

				return string.Join(Environment.NewLine, s.Where(x => !string.IsNullOrEmpty(x)));
			}

			return null;
		}

		/// <summary>
		/// Gets the documentation for the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The documentation for the type.</returns>
		public string GetDocumentation(Type type)
		{
			var typeNode = this.GetTypeNode(type);

			var s = new[]
						{
							GetTagValue(typeNode, "summary"),
							GetTagValue(typeNode, "returns")
						};

			return string.Join(Environment.NewLine, s.Where(x => !string.IsNullOrEmpty(x)));
		}

		/// <summary>
		/// Gets the response documentation.
		/// </summary>
		/// <param name="actionDescriptor">The action descriptor.</param>
		/// <returns>A html-formatted string for the action response.</returns>
		public string GetResponseDocumentation(HttpActionDescriptor actionDescriptor)
		{
			var methodNode = this.GetMethodNode(actionDescriptor);

			var s = new[]
						{
							GetTagValue(methodNode, "summary"),
							GetTagValue(methodNode, "returns")
						};

			return string.Join(Environment.NewLine, s.Where(x => !string.IsNullOrEmpty(x)));
		}

		/// <summary>
		/// Gets the method node.
		/// </summary>
		/// <param name="actionDescriptor">The action descriptor.</param>
		/// <returns>A xpath navigator.</returns>
		private XPathNavigator GetMethodNode(HttpActionDescriptor actionDescriptor)
		{
			var reflectedActionDescriptor = actionDescriptor as ReflectedHttpActionDescriptor;

			if (reflectedActionDescriptor != null)
			{
				var selectExpression = string.Format(CultureInfo.InvariantCulture, MethodExpression, GetMemberName(reflectedActionDescriptor.MethodInfo));

				var n = _documentNavigator.SelectSingleNode(selectExpression);

				if (n != null)
				{
					return n;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the type node.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>A xpath navigator.</returns>
		private XPathNavigator GetTypeNode(Type type)
		{
			var controllerTypeName = GetTypeName(type);
			var selectExpression = String.Format(CultureInfo.InvariantCulture, TypeExpression, controllerTypeName);

			var n = _documentNavigator.SelectSingleNode(selectExpression);

			if (n != null)
			{
				return n;
			}

			return null;
		}

		/// <summary>
		/// Gets the member name.
		/// </summary>
		/// <param name="method">The method.</param>
		/// <returns>The member name.</returns>
		private static string GetMemberName(MethodInfo method)
		{
			var name = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", GetTypeName(method.DeclaringType), method.Name);
			var parameters = method.GetParameters();

			if (parameters.Length != 0)
			{
				var parameterTypeNames = parameters.Select(param => GetTypeName(param.ParameterType)).ToArray();
				name += String.Format(CultureInfo.InvariantCulture, "({0})", String.Join(",", parameterTypeNames));
			}

			return name;
		}

		/// <summary>
		/// Gets the type name.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The type name.</returns>
		private static string GetTypeName(Type type)
		{
			var name = type.FullName;

			if (type.IsGenericType)
			{
				// Format the generic type name to something like: Generic{System.Int32,System.String}
				var genericType = type.GetGenericTypeDefinition();
				var genericArguments = type.GetGenericArguments();
				var genericTypeName = genericType.FullName;

				// Trim the generic parameter counts from the name
				genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
				var argumentTypeNames = genericArguments.Select(t => GetTypeName(t)).ToArray();

				name = String.Format(CultureInfo.InvariantCulture, "{0}{{{1}}}", genericTypeName, String.Join(",", argumentTypeNames));
			}

			if (type.IsNested)
			{
				// Changing the nested type name from OuterType+InnerType to OuterType.InnerType to match the XML documentation syntax.
				name = name.Replace("+", ".");
			}

			return name;
		}

		/// <summary>
		/// Gets the tag value.
		/// </summary>
		/// <param name="parentNode">The parent node.</param>
		/// <param name="tagName">The tag name.</param>
		/// <returns>A html-formatted representation of the tag value.</returns>
		private static string GetTagValue(XPathNavigator parentNode, string tagName)
		{
			if (parentNode != null)
			{
				var node = parentNode.SelectSingleNode(tagName);

				if (node != null)
				{
					var v = node.InnerXml.Trim();

					if (string.IsNullOrEmpty(v))
					{
						return string.Empty;
					}

					// Convert xml doc tags to html tags
					var html = ConvertVsXmlDocTagsToHtml(v);

					var c = new Html2Markdown.Converter();
					var md = c.Convert(html);

					return md;
				}
			}

			return null;
		}

		/// <summary>
		/// Converts the XML document tags to HTML.
		/// </summary>
		/// <param name="xml">The input xml.</param>
		/// <returns>A HTML-formatted string.</returns>
		private static string ConvertVsXmlDocTagsToHtml(string xml)
		{
			// remove generics formatting
			xml = xml.Replace("`1", string.Empty);

			// Convert <para> to <p>
			xml = xml.Replace("<para>", "<p>").Replace("</para>", "</p>");

			// Convert <code> to <pre>
			xml = xml.Replace("<code>", "<pre>").Replace("</code>", "</pre>");

			// Convert <c> to <code>
			xml = xml.Replace("<c>", "<code>").Replace("</c>", "</code>");

			// Convert <example> to <samp>
			xml = xml.Replace("<example>", "<samp>").Replace("</example>", "</samp>");

			// Convert <exception cref=""></exception> to <code</code>
			xml = Regex.Replace(xml, @"(.*)<exception cref=""([^""]+)""\s*/>(.*)", @"$1<code>$2</code>$3");

			// Convert <paramref name=""/> to <code/>
			xml = Regex.Replace(xml, @"(.*)<paramref name=""([^""]+)""\s*/>(.*)", @"$1<code>$2</code>$3");

			// Convert <see cref=""/> to <code/>
			xml = Regex.Replace(xml, @"(.*)<see cref=""([^""]+)""\s*/>(.*)", @"$1<code>$2</code>$3");

			// Convert <seealso cref=""/> to <code/>
			xml = Regex.Replace(xml, @"(.*)<seealso cref=""([^""]+)""\s*/>(.*)", @"$1<code>$2</code>$3");

			// Convert <typeparamref name=""/> to <code/>
			xml = Regex.Replace(xml, @"(.*)<typeparamref name=""([^""]+)""\s*/>(.*)", @"$1<code>$2</code>$3");

			return xml;
		}
	}
}
