using Microsoft.EntityFrameworkCore;
using Repository.Enums.Behaviors;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Builders;

/// <summary>
/// Builder class for streamilining database queries
/// </summary>
/// <typeparam name="T">The class model of the table that will be queried</typeparam>
public class QueryBuilder<T> where T : class
{
    private readonly DbSet<T> _dbSet;
    private readonly Dictionary<IncludeBehavior, Func<IQueryable<T>>> behaviorMap;

    private Func<IQueryable<T>, IQueryable<T>>? _includes;
    private IncludeBehavior _behavior = IncludeBehavior.NoInclude;

    /// <summary>
    /// Basic constructor for the <see cref="QueryBuilder{T}"/>
    /// </summary>
    /// <param name="dbSet">The table that the query will be made on</param>
    public QueryBuilder(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
        behaviorMap = new Dictionary<IncludeBehavior, Func<IQueryable<T>>>
            {
                { IncludeBehavior.NoInclude, BehaviorMapNoIncludes },
                { IncludeBehavior.AllIncludes, BehaviorMapAllIncludes },
                { IncludeBehavior.GivenIncludes, BehaviorMapGivenIncludes }
            };
    }

    /// <summary>
    /// For specifying the wanted includes for the query
    /// </summary>
    /// <param name="includes">Function containing the query includes</param>
    /// <returns>
    /// The same reference of the <see cref="QueryBuilder{T}"/> that can be used
    /// to further configure it
    /// </returns>
    public QueryBuilder<T> AddIncludes(Func<IQueryable<T>, IQueryable<T>>? includes)
    {
        _includes = includes;
        return this;
    }

    /// <summary>
    /// For specifying the wanted include behavior for the query
    /// </summary>
    /// <param name="behavior">
    /// Enum value for the wanted behavior:
    /// <list type="bullet">
    /// <item> <see cref="IncludeBehavior.NoInclude"/>
    /// <description>=> Make no joins with other tables linked with relations</description>
    /// </item>
    /// <item> <see cref="IncludeBehavior.AllIncludes"/>
    /// <description>=> Attempt to make joins with all tables linked with relations</description>
    /// </item>
    /// <item> <see cref="IncludeBehavior.GivenIncludes"/>
    /// <description>=> Use the joins that have been given</description>
    /// </item>
    /// </list>
    /// </param>
    /// <returns>
    /// The same reference of the <see cref="QueryBuilder{T}"/> that can be used
    /// to further configure it
    /// </returns>
    public QueryBuilder<T> AddBehavior(IncludeBehavior behavior)
    {
        _behavior = behavior;
        return this;
    }

    /// <summary>
    /// Builds the final <see cref="IQueryable{T}"/>
    /// </summary>
    /// <returns>
    /// The table as an <see cref="IQueryable{T}"/> with all specified
    /// query configurations
    /// </returns>
    public IQueryable<T> Build()
    {
        if (_includes == null && _behavior != IncludeBehavior.AllIncludes)
        {
            return BehaviorMapNoIncludes();
        }

        return behaviorMap[_behavior]();
    }

    private IQueryable<T> BehaviorMapNoIncludes() => _dbSet;

    private IQueryable<T> BehaviorMapAllIncludes()
    {
        IQueryable<T> query = _dbSet;
        var properties = typeof(T).GetProperties()
            .Where(p =>
                !p.IsDefined(typeof(NotMappedAttribute), true) &&
                ((p.PropertyType.IsClass && p.PropertyType != typeof(string)) ||
                (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string)))
            );

        foreach (var property in properties)
        {
            query = query.Include(property.Name);
        }
        return query;
    }

    private IQueryable<T> BehaviorMapGivenIncludes()
    {
        IQueryable<T> query = _dbSet;
        if (_includes != null)
        {
            query = _includes(query);
        }
        return query;
    }
}
