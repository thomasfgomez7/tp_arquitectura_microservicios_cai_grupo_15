namespace Users.API.ExceptionHandlers;

public class ErrorHandlingOptions
{
    public const string SectionName = "ErrorHandling";

    public bool IncludeExceptionDetails { get; set; }
}