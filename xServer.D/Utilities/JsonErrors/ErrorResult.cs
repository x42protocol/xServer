using Microsoft.AspNetCore.Mvc;

namespace x42.Utilities.JsonErrors
{
    public class ErrorResult : ObjectResult
    {
        public ErrorResult(int statusCode, ErrorResponse value) : base(value)
        {
            StatusCode = statusCode;
        }
    }
}