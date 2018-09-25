using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

class FlexibleQuarryWindow : BaseMachineWindow
{
    public const string InterfaceName = "FlexibleGames.FlexibleQuarryWindow";

    public const string InterfaceConfirmMk123 = "ConfirmMk123";
    public const string InterfaceResetMk123 = "ResetMk123";

    public const string InterfaceConfirmMk4 = "ConfirmMk4";
    public const string InterfaceMk4DestroyOre = "Mk4DestroyOre";
    public const string InterfaceResetMk4 = "ResetMk4";

    public const string InterfaceConfirmMk5 = "ConfirmMk5";
    public const string InterfaceMk5DestroyOre = "Mk5DestroyOre";
    public const string InterfaceResetMk5 = "ResetMk5";
    public const string InterfaceRotateMk5 = "RotateMk5";

    public static bool ConfirmMk123(AdvancedQuarrys machine, int data)
    {
        // do stuff
        machine.ConfirmQuarry();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ConfirmMk123", data.ToString(), null, machine, 0.0f);

        return true;
    }
    public static bool ResetMk123(AdvancedQuarrys machine, int data)
    {
        // do stuff
        machine.ResetStats();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ResetMk123", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool ConfirmMk4(Mk4Quarry machine, int data)
    {
        // do stuff
        machine.ConfirmQuarry();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ConfirmMk4", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool Mk4DestroyOre(Mk4Quarry machine, int data)
    {
        // do stuff
        machine.mbDoDestroyIgnored = !machine.mbDoDestroyIgnored;
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "Mk4DestroyOre", data.ToString(), null, machine, 0.0f);

        return true;
    }
    public static bool ResetMk4(Mk4Quarry machine, int data)
    {
        // do stuff
        machine.ResetStats();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ResetMk4", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool ConfirmMk5(Mk5Quarry machine, int data)
    {
        // do stuff
        machine.ConfirmQuarry();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ConfirmMk5", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static bool Mk5DestroyOre(Mk5Quarry machine, int data)
    {
        // do stuff
        machine.mbDoDestroyIgnored = !machine.mbDoDestroyIgnored;
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "Mk5DestroyOre", data.ToString(), null, machine, 0.0f);

        return true;
    }
    public static bool ResetMk5(Mk5Quarry machine, int data)
    {
        // do stuff
        machine.ResetStats();
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "ResetMk5", data.ToString(), null, machine, 0.0f);

        return true;
    }
    public static bool RotateMk5(Mk5Quarry machine, int data)
    {
        // do stuff
        machine.Rotate(true);
        machine.MarkDirtyDelayed();

        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand("FlexibleGames.FlexibleQuarryWindow", "RotateMk5", data.ToString(), null, machine, 0.0f);

        return true;
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        int quarrylevel;
        int.TryParse(nic.payload ?? "1", out quarrylevel);
        
        AdvancedQuarrys lMk123Quarry = null;
        Mk4Quarry lMk4Quarry = null;
        Mk5Quarry lMk5Quarry = null;
        SegmentEntity machine = null;

        switch (quarrylevel)
        {
            case 123: lMk123Quarry = nic.target as AdvancedQuarrys;
                machine = (SegmentEntity)lMk123Quarry;
                break;
            case 4: lMk4Quarry = nic.target as Mk4Quarry;
                machine = (SegmentEntity)lMk4Quarry;
                break;
            case 5: lMk5Quarry = nic.target as Mk5Quarry;
                machine = (SegmentEntity)lMk5Quarry;
                break;            
        }
        
        string key = nic.command;
        if (key != null)
        {            
            if (key == "ConfirmMk123")
            {
                FlexibleQuarryWindow.ConfirmMk123(lMk123Quarry, quarrylevel);
            }
            else if (key == "ResetMk123")
            {
                FlexibleQuarryWindow.ResetMk123(lMk123Quarry, quarrylevel);
            }
            else if (key == "ConfirmMk4")
            {
                FlexibleQuarryWindow.ConfirmMk4(lMk4Quarry, quarrylevel);
            }
            else if (key == "Mk4DestroyOre")
            {
                FlexibleQuarryWindow.Mk4DestroyOre(lMk4Quarry, quarrylevel);
            }
            else if (key == "ResetMk4")
            {
                FlexibleQuarryWindow.ResetMk4(lMk4Quarry, quarrylevel);
            }
            else if (key == "ConfirmMk5")
            {
                FlexibleQuarryWindow.ConfirmMk5(lMk5Quarry, quarrylevel);
            }
            else if (key == "Mk5DestroyOre")
            {
                FlexibleQuarryWindow.Mk5DestroyOre(lMk5Quarry, quarrylevel);
            }
            else if (key == "ResetMk5")
            {
                FlexibleQuarryWindow.ResetMk5(lMk5Quarry, quarrylevel);
            }
            else if (key == "RotateMk5")
            {
                FlexibleQuarryWindow.RotateMk5(lMk5Quarry, quarrylevel);
            }
        }
        return new NetworkInterfaceResponse()
        {
            entity = (SegmentEntity)machine,
            inventory = player.mInventory
        };
    }
}

