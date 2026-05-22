using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Frontend;

[Collection("Selenium frontend")]
public sealed class SeleniumSignInTests : IClassFixture<ViteFrontendFixture>, IDisposable
{
    private readonly ViteFrontendFixture _fixture;
    private readonly ChromeDriver _driver;
    private readonly WebDriverWait _wait;

    public SeleniumSignInTests(ViteFrontendFixture fixture)
    {
        _fixture = fixture;

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--window-size=1280,900");

        _driver = new ChromeDriver(options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void SignInPage_LoadsDefaultSignInForm()
    {
        GoToSignInPage();

        Assert.Equal("Sign in to your climate dashboard", _driver.FindElement(By.CssSelector("h1")).Text);
        Assert.NotNull(_driver.FindElement(By.CssSelector("input[type='email']")));
        Assert.NotNull(_driver.FindElement(By.CssSelector("input[type='password']")));
        Assert.Equal("Sign in", _driver.FindElement(By.CssSelector("button.submit-button")).Text);
    }

    [Fact]
    public void SignInPage_CanSwitchToCreateAccountMode()
    {
        GoToSignInPage();

        _wait.Until(driver => driver.FindElement(By.XPath("//button[normalize-space()='Create account']"))).Click();

        _wait.Until(driver => driver.FindElement(By.CssSelector("button.submit-button")).Text == "Create account");
        Assert.Contains("Create account", _driver.PageSource);
    }

    [Fact]
    public void SignInPage_CanOpenAndReturnFromForgotPasswordMode()
    {
        GoToSignInPage();

        _wait.Until(driver => driver.FindElement(By.XPath("//button[normalize-space()='Forgot password?']"))).Click();
        _wait.Until(driver => driver.FindElement(By.CssSelector(".form-title")).Displayed);

        Assert.Contains("Reset Your Password", _driver.PageSource);

        _driver.FindElement(By.CssSelector("button.back-button")).Click();
        _wait.Until(driver => driver.FindElement(By.CssSelector("button.submit-button")).Text == "Sign in");

        Assert.Contains("Sign in to your climate dashboard", _driver.PageSource);
    }

    private void GoToSignInPage()
    {
        _driver.Navigate().GoToUrl(_fixture.SignInUrl);
        _wait.Until(driver =>
        {
            try
            {
                return driver.FindElement(By.CssSelector("h1")).Text == "Sign in to your climate dashboard";
            }
            catch (WebDriverException)
            {
                return false;
            }
        });
    }

    public void Dispose()
    {
        _driver.Dispose();
    }
}

[CollectionDefinition("Selenium frontend", DisableParallelization = true)]
public sealed class SeleniumFrontendCollection
{
}

public sealed class ViteFrontendFixture : IAsyncLifetime
{
    private Process? _viteProcess;

    public string BaseUrl { get; private set; } = string.Empty;
    public string SignInUrl => $"{BaseUrl}/signin";

    public async Task InitializeAsync()
    {
        var frontendDirectory = FindFrontendDirectory();
        var port = GetAvailablePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        _viteProcess = new Process
        {
            StartInfo = CreateNpmStartInfo(frontendDirectory, port)
        };

        _viteProcess.Start();
        await WaitForFrontendAsync(BaseUrl);
    }

    public Task DisposeAsync()
    {
        if (_viteProcess is null)
            return Task.CompletedTask;

        try
        {
            if (!_viteProcess.HasExited)
                _viteProcess.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process may never have started if npm could not be launched.
        }
        finally
        {
            _viteProcess.Dispose();
            _viteProcess = null;
        }

        return Task.CompletedTask;
    }

    private static ProcessStartInfo CreateNpmStartInfo(string frontendDirectory, int port)
    {
        var arguments = $"npm run dev -- --host 127.0.0.1 --port {port} --strictPort";

        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {arguments}",
                WorkingDirectory = frontendDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = $"run dev -- --host 127.0.0.1 --port {port} --strictPort",
            WorkingDirectory = frontendDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private static string FindFrontendDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var frontendPath = Path.Combine(directory.FullName, "Klima-Kontrolloeren-Frontend");
            var packageJsonPath = Path.Combine(frontendPath, "package.json");

            if (File.Exists(packageJsonPath))
                return frontendPath;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Klima-Kontrolloeren-Frontend from the test output directory.");
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForFrontendAsync(string baseUrl)
    {
        using var client = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Vite may still be starting.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"The Vite frontend did not become available at {baseUrl}.");
    }
}
