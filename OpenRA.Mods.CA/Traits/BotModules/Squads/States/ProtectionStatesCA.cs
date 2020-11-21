#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class ProtectionStateBase : GroundStateBaseCA
	{
	}

	class UnitsForProtectionIdleState : ProtectionStateBase, IState
	{
		public void Activate(SquadCA owner) { }
		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(owner.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));

				if (owner.TargetActor == null)
				{
					Retreat(owner, false, true, true);
					return;
				}
			}

			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionAttackState : ProtectionStateBase, IState
	{
		public const int BackoffTicks = 4;
		internal int Backoff = BackoffTicks;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(owner.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));

				if (owner.TargetActor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					return;
				}
			}

			// rescan target to prevent being ambushed and die without fight
			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (leader == null)
				return;
			var protectionScanRadius = WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius);
			var targetActor = owner.SquadManager.FindClosestEnemy(leader.CenterPosition, protectionScanRadius);
			var cannotRetaliate = false;

			if (targetActor != null)
				owner.TargetActor = targetActor;

			if (!owner.IsTargetVisible)
			{
				if (Backoff < 0)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					Backoff = BackoffTicks;
					return;
				}

				Backoff--;
			}
			else
			{
				cannotRetaliate = true;

				foreach (var a in owner.Units)
				{
					// Air units control:
					var ammoPools = a.TraitsImplementing<AmmoPool>().ToArray();
					if (a.Info.HasTraitInfo<AircraftInfo>() && ammoPools.Any())
					{
						if (BusyAttack(a))
						{
							cannotRetaliate = false;
							continue;
						}

						if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()))
						{
							if (IsRearming(a))
								continue;

							if (!HasAmmo(ammoPools))
							{
								owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
								continue;
							}
						}

						if (CanAttackTarget(a, owner.TargetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, leader.Location), false));
					}

					// Ground/naval units control:
					else
					{
						if (CanAttackTarget(a, owner.TargetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, leader.Location), false));
					}
				}
			}

			if (cannotRetaliate)
				owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionFleeState : ProtectionStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, true, true, true);
			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
