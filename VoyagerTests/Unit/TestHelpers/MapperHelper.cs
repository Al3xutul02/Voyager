using AutoMapper;
using BusinessLogic.Mapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace VoyagerTests.TestHelpers;

/// <summary>
/// Builds a real <see cref="IMapper"/> configured with the production
/// <see cref="MappingProfile"/>, so tests exercise the actual mapping rules
/// (including the JSON (de)serialization of <c>User.Settings</c>) rather than
/// a stubbed mapper.
/// </summary>
public static class MapperHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return config.CreateMapper();
    }
}
