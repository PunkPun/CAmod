#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	class AlliedCoalitionIndicatorLogic : ChromeLogic
	{
		const string NoneImage = "none";
		const string DisabledImage = "disabled";

		string chosenCoalition;

		private readonly UpgradesManager upgradesManager;

		[ObjectCreator.UseCtor]
		public AlliedCoalitionIndicatorLogic(Widget widget, World world)
		{
			var container = widget.Get<ContainerWidget>("ALLIED_COALITION");
			var coalitionImage = container.Get<ImageWidget>("ALLIED_COALITION_IMAGE");

			if (world.LocalPlayer.Faction.Side != "Allies")
			{
				coalitionImage.GetImageName = () =>  DisabledImage;
				coalitionImage.IsVisible = () => false;
				return;
			}

			upgradesManager = world.LocalPlayer.PlayerActor.Trait<UpgradesManager>();

			if (upgradesManager == null)
			{
				coalitionImage.GetImageName = () => NoneImage;
				coalitionImage.IsVisible = () => true;
				return;
			}

			upgradesManager.UpgradeCompleted += HandleUpdatedCompleted;

			coalitionImage.GetImageName = () =>  $"{chosenCoalition ?? NoneImage}";
			coalitionImage.IsVisible = () => true;
		}

		private void HandleUpdatedCompleted(string coalitionName)
		{
			if (coalitionName.EndsWith(".coalition"))
			{
				chosenCoalition = coalitionName.Split('.')[0];
				upgradesManager.UpgradeCompleted -= HandleUpdatedCompleted;
			}
		}
	}
}
