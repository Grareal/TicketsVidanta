using System.ComponentModel.DataAnnotations;

namespace TicketsVidanta.Shared.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool UseSqlPersistence { get; init; }

    [Required]
    public string ConnectionStringName { get; init; } = "TicketsVidanta";
}
