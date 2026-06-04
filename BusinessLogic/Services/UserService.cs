using AutoMapper;
using BusinessLogic.Dtos.User;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;

namespace BusinessLogic.Services;

/// <summary>
/// Business logic service for handling user information.
/// </summary>
/// <param name="mapper">The mapper used for mapping operations.</param>
/// <param name="userRepository">The database context to use.</param>
public class UserService(IMapper mapper, IUserRepository userRepository)
    : BaseService<User, UserReadDto, UserCreateDto, UserUpdateDto>(mapper, userRepository), IUserService
{
    private IUserRepository UserRepository => (IUserRepository)_repository;

    public async Task<UserReadDto?> GetByName(string name)
    {
        User? user = await UserRepository.GetByNameAsync(name, IncludeBehavior.NoInclude);
        return _mapper.Map<UserReadDto>(user);
    }
}
