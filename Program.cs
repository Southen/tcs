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

		public static void Main(string[] args) {
			//Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

			if (SHGetKnownFolderPath(SavedGames, 0, IntPtr.Zero, out string path) != 0) {
				throw new DirectoryNotFoundException("Saved Games folder not found");
			}
			string logs = Path.GetFullPath(Path.Combine(path, "Frontier Developments/Elite Dangerous"));
			Console.WriteLine("Analysing " + logs);

			var counts = Enum.GetValues<ThargoidRewards>().ToDictionary<ThargoidRewards, ThargoidRewards, uint>(s => s, s => 0);
			int missionRewards = 0;
			
			foreach (var log in Directory.GetFiles(logs, "Journal.*.log").OrderBy(s => s)) {
				var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var sr = new StreamReader(fs);
				//Console.WriteLine(log);

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

							if (!Enum.IsDefined(typeof(ThargoidRewards), evt.Reward)) {
								Console.WriteLine("Unknown reward value {0}", evt.Reward);
							}
							counts[ (ThargoidRewards)evt.Reward ]++;
						}
					}
					else if (evt.Event == "MissionCompleted") {
						if (evt.Name == "Mission_MassacreThargoid_name") {
							missionRewards += evt.Reward;
						}
					}
				}
			}

			Console.WriteLine("--------------------------------------------");
			Console.WriteLine("          THARGOID COMBAT STATS             ");
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine(" Thargoid Scouts killed:                {0,4}", counts[ThargoidRewards.Scout]);
			Console.WriteLine(" Cyclops Variant Interceptors killed:   {0,4}", counts[ThargoidRewards.Cyclops]);
			Console.WriteLine(" Basilisk Variant Interceptors killed:  {0,4}", counts[ThargoidRewards.Basilisk]);
			Console.WriteLine(" Medusa Variant Interceptors killed:    {0,4}", counts[ThargoidRewards.Medusa]);
			Console.WriteLine(" Hydra Variant Interceptors killed:     {0,4}", counts[ThargoidRewards.Hydra]);
			Console.WriteLine("--------------------------------------------");
			Console.WriteLine(" Total Thargoids killed:                {0,4}", counts.Sum(s => s.Value));
			Console.WriteLine("");
			Console.WriteLine(" Total Thargoid mission payouts:  {0,10:n0}", missionRewards);
		}

		enum ThargoidRewards {
			// Logs still have the pre-Dec 2020 values
			Scout		=    10000,
			Cyclops		=  2000000,
			Basilisk	=  6000000,
			Medusa		= 10000000,
			Hydra		= 15000000,
		}

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
