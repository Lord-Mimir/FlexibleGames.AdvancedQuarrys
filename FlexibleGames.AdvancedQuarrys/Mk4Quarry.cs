using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


class Mk4Quarry : MachineEntity, PowerConsumerInterface
{
   // my variables
    /// <summary>
    /// Internal CubeValue
    /// </summary>
    ushort CubeValue;
    /// <summary>
    /// When diggings ores, multiply base dig cost with this
    /// </summary>
    float mfOreDigCostMult;
    /// <summary>
    /// Quarry Level 4
    /// </summary>
    ushort lsQuarryLevel = 4;
    /// <summary>
    /// Efficiency of advanced quarry (50%, 75%, 100%) NumOreRemaining > this value, decrement by this value.
    /// </summary>
    ushort lsEfficiency;
    float lfHardnessLimit;

    ushort msMine1;
    ushort msMine2;
    ushort msMine3;
    ushort msMine4;
    int miTotalBlocksIgnored;
    // end my variables

    // Mk1 = 17, Mk2 = 33, Mk3 = 65
	int mnSize = 15;
    public bool mbDoDestroyIgnored = false;


	//int mnDepthScanned;

	bool mbLinkedToGO;
	
	Animation mAnimation;

	ushort mReplaceType = eCubeTypes.Air;
	ushort mCarryCube;
	ushort mCarryValue;

    // Mk1 = 75, Mk2 = 150, Mk3 = 300
    // Digging ores = 4x
	float mrBaseDigCost = 75;//per cube extracted

    // no plans to change
	public float mrCurrentDigCost = 0;//Base + Depth
    public float mfBasePowerCost = 1000;

    // Mk1 = 250, Mk2 = 500, Mk3 = 20k
	public int HardnessLimit = 500;//T2 only
    // excludes hives, resin, Uranium, and SuperHardRock


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

    //******************** PowerConsumerInterface **********************
    public float mrCurrentPower;
    // oh, yeah, this will change
    public float mrMaxPower;

    public float mrMaxTransferRate = 1024; // ??

	// **************************************************************************************************************************d**********************
	public Mk4Quarry(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue, bool loadFromDisk, Mk4QuarryConfig qConfig) : base(eSegmentEntity.Quarry, SpawnableObjectEnum.Quarry, x, y, z, cube, flags, lValue, Vector3.zero, segment)
	{

        // which quarry am I?
        string cubeKey = global::TerrainData.GetCubeKey(cube, lValue);
        bool isMk4 = cubeKey == "FlexibleGames.Mk4Quarry";
        if (isMk4)
        {       
            //Yep, Mk4 specific stuff
            this.CubeValue = 3;
            this.lsQuarryLevel = 4;
            this.mrBaseDigCost = 75;
            this.mfOreDigCostMult = 1f;
            this.lsEfficiency = 1; // 100% efficiency
            this.mnSize = qConfig.Size;
            this.mrMaxPower = 4500000;
            this.mrMaxTransferRate = 10000;
            this.lfHardnessLimit = 500;
            this.Depth = -5;
            SetQuarryOptions(qConfig);
            this.miTotalBlocksIgnored = 0;
        }

		mbNeedsLowFrequencyUpdate = true;
		mbNeedsUnityUpdate = true;			

		mForwards = SegmentCustomRenderer.GetRotationQuaternion(flags) * Vector3.forward;
		mForwards.Normalize();

		maAttachedHoppers = new StorageHopper[6];//yay for references

		mnHalfSize = (mnSize+1) /2;

		//Debug.LogWarning("Quarry " + mForwards + ":" + loadFromDisk);

		mnBuildShuntX = (int)(mForwards.x * (mnHalfSize+1));
		mnBuildShuntZ = (int)(mForwards.z * (mnHalfSize+1));		
	}

    private bool SetQuarryOptions(Mk4QuarryConfig qconfig)
    {
        // logic for detecting which blocks to ignore. Valid Values:
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass
        //None
        switch (qconfig.Mine1)
        {
            case "OreCoal":     msMine1 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msMine1 = eCubeTypes.OreCopper; break;
            case "OreTin":      msMine1 = eCubeTypes.OreTin; break;
            case "OreIron":     msMine1 = eCubeTypes.OreIron; break;
            case "OreLithium":  msMine1 = eCubeTypes.OreLithium; break;
            case "OreGold":     msMine1 = eCubeTypes.OreGold; break;
            case "OreNickel":   msMine1 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine1 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msMine1 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msMine1 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine1 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly":     msMine1 = eCubeTypes.OreDiamond_T4_2; break;
            case "None":        msMine1 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine2)
        {
            case "OreCoal":     msMine2 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msMine2 = eCubeTypes.OreCopper; break;
            case "OreTin":      msMine2 = eCubeTypes.OreTin; break;
            case "OreIron":     msMine2 = eCubeTypes.OreIron; break;
            case "OreLithium":  msMine2 = eCubeTypes.OreLithium; break;
            case "OreGold":     msMine2 = eCubeTypes.OreGold; break;
            case "OreNickel":   msMine2 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine2 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msMine2 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msMine2 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine2 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly":     msMine2 = eCubeTypes.OreDiamond_T4_2; break;
            case "None":        msMine2 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine3)
        {
            case "OreCoal":     msMine3 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msMine3 = eCubeTypes.OreCopper; break;
            case "OreTin":      msMine3 = eCubeTypes.OreTin; break;
            case "OreIron":     msMine3 = eCubeTypes.OreIron; break;
            case "OreLithium":  msMine3 = eCubeTypes.OreLithium; break;
            case "OreGold":     msMine3 = eCubeTypes.OreGold; break;
            case "OreNickel":   msMine3 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine3 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msMine3 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msMine3 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine3 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly":     msMine3 = eCubeTypes.OreDiamond_T4_2; break;
            case "None":        msMine3 = eCubeTypes.Air; break;
        }
        switch (qconfig.Mine4)
        {
            case "OreCoal":     msMine4 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msMine4 = eCubeTypes.OreCopper; break;
            case "OreTin":      msMine4 = eCubeTypes.OreTin; break;
            case "OreIron":     msMine4 = eCubeTypes.OreIron; break;
            case "OreLithium":  msMine4 = eCubeTypes.OreLithium; break;
            case "OreGold":     msMine4 = eCubeTypes.OreGold; break;
            case "OreNickel":   msMine4 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msMine4 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msMine4 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msMine4 = eCubeTypes.OreBioMass; break;
            case "OreChromium": msMine4 = eCubeTypes.OreEmerald_T4_1; break;
            case "OreMoly":     msMine4 = eCubeTypes.OreDiamond_T4_2; break;
            case "None":        msMine4 = eCubeTypes.Air; break;
        }
        this.mnSize = qconfig.Size;
        return true;
    }

	// ************************************************************************************************************************************************
	public override void DropGameObject ()
	{
		base.DropGameObject ();
		mbLinkedToGO = false;
		DrillDrone = null;
		LaserHolder = null;
		Laser = null;
		LaserImpact = null;
		LaserImpactDust = null;
		LaserSource = null;
	}
	// ************************************************************************************************************************************************
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
				if (mWrapper.mGameObjectList == null) Debug.LogError("Q missing game object #0?");
				if (mWrapper.mGameObjectList[0].gameObject == null) Debug.LogError("Q missing game object #0 (GO)?");

				DrillDrone = mWrapper.mGameObjectList[0].transform.Find("DrillDrone").gameObject;

				DrillDrone.transform.position = mWrapper.mGameObjectList[0].transform.position + new Vector3(0,50,0);

				LaserHolder = DrillDrone.transform.Find("LaserTransfer").gameObject;
				if (LaserHolder == null) Debug.LogError("Can't find LaserHolder!");
				Laser = LaserHolder.transform.Search("Laser").gameObject;
				LaserImpact = DrillDrone.transform.Search("Laser Impact").gameObject;
				LaserImpactDust = LaserImpact.transform.Search("LaserImpactDust").gameObject.GetComponent<ParticleSystem>();

				LaserSource  = LaserHolder.transform.Search("Laser Source").gameObject;

				if (Laser == null) Debug.LogError("Can't find LaserHolder!");

				mAnimation = mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();

				mbLinkedToGO = true;

				//default to off
				Laser.SetActive (false);
				LaserImpact.GetComponent<ParticleSystem>().emissionRate = 0;
				LaserImpact.GetComponent<Light>().enabled = false;
				LaserImpactDust.emissionRate = 0;
				LaserSource.SetActive(false);
				LaserImpact.GetComponent<MeshRenderer>().enabled = false;

                QuarryBase = base.mWrapper.mGameObjectList[0].gameObject.transform.Search("SmallQuarry").gameObject;
                MeshRenderer baserenderer = QuarryBase.GetComponent<MeshRenderer>();
                
                if (baserenderer != null)
                {
                    baserenderer.material.SetColor("_Color", Color.black);                    
                }
			}
		}

		if (mbUserHasConfirmedQuarry)
		{
			//float drone above target drill square
			if (mUnityDronePos != Vector3.zero)
			{
				//remain in the air until we've done the initial clear
				if (Depth < 0) mHoverOffset = new Vector3(0,Depth,0);
				
				//Drone floats very slowly, but will eventually get into place over ores, which take time to extract
				DrillDrone.transform.position += ((mUnityDronePos + mHoverOffset) - DrillDrone.transform.position) * Time.deltaTime * 0.05f;

			}


			Vector3 lDrillVec = Vector3.down;
			float lrRate = 1.0f;

			if (mUnityDrillPos != Vector3.zero)
			{
				//I'm not 100% there's not a code path that involves this not happening, hence the dumb light check
				if (mrUnityDrillResetTimer == 0.0f || LaserImpact.GetComponent<Light>().enabled ==false)
				{
					LaserImpact.GetComponent<MeshRenderer>().enabled = true;
					LaserImpact.transform.position = mUnityDrillPos + Vector3.up;
					LaserImpact.transform.forward = Vector3.up;


					//LaserImpact.SetActive (true);
					LaserImpact.GetComponent<ParticleSystem>().emissionRate = 250;
					LaserImpact.GetComponent<Light>().enabled = true;
					LaserImpactDust.emissionRate = 64;
					LaserSource.SetActive (true);

					Laser.SetActive (true);
					Vector3 lVTT = mUnityDrillPos - Laser.transform.position;
					mrLaserDist = lVTT.magnitude;
					Laser.transform.localScale = new Vector3(mrLaserDist,1,1);

				//	Debug.DrawRay(mWrapper.mGameObjectList[0].transform.position,mForwards * 5.0f,Color.red,1.0f);
						
						
				}

				//Drilling is active here
				Laser.transform.localScale = new Vector3(
					mrLaserDist,
					UnityEngine.Random.Range(50,550) / 100.0f,
					UnityEngine.Random.Range(50,550) / 100.0f);

				lDrillVec = mUnityDrillPos - LaserHolder.transform.position;
				lrRate = 15.0f;//really point right at it quickly
				mrUnityDrillResetTimer+= Time.deltaTime;
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
					Laser.SetActive (false);

					LaserImpact.GetComponent<ParticleSystem>().emissionRate = 0;
					LaserImpact.GetComponent<Light>().enabled = true;
					LaserImpactDust.emissionRate = 0;
					//LaserImpact.SetActive (false);//fucking particles don't hang around ofc
					LaserImpact.GetComponent<MeshRenderer>().enabled = false;

					LaserSource.SetActive (false);
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
			lTargetPos += mForwards * mnSize/2.0f;
			lTargetPos.y += 8.0f;
			DrillDrone.transform.position += (lTargetPos - DrillDrone.transform.position) * Time.deltaTime;
		}
	}
	// ************************************************************************************************************************************************
	public void ConfirmQuarry()
	{
		mbUserHasConfirmedQuarry = true;
		//Shunt one further so we're on the outside

		MarkDirtyDelayed();
		RequestImmediateNetworkUpdate();
	}
	// ************************************************************************************************************************************************

	// *********************************************************************************************************
	public override void LowFrequencyUpdate()
	{
        float lfTimeStep = LowFrequencyThread.mrPreviousUpdateTimeStep/4;
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

        float threadpowercost = this.mfBasePowerCost * LowFrequencyThread.mrPreviousUpdateTimeStep;
        if (this.mrCurrentPower < threadpowercost)
        {
            // not enough power for basic functions
            mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
            return;
        }
        else
        {
            this.mrCurrentPower -= threadpowercost;
        }

        // now, do we have enough power left over to dig?
        if (!HasEnoughPower(Depth))
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

            if (!HasEnoughPower(Depth)) // do we have enough power for another block?
            {
                mrPowerWaitTime += lfTimeStep/4;
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
                    return;
                }
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

	// *********************************************************************************************************
	bool DoHealthAndSafetyCheck()
	{
        // height check
        if (mnY - WorldScript.mDefaultOffset < -275)
        {
            mbBuiltTooLow = true;            
        }
        // needed variable checks here

		//draw around the quarry area; failure involves coming back next frame
		//todo, check hopper for resources

		mbHealthAndSafetyChecksPassed = false;

		mbMissingInputHopper = true;
		UpdateAttachedHoppers(false);
		if (mnNumAttachedHoppers == 0)
		{
			mbMissingInputHopper = true;
			return false;
		}

		//if we can get a chevron, do so now
		bool lbGotChevron = false;
		for (int i=0;i<mnNumAttachedHoppers;i++)
		{
            StorageMachineInterface storageMachineInterface = this.maAttachedHoppers[i] as StorageMachineInterface;
            if (storageMachineInterface != null && storageMachineInterface.TryExtractCubes(this, 124, global::TerrainData.GetDefaultValue(124), 1))
            {
                this.mbMissingChevrons = false;
                lbGotChevron = true;
                break;
            }
/*
			int lnChevrons;
			lnChevrons = maAttachedHoppers[i].CountHowManyOfType(eCubeTypes.ChevronRedWhite,TerrainData.GetDefaultValue(eCubeTypes.ChevronRedWhite));
			if (lnChevrons > 0)
			{
				//Debug.LogWarning("Found " + lnChevrons + " in attached storage hopper");
				if (maAttachedHoppers[i].RemoveInventoryCube(eCubeTypes.ChevronRedWhite))
 			    {
					mbMissingChevrons = false;
					lbGotChevron = true;
				}
				else
				{
					Debug.LogError("Error, storage hopper said it had chevrons, but it didn't seem to!");
					return false;
				}


				break;
			}
 * */
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
		mnBuildShuntX = (int)(mForwards.x * (mnHalfSize + 1));
		mnBuildShuntZ = (int)(mForwards.z * (mnHalfSize + 1));

		//now build the H&S barrier, except on us

		int lnChevronIndex = 0;//just run the build process below until we get to this index. Cheap and hacky but works.
		//Debug.LogWarning ("Building Chevron Index " + mnChevronsPlaced);

		for (int x=0;x<mnSize+2;x++)
			for (int z=0;z<mnSize+2;z++)
			{
				long lx = x + mnX + mnBuildShuntX - (mnHalfSize);//the +1 centralises it
				long ly = mnY -1;
				long lz = z + mnZ + mnBuildShuntZ - (mnHalfSize);
				
			//	if (lx == mnX && lz == mnZ) continue;//dont build over us!

				if (x == 0 || z == 0 || x == mnSize+1 || z == mnSize+1)
				{
					//If we have a chevron
					if (lnChevronIndex == mnChevronsPlaced)
					{
						
						if (BuildChevron(lx,ly,lz,eCubeTypes.ChevronRedWhite))
						{
							if (lx != mnX && lz != mnZ) //dont build over us!
							{
								BuildChevron(lx,ly+1,lz,eCubeTypes.Air);//don't care if this fails
								
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
		return true;;

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

			int lnXMod = (mnXSearch-(mnHalfSize-1)) + mnBuildShuntX;
			int lnZMod = (mnZSearch-(mnHalfSize-1)) + mnBuildShuntZ;

			long lx = mnX + lnXMod;
			long lz = mnZ + lnZMod;
			long ly = mnY + 4;//our hover height

			mUnityDronePos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(lx, ly, lz)  + new Vector3(0.5f,0.5f,0.5f);//CENTRE of the cube
		}
		//Debug.LogWarning("XS:" + mnXSearch + ".ZS:" + mnZSearch);
	}

    // ************************************************************************************************************
	bool AttemptToOffloadCargo()
	{
		if (mCarryCube == eCubeTypes.NULL) return true;//sure. Why not.

		UpdateAttachedHoppers(true);

		mbMissingOutputHopper = false;
		mbOutputHoppersLockedOrFull = false;
		
		if (mnTotalHoppers == 0)//this includes invalid ones
		{
			mbMissingOutputHopper = true;
			mbOutputHoppersLockedOrFull = false;//or at least unknown
			return false;
		}
		if (mnNumAttachedHoppers == 0)
		{
			mbOutputHoppersLockedOrFull = true;//or at least unknown
			return false;
		}

		//we know that all these hoppers are valid places to put things, so we only need to worry about the first one
		//(When full, the 0'th hopper won't be in the next update's UpdateAttachedHoppers
		//This could be reflected in the search code
        StorageMachineInterface storageMachineInterface = this.maAttachedHoppers[0] as StorageMachineInterface;
        if (storageMachineInterface != null && storageMachineInterface.TryInsert(this, this.mCarryCube, this.mCarryValue, 1))
        {
            this.mCarryCube = 0;
            if (storageMachineInterface is MachineEntity)
            {
                ((MachineEntity)storageMachineInterface).RequestImmediateNetworkUpdate();
            }
            //this.mbCollectionEffectRequested = true;
            this.RequestImmediateNetworkUpdate();
            return true;
        }

        /*

		if (maAttachedHoppers[0].mnStorageFree <=0)
		{
			Debug.LogError("Derp, how did a Quarry pick a full hopper to empty into?");
		}
		else
		{
			maAttachedHoppers[0].AddCube(mCarryCube,mCarryValue);
		}
		mCarryCube = eCubeTypes.NULL;
         */


		return true;

	}
	// ************************************************************************************************************

    bool HasEnoughPower(int lDepth)
    {       
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
        if (lCube == eCubeTypes.MagmaFluid) return false;
        if (lCube == eCubeTypes.HiveSpawn) return false;
        if (lCube == eCubeTypes.HardenedResin) return false;
        if (lCube == eCubeTypes.AblatedResin) return false;
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
        if (lCube == 92 || lCube == 93 || lCube == 94) // T5-T6 ores
        {
            return false;
        }
        if (lCube == eCubeTypes.CentralPowerHub)
            return false;

        if (TerrainData.GetHardness(lCube, lValue) > this.lfHardnessLimit) return false;
        
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

		if (CubeHelper.IsOre(lCube))
		{
			// we got this far, do we have enough power to progress?
            if (!HasEnoughPower(Depth))
            {
                // not enough power to dig ore, return until we do.
                mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
                otherError = true;
                return false;
            }
            // check our mine types, if we are a mk4.
            if (CubeValue == 3)
            {
                if (lCube != msMine1 && lCube != msMine2 && lCube != msMine3 && lCube != msMine4)
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
			}

			mnTotalOreFound++; //take into account efficiency?
		}
		else
		{
			// Destroy this block, it's garbage

			// Make sure we have any adjacent segments required loaded in.
            if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
            {
                otherError = true;
                return false;
            }

			WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
			mnTotalNonOreCubes++;
			MoveToNextCube();
            lCube = eCubeTypes.NULL;
            mrCurrentDigCost = (this.mrBaseDigCost + Depth);
            mrCurrentPower -= mrCurrentDigCost; 
            return true;
		}

		mCarryCube = lCube;

		// check if we should use given value or the default - this works for ore as well!
		TerrainDataEntry lEntry = TerrainData.mEntries[mCarryCube];
		if (lEntry == null)
		{
//#if UNITY_EDITOR
//			Debug.LogWarning("Quarry digging unknown cube."); // editor only warning to avoid client spam - it doesn't break anything
//#endif
			mCarryValue = lData.mValue;
		}
		else
		{
			if (lEntry.GetValue(lData.mValue) != null)
			{
				mCarryValue = lData.mValue;
			}
			else
			{
				// no specific entry for this value, this was a colorised block or an ore. store as default value
				mCarryValue = lEntry.DefaultValue;
			}
		}
        mrCurrentDigCost = (this.mrBaseDigCost + Depth);        
        mrCurrentPower -= mrCurrentDigCost;                        		
		 
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
	// ************************************************************************************************************************************************

	// ************************************************************************************************************************************************
	void UpdateAttachedHoppers(bool lbInput)
	{
		int lnNextHopper = 0;
		mnTotalHoppers = 0;
		mnNumAttachedHoppers = 0;
		
		for (int i = 0; i < 6; i++)
		{
			maAttachedHoppers[i] = null;
			long CheckX = this.mnX;
			long CheckY = this.mnY;
			long CheckZ = this.mnZ;
			
			if (i == 0) CheckX--;
			if (i == 1) CheckX++;
			if (i == 2) CheckY--;
			if (i == 3) CheckY++;
			if (i == 4) CheckZ--;
			if (i == 5) CheckZ++;
			
			Segment targetSegment = AttemptGetSegment(CheckX, CheckY, CheckZ);

            if (targetSegment != null)
            {
                SegmentEntity segmentEntity = targetSegment.SearchEntity(CheckX, CheckY, CheckZ);
                if (segmentEntity != null)
                {
                    StorageMachineInterface storageMachineInterface = segmentEntity as StorageMachineInterface;
                    if (storageMachineInterface != null)
                    {
                        this.mnTotalHoppers++;
                        eHopperPermissions permissions = storageMachineInterface.GetPermissions();
                        if (permissions != eHopperPermissions.Locked)
                        {
                            if (lbInput || permissions != eHopperPermissions.AddOnly)
                            {
                                if (!lbInput || permissions != eHopperPermissions.RemoveOnly)
                                {
                                    if (!lbInput || !storageMachineInterface.IsFull())
                                    {
                                        if (lbInput || !storageMachineInterface.IsEmpty())
                                        {
                                            this.maAttachedHoppers[lnNextHopper] = segmentEntity;
                                            lnNextHopper++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
			
/*          Old Way
			if (targetSegment == null)
				continue;//I hope the next segment is ok :-)
			
			ushort lCube = targetSegment.GetCube(CheckX, CheckY, CheckZ);
			
			if (lCube == eCubeTypes.StorageHopper)
			{
				mnTotalHoppers++;
				StorageHopper lHop = targetSegment.FetchEntity(eSegmentEntity.StorageHopper, CheckX, CheckY, CheckZ) as StorageHopper;
				if (lHop == null) continue;
				if (lHop.mPermissions == StorageHopper.ePermissions.Locked) continue;
				if (lbInput == false && lHop.mPermissions == StorageHopper.ePermissions.AddOnly) continue;
				if (lbInput == true  && lHop.mPermissions == StorageHopper.ePermissions.RemoveOnly) continue;
				if (lbInput == true  && lHop.mnStorageFree <= 0) continue;
				if (lbInput == false && lHop.mnStorageUsed == 0) continue;//we want to get OUT, but there's nothing here
				maAttachedHoppers[lnNextHopper] = lHop;
				
				lnNextHopper++;
			}
 * */
		}


		//Better name; valid attached hoppers
		mnNumAttachedHoppers = lnNextHopper;
	}

	// ************************************************************************************************************************************************
	public override bool ShouldSave ()
	{
		return true;
	}

	// ************************************************************************************************************************************************
    public override bool ShouldNetworkUpdate()
    {
        return true;
    }
	
	public override void Write (System.IO.BinaryWriter writer)
	{
		int lnDummy = -1;
        int destroyignored = mbDoDestroyIgnored ? 1 : 0;
		writer.Write(mbHealthAndSafetyChecksPassed);
		writer.Write(mbUserHasConfirmedQuarry);
		writer.Write(Depth);
		writer.Write(mrCurrentPower);

		writer.Write(mfOreDigCostMult);
		writer.Write(lsQuarryLevel);
		writer.Write(lsEfficiency);
		writer.Write(lfHardnessLimit);

		writer.Write(mnSize);
		writer.Write(CubeValue);
		writer.Write(mnTotalOreCubes);
		writer.Write(mnTotalOreFound);

		writer.Write(mnTotalNonOreCubes);
		writer.Write(miTotalBlocksIgnored);
		writer.Write(destroyignored);
		writer.Write(lnDummy);

	//	Debug.LogWarning("Quarry saving " + mbHealthAndSafetyChecksPassed.ToString());
	}
	// ************************************************************************************************************************************************
	public override void Read (System.IO.BinaryReader reader, int entityVersion)
	{
		int lnDummy = -1;
        int destroyignored = 0;
		mbHealthAndSafetyChecksPassed   = reader.ReadBoolean();
		mbUserHasConfirmedQuarry 		= reader.ReadBoolean();
		Depth 							= reader.ReadInt32();
		mrCurrentPower 					= reader.ReadSingle();

		mfOreDigCostMult                = reader.ReadSingle();
		lsQuarryLevel                   = reader.ReadUInt16();
		lsEfficiency                    = reader.ReadUInt16();
		lfHardnessLimit                 = reader.ReadSingle();

		mnSize                          = reader.ReadInt32();
        CubeValue                       = reader.ReadUInt16();
		mnTotalOreCubes                 = reader.ReadInt32();
		mnTotalOreFound                 = reader.ReadInt32();

		mnTotalNonOreCubes              = reader.ReadInt32();
        if (CubeValue == 3)
        {
            miTotalBlocksIgnored        = reader.ReadInt32();
        }
        else
        {
            lnDummy = reader.ReadInt32();
        }
		destroyignored=reader.ReadInt32();
        this.mbDoDestroyIgnored = destroyignored > 0 ? true : false;

		lnDummy=reader.ReadInt32();

        switch (CubeValue)
        {
            case 0: lsQuarryLevel = 1; break;
            case 1: lsQuarryLevel = 2; break;
            case 2: lsQuarryLevel = 3; break;
            case 3: lsQuarryLevel = 4; break;
        }
		//Debug.LogError("Quarry loading " + mbHealthAndSafetyChecksPassed.ToString());
	}

    //******************** PowerConsumerInterface **********************	
	public float GetRemainingPowerCapacity()
	{
		return mrMaxPower - mrCurrentPower;
	}
	
	public float GetMaximumDeliveryRate()
	{
		return mrMaxTransferRate;
	}
	
	public float GetMaxPower()
	{
		return mrMaxPower;
	}
	
	public bool DeliverPower(float amount)//to what? O.o
	{
		
		if (amount > GetRemainingPowerCapacity())
			return false;
		
		mrCurrentPower += amount;
		MarkDirtyDelayed();
		return true;
	}
	
	public bool WantsPowerFromEntity(SegmentEntity entity)
	{
		return true;
	}		
	/****************************************************************************************/

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
    }

    public override string GetPopupText()
    {
        ushort selectBlockType = WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockType;
        bool flag = (int)selectBlockType == this.GetCubeType("FlexibleGames.AdvancedQuarrys");
        string popuptext = "";
        if (flag)
        {
            Mk4Quarry quarry = WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectedEntity as Mk4Quarry;
            if (quarry != null)
            {
                popuptext = "Quarry Mk4 the Hole Puncher";
                if (quarry.mbBuiltTooLow)
                {
                    popuptext = "Quarry is unable to draw sufficient O2 for cooling!";
                    popuptext += "\nPlease move the Quarry closer to the surface.";
                    popuptext += "\nThank you!";
                    return popuptext;
                }
                if (quarry.mbMissingInputHopper)
                {
                    popuptext = "Quarry cannot locate Input Hopper";
                    popuptext += "\nPlease attach a Storage Hopper";
                    popuptext += "\nThis should contain Red and White Chevrons";
                    return popuptext;
                }
                if (quarry.mbMissingChevrons)
                {
                    popuptext = "Quarry cannot locate Health and Safety Resources!";
                    popuptext += "\nQuarry needs more Red and White Chevrons";
                    popuptext += "\nPlace these in an attached Storage Hopper";
                    return popuptext;
                }
                if (!quarry.mbHealthAndSafetyChecksPassed)
                {
                    popuptext = "Quarry currently constructing Edge Warning";
                    popuptext += "\nSAFETY FIRST!";
                    return popuptext;
                }
                if (quarry.mbMissingOutputHopper)
                {
                    popuptext = "Quarry cannot find Output Hopper";
                    return popuptext;
                }
                if (quarry.mbOutputHoppersLockedOrFull)
                {
                    popuptext = "All output hoppers full or locked!";
                    return popuptext;
                }
                if (!quarry.mbUserHasConfirmedQuarry)
                {
                    popuptext = "Press E to confirm Quarry position\n";
                    popuptext += "Press Q to Destroy Ignored Ore: " + (mbDoDestroyIgnored ? "Yes" : "No");
                    popuptext += "\nOnce set, it cannot be reset.";
                    if (Input.GetKeyDown(KeyCode.E) && UIManager.AllowInteracting)
                    {
                        FlexibleQuarryWindow.ConfirmMk4(this, 4);
                        //this.ConfirmQuarry(); // QuarryWindow.Confirm(quarry);
                    }
                    if (Input.GetKeyDown(KeyCode.Q) && UIManager.AllowInteracting)
                    {
                        FlexibleQuarryWindow.Mk4DestroyOre(this, 4);
                        //mbDoDestroyIgnored = !mbDoDestroyIgnored;
                    }
                    return popuptext;
                }
                string text2;
                if (quarry.mrCurrentPower < quarry.mrCurrentDigCost)
                {
                    popuptext = string.Concat(new string[]
					{
						"Power Low (",
						quarry.mrCurrentDigCost.ToString("F0"),
						" needed to dig, ",
						quarry.mrCurrentPower.ToString("F0"),
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
						quarry.mrCurrentPower.ToString("F0"),
						"/",
						quarry.mrMaxPower.ToString("F0")
					});
                }
                text2 = popuptext;
                popuptext = string.Concat(new object[]
				{
					text2,
					"\nAt Depth ",
					quarry.Depth,
					"m ", 
                    "Size: ",
                    mnSize
				});
                text2 = popuptext;
                popuptext = string.Concat(new string[]
				{
					text2,
					"\nFound ",
					string.Format("{0:n0}", quarry.mnTotalOreFound),
					" in ",
					string.Format("{0:n0}", quarry.mnTotalOreCubes),
					" ore blocks"
				});
                popuptext = popuptext + "\nFound " + string.Format("{0:n0}", quarry.mnTotalNonOreCubes) + " non-ore blocks";
                if (CubeValue == 3)
                {
                    popuptext += "\nIgnored " + (mbDoDestroyIgnored ? " and Destroyed " : "") + string.Format("{0:n0}", miTotalBlocksIgnored) + " ore blocks.";
                }
                float num = quarry.mrWorkTime / quarry.mrTotalTime;
                float num2 = quarry.mrStorageWaitTime + quarry.mrPowerWaitTime + quarry.mrSearchWaitTime;
                popuptext = popuptext + "\nWork Ratio " + num.ToString("P1");
                popuptext = popuptext + "\nTotal Idle Time : " + num2.ToString("F0");
                popuptext += "(E to reset)";
                if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting)
                {
                    FlexibleQuarryWindow.ResetMk4(this, 4);
                }                
            }
        }
        return popuptext;
    }
}

