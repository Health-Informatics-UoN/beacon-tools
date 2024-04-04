using BeaconBridge.Config;
using ROCrates;

namespace BeaconBridge.Utilities;

public class RoCrateBuilder
{
  private readonly AgreementPolicyOptions _agreementPolicy;
  private readonly CrateAgentOptions _crateAgentOptions;
  private readonly CrateOrganizationOptions _crateOrganizationOptions;
  private readonly CrateProjectOptions _crateProjectOptions;
  private readonly CratePublishingOptions _publishingOptions;
  private readonly WorkflowOptions _workflowOptions;
  private ROCrate _crate = new();

  /// <summary>
  /// Returns the <c>ROCrate</c> that has been built.
  /// </summary>
  /// <returns>The <c>ROCrate</c> that has been built.</returns>
  public ROCrate GetROCrate()
  {
    var result = _crate;
    ResetCrate();
    return result;
  }

  /// <summary>
  /// Resets the <c>ROCrate</c> object in the builder.
  /// </summary>
  private void ResetCrate()
  {
    _crate = new ROCrate();
  }
}
