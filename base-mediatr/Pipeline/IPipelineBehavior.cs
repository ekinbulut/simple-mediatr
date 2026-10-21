namespace Smpl.Mediatr;

/// <summary>Represents the rest of the pipeline (the next behavior or the handler itself).</summary>
public delegate Task<TResult> RequestHandlerDelegate<TResult>(CancellationToken ct);

/// <summary>
/// Wraps the handling of a request. Behaviors are resolved from DI and are NOT scanned:
/// register them as open generics yourself. The first registered behavior is the outermost.
/// Commands without a result flow through the pipeline with <c>TResult = Unit</c>.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResult> where TRequest : notnull
{
    Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct);
}
