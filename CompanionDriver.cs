using System;
using System.Drawing;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;

public class CompanionDriver : Script
{
    private MenuPool menuPool;
    private UIMenu mainMenu;
    private Ped companion;
    private Vehicle playerVehicle;
    private bool companionExists = false;

    public CompanionDriver()
    {
        Tick += OnTick;
        KeyDown += OnKeyDown;

        InitializeMenu();
    }

    private void InitializeMenu()
    {
        menuPool = new MenuPool();
        mainMenu = new UIMenu("Companion Driver", "~b~Control Your NPC Driver");
        menuPool.Add(mainMenu);

        // Spawn Companion
        var spawnItem = new UIMenuItem("Spawn Companion Driver", "Spawns an NPC companion and makes him drive your vehicle");
        mainMenu.AddItem(spawnItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == spawnItem)
            {
                SpawnCompanion();
            }
        };

        // Remove Companion
        var removeItem = new UIMenuItem("Remove Companion", "Removes the current companion");
        mainMenu.AddItem(removeItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == removeItem)
            {
                RemoveCompanion();
            }
        };

        // Drive to Waypoint
        var waypointItem = new UIMenuItem("Drive to Waypoint", "Companion will drive to your map waypoint");
        mainMenu.AddItem(waypointItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == waypointItem)
            {
                DriveToWaypoint();
            }
        };

        // Wander Around
        var wanderItem = new UIMenuItem("Wander Around", "Companion will drive around randomly");
        mainMenu.AddItem(wanderItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == wanderItem)
            {
                WanderAround();
            }
        };

        // Stop Driving
        var stopItem = new UIMenuItem("Stop Vehicle", "Companion will stop the vehicle");
        mainMenu.AddItem(stopItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == stopItem)
            {
                StopDriving();
            }
        };

        // Driving Style Submenu
        var drivingStyleMenu = menuPool.AddSubMenu(mainMenu, "Driving Style");
        drivingStyleMenu.MenuItems.Add(new UIMenuItem("Normal", "Normal driving style"));
        drivingStyleMenu.MenuItems.Add(new UIMenuItem("Rushed", "Fast and aggressive driving"));
        drivingStyleMenu.MenuItems.Add(new UIMenuItem("Careful", "Slow and careful driving"));
        drivingStyleMenu.MenuItems.Add(new UIMenuItem("Reckless", "Ignore traffic rules completely"));

        drivingStyleMenu.OnItemSelect += (sender, item, index) =>
        {
            SetDrivingStyle(index);
        };

        // Companion Status
        var statusItem = new UIMenuItem("Companion Status", companionExists ? "~g~Active" : "~r~Inactive");
        mainMenu.AddItem(statusItem);
        mainMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == statusItem)
            {
                ShowStatus();
            }
        };

        mainMenu.RefreshIndex();
    }

    private void SpawnCompanion()
    {
        if (companionExists && companion != null && companion.Exists())
        {
            UI.Notify("~r~Companion already exists!");
            return;
        }

        playerVehicle = Game.Player.Character.CurrentVehicle;

        if (playerVehicle == null)
        {
            UI.Notify("~r~You must be in a vehicle!");
            return;
        }

        // Get a random ped model
        Model[] pedModels = new Model[]
        {
            PedHash.Business01AMY,
            PedHash.Business02AMY,
            PedHash.ChiGoon01GMY,
            PedHash.FibSec01SMM,
            PedHash.Hipster01AMY
        };

        Model pedModel = pedModels[new Random().Next(pedModels.Length)];
        pedModel.Request(5000);

        Vector3 spawnPos = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 3;
        companion = World.CreatePed(pedModel, spawnPos);

        if (companion != null && companion.Exists())
        {
            companion.Task.WarpIntoVehicle(playerVehicle, VehicleSeat.Driver);
            companion.AlwaysKeepTask = true;
            companion.BlockPermanentEvents = true;
            companion.CanRagdoll = false;

            // Set relationship to player
            companion.RelationshipGroup = Game.Player.Character.RelationshipGroup;

            companionExists = true;

            // Make player move to passenger seat
            Game.Player.Character.Task.WarpIntoVehicle(playerVehicle, VehicleSeat.RightFront);

            UI.Notify("~g~Companion driver spawned!");
        }
        else
        {
            UI.Notify("~r~Failed to spawn companion!");
        }

        pedModel.MarkAsNoLongerNeeded();
    }

    private void RemoveCompanion()
    {
        if (!companionExists || companion == null || !companion.Exists())
        {
            UI.Notify("~r~No companion to remove!");
            return;
        }

        companion.Task.LeaveVehicle();
        Wait(1000);
        companion.Delete();
        companionExists = false;

        UI.Notify("~g~Companion removed!");
    }

    private void DriveToWaypoint()
    {
        if (!companionExists || companion == null || !companion.Exists())
        {
            UI.Notify("~r~No companion driver!");
            return;
        }

        if (!Function.Call<bool>(Hash.IS_WAYPOINT_ACTIVE))
        {
            UI.Notify("~r~No waypoint set!");
            return;
        }

        Vector3 waypointPos = World.GetWaypointPosition();

        if (companion.IsInVehicle())
        {
            companion.Task.DriveTo(companion.CurrentVehicle, waypointPos, 5f, 25f, (int)DrivingStyle.Normal);
            UI.Notify("~g~Driving to waypoint!");
        }
        else
        {
            UI.Notify("~r~Companion is not in a vehicle!");
        }
    }

    private void WanderAround()
    {
        if (!companionExists || companion == null || !companion.Exists())
        {
            UI.Notify("~r~No companion driver!");
            return;
        }

        if (companion.IsInVehicle())
        {
            companion.Task.CruiseWithVehicle(companion.CurrentVehicle, 20f, (int)DrivingStyle.Normal);
            UI.Notify("~g~Companion is wandering around!");
        }
        else
        {
            UI.Notify("~r~Companion is not in a vehicle!");
        }
    }

    private void StopDriving()
    {
        if (!companionExists || companion == null || !companion.Exists())
        {
            UI.Notify("~r~No companion driver!");
            return;
        }

        companion.Task.ClearAll();

        if (companion.IsInVehicle())
        {
            companion.CurrentVehicle.HandbrakeOn = true;
        }

        UI.Notify("~g~Companion stopped!");
    }

    private void SetDrivingStyle(int styleIndex)
    {
        if (!companionExists || companion == null || !companion.Exists())
        {
            UI.Notify("~r~No companion driver!");
            return;
        }

        DrivingStyle style;
        string styleName;

        switch (styleIndex)
        {
            case 0:
                style = DrivingStyle.Normal;
                styleName = "Normal";
                break;
            case 1:
                style = DrivingStyle.Rushed;
                styleName = "Rushed";
                break;
            case 2:
                style = DrivingStyle.AvoidTrafficExtremely;
                styleName = "Careful";
                break;
            case 3:
                style = DrivingStyle.IgnoreLights | DrivingStyle.IgnorePathing;
                styleName = "Reckless";
                break;
            default:
                style = DrivingStyle.Normal;
                styleName = "Normal";
                break;
        }

        Function.Call(Hash.SET_DRIVE_TASK_DRIVING_STYLE, companion, (int)style);
        UI.Notify($"~g~Driving style: {styleName}");
    }

    private void ShowStatus()
    {
        if (companionExists && companion != null && companion.Exists())
        {
            string vehicleStatus = companion.IsInVehicle() ? "In Vehicle" : "On Foot";
            string healthStatus = $"{companion.Health}/{companion.MaxHealth}";

            UI.Notify($"~b~Status: ~g~Active~n~~b~Location: ~w~{vehicleStatus}~n~~b~Health: ~w~{healthStatus}");
        }
        else
        {
            UI.Notify("~r~No active companion!");
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        menuPool.ProcessMenus();

        // Check if companion still exists
        if (companionExists && (companion == null || !companion.Exists() || companion.IsDead))
        {
            companionExists = false;
            UI.Notify("~r~Companion has been lost!");
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // F8 to toggle menu
        if (e.KeyCode == Keys.F8)
        {
            mainMenu.Visible = !mainMenu.Visible;
        }
    }
}
