using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TestSPMod
{
    internal class NemesisSystem : CampaignBehaviorBase
    {
        private List<Clan> _nemesisClans = new List<Clan>();

        public override void RegisterEvents() 
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.MapEventEnded .AddNonSerializedListener(this,new Action<MapEvent>(OnMapEvent));
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {

           
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {

                dataStore.SyncData("_nemesisClans", ref this._nemesisClans);
            }
            catch (NullReferenceException doesntExist)
            {
                
            }
        }

        private void OnMapEvent(MapEvent obj)
        {
            
            switch (obj.BattleState)
            {
                case BattleState.None:
                    break;
                case BattleState.DefenderVictory:
                case BattleState.AttackerVictory:
                    var winnerSide = obj.BattleState == BattleState.AttackerVictory ? obj.AttackerSide: obj.DefenderSide;
                    
                    var looserSide = obj.BattleState == BattleState.AttackerVictory ? obj.DefenderSide : obj.AttackerSide;
                    var LooserParties = looserSide.PartiesOnThisSide;
                        
                    foreach (var VARIABLE in LooserParties)
                    {
                        if (VARIABLE.Owner == null) continue;
                        if (VARIABLE.Owner.IsWounded && MBRandom.RandomInt(0,3) == 2)
                        {
                            MakeNemesis(winnerSide.PartiesOnThisSide.GetRandomElement());
                        }
                    }
                    break;
                case BattleState.Dispersed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("list_nemesis_clans", "nemesis")]
        private static string PrintNemesisClans(List<string> strings)
        {
            var nemesisSystem =  Campaign.Current.GetCampaignBehavior<NemesisSystem>();
            if (nemesisSystem == null) return "Nemesis Mode not Enabled";
            for (var i = 0; i < nemesisSystem._nemesisClans.Count; i++)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ID: {i} Clan: {nemesisSystem._nemesisClans[i].EncyclopediaLinkWithName} Leader: {nemesisSystem._nemesisClans[i].Leader.EncyclopediaLinkWithName}",
                    "event:/ui/notification/army_created"));
            }

            return "";
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("goto_nemesis_clan", "nemesis")]
        private static string GotoNemesisClans(List<string> strings)
        {
            var nemesisSystem =  Campaign.Current.GetCampaignBehavior<NemesisSystem>(); 
            if (nemesisSystem == null||strings.Count != 1) return "Nemesis Mode not Enabled/Wrong Args";
            var index = int.Parse(strings[0]);
            for (var i = 0; i < nemesisSystem._nemesisClans.Count; i++)
            {
                if (i != index) continue;
                var playersParty = MobileParty.MainParty;
                if (playersParty !=null && playersParty.IsActive && playersParty.MapEvent == null && nemesisSystem._nemesisClans[i].Leader.PartyBelongedTo != null )
                {
                        
                    playersParty.Position2D = playersParty.FindReachablePointAroundPosition( nemesisSystem._nemesisClans[i].Leader.PartyBelongedTo.Position2D,0.2f,0.1f,false );
                        
                }
            }

            return "";
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("tp_all_nemesis_clans", "nemesis")]
        private static string TPALlNemesisClan(List<string> strings)
        {
            var nemesisSystem =  Campaign.Current.GetCampaignBehavior<NemesisSystem>();
            if (nemesisSystem == null||strings.Count != 0) return "Nemesis Mode not Enabled/Wrong Args";
           foreach (var t in nemesisSystem._nemesisClans)
            {
                var playersParty = MobileParty.MainParty;
                var nemesisLeaderPartyBelongedTo = t.Leader.PartyBelongedTo;
                if (nemesisLeaderPartyBelongedTo != null && playersParty != null &&
                    playersParty.IsActive && playersParty.MapEvent == null &&
                    nemesisLeaderPartyBelongedTo.IsActive &&
                    nemesisLeaderPartyBelongedTo.MapEvent == null)
                {
                        
                    nemesisLeaderPartyBelongedTo.Position2D = playersParty.FindReachablePointAroundPosition( playersParty.Position2D,0.2f,0.1f,false );
                        
                }
            }

            return "";
        }
        private void MakeNemesis(PartyBase winnerSide)
        {
            if (GetNemesisHomeAndCulture(winnerSide, out var winnerCulture, out var homeTown)) return;

            var clan = InitializeClan(winnerSide, homeTown, winnerCulture);
            var hero = InitializeHero(winnerSide, homeTown, clan);
            hero.Spouse =   CreateHeroSpouse(hero, homeTown, clan);
            hero.SetPersonalRelation(winnerSide.Owner,-10);
            hero.SetPersonalRelation(hero.Spouse,100);
            var mobileParty = clan.CreateNewMobilePartyAtPosition(hero,homeTown.GatePosition);
            mobileParty.Aggressiveness = 100;
            mobileParty.AddElementToMemberRoster(hero.CharacterObject, 1, true);
            List<CharacterObject> e = (from t in CharacterObject.All
                where t.IsSoldier && t.Culture == hero.Culture && t.Tier >= 3 && t.Tier <= 6
                select t).ToList<CharacterObject>();
            e.ForEach(unittype =>
                {
                    mobileParty.MemberRoster.AddToCounts(e.GetRandomElement(),
                        MBRandom.RandomInt(0, 100), false);
                }
            );
            float MaxValue = 10000f;
            ItemObject PackAnimalItemID = (ItemObject) null;
            foreach (var itemObject2 in ItemObject.All.Where(itemObject2 => itemObject2.ItemCategory == DefaultItemCategories.PackAnimal && (double) itemObject2.Value < (double) MaxValue))
            {
                PackAnimalItemID = itemObject2;
                MaxValue = (float) itemObject2.Value;
            }

            if (PackAnimalItemID != null)
            {
                mobileParty.ItemRoster.Add(new ItemRosterElement(PackAnimalItemID,
                    mobileParty.MemberRoster.TotalManCount, (ItemModifier) null));
            }
            foreach (var itemObject2 in ItemObject.All.Where(itemObject2 => itemObject2.ItemCategory == DefaultItemCategories.WarHorse && (double) itemObject2.Value < (double) MaxValue))
            {
                PackAnimalItemID = itemObject2;
                MaxValue = (float) itemObject2.Value;
            }

            if (PackAnimalItemID != null)
            {
                mobileParty.ItemRoster.Add(new ItemRosterElement(PackAnimalItemID,
                    mobileParty.MemberRoster.TotalManCount, (ItemModifier) null));
            }
            GiveGoldAction.ApplyBetweenCharacters(null, hero, mobileParty.MemberRoster.TotalManCount*10, true);
            GiveGoldAction.ApplyBetweenCharacters(null, hero.Spouse, mobileParty.MemberRoster.TotalManCount*10, true);
            mobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, mobileParty.MemberRoster.TotalManCount, true);
            mobileParty.ItemRoster.AddToCounts(DefaultItems.Meat, mobileParty.MemberRoster.TotalManCount, true);
            mobileParty.SetMovePatrolAroundPoint(mobileParty.Position2D);
            hero.Spouse.ChangeState(Hero.CharacterStates.Active);
            clan.SetPartyObjective(mobileParty, Clan.PartyObjective.Aggressive);
            foreach (var kingdom in Kingdom.All)
            {
                FactionManager.Instance.RegisterCampaignWar(kingdom,clan);
                FactionManager.SetStanceTwoSided(kingdom,clan,-10);
            }
            hero.ChangeState(Hero.CharacterStates.Active);
            this._nemesisClans.Add(clan);
            InformationManager.DisplayMessage(new InformationMessage($"Warrior {hero.EncyclopediaLinkWithName} gained Fame and founded his Clan: {clan.EncyclopediaLinkWithName}",
                "event:/ui/notification/army_created"));
        }

        private static Hero CreateHeroSpouse(Hero hero, Settlement homeTown, Clan clan)
        {
            var spouse = HeroCreator.CreateSpecialHero(CharacterObject.All.Where(x=>x.IsHero).Where(x=>x.IsFemale == !hero.IsFemale).GetRandomElement(), homeTown, clan, null, -1);
            
            EnterSettlementAction.ApplyForCharacterOnly(spouse,homeTown);
            return spouse;
        }

        private static Hero InitializeHero(PartyBase winnerSide, Settlement homeTown, Clan clan)
        {
            var hero = HeroCreator.CreateSpecialHero(winnerSide.Owner.CharacterObject, homeTown, clan, null, -1);
            clan.SetLeader(hero);
            hero.AlwaysDie = true;
            hero.IsMercenary = true;
            
            EnterSettlementAction.ApplyForCharacterOnly(hero,homeTown);
            return hero;
        }

        private static Clan InitializeClan(PartyBase winnerSide, Settlement homeTown, CultureObject winnerCulture)
        {
            var clanName =
                new TextObject(NameGenerator.Current.GenerateClanName(winnerSide.Culture, homeTown) + "NEMESIS");
            var clan = MBObjectManager.Instance.CreateObject<Clan>(string.Concat(new object[]

            {
                $"nemesis_clan_{MBRandom.RandomInt(0, 100000)}_",
                clanName.ToString(),
                "_",
                Clan.All.Count((Clan t) => t.Name == clanName)
            }));

            clan.InitializeClan(clanName, clanName, winnerCulture, Banner.CreateRandomClanBanner(-1));
            clan.InitialPosition = homeTown.GatePosition;
            clan.InitializeHomeSettlement(homeTown);
            
            clan.ClanLeaveKingdom(false);
            clan.AddRenown(1000, true);
            return clan;
        }

        private static bool GetNemesisHomeAndCulture(PartyBase winnerSide, out CultureObject winnerCulture,
            out Settlement homeTown)
        {
            winnerCulture = null;
            homeTown = null;
            if (winnerSide.Owner == null ||
                !winnerSide.MobileParty.MemberRoster.Any(x => x.Character.Culture.IsMainCulture)) return true;
            var wc = winnerCulture = winnerSide.MobileParty.MemberRoster.Where(x => x.Character.Culture.IsMainCulture).GetRandomElement()
                .Character.Culture;
            
            homeTown = Town.All.Where(x => x.Culture == wc).GetRandomElement().Settlement;
            if (homeTown == null)
            {
                wc = winnerCulture = MBObjectManager.Instance.GetObjectTypeList<CultureObject>()
                    .Where(x => x.IsMainCulture).Where(
                        culture => Town.All.Count(town => town.Culture == culture) > 0).GetRandomElement();
                homeTown = Town.All.Where(x => x.Culture == wc).GetRandomElement().Settlement;
            }

            return false;
        }
    }
}