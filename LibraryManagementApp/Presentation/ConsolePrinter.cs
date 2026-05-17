using System;

namespace LibraryManagementApp.Presentation;

internal static class ConsolePrinter
{
    public static void PrintHeader(string title)
	{
		WriteColored($"========= {title} =========", ConsoleColor.Cyan);
	}

	public static void PrintSection(string title)
	{
		WriteColored($"--- {title} ---", ConsoleColor.DarkCyan);
	}

	public static void WriteSuccess(string message) => WriteColored(message, ConsoleColor.Green);

	public static void WriteWarning(string message) => WriteColored(message, ConsoleColor.Yellow);

	public static void WriteInfo(string message) => WriteColored(message, ConsoleColor.Gray);

	public static void WriteColored(string message, ConsoleColor color)
	{
		var previousColor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.WriteLine("\n" + message);
		Console.ForegroundColor = previousColor;
	}

    public static int ReadInt(string prompt)
	{
		while (true)
		{
			Console.Write(prompt);
			string? input = Console.ReadLine();
			if (int.TryParse(input, out int value))
				return value;

			Console.WriteLine("Please enter a valid number.");
		}
	}

	public static string ReadString(string prompt)
	{
		Console.Write(prompt);
		return Console.ReadLine()?.Trim() ?? string.Empty;
	}

	public static string ReadOptionalString(string prompt)
	{
		Console.Write(prompt);
		return Console.ReadLine()?.Trim() ?? string.Empty;
	}
}
