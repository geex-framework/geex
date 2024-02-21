using cqrs.models.Dto;
using MediatR;

namespace cqrs.models.Commands{
  public class MediatRRequest3: IRequest<IEnumerable<ResponseDTO>>{
    
  }
}