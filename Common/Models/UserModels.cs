using System;
using System.Collections.Generic;

namespace BlitzConnect.Common.Models;

/// <summary>Response from the logout (session revoke) endpoint.</summary>
public class LogoutResponse
{
    public string? Message { get; init; }
    public int RevokedSessions { get; init; }
}

/// <summary>A single holding (portfolio/demat position).</summary>
public class Holding
{
    public string? UserID { get; init; }
    public string? ISIN { get; init; }
    public string? InstrumentName { get; init; }
    public string? Ticker { get; init; }
    public string? InstrumentId { get; init; }
    public string? ExchangeSegment { get; init; }
    public double Quantity { get; init; }
    public double BuyAvgPrice { get; init; }
}

/// <summary>Wraps a holdings response. The server returns a bare JSON array.</summary>
public class HoldingsResponse
{
    public List<Holding> Data { get; init; } = [];
    public int Count => Data.Count;
}

/// <summary>User profile returned by the profile endpoint.</summary>
public class Profile
{
    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public int PinCode { get; init; }
    public string? Country { get; init; }
    public DateTime DateOfBirth { get; init; }
    public bool IsTemporaryPassword { get; init; }
    public string? UserType { get; init; }
    public int ClientType { get; init; }
    public string? BranchId { get; init; }
    public string? PanNumber { get; init; }
    public bool IsKYC { get; init; }
    public string? ClientId { get; init; }
    public bool IsActivated { get; init; }
    public bool IsSuspended { get; init; }
    public bool IsDeleted { get; init; }
    public string? UserName { get; init; }
    public string? NormalizedUserName { get; init; }
    public string? Email { get; init; }
    public string? NormalizedEmail { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public bool LockoutEnabled { get; init; }
    public int AccessFailedCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public int RiskLevel { get; init; }
    public bool Is2FAEmail { get; init; }
    public bool Is2FAPhone { get; init; }
    public DateTime LastLogin { get; init; }
    public DateTime SubscriptionExpiry { get; init; }
    public bool IsPro { get; init; }
    public string? CountryCode { get; init; }
    public bool AutoSquareOfEnabled { get; init; }
    public bool TPOmsRoute { get; init; }
    public bool IsTOTPEnabled { get; init; }
}