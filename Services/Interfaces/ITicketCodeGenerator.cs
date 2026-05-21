namespace tickets_management.Services.Interfaces;

public interface ITicketCodeGenerator
{
    /// <summary>
    /// Produces a cryptographically random, human-readable ticket code.
    /// Uniqueness is enforced at persistence time against the database's unique
    /// index; this method only guarantees high-entropy candidates.
    /// </summary>
    string Generate();
}
