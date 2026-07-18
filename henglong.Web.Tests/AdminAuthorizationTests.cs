using henglong.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace henglong.Web.Tests;

public class AdminAuthorizationTests
{
    [Fact]
    public void ProductController_requires_authorization()
    {
        Assert.Contains(typeof(ProductController).GetCustomAttributes(false), item => item is AuthorizeAttribute);
    }

    [Fact]
    public void OrderController_requires_authorization()
    {
        Assert.Contains(typeof(OrderController).GetCustomAttributes(false), item => item is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(typeof(HomeController))]
    [InlineData(typeof(ImageController))]
    [InlineData(typeof(MiniProgramController))]
    [InlineData(typeof(CategoryController))]
    public void Public_controllers_do_not_require_backend_cookie_authorization(Type controllerType)
    {
        Assert.DoesNotContain(controllerType.GetCustomAttributes(false), item => item is AuthorizeAttribute);
    }
}
