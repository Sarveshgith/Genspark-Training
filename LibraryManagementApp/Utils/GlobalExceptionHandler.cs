using System;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Presentation;

namespace LibraryManagementApp.Utils;

internal static class GlobalExceptionHandler
{
    public static void HandleException(Exception ex)
    {
        switch (ex)
        {
            case InvalidArgumentException:
                ConsolePrinter.WriteError($"Invalid Argument: {ex.Message}");
                break;
            case AuthenticationException:
                ConsolePrinter.WriteError($"Authentication Failed: {ex.Message}");
                break;
            case UnauthorizedException:
                ConsolePrinter.WriteError($"Unauthorized Access: {ex.Message}");
                break;
            case ConflictException:
                ConsolePrinter.WriteError($"Conflict: {ex.Message}");
                break;
            case InvalidInputException:
                ConsolePrinter.WriteError($"Invalid Input: {ex.Message}");
                break;
            case BusinessRuleException:
                ConsolePrinter.WriteError($"Business Rule Violation: {ex.Message}");
                break;
            case NotFoundException:
                ConsolePrinter.WriteError($"Not Found: {ex.Message}");
                break;
            default:
                ConsolePrinter.WriteError($"An unexpected error occurred: {ex.Message}");
                break;
        }
    }
}
