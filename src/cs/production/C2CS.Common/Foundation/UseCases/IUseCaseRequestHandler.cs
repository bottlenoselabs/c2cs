// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace C2CS;

public interface IUseCaseRequestHandler<in TRequest, TResponse>
    where TRequest : IUseCaseRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest> : IUseCaseRequestHandler<TRequest, UseCaseResponseUnit>
    where TRequest : IUseCaseRequest<UseCaseResponseUnit>
{
}
