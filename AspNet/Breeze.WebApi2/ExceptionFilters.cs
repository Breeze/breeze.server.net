using Breeze.ContextProvider;
using System.Net.Http;
using System.Web.Http.Filters;

namespace Breeze.WebApi2 {

    public class EntityErrorsFilterAttribute : ExceptionFilterAttribute {

        public override void OnException(HttpActionExecutedContext context) {
            if (context.Exception is EntityErrorsException) {
                var e = (EntityErrorsException)context.Exception;
                var error = new SaveError(e.Message, e.EntityErrors);
                var resp = new HttpResponseMessage(e.StatusCode) {
                  Content = new ObjectContent(typeof(SaveError), error, JsonFormatter.Create()),
                };
                context.Response = resp;
            }
        }
    }



    // Example code
    //public class NotImplExceptionFilterAttribute : ExceptionFilterAttribute {
    //    public override void OnException(HttpActionExecutedContext context) {
    //        if (context.Exception is NotImplementedException) {
    //            context.Response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
    //        }
    //    }
    //}
    
}
