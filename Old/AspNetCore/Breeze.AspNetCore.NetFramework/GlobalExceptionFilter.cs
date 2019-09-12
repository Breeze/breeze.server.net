using Breeze.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Breeze.AspNetCore {
  public class GlobalExceptionFilter : IExceptionFilter {

    public GlobalExceptionFilter() {
      
    }

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

  public class ErrorDto {
    public int Code { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public List<EntityError> EntityErrors { get; set; }

    // other fields

    public override string ToString() {
      return JsonConvert.SerializeObject(this);
    }
  }
}
