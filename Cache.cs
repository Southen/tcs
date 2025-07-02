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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TCS {
	public class Cache {
		private static JsonSerializerOptions jsonOptions => new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			WriteIndented = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
		};

		public DateTimeOffset Last { get; set; }
		public Dictionary<ThargoidVariant, uint> Counts { get; set; }
		public int CombatBonds { get; set; }
		public int MissionRewards { get; set; }

		public Cache() {
			Last = DateTimeOffset.MinValue;
			Counts = Enum.GetValues<ThargoidVariant>()
				.ToDictionary<ThargoidVariant, ThargoidVariant, uint>(s => s, s => 0);
			//Counts = new uint[ (int)Enum.GetValues<ThargoidVariant>().Max() ];
			//MissionRewards = 0;
		}

		public static Cache Read(string cachePath) {
			if (File.Exists(cachePath)) {
				try {
					var json = File.ReadAllText(cachePath);
					var cache = JsonSerializer.Deserialize<Cache>(json, jsonOptions);
					if (cache.Counts.Count < Enum.GetNames<ThargoidVariant>().Length
						|| cache.CombatBonds <= 0)
						return null;	// Reset cache on new types/features
					return cache;
				}
				catch (UnauthorizedAccessException e) {
					Console.Error.WriteLine("Unable to read cache from {0}: {1}", cachePath, e.Message);
				}
				catch (JsonException e) {
					Console.Error.WriteLine("Error reading cache from {0}: {1}", cachePath, e.Message);
				}
			}
			return null;
		}

		public static void Save(string cachePath, Cache state) {
			try {
				File.WriteAllText(cachePath, JsonSerializer.Serialize(state, jsonOptions));
			}
			catch (UnauthorizedAccessException e) {
				Console.Error.WriteLine("Unable to write cache to {0}: {1}", cachePath, e.Message);
			}
		}

		public void ParseLog(string log) {
			var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var sr = new StreamReader(fs);

			var lines = sr.ReadToEnd();
			if (!lines.Contains("Thargoid"))
				return;

			StringReader lr = new StringReader(lines);
			string line;
			while ((line = lr.ReadLine()) != null) {
				try {
					parseLine(line);
				}
				catch (JsonException) { /* Ignore partially written lines */ }
			}
		}

		private void parseLine(string line) {
			LogEvent evt = JsonSerializer.Deserialize<LogEvent>(line, jsonOptions);

			if (evt.Event == "FactionKillBond") {
				if (!string.IsNullOrEmpty(evt.VictimFaction) && evt.VictimFaction.Contains("$faction_Thargoid")) {
					//Debug.WriteLine("{0} {1} {2} => {3}", evt.Timestamp, evt.VictimFaction, evt.Reward, (ThargoidRewards)evt.Reward);

					var payout = Thargoid.Map.FirstOrDefault(t
						=> DateOnly.FromDateTime(evt.Timestamp) > t.AfterDate
						&& evt.Reward == t.Payout);

					if (payout == default) {
						if (Console.GetCursorPosition().Left > 0)
							Console.Error.WriteLine();
						Console.Error.WriteLine("Unknown reward value {0} at {1}", evt.Reward, evt.Timestamp.ToString("d"));
						return;
					}
					Counts[payout.Variant]++;
					CombatBonds += evt.Reward;
				}
			}
			else if (evt.Event == "MissionCompleted") {
				if (evt.Name == "Mission_MassacreThargoid_name") {
					MissionRewards += evt.Reward;
				}
			}
			Last = evt.Timestamp;
		}

		public override string ToString() => $"{Counts.Sum(c => c.Value)} Thargoids @ {Last}";
	}
}
