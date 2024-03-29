using BeaconBridge.Constants;

namespace BeaconBridge.Models;

public class RequestSummary
{
  public string ApiVersion { get; set; } = string.Empty;
  public string Schemas { get; set; } = string.Empty;
  public List<string> Filters { get; set; } = new();
  public string Parameters { get; set; } = string.Empty;
  public string IncludeResultSetResponses { get; set; } = ResultsetResponses.Hit;
  public string Pagination { get; set; } = string.Empty;
  public string Granularity { get; set; } = Constants.Granularity.Boolean;
  public bool TestMode { get; set; } = false;

}
