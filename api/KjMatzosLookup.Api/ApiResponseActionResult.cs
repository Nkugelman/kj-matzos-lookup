using BackOffice.Common;
using Microsoft.AspNetCore.Mvc;

namespace KjMatzosLookup.Api;

internal static class ApiResponseActionResult
{
    public static IActionResult From<T>(ApiResponse<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result);

        var status = (int)result.StatusCode;
        if (status < 400)
            status = 500;

        return new ObjectResult(result) { StatusCode = status };
    }
}
