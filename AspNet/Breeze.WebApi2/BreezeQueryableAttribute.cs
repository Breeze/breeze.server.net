#region Assembly System.Web.Http.OData.dll, v5.2.2.0
// ~breeze.server.net\packages\Microsoft.AspNet.WebApi.OData.5.2.2\lib\net45\System.Web.Http.OData.dll
#endregion

using System;
using System.Web.Http.OData;

namespace Breeze.WebApi2
{
  /// <summary>
  /// Extend Web API's <see cref="EnableQueryAttribute"/> for expected Breeze OData-like query support.
  /// </summary>
  /// <remarks>
  /// See http://blogs.msdn.com/b/webdev/archive/2014/03/13/getting-started-with-asp-net-web-api-2-2-for-odata-v4-0.aspx
  /// </remarks>
  [Obsolete("This class is obsolete; use the EnableBreezeQueryAttribute class.")]
  public class BreezeQueryableAttribute : EnableBreezeQueryAttribute {}
}
