using System;
using XRL;
using XRL.World;
using XRL.World.Parts;

namespace XRL.World.Parts
{
    public class Klehrik_ShieldPassiveOnly : IPart
    {
    }
    
    public class Klehrik_ModBlocking : IMeleeModification
    {
        public Klehrik_ModBlocking()
        {
        }

        public Klehrik_ModBlocking(int Tier)
            : base(Tier)
        {
        }

        public override void ApplyModification(GameObject Object)
        {
            Shield p = Object.AddPart<Shield>();
            p.AV = GetBlockValue(Tier);
            p.WornOn = Object.GetPart<MeleeWeapon>().Slot;

            Object.AddPart<Klehrik_ShieldPassiveOnly>();
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == GetDisplayNameEvent.ID;
        }

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Understood() && !E.Object.HasProperName)
            {
                E.AddAdjective("{{c|blocking}}", 20);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules(GetInstanceDescription());
            return base.HandleEvent(E);
        }

        public static int GetBlockValue(int Tier)
        {
            return 1 + (int)Math.Floor(Tier / 2.2f);
        }

        public static string GetDescription(int Tier)
        {
            return "Blocking: This weapon can function as a " + ((Tier > 0) ? (GetBlockValue(Tier) + " AV ") : "") + "shield (excluding activated abilities).";
        }

        public string GetInstanceDescription()
        {
            return "Blocking: This weapon can function as a " + GetBlockValue(Tier) + " AV shield (excluding activated abilities).";
        }
    }
}