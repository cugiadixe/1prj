using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTKD.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace PTKD.API.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is FluentValidation.ValidationException fluentEx)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Type = "https://ptkd-erp.internal/docs/errors/validation"
            };
            problemDetails.Extensions["errorCode"] = "ORG_VALIDATION_FAILED";
            problemDetails.Extensions["validationErrors"] = fluentEx.Errors;
            SetResult(context, problemDetails, StatusCodes.Status400BadRequest);
        }
        else if (context.Exception is FormatException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Malformed Row Version",
                Detail = "The provided row version is malformed.",
                Type = "https://ptkd-erp.internal/docs/errors/malformed-row-version"
            };
            problemDetails.Extensions["errorCode"] = "ORG_MALFORMED_ROW_VERSION";
            SetResult(context, problemDetails, StatusCodes.Status400BadRequest);
        }
        else if (context.Exception is BusinessRuleValidationException validationEx)
        {
            if (validationEx.ErrorCode == "ORG_HIERARCHY_CYCLE_DETECTED")
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Hierarchy Cycle Detected",
                    Detail = validationEx.Message,
                    Type = "https://ptkd-erp.internal/docs/errors/business-rule"
                };
                problemDetails.Extensions["errorCode"] = validationEx.ErrorCode;
                SetResult(context, problemDetails, StatusCodes.Status400BadRequest);
            }
            else
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Business Rule Validation Error",
                    Detail = validationEx.Message,
                    Type = "https://ptkd-erp.internal/docs/errors/business-rule"
                };
                // Map specific errors like ORG_TEMPORAL_OVERLAP or stable duplicate codes
                problemDetails.Extensions["errorCode"] = validationEx.ErrorCode;
                SetResult(context, problemDetails, StatusCodes.Status409Conflict);
            }
        }
        else if (context.Exception is ConcurrencyException concurrencyEx)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = concurrencyEx.Message,
                Type = "https://ptkd-erp.internal/docs/errors/concurrency"
            };
            problemDetails.Extensions["errorCode"] = "ORG_INVALID_ROW_VERSION";
            SetResult(context, problemDetails, StatusCodes.Status409Conflict);
        }
        else if (context.Exception is DbUpdateConcurrencyException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = "The entity has been modified by another process.",
                Type = "https://ptkd-erp.internal/docs/errors/concurrency"
            };
            problemDetails.Extensions["errorCode"] = "ORG_INVALID_ROW_VERSION";
            SetResult(context, problemDetails, StatusCodes.Status409Conflict);
        }
        else if (context.Exception is EntityNotFoundException notFoundEx)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Entity Not Found",
                Detail = notFoundEx.Message,
                Type = "https://ptkd-erp.internal/docs/errors/not-found"
            };
            problemDetails.Extensions["errorCode"] = notFoundEx.ErrorCode;
            SetResult(context, problemDetails, StatusCodes.Status404NotFound);
        }
        else if (context.Exception is RetryLimitExceededException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Transaction Retry Exhausted",
                Detail = "The operation could not be completed after maximum retries due to deadlocks.",
                Type = "https://ptkd-erp.internal/docs/errors/transaction-retry"
            };
            problemDetails.Extensions["errorCode"] = "ORG_TRANSACTION_RETRY_EXHAUSTED";
            SetResult(context, problemDetails, StatusCodes.Status503ServiceUnavailable);
        }
        else if (context.Exception is SqlException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Database Error",
                Detail = "An unexpected database error occurred.",
                Type = "https://ptkd-erp.internal/docs/errors/database"
            };
            problemDetails.Extensions["errorCode"] = "ORG_UNEXPECTED_DATABASE_ERROR";
            SetResult(context, problemDetails, StatusCodes.Status500InternalServerError);
        }
        else
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://ptkd-erp.internal/docs/errors/internal"
            };
            SetResult(context, problemDetails, StatusCodes.Status500InternalServerError);
        }
    }

    private void SetResult(ExceptionContext context, ProblemDetails problemDetails, int statusCode)
    {
        // Add CorrelationId to ProblemDetails if it exists
        if (context.HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }
        else if (context.HttpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var headerCorrelationId))
        {
            problemDetails.Extensions["correlationId"] = headerCorrelationId.ToString();
        }

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
