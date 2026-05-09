using HarmonyLib;
using NodeCanvas.Tasks.Actions;
namespace RaidMode
{
    //Implments the share blacksmith repairs option.
    [HarmonyPatch(typeof(RepairEquipmentNodeAction), "OnExecute")]
    public class RepairEquipmentNodeAction_OnExecute
    {
        public static bool Prefix()
        {
            if (RaidModeConfig.LiveSettings.ShareBlacksmithRepairs)
            {
                for (int i = 0; i < CharacterManager.Instance.PlayerCharacters.Count; i++)
                {
                    Character character = CharacterManager.Instance.GetCharacter(CharacterManager.Instance.PlayerCharacters.Values[i]);
                    if (character && character.Inventory)
                        character.Inventory.RepairEverything();
                }
            }
            return true;
        }
    }
}
