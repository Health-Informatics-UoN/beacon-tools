﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * Task Execution Service
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * OpenAPI spec version: 0.3.0
 *
 * Generated by: https://openapi-generator.tech
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace BeaconBridge.Models.Submission.Tes;

/// <summary>
/// Gets or Sets tesFileType
/// </summary>
[JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum TesFileType
{
  /// <summary>
  /// Enum FILEEnum for FILE
  /// </summary>
  [EnumMember(Value = "FILE")] FILEEnum = 1,

  /// <summary>
  /// Enum DIRECTORYEnum for DIRECTORY
  /// </summary>
  [EnumMember(Value = "DIRECTORY")] DIRECTORYEnum = 2
}
