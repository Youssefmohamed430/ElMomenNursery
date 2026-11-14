using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IClassRoomService
{
    Task<IEnumerable<ClassRoomDto>> GetAllClassRoomsAsync();
    Task<ClassRoomDto> GetClassRoomByIdAsync(int id);
    Task<ClassRoomDto> CreateClassRoomAsync(CreateClassRoomDto classRoomDto);
    Task<ClassRoomDto> UpdateClassRoomAsync(int id, UpdateClassRoomDto classRoomDto);
    Task<bool> DeleteClassRoomAsync(int id);
}
