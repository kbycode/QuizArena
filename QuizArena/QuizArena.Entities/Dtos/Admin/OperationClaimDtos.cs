using QuizArena.Core.Entities;

namespace QuizArena.Entities.Dtos.Admin;

/// <summary>Yetki tanımı.</summary>
public sealed record OperationClaimResponse(
    Guid Id,
    string Name,
    string? Description) : IDto;

/// <summary>Yetki oluşturma/güncelleme isteği.</summary>
public sealed record SaveOperationClaimRequest(
    string Name,
    string? Description) : IDto;

/// <summary>Kullanıcıya yetki atama isteği.</summary>
public sealed record AssignOperationClaimRequest(
    Guid UserId,
    Guid OperationClaimId) : IDto;
