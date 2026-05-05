using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestProject.Helpers;

public static class MockHelpers
{
    // Attaches a fake Authorization header to a controller for testing
    public static void SetAuthHeader(ControllerBase controller, string token = "valid-token")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    // Attaches a request with no Authorization header
    public static void SetNoAuthHeader(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }
}