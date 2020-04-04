using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TestSPMod
{
    public class WorkTheFields : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
        }

      

            private float _workProgressHours = 0;

        private void onWaitTick(MenuCallbackArgs args, CampaignTime dt)
        {
           
            this._workProgressHours += (float)dt.ToHours;
            
        }

        private void game_menu_stop_working_at_village_on_consequence(MenuCallbackArgs args)
        {
            CalculateProduce();
            EnterSettlementAction.ApplyForParty(MobileParty.MainParty, MobileParty.MainParty.LastVisitedSettlement);
            GameMenu.SwitchToMenu("village");
        }

        private void CalculateProduce()
        {
            if (this._workProgressHours >= 1)
            {
                var village = Hero.MainHero.PartyBelongedTo.LastVisitedSettlement.Village;
                var products = village.VillageType.Productions;
                if (Hero.MainHero.IsPartyLeader)
                {
                    var strength = Hero.MainHero.PartyBelongedTo.GetTotalStrengthWithFollowers();
                    products = products.Select(x => (x.Item1,
                        ((x.Item2 * (this._workProgressHours / 24)) / village.GetNumberOfTroops()) * strength)).ToList();
                }

                foreach (var (item, amount) in products)
                {
                    var intAmount = (int) Math.Round(amount, MidpointRounding.ToEven);
                    Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item,
                        intAmount < 1 ? 1 : intAmount);

                    InformationManager.DisplayMessage(new InformationMessage($"Produced: {intAmount} {item.Name}",
                        "event:/ui/notification/child_born"));
                }

                InformationManager.DisplayMessage(new InformationMessage("You Stopped Working",
                    "event:/ui/notification/child_born"));

                this._workProgressHours = 0;
            }
        }

        private void game_menu_work_village_on_consequence(MenuCallbackArgs args)
        {
            this._workProgressHours = 0; 
            GameMenu.SwitchToMenu("village_work_menus");
            
            LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
        }

        private bool game_menu_stop_waiting_at_village_on_condition(MenuCallbackArgs args)
        {
            CalculateProduce();
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private bool game_menu_village_work_on_condition(MenuCallbackArgs args)
        {
            
            this._workProgressHours = 0f;
            Village village = Settlement.CurrentSettlement.Village;
            args.MenuContext.GameMenu.AllowWaitingAutomatically();
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            return village.VillageState == Village.VillageStates.Normal;
        }

        private bool game_menu_work_here_on_condition(MenuCallbackArgs args)
        {
            args.MenuContext.GameMenu.AllowWaitingAutomatically();
            args.optionLeaveType = GameMenuOption.LeaveType.Wait;
            MBTextManager.SetTextVariable("CURRENT_SETTLEMENT", Settlement.CurrentSettlement.EncyclopediaLinkWithName, false);
            return true;
        }
        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("village", "village_work", "Work", game_menu_work_here_on_condition, this.game_menu_work_village_on_consequence,false,3);
            obj.AddWaitGameMenu("village_work_menus", "You are Working", null, game_menu_village_work_on_condition, null, onWaitTick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.SettlementWithBoth);

            obj.AddGameMenuOption("village_work_menus", "work_leave", "Stop Working", game_menu_stop_waiting_at_village_on_condition, this.game_menu_stop_working_at_village_on_consequence, true);
          

        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {

                dataStore.SyncData("_workProgressHours", ref _workProgressHours);
            }
            catch (NullReferenceException doesntExist)
            {
                
            }
        }
    }
}