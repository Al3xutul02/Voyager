namespace Repository.Enums.Behaviors;

/// <summary>
/// Enum type for different include behaviors in the querry builder
/// </summary>
public enum IncludeBehavior
{
    /// <summary>
    /// No inclusions should be made in the querry, only the data in the queried entity
    /// </summary>
    NoInclude,

    /// <summary>
    /// The querry attempts to include all tables it is directly
    /// linked to, this is more taxing on performance
    /// </summary>
    AllIncludes,

    /// <summary>
    /// Use the query given in the parameters
    /// </summary>
    GivenIncludes,
}