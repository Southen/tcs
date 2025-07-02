/*
 * Thargoid Combat Statistics
 * Copyright (c) 2021 Sebastian Southen
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace TCS {
	public partial class Program {
		// Ex: C:\Users\%USERNAME%\Saved Games
		private static Guid SavedGames => Guid.Parse("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");

		private static string[] timestampFormats => new[] {
			"yyyy-MM-ddTHHmmss",	// Journal.2022-03-30T221949.01.log
			"yyMMddHHmmss"			// Journal.200619124437.01.log
		};

		[DllImport("shell32.dll")]
		internal extern static int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid folderId, uint flags, IntPtr token, [MarshalAs(UnmanagedType.LPWStr)] out string pszPath);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal extern static uint GetConsoleProcessList(uint[] processList, uint processCount);

		// https://stackoverflow.com/a/50244207
		// https://devblogs.microsoft.com/oldnewthing/20160125-00/?p=92922
		private static bool consoleWillBeDestroyed() {
			var processList = new uint[1];
			var processCount = GetConsoleProcessList(processList, 1);
			return processCount == 1;
		}

		public static void Main(string[] args) {
			Console.Title = "Thargoid Combat Statistics";
			if (SHGetKnownFolderPath(SavedGames, 0, IntPtr.Zero, out string path) != 0) {
				throw new DirectoryNotFoundException("Saved Games folder not found");
			}
			string logs = Path.GetFullPath(Path.Combine(path, "Frontier Developments/Elite Dangerous"));

			string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".json");
			Cache state = Cache.Read(cachePath) ?? new Cache();
			DateTimeOffset curr = DateTimeOffset.MinValue;

			foreach (var log in Directory.GetFiles(logs, "Journal.*.log")
				.OrderBy(s => s.Contains('-'))  // Sort new yyyy-MM-dd logs after old yyMMdd ones
				.ThenBy(s => s)                 // Sort by datestamp
			) {
				Console.SetCursorPosition(0, Console.CursorTop);

				var timestamp = log.Split('.').Skip(1).First();
				curr = DateTimeOffset.ParseExact(
					timestamp,
					timestampFormats,
					CultureInfo.InvariantCulture.DateTimeFormat,
					DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal
				);
				if (curr < state.Last) {
					Console.Write("Skipping " + log);
					continue;
				}
				Console.Write(("Analysing " + log)/*.PadRight(Console.BufferWidth - 1)*/);

				state.ParseLog(log);
			}
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, Console.CursorTop);

			Cache.Save(cachePath, state);

			showStats(state, args.Any(a => a == "-c" || a == "--colour"));

			if (consoleWillBeDestroyed()) {
				Console.WriteLine();
				Console.WriteLine("Press any key to continue . . . ");
				Console.ReadKey();
			}
		}

		private static void showStats(Cache state, bool colour = false) {
			if (colour) Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine("          THARGOID COMBAT STATS             ");
			Console.WriteLine("--------------------------------------------");
			if (colour) Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(" Thargoid Scouts killed:                {0,4}", state.Counts[ThargoidVariant.Scout]);
			if (colour) Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine(" Cyclops Variant Interceptors killed:   {0,4}", state.Counts[ThargoidVariant.Cyclops]);
			if (colour) Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine(" Basilisk Variant Interceptors killed:  {0,4}", state.Counts[ThargoidVariant.Basilisk]);
			if (colour) Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine(" Medusa Variant Interceptors killed:    {0,4}", state.Counts[ThargoidVariant.Medusa]);
			if (colour) Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(" Hydra Variant Interceptors killed:     {0,4}", state.Counts[ThargoidVariant.Hydra]);
			if (colour) Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(" Orthrus Variant Interceptors killed:   {0,4}", state.Counts[ThargoidVariant.Orthrus]);
			Console.WriteLine(" Glaive Variant Hunters killed:         {0,4}", state.Counts[ThargoidVariant.Glaive]);
			Console.WriteLine(" Scythe Variant Hunters killed:         {0,4}", state.Counts[ThargoidVariant.Scythe]);
			if (colour) Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine(" Total Thargoids killed:                {0,4}", state.Counts.Sum(s => s.Value));
			Console.WriteLine();
			Console.WriteLine(" Total Thargoid combat bonds:    {0,11:n0}", state.CombatBonds);
			Console.WriteLine(" Total Thargoid mission payouts: {0,11:n0}", state.MissionRewards);
			if (colour) Console.ResetColor();
		}
	}
}
