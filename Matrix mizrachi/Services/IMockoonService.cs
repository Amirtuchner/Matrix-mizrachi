namespace Matrix_mizrachi.Services;

/// <summary>
/// Retrieves operation metadata from the Mockoon stub server.
/// </summary>
public interface IMockoonService
{
    /// <summary>
    /// Calls GET /api/meta/{operation} on Mockoon and returns the description.
    /// </summary>
    Task<string> GetOperationDescriptionAsync(string operation);
}
