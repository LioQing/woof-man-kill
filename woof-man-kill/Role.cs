namespace WoofManKill
{
    class Role
    {
        public enum SideType
        {
            Town,
            Woof,
        }

        public enum Type
        {
            BodyGuard,
            Doctor,
            Sheriff,
            Swapper,
            Spy,

            Killer,
            Freezer,
        }

        public SideType Side { get; private set; }

        private Type _ty;
        public Type Ty
        {
            get
            {
                return _ty;
            }
            private set
            {
                if (
                    value == Type.Killer ||
                    value == Type.Freezer
                )
                {
                    Side = SideType.Woof;
                }
                else
                {
                    Side = SideType.Town;
                }
                _ty = value;
            }
        }

        public Role(Type ty)
        {
            Ty = ty;
        }

        public string Description() => Ty switch
        {
            Type.BodyGuard => @"You are a bodyguard.

  - protect one person each night
  
  - if you are attacked, you and the attacker die
  
  - if your protected target is attacked, your target will live

  - you will know if your target is attacked
",

            Type.Doctor => @"You are a doctor.

  - heal one person each night
  
  - you can only heal yourself each second night
  
  - you will know if your target is attacked
",

            Type.Sheriff => @"You are a sheriff.

  - investigate 2 persons each night
  
  - you will know if the 2 persons are of same role
",

            Type.Swapper => @"You are a swapper.

  - swap the position of 2 persons each night
  
  - if the one of them was being taken an action, the other one is affected instead
",

            Type.Spy => @"You are a spy.

  - check whether a person visited another person each night
",

            Type.Killer => @"You are a killer.

  - kill one person at night
",

            Type.Freezer => @"You are a freezer.

  - freeze a person's action at night
",
  
            _ => throw new NotImplementedException(),
        }
        + Side switch
        {
            SideType.Town => "\nObjective: kill all the wooves.\n",
            SideType.Woof => "\nObjective: kill all the townies.\n",
            _ => throw new NotImplementedException(),
        };

        public string NightTimePrompt() => Description() + "\n" +
        Ty switch
        {
            Type.BodyGuard => "Type '/prot <player name>' to protect someone.",
            Type.Doctor => "Type '/heal <player name>' to heal someone.",
            Type.Sheriff => "Type '/inve <player name> <player name>' to investigate someone.",
            Type.Swapper => "Type '/swap <player name> <player name>' to swap the position of someone.",
            Type.Spy => "Type '/spy <player name>' to spy on someone.",
            Type.Killer => "Type '/kill <player name>' to kill someone. You may chat with the other woof.",
            Type.Freezer => "Type '/freeze <player name>' to freeze someone's action. You may chat with the other woof.",
            _ => throw new NotImplementedException(),
        };
    }
}