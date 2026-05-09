using Attendance_Management_System.Backend.Controllers;
using Attendance_Management_System.Backend.ViewModels.Auth;
using Npgsql;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Attendance_Management_System.Tests;

public class AccountControllerLoginTests
{
    [Fact]
    public async Task Login_ReturnsInvalidCredentials_WhenSignInFails()
    {
        var signInManagerMock = CreateSignInManager();
        signInManagerMock
            .Setup(manager => manager.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                false))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var controller = CreateController(signInManagerMock);
        var model = new LoginViewModel
        {
            Email = "missing.user@example.com",
            Password = "not-the-right-password"
        };

        var result = await controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Invalid email or password.", GetModelOnlyError(controller));
    }

    [Fact]
    public async Task Login_ReturnsVerificationMessage_WhenSignInIsNotAllowed()
    {
        var signInManagerMock = CreateSignInManager();
        signInManagerMock
            .Setup(manager => manager.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                false))
            .ReturnsAsync(IdentitySignInResult.NotAllowed);

        var controller = CreateController(signInManagerMock);
        var model = new LoginViewModel
        {
            Email = "pending.user@example.com",
            Password = "Password123!"
        };

        var result = await controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(
            "Please verify your email before signing in. You can request a new verification link below.",
            GetModelOnlyError(controller));
        Assert.Equal(true, controller.ViewData["EmailVerificationRequired"]);
        Assert.Equal(model.Email, controller.ViewData["EmailVerificationAddress"]);
    }

    [Fact]
    public async Task Login_ReturnsNeutralUnavailableMessage_WhenDatabaseConnectionFails()
    {
        var signInManagerMock = CreateSignInManager();
        signInManagerMock
            .Setup(manager => manager.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                false))
            .ThrowsAsync(new NpgsqlException("Simulated database connectivity failure"));

        var controller = CreateController(signInManagerMock);
        var model = new LoginViewModel
        {
            Email = "student@example.com",
            Password = "Password123!"
        };

        var result = await controller.Login(model);

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Unable to sign in right now. Please try again shortly.", GetModelOnlyError(controller));
    }

    private static AccountController CreateController(Mock<SignInManager<User>> signInManagerMock)
    {
        var userManagerMock = CreateUserManager();

        return new AccountController(
            signInManagerMock.Object,
            userManagerMock.Object,
            Mock.Of<IAuthService>(),
            Mock.Of<ICoursesService>(),
            Mock.Of<IAcademicYearsService>(),
            NullLogger<AccountController>.Instance);
    }

    private static Mock<UserManager<User>> CreateUserManager()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<SignInManager<User>> CreateSignInManager()
    {
        var userManagerMock = CreateUserManager();
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        contextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(new DefaultHttpContext());

        return new Mock<SignInManager<User>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());
    }

    private static string GetModelOnlyError(Controller controller)
    {
        Assert.True(controller.ModelState.TryGetValue(string.Empty, out var modelState));
        var error = Assert.Single(modelState!.Errors);
        return error.ErrorMessage;
    }
}
