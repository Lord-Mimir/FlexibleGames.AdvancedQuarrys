using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class Mk5Quarry : MachineEntity, PowerConsumerInterface
{
    public enum DigCubeType
    {
        Garbage,
        Ore,
        Resin,
        T4,
        Uranium,
        VeryHard
    }
    public static ushort PLACEMENT_VALUE = ModManager.mModMappings.CubesByKey["MachinePlacement"].ValuesByKey["FlexibleGames.Mk5QuarryPlacement"].Value; // panel sides with something on top?
    public static ushort CUBE_TYPE = ModManager.mModMappings.CubesByKey["FlexibleGames.Mk5Quarry"].CubeType;
    public static ushort CENTER_VALUE = ModManager.mModMappings.CubesByKey["FlexibleGames.Mk5Quarry"].ValuesByKey["FlexibleGames.Mk5QuarryCenter"].Value;
    public static ushort COMPONENT_VALUE = ModManager.mModMappings.CubesByKey["FlexibleGames.Mk5Quarry"].ValuesByKey["FlexibleGames.Mk5QuarryBlock"].Value;
    public const string SHORT_NAME = "Mk5 Quarry";    
    public const SpawnableObjectEnum SPAWNABLE_OBJECT = SpawnableObjectEnum.Quarry;
    public const eSegmentEntity SEGMENT_ENTITY = eSegmentEntity.Mod;
    public int mEntityVersion;

    // These represent the dimensions of the multi-block machine, for now they must all be odd numbers.
    static int MB_X = 3;
    static int MB_Y = 3;
    static int MB_Z = 3;
    //MaxX + -MinX + 1 == MB_X

    // This is the minimum coordinate of the machine relative to the centre block.
    static int MB_MIN_X = -(MB_X / 2);//cast rounds down
    static int MB_MIN_Y = -(MB_Y / 2);
    static int MB_MIN_Z = -(MB_Z / 2);

    // This is the maximum coordinate of the machine relative to the centre block.
    static int MB_MAX_X = -MB_MIN_X;
    static int MB_MAX_Y = -MB_MIN_Y;
    static int MB_MAX_Z = -MB_MIN_Z;

    // This is the maximum possible distance from one block to any other block in the machine.
    // It is used to define the search area when attempting to build the multi-block machine.
    static int MB_OUTER_X = MB_MAX_X * 2;//2;
    static int MB_OUTER_Y = MB_MAX_Y * 2;//2;
    static int MB_OUTER_Z = MB_MAX_Z * 2;//2;

    public bool mbIsCenter;
    public Mk5Quarry mLinkedCenter; // if not center, will hold reference to center
    public MBMState mMBMState;

    public long mLinkX = 0; // coords for central lab
    public long mLinkY = 0;
    public long mLinkZ = 0;

    public bool mbLinkedToGO;

    // machine polling function
    public int mnCurrentSideIndex;
    public int mnCurrentSide;
    public bool mbHasDLC;

    public List<StorageMachineInterface> mAttachedHoppers = new List<StorageMachineInterface>(); // cached machine references TODO: replace storage hopper with inventory interface when that exists
    public List<PowerConsumerInterface> mAttachedPCI = new List<PowerConsumerInterface>();

    private int mnInventoryRoundRobinPosition;    

    public string FriendlyState = "Unknown state!";

    // Gameplay variables

    public Mk5QuarryConfig mQuarryConfig;
    public float mrCurrentPower;
    public float mrMaxPower = 4500000;
    public float mrMaxTransferRate = 5000000;//increased; the limiting factor isn't supposed to be the transfer rate, but the generation rate.
    private float mfCurrentPPS;
    float mfOreDigCostMult;
    private bool mbRotateModel;
    private Quaternion mTargetRotation = Quaternion.identity;
    public bool mbMineAllOre = false;
    public bool mbMineGarbage = false;

    /// <summary>
    /// Quarry Level 5
    /// </summary>
    ushort lsQuarryLevel = 5;
    /// <summary>
    /// Efficiency of advanced quarry (50%, 75%, 100%) NumOreRemaining > this value, decrement by this value.
    /// </summary>
    ushort lsEfficiency = 1;
    float lfHardnessLimit;

    ushort msMine1;
    ushort msMine2;
    ushort msMine3;
    ushort msMine4;
    ushort msMine5;
    ushort msMine6;
    ushort msMine7;
    ushort msMine8;
    int miTotalBlocksIgnored;
    // end my variables

    // Mk1 = 17, Mk2 = 33, Mk3 = 65
    int mnSize = 15;
    public bool mbDoDestroyIgnored = false;

    Animation mAnimation;

    ushort mReplaceType = eCubeTypes.Air;
    ushort mCarryCube;
    ushort mCarryValue;

    // Mk1 = 75, Mk2 = 150, Mk3 = 300
    // Digging ores = 4x
    float mrBaseDigCost = 50;//per cube extracted

    // no plans to change
    public float mrCurrentDigCost = 0;//Base + Depth

    // base will be based off what block is being mined.
    // garbage = 50, ore = 100, resin = 2000, T4 = 1000, Uranium = 1500
    public float mfBasePowerCost = 0;

    private int miPowerForGarbage = 50;
    private int miPowerForOre = 100;
    private int miPowerForResin = 2000;
    private int miPowerForT4 = 1000;
    private int miPowerForUranium = 1500;

    // Mk1 = 250, Mk2 = 500, Mk3 = 20k
    public int HardnessLimit = 500;

    Vector3 mUnityDronePos;
    Vector3 mUnityDrillPos;
    Vector3 mHoverOffset;

    GameObject DrillDrone;
    GameObject LaserHolder;
    GameObject Laser;
    GameObject LaserImpact;
    GameObject LaserSource;
    ParticleSystem LaserImpactDust;
    GameObject QuarryBase;
    float mrUnityDrillResetTimer;
    float mrLaserDist;

    public bool mbHealthAndSafetyChecksPassed;//we aren't going to force a recheck
    Vector3 mForwards;

    public bool mbMissingInputHopper;
    public bool mbMissingChevrons;
    public bool mbMissingOutputHopper;
    public bool mbOutputHoppersLockedOrFull;
    public bool mbBuiltTooLow;

    public float mrTotalTime;
    public float mrWorkTime;
    public float mrPowerWaitTime;
    public float mrStorageWaitTime;
    public float mrSearchWaitTime;
    float mrDigDelay;
    //	ushort mLastCube;//Unused?

    public int Depth;
    public int mnTotalOreCubes;
    public int mnTotalNonOreCubes;
    public int mnTotalOreFound;

    public int mnXSearch;
    public int mnZSearch;

    int mnLFUpdates;

    public bool mbUserHasConfirmedQuarry;

    int mnBuildShuntX;
    int mnBuildShuntZ;
    int mnHalfSize;

    int mnChevronsPlaced;

    //I half wonder if this should be generic...
    SegmentEntity[] maAttachedHoppers;
    int mnNumAttachedHoppers;
    int mnTotalHoppers;//includes invalid ones    

    public Mk5Quarry(ModCreateSegmentEntityParameters parameters, Mk5QuarryConfig QuarryConfig, int entityver)
        : base(parameters)
    {
        mQuarryConfig = QuarryConfig;        
        
        mMBMState = MBMState.WaitingForLink;
        if ((int)parameters.Value != CENTER_VALUE)
            return;
        this.mbIsCenter = true;
        
        mForwards = SegmentCustomRenderer.GetRotationQuaternion(parameters.Flags) * Vector3.forward;
        mForwards.Normalize();
        
        mMBMState = MBMState.ReacquiringLink;
        this.RequestLowFrequencyUpdates();
        this.mbNeedsUnityUpdate = true;

        this.Depth = -10;

        SetQuarryOptions(QuarryConfig);
        mnHalfSize = (mnSize + 1) / 2;

        //Debug.LogWarning("Quarry " + mForwards + ":" + loadFromDisk);

        mnBuildShuntX = (int)(mForwards.x * (mnHalfSize + 2));
        mnBuildShuntZ = (int)(mForwards.z * (mnHalfSize + 2));

    }

    public void Rotate(bool CW)
    {

        // fetch current flags
        byte rotateFlags = mFlags;

        if ((rotateFlags & CubeHelper.FACEMASK) == 0) // check for bad flags
        {
            // old shitty generation. compensate
            rotateFlags = (byte)(rotateFlags | CubeHelper.TOP);
        }

        rotateFlags = CubeHelper.RotateFlags(rotateFlags, CW); // clockwise for ']'

        //mTargetRotation = SegmentCustomRenderer.GetRotationQuaternion(rotateFlags);

        //this.mbRotateModel = true;

        WorldScript.instance.BuildRotateFromEntity(mSegment, mnX, mnY, mnZ, mCube, mValue, rotateFlags);
        mForwards = SegmentCustomRenderer.GetRotationQuaternion(rotateFlags) * Vector3.forward;
        mForwards.Normalize();
    }

    public override void UnityUpdate()
    {
        if (!mbLinkedToGO)
        {
            if (mWrapper == null || mWrapper.mbHasGameObject == false)
            {
                return;
            }
            else
            {
                mWrapper.mGameObjectList[0].transform.localScale = new Vector3(3, 3, 3);

                if (mWrapper.mGameObjectList == null) Debug.LogError("Q missing game object #0?");
                if (mWrapper.mGameObjectList[0].gameObject == null) Debug.LogError("Q missing game object #0 (GO)?");

                DrillDrone = mWrapper.mGameObjectList[0].transform.Find("DrillDrone").gameObject;

                DrillDrone.transform.position = mWrapper.mGameObjectList[0].transform.position + new Vector3(0, 50, 0);

                LaserHolder = DrillDrone.transform.Find("LaserTransfer").gameObject;
                if (LaserHolder == null) Debug.LogError("Can't find LaserHolder!");
                Laser = LaserHolder.transform.Search("Laser").gameObject;
                LaserImpact = DrillDrone.transform.Search("Laser Impact").gameObject;
                LaserImpactDust = LaserImpact.transform.Search("LaserImpactDust").gameObject.GetComponent<ParticleSystem>();

                LaserSource = LaserHolder.transform.Search("Laser Source").gameObject;

                if (Laser == null) Debug.LogError("Can't find LaserHolder!");

                mAnimation = mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();

                mbLinkedToGO = true;

                //default to off
                Laser.SetActive(false);
                LaserImpact.GetComponent<ParticleSystem>().emissionRate = 0;
                LaserImpact.GetComponent<Light>().enabled = false;
                LaserImpactDust.emissionRate = 0;
                LaserSource.SetActive(false);
                LaserImpact.GetComponent<MeshRenderer>().enabled = false;

                QuarryBase = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("SmallQuarry").gameObject;
                MeshRenderer baserenderer = QuarryBase.GetComponent<MeshRenderer>();

                if (baserenderer != null)
                {
                    baserenderer.material.SetColor("_Color", Color.white);
                }
            }
        }

        if (mbUserHasConfirmedQuarry)
        {
            //float drone above target drill square
            if (mUnityDronePos != Vector3.zero)
            {
                //remain in the air until we've done the initial clear
                if (Depth < 0) mHoverOffset = new Vector3(0, Depth, 0);

                //Drone floats very slowly, but will eventually get into place over ores, which take time to extract
                DrillDrone.transform.position += ((mUnityDronePos + mHoverOffset) - DrillDrone.transform.position) * Time.deltaTime * 0.05f;

            }


            Vector3 lDrillVec = Vector3.down;
            float lrRate = 1.0f;

            if (mUnityDrillPos != Vector3.zero)
            {
                //I'm not 100% there's not a code path that involves this not happening, hence the dumb light check
                if (mrUnityDrillResetTimer == 0.0f || LaserImpact.GetComponent<Light>().enabled == false)
                {
                    LaserImpact.GetComponent<MeshRenderer>().enabled = true;
                    LaserImpact.transform.position = mUnityDrillPos + Vector3.up;
                    LaserImpact.transform.forward = Vector3.up;


                    //LaserImpact.SetActive (true);
                    LaserImpact.GetComponent<ParticleSystem>().emissionRate = 250;
                    LaserImpact.GetComponent<Light>().enabled = true;
                    LaserImpactDust.emissionRate = 64;
                    LaserSource.SetActive(true);

                    Laser.SetActive(true);
                    Vector3 lVTT = mUnityDrillPos - Laser.transform.position;
                    mrLaserDist = lVTT.magnitude;
                    Laser.transform.localScale = new Vector3(mrLaserDist, 1, 1);

                    //	Debug.DrawRay(mWrapper.mGameObjectList[0].transform.position,mForwards * 5.0f,Color.red,1.0f);


                }

                //Drilling is active here
                Laser.transform.localScale = new Vector3(
                    mrLaserDist,
                    UnityEngine.Random.Range(50, 550) / 100.0f,
                    UnityEngine.Random.Range(50, 550) / 100.0f);

                lDrillVec = mUnityDrillPos - LaserHolder.transform.position;
                lrRate = 15.0f;//really point right at it quickly
                mrUnityDrillResetTimer += Time.deltaTime;
                //go back into 'looking down' mode when we're out of go for some reason.
                //This happens if we're totally out of power or storage
                //If we are busy on the same location, the DrillPos will not reset to zero
                if (mrUnityDrillResetTimer > 1.5f)
                {
                    mUnityDrillPos = Vector3.zero;
                    mrUnityDrillResetTimer = 0;

                    //avoid being DIRECTLY above the ore we're drilling slowly
                    mHoverOffset = UnityEngine.Random.onUnitSphere;
                    mHoverOffset.y /= 0.1f;
                    Laser.SetActive(false);

                    LaserImpact.GetComponent<ParticleSystem>().emissionRate = 0;
                    LaserImpact.GetComponent<Light>().enabled = true;
                    LaserImpactDust.emissionRate = 0;
                    //LaserImpact.SetActive (false);//fucking particles don't hang around ofc
                    LaserImpact.GetComponent<MeshRenderer>().enabled = false;

                    LaserSource.SetActive(false);
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(lDrillVec);

            // Smoothly rotate towards the target point.
            LaserHolder.transform.rotation = Quaternion.Slerp(LaserHolder.transform.rotation, targetRotation, lrRate * Time.deltaTime);


        }
        else
        {
            //place drone floating above the centre of the quarry

            Vector3 lTargetPos = mWrapper.mGameObjectList[0].transform.position;
            lTargetPos += mForwards * mnSize / 2.0f;
            lTargetPos.y += 8.0f;
            DrillDrone.transform.position += (lTargetPos - DrillDrone.transform.position) * Time.deltaTime;
        }
    }

    private bool SetQuarryOptions(Mk5QuarryConfig qconfig)
    {
        // logic for detecting which blocks to ignore. Valid Values:
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass, eCubeTypes.OreEmerald_T4_1, eCubeTypes.OreDiamond_T4_2
        //eCubeTypes.Uranium_T7, eCubeTypes.HardendResin
        //None
        switch (qconfig.Mine1)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine1 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine1 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine1 = eCubeTypes.OreTin; break;
            case "OreIron": msMine1 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine1 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine1 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine1 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine1 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine1 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine1 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine1 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine1 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine1 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine1 = eCubeTypes.HardenedResin; break;
            case "None": msMine1 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine2)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine2 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine2 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine2 = eCubeTypes.OreTin; break;
            case "OreIron": msMine2 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine2 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine2 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine2 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine2 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine2 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine2 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine2 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine2 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine2 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine2 = eCubeTypes.HardenedResin; break;
            case "None": msMine2 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine3)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine3 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine3 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine3 = eCubeTypes.OreTin; break;
            case "OreIron": msMine3 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine3 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine3 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine3 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine3 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine3 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine3 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine3 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine3 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine3 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine3 = eCubeTypes.HardenedResin; break;
            case "None": msMine3 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine4)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine4 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine4 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine4 = eCubeTypes.OreTin; break;
            case "OreIron": msMine4 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine4 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine4 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine4 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine4 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine4 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine4 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine4 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine4 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine4 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine4 = eCubeTypes.HardenedResin; break;
            case "None": msMine4 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine5)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine5 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine5 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine5 = eCubeTypes.OreTin; break;
            case "OreIron": msMine5 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine5 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine5 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine5 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine5 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine5 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine5 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine5 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine5 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine5 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine5 = eCubeTypes.HardenedResin; break;
            case "None": msMine5 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine6)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine6 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine6 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine6 = eCubeTypes.OreTin; break;
            case "OreIron": msMine6 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine6 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine6 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine6 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine6 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine6 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine6 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine6 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine6 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine6 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine6 = eCubeTypes.HardenedResin; break;
            case "None": msMine6 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine7)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine7 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine7 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine7 = eCubeTypes.OreTin; break;
            case "OreIron": msMine7 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine7 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine7 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine7 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine7 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine7 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine7 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine7 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine7 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine7 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine7 = eCubeTypes.HardenedResin; break;
            case "None": msMine7 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine8)
        {
            case "AllOre": this.mbMineAllOre = true; break;
            case "Garbage": this.mbMineGarbage = true; break;
            case "OreCoal": msMine8 = eCubeTypes.OreCoal; break;
            case "OreCopper": msMine8 = eCubeTypes.OreCopper; break;
            case "OreTin": msMine8 = eCubeTypes.OreTin; break;
            case "OreIron": msMine8 = eCubeTypes.OreIron; break;
            case "OreLithium": msMine8 = eCubeTypes.OreLithium; break;
            case "OreGold": msMine8 = eCubeTypes.OreGold; break;
            case "OreNickel": msMine8 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine8 = eCubeTypes.OreTitanium; break;
            case "OreCrystal": msMine8 = eCubeTypes.OreCrystal; break;
            case "OreBioMass": msMine8 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine8 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly": msMine8 = eCubeTypes.OreDiamond_T4_2; break;
            case "OreUranium": msMine8 = eCubeTypes.Uranium_T7; break;
            case "Resin": msMine8 = eCubeTypes.HardenedResin; break;
            case "None": msMine8 = eCubeTypes.Air; break;
        }
        this.mnSize = qconfig.Size;
        this.mrMaxPower = qconfig.MaxPower;
        this.miPowerForGarbage = qconfig.PowerForGarbage;
        this.miPowerForOre = qconfig.PowerForOre;
        this.miPowerForResin = qconfig.PowerForResin;
        this.miPowerForT4 = qconfig.PowerForT4;
        this.miPowerForUranium = qconfig.PowerForUranium;
        return true;
    }

    #region Multiblock

    // When called on the centre block this will deconstruct the entire machine into
    // placement blocks. If not all segments are available it will finish on a later
    // call to LowFrequencyUpdate.
    void DeconstructMachineFromCentre(Mk5Quarry deletedBlock)
    {
        Debug.LogWarning("Deconstructing " + SHORT_NAME + " into placement blocks");

        // Replace the multi-block machine pieces with placement blocks.
        for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
        {
            for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
            {
                for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                {
                    long tx = mnX + x, ty = mnY + y, tz = mnZ + z;

                    if (x == 0 && y == 0 && z == 0)
                        continue; // dont check self

                    if (deletedBlock != null && (tx == deletedBlock.mnX && ty == deletedBlock.mnY && tz == deletedBlock.mnZ))
                        continue; // don't bother with block that just got deleted

                    Segment targetSegment = WorldScript.instance.GetSegment(tx, ty, tz);

                    if (targetSegment == null || !targetSegment.mbInitialGenerationComplete || targetSegment.mbDestroyed)
                    {
                        // Could not find one of the segments, we'll handle the rest of the delinking in low frequency update.
                        mMBMState = MBMState.Delinking;
                        RequestLowFrequencyUpdates();
                        return;
                    }

                    ushort lType = targetSegment.GetCube(tx, ty, tz);

                    if (lType == CUBE_TYPE)
                    {
                        // fetch entity
                        Mk5Quarry mbmEntity = targetSegment.FetchEntity(SEGMENT_ENTITY, tx, ty, tz) as Mk5Quarry;

                        if (mbmEntity == null)
                        {
                            Debug.LogWarning("Failed to refind a " + SHORT_NAME + " entity? wut?");
                            continue; // whoops. more shit that will remain
                        }

                        // order them to deconstruct, which will make them replace themselves with placement blocks
                        mbmEntity.DeconstructSingleBlock();
                    }
                }
            }
        }

        // Finally delete this block (which should be the centre)
        if (this != deletedBlock)
            DeconstructSingleBlock();
        
    }

    // This will deconstruct a single block of the multi-block machine, changing it back to a machine placement block.
    public void DeconstructSingleBlock()
    {
        //Debug.LogWarning("Deconstructing block");
        mMBMState = MBMState.Delinked;
        WorldScript.instance.BuildFromEntity(mSegment, mnX, mnY, mnZ, eCubeTypes.MachinePlacementBlock, PLACEMENT_VALUE);
    }

    // This will link the entire multi-block machine together and must be called on the centre.
    public void LinkMultiBlockMachine()
    {
        // Loop through every block in the machine to link it up. This should only ever be called for the Centre block.
        for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
        {
            for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
            {
                for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                {
                    long tx = mnX + x, ty = mnY + y, tz = mnZ + z;

                    if (x == 0 && y == 0 && z == 0)
                        continue; // Don't check the centre block.

                    Segment targetSegment = AttemptGetSegment(tx, ty, tz);

                    if (targetSegment == null)
                        return; // abort, try again later

                    // Make sure the current type is in use.
                    ushort lType = targetSegment.GetCube(tx, ty, tz);

                    if (lType == CUBE_TYPE)
                    {
                        // fetch entity
                        Mk5Quarry mbmEntity = targetSegment.FetchEntity(SEGMENT_ENTITY, tx, ty, tz) as Mk5Quarry;

                        if (mbmEntity == null)
                        {
                            return; // abort. found MBM not spawned in yet.
                        }

                        if (mbmEntity.mMBMState == MBMState.Linked && mbmEntity.mLinkedCenter == this)
                            continue; // we already connected this one previously

                        if (mbmEntity.mMBMState != MBMState.ReacquiringLink || mbmEntity.mLinkX != mnX || mbmEntity.mLinkY != mnY || mbmEntity.mLinkZ != mnZ)
                        {
                            // DERP.
                            //							Debug.Log("Overwriting badly formed lab?");
                        }

                        mbmEntity.mMBMState = MBMState.Linked;
                        mbmEntity.AttachToCentreBlock(this);
                        //sidelab.mLinkedCenter = this;
                        //						sidelab.mSegment.RequestRegenerateGraphics();
                    }
                    else
                    {
                        // more wtf. this MBM is all sorts of broken.
                        //	Debug.LogWarning("Found badly formed " + SHORT_NAME + "?");

                        // just ignore for now, if player deletes a different block this will overwrite with placement.
                    }
                }
            }
        }

        // if we got here, we found all sides. (ergo we are the centre?)
        //Debug.Log(SHORT_NAME + " successfully linked to all sides!");
        ContructionFinished();

        DropExtraSegments(null); // we don't need to keep the sides loaded if we're done.
    }

    // Must be called on the centre block.
    void ContructionFinished()
    {
        FriendlyState = SHORT_NAME + " Constructed!";
        mMBMState = MBMState.Linked;
        mSegment.RequestRegenerateGraphics();
        MarkDirtyDelayed();
    }

    void AttachToCentreBlock(Mk5Quarry centerBlock)
    {
        if (centerBlock == null) Debug.LogError("Error, can't set side - requested centre is null!");

        mMBMState = MBMState.Linked;

        if (mLinkX != centerBlock.mnX) // if it doesn't match, save and regen graphics, because it may just have been built
        {
            MarkDirtyDelayed();
            mSegment.RequestRegenerateGraphics(); // should not be necessary, but it is?
        }

        mLinkedCenter = centerBlock;
        mLinkX = centerBlock.mnX;
        mLinkY = centerBlock.mnY;
        mLinkZ = centerBlock.mnZ;
    }


    static int GetExtents(int x, int y, int z, long lastX, long lastY, long lastZ, WorldFrustrum frustrum)
    {
        long TestX = lastX;
        long TestY = lastY;
        long TestZ = lastZ;

        int lnSpan = 0;

        //Debug.Log("Getting Extents " + x+":" + z);

        for (int Move = 0; Move < 100; Move++)//YOU MAY NOT HAVE LAUNCH PADS MORE THAN 100 METRES BIG 
        {
            TestX += x;
            TestY += y;
            TestZ += z;
            if (IsCubeThisMachine(TestX, TestY, TestZ, frustrum))
                lnSpan++;
            else
                break;

        }
        return lnSpan;
    }

    //******************** ******************** **********************
    static bool IsCubeThisMachine(long checkX, long checkY, long checkZ, WorldFrustrum frustrum)
    {
        Segment targetSegment = frustrum.GetSegment(checkX, checkY, checkZ);

        if (targetSegment == null || !targetSegment.mbInitialGenerationComplete || targetSegment.mbDestroyed)
            return false; // abort.

        ushort lCube = targetSegment.GetCube(checkX, checkY, checkZ);

        if (lCube != eCubeTypes.MachinePlacementBlock) return false;
        ushort lData = targetSegment.GetCubeData(checkX, checkY, checkZ).mValue;
        if (lData != PLACEMENT_VALUE) return false;


        return true;//correct type and correct value
    }

    // structure detection code
    //SO YES THIS WAS RETARDED AND IS TAKING OVER A SECOND TO COMPLETE NOW
    public static void CheckForCompletedMachine(WorldFrustrum frustrum, long lastX, long lastY, long lastZ)
    {
        //SANITY CHECK
        if ((-MB_MIN_X + MB_MAX_X + 1) != MB_X) Debug.LogError("Error, X is configured wrongly");
        if ((-MB_MIN_Y + MB_MAX_Y + 1) != MB_Y) Debug.LogError("Error, Y is configured wrongly");
        if ((-MB_MIN_Z + MB_MAX_Z + 1) != MB_Z) Debug.LogError("Error, Z is configured wrongly");

        //A potentially better method; scan along -=XYZ until we hit the edges; this gives us the size along our cross-section
        //If all things within that area are of the right type, we're valid
        //The inverse (?) distance along these 3 axis gives us our offset into the machine

        int lnXSpan = GetExtents(-1, 0, 0, lastX, lastY, lastZ, frustrum);
        lnXSpan += GetExtents(1, 0, 0, lastX, lastY, lastZ, frustrum);
        lnXSpan++;
        if (MB_X > lnXSpan) { Debug.LogWarning(SHORT_NAME + " isn't big enough along X(" + lnXSpan + ")"); return; }
        if (MB_X > lnXSpan) { return; }

        int lnYSpan = GetExtents(0, -1, 0, lastX, lastY, lastZ, frustrum);
        lnYSpan += GetExtents(0, 1, 0, lastX, lastY, lastZ, frustrum);
        lnYSpan++;
        if (MB_Y > lnYSpan) { Debug.LogWarning(SHORT_NAME + " isn't big enough along Y(" + lnYSpan + ")"); return; }
        if (MB_Y > lnYSpan) { return; }

        int lnZSpan = GetExtents(0, 0, -1, lastX, lastY, lastZ, frustrum);
        lnZSpan += GetExtents(0, 0, 1, lastX, lastY, lastZ, frustrum);
        lnZSpan++;

        if (MB_Z > lnZSpan) { Debug.LogWarning(SHORT_NAME + " isn't big enough along Z(" + lnZSpan + ")"); return; }
        if (MB_Z > lnZSpan) { return; }

        Debug.LogWarning(SHORT_NAME + " is detecting test span of " + lnXSpan + ":" + lnYSpan + ":" + lnZSpan);

        //If XYZ Spans do NOT match the size of our machine, there is absolutely no way we can continue
        //The next step will be to check every square within that
        //and then if I can work it out, we can then calc our offset and skip all this code beneath


        //For the moment, the above stuff is actually pretty good (oddly enough, the check below went from 1000+ms to 85ms, unsure why)
        //It's VERY HARD to build a block that hits all the previous spans that isn't valid




        // check neighbouring placement blocks. if we run into unusable segment, just abort.

        bool[, ,] possibleConfigs = new bool[MB_X, MB_Y, MB_Z];
        for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
        {
            for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
            {
                for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                {
                    possibleConfigs[x + MB_MAX_X, y + MB_MAX_Y, z + MB_MAX_Z] = true;
                }
            }
        }

        // can we have a valid configuration at this offset from our position?
        //This check scans as if we're every possible block, including all corners
        for (int y = -MB_OUTER_Y; y <= MB_OUTER_Y; y++)
        {
            for (int z = -MB_OUTER_Z; z <= MB_OUTER_Z; z++)
            {
                for (int x = -MB_OUTER_X; x <= MB_OUTER_X; x++)
                {

                    if (x == 0 && y == 0 && z == 0)
                        continue; // dont check self

                    Segment targetSegment = frustrum.GetSegment(lastX + x, lastY + y, lastZ + z);

                    if (targetSegment == null || !targetSegment.mbInitialGenerationComplete || targetSegment.mbDestroyed)
                        return; // abort.

                    ushort lType = targetSegment.GetCube(lastX + x, lastY + y, lastZ + z);

                    bool validBlock = false;

                    if (lType == eCubeTypes.MachinePlacementBlock)
                    {
                        ushort lValue = targetSegment.GetCubeData(lastX + x, lastY + y, lastZ + z).mValue;

                        if (lValue == PLACEMENT_VALUE)
                        {
                            validBlock = true;
                        }
                    }

                    if (!validBlock)
                    {
                        // deny possible configs

                        // this denies any block in a 3x3x3 area around it from being a valid MBM center.
                        for (int cy = MB_MIN_Y; cy <= MB_MAX_Y; cy++)
                        {
                            for (int cz = MB_MIN_Z; cz <= MB_MAX_Z; cz++)
                            {
                                for (int cx = MB_MIN_X; cx <= MB_MAX_X; cx++)
                                {
                                    int tx = x + cx;
                                    int ty = y + cy;
                                    int tz = z + cz;

                                    if (tx < MB_MIN_X || tx > MB_MAX_X ||
                                        ty < MB_MIN_Y || ty > MB_MAX_Y ||
                                        tz < MB_MIN_Z || tz > MB_MAX_Z)
                                        continue; // only store the ones that match our possible configs

                                    possibleConfigs[tx + MB_MAX_X, ty + MB_MAX_Y, tz + MB_MAX_Z] = false;//centered in bool array
                                }
                            }
                        }
                    }
                }
            }
        }

        // possibleConfigs now contains all valid MBM configurations.

        // given that we've already replaced the placement blocks, that should be only one.
        //DEBUG
        int validPositions = 0;
        for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
        {
            for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
            {
                for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                {

                    if (possibleConfigs[x + MB_MAX_X, y + MB_MAX_Y, z + MB_MAX_Z])
                    {
                        validPositions++;
                    }
                }
            }
        }
        //This is a result of skipping the 'mark things as can't be centers' above, but is about 100 times fucking faster
        if (validPositions > 1)
        {
            Debug.LogWarning("Warning, OE has too many valid positions (" + validPositions + ")");
            return;
        }
        if (validPositions == 0)
        {
            return;//sad times
        }
        //Debug.Log("Found " + validPositions + " valid MBM positions");

        //what the fuck, we then ignore that above caculated var and do this shit anyways?
        //I guess that starts to pull in the segments in advance of the player building...?
        // pick the first one as valid.
        for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
        {
            for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
            {
                for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                {

                    if (possibleConfigs[x + MB_MAX_X, y + MB_MAX_Y, z + MB_MAX_Z])
                    {
                        if (BuildMultiBlockMachine(frustrum, lastX + x, lastY + y, lastZ + z))
                        {
                            //	Debug.Log("Built MBM successfully!");
                            return; // DONE!
                        }
                        else
                        {
                            Debug.LogError("Error, failed to build " + SHORT_NAME + " due to bad segment?");
                        }

                    }
                }
            }
        }
        if (validPositions != 0)
        {

            Debug.LogError("Error, thought we found a valid position, but failed to build the " + SHORT_NAME + "?");
        }
    }

    public static bool BuildMultiBlockMachine(WorldFrustrum frustrum, long centerX, long centerY, long centerZ)
    {
        HashSet<Segment> laSegments = new HashSet<Segment>();

        // now link up the other labs

        bool lbSucceeded = true;

        try
        {
            WorldScript.mLocalPlayer.mResearch.GiveResearch(CUBE_TYPE, 0); // scanning this would be silly

            for (int y = MB_MIN_Y; y <= MB_MAX_Y; y++)
            {
                for (int z = MB_MIN_Z; z <= MB_MAX_Z; z++)
                {
                    for (int x = MB_MIN_X; x <= MB_MAX_X; x++)
                    {
                        Segment targetSegment = frustrum.GetSegment(centerX + x, centerY + y, centerZ + z);

                        if (targetSegment == null || !targetSegment.mbInitialGenerationComplete || targetSegment.mbDestroyed)
                        {
                            lbSucceeded = false;
                            continue;
                        }

                        if (!laSegments.Contains(targetSegment))
                        {
                            laSegments.Add(targetSegment);
                            targetSegment.BeginProcessing();//do not process
                        }

                        // construct the multiblock machine
                        if (x == 0 && y == 0 && z == 0)
                            frustrum.BuildOrientation(targetSegment, centerX + x, centerY + y, centerZ + z, CUBE_TYPE, CENTER_VALUE, CubeHelper.mDefaultOrientation);//centre
                        else
                            frustrum.BuildOrientation(targetSegment, centerX + x, centerY + y, centerZ + z, CUBE_TYPE, COMPONENT_VALUE, CubeHelper.mDefaultOrientation);

                        //	Debug.Log("Built machine ok!");

                    }
                }
            }
        }
        finally
        {
            foreach (Segment l in laSegments)
            {
                l.EndProcessing();
            }

            WorldScript.instance.mNodeWorkerThread.KickNodeWorkerThread();
        }

        if (lbSucceeded == false)
        {
            Debug.LogError("Error, failed to build " + SHORT_NAME + " as one of it's segments wasn't valid!");
        }
        else
        {
            AudioSpeechManager.PlayStructureCompleteDelayed = true;
        }
        return lbSucceeded;
    }
    public override void OnDelete()
    {
        base.OnDelete();
        //GameManager.DoLocalChat("Deconstructing Mk5 Quarry, State = " + mMBMState);

        if (mMBMState == MBMState.Linked) // an unlinked lab being deleted means that the block was replaced with a lab, probably.
        {
            // if not in a player segment when deleted, you're out of luck!
            //if (mSegment.mbInLocalFrustrum && NetworkManager.mbHostingServer)
            if (WorldScript.mbIsServer)
            {
                ItemManager.DropNewCubeStack(eCubeTypes.MachinePlacementBlock, PLACEMENT_VALUE, 1, mnX, mnY, mnZ, Vector3.zero);

            }

            mMBMState = MBMState.Delinking;

            if (mbIsCenter)
                this.DeconstructMachineFromCentre(this);
            else
                mLinkedCenter.DeconstructMachineFromCentre(this);            
        }
        else if (mMBMState != MBMState.Delinked)
        {
            Debug.LogWarning("Deleted Quarry while in state " + mMBMState);
        }
    }

    public override void SpawnGameObject()
    {
        this.mObjectType = SPAWNABLE_OBJECT;
        if (!this.mbIsCenter)
            return;
        base.SpawnGameObject();
    }

    void AcquireSides()
    {
        // locate our previously linked sides

        for (int y = -1; y <= 1; y++)
        {
            for (int z = -1; z <= 1; z++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    long tx = mnX + x, ty = mnY + y, tz = mnZ + z;

                    if (x == 0 && y == 0 && z == 0)
                        continue; // dont check self

                    Segment targetSegment = AttemptGetSegment(tx, ty, tz);

                    if (targetSegment == null)
                        return; // abort, try again later

                    ushort lType = targetSegment.GetCube(tx, ty, tz);

                    if (lType == CUBE_TYPE)
                    {
                        // fetch entity
                        Mk5Quarry sidelab = targetSegment.FetchEntity(eSegmentEntity.Mod, tx, ty, tz) as Mk5Quarry;

                        if (sidelab == null)
                        {
                            return; // abort. found lab not spawned in yet.
                        }

                        if (sidelab.mMBMState == MBMState.Linked && sidelab.mLinkedCenter == this)
                            continue; // we already connected this one previously

                        if (sidelab.mMBMState != MBMState.ReacquiringLink || sidelab.mLinkX != mnX || sidelab.mLinkY != mnY || sidelab.mLinkZ != mnZ)
                        {
                            // DERP.
                            //							Debug.Log("Overwriting badly formed lab?");
                        }

                        sidelab.mMBMState = MBMState.Linked;
                        sidelab.AttachToCentreBlock(this);
                    }
                }
            }
        }

        // if we got here, we found all sides.
        Debug.Log("Quarry linked!");
        mMBMState = MBMState.Linked;

        DropExtraSegments(null); // we don't need to keep the sides loaded if we're done.
    }

    public override void LowFrequencyUpdate()
    {
        if (!mbIsCenter) return;
        else
        {            
            mbNeedsUnityUpdate = true;

            if (mMBMState == MBMState.ReacquiringLink)
            {
                //AcquireSides();
                LinkMultiBlockMachine();
            }
            if (mMBMState == MBMState.Delinking)
            {
                // Attempt to deconstruct the multi-block machine.
                DeconstructMachineFromCentre(null);
            }
            // *************
            if (WorldScript.mbIsServer)
            {
                this.LookForMachines();
            }
        }

        float lfTimeStep = LowFrequencyThread.mrPreviousUpdateTimeStep / 4;
        int liFourMax = 0;

        if (mbHealthAndSafetyChecksPassed == false)
        {
            mnLFUpdates++;
            if (!DoHealthAndSafetyCheck())//if for any reason this fails, set LF Updates back to zero.
            {
                //Debug.LogWarning ("H&S check failed");
                mnLFUpdates = 0;
            }
            MarkDirtyDelayed();
            //Debug.LogWarning ("H&S check succeeded!");
            return;

        }
        else
        {
            mbMissingInputHopper = false;
            mbMissingChevrons = false;
        }
        if (mbUserHasConfirmedQuarry == false)
        {
            mbMissingInputHopper = false;
            mbMissingChevrons = false;
            return;

        }

        mrTotalTime += LowFrequencyThread.mrPreviousUpdateTimeStep;

        // now, do we have enough power left over to dig the cheapest thing?
        if (!HasEnoughPower(Depth, DigCubeType.Garbage))
        {
            mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
            return;
        }

        mnLFUpdates++;
        if (mrDigDelay > 0.0f)
        {
            mrDigDelay -= lfTimeStep;
            return;
        }

        //If we have an item, attempt to offload it now
        if (!AttemptToOffloadCargo())
        {
            mrStorageWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
            return;
        }
        //Now iterate until we get another cube, run out of power, or fail somehow.

        bool cargodroppedoff = false;
        bool otherError = false;
        //we search up to 16 non-ore blocks until we get something to dig
        for (int i = 0; i < 16; i++)
        {

            if (!HasEnoughPower(Depth, DigCubeType.Garbage)) // do we have enough power for another block of garbage?
            {
                mrPowerWaitTime += lfTimeStep / 4;
                return;
            }
            //avoid recalcing this for every block #minoroptimisationj
            //only we have to recalc it cuz the X and Z searches change.
            int lnXMod = (mnXSearch - (mnHalfSize - 1)) + mnBuildShuntX;
            int lnZMod = (mnZSearch - (mnHalfSize - 1)) + mnBuildShuntZ;

            long lx = mnX + lnXMod;
            long lz = mnZ + lnZMod;
            long ly = mnY - Depth;

            if (!AttemptToDig(lx, ly, lz, out otherError))
            {
                if (otherError) return;
                liFourMax++;
                cargodroppedoff = AttemptToOffloadCargo();
                mUnityDrillPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(lx, ly, lz) + new Vector3(0.5f, 0.5f, 0.5f);//CENTRE of the cube

                mrWorkTime += lfTimeStep;


                if (!cargodroppedoff)
                {
                    // we need to do four blocks, but we don't have the storage for the one we have now... 
                    mrStorageWaitTime += lfTimeStep;
                    mbOutputHoppersLockedOrFull = true;
                    return;
                }
                else
                    mbOutputHoppersLockedOrFull = false;

                if (liFourMax > 3)
                {
                    return;
                }
            }
        }
        //still searching (prolly started on an existing shaft?
        mrSearchWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
        //Once complete, move to next area, waiting on segment if necessary        
    }

    bool DoHealthAndSafetyCheck()
    {
        // height check
        if (mnY - WorldScript.mDefaultOffset < -900)
        {
            mbBuiltTooLow = true;
        }
        // needed variable checks here

        //draw around the quarry area; failure involves coming back next frame
        //todo, check hopper for resources

        mbHealthAndSafetyChecksPassed = false;

        mbMissingInputHopper = true;
        LookForMachines();
        mnNumAttachedHoppers = mAttachedHoppers.Count;
        //UpdateAttachedHoppers(false);
        if (mnNumAttachedHoppers == 0)
        {
            mbMissingInputHopper = true;
            return false;
        }

        //if we can get a chevron, do so now
        bool lbGotChevron = false;
        for (int i = 0; i < mnNumAttachedHoppers; i++)
        {
            StorageMachineInterface storageMachineInterface = this.mAttachedHoppers[i] as StorageMachineInterface;
            if (storageMachineInterface != null && storageMachineInterface.TryExtractCubes(this, 124, global::TerrainData.GetDefaultValue(124), 1))
            {
                this.mbMissingChevrons = false;
                lbGotChevron = true;
                break;
            }
        }
        if (lbGotChevron == false)
        {
            mbMissingInputHopper = false;
            mbMissingChevrons = true;

            return false;
        }
        //Else, build the next part of the H&S barrier
        //if no hopper mnLFUPdates==0 return;
        //shunt the build mod over by the forwards
        //Shunt one more because we're describing the perimeter
        mnBuildShuntX = (int)(mForwards.x * (mnHalfSize + 2));
        mnBuildShuntZ = (int)(mForwards.z * (mnHalfSize + 2));

        //now build the H&S barrier, except on us

        int lnChevronIndex = 0;//just run the build process below until we get to this index. Cheap and hacky but works.
        //Debug.LogWarning ("Building Chevron Index " + mnChevronsPlaced);

        for (int x = 0; x < mnSize + 2; x++)
            for (int z = 0; z < mnSize + 2; z++)
            {
                long lx = x + mnX + mnBuildShuntX - (mnHalfSize);//the +1 centralises it
                long ly = mnY - 2; // -2 for multiblock
                long lz = z + mnZ + mnBuildShuntZ - (mnHalfSize);

                //	if (lx == mnX && lz == mnZ) continue;//dont build over us!

                if (x == 0 || z == 0 || x == mnSize + 1 || z == mnSize + 1)
                {
                    //If we have a chevron
                    if (lnChevronIndex == mnChevronsPlaced)
                    {

                        if (BuildChevron(lx, ly, lz, eCubeTypes.ChevronRedWhite))
                        {
                            if (lx != mnX && lz != mnZ) //dont build over us!
                            {
                                BuildChevron(lx, ly + 1, lz, eCubeTypes.Air);//don't care if this fails

                            }

                            mbMissingInputHopper = false;
                            mnLFUpdates = 0;
                            mnChevronsPlaced++;
                            return false;
                        }
                        else
                        {
                            //segment not loaded, etc
                            mnLFUpdates = 0;
                            return false;
                        }
                    }
                    //if so, remove a chevron
                    lnChevronIndex++;
                }

            }

        //		Debug.LogWarning("H&S check complete!");
        mbHealthAndSafetyChecksPassed = true;
        return true; ;

    }
    // *********************************************************************************************************
    bool BuildChevron(long checkX, long checkY, long checkZ, ushort lType)
    {
        Segment checkSegment = null;

        if (mFrustrum != null)
        {
            checkSegment = AttemptGetSegment(checkX, checkY, checkZ);

            if (checkSegment == null)
                return false;
        }
        else
        {
            // we don't have a frustrum :(
            checkSegment = WorldScript.instance.GetSegment(checkX, checkY, checkZ);

            if (checkSegment == null || !checkSegment.mbInitialGenerationComplete || checkSegment.mbDestroyed)
            {
                // postpone doing this again by quite a while, low chance that we'll be available soon
                return false;
            }
        }

        //todo, check this isn't a machine!
        ushort lcube = checkSegment.GetCube(checkX, checkY, checkZ);
        if (!CubeHelper.IsMachine(lcube))
        {
            WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, lType, TerrainData.GetDefaultValue(lType));
        }
        return true;

    }
    // *********************************************************************************************************
    //we do this only if we have cleared a cube for some reason.
    void MoveToNextCube()
    {
        mnXSearch++;
        if (mnXSearch >= mnSize)
        {
            mnXSearch = 0;
            mnZSearch++;
            if (mnZSearch >= mnSize)
            {
                mnZSearch = 0;
                Depth++;
                MarkDirtyDelayed();

            }
        }

        if (WorldScript.mbHasPlayer)//dedicated server cares not for graphics
        {

            int lnXMod = (mnXSearch - (mnHalfSize - 1)) + mnBuildShuntX;
            int lnZMod = (mnZSearch - (mnHalfSize - 1)) + mnBuildShuntZ;

            long lx = mnX + lnXMod;
            long lz = mnZ + lnZMod;
            long ly = mnY + 4;//our hover height

            mUnityDronePos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(lx, ly, lz) + new Vector3(0.5f, 0.5f, 0.5f);//CENTRE of the cube
        }
        //Debug.LogWarning("XS:" + mnXSearch + ".ZS:" + mnZSearch);
    }

    // ************************************************************************************************************
    bool AttemptToOffloadCargo()
    {
        if (mCarryCube == eCubeTypes.NULL) return true;//sure. Why not.

        LookForMachines();
        //UpdateAttachedHoppers(true);

        mbMissingOutputHopper = false;
        mbOutputHoppersLockedOrFull = false;

        if (mAttachedHoppers.Count == 0)//this includes invalid ones
        {
            mbMissingOutputHopper = true;
            mbOutputHoppersLockedOrFull = false;//or at least unknown
            return false;
        }

        if (mAttachedHoppers.Count > 0)
        {
            StorageMachineInterface hopper = mAttachedHoppers[mnInventoryRoundRobinPosition % mAttachedHoppers.Count];

            if (hopper == null || ((SegmentEntity)hopper).mbDelete)
            {
                mAttachedHoppers.RemoveAt(mnInventoryRoundRobinPosition % mAttachedHoppers.Count);
            }
            else
            {
                eHopperPermissions permissions = hopper.GetPermissions();

                if (permissions == eHopperPermissions.AddAndRemove ||
                    permissions == eHopperPermissions.AddOnly)
                {
                    if (hopper.TryInsert(this, this.mCarryCube, mCarryValue, 1))
                    {
                        mCarryCube = eCubeTypes.NULL;                        
                        this.RequestImmediateNetworkUpdate();                        
                    }
                }
            }
            mnInventoryRoundRobinPosition++;
        }
        if (mCarryCube != eCubeTypes.NULL)
        {            
            return false;
        }
        else
            return true;        
    }
    // ************************************************************************************************************

    bool HasEnoughPower(int lDepth, DigCubeType ldigcubetype)
    {
        switch (ldigcubetype)
        {
            case DigCubeType.Garbage: mrBaseDigCost = miPowerForGarbage; break;
            case DigCubeType.Ore: mrBaseDigCost = miPowerForOre; break;
            case DigCubeType.Resin: mrBaseDigCost = miPowerForResin; break;
            case DigCubeType.T4: mrBaseDigCost = miPowerForT4; break;
            case DigCubeType.Uranium: mrBaseDigCost = miPowerForUranium; break;
            case DigCubeType.VeryHard: mrBaseDigCost = miPowerForT4; break;
        }
        float lCurrentDigCost = mrBaseDigCost;
        if (lDepth > 0) lCurrentDigCost += lDepth;
        if (mrCurrentPower > lCurrentDigCost)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CanQuarry(ushort lCube, ushort lValue)
    {
        //if (lCube == eCubeTypes.MagmaFluid) return false;
        if (lCube == eCubeTypes.HiveSpawn) return false;
        //if (lCube == eCubeTypes.HardenedResin) return false;
        //if (lCube == eCubeTypes.AblatedResin) return false;
        if (lCube == 90 || lCube == 91) // T4 ores
        {

            if (DLCOwnership.HasT4() || DLCOwnership.HasPatreon())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
       // if (lCube == 92 || lCube == 93 || lCube == 94) // T5-T6 ores
       // {
       //     return false;
       // }
        if (lCube == eCubeTypes.CentralPowerHub)
            return false;
       // if (TerrainData.GetHardness(lCube, lValue) > this.lfHardnessLimit) return false;

        return true;
    }
    //ALMOST never return true
    //False == we did not simply remove the cube
    //True = cave/air only
    bool AttemptToDig(long checkX, long checkY, long checkZ, out bool otherError)
    {
        otherError = false;
        mCarryCube = eCubeTypes.NULL;
        Segment checkSegment = null;
        if (mFrustrum != null)
        {
            checkSegment = AttemptGetSegment(checkX, checkY, checkZ);

            if (checkSegment == null)
            {
                otherError = true;
                return false;
            }
        }
        else
        {
            // we don't have a frustrum :(
            checkSegment = WorldScript.instance.GetSegment(checkX, checkY, checkZ);

            if (checkSegment == null || !checkSegment.mbInitialGenerationComplete || checkSegment.mbDestroyed)
            {
                // postpone doing this again by quite a while, low chance that we'll be available soon
                mrDigDelay = 1.0f;
                otherError = true;
                return false;
            }
        }

        ushort lCube = checkSegment.GetCube(checkX, checkY, checkZ);

        if (lCube == mReplaceType)
        {
            MoveToNextCube();
            return true;
        }
        CubeData lData = checkSegment.GetCubeData(checkX, checkY, checkZ);

        // quarryable checks
        bool lbCannotQuarry = false;
        lbCannotQuarry = !CanQuarry(lCube, lData.mValue);

        //unsure on behaviour; this might just skip the magma, but continue to bore underneath it.
        if (lbCannotQuarry)
        {
            MoveToNextCube();
            otherError = true;
            return false;//true? 
        }

        // so, cube is something we can mine, but what is it? Ore? Garbage? Resin?
        DigCubeType curCubeType = DigCubeType.VeryHard;

        if (CubeHelper.IsGarbage(lCube))
            curCubeType = DigCubeType.Garbage;

        else if (CubeHelper.IsOre(lCube))
        {
            curCubeType = DigCubeType.Ore;
            if (lCube == 90 || lCube == 91)
            {
                curCubeType = DigCubeType.T4;
            }
        }
        else if (lCube == eCubeTypes.HardenedResin || lCube == eCubeTypes.AblatedResin)
            curCubeType = DigCubeType.Resin;

        else if (lCube == eCubeTypes.Uranium_T7)
            curCubeType = DigCubeType.Uranium;

        // lets check garbage first, as there's bound to be more of it.
        if (curCubeType == DigCubeType.Garbage || curCubeType == DigCubeType.VeryHard)
        {
            // Destroy this block, it's garbage
            if (!HasEnoughPower(Depth, curCubeType))
            {
                mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
                otherError = true;
                return false;
            }
            // Make sure we have any adjacent segments required loaded in.
            if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
            {
                otherError = true;
                return false;
            }

            WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
            if (mbMineGarbage && curCubeType == DigCubeType.Garbage)
            {
                mCarryCube = lCube;
            }
            mnTotalNonOreCubes++;
            MoveToNextCube();
            lCube = eCubeTypes.NULL;
            mrCurrentDigCost = (this.mrBaseDigCost + Depth);
            mrCurrentPower -= mrCurrentDigCost;
            return true;        
        }
        else if (curCubeType == DigCubeType.Ore || curCubeType == DigCubeType.T4 || curCubeType == DigCubeType.Uranium)
        {
            bool lbDoMineAll = false;
            if (curCubeType == DigCubeType.Ore && mbMineAllOre)
            {
                lbDoMineAll = true;
            }
            // we got this far, do we have enough power to progress?
            if (!HasEnoughPower(Depth, curCubeType))
            {
                // not enough power to dig this ore, return until we do.
                mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
                otherError = true;
                return false;
            }
            // check our mine types            
            if (!lbDoMineAll || (lCube != msMine1 && lCube != msMine2 && lCube != msMine3 && lCube != msMine4 && lCube != msMine5 && lCube != msMine6 && lCube != msMine7 && lCube != msMine8))
            {
                this.miTotalBlocksIgnored++;
                // check DoDestroy...
                if (mbDoDestroyIgnored)
                {
                    if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
                    {
                        otherError = true;
                        return false;
                    }

                    WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
                    MoveToNextCube();
                    lCube = eCubeTypes.NULL;
                    mrCurrentDigCost = (this.mrBaseDigCost + Depth);
                    mrCurrentPower -= mrCurrentDigCost;
                    return true;
                }
                else
                {
                    MoveToNextCube();
                    return true;
                }
            }

            ushort lnOreLeft = lData.mValue;

            // oh.. this simply won't do
            //10% efficiency (variable efficiency)
            if (lnOreLeft > this.lsEfficiency) // 10)
            {
                //	Debug.Log("Decrementing ore from " + lnOreLeft);
                lnOreLeft -= this.lsEfficiency;// 10;
                checkSegment.SetCubeValueNoChecking((int)(checkX % WorldHelper.SegmentX), (int)(checkY % WorldHelper.SegmentY), (int)(checkZ % WorldHelper.SegmentZ), lnOreLeft);
                checkSegment.RequestDelayedSave();
                mCarryCube = lCube;
                mrCurrentDigCost = (this.mrBaseDigCost + Depth);
                mrCurrentPower -= mrCurrentDigCost;
            }
            else
            {
                // Out of ore, remove block.
                if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
                {
                    otherError = true;
                    return false;
                }

                mnTotalOreCubes++;
                WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
                MoveToNextCube();
                mrCurrentDigCost = (this.mrBaseDigCost + Depth);
                mrCurrentPower -= mrCurrentDigCost;
            }

            mnTotalOreFound++; //take into account efficiency?
        }
        else if (curCubeType == DigCubeType.Resin)
        {
            // we encountered resin! Do we have enough power?

            if (!HasEnoughPower(Depth, curCubeType))
            {
                // not enough power to dig this ore, return until we do.
                mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
                otherError = true;
                return false;
            }

            // power is good... do we want it?
            if (lCube != msMine1 && lCube != msMine2 && lCube != msMine3 && lCube != msMine4 && lCube != msMine5 && lCube != msMine6 && lCube != msMine7 && lCube != msMine8)
            {
                // we don't want it, do we destroy it?
                if (mbDoDestroyIgnored)
                {
                    if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
                    {
                        otherError = true;
                        return false;
                    }

                    WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
                    MoveToNextCube();
                    lCube = eCubeTypes.NULL;
                    mrCurrentDigCost = (this.mrBaseDigCost + Depth);
                    mrCurrentPower -= mrCurrentDigCost;
                    return true;
                }
                else
                {
                    MoveToNextCube();
                    return true;
                }
            }
            else
            {
                // gimme!
                if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
                {
                    otherError = true;
                    return false;
                }

                mnTotalOreCubes++;
                WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
                MoveToNextCube();

                mCarryCube = eCubeTypes.AblatedResin;
                mCarryValue = 2;
                return true;
            }
        }
        mCarryCube = lCube;

        // check if we should use given value or the default - this works for ore as well!
        TerrainDataEntry lEntry = TerrainData.mEntries[mCarryCube];
        if (lEntry == null)
        {
            if (lCube != eCubeTypes.AblatedResin)
                mCarryValue = lData.mValue;
        }
        else
        {
            if (lEntry.GetValue(lData.mValue) != null)
            {
                if (lCube != eCubeTypes.AblatedResin)
                    mCarryValue = lData.mValue;
            }
            else
            {
                // no specific entry for this value, this was a colorised block or an ore. store as default value
                if (lCube != eCubeTypes.AblatedResin)
                    mCarryValue = lEntry.DefaultValue;
            }
        }
        return false; // return true means we handled this cube.
    }

    private bool LoadAdjacentSegmentsIfRequired(Segment checkSegment, long checkX, long checkY, long checkZ)
    {
        // If we're about to dig something out on the edge of a segment then we need to also
        // load the adjacent segment so that the faces can be regenerated correctly.
        int sx = (int)(checkX - checkSegment.baseX);
        int sy = (int)(checkY - checkSegment.baseY);
        int sz = (int)(checkZ - checkSegment.baseZ);

        if (sx == 0)
        {
            if (AttemptGetSegment(checkX - 1, checkY, checkZ) == null)
                return false;
        }

        if (sx == 15)
        {
            if (AttemptGetSegment(checkX + 1, checkY, checkZ) == null)
                return false;
        }

        if (sy == 0)
        {
            if (AttemptGetSegment(checkX, checkY - 1, checkZ) == null)
                return false;
        }

        if (sy == 15)
        {
            if (AttemptGetSegment(checkX, checkY + 1, checkZ) == null)
                return false;
        }

        if (sz == 0)
        {
            if (AttemptGetSegment(checkX, checkY, checkZ - 1) == null)
                return false;
        }

        if (sz == 15)
        {
            if (AttemptGetSegment(checkX, checkY, checkZ + 1) == null)
                return false;
        }

        return true;
    }

    void Deconstruct()
    {
        mMBMState = MBMState.Delinked;
        WorldScript.instance.BuildFromEntity(mSegment, mnX, mnY, mnZ, eCubeTypes.MachinePlacementBlock, PLACEMENT_VALUE);
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        mbLinkedToGO = false;
        DrillDrone = null;
        LaserHolder = null;
        Laser = null;
        LaserImpact = null;
        LaserImpactDust = null;
        LaserSource = null;
    }

    void RoundRobinSide(out int y, out int x, out int z)
    {
        int sideArea = 9;

        if (mnCurrentSide == 0)
        {
            // left (from center looking at intake)
            y = (mnCurrentSideIndex / MB_Z) + MB_MIN_Y;
            x = MB_MIN_X - 1;
            z = (mnCurrentSideIndex % MB_Z) + MB_MIN_Z;
            sideArea = MB_Y * MB_Z;
        }
        else if (mnCurrentSide == 1)
        {
            // right
            y = (mnCurrentSideIndex / MB_Z) + MB_MIN_Y;
            x = MB_MAX_X + 1;
            z = (mnCurrentSideIndex % MB_Z) + MB_MIN_Z;
            sideArea = MB_Y * MB_Z;
        }
        else if (mnCurrentSide == 2)
        {
            // front
            y = (mnCurrentSideIndex / MB_X) + MB_MIN_Y;
            x = (mnCurrentSideIndex % MB_X) + MB_MIN_X;
            z = MB_MAX_Z + 1;
            sideArea = MB_Y * MB_X;
        }
        else if (mnCurrentSide == 3)
        {
            // back
            y = (mnCurrentSideIndex / MB_X) + MB_MIN_Y;
            x = (mnCurrentSideIndex % MB_X) + MB_MIN_X;
            z = MB_MIN_Z - 1;
            sideArea = MB_Y * MB_X;
        }
        else if (mnCurrentSide == 4)
        {
            // bottom
            y = MB_MIN_Y - 1;
            x = (mnCurrentSideIndex / MB_Z) + MB_MIN_X;
            z = (mnCurrentSideIndex % MB_Z) + MB_MIN_Z;
            sideArea = MB_X * MB_Z;
        }
        else
        {
            // top
            y = MB_MAX_Y + 1;
            x = (mnCurrentSideIndex / MB_Z) + MB_MIN_X;
            z = (mnCurrentSideIndex % MB_Z) + MB_MIN_Z;
            sideArea = MB_X * MB_Z;
        }

        mnCurrentSideIndex++;
        if (mnCurrentSideIndex == sideArea)
        {
            mnCurrentSideIndex = 0;
            mnCurrentSide = (mnCurrentSide + 1) % 6;
        }
    }

    #endregion Multiblock
    private void LookForMachines()
    {
        int num;
        int num2;
        int num3;
        this.RoundRobinSide(out num, out num2, out num3);

        long x = (long)num2 + this.mnX;
        long y = (long)num + this.mnY;
        long z = (long)num3 + this.mnZ;
        Segment segment = base.AttemptGetSegment(x, y, z);
        if (segment == null)
        {
            return;
        }
        ushort cube = segment.GetCube(x, y, z);
        if (cube == 1)
        {
            return;
        }
        SegmentEntity segmentEntity = segment.SearchEntity(x, y, z);
        if (segmentEntity is StorageMachineInterface)
        {
            if (((StorageMachineInterface)segmentEntity).IsNotFull())
            {
                this.AddAttachedHopper((StorageMachineInterface)segmentEntity);
            }
        }
    }


    void AddAttachedHopper(StorageMachineInterface storageMachine)
    {
        // check if any of the existing machines match this hopper
        for (int i = 0; i < mAttachedHoppers.Count; i++)
        {
            StorageMachineInterface existingHopper = mAttachedHoppers[i];

            if (existingHopper != null) // should be always
            {
                if ((existingHopper as SegmentEntity).mbDelete) // machine was deleted
                {
                    mAttachedHoppers.RemoveAt(i);
                    i--;
                    continue;
                }

                if (existingHopper == storageMachine)
                {
                    // already in the list.
                    storageMachine = null; // null it, so we won't add it below
                }
            }
        }

        if (storageMachine != null)
        {
            mAttachedHoppers.Add(storageMachine);

            //	Debug.Log ("Trencher Motor now has " + mAttachedHoppers.Count + " attached hoppers");
        }
    }


    StorageMachineInterface FetchRoundRobinInventory()
    {
        if (mAttachedHoppers.Count == 0)
            return null;

        if (mnInventoryRoundRobinPosition >= mAttachedHoppers.Count)
            mnInventoryRoundRobinPosition = 0;

        // round robin over hopper list
        return mAttachedHoppers[mnInventoryRoundRobinPosition++];
    }

    void RequestLowFrequencyUpdates()
    {
        if (!mbNeedsLowFrequencyUpdate)
        {
            mbNeedsLowFrequencyUpdate = true;
            mSegment.mbNeedsLowFrequencyUpdate = true;
            if (!mSegment.mbIsQueuedForUpdate)
            {
                WorldScript.instance.mSegmentUpdater.AddSegment(mSegment);
            }
        }
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override void Write(System.IO.BinaryWriter writer)
    {
        long startPos = writer.BaseStream.Position;

        writer.Write(mbIsCenter);
        writer.Write(mLinkX);
        writer.Write(mLinkY);
        writer.Write(mLinkZ);

        if (!mbIsCenter)
            return;

        // version
        writer.Write(mEntityVersion);

        // power
        writer.Write(mrCurrentPower);
        // other stuff
        writer.Write(Depth);
        writer.Write(mnTotalNonOreCubes);
        writer.Write(mnTotalOreCubes);
        writer.Write(mnTotalOreFound);
        writer.Write(mrCurrentPower);
        writer.Write(mnSize);

        writer.Write(mbUserHasConfirmedQuarry); // bools
        writer.Write(mbHealthAndSafetyChecksPassed);
        writer.Write(mbDoDestroyIgnored);

        int blank = 0;
        writer.Write(blank);
        writer.Write(blank);
        writer.Write(blank);
        writer.Write(blank);
        writer.Write(blank);

    }

    // ************************************************************************************************************************************************
    public override void Read(System.IO.BinaryReader reader, int entityVersion)
    {
        long startPos = reader.BaseStream.Position;

        mbIsCenter = reader.ReadBoolean();
        mLinkX = reader.ReadInt64();
        mLinkY = reader.ReadInt64();
        mLinkZ = reader.ReadInt64();

        if (!mbIsCenter)
        {
            mMBMState = MBMState.ReacquiringLink;
            return;
        }
        mEntityVersion = reader.ReadInt32(); // version, very important for any future updates!
        
        mrCurrentPower = reader.ReadSingle();
        Depth = reader.ReadInt32();
        mnTotalNonOreCubes = reader.ReadInt32();
        mnTotalOreCubes = reader.ReadInt32();
        mnTotalOreFound = reader.ReadInt32();
        mrCurrentPower = reader.ReadSingle();
        mnSize = reader.ReadInt32();

        // bools
        mbUserHasConfirmedQuarry = reader.ReadBoolean();
        mbHealthAndSafetyChecksPassed = reader.ReadBoolean();
        mbDoDestroyIgnored = reader.ReadBoolean();

        int blank;
        blank = reader.ReadInt32();
        blank = reader.ReadInt32();
        blank = reader.ReadInt32();
        blank = reader.ReadInt32();
        blank = reader.ReadInt32();
    }

    public override int GetVersion()
    {
        return mEntityVersion;
    }

    //******************** PowerConsumerInterface **********************	
    public float GetRemainingPowerCapacity()
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.GetRemainingPowerCapacity();

        return mrMaxPower - mrCurrentPower;
    }

    public float GetMaximumDeliveryRate()
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.GetMaximumDeliveryRate();

        return mrMaxTransferRate;
    }

    public float GetMaxPower()
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.GetMaxPower();

        return mrMaxPower;
    }

    public bool DeliverPower(float amount)//to what? O.o
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.DeliverPower(amount);

        if (amount > GetRemainingPowerCapacity())
            return false;

        mrCurrentPower += amount;
        MarkDirtyDelayed();
        return true;
    }

    public bool WantsPowerFromEntity(SegmentEntity entity)
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.WantsPowerFromEntity(entity);

        return true;
    }	

    // ******************* Network Syncing *************************
    public override bool ShouldNetworkUpdate()
    {
        return true;
    }
    //******************** Holobase **********************

    /// <summary>
    /// Called when the holobase has been opened and it requires this entity to add its
    /// visualisations. If there is no visualisation for an entity return null.
    /// 
    /// To receive updates each frame set the <see cref="HoloMachineEntity.RequiresUpdates"/> flag.
    /// </summary>
    /// <returns>The holobase entity visualisation.</returns>
    /// <param name="holobase">Holobase.</param>
    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        var creationParameters = new HolobaseEntityCreationParameters(this);

        if (mbIsCenter)
        {
            var primaryVisualisation = creationParameters.AddVisualisation(holobase.mPreviewCube);
            primaryVisualisation.Scale = new Vector3(3.0f, 3.0f, 3.0f);
            primaryVisualisation.Color = Color.yellow;
        }
        else
        {
            return null;
        }

        return holobase.CreateHolobaseEntity(creationParameters);
    }

    public int GetCubeType(string key)
    {
        ModCubeMap modCubeMap = null;
        ModManager.mModMappings.CubesByKey.TryGetValue(key, out modCubeMap);
        bool flag = modCubeMap != null;
        int result;
        if (flag)
        {
            result = (int)modCubeMap.CubeType;
        }
        else
        {
            result = 0;
        }
        return result;
    }

    public void ResetStats()
    {
        mrPowerWaitTime = 0f;
        mrStorageWaitTime = 0f;
        mrWorkTime = 0f;
        mrTotalTime = 0f;
        mnTotalNonOreCubes = 0;
        mnTotalOreCubes = 0;
        mnTotalOreFound = 0;
        Depth = 0;
    }

    public void ConfirmQuarry()
    {
        mbUserHasConfirmedQuarry = true;
        //Shunt one further so we're on the outside

        MarkDirtyDelayed();
        RequestImmediateNetworkUpdate();
    }

    public override string GetPopupText()
    {
        if (mLinkedCenter != null)
            return mLinkedCenter.GetPopupText();        
        
        string popuptext = "";
        if (Input.GetKeyDown(KeyCode.R) && !mbHealthAndSafetyChecksPassed && UIManager.AllowInteracting)
        {
            FlexibleQuarryWindow.RotateMk5(this, 5);
            //Rotate(true);
        }
        popuptext = "Quarry Mk5 aka Big Bertha";
        if (mbBuiltTooLow)
        {
            popuptext = "Quarry is unable to draw sufficient O2 for cooling!";
            popuptext += "\nPlease move the Quarry closer to the surface.";
            popuptext += "\nThank you!";
            return popuptext;
        }
        if (mbMissingInputHopper)
        {
            popuptext = "Quarry cannot locate Input Hopper";
            popuptext += "\nPlease attach a Storage Hopper";
            popuptext += "\nThis should contain Red and White Chevrons";
            return popuptext;
        }
        if (mbMissingChevrons)
        {
            popuptext = "Quarry cannot locate Health and Safety Resources!";
            popuptext += "\nQuarry needs more Red and White Chevrons";
            popuptext += "\nPlace these in an attached Storage Hopper";
            popuptext += "\nRotate (R) BEFORE supplying Chevrons!";
            return popuptext;
        }
        if (!mbHealthAndSafetyChecksPassed)
        {
            popuptext = "Quarry currently constructing Edge Warning";
            popuptext += "\nSAFETY FIRST!";
            return popuptext;
        }
        if (mbMissingOutputHopper)
        {
            popuptext = "Quarry cannot find Output Hopper";
            return popuptext;
        }
        //if (mbOutputHoppersLockedOrFull)
        //{
            //popuptext = "All output hoppers full or locked!";
            //return popuptext;
        //}
        if (!mbUserHasConfirmedQuarry)
        {
            popuptext = "Press E to confirm Quarry position\n";
            popuptext += "Press Q to Destroy Ignored Ore: " + (mbDoDestroyIgnored ? "Yes" : "No");
            popuptext += "\nOnce set, it cannot be reset.";
            if (Input.GetKeyDown(KeyCode.E) && UIManager.AllowInteracting)
            {
                FlexibleQuarryWindow.ConfirmMk5(this, 5);
                //this.ConfirmQuarry(); // QuarryWindow.Confirm(quarry);
            }
            if (Input.GetKeyDown(KeyCode.Q) && UIManager.AllowInteracting)
            {
                FlexibleQuarryWindow.Mk5DestroyOre(this, 5);
                //mbDoDestroyIgnored = !mbDoDestroyIgnored;
            }
            return popuptext;
        }
        string text2;
        if (mrCurrentPower < mrCurrentDigCost)
        {
            popuptext = string.Concat(new string[]
			{
				"Power Low (",
				mrCurrentDigCost.ToString("F0"),
				" needed to dig, ",
				mrCurrentPower.ToString("F0"),
				" available)"
			});
        }
        else
        {
            text2 = popuptext;
            popuptext = string.Concat(new string[]
			{
				text2,
				"\nPower ",
				mrCurrentPower.ToString("F0"),
				"/",
				mrMaxPower.ToString("F0")
			});
        }
        text2 = popuptext;
        popuptext = string.Concat(new object[]
		{
			text2,
			"\nAt Depth ",
			Depth,
			"m ", 
            "Size: ",
            mnSize
		});
        text2 = popuptext;
        popuptext = string.Concat(new string[]
		{
			text2,
			"\nFound ",
			string.Format("{0:n0}", mnTotalOreFound),
			" in ",
			string.Format("{0:n0}", mnTotalOreCubes),
			" ore blocks"
		});
        popuptext = popuptext + "\nFound " + string.Format("{0:n0}", mnTotalNonOreCubes) + " non-ore blocks";
                
        popuptext += "\nIgnored " + (mbDoDestroyIgnored ? " and Destroyed " : "") + string.Format("{0:n0}", miTotalBlocksIgnored) + " ore blocks.";
                
        float num = mrWorkTime / mrTotalTime;
        float num2 = mrStorageWaitTime + mrPowerWaitTime + mrSearchWaitTime;
        popuptext = popuptext + "\nWork Ratio " + num.ToString("P1");
        popuptext = popuptext + "\nTotal Idle Time : " + num2.ToString("F0");
        popuptext += "(E to reset)";
        if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting)
        {
            FlexibleQuarryWindow.ResetMk5(this, 5);
        }
            
        
        return popuptext;
    }
}

