using AutoMapper;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Dtos.Server;
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
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Settings, VoyagerJsonSettings.Default)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DiceRolls, opt => opt.Ignore());

        CreateMap<User, UserReadDto>()
            .ForCtorParam(nameof(UserReadDto.Settings),
                opt => opt.MapFrom(src => JsonConvert.DeserializeObject<UserSettings>(src.Settings, VoyagerJsonSettings.Default)));

        CreateMap<UserUpdateDto, User>()
            .ForMember(dest => dest.Settings,
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Settings, VoyagerJsonSettings.Default)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DiceRolls, opt => opt.Ignore());

        // Servers
        CreateMap<ServerCreateDto, Server>()
            .ForMember(dest => dest.Settings,
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Settings, VoyagerJsonSettings.Default)))
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        CreateMap<Server, ServerReadDto>()
            .ForCtorParam(nameof(ServerReadDto.Settings),
                opt => opt.MapFrom(src => JsonConvert.DeserializeObject<ServerSettings>(src.Settings, VoyagerJsonSettings.Default)));

        CreateMap<ServerUpdateDto, Server>()
            .ForMember(dest => dest.Settings,
                opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Settings, VoyagerJsonSettings.Default)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        // DiceRolls
        CreateMap<DiceRollCreateDto, DiceRoll>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        CreateMap<DiceRoll, DiceRollReadDto>();

        CreateMap<DiceRollUpdateDto, DiceRoll>()
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
    }
}
