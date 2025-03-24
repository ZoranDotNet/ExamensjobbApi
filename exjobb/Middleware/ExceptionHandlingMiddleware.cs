namespace exjobb.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;


        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {

                httpContext.Response.StatusCode = 500;
                httpContext.Response.ContentType = "application/json";

                var errorResponse = new { Message = "An unexpected error occurred." };
                await httpContext.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
