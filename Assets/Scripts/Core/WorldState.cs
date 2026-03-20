// WorldState.cs


// enums

public enum WorldState
{
    WorldA = 0,  // world ni leo
    WorldB = 1,  // world ni andr
    Rift = 2   // combined world, corrupted, mashed
}

public enum GamePhase
{
    Menu,        // main menu / lobby
    Stage1,      // "The Bleeding World"
    Transition,  // Cinematic between stages
    Stage2,      // "The Rift"
    GameOver,    // olats
    Win          // final boss defeated
}

public enum ItemType
{
    SmallVial,      // one plyr heal
    SharedPotion,   // heals both
    FountainPen,     // auto-fire ink gun (low damage, high fire rate)
    PixelShield,    // 100hp shield that absorbs damage until depleted/expired
    RiftShard,      // grants both players buffs (insta heal, increased damage, increase fire rate/movement spd)
}