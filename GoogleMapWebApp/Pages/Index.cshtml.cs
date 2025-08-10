using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.Google;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GoogleMapWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    public string ApiKey { get; private set; }
    public UserLocation? UserLocation { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        ApiKey = configuration["GoogleMaps:ApiKey"]!;
    }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User is authenticated. Fetching location data.");
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GoogleMapWebApp"
            });

            try
            {
                var dataSources = await service.Users.DataSources.List("me").ExecuteAsync();
                _logger.LogInformation("Found {count} data sources.", dataSources.DataSource.Count);

                var locationDataSource = dataSources.DataSource.FirstOrDefault(ds => ds.DataStreamName.Contains("location"));

                if (locationDataSource != null)
                {
                    _logger.LogInformation("Found location data source: {dataSourceName}", locationDataSource.DataStreamName);
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.AddDays(-7);
                    var startTimeNanos = (long)(startTime.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds * 1000000;
                    var endTimeNanos = (long)(endTime.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds * 1000000;
                    var datasetId = startTimeNanos + "-" + endTimeNanos;
                    _logger.LogInformation("Querying dataset with ID: {datasetId}", datasetId);

                    var response = await service.Users.DataSources.Datasets.Get("me", locationDataSource.DataStreamId, datasetId).ExecuteAsync();

                    if (response.Point != null && response.Point.Count > 0)
                    {
                        _logger.LogInformation("{count} location points found.", response.Point.Count);
                        var point = response.Point.Last();
                        if (point.Value != null && point.Value.Count >= 2 && point.Value[0].FpVal.HasValue && point.Value[1].FpVal.HasValue)
                        {
                            UserLocation = new UserLocation
                            {
                                Latitude = point.Value[0].FpVal.Value,
                                Longitude = point.Value[1].FpVal.Value
                            };
                            _logger.LogInformation("User location set to: Lat={lat}, Lng={lng}", UserLocation.Latitude, UserLocation.Longitude);
                        }
                        else
                        {
                            _logger.LogWarning("Last point has invalid data.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No location points found in the response for this data source.");
                    }
                }
                else
                {
                    _logger.LogWarning("No location data source found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching data from the Fitness API.");
            }
        }
    }

    public IActionResult OnPost()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage();
    }
}

public class UserLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}