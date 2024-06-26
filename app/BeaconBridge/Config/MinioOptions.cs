namespace BeaconBridge.Config;

public class MinioOptions
{
  public string Host { get; set; } = "localhost:9000";
  public string AccessKey { get; set; } = string.Empty;
  public string SecretKey { get; set; } = string.Empty;
  public bool Secure { get; set; } = true;
  public string Bucket { get; set; } = string.Empty;
}
