using Microsoft.Playwright;
using System.Text.Json;

namespace WeatherApiPlaywrightTest
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class WeatherApiTest : PlaywrightTest
    {
        // Example URL: https://api.weather.gov/points/39.7456,-97.0892

        private static readonly string _baseUrl = "https://api.weather.gov/points/";
        private IAPIRequestContext _request = null!;


        [OneTimeSetUp]
        public async Task LocalOneTimeSetUp() // Execute once before any tests run
        { }


        // Data source: one TestCaseData per CSV row of WeatherItem.
        public static IEnumerable<TestCaseData> WeatherItems()
        {
            // Read the input file content and feed one row at a time to our TestLatLongInput
            // test method.
            foreach (var item in GetApiTestItems())
            {
                yield return new TestCaseData(item)
                    .SetName($"Points_{item.Latitude}_{item.Longitude}_{item.City}_{item.State}");
            }
        }


        [SetUp]
        public async Task LocalSetup() // Execute before each test runs
        {
            _request = await Playwright.APIRequest.NewContextAsync(new()
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["User-Agent"] = "WeatherApiPlaywrightTest",
                    ["Accept"] = "application/geo+json"
                }
            });
        }


        // Iterate over each WeatherItem from the data source WeatherItems and run the test.
        [TestCaseSource(nameof(WeatherItems))]
        public async Task TestLatLongInput(WeatherApiItem weatherItem)
        {
            string url = $"{_baseUrl}{weatherItem.Latitude},{weatherItem.Longitude}";
            var response = await _request.GetAsync(url);

            // Validate that we got an OK response status code
            await Expect(response).ToBeOKAsync();

            // Get our JSON response body content.
            var json = await response.JsonAsync();
            Assert.That(json, Is.Not.Null);
            JsonElement root = json!.Value;

            // Validate the id, city, and state properties and values in the response.
            Assert.Multiple(() =>
            {
                // Validate that the id is equal to the URL we used for the request.
                Assert.That(root.TryGetProperty("id", out var id), Is.True, "missing property 'id'");
                string? idStr = id.GetString();
                Assert.That(idStr, Is.Not.Null.And.Not.Empty, "id value is null or empty");
                Assert.That(idStr, Is.EqualTo(url));

                // Navigate to the city and state properties.
                Assert.That(root.TryGetProperty("properties", out var props1), Is.True, "missing first 'properties'");
                Assert.That(props1.TryGetProperty("relativeLocation", out var loc), Is.True, "missing 'relativeLocation'");
                Assert.That(loc.TryGetProperty("properties", out var props2), Is.True, "missing second 'properties'");

                // Validate the city value
                string? city = props2.GetProperty("city").GetString();
                Assert.That(city, Is.Not.Null.And.Not.Empty, "city value is missing or empty in the response");
                Assert.That(city, Is.EqualTo(weatherItem.City), $"city {city} value is not equal to {weatherItem.City}");

                // Validate the state value
                string? state = props2.GetProperty("state").GetString();
                Assert.That(state, Is.Not.Null.And.Not.Empty, "state property is missing or empty in the response");
                Assert.That(state, Is.EqualTo(weatherItem.State), $"state {state} is not equal to {weatherItem.State}");
            });
        }


        [TearDown]
        public async Task LocalTearDown() // Execute after each test completes
        {
            await _request.DisposeAsync();
        }


        [OneTimeTearDown]
        public async Task LocalOneTimeTearDown() // Execute once after all tests have completed
        { }


        // Read our input data from a csv file into an array of WeatherApiItem.
        private static WeatherApiItem[] GetApiTestItems()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "weather_loc_input.csv");
            return File.ReadLines(path)
                .Skip(1)   // skip the header
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => new WeatherApiItem(l))
                .ToArray();
        }
    }
}
