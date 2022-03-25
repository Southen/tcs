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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace TCS {
	public class Program {
		public static readonly Guid SavedGames = Guid.Parse("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");

		[DllImport("shell32.dll")]
		extern static int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid folderId, uint flags, IntPtr token, [MarshalAs(UnmanagedType.LPWStr)] out string pszPath);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
		// https://stackoverflow.com/a/50244207
		// https://devblogs.microsoft.com/oldnewthing/20160125-00/?p=92922
		private static bool ConsoleWillBeDestroyed() {
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

			Cache state = new Cache();
			string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".json");
			if (File.Exists(cachePath)) {
				try {
					var json = File.ReadAllText(cachePath);
					state = JsonSerializer.Deserialize<Cache>(json);
				}
				catch (UnauthorizedAccessException e) {
					Console.Error.WriteLine("Unable to read cache from {0}", cachePath);
				}
			}
			DateTimeOffset curr = DateTimeOffset.MinValue;

			foreach (var log in Directory.GetFiles(logs, "Journal.*.log").OrderBy(s => s)) {
				Console.SetCursorPosition(0, Console.CursorTop);

				var ts = log.Split('.').Skip(1).First();
				//                            Journal.200619124437.01.log
				curr = DateTimeOffset.ParseExact(ts, "yyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
				if (curr <= state.Last) {
					Console.Write("Skipping " + log);
					continue;
				}
				Console.Write(("Analysing " + log)/*.PadRight(Console.BufferWidth - 1)*/);

				var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var sr = new StreamReader(fs);

				var lines = sr.ReadToEnd();
				if (!lines.Contains("Thargoid")) continue;
				using StringReader lr = new StringReader(lines);
				string line;
				LogEvent evt;

				while ((line = lr.ReadLine()) != null) {
					evt = JsonSerializer.Deserialize<LogEvent>(line, new JsonSerializerOptions {
						PropertyNameCaseInsensitive = true
					});

					if (evt.Event == "FactionKillBond") {
						if (!string.IsNullOrEmpty(evt.VictimFaction) && evt.VictimFaction.Contains("$faction_Thargoid")) {
							//Console.WriteLine("{0} {1} {2} => {3}", evt.Timestamp, evt.VictimFaction, evt.Reward, (ThargoidRewards)evt.Reward);

							if (!Map.ContainsKey(evt.Reward)) {
								if (Console.GetCursorPosition().Left > 0)
									Console.Error.WriteLine();
								Console.Error.WriteLine("Unknown reward value {0}", evt.Reward);
								continue;
							}
							state.Counts[ Map[evt.Reward] ]++;
						}
					}
					else if (evt.Event == "MissionCompleted") {
						if (evt.Name == "Mission_MassacreThargoid_name") {
							state.MissionRewards += evt.Reward;
						}
					}
				}
			}
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, Console.CursorTop);

			state.Last = curr/*DateTimeOffset.Now*/;
			try {
				File.WriteAllText(cachePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
			}
			catch (UnauthorizedAccessException e) {
				Console.Error.WriteLine("Unable to write cache to {0}", cachePath);
			}

			Console.WriteLine("--------------------------------------------");
			Console.WriteLine("          THARGOID COMBAT STATS             ");
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine(" Thargoid Scouts killed:                {0,4}", state.Counts[Thargoids.Scout]);
			Console.WriteLine(" Cyclops Variant Interceptors killed:   {0,4}", state.Counts[Thargoids.Cyclops]);
			Console.WriteLine(" Basilisk Variant Interceptors killed:  {0,4}", state.Counts[Thargoids.Basilisk]);
			Console.WriteLine(" Medusa Variant Interceptors killed:    {0,4}", state.Counts[Thargoids.Medusa]);
			Console.WriteLine(" Hydra Variant Interceptors killed:     {0,4}", state.Counts[Thargoids.Hydra]);
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine(" Total Thargoids killed:                {0,4}", state.Counts.Sum(s => s.Value));
			Console.WriteLine();
			Console.WriteLine(" Total Thargoid mission payouts: {0,11:n0}", state.MissionRewards);

			if (ConsoleWillBeDestroyed()) {
				Console.WriteLine();
				Console.WriteLine("Press any key to continue . . . ");
				Console.ReadKey();
			}
		}

		public enum Thargoids {
			Scout,
			Cyclops,
			Basilisk,
			Medusa,
			Hydra,
		}

		public readonly static Dictionary<int, Thargoids> Map = new Dictionary<int, Thargoids> {
			// Logs with pre-Dec 2020 values:
			{    10000, Thargoids.Scout },
			{  2000000, Thargoids.Cyclops },
			{  6000000, Thargoids.Basilisk },
			{ 10000000, Thargoids.Medusa },
			{ 15000000, Thargoids.Hydra },
			// Logs ≈post-Sep 2021:
			{    80000, Thargoids.Scout },
			{  8000000, Thargoids.Cyclops },
			{ 24000000, Thargoids.Basilisk },
			{ 40000000, Thargoids.Medusa },
			{ 60000000, Thargoids.Hydra },
			{ 80000000, Thargoids.Hydra },
		};

		public class Cache {
			public DateTimeOffset Last { get; set; }
			public Dictionary<Thargoids, uint> Counts { get; set; }
			//public uint[] Counts { get; set; }
			public int MissionRewards { get; set; }

			public Cache() {
				Last = DateTimeOffset.MinValue;
				Counts = Enum.GetValues<Thargoids>().ToDictionary<Thargoids, Thargoids, uint>(s => s, s => 0);
				//Counts = new uint[ (int)Enum.GetValues<Thargoids>().Max() ];
				MissionRewards = 0;
			}
		};

		public class LogEvent {
			public DateTime Timestamp { get; set; }
			public string Event { get; set; }
			public int Reward { get; set; }
			public string AwardingFaction { get; set; }
			public string VictimFaction { get; set; }

			public string Name { get; set; }
		}
	}
}
