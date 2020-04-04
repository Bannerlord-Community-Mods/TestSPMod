using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TestSPMod
{
    public class CustomSaveTickerTest : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, onTick);
        }

        private void onTick()
        {
            
             test.Add(Campaign.CurrentTime, new TestSaveAbleClass {myHero = Hero.MainHero,mystring = "TESTTTTTTT"});
        }

        Dictionary<float, TestSaveAbleClass> test = new Dictionary<float, TestSaveAbleClass>();
        
        public class TestSaveAbleClass
        {
            [SaveableField(1)] public Hero myHero;
            [SaveableField(2)]
            public string mystring = "";
        }

        public class MySaveDefiner : SaveableTypeDefiner
        {
            public MySaveDefiner() : base(10000001)
            {
            }

            protected override void DefineClassTypes()
            {
                AddClassDefinition(typeof(TestSaveAbleClass), 1);
            }

            protected override void DefineContainerDefinitions()
            {
                ConstructContainerDefinition(typeof (Dictionary<float, TestSaveAbleClass>));
            }
        }

        private void OnSessionLaunched()
        {
        

        }

        public override void SyncData(IDataStore dataStore)
        {
           
            dataStore.SyncData("test", ref test);
        }
    }
}