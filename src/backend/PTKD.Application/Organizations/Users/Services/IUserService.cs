using System.Collections.Generic;
using System.Threading.Tasks;
using PTKD.Application.Organizations.Users.DTOs;

namespace PTKD.Application.Organizations.Users.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(long id, UpdateUserRequest request, long actingUserId);
    Task<UserDto?> GetUserByIdAsync(long id);
    Task<IEnumerable<UserDto>> GetUsersAsync();
}
