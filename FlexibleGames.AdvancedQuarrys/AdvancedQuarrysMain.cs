using System;
using System.Reflection;
using System.IO;
using UnityEngine;


public class AdvancedQuarrysMain : FortressCraftMod
{
    public ushort mQuarryType;
    public ushort mExcavatorCubeType;
    private string XMLConfigFile = "QuarryConfig.XML";
    private string XMLMk4ConfigFile = "Mk4QuarryConfig.XML";
    private string XMLMk5ConfigFile = "Mk5QuarryConfig.XML";
    private string XMLConfigPath = "";
    private string XMLModID = "FlexibleGames.AdvancedQuarrys";
    private int XMLModVersion = 6;
    public QuarryConfig mConfig;
    public Mk4QuarryConfig mMk4Config;
    public Mk5QuarryConfig mMk5Config;
    private bool mXMLFileExists;
    private bool mMk4XMLFileExists;
    private bool mMk5XMLFileExists;
    public ushort Mk5QuarryType = ModManager.mModMappings.CubesByKey["FlexibleGames.Mk5Quarry"].CubeType;
    public ushort PlacementType = ModManager.mModMappings.CubesByKey["MachinePlacement"].CubeType;
    public ushort Mk5QuarryPlacementValue = ModManager.mModMappings.CubesByKey["MachinePlacement"].ValuesByKey["FlexibleGames.Mk5QuarryPlacement"].Value;
    // mod initializaion

    /// <summary>
    /// Register Mod
    /// </summary>
    /// <returns>ModRegistrationData for FlexibleGames.AdvancedQuarrys</returns>
    public override ModRegistrationData Register()
    {
        // load XML configurations
        //XMLConfigPath = ModManager.GetModPath();
        //string text = Path.Combine(XMLConfigPath, XMLModID + Path.DirectorySeparatorChar + XMLModVersion);
        //text += String.Concat(Path.DirectorySeparatorChar + XMLConfigFile);

        XMLConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        Debug.Log("AdvQuarrys: AssemblyPath: " + XMLConfigPath);

        string configfile = Path.Combine(XMLConfigPath, XMLConfigFile);

        Debug.Log("AdvQuarrys: Checking if XMLConfig File Exists at " + configfile);
        if (File.Exists(configfile))
        {
            mXMLFileExists = true;
            Debug.Log("AdvQuarrys: XMLConfig File Exists, loading.");
            string xmltext = File.ReadAllText(configfile);
            try
            {
                mConfig = (QuarryConfig)XMLParser.DeserializeObject(xmltext, typeof(QuarryConfig));                                
            }
            catch (Exception e)
            {
                Debug.LogError("AdvQuarrys: Something is wrong with ConfigXML, using defaults.\n Exception: " + e.ToString());
                mXMLFileExists = false;
                mConfig = new QuarryConfig();
            }
            if (mXMLFileExists) Debug.Log("AdvQuarrys: XMLConfig File Loaded.");
        }
        else
        {
            GameManager.DoLocalChat("AdvQuarrys: ERROR: XML File Does not exist at " + configfile);
            mXMLFileExists = false;
        }
        if (mXMLFileExists)
        {
            if (!ValidateConfig())
            {
                mXMLFileExists = false;                
                GameManager.DoLocalChat("AdvQuarrys: Quarry Config file is invalid.");                
            }
        }
        // load Mk4 XML configurations
        //XMLConfigPath = ModManager.GetModPath();
        //text = Path.Combine(XMLConfigPath, XMLModID + Path.DirectorySeparatorChar + XMLModVersion);
        //text += String.Concat(Path.DirectorySeparatorChar + XMLMk4ConfigFile);
        configfile = Path.Combine(XMLConfigPath, XMLMk4ConfigFile);
        Debug.Log("AdvQuarrys: Checking if Mk4 XMLConfig File Exists at " + configfile);
        if (File.Exists(configfile))
        {
            mMk4XMLFileExists = true;
            Debug.Log("AdvQuarrys: XMLConfig File Exists, loading.");
            string xmltext = File.ReadAllText(configfile);
            try
            {
                mMk4Config = (Mk4QuarryConfig)XMLParser.DeserializeObject(xmltext, typeof(Mk4QuarryConfig));
            }
            catch (Exception e)
            {
                Debug.LogError("AdvQuarrys: Something is wrong with Mk4 ConfigXML, using defaults.\n Exception: " + e.ToString());
                mMk4XMLFileExists = false;
                mMk4Config = new Mk4QuarryConfig();
            }
            if (mMk4XMLFileExists) Debug.Log("AdvQuarry: Mk4 XMLConfig File Loaded.");
        }
        else
        {
            GameManager.DoLocalChat("AdvQuarrys: ERROR: Mk4 XML File Does not exist at " + configfile);
            mMk4XMLFileExists = false;
        }
        if (mMk4XMLFileExists)
        {
            if (!ValidateMk4Config())
            {
                mMk4XMLFileExists = false;
                mMk4Config.Mine1 = "None";
                mMk4Config.Mine2 = "None";
                mMk4Config.Mine3 = "None";
                mMk4Config.Mine4 = "None";
                mMk4Config.Size = 15;
                GameManager.DoLocalChat("AdvQuarrys: Mk4 Quarry Config file is invalid.");
            }
        }

        // Mk5 quarry config.
        configfile = Path.Combine(XMLConfigPath, XMLMk5ConfigFile);
        Debug.Log("AdvQuarrys: Checking if Mk5 XMLConfig File Exists at " + configfile);
        if (File.Exists(configfile))
        {
            mMk5XMLFileExists = true;
            Debug.Log("AdvQuarrys: Mk5 XMLConfig File Exists, loading.");
            string xmltext = File.ReadAllText(configfile);
            try
            {
                mMk5Config = (Mk5QuarryConfig)XMLParser.DeserializeObject(xmltext, typeof(Mk5QuarryConfig));
            }
            catch (Exception e)
            {
                Debug.LogError("AdvQuarrys: Something is wrong with Mk5 ConfigXML, using defaults.\n Exception: " + e.ToString());
                mMk5XMLFileExists = false;
                mMk5Config = new Mk5QuarryConfig();
            }
            if (mMk5XMLFileExists) Debug.Log("AdvQuarrys: Mk5 XMLConfig File Loaded.");
        }
        else
        {
            GameManager.DoLocalChat("AdvQuarrys: ERROR: Mk5 XML File Does not exist at " + configfile);
            mMk5XMLFileExists = false;
        }
        if (mMk5XMLFileExists)
        {
            if (!ValidateMk5Config())
            {
                mMk5XMLFileExists = false;
                mMk5Config.Mine1 = "None";
                mMk5Config.Mine2 = "None";
                mMk5Config.Mine3 = "None";
                mMk5Config.Mine4 = "None";
                mMk5Config.Mine5 = "None";
                mMk5Config.Mine6 = "None";
                mMk5Config.Mine7 = "None";
                mMk5Config.Mine8 = "None";
                mMk5Config.Size = 9;
                GameManager.DoLocalChat("AdvQuarrys: Mk5 Quarry Config file is invalid.");
            }
        }


        // register mod
        ModRegistrationData lmoddata = new ModRegistrationData();
        lmoddata.RegisterEntityHandler("FlexibleGames.AdvancedQuarrys");
        lmoddata.RegisterEntityHandler("FlexibleGames.Mk5Quarry");
        lmoddata.RegisterEntityHandler("FlexibleGames.Mk5QuarryBlock");
        lmoddata.RegisterEntityHandler("FlexibleGames.Mk5QuarryCenter");
        lmoddata.RegisterEntityHandler("FlexibleGames.Mk5QuarryPlacement");

        // UIManager.NetworkCommandFunctions.Add("FlexibleGames.Mk2ExcavatorWindow", new UIManager.HandleNetworkCommand(Mk2ExcavatorWindow.HandleNetworkCommand));

        UIManager.NetworkCommandFunctions.Add("FlexibleGames.FlexibleQuarryWindow", new UIManager.HandleNetworkCommand(FlexibleQuarryWindow.HandleNetworkCommand));

        TerrainDataEntry lterraindata;
        TerrainDataValueEntry lterraindatavalue;
        global::TerrainData.GetCubeByKey("FlexibleGames.AdvancedQuarrys", out lterraindata, out lterraindatavalue);
        bool flag = lterraindata != null;
        if (flag)
        {
            this.mQuarryType = lterraindata.CubeType;
        }
        return lmoddata;
    }
    public override void CheckForCompletedMachine(ModCheckForCompletedMachineParameters parameters)
    {                         
        if (parameters.CubeValue == Mk5QuarryPlacementValue) 
        {
            Mk5Quarry.CheckForCompletedMachine(parameters.Frustrum, parameters.X, parameters.Y, parameters.Z);
        }
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults lmodcreatesetmentresults = new ModCreateSegmentEntityResults();
        bool flag = parameters.Cube == this.mQuarryType;        

        if (parameters.Cube == Mk5QuarryType || (parameters.Cube == PlacementType && parameters.Value == Mk5QuarryPlacementValue))
        {
            parameters.ObjectType = SpawnableObjectEnum.Quarry;
            lmodcreatesetmentresults.Entity = new Mk5Quarry(parameters, mMk5Config, XMLModVersion);
            return lmodcreatesetmentresults; 
        }

        if (flag)
        {
            string cubekey = global::TerrainData.GetCubeKey(parameters.Cube, parameters.Value);
            if (cubekey == "FlexibleGames.Mk4Quarry")
            {
                lmodcreatesetmentresults.Entity = new Mk4Quarry(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk, mMk4Config);
            }
            else
            {
                lmodcreatesetmentresults.Entity = new AdvancedQuarrys(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk, mConfig);
            }
        }
        return lmodcreatesetmentresults;
    }

    private bool ValidateMk5Config()
    {
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass 
        bool mine1 = false;
        bool mine2 = false;
        bool mine3 = false;
        bool mine4 = false;
        bool mine5 = false;
        bool mine6 = false;
        bool mine7 = false;
        bool mine8 = false;
        bool sizeodd = false;
        bool powervalid = false;
        switch (mMk5Config.Mine1)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine1 = true;
                break;
        }
        switch (mMk5Config.Mine2)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine2 = true;
                break;
        }
        switch (mMk5Config.Mine3)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine3 = true;
                break;
        }
        switch (mMk5Config.Mine4)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine4 = true;
                break;
        }
        switch (mMk5Config.Mine5)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine5 = true;
                break;
        }
        switch (mMk5Config.Mine6)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine6 = true;
                break;
        }
        switch (mMk5Config.Mine7)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine7 = true;
                break;
        }
        switch (mMk5Config.Mine8)
        {
            case "Garbage":
            case "AllOre":
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "OreUranium":
            case "Resin":
            case "None":
                mine8 = true;
                break;
        }
        if (mMk5Config.Size % 2 != 0)
        {
            sizeodd = true;
        }
        if (mMk5Config.PowerForGarbage >= 0 && mMk5Config.PowerForOre >= 0 && mMk5Config.PowerForResin >= 0 && mMk5Config.PowerForT4 >= 0 && mMk5Config.PowerForUranium >= 0)
            powervalid = true;
        else
            powervalid = false;

        if (mMk5Config.MaxPower > 0 && mMk5Config.MaxPower < 1000000000 && powervalid)
            powervalid = true;
        else
            powervalid = false;

        return (mine1 && mine2 && mine3 && mine4 && mine5 && mine6 && mine7 && mine8 && sizeodd && powervalid);
    }

    private bool ValidateMk4Config()
    {
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass 
        bool mine1 = false;
        bool mine2 = false;
        bool mine3 = false;
        bool mine4 = false;
        bool sizeodd = false;
        switch (mMk4Config.Mine1)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "None":
                mine1 = true;
                break;
        }
        switch (mMk4Config.Mine2)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "None":
                mine2 = true;
                break;
        }
        switch (mMk4Config.Mine3)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "None":
                mine3 = true;
                break;
        }
        switch (mMk4Config.Mine4)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "OreChromium":
            case "OreMoly":
            case "None":
                mine4 = true;
                break;
        }
        if (mMk4Config.Size % 2 != 0)
        {
            sizeodd = true;
        }
        return (mine1 && mine2 && mine3 && mine4 && sizeodd);
    }

    private bool ValidateConfig()
    {
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass
        //None
        bool ignore1valid = false;
        bool ignore2valid = false;
        bool ignore3valid = false;
        bool ignore4valid = false;
        switch (mConfig.Ignore1)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "None":
                ignore1valid = true;
                break;
        }
        switch (mConfig.Ignore2)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "None":
                ignore2valid = true;
                break;
        }
        switch (mConfig.Ignore3)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "None":
                ignore3valid = true;
                break;
        }
        switch (mConfig.Ignore4)
        {
            case "OreCoal":
            case "OreCopper":
            case "OreTin":
            case "OreIron":
            case "OreLithium":
            case "OreGold":
            case "OreNickel":
            case "OreTitanium":
            case "OreCrystal":
            case "OreBioMass":
            case "None":
                ignore4valid = true;
                break;
        }
        // if all four are valid, return true, otherwise return false
        return (ignore1valid && ignore2valid && ignore3valid && ignore4valid);
    }
}

