using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace StpSDK;

public class NullSafeStringEnumConverter : StringEnumConverter
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        try
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        catch
        {
            return null;
        }
    }
}

#pragma warning disable CS1591
public enum Strength { none = 0, reduced = 1, reinforced = 2, reduced_reinforced = 3 };

public enum CodingScheme
{
    unknown = 0, warfighting = 1, tactical_graphics = 2, metoc = 3, intelligence = 4, mootw = 5, emergency = 6,
    ui = 7, macro = 8
};

public enum Affiliation
{
    pending = 0, unknown = 1, assumedfriend = 2, friend = 3, neutral = 4, suspected = 5, hostile = 6,
    joker = 5, faker = 6,
    exercisepending = 10, exerciseunknown = 11, exerciseassumedfriend = 12, exercisefriend = 13, exerciseneutral = 14,
    exercisesuspected = 15, exercisehostile = 16,
    simulationpending = 20, simulationunknown = 21, simulationassumedfriend = 22, simulationfriend = 23, simulationneutral = 24,
    simulationsuspected = 25, simulationhostile = 26
};

public enum BattleDimension
{
    unknown = 0, air = 1, air_missile = 2, space = 5, space_missile = 6,
    ground = 10, land_civilian_unit_organization = 11, land_equipment = 15,
    land_installation = 20, control_measure = 25, sea_surface = 30,
    sub_surface = 35, mine_warfare = 36, activities = 40,
    atmospheric = 45, oceanographic = 46, meteorological_space = 47,
    signals_intelligence_space = 50, signals_intelligence_air = 51,
    signals_intelligence_land = 52, signals_intelligence_surface = 53,
    signals_intelligence_subsurface = 54, cyberspace = 60
};

public enum Status
{
    present = 0, anticipated = 1, fully_capable = 2, damaged = 3, destroyed = 4, full = 5
};

public enum Echelon
{
    none = 0, team = 11, squad = 12, section = 13, platoon = 14, company = 15, battery = 15, troop = 15, battalion = 16, regiment = 17, brigade = 18, division = 21, corps = 22, army = 23, armygroup = 24, region = 25, command = 26
};

public enum Modifier
{
    none = '-', dummy = 1, hq = 2, dummy_hq = 3, task_force = 4, dummy_task_force = 5, task_force_hq = 6, dummytask_force_hq = 7
};

public enum Mobility
{
    none = 0, wheeled_limited_cross_country = 31, wheeled_cross_country = 32, tracked = 33, wheeled_and_tracked_combination = 34, towed = 35, rail = 36, pack_animals = 37,
    over_snow_prime_mover = 41, sled = 42,
    barge = 51, amphibious = 52,
    short_towed_array = 61, long_towed_array = 62
};

public enum Branch
{
    na, weapon, ground_unit, civilian_air, special_operations, vstol, equipment, installation, military_air, military_sea, military_submarine
};

public enum OrderOfBattle
{
    none = 0, air = 1, electronic = 2, civilian = 3, ground = 4, maritime = 5, strategic_force = 6, control_markings = 7
};

public enum GeometryTypeEnum { NA, POINT, LINE, AREA, MIXED }

public enum CommandRelationship
{
    none,
    organic,
    attached,
    assigned,
    adcon,
    opcon,
    tacon,
    ds,
    r,
    gsr,
    gs
};

public enum TaskWhat
{
    NOT_SPECIFIED,
    ADVISE_POLICE,
    AMBUSH,
    ASSIGN_RESPONSIBILITY,
    BLOCK,
    BOMB_ATTACK,
    BREACH,
    BYPASS,
    CLEAR,
    COERCIVE_RECRUITING,
    COLLECT_CASUALTIES,
    COLLECT_CIVILIANS,
    COLLECT_PRISONERS,
    CONDUCT_AMBUSH,
    CONDUCT_AVIATON_AMBUSH,
    CONDUCT_BILAT,
    CONDUCT_GROUP_ENGAGEMENT,
    CONDUCT_RAID,
    CONDUCT_TCP_OPERATION,
    CONSTITUTE_RESERVE,
    CONVOY,
    DEFEAT,
    DELAY,
    DELIVER_LEAFLET_PSYOP,
    DEMONSTRATE,
    DESTROY,
    DISRUPT,
    DISTRIBUTE_FOOD,
    EMPLACE,
    EQUIP_POLICE,
    ESCORT_CONVOY,
    EVACUATE_CASUALTIES,
    EVACUATE_CIVILIANS,
    EVACUATE_PRISONERS,
    FIX,
    FOLLOW,
    FOLLOW_AND_ASSUME,
    FOLLOW_AND_SUPPORT,
    HALT,
    HARRASSMENT_FIRES,
    HOUSE_TO_HOUSE_PSYOP,
    IED_ATTACK,
    LIMIT,
    LOOTING,
    MAINTAIN_HIDE,
    MAINTAIN_OUTPOST,
    MOVE,
    NEUTRALIZE,
    OBSERVE,
    OCCUPY,
    PATROL,
    PENETRATE,
    POSITION_SNIPER,
    PRIORITY_OF_FIRES,
    PROVIDE_MEDICAL_SERVICES,
    PROVIDE_SERVICE,
    RECEIVE,
    RECONSTRUCTION,
    RECRUIT_POLICE,
    REFUEL,
    REGULATE_TRAFFIC,
    REINFORCE,
    RELEASE,
    RESUPPLY,
    RETAIN,
    RIOTING,
    SECURE,
    SEEK_REFUGE,
    SEIZE,
    SNIPER_ATTACK,
    SUPPLY,
    SUPPLY_MUNITIONS,
    TRAIN_POLICE,
    TRANSFER_MUNITIONS,
    TRASH_REMOVAL,
    TURN,
    TV_RADIO_PSYOP,
    WATER_DELIVERY,
    WILLFUL_RECRUITING
}

public enum TaskHow
{
    NOT_SPECIFIED,
    AIR_ASSAULT,
    AIR_RECONNAISSANCE,
    AREA_DEFENSE,
    ASSAULT,
    ATTACK,
    ATTACK_IN_ZONE,
    ATTACK_BY_FIRE,
    CERP_FUNDING,
    CIVILIAN,
    CONTRACTING,
    CORDON_AND_SEARCH,
    COUNTERATTACK,
    COUNTERATTACK_BY_FIRE,
    COVER,
    DEFEND,
    DELIVER_SERVICES,
    GUARD,
    INFORMATION_OPERATIONS,
    INSURGENT,
    MOBILE_DEFENSE,
    MOVING_SCREEN,
    NGO_OPERATION,
    PASSAGE_OF_LINES,
    SCREEN,
    SEARCH_AND_ATTACK,
    SECURITY,
    SECURITY_FORCE_ASSISTANCE,
    SUPPORT_BY_FIRE,
    WITHDRAWAL
}

public enum TaskWhy
{
    UNKNOWN,
    ALLOW,
    CAUSE,
    CREATE,
    DECEIVE,
    DENY,
    DIVERT,
    ENABLE,
    ENVELOP,
    INFLUENCE,
    OPEN,
    PREVENT,
    PROTECT,
    SUPPORT,
    SURPRISE
}

public enum ROE
{
    NOT_SPECIFIED,
    Hold,
    Free,
    Tight
}
#pragma warning restore CS1591
