using AutoMapper;
using BusinessLogic.Dtos.User;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Models;

namespace BusinessLogic.Mapper;

/// <summary>
/// Business logic service for AutoMapper configuration.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User.Settings is a JSON string in the DB. When writing, serialize the
        // UserSettings object to a string. When reading, deserialize it.
        // NOTE: UserReadDto is a positional record, so its properties are
        // init-only and populated through the constructor. AutoMapper's
        // .ForMember(...) is silently ignored for positional record ctor
        // parameters — use .ForCtorParam(...) instead.
        // We pass VoyagerJsonSettings.Default explicitly to every JsonConvert
        // call so the camelCase resolver only affects JSON we own — never
        // DSharpPlus's internal serialization.

        CreateMap<UserCreateDto, User>()
            .ForMember(dest => dest.Settings,
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(new UserSettings(), VoyagerJsonSettings.Default)));

        CreateMap<User, UserReadDto>()
            .ForCtorParam(nameof(UserReadDto.Settings),
                opt => opt.MapFrom(src => JsonConvert.DeserializeObject<UserSettings>(src.Settings, VoyagerJsonSettings.Default)));

        CreateMap<UserUpdateDto, User>()
            .ForMember(dest => dest.Settings,
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Settings, VoyagerJsonSettings.Default)));
    }
}
