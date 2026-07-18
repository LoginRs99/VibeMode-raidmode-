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
            if (!RaidModeConfig.LiveSettings.ShareBlacksmithRepairs)
                return true;
            if (CharacterManager.Instance == null)
                return true;
            if (PhotonNetwork.inRoom && !PhotonNetwork.isMasterClient)
                return true;

            foreach (string uid in CharacterManager.Instance.PlayerCharacters.Values)
            {
                Character character = CharacterManager.Instance.GetCharacter(uid);
                if (character && character.Inventory)
                    character.Inventory.RepairEverything();
            }

            return false;
        }
    }
}
