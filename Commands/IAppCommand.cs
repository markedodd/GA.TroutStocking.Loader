namespace GA_TroutStocking_Loader.Commands;

/// <summary>
/// Entry abstraction for the application workflow.
/// </summary>
internal interface IAppCommand
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
