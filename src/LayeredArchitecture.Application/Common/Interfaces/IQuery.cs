using MediatR;

namespace LayeredArchitecture.Application.Common.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}