using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Middlewares;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService service) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
	{
		if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested) return false;

		logger.LogError(exception, "Unhandle request error. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
		httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

		httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

		var problem = new ProblemDetails
		{
			Status = StatusCodes.Status500InternalServerError,
			Title = "Internal Server Error",
			Detail = "There is a problem, sir!",
			Instance = httpContext.Request.Path
		};

		problem.Extensions["traceId"] = httpContext.TraceIdentifier;

		var written = await service.TryWriteAsync(new ProblemDetailsContext
		{
			HttpContext = httpContext,
			ProblemDetails = problem,
			Exception = exception
		});

		if (!written)
		{
			httpContext.Response.ContentType = "text/plain; charset=utf-8";
			await httpContext.Response.WriteAsync("Unexpectedly there was an error in the server", ct);
		}

		return true;
	}
}