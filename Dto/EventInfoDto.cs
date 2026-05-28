namespace tickets_management.Dto;

/// <summary>Event details read from the catalog, used to print real ticket info.</summary>
public record EventInfoDto(int Id, string Name, DateTime StartDate, string? VenueName);
