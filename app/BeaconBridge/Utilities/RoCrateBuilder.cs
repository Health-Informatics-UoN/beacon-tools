using BeaconBridge.Config;
using BeaconBridge.Constants;
using Flurl;
using ROCrates;
using ROCrates.Models;

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

  public RoCrateBuilder(WorkflowOptions workflowOptions, CratePublishingOptions publishingOptions,
    CrateAgentOptions crateAgentOptions, CrateProjectOptions crateProjectOptions,
    CrateOrganizationOptions crateOrganizationOptions,
    string archivePayloadDirectoryPath, AgreementPolicyOptions agreementPolicy)
  {
    _workflowOptions = workflowOptions;
    _publishingOptions = publishingOptions;
    _crateAgentOptions = crateAgentOptions;
    _crateProjectOptions = crateProjectOptions;
    _crateOrganizationOptions = crateOrganizationOptions;
    _agreementPolicy = agreementPolicy;

    _crate.Initialise(archivePayloadDirectoryPath);
    AddProject();
    AddOrganisation();
  }

  public RoCrateBuilder(WorkflowOptions workflowOptions, CratePublishingOptions publishingOptions,
    CrateAgentOptions crateAgentOptions, CrateProjectOptions crateProjectOptions,
    CrateOrganizationOptions crateOrganizationOptions, AgreementPolicyOptions agreementPolicy)
  {
    _workflowOptions = workflowOptions;
    _publishingOptions = publishingOptions;
    _crateAgentOptions = crateAgentOptions;
    _crateProjectOptions = crateProjectOptions;
    _crateOrganizationOptions = crateOrganizationOptions;
    _agreementPolicy = agreementPolicy;

    AddProject();
    AddOrganisation();
  }

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

  /// <summary>
  /// Adds Project Entity as configured
  /// </summary>
  /// <returns></returns>
  private void AddProject()
  {
    var projectEntity = new Entity(identifier: $"#project-{Guid.NewGuid()}");
    projectEntity.SetProperty("@type", _crateProjectOptions.Type);
    projectEntity.SetProperty("name", _crateProjectOptions.Name);
    projectEntity.SetProperty("identifier", _crateProjectOptions.Identifiers);
    projectEntity.SetProperty("funding", _crateProjectOptions.Funding);
    projectEntity.SetProperty("member", _crateProjectOptions.Member);
    _crate.Add(projectEntity);
  }

  /// <summary>
  /// Adds Organisation Entity as configured.
  /// </summary>
  /// <returns></returns>
  private void AddOrganisation()
  {
    var orgEntity = new Entity(identifier: _crateOrganizationOptions.Id);
    orgEntity.SetProperty("@type", _crateOrganizationOptions.Type);
    orgEntity.SetProperty("name", _crateOrganizationOptions.Name);
    _crate.Add(orgEntity);
  }

  public void AddCheckValueAssessAction(string status, DateTime startTime, Part validator)
  {
    var checkActionId = $"#check-{Guid.NewGuid()}";
    var checkAction = new ContextEntity(_crate, checkActionId);
    checkAction.SetProperty("startTime", startTime);
    checkAction.SetProperty("@type", "AssessAction");
    checkAction.SetProperty("additionalType", new Part() { Id = "https://w3id.org/shp#CheckValue" });
    var statusMsg = GetStatus(status);
    checkAction.SetProperty("name", $"BagIt checksum of Crate: {statusMsg}");
    checkAction.SetProperty("actionStatus", status);
    checkAction.SetProperty("object", new Part { Id = _crate.RootDataset.Id });

    var instrument = new Entity { Id = "https://www.iana.org/assignments/named-information#sha-512" };
    instrument.SetProperty("@type", "DefinedTerm");
    instrument.SetProperty("name", "sha-512 algorithm");
    checkAction.SetProperty("instrument", new Part() { Id = instrument.Id });
    checkAction.SetProperty("agent", validator);
    checkAction.SetProperty("endTime", DateTime.Now);
    _crate.RootDataset.AppendTo("mentions", checkAction);
    _crate.Add(checkAction, instrument);
  }

  public void AddValidateCheck(string status, Part validator)
  {
    var profile = _crate.RootDataset.GetProperty<Part>("conformsTo") ??
                  throw new NullReferenceException("No profile found in RootDataset");

    var validateId = $"#validate - {Guid.NewGuid()}";
    var validateAction = new ContextEntity(_crate, validateId);
    validateAction.SetProperty("startTime", DateTime.Now);
    validateAction.SetProperty("@type", "AssessAction");
    validateAction.SetProperty("additionalType", new Part() { Id = "https://w3id.org/shp#ValidationCheck" });

    validateAction.SetProperty("name", $"Validation against Five Safes RO-Crate profile: approved");
    validateAction.SetProperty("actionStatus", status);
    validateAction.SetProperty("object", new Part { Id = _crate.RootDataset.Id });
    validateAction.SetProperty("instrument", new Part() { Id = profile.Id });
    validateAction.SetProperty("agent", validator);
    validateAction.SetProperty("endTime", DateTime.Now);
    _crate.RootDataset.AppendTo("mentions", validateAction);

    _crate.Add(validateAction);
  }

  public void AddSignOff()
  {
    var signOffEntity = new ContextEntity(identifier: $"#signoff-{Guid.NewGuid()}");
    signOffEntity.SetProperty("@type", "AssessAction");
    signOffEntity.SetProperty("additionalType", new Part { Id = "https://w3id.org/shp#SignOff" });
    signOffEntity.SetProperty("name", "Sign-off of execution according to Agreement policy");
    signOffEntity.SetProperty("endTime", DateTime.Now);
    _crate.Entities.TryGetValue(_crateAgentOptions.Id, out var agent);
    signOffEntity.SetProperty("agent", new Part() { Id = agent!.Id });
    var projectId = _crate.Entities.Keys.First(x => x.StartsWith("#project-"));
    signOffEntity.SetProperty("object", new Part[]
    {
      new() { Id = _crate.RootDataset.Id },
      new() { Id = GetWorkflowUrl() },
      new() { Id = projectId },
    });
    signOffEntity.SetProperty("actionStatus", ActionStatus.CompletedActionStatus);
    var agreementPolicyEntity = new CreativeWork(identifier: _agreementPolicy.Id);
    signOffEntity.SetProperty("instrument", new Part { Id = _agreementPolicy.Id });
    // Manually set type due to bug in ROCrates.Net
    agreementPolicyEntity.SetProperty("@type", "CreativeWork");
    agreementPolicyEntity.SetProperty("name", _agreementPolicy.Name);

    _crate.RootDataset.AppendTo("mentions", signOffEntity);
    _crate.Add(signOffEntity, agreementPolicyEntity);
  }

  /// <summary>
  /// Construct the Workflow URL from WorkflowOptions.
  /// </summary>
  /// <returns>Workflow URL</returns>
  public string GetWorkflowUrl()
  {
    return Url.Combine(_workflowOptions.BaseUrl, _workflowOptions.Id.ToString())
      .SetQueryParam("version", _workflowOptions.Version.ToString());
  }

  private string GetStatus(string status)
  {
    return status switch
    {
      ActionStatus.CompletedActionStatus => "completed",
      ActionStatus.ActiveActionStatus => "active",
      ActionStatus.FailedActionStatus => "failed",
      ActionStatus.PotentialActionStatus => "potential",
      _ => ""
    };
  }
}