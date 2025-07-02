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

namespace TCS {
	public class LogEvent {
		// Common:
		public DateTime Timestamp { get; set; }
		public string Event { get; set; }
		public int Reward { get; set; }

		// Bonds:
		public string AwardingFaction { get; set; }
		public string VictimFaction { get; set; }

		// Missions:
		public string Name { get; set; }
		public string TargetType { get; set; }
		public int KillCount { get; set; }

		public override string ToString() => $"{Timestamp} {Event} {Reward}";
	}
}
