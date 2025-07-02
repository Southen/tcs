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
using System.Collections.Immutable;
using System.Linq;

namespace TCS {
	public enum ThargoidVariant {
		Scout,
		// Interceptors:
		Cyclops,
		Basilisk,
		Medusa,
		Hydra,
		Orthrus,
		// Hunters:
		Glaive,
		Scythe,
		// Skimmers:
		Scavenger,
		Revenant,
		Banshee
	}

	public class Thargoid {
		public ThargoidVariant Variant { get; set; }
		public int Payout { get; set; }
		public DateOnly AfterDate { get; set; }

		public Thargoid(ThargoidVariant variant, int payout, DateOnly? afterDate = null) {
			Variant = variant;
			Payout = payout;
			AfterDate = afterDate ?? DateOnly.MinValue;
		}

		public override string ToString() => $"{Variant} {Payout} {AfterDate}";

		public static ImmutableArray<Thargoid> Map => new Thargoid[] {
			// Logs with pre-Dec 2020 values:
			new(ThargoidVariant.Scout,        10000),
			new(ThargoidVariant.Cyclops,   2_000000),
			new(ThargoidVariant.Basilisk,  6_000000),
			new(ThargoidVariant.Medusa,   10_000000),
			new(ThargoidVariant.Hydra,    15_000000),
			// Logs ≈post-Dec 2020: https://forums.frontier.co.uk/threads/game-balancing-pt-3.560418/
			new(ThargoidVariant.Scout,        80000, new DateOnly(2021, 9, 1)),
			new(ThargoidVariant.Cyclops,   8_000000, new DateOnly(2021, 9, 1)),
			new(ThargoidVariant.Basilisk, 24_000000, new DateOnly(2021, 9, 1)),
			new(ThargoidVariant.Medusa,   40_000000, new DateOnly(2021, 9, 1)),
			new(ThargoidVariant.Hydra,    60_000000, new DateOnly(2021, 9, 1)),
			//new(ThargoidVariant.Hydra,  80_000000, new DateOnly(2021, 9, 1)),
			new(ThargoidVariant.Orthrus,  30_000000, new DateOnly(2021, 9, 1)),
			// Logs post 14.02: https://forums.frontier.co.uk/threads/information-regarding-ax-combat-bond-reward-revisions.613979/
			new(ThargoidVariant.Scout,        65000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Scout,        75000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Cyclops,   6_500000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Basilisk, 20_000000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Medusa,   34_000000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Hydra,    50_000000, new DateOnly(2023, 1, 31)),
			//new(ThargoidVariant.Orthrus,  25_000000), [citation needed]
			new(ThargoidVariant.Orthrus,  40_000000, new DateOnly(2023, 1, 31)),
			new(ThargoidVariant.Glaive,    4_500000),
			new(ThargoidVariant.Scythe,    4_500000),
			new(ThargoidVariant.Revenant,     25000),
			new(ThargoidVariant.Banshee,   1_000000),
			// Logs post 18.06: https://forums.frontier.co.uk/threads/elite-dangerous-update-18-06-thargoid-war-overhaul-tuesday-28-may-2024.625728/
			new(ThargoidVariant.Cyclops,   8_000000, new DateOnly(2024, 5, 28)),
			new(ThargoidVariant.Basilisk, 24_000000, new DateOnly(2024, 5, 28)),
			new(ThargoidVariant.Medusa,   40_000000, new DateOnly(2024, 5, 28)),
			new(ThargoidVariant.Hydra,    60_000000, new DateOnly(2024, 5, 28)),
			new(ThargoidVariant.Orthrus,  15_000000, new DateOnly(2024, 5, 28)),
		}
		.OrderByDescending(t => t.AfterDate)
		.ToImmutableArray();
	}
}
