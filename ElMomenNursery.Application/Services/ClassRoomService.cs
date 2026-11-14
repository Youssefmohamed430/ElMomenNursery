using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;

namespace ElMomenNursery.Application.Services;

public class ClassRoomService : IClassRoomService
{
    public Task<ClassRoomDto> CreateClassRoomAsync(CreateClassRoomDto classRoomDto)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteClassRoomAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ClassRoomDto>> GetAllClassRoomsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ClassRoomDto> GetClassRoomByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<ClassRoomDto> UpdateClassRoomAsync(int id, UpdateClassRoomDto classRoomDto)
    {
        throw new NotImplementedException();
    }
}
