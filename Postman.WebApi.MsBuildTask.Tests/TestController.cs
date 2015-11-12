using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.OData;

namespace Postman.WebApi.MsBuildTask.Tests
{
	/// <summary>
	/// The test content
	/// </summary>
	/// <remarks>
	/// Defines the content used for testing
	/// </remarks>
	public class TestContent
	{
		/// <summary>Gets or sets the identifier.</summary>
		/// <value>The identifier.</value>
		public int Id { get; set; }

		/// <summary>Gets or sets the name.</summary>
		/// <value>The name.</value>
		public string Name { get; set; }
	}

	public class NoDocTestController : ApiController
	{
		public IHttpActionResult Get()
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// The Test Controller
	/// </summary>
	/// <remarks>
	/// This controller does not do much
	/// <para>See <see cref="TestGenericParameter{T1, T2}(Expression{Func{T1, T2, string}})"/>.</para>
	/// </remarks>
	[Authorize]
	public class TestController : ApiController
	{
		/// <summary>Retrieves a list of <see cref="TestContent"/></summary>
		/// <returns>The test content</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		/// <remarks>See help file for documentation</remarks>
		[ResponseType(typeof(IEnumerable<TestContent>))]
		[Authorize(Roles = "admin")]
		public IHttpActionResult GetAll()
		{
			throw new NotImplementedException();
		}

		/// <summary>Some obsolete GET method.</summary>
		/// <param name="myIntParameter">An int parameter</param>
		/// <param name="myStringParamter">A string paramter</param>
		/// <param name="myBoolParameter">if set to <c>true</c> [my bool parameter]</param>
		/// <param name="myIds">A list of integer ids.</param>
		/// <returns></returns>
		/// <exception cref="System.NotImplementedException"></exception>
		/// <remarks>Deprecated, do not use</remarks>
		[Obsolete]
		[ResponseType(typeof(TestContent))]
		[Authorize(Roles = "admin")]
		[HttpGet]
		[EnableQuery]
		[Route("foo/{id}/bar")]
		public IHttpActionResult SomeObsoleteGetMethod(int myIntParameter, string myStringParamter, bool myBoolParameter, List<int> myIds)
		{
			throw new NotImplementedException();
		}

		/// <summary>Retrieves a <see cref="TestContent"/></summary>
		/// <remarks></remarks>
		/// <returns></returns>
		[ResponseType(typeof(TestContent))]
		public IHttpActionResult GetOne(int id)
		{
			throw new NotImplementedException();
		}

		/// <summary>Creates a new <see cref="TestContent"/>.</summary>
		/// <param name="content">The content.</param>
		/// <returns></returns>
		public void Post(TestContent content)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the messages in a queue
		/// </summary>
		/// <param name="typeName">The queue name</param>
		/// <param name="skip">The number of records to skip</param>
		/// <param name="take">The number of records to take</param>
		/// <returns>The list of messages</returns>
		[Route("message/{typeName}")]
		public List<string> GetMessage(string typeName, int? skip, int? take)
		{
			throw new NotImplementedException();
		}

#pragma warning disable SA1614 // Element parameter documentation must have text
		/// <summary>
		/// Test a param tag without description.
		/// </summary>
		/// <param name="p"></param>
		/// <returns>Nothing.</returns>
		internal string TestParamWithoutDescription(string p) => null;
#pragma warning restore SA1614 // Element parameter documentation must have text

		/// <summary>
		/// Test generic reference type.
		/// <para>See <see cref="TestGenericParameter{T1, T2}(Expression{Func{T1, T2, string}})"/>.</para>
		/// </summary>
		/// <returns>Nothing.</returns>
		internal string TestGenericRefence() => null;

		/// <summary>
		/// Test generic parameter type.
		/// <para>See <typeparamref name="T1"/> and <typeparamref name="T2"/>.</para>
		/// </summary>
		/// <typeparam name="T1">Generic type 1.</typeparam>
		/// <typeparam name="T2">Generic type 2.</typeparam>
		/// <param name="expression">The linq expression.</param>
		/// <returns>Nothing.</returns>
		internal string TestGenericParameter<T1, T2>(
			 Expression<Func<T1, T2, string>> expression) =>
			 null;

		/// <summary>
		/// Test generic exception type.
		/// </summary>
		/// <returns>Nothing.</returns>
		/// <exception cref="TestGenericParameter{T1, T2}(Expression{Func{T1, T2, string}})">Just for test.</exception>
		internal string TestGenericException() => null;

		/// <summary>
		/// Test generic exception type.
		/// </summary>
		/// <returns>Nothing.</returns>
		/// <permission cref="TestGenericParameter{T1, T2}(Expression{Func{T1, T2, string}})">Just for test.</permission>
		internal string TestGenericPermission() => null;

		/// <summary>
		/// Test backtick characters in summary comment.
		/// <para>See `should not inside code block`.</para>
		/// <para>See <c>`backtick inside code block`</c></para>
		/// <para>See `<c>code block inside backtick</c>`</para>
		/// </summary>
		/// <returns>Nothing.</returns>
		internal string TestBacktickInSummary() => null;

		/// <summary>
		/// Test generic type.
		/// <para>See <see cref="TestGenericType{T1, T2}"/>.</para>
		/// </summary>
		/// <typeparam name="T1">Generic type 1.</typeparam>
		/// <typeparam name="T2">Generic type 2.</typeparam>
		private class TestGenericType<T1, T2>
		{
			/// <summary>
			/// Test generic method.
			/// <para>See <see cref="TestGenericMethod{T3, T4}"/></para>
			/// </summary>
			/// <typeparam name="T3">Generic type 3.</typeparam>
			/// <typeparam name="T4">Generic type 4.</typeparam>
			/// <returns>Nothing.</returns>
			internal string TestGenericMethod<T3, T4>() => null;
		}
	}
}
