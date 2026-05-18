using System;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Presentation;
using Serilog;

namespace LibraryManagementApp.Utils;

internal static class GlobalExceptionHandler
{
    public static void HandleException(Exception ex)
    {
        switch (ex)
        {
            case InvalidArgumentException:
                Log.Warning(ex, "Invalid argument handled");
                ConsolePrinter.WriteError($"Invalid Argument: {ex.Message}");
                break;
            case AuthenticationException:
                Log.Warning(ex, "Authentication failure handled");
                ConsolePrinter.WriteError($"Authentication Failed: {ex.Message}");
                break;
            case UnauthorizedException:
                Log.Warning(ex, "Unauthorized access handled");
                ConsolePrinter.WriteError($"Unauthorized Access: {ex.Message}");
                break;
            case ConflictException:
                Log.Warning(ex, "Conflict handled");
                ConsolePrinter.WriteError($"Conflict: {ex.Message}");
                break;
            case InvalidInputException:
                Log.Warning(ex, "Invalid input handled");
                ConsolePrinter.WriteError($"Invalid Input: {ex.Message}");
                break;
            case BusinessRuleException:
                Log.Warning(ex, "Business rule violation handled");
                ConsolePrinter.WriteError($"Business Rule Violation: {ex.Message}");
                break;
            case NotFoundException:
                Log.Warning(ex, "Not found handled");
                ConsolePrinter.WriteError($"Not Found: {ex.Message}");
                break;
            default:
                Log.Error(ex, "Unexpected exception handled");
                ConsolePrinter.WriteError($"An unexpected error occurred: {ex.Message}");
                break;
        }
    }
}
