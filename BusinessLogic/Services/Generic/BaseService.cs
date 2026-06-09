using AutoMapper;
using Repository.Enums.Behaviors;
using Repository.Repositories.Generic;

namespace BusinessLogic.Services.Generic;

/// <summary>
/// The implementation of the <see cref="IBaseService{T}"/> interface
/// </summary>
/// <typeparam name="T">The class model for the repository</typeparam>
/// <typeparam name="TReadDto">The read DTO of the entity</typeparam>
/// <typeparam name="TCreateDto">The create DTO of the entity</typeparam>
/// <typeparam name="TUpdateDto">The update DTO of the entity</typeparam>
/// <param name="mapper">The mapper for the DTOs and models</param>
/// <param name="repository">The main repository the service communicates with</param>
public abstract class BaseService<T, TReadDto, TCreateDto, TUpdateDto>(
    IMapper mapper,
    IBaseRepository<T> repository) : IBaseService<T, TReadDto, TCreateDto, TUpdateDto>
    where T : class
    where TReadDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly IMapper _mapper = mapper;
    protected readonly IBaseRepository<T> _repository = repository;

    /// <inheritdoc />
    public virtual async Task<TReadDto?> GetByIdAsync(ulong id)
    {
        var entity = await _repository.GetByIdAsync(id, IncludeBehavior.NoInclude);
        return entity != null ? _mapper.Map<TReadDto>(entity)
            : throw new ArgumentException($"{typeof(T).Name} with id {id} not found");
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TReadDto>> GetAllAsync()
    {
        IEnumerable<T> entities = await _repository.GetAllAsync(IncludeBehavior.NoInclude);
        return _mapper.Map<IEnumerable<TReadDto>>(entities);
    }

    /// <inheritdoc />
    public virtual async Task CreateAsync(TCreateDto entityCreateDto)
    {
        var entity = _mapper.Map<T>(entityCreateDto);
        await _repository.AddAsync(entity);
        await _repository.SaveAsync();
    }

    /// <inheritdoc />
    public virtual async Task UpdateAsync(TUpdateDto dto)
    {

        var entity = _mapper.Map<T>(dto);
        _repository.Update(entity);
        await _repository.SaveAsync();
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(ulong id)
    {
        var entity = await _repository.GetByIdAsync(id, IncludeBehavior.NoInclude)
            ?? throw new Exception("User not found");
        _repository.Delete(entity);
        await _repository.SaveAsync();
    }
}