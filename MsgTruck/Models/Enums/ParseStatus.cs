
namespace MsgTruck
{
    /// <summary>
    /// Represents parse result for e.g. Message
    /// </summary>
    /// <remarks>
    /// <c>Success</c> means it is successfully parsed.
    /// <c>InvalidId</c> means the ID (or URL depends on context, potentially because of the user input) *format* is invalid.
    /// <c>InvalidData</c> means the ID format is correct, but failed to retrieve data. This can be cache or responding issue.
    /// </remarks>
    internal enum ParseStatus
    {
        Success,
        InvalidId,
        InvalidData
    }
}
