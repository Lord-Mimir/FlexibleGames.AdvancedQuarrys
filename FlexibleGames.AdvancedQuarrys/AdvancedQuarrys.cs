using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

class AdvancedQuarrys : MachineEntity, PowerConsumerInterface
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
    /// Quarry Level, 1, 2, or 3
    /// </summary>
    ushort lsQuarryLevel;
    /// <summary>
    /// Efficiency of advanced quarry (50%, 75%, 100%) NumOreRemaining > this value, decrement by this value.
    /// </summary>
    ushort lsEfficiency;
    float lfHardnessLimit;

    ushort msIgnore1;
    ushort msIgnore2;
    ushort msIgnore3;
    ushort msIgnore4;
    int miTotalBlocksIgnored;
    // end my variables

    // Mk1 = 17, Mk2 = 33, Mk3 = 65
	int mnSize = 9;


	//int mnDepthScanned;

	bool mbLinkedToGO;
	
	Animation mAnimation;

	ushort mReplaceType = eCubeTypes.Air;
	ushort mCarryCube;
	ushort mCarryValue;

    // Mk1 = 75, Mk2 = 150, Mk3 = 300
    // Digging ores = 4x
	float mrBaseDigCost = 150;//per cube extracted

    // no plans to change
	public float mrCurrentDigCost = 0;//Base + Depth

    // Mk1 = 250, Mk2 = 500, Mk3 = 20k
	public int HardnessLimit = 250;//T2 only
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
    public float mrMaxPower;// = 4096;//power is 150 per block + 1 per depth.

    public float mrMaxTransferRate = 1024; // ??

	// **************************************************************************************************************************d**********************
	public AdvancedQuarrys(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue, bool loadFromDisk, QuarryConfig qConfig) : base(eSegmentEntity.Quarry, SpawnableObjectEnum.Quarry, x, y, z, cube, flags, lValue, Vector3.zero, segment)
	{

        // which quarry am I?
        string cubeKey = global::TerrainData.GetCubeKey(cube, lValue);
        bool isMk1 = cubeKey == "FlexibleGames.Mk1Quarry";
        if (isMk1)
        {
            // Mk1 specific stuff 
            this.CubeValue = 0;
            this.lsQuarryLevel = 1;
            this.mrBaseDigCost = 75;
            this.mfOreDigCostMult = 1.25f;
            this.lsEfficiency = 5; // 20% efficiency
            this.mnSize = 17;
            this.mrMaxPower = 600;
            this.lfHardnessLimit = 250;
            this.Depth = -5;
            this.miTotalBlocksIgnored = 0;
        }
        bool isMk2 = cubeKey == "FlexibleGames.Mk2Quarry";
        if (isMk2)
        {
            // Mk2 specific stuff
            this.CubeValue = 1;
            this.lsQuarryLevel = 2;
            this.mrBaseDigCost = 150;
            this.mfOreDigCostMult = 2f;
            this.lsEfficiency = 2; // 50% efficiency
            this.mnSize = 33;
            this.mrMaxPower = 6144;
            this.mrMaxTransferRate = 4096;
            this.lfHardnessLimit = 500;
            this.Depth = -5;
            this.miTotalBlocksIgnored = 0;
        }
        bool isMk3 = cubeKey == "FlexibleGames.Mk3Quarry";
        if (isMk3)
        {
            //Yep, Mk3 specific stuff
            this.CubeValue = 2;
            this.lsQuarryLevel = 3;
            this.mrBaseDigCost = 150;
            this.mfOreDigCostMult = 4f;
            this.lsEfficiency = 1; // 100% efficiency
            this.mnSize = 65;
            this.mrMaxPower = 40000;
            this.mrMaxTransferRate = 10000;
            this.lfHardnessLimit = 20000;
            this.Depth = -10;
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

    private bool SetQuarryOptions(QuarryConfig qconfig)
    {
        // logic for detecting which blocks to ignore. Valid Values:
        //eCubeTypes.OreCoal, eCubeTypes.OreCopper, eCubeTypes.OreTin, eCubeTypes.OreIron, eCubeTypes.OreLithium,
        //eCubeTypes.OreGold, eCubeTypes.OreNickel, eCubeTypes.OreTitanium,
        //eCubeTypes.OreCrystal, eCubeTypes.OreBioMass
        //None
        switch (qconfig.Ignore1)
        {
            case "OreCoal":     msIgnore1 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msIgnore1 = eCubeTypes.OreCopper; break;
            case "OreTin":      msIgnore1 = eCubeTypes.OreTin; break;
            case "OreIron":     msIgnore1 = eCubeTypes.OreIron; break;
            case "OreLithium":  msIgnore1 = eCubeTypes.OreLithium; break;
            case "OreGold":     msIgnore1 = eCubeTypes.OreGold; break;
            case "OreNickel":   msIgnore1 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msIgnore1 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msIgnore1 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msIgnore1 = eCubeTypes.OreBioMass; break;
            case "None":        msIgnore1 = eCubeTypes.Air; break;
        }
        switch (qconfig.Ignore2)
        {
            case "OreCoal":     msIgnore2 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msIgnore2 = eCubeTypes.OreCopper; break;
            case "OreTin":      msIgnore2 = eCubeTypes.OreTin; break;
            case "OreIron":     msIgnore2 = eCubeTypes.OreIron; break;
            case "OreLithium":  msIgnore2 = eCubeTypes.OreLithium; break;
            case "OreGold":     msIgnore2 = eCubeTypes.OreGold; break;
            case "OreNickel":   msIgnore2 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msIgnore2 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msIgnore2 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msIgnore2 = eCubeTypes.OreBioMass; break;
            case "None":        msIgnore2 = eCubeTypes.Air; break;
        }
        switch (qconfig.Ignore3)
        {
            case "OreCoal":     msIgnore3 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msIgnore3 = eCubeTypes.OreCopper; break;
            case "OreTin":      msIgnore3 = eCubeTypes.OreTin; break;
            case "OreIron":     msIgnore3 = eCubeTypes.OreIron; break;
            case "OreLithium":  msIgnore3 = eCubeTypes.OreLithium; break;
            case "OreGold":     msIgnore3 = eCubeTypes.OreGold; break;
            case "OreNickel":   msIgnore3 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msIgnore3 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msIgnore3 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msIgnore3 = eCubeTypes.OreBioMass; break;
            case "None":        msIgnore3 = eCubeTypes.Air; break;
        }
        switch (qconfig.Ignore4)
        {
            case "OreCoal":     msIgnore4 = eCubeTypes.OreCoal; break;
            case "OreCopper":   msIgnore4 = eCubeTypes.OreCopper; break;
            case "OreTin":      msIgnore4 = eCubeTypes.OreTin; break;
            case "OreIron":     msIgnore4 = eCubeTypes.OreIron; break;
            case "OreLithium":  msIgnore4 = eCubeTypes.OreLithium; break;
            case "OreGold":     msIgnore4 = eCubeTypes.OreGold; break;
            case "OreNickel":   msIgnore4 = eCubeTypes.OreNickel; break;
            case "OreTitanium": msIgnore4 = eCubeTypes.OreTitanium; break;
            case "OreCrystal":  msIgnore4 = eCubeTypes.OreCrystal; break;
            case "OreBioMass":  msIgnore4 = eCubeTypes.OreBioMass; break;
            case "None":        msIgnore4 = eCubeTypes.Air; break;
        }

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
                    switch (CubeValue)
                    {
                        case 0:
                            baserenderer.material.SetColor("_Color", Color.green);
                            break;
                        case 1:
                            baserenderer.material.SetColor("_Color", Color.blue);
                            break;
                        case 2:
                            baserenderer.material.SetColor("_Color", Color.magenta);
                            break;
                    }
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
        float lfTimeStep = LowFrequencyThread.mrPreviousUpdateTimeStep/2;
        if (lsQuarryLevel < 3)
        {
            lfTimeStep = LowFrequencyThread.mrPreviousUpdateTimeStep;
        }

        // height check
        if (mnY - WorldScript.mDefaultOffset < -25)
        {
            mbBuiltTooLow = true;                
            return;
        }

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
        bool lpowerforore;

        if (!HasEnoughPower(Depth, out lpowerforore))
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

        bool doTwo = (lsQuarryLevel == 3);  // Mk3 does two blocks per update
        bool cargodroppedoff = false;

        //we search up to 32 air blocks until we get something to dig
        for (int i = 0; i < 32; i++)
        {

            //avoid recalcing this for every block #minoroptimisationj
            //only we have to recalc it cuz the X and Z searches change.
            int lnXMod = (mnXSearch - (mnHalfSize - 1)) + mnBuildShuntX;
            int lnZMod = (mnZSearch - (mnHalfSize - 1)) + mnBuildShuntZ;

            //Debug.LogWarning("lnXMod:" + lnXMod + ".lnZMod:" + lnZMod);


            long lx = mnX + lnXMod;
            long lz = mnZ + lnZMod;
            long ly = mnY - Depth;



            if (!AttemptToDig(lx, ly, lz))
            {                
                cargodroppedoff = AttemptToOffloadCargo();
                mUnityDrillPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(lx, ly, lz) + new Vector3(0.5f, 0.5f, 0.5f);//CENTRE of the cube

                mrWorkTime += lfTimeStep;

                if (doTwo)
                {
                    if (!cargodroppedoff)
                    {
                        // we need to do two blocks, but we don't have the storage for the one we have now... 
                        mrStorageWaitTime += lfTimeStep;
                        return;
                    }
                    if (!HasEnoughPower(Depth, out lpowerforore)) // do we have enough power for another block?
                    {
                        mrPowerWaitTime += lfTimeStep;
                        return;
                    }
                    i--;
                    doTwo = false;
                }
                else
                {
                    return; //we failed or stopped or succeeded for some reason It was probably ok tho!
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

        // needed variable checks here

		//draw 12x12 around the 10x10 quarry area; failure involves coming back next frame
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
			} */
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

    bool HasEnoughPower(int lDepth, out bool powerforore)
    {
        powerforore = false;
        float lCurrentDigCost = mrBaseDigCost;
        if (lDepth > 0) lCurrentDigCost += lDepth;
        if (mrCurrentPower > lCurrentDigCost)
        {
            powerforore = ((lCurrentDigCost) * mfOreDigCostMult) < mrCurrentPower;
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

        if (TerrainData.GetHardness(lCube, lValue) > this.lfHardnessLimit) return false;
        
        return true;
    }
	//ALMOST never return true
	//False == we did not simply remove the cube
	//True = cave/air only
	bool AttemptToDig(long checkX, long checkY, long checkZ)
	{
		mCarryCube = eCubeTypes.NULL;
		Segment checkSegment = null;
        bool dugOre = false;
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
				mrDigDelay = 1.0f;
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
			return false;//true? 
		}

		if (CubeHelper.IsOre(lCube))
		{
			// we got this far, do we have enough power to progress?
            if ((this.mrBaseDigCost + Depth) * this.mfOreDigCostMult > mrCurrentPower)
            {
                // not enough power to dig ore, return until we do.
                mrPowerWaitTime += LowFrequencyThread.mrPreviousUpdateTimeStep;
                return false;
            }
            // check our ignore types, if we are a mk3.
            if (CubeValue == 2)
            {
                if (lCube == msIgnore1 || lCube == msIgnore2 || lCube == msIgnore3 || lCube == msIgnore4)
                {
                    this.miTotalBlocksIgnored++;
                    MoveToNextCube();
                    return false;
                }
            }


		//	Debug.LogWarning(TerrainData.GetNameForValue(lCube,lData.mValue) +" has hardness " + lHardness.ToString("F0"));

            // hardness check (NO need to do this twice)
        /*    float lHardness = TerrainData.GetHardness(lCube, lData.mValue);
            if (lHardness > this.lfHardnessLimit)
            {
                MoveToNextCube();
                return false;
            } 
            else
            { */
			// if you're replacing with ANYTHING other than air, then CHECK VALUES (why would I excavate with anything other than air?)
			ushort lnOreLeft = lData.mValue;

            // oh.. this simply won't do
			//10% efficiency (variable efficiency)
			if (lnOreLeft > this.lsEfficiency) // 10)
			{
			//	Debug.Log("Decrementing ore from " + lnOreLeft);
                lnOreLeft -= this.lsEfficiency;// 10;
				checkSegment.SetCubeValueNoChecking((int)(checkX % WorldHelper.SegmentX), (int)(checkY % WorldHelper.SegmentY), (int)(checkZ % WorldHelper.SegmentZ), lnOreLeft);
				checkSegment.RequestDelayedSave();
                dugOre = true;
			}
			else
			{
				// Out of ore, remove block.
				if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
					return false;

				mnTotalOreCubes++;
				WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
				MoveToNextCube();
                dugOre = false;
			}

			mnTotalOreFound++; //take into account efficiency?
//			}
		}
		else
		{
			// Destory this block

			// Make sure we have any adjacent segments required loaded in.
			if (!LoadAdjacentSegmentsIfRequired(checkSegment, checkX, checkY, checkZ))
				return false;

			WorldScript.instance.BuildFromEntity(checkSegment, checkX, checkY, checkZ, mReplaceType, TerrainData.GetDefaultValue(mReplaceType));
			mnTotalNonOreCubes++;
			MoveToNextCube();
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
        if (dugOre)
        {
            mrCurrentPower -= (mrCurrentDigCost * this.mfOreDigCostMult);
        }
        else
        {
            mrCurrentPower -= mrCurrentDigCost;
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
/*          Old way of doing it
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
 */
		}


		//Better name; valid attached hoppers
		mnNumAttachedHoppers = lnNextHopper;
		/*
		for (int i=0;i<mnNumAttachedHoppers;i++)
		{
			if (lbInput)
			{
				if (maAttachedHoppers[i].mnStorageFree <
			}
		}
		*/
		//	Debug.LogWarning("RA found " + mnNumAttachedHoppers + " attached hoppers");
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
		writer.Write(lnDummy);
		writer.Write(lnDummy);

	//	Debug.LogWarning("Quarry saving " + mbHealthAndSafetyChecksPassed.ToString());
	}
	// ************************************************************************************************************************************************
	public override void Read (System.IO.BinaryReader reader, int entityVersion)
	{
		int lnDummy = -1;
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
        if (CubeValue == 2)
        {
            miTotalBlocksIgnored        = reader.ReadInt32();
        }
        else
        {
            lnDummy = reader.ReadInt32();
        }
		lnDummy=reader.ReadInt32();
		lnDummy=reader.ReadInt32();

        switch (CubeValue)
        {
            case 0: lsQuarryLevel = 1; break;
            case 1: lsQuarryLevel = 2; break;
            case 2: lsQuarryLevel = 3; break;
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
            AdvancedQuarrys quarry = WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectedEntity as AdvancedQuarrys;
            if (quarry != null)
            {
                popuptext = "Quarry Mk" + (quarry.CubeValue + 1) + (CubeValue == 2 ? " the World Eater" : "");
                if (quarry.mbBuiltTooLow)
                {
                    popuptext = "Quarry is unable to draw sufficient O2 for cooling!";
                    popuptext += "\nPlease move the Quarry close to the surface.";
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
                    popuptext = "Press E to confirm Quarry position";
                    if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting)
                    {
                        FlexibleQuarryWindow.ConfirmMk123(this, 123);
                        //this.ConfirmQuarry(); // QuarryWindow.Confirm(quarry);
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
					"m"
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
                if (CubeValue == 2)
                {
                    popuptext += "\nIgnored " + string.Format("{0:n0}", miTotalBlocksIgnored) + " ore blocks.";
                }
                float num = quarry.mrWorkTime / quarry.mrTotalTime;
                float num2 = quarry.mrStorageWaitTime + quarry.mrPowerWaitTime + quarry.mrSearchWaitTime;
                popuptext = popuptext + "\nWork Ratio " + num.ToString("P1");
                popuptext = popuptext + "\nTotal Idle Time : " + num2.ToString("F0");
                popuptext += "(E to reset)";
                if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting)
                {
                    FlexibleQuarryWindow.ResetMk123(this, 123);
                }                
            }
        }
        return popuptext;
    }
}

