using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace bthr.Api.Filters;

// SQL remains authoritative for constraints, including concurrent writes.
public sealed class DatabaseConstraintFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateException { InnerException: PostgresException error }) return;

        var response = error.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (409, "A record with these unique values already exists, including deleted records."),
            PostgresErrorCodes.ForeignKeyViolation => (400, "A referenced record does not exist or is still in use."),
            PostgresErrorCodes.CheckViolation => (400, "The supplied values do not satisfy the database constraints."),
            PostgresErrorCodes.NotNullViolation => (400, "A required value is missing."),
            PostgresErrorCodes.StringDataRightTruncation => (400, "A text value exceeds the allowed length."),
            PostgresErrorCodes.NumericValueOutOfRange => (400, "A numeric value exceeds the allowed range."),
            _ => (0, "")
        };
        if (response.Item1 == 0) return;
        context.Result = new ObjectResult(new { message = response.Item2 }) { StatusCode = response.Item1 };
        context.ExceptionHandled = true;
    }
}
