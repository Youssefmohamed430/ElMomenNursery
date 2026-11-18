using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.IServices;

namespace ElMomenNursery.Application.Services;

public class FinanceService(IUnitOfWork unitOfWork) : IFinanceService
{
}
