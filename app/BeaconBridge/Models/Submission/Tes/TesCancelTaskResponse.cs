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
using System.Text;
using BeaconBridge.Utilities;
using Newtonsoft.Json;

namespace BeaconBridge.Models.Submission.Tes;

/// <summary>
/// CancelTaskResponse describes a response from the CancelTask endpoint.
/// </summary>
[DataContract]
public partial class TesCancelTaskResponse : IEquatable<TesCancelTaskResponse>
{
  public TesCancelTaskResponse()
    => NewtonsoftJsonSafeInit.SetDefaultSettings();

  /// <summary>
  /// Returns true if TesCancelTaskResponse instances are equal
  /// </summary>
  /// <param name="other">Instance of TesCancelTaskResponse to be compared</param>
  /// <returns>Boolean</returns>
  public bool Equals(TesCancelTaskResponse other)
    => other switch
    {
      var x when x is null => false,
      var x when ReferenceEquals(this, x) => true,
    };


  /// <summary>
  /// Returns the string presentation of the object
  /// </summary>
  /// <returns>String presentation of the object</returns>
  public override string ToString()
    => new StringBuilder()
      .Append("class TesCancelTaskResponse {\n")
      .Append("}\n")
      .ToString();

  /// <summary>
  /// Returns the JSON string presentation of the object
  /// </summary>
  /// <returns>JSON string presentation of the object</returns>
  public string ToJson()
    => JsonConvert.SerializeObject(this, Formatting.Indented);

  /// <summary>
  /// Returns true if objects are equal
  /// </summary>
  /// <param name="obj">Object to be compared</param>
  /// <returns>Boolean</returns>
  public override bool Equals(object obj)
    => obj switch
    {
      var x when x is null => false,
      var x when ReferenceEquals(this, x) => true,
      _ => obj.GetType() == GetType() && Equals((TesCancelTaskResponse)obj),
    };

  /// <summary>
  /// Gets the hash code
  /// </summary>
  /// <returns>Hash code</returns>
  public override int GetHashCode()
  {
    unchecked // Overflow is fine, just wrap
    {
      var hashCode = 41;
      // Suitable nullity checks etc, of course :)


      return hashCode;
    }
  }

  #region Operators

#pragma warning disable 1591

  public static bool operator ==(TesCancelTaskResponse left, TesCancelTaskResponse right)
    => Equals(left, right);

  public static bool operator !=(TesCancelTaskResponse left, TesCancelTaskResponse right)
    => !Equals(left, right);

#pragma warning restore 1591

  #endregion Operators
}
