using Breeze.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Breeze.AspNetCore {
  /// <summary> Filter to capture and return entity errors </summary>
  public class GlobalExceptionFilter : IExceptionFilter {

    /// <summary> Empty constructor </summary>
    public GlobalExceptionFilter() {}

    /// <summary> Process exceptions to extract EntityErrors and include them in the response </summary>
    public void OnException(ExceptionContext context) {
      var ex = context.Exception;
      var msg = ex.InnerException == null ? ex.Message : ex.Message + "--" + ex.InnerException.Message;

      var statusCode = 500;
      var response = new ErrorDto() {
        Message = msg,
        StackTrace = context.Exception.StackTrace
        
      };

      var eeEx = ex as EntityErrorsException;
      if (eeEx != null) {
        response.Code = (int)eeEx.StatusCode;
        response.EntityErrors = eeEx.EntityErrors;
        statusCode = response.Code;
      }

      context.Result = new ObjectResult(response) {
        StatusCode = statusCode,
        DeclaredType = typeof(ErrorDto)
      };

    }
  }

  /// <summary> Error object returned to the client </summary>
  public class ErrorDto {
    /// <summary> HTTP status code </summary>
    public int Code { get; set; }
    /// <summary> Exception message </summary>
    public string Message { get; set; }
    /// <summary> Exception stack trace </summary>
    public string StackTrace { get; set; }
    /// <summary> Entity validation errors </summary>
    public List<EntityError> EntityErrors { get; set; }

    /// <summary> Return ErrorDto as JSON </summary>
    public override string ToString() {
      return JsonConvert.SerializeObject(this);
    }
  }
}
