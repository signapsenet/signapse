using Microsoft.AspNetCore.Builder;
using Signapse.Exceptions;

namespace Signapse.Server.Middleware
{
    public static class ExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseHttpExceptionHandler(this IApplicationBuilder app)
        {
            return app.Use(async (ctx, next) =>
            {
                try
                {
                    await next(ctx);
                }
                catch (HttpException ex)
                {
                    ex.WriteTo(ctx.Response);
                }
            });
        }
    }
}