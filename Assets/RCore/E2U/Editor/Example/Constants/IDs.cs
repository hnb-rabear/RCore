/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 ***/

using System;

public class IDs
{
	#region Hero
	public const int HERO_1 = 1;
	public const int HERO_2 = 2;
	public const int HERO_3 = 3;
	public const int HERO_4 = 4;
	public const int HERO_5 = 5;
	#endregion
	#region Building
	public const int BUILDING_NULL = 0;
	public const int BUILDING_1 = 1;
	public const int BUILDING_2 = 2;
	public const int BUILDING_3 = 3;
	public const int BUILDING_4 = 4;
	public const int BUILDING_5 = 5;
	public const int BUILDING_6 = 6;
	public const int BUILDING_7 = 7;
	public const int BUILDING_8 = 8;
	public const int BUILDING_9 = 9;
	public const int BUILDING_10 = 10;
	public const int BUILDING_11 = 11;
	public const int BUILDING_12 = 12;
	public const int BUILDING_13 = 13;
	#endregion
	#region Pet Id
	public const int PET_NULL = 0;
	public const int PET_1 = 1;
	public const int PET_2 = 2;
	public const int PET_3 = 3;
	public const int PET_4 = 4;
	public const int PET_5 = 5;
	public const int PET_6 = 6;
	public const int PET_7 = 7;
	public const int PET_8 = 8;
	public const int PET_9 = 9;
	#endregion
	#region Race[enum]
	public const int RACE_NONE = 0;
	public const int RACE_HUMAN = 1;
	public const int RACE_ELF = 2;
	public const int RACE_ORC = 3;
	public enum Race { RACE_NONE = 0, RACE_HUMAN = 1, RACE_ELF = 2, RACE_ORC = 3 }
	#endregion
	#region Attribute
	public const int ATT_MAXIMUM_HP = 1;
	public const int ATT_ATTACK = 2;
	public const int ATT_ATTACK_RANGE = 3;
	public const int ATT_MOVE_SPEED = 4;
	public const int ATT_DODGE_CHANCE = 5;
	public const int ATT_KNOCKBACK = 6;
	public const int ATT_CRIT_CHANCE = 7;
	public const int ATT_RELOAD_TIME = 8;
	public const int ATT_MAGAZINE = 9;
	public const int ATT_FIRE_RATE = 10;
	public const int ATT_CRIT_MULTIPLIER = 11;
	public const int ATT_ACCURACY = 12;
	#endregion
	#region Gender[enum]
	public const int GENDER_NONE = 1;
	public const int GENDER_MALE = 2;
	public const int GENDER_FEMALE = 3;
	public const int GENDER_HELICOPTER = 4;
	public enum Gender { GENDER_NONE = 1, GENDER_MALE = 2, GENDER_FEMALE = 3, GENDER_HELICOPTER = 4 }
	#endregion

}
