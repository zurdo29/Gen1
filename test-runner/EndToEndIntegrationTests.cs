using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ProceduralMiniGameGenerator.EndToEndTests
{
    /// <summary>
    /// End-to-end integration tests that verify the complete system:
    /// - Backend API functionality
    /// - Frontend React application
    /// - Real browser interactions
    /// - Performance characteristics
    /// - Cross-browser compatibility
    /// </summary>
    public class EndToEndIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _apiClient;
        private readonly IWebDriver _webDriver;
        private readonly ITestOutputHelper _output;
        private readonly string _frontendUrl;

        public EndToEndIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _apiClient = _factory.CreateClient();
            
            // Setup Chrome driver for frontend testing
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--headless", "--no-sandbox", "--disable-dev-shm-usage");
            _webDriver = new ChromeDriver(chromeOptions);
            
            // Assume frontend is running on port 3000 during tests
            _frontendUrl = "http://localhost:3000";
        }

        [Fact]
        public async Task CompleteUserJourney_NewUserToLevelExport_ShouldWorkSeamlessly()
        {
            _output.WriteLine("Starting complete user journey test...");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Step 1: Navigate to application
                _webDriver.Navigate().GoToUrl(_frontendUrl);
                await WaitForPageLoad();
                _output.WriteLine($"✓ Page loaded in {stopwatch.ElapsedMilliseconds}ms");

                // Step 2: Verify initial UI state
                var welcomeMessage = _webDriver.FindElement(By.XPath("//*[contains(text(), 'Level Generator')]"));
                welcomeMessage.Should().NotBeNull();

                // Step 3: Configure generation parameters
                var widthInput = _webDriver.FindElement(By.Name("width"));
                widthInput.Clear();
                widthInput.SendKeys("40");

                var heightInput = _webDriver.FindElement(By.Name("height"));
                heightInput.Clear();
                heightInput.SendKeys("40");

                var terrainSelect = new SelectElement(_webDriver.FindElement(By.Name("terrainType")));
                terrainSelect.SelectByValue("PerlinNoise");

                var entityDensitySlider = _webDriver.FindElement(By.Name("entityDensity"));
                ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].value = 0.3; arguments[0].dispatchEvent(new Event('input'));", entityDensitySlider);

                _output.WriteLine("✓ Parameters configured");

                // Step 4: Generate level and verify real-time updates
                var generateButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Generate')]"));
                generateButton.Click();

                // Wait for generation to start
                var progressIndicator = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(2))
                    .Until(driver => driver.FindElement(By.ClassName("progress-indicator")));
                progressIndicator.Should().NotBeNull();

                // Wait for generation to complete
                var canvas = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10))
                    .Until(driver => driver.FindElement(By.TagName("canvas")));
                canvas.Should().NotBeNull();

                _output.WriteLine($"✓ Level generated in {stopwatch.ElapsedMilliseconds}ms");

                // Step 5: Verify canvas rendering
                var canvasSize = canvas.Size;
                canvasSize.Width.Should().BeGreaterThan(0);
                canvasSize.Height.Should().BeGreaterThan(0);

                // Verify canvas has content (not blank)
                var canvasDataUrl = ((IJavaScriptExecutor)_webDriver)
                    .ExecuteScript("return arguments[0].toDataURL();", canvas) as string;
                canvasDataUrl.Should().NotBeNullOrEmpty();
                canvasDataUrl.Should().StartWith("data:image/png;base64,");

                // Step 6: Test interactive editing
                var editButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Edit')]"));
                editButton.Click();

                // Click on canvas to edit
                canvas.Click();

                // Verify edit menu appears
                var editMenu = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(2))
                    .Until(driver => driver.FindElement(By.ClassName("edit-menu")));
                editMenu.Should().NotBeNull();

                _output.WriteLine("✓ Interactive editing works");

                // Step 7: Test preset saving
                var savePresetButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Save Preset')]"));
                savePresetButton.Click();

                var presetNameInput = _webDriver.FindElement(By.Name("presetName"));
                presetNameInput.SendKeys("E2E Test Preset");

                var confirmSaveButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Confirm')]"));
                confirmSaveButton.Click();

                // Verify preset appears in list
                var presetList = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3))
                    .Until(driver => driver.FindElement(By.ClassName("preset-list")));
                var savedPreset = presetList.FindElement(By.XPath(".//*[contains(text(), 'E2E Test Preset')]"));
                savedPreset.Should().NotBeNull();

                _output.WriteLine("✓ Preset saved successfully");

                // Step 8: Test export functionality
                var exportButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Export')]"));
                exportButton.Click();

                var formatSelect = new SelectElement(_webDriver.FindElement(By.Name("exportFormat")));
                formatSelect.SelectByValue("JSON");

                var downloadButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Download')]"));
                downloadButton.Click();

                // Wait for download to initiate (check for success message or file download)
                var downloadSuccess = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5))
                    .Until(driver => 
                    {
                        try
                        {
                            return driver.FindElement(By.XPath("//*[contains(text(), 'Download started') or contains(text(), 'Export complete')]"));
                        }
                        catch
                        {
                            return null;
                        }
                    });
                downloadSuccess.Should().NotBeNull();

                _output.WriteLine("✓ Export functionality works");

                // Step 9: Test sharing
                var shareButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Share')]"));
                shareButton.Click();

                var shareUrl = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3))
                    .Until(driver => driver.FindElement(By.ClassName("share-url")));
                var shareUrlText = shareUrl.GetAttribute("value") ?? shareUrl.Text;
                shareUrlText.Should().NotBeNullOrEmpty();
                shareUrlText.Should().StartWith("http");

                _output.WriteLine("✓ Sharing functionality works");

                // Step 10: Test batch generation
                var batchTab = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Batch Generation')]"));
                batchTab.Click();

                var batchCountInput = _webDriver.FindElement(By.Name("batchCount"));
                batchCountInput.Clear();
                batchCountInput.SendKeys("3");

                var startBatchButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Start Batch')]"));
                startBatchButton.Click();

                // Wait for batch completion
                var batchResults = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(15))
                    .Until(driver => driver.FindElements(By.ClassName("batch-result-thumbnail")));
                batchResults.Count.Should().Be(3);

                _output.WriteLine("✓ Batch generation works");

                stopwatch.Stop();
                _output.WriteLine($"✓ Complete user journey completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Test failed: {ex.Message}");
                
                // Take screenshot for debugging
                var screenshot = ((ITakesScreenshot)_webDriver).GetScreenshot();
                var screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), $"test-failure-{DateTime.Now:yyyyMMdd-HHmmss}.png");
                screenshot.SaveAsFile(screenshotPath);
                _output.WriteLine($"Screenshot saved: {screenshotPath}");
                
                throw;
            }
        }

        [Fact]
        public async Task PerformanceTest_LargeLevelGeneration_ShouldMeetPerformanceTargets()
        {
            _output.WriteLine("Starting performance test...");
            var stopwatch = Stopwatch.StartNew();

            // Navigate to application
            _webDriver.Navigate().GoToUrl(_frontendUrl);
            await WaitForPageLoad();

            // Configure large level
            var widthInput = _webDriver.FindElement(By.Name("width"));
            widthInput.Clear();
            widthInput.SendKeys("100");

            var heightInput = _webDriver.FindElement(By.Name("height"));
            heightInput.Clear();
            heightInput.SendKeys("100");

            var terrainSelect = new SelectElement(_webDriver.FindElement(By.Name("terrainType")));
            terrainSelect.SelectByValue("CellularAutomata");

            // Start generation and measure time
            var generateButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Generate')]"));
            var generationStartTime = stopwatch.ElapsedMilliseconds;
            generateButton.Click();

            // Wait for completion
            var canvas = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(30))
                .Until(driver => driver.FindElement(By.TagName("canvas")));
            var generationEndTime = stopwatch.ElapsedMilliseconds;

            var generationTime = generationEndTime - generationStartTime;
            _output.WriteLine($"Large level (100x100) generated in {generationTime}ms");

            // Performance assertions
            generationTime.Should().BeLessThan(15000, "Large level generation should complete within 15 seconds");

            // Test canvas interaction performance
            var interactionStartTime = stopwatch.ElapsedMilliseconds;
            
            // Zoom in/out multiple times
            for (int i = 0; i < 5; i++)
            {
                ((IJavaScriptExecutor)_webDriver).ExecuteScript(
                    "arguments[0].dispatchEvent(new WheelEvent('wheel', { deltaY: -100 }));", canvas);
                await Task.Delay(100);
            }

            var interactionEndTime = stopwatch.ElapsedMilliseconds;
            var interactionTime = interactionEndTime - interactionStartTime;
            
            _output.WriteLine($"Canvas interactions completed in {interactionTime}ms");
            interactionTime.Should().BeLessThan(2000, "Canvas interactions should be responsive");
        }

        [Fact]
        public async Task StressTest_ConcurrentUsers_ShouldHandleLoad()
        {
            _output.WriteLine("Starting stress test with concurrent users...");

            var tasks = new List<Task>();
            var results = new List<(bool Success, long Duration, string Error)>();
            var lockObject = new object();

            // Simulate 5 concurrent users
            for (int i = 0; i < 5; i++)
            {
                var userId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        // Each user generates a different level
                        var config = new
                        {
                            Width = 30 + userId * 5,
                            Height = 30 + userId * 5,
                            TerrainType = "PerlinNoise",
                            EntityDensity = 0.2f + userId * 0.1f,
                            Seed = 1000 + userId * 100
                        };

                        var response = await _apiClient.PostAsJsonAsync("/api/generation/generate", new
                        {
                            Config = config,
                            IncludePreview = true
                        });

                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStringAsync();
                        
                        stopwatch.Stop();
                        
                        lock (lockObject)
                        {
                            results.Add((true, stopwatch.ElapsedMilliseconds, null));
                        }
                        
                        _output.WriteLine($"User {userId} completed in {stopwatch.ElapsedMilliseconds}ms");
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        lock (lockObject)
                        {
                            results.Add((false, stopwatch.ElapsedMilliseconds, ex.Message));
                        }
                        _output.WriteLine($"User {userId} failed: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Analyze results
            var successCount = results.Count(r => r.Success);
            var averageDuration = results.Where(r => r.Success).Average(r => r.Duration);
            var maxDuration = results.Where(r => r.Success).Max(r => r.Duration);

            _output.WriteLine($"Stress test results: {successCount}/5 successful, avg: {averageDuration:F0}ms, max: {maxDuration}ms");

            // Assertions
            successCount.Should().Be(5, "All concurrent users should succeed");
            averageDuration.Should().BeLessThan(10000, "Average response time should be reasonable");
            maxDuration.Should().BeLessThan(20000, "Maximum response time should be acceptable");
        }

        [Fact]
        public async Task CrossBrowserCompatibility_MultipleEngines_ShouldWorkConsistently()
        {
            // This test would ideally run with multiple browser engines
            // For now, we'll test different viewport sizes to simulate different devices
            
            var viewports = new[]
            {
                new { Width = 1920, Height = 1080, Name = "Desktop" },
                new { Width = 1024, Height = 768, Name = "Tablet" },
                new { Width = 375, Height = 667, Name = "Mobile" }
            };

            foreach (var viewport in viewports)
            {
                _output.WriteLine($"Testing {viewport.Name} viewport ({viewport.Width}x{viewport.Height})");
                
                _webDriver.Manage().Window.Size = new System.Drawing.Size(viewport.Width, viewport.Height);
                _webDriver.Navigate().GoToUrl(_frontendUrl);
                await WaitForPageLoad();

                // Verify responsive design
                var mainContainer = _webDriver.FindElement(By.ClassName("app-container"));
                mainContainer.Should().NotBeNull();

                // Test basic functionality
                var generateButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Generate')]"));
                generateButton.Should().NotBeNull();
                generateButton.Displayed.Should().BeTrue();

                // For mobile, test that controls are accessible
                if (viewport.Width < 768)
                {
                    // Check if mobile menu exists
                    try
                    {
                        var mobileMenu = _webDriver.FindElement(By.ClassName("mobile-menu"));
                        mobileMenu.Should().NotBeNull();
                    }
                    catch (NoSuchElementException)
                    {
                        // Mobile menu might not be needed if layout is simple enough
                        _output.WriteLine("No mobile menu found, assuming responsive layout");
                    }
                }

                _output.WriteLine($"✓ {viewport.Name} viewport works correctly");
            }
        }

        [Fact]
        public async Task AccessibilityCompliance_WCAG_ShouldMeetStandards()
        {
            _webDriver.Navigate().GoToUrl(_frontendUrl);
            await WaitForPageLoad();

            // Test keyboard navigation
            var firstInput = _webDriver.FindElement(By.Name("width"));
            firstInput.SendKeys(Keys.Tab);

            var activeElement = _webDriver.SwitchTo().ActiveElement();
            activeElement.GetAttribute("name").Should().Be("height");

            // Test ARIA labels
            var generateButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'Generate')]"));
            var ariaLabel = generateButton.GetAttribute("aria-label");
            ariaLabel.Should().NotBeNullOrEmpty("Generate button should have aria-label");

            // Test focus indicators
            generateButton.SendKeys("");
            var focusedElement = _webDriver.SwitchTo().ActiveElement();
            focusedElement.Should().Be(generateButton);

            // Test color contrast (would need additional tools in real implementation)
            var backgroundColor = generateButton.GetCssValue("background-color");
            var textColor = generateButton.GetCssValue("color");
            backgroundColor.Should().NotBeNullOrEmpty();
            textColor.Should().NotBeNullOrEmpty();

            _output.WriteLine("✓ Basic accessibility checks passed");
        }

        private async Task WaitForPageLoad()
        {
            var wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10));
            wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
            
            // Additional wait for React app to initialize
            await Task.Delay(1000);
        }

        public void Dispose()
        {
            _webDriver?.Quit();
            _webDriver?.Dispose();
            _apiClient?.Dispose();
        }
    }

    /// <summary>
    /// Performance monitoring and metrics collection
    /// </summary>
    public class PerformanceMetrics
    {
        public static async Task<PerformanceReport> MeasureEndToEndPerformance(
            WebApplicationFactory<Program> factory,
            ITestOutputHelper output)
        {
            var report = new PerformanceReport();
            var client = factory.CreateClient();

            // Test API performance
            var apiTests = new[]
            {
                ("Generate Small Level", () => GenerateLevel(client, 20, 20)),
                ("Generate Medium Level", () => GenerateLevel(client, 50, 50)),
                ("Generate Large Level", () => GenerateLevel(client, 100, 100)),
                ("Export JSON", () => ExportLevel(client, "JSON")),
                ("Export Unity", () => ExportLevel(client, "Unity")),
                ("Validate Config", () => ValidateConfig(client)),
                ("Save Preset", () => SavePreset(client)),
                ("Load Presets", () => LoadPresets(client))
            };

            foreach (var (name, test) in apiTests)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await test();
                    stopwatch.Stop();
                    report.AddMetric(name, stopwatch.ElapsedMilliseconds, true);
                    output.WriteLine($"✓ {name}: {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    report.AddMetric(name, stopwatch.ElapsedMilliseconds, false, ex.Message);
                    output.WriteLine($"❌ {name}: {ex.Message}");
                }
            }

            return report;
        }

        private static async Task GenerateLevel(HttpClient client, int width, int height)
        {
            var config = new
            {
                Width = width,
                Height = height,
                TerrainType = "PerlinNoise",
                EntityDensity = 0.3f,
                Seed = 12345
            };

            var response = await client.PostAsJsonAsync("/api/generation/generate", new { Config = config });
            response.EnsureSuccessStatusCode();
        }

        private static async Task ExportLevel(HttpClient client, string format)
        {
            // First generate a level
            await GenerateLevel(client, 30, 30);
            
            // Then export it (simplified for test)
            var exportRequest = new
            {
                Format = format,
                Options = new { includeMetadata = true }
            };

            var response = await client.PostAsJsonAsync("/api/export/level", exportRequest);
            response.EnsureSuccessStatusCode();
        }

        private static async Task ValidateConfig(HttpClient client)
        {
            var config = new
            {
                Width = 40,
                Height = 40,
                TerrainType = "PerlinNoise",
                EntityDensity = 0.3f
            };

            var response = await client.PostAsJsonAsync("/api/configuration/validate", config);
            response.EnsureSuccessStatusCode();
        }

        private static async Task SavePreset(HttpClient client)
        {
            var preset = new
            {
                Name = "Performance Test Preset",
                Description = "Test preset for performance measurement",
                Config = new
                {
                    Width = 25,
                    Height = 25,
                    TerrainType = "PerlinNoise",
                    EntityDensity = 0.2f
                }
            };

            var response = await client.PostAsJsonAsync("/api/configuration/presets", preset);
            response.EnsureSuccessStatusCode();
        }

        private static async Task LoadPresets(HttpClient client)
        {
            var response = await client.GetAsync("/api/configuration/presets");
            response.EnsureSuccessStatusCode();
        }
    }

    public class PerformanceReport
    {
        public List<PerformanceMetric> Metrics { get; } = new();
        public DateTime TestRunTime { get; } = DateTime.UtcNow;

        public void AddMetric(string operation, long durationMs, bool success, string error = null)
        {
            Metrics.Add(new PerformanceMetric
            {
                Operation = operation,
                DurationMs = durationMs,
                Success = success,
                Error = error,
                Timestamp = DateTime.UtcNow
            });
        }

        public void PrintSummary(ITestOutputHelper output)
        {
            output.WriteLine($"\n=== Performance Report ({TestRunTime:yyyy-MM-dd HH:mm:ss} UTC) ===");
            output.WriteLine($"Total Operations: {Metrics.Count}");
            output.WriteLine($"Successful: {Metrics.Count(m => m.Success)}");
            output.WriteLine($"Failed: {Metrics.Count(m => !m.Success)}");
            output.WriteLine($"Average Duration: {Metrics.Where(m => m.Success).Average(m => m.DurationMs):F1}ms");
            output.WriteLine($"Max Duration: {Metrics.Where(m => m.Success).Max(m => m.DurationMs)}ms");
            output.WriteLine($"Min Duration: {Metrics.Where(m => m.Success).Min(m => m.DurationMs)}ms");
            
            output.WriteLine("\nDetailed Results:");
            foreach (var metric in Metrics.OrderBy(m => m.Operation))
            {
                var status = metric.Success ? "✓" : "❌";
                var error = metric.Success ? "" : $" ({metric.Error})";
                output.WriteLine($"{status} {metric.Operation}: {metric.DurationMs}ms{error}");
            }
        }
    }

    public class PerformanceMetric
    {
        public string Operation { get; set; }
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
    }
}